#nullable enable
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Models;

namespace BlazorWasmRegex.Shared.Interfaces
{
    public interface IRegexOptionsService
    {
        /// <summary>Maps the UI flags to <see cref="RegexOptions"/> (never adds <c>Compiled</c>).</summary>
        RegexOptions ToRegexOptions(RegexOptionsData data);

        /// <summary>
        /// Returns user-friendly messages for option combinations the .NET regex engine rejects.
        /// An empty list means the combination is valid.
        /// </summary>
        IReadOnlyList<string> Validate(RegexOptionsData data);

        /// <summary>
        /// Why the option <paramref name="optionId"/> (a <see cref="RegexOptionInfo.Id"/>) cannot be
        /// switched on together with the other options in <paramref name="data"/>, or <c>null</c> when it
        /// can. An option that is already on is never locked, so the user can always turn it off.
        /// </summary>
        string? GetDisabledReason(string optionId, RegexOptionsData data);

        /// <summary>
        /// Validates the options and constructs the regex with <paramref name="matchTimeout"/>,
        /// turning construction failures (parse errors, invalid options, constructs unsupported by
        /// NonBacktracking) into an error result instead of an exception.
        /// </summary>
        RegexBuildResult TryCreateRegex(string pattern, RegexOptionsData data, TimeSpan matchTimeout);

        /// <summary>Serializes options for local storage (JSON).</summary>
        string Serialize(RegexOptionsData data);

        /// <summary>
        /// Reads stored options: current JSON, older JSON missing newer fields, or the legacy
        /// pipe-delimited <c>{RegexText}|{IgnoreCase}|{Multiline}|{Singleline}</c> format.
        /// Returns <c>null</c> for empty or unrecognised input.
        /// </summary>
        RegexOptionsData? Deserialize(string? stored);
    }

    /// <summary>Outcome of <see cref="IRegexOptionsService.TryCreateRegex"/>.</summary>
    /// <param name="Regex">The constructed regex, or <c>null</c> on failure.</param>
    /// <param name="Error">Plain-text (not HTML-encoded) error message on failure.</param>
    /// <param name="ParseError">Parse error details when the pattern itself is invalid.</param>
    public sealed record RegexBuildResult(Regex? Regex, string? Error, RegexParseException? ParseError = null)
    {
        public bool Success => Regex is not null;
    }
}
