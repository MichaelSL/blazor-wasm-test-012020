#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using BlazorWasmRegex.Shared.Models;

namespace BlazorWasmRegex.Shared.Services
{
    /// <summary>
    /// Builds the JSON, CSV and text exports of the matches. The formats are the same as before the
    /// per-line redesign: JSON is an object keyed by test string (a repeated test line is exported once).
    /// </summary>
    public static class MatchExporter
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string ToJson(EvaluationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var export = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (var line in MatchedLines(result))
            {
                export.TryAdd(line.Text, line.Matches.Select(m => new
                {
                    m.Value,
                    m.Index,
                    m.Length,
                    Groups = m.Groups.Where(g => g.Number != 0).Select(g => new { g.Value, g.Index, g.Length }).ToList(),
                }).ToList());
            }
            return JsonSerializer.Serialize(export, JsonOptions);
        }

        public static string ToCsv(EvaluationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var csv = new StringBuilder();
            csv.AppendLine("Test String,Match Value,Match Index,Match Length");
            foreach (var line in MatchedLines(result))
            {
                foreach (var m in line.Matches)
                {
                    csv.AppendLine($"\"{EscapeCsv(line.Text)}\",\"{EscapeCsv(m.Value)}\",{m.Index},{m.Length}");
                }
            }
            return csv.ToString();
        }

        public static string ToText(string pattern, EvaluationResult result)
        {
            ArgumentNullException.ThrowIfNull(result);

            var lines = MatchedLines(result).ToList();
            var text = new StringBuilder();
            text.AppendLine($"Regex Pattern: {pattern}");
            text.AppendLine($"Total Matches: {lines.Sum(l => l.Matches.Count)}");
            text.AppendLine($"Test Strings with Matches: {lines.Count}");
            text.AppendLine();
            foreach (var line in lines)
            {
                text.AppendLine($"Test String: {line.Text}");
                text.AppendLine($"Matches ({line.Matches.Count}):");
                foreach (var m in line.Matches)
                {
                    text.AppendLine($"  - Value: \"{m.Value}\" (Index: {m.Index}, Length: {m.Length})");
                }
                text.AppendLine();
            }
            return text.ToString();
        }

        private static IEnumerable<LineResult> MatchedLines(EvaluationResult result) =>
            result.Lines.Where(l => l.Matches.Count > 0);

        private static string EscapeCsv(string value) =>
            string.IsNullOrEmpty(value) ? string.Empty : value.Replace("\"", "\"\"");
    }
}
