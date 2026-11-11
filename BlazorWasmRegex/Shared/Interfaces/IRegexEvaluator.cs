#nullable enable
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Models;

namespace BlazorWasmRegex.Shared.Interfaces
{
    public interface IRegexEvaluator
    {
        /// <summary>
        /// Runs <paramref name="regex"/> over each line of <paramref name="tests"/> (split on \n; a trailing
        /// \r is dropped). Every line is matched, split and (when <paramref name="replacement"/> is not
        /// empty) replaced on its own; empty lines are kept but not evaluated. All regex work happens here,
        /// so a <see cref="RegexMatchTimeoutException"/> is thrown from this call, never during rendering.
        /// </summary>
        EvaluationResult Evaluate(string? tests, Regex regex, string? replacement);

        /// <summary>Splits <paramref name="text"/> into plain text and highlighted matches.</summary>
        IReadOnlyList<HighlightSpan> HighlightLine(string text, IReadOnlyList<MatchResult> matches);

        /// <summary>Runs for the value of a single match, colored by its capture groups.</summary>
        IReadOnlyList<HighlightRun> HighlightMatch(MatchResult match);
    }
}
