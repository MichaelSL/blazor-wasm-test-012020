using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Interfaces;
using BlazorWasmRegex.Shared.Models;
using BlazorWasmRegex.Shared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorWasmRegex.Client.Shared;

public partial class RegexTester : IDisposable
{
    // Storage keys and formats are unchanged from earlier versions (see AGENTS.md).
    private const string RegexStorageKey = "REGEX_SESSION_STORAGE_KEY";
    private const string TestsStorageKey = "TESTS_SESSION_STORAGE_KEY";
    private const string OptionsStorageKey = "REGEX_OPTIONS";
    private const string LiveStorageKey = "LIVE_MODE";

    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LiveDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FlashDuration = TimeSpan.FromSeconds(3.2);

    [Inject] private IRegexOptionsService RegexOptionsService { get; set; } = default!;
    [Inject] private IRegexEvaluator Evaluator { get; set; } = default!;
    [Inject] private ILocalStorageService LocalStorage { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private enum RunState { Empty, Ready, Error, Timeout }

    private sealed record TabInfo(string Key, string Label, int Count);

    private string Pattern { get; set; } = "";
    private string Tests { get; set; } = "";
    private string Replacement { get; set; } = "";
    private RegexOptionsData Options { get; set; } = RegexOptionsData.Default;
    private bool Live { get; set; } = true;
    private bool ShowAdvanced { get; set; }
    private string ActiveTab { get; set; } = "matches";
    private bool ExportOpen { get; set; }

    private RunState State { get; set; } = RunState.Empty;
    private EvaluationResult Result { get; set; } = EvaluationResult.Empty;
    private string ErrorMessage { get; set; } = "";
    private RegexParseException? ParseError { get; set; }
    private (string Main, string Sub)? Flash { get; set; }

    private Regex? regex;
    private (string Pattern, RegexOptionsData Options)? builtConfig;
    private CancellationTokenSource? liveCts;
    private CancellationTokenSource? flashCts;
    private ElementReference root;
    private readonly Dictionary<string, ElementReference> tabRefs = new();

    private bool IsReady => RendererInfo.IsInteractive;
    private int GroupCount => regex is null ? 0 : regex.GetGroupNumbers().Length - 1;
    private bool HasResults => State == RunState.Ready;
    private bool CanExport => HasResults && Result.TotalMatches > 0;
    private int AdvancedOnCount => RegexOptionInfo.All.Count(o => !o.IsPrimary && Options.IsSet(o.Id));

    private IEnumerable<TabInfo> Tabs =>
    [
        new("matches", "Matches", HasResults ? Result.TotalMatches : 0),
        new("split", "Split", HasResults ? Result.SplitParts : 0),
        new("table", "Table", HasResults ? Result.TotalMatches : 0),
        new("groups", "Groups", HasResults ? Result.CapturedGroups : 0),
        new("replace", "Replace", HasResults ? Result.ReplacedLines : 0),
    ];

    protected override async Task OnInitializedAsync()
    {
        // Prerendering on the server: no browser storage, controls stay disabled.
        if (!RendererInfo.IsInteractive)
            return;

        var storedPattern = await LocalStorage.GetStringAsync(RegexStorageKey);
        var storedTests = await LocalStorage.GetStringAsync(TestsStorageKey);
        if (storedPattern is null && storedTests is null)
        {
            // First visit: show what the tester does instead of an empty form.
            var sample = SampleRegexLibrary.All[0];
            Pattern = sample.Pattern;
            Tests = sample.TestData;
        }
        else
        {
            Pattern = storedPattern ?? "";
            Tests = storedTests ?? "";
        }

        Options = RegexOptionsService.Deserialize(await LocalStorage.GetStringAsync(OptionsStorageKey)) ?? RegexOptionsData.Default;
        Live = await LocalStorage.GetStringAsync(LiveStorageKey) != "false";
        // Don't hide active advanced options behind a collapsed section.
        ShowAdvanced = AdvancedOnCount > 0;

        await RunAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RendererInfo.IsInteractive)
        {
            try
            {
                await JS.InvokeVoidAsync("regexTester.attach", root);
            }
            catch (JSException ex)
            {
                // Without it the editor still works; only scroll sync and gutter heights are lost.
                Console.WriteLine($"regexTester.attach failed: {ex.Message}");
            }
        }
    }

    // ----- Evaluation -------------------------------------------------------------------------

    private async Task RunAsync()
    {
        CancelLive();
        Flash = null;

        if (string.IsNullOrEmpty(Pattern))
        {
            ClearResults(RunState.Empty);
            await LocalStorage.SetStringAsync(RegexStorageKey, Pattern);
            await LocalStorage.SetStringAsync(TestsStorageKey, Tests);
            return;
        }

        var config = (Pattern, Options);
        if (builtConfig != config)
        {
            var build = RegexOptionsService.TryCreateRegex(Pattern, Options, RegexTimeout);
            if (!build.Success)
            {
                regex = null;
                builtConfig = null;
                ClearResults(RunState.Error);
                ParseError = build.ParseError;
                ErrorMessage = build.ParseError is { } parse
                    ? PatternHighlighter.DescribeParseError(parse.Error)
                    : build.Error ?? "Invalid pattern";
                await LocalStorage.SetStringAsync(TestsStorageKey, Tests);
                return;
            }

            regex = build.Regex!;
            builtConfig = config;
            await LocalStorage.SetStringAsync(RegexStorageKey, Pattern);
            await LocalStorage.SetStringAsync(OptionsStorageKey, RegexOptionsService.Serialize(Options));
        }

        try
        {
            Result = Evaluator.Evaluate(Tests, regex!, Replacement);
            State = RunState.Ready;
            ParseError = null;
            ErrorMessage = "";
        }
        catch (RegexMatchTimeoutException ex)
        {
            Console.WriteLine($"Regex timed out: {ex.Message}");
            ClearResults(RunState.Timeout);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Regex evaluation failed: {ex}");
            ClearResults(RunState.Error);
            ErrorMessage = "Error evaluating the expression.";
        }

        await LocalStorage.SetStringAsync(TestsStorageKey, Tests);
    }

    private void ClearResults(RunState state)
    {
        State = state;
        Result = EvaluationResult.Empty;
        ParseError = null;
        ErrorMessage = "";
        ExportOpen = false;
    }

    // Debounced re-run while typing when Live is on.
    private async Task ScheduleRunAsync()
    {
        if (!Live)
            return;

        CancelLive();
        var cts = liveCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(LiveDelay, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }
        await RunAsync();
        StateHasChanged();
    }

    private void CancelLive()
    {
        liveCts?.Cancel();
        liveCts?.Dispose();
        liveCts = null;
    }

    // ----- Input handlers ---------------------------------------------------------------------

    private Task OnInputChanged() => ScheduleRunAsync();

    private async Task OnOptionChanged(string id, ChangeEventArgs e)
    {
        Options = Options.With(id, e.Value is true);
        if (Live)
            await RunAsync();
    }

    private async Task OnLiveChanged(ChangeEventArgs e)
    {
        Live = e.Value is true;
        await LocalStorage.SetStringAsync(LiveStorageKey, Live ? "true" : "false");
        if (Live)
            await RunAsync();
    }

    private async Task LoadSample(RegexSample sample)
    {
        Pattern = sample.Pattern;
        // Replace the test lines only when they are empty or still another sample's data.
        if (string.IsNullOrWhiteSpace(Tests) || SampleRegexLibrary.All.Any(s => s.TestData == Tests))
        {
            Tests = sample.TestData;
        }
        await RunAsync();
    }

    private async Task OnFormKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && (e.CtrlKey || e.MetaKey))
        {
            await RunAsync();
        }
    }

    private Task TestClick() => RunAsync();

    private async Task ReplaceClick()
    {
        await RunAsync();
        ActiveTab = "replace";
    }

    // ----- Tabs -------------------------------------------------------------------------------

    private void SelectTab(string key) => ActiveTab = key;

    private async Task OnTabKeyDown(KeyboardEventArgs e)
    {
        var keys = Tabs.Select(t => t.Key).ToList();
        var index = keys.IndexOf(ActiveTab);
        var next = e.Key switch
        {
            "ArrowRight" => (index + 1) % keys.Count,
            "ArrowLeft" => (index - 1 + keys.Count) % keys.Count,
            "Home" => 0,
            "End" => keys.Count - 1,
            _ => -1,
        };
        if (next < 0)
            return;

        ActiveTab = keys[next];
        if (tabRefs.TryGetValue(ActiveTab, out var tab))
        {
            await tab.FocusAsync();
        }
    }

    // ----- Export -----------------------------------------------------------------------------

    private void ToggleExport() => ExportOpen = CanExport && !ExportOpen;

    private void OnExportKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
            ExportOpen = false;
    }

    private async Task Export(string kind)
    {
        ExportOpen = false;
        if (!CanExport)
            return;

        var (file, content, type, label) = kind switch
        {
            "json" => ("matches.json", MatchExporter.ToJson(Result), "application/json", "JSON"),
            "csv" => ("matches.csv", MatchExporter.ToCsv(Result), "text/csv", "CSV"),
            _ => ("matches.txt", MatchExporter.ToText(Pattern, Result), "text/plain", "text"),
        };

        try
        {
            await JS.InvokeVoidAsync("downloadFile", file, content, type);
            await ShowFlash($"Exported matches as {label}", file);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Export failed: {ex.Message}");
            await ShowFlash("Export failed", ex.Message);
        }
    }

    private async Task ShowFlash(string main, string sub)
    {
        flashCts?.Cancel();
        var cts = flashCts = new CancellationTokenSource();
        Flash = (main, sub);
        StateHasChanged();
        try
        {
            await Task.Delay(FlashDuration, cts.Token);
            Flash = null;
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
        }
    }

    // ----- View helpers -----------------------------------------------------------------------

    private IReadOnlyList<PatternSegment> PatternSegments =>
        PatternHighlighter.Highlight(Pattern, Options.ExplicitCapture, Options.IgnorePatternWhitespace,
            builtConfig == (Pattern, Options) ? regex : null, ParseError);

    private string PatternErrorText()
    {
        if (ParseError is not { } parse)
            return ErrorMessage;

        var (pos, atEnd) = PatternHighlighter.ErrorPosition(Pattern, parse);
        var where = atEnd ? "end of pattern" : $"'{Pattern[pos]}'";
        return $"{ErrorMessage} at offset {parse.Offset} ({where}).";
    }

    private (string Letters, string Extras) Flags()
    {
        var on = RegexOptionInfo.All.Where(o => Options.IsSet(o.Id)).ToList();
        return (string.Concat(on.Where(o => o.IsInline).Select(o => o.Flag)),
                string.Join("·", on.Where(o => !o.IsInline).Select(o => o.Flag)));
    }

    private static List<string> EditorLines(string? tests) =>
        string.IsNullOrEmpty(tests) ? [""] : tests.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    // The result for editor line i, unless the line was edited since the last run.
    private LineResult? ResultForLine(int i, string text) =>
        HasResults && i < Result.Lines.Count && Result.Lines[i].Text == text ? Result.Lines[i] : null;

    private bool IsSampleActive(RegexSample sample) => Pattern == sample.Pattern;

    public void Dispose()
    {
        CancelLive();
        flashCts?.Cancel();
        flashCts?.Dispose();
    }
}
