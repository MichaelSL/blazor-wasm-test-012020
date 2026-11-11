#nullable enable
using System;
using System.Collections.Generic;

namespace BlazorWasmRegex.Shared.Models
{
    /// <summary>
    /// UI metadata for one <see cref="System.Text.RegularExpressions.RegexOptions"/> flag.
    /// </summary>
    /// <param name="Id">The <c>RegexOptions</c> member name, also used as the element id suffix.</param>
    /// <param name="Label">Short label shown on the toggle.</param>
    /// <param name="Flag">Letter(s) shown after the pattern, e.g. <c>i</c> or <c>NB</c>.</param>
    /// <param name="IsInline">True when <see cref="Flag"/> is a real inline option letter, as in <c>(?imnsx)</c>.</param>
    /// <param name="IsPrimary">Always visible; the others live under "Advanced".</param>
    /// <param name="Tip">Tooltip text.</param>
    /// <param name="Tag">Optional badge, e.g. ".NET 11".</param>
    public sealed record RegexOptionInfo(
        string Id,
        string Label,
        string Flag,
        bool IsInline,
        bool IsPrimary,
        string Tip,
        string? Tag = null)
    {
        /// <summary>Every option the tester exposes, in display order.</summary>
        public static IReadOnlyList<RegexOptionInfo> All { get; } =
        [
            new("IgnoreCase", "Ignore case", "i", true, true,
                "Case-insensitive matching. Same as the inline option (?i)."),
            new("Multiline", "Multiline", "m", true, true,
                "^ and $ match at the start and end of each line, not just the whole input. Inline: (?m)."),
            new("Singleline", "Singleline", "s", true, true,
                "The dot (.) also matches \\n. Inline: (?s)."),
            new("AnyNewLine", "Any NewLine", "ANL", false, false,
                "^, $, \\Z and . treat \\r\\n, \\r, \\n, \\u0085, \\u2028 and \\u2029 as line breaks, not only \\n.",
                ".NET 11"),
            new("NonBacktracking", "NonBacktracking", "NB", false, false,
                "Linear-time engine without backtracking. Does not support backreferences, lookarounds, atomic groups, conditionals, balancing groups or \\G."),
            new("ExplicitCapture", "Explicit capture", "n", true, false,
                "Only named or numbered groups like (?<name>...) capture; plain (...) groups do not. Inline: (?n)."),
            new("IgnorePatternWhitespace", "Ignore pattern whitespace", "x", true, false,
                "Ignore unescaped whitespace in the pattern and allow # comments. Inline: (?x)."),
            new("RightToLeft", "Right to left", "RTL", false, false,
                "Search from the end of the input towards the start."),
            new("CultureInvariant", "Culture invariant", "CI", false, false,
                "Ignore cultural differences in language when ignoring case."),
            new("ECMAScript", "ECMAScript", "ES", false, false,
                "ECMAScript-compliant behaviour for character classes and backreferences."),
        ];
    }

    /// <summary>Reads and sets <see cref="RegexOptionsData"/> flags by <see cref="RegexOptionInfo.Id"/>.</summary>
    public static class RegexOptionsDataExtensions
    {
        public static bool IsSet(this RegexOptionsData data, string id) => id switch
        {
            "IgnoreCase" => data.IgnoreCase,
            "Multiline" => data.Multiline,
            "Singleline" => data.Singleline,
            "AnyNewLine" => data.AnyNewLine,
            "NonBacktracking" => data.NonBacktracking,
            "ExplicitCapture" => data.ExplicitCapture,
            "IgnorePatternWhitespace" => data.IgnorePatternWhitespace,
            "RightToLeft" => data.RightToLeft,
            "CultureInvariant" => data.CultureInvariant,
            "ECMAScript" => data.ECMAScript,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown regex option."),
        };

        public static RegexOptionsData With(this RegexOptionsData data, string id, bool value) => id switch
        {
            "IgnoreCase" => data with { IgnoreCase = value },
            "Multiline" => data with { Multiline = value },
            "Singleline" => data with { Singleline = value },
            "AnyNewLine" => data with { AnyNewLine = value },
            "NonBacktracking" => data with { NonBacktracking = value },
            "ExplicitCapture" => data with { ExplicitCapture = value },
            "IgnorePatternWhitespace" => data with { IgnorePatternWhitespace = value },
            "RightToLeft" => data with { RightToLeft = value },
            "CultureInvariant" => data with { CultureInvariant = value },
            "ECMAScript" => data with { ECMAScript = value },
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown regex option."),
        };
    }
}
