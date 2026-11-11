#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using BlazorWasmRegex.Shared.Interfaces;
using BlazorWasmRegex.Shared.Models;

namespace BlazorWasmRegex.Shared.Services
{
    public class RegexOptionsService : IRegexOptionsService
    {
        public RegexOptions ToRegexOptions(RegexOptionsData data)
        {
            ArgumentNullException.ThrowIfNull(data);

            // RegexOptions.Compiled is intentionally omitted: WASM only has the regex interpreter.
            var options = RegexOptions.None;
            if (data.IgnoreCase) options |= RegexOptions.IgnoreCase;
            if (data.Multiline) options |= RegexOptions.Multiline;
            if (data.Singleline) options |= RegexOptions.Singleline;
            if (data.AnyNewLine) options |= RegexOptions.AnyNewLine;
            if (data.NonBacktracking) options |= RegexOptions.NonBacktracking;
            if (data.ExplicitCapture) options |= RegexOptions.ExplicitCapture;
            if (data.IgnorePatternWhitespace) options |= RegexOptions.IgnorePatternWhitespace;
            if (data.RightToLeft) options |= RegexOptions.RightToLeft;
            if (data.CultureInvariant) options |= RegexOptions.CultureInvariant;
            if (data.ECMAScript) options |= RegexOptions.ECMAScript;
            return options;
        }

        public IReadOnlyList<string> Validate(RegexOptionsData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            var errors = new List<string>();

            // Rules mirror Regex.ValidateOptions in .NET 11 (verified empirically): each of these
            // combinations throws ArgumentOutOfRangeException from the Regex constructor.
            if (data.ECMAScript && (data.Singleline || data.ExplicitCapture || data.IgnorePatternWhitespace
                                    || data.RightToLeft || data.NonBacktracking || data.AnyNewLine))
            {
                errors.Add("ECMAScript can only be combined with Ignore Case, Multiline and Culture Invariant.");
            }
            if (data.NonBacktracking && data.RightToLeft)
            {
                errors.Add("NonBacktracking cannot be combined with RightToLeft.");
            }
            if (data.AnyNewLine && data.NonBacktracking)
            {
                errors.Add("AnyNewLine cannot be combined with NonBacktracking.");
            }
            return errors;
        }

        public string? GetDisabledReason(string optionId, RegexOptionsData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (data.IsSet(optionId))
            {
                return null;
            }

            // Only report conflicts the new option introduces, not ones already present.
            var before = Validate(data);
            var after = Validate(data.With(optionId, true));
            var added = after.Where(e => !before.Contains(e)).ToList();
            return added.Count == 0 ? null : string.Join(" ", added);
        }

        public RegexBuildResult TryCreateRegex(string pattern, RegexOptionsData data, TimeSpan matchTimeout)
        {
            ArgumentNullException.ThrowIfNull(pattern);

            var errors = Validate(data);
            if (errors.Count > 0)
            {
                return new RegexBuildResult(null, "Invalid option combination: " + string.Join(" ", errors));
            }

            try
            {
                return new RegexBuildResult(new Regex(pattern, ToRegexOptions(data), matchTimeout), null);
            }
            catch (RegexParseException ex)
            {
                return new RegexBuildResult(null, $"Failed to parse an expression: {ex.Error} at pos {ex.Offset}.", ex);
            }
            catch (NotSupportedException ex) when (data.NonBacktracking)
            {
                // The non-backtracking engine rejects patterns that use a construct it lacks
                // (backreference, lookaround, atomic group, conditional, balancing group, \G) and
                // patterns whose automaton would be too large (e.g. big nested repetition counts).
                return new RegexBuildResult(null, DescribeNonBacktrackingFailure(ex.Message));
            }
            catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "options")
            {
                // Safety net for any combination Validate does not know about.
                return new RegexBuildResult(null, "Invalid option combination for the .NET regex engine.");
            }
            catch (ArgumentException ex)
            {
                return new RegexBuildResult(null, $"Failed to parse an expression: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds the user-facing message for a <see cref="NotSupportedException"/> thrown while constructing
        /// a NonBacktracking regex. Handles both the full desktop message and the trimmed resource-key form
        /// used in Blazor WASM.
        /// </summary>
        public static string DescribeNonBacktrackingFailure(string? message)
        {
            if (IsUnsafeSizeMessage(message))
            {
                return "NonBacktracking cannot handle this pattern: it is too large or complex (e.g. large nested repetition counts).";
            }

            var construct = DescribeUnsupportedConstruct(message);
            return construct is null
                ? "NonBacktracking does not support this pattern."
                : $"NonBacktracking does not support this pattern: it contains {construct}.";
        }

        // Desktop: "... could result in an automata as large as '1000001' nodes, which is larger than the
        // configured limit of '10000'."; WASM (trimmed resources): "NotSupported_NonBacktrackingUnsafeSize, ...".
        private static bool IsUnsafeSizeMessage(string? message) =>
            message is not null
            && (message.Contains("NonBacktrackingUnsafeSize", StringComparison.Ordinal)
                || message.Contains("automata as large as", StringComparison.Ordinal));

        /// <summary>
        /// Extracts the unsupported construct from a NonBacktracking <see cref="NotSupportedException"/>
        /// message. On desktop .NET the message reads "... expressions containing: 'backreference (\ number)'.";
        /// in Blazor WASM resource strings are trimmed (UseSystemResourceKeys), so it is only the resource
        /// keys, e.g. "NotSupported_NonBacktrackingConflictingExpression, ExpressionDescription_Backreference".
        /// </summary>
        public static string? DescribeUnsupportedConstruct(string? message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }

            var quoted = Regex.Match(message, @":\s*'(?<d>.+)'", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (quoted.Success)
            {
                return quoted.Groups["d"].Value;
            }

            var key = Regex.Match(message, @"ExpressionDescription_(?<k>\w+)", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (key.Success)
            {
                // "PositiveLookaroundAssertion" -> "positive lookaround assertion"
                return Regex.Replace(key.Groups["k"].Value, "(?<=[a-z])(?=[A-Z])", " ", RegexOptions.None, TimeSpan.FromSeconds(1))
                    .ToLowerInvariant();
            }

            return null;
        }

        public string Serialize(RegexOptionsData data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return JsonSerializer.Serialize(data);
        }

        public RegexOptionsData? Deserialize(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored))
            {
                return null;
            }

            try
            {
                // Missing properties bind to the constructor parameter defaults (false), so JSON from
                // older versions with only IgnoreCase/Multiline/Singleline still loads.
                return JsonSerializer.Deserialize<RegexOptionsData>(stored);
            }
            catch (JsonException)
            {
                // Legacy pipe-delimited format: "{RegexText}|{IgnoreCase}|{Multiline}|{Singleline}".
                // RegexText may itself contain '|', so only the last three values are options.
                var parts = stored.Split('|');
                if (parts.Length < 4)
                {
                    return null;
                }
                bool.TryParse(parts[^3], out var ignoreCase);
                bool.TryParse(parts[^2], out var multiline);
                bool.TryParse(parts[^1], out var singleline);
                return new RegexOptionsData(ignoreCase, multiline, singleline);
            }
        }
    }
}
