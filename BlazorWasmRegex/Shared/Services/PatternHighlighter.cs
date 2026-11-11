#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegex.Shared.Services
{
    /// <summary>A run of pattern text and the CSS class it is drawn with (<c>null</c> for plain text).</summary>
    public sealed record PatternSegment(string Text, string? CssClass);

    /// <summary>
    /// Splits a .NET regex pattern into colored segments for the pattern field: escapes, character
    /// classes, quantifiers, metacharacters and capture-group parentheses (colored by group number so
    /// they match the highlighted captures). It is a lightweight lexer for display only; the real
    /// <see cref="Regex"/> constructor remains the authority on whether the pattern is valid.
    /// </summary>
    public static class PatternHighlighter
    {
        /// <summary>Number of distinct capture-group colors; group n uses slot ((n - 1) % 6) + 1.</summary>
        public const int GroupColorCount = 6;

        /// <summary>Color slot (1..6) for capture group <paramref name="groupNumber"/>; 0 for the whole match.</summary>
        public static int SlotFor(int groupNumber) =>
            groupNumber <= 0 ? 0 : ((groupNumber - 1) % GroupColorCount) + 1;

        /// <summary>
        /// Colors <paramref name="pattern"/>. When <paramref name="regex"/> is given, named groups take
        /// their numbers from it so the colors match the match results exactly. When
        /// <paramref name="parseError"/> is given, the offending character (or the end of the pattern)
        /// gets the <c>tok-err</c> class.
        /// </summary>
        public static IReadOnlyList<PatternSegment> Highlight(
            string? pattern,
            bool explicitCapture = false,
            bool ignorePatternWhitespace = false,
            Regex? regex = null,
            RegexParseException? parseError = null)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return parseError is null ? [] : [new PatternSegment(" ", "tok-err tok-err-end")];
            }

            var tokens = Tokenize(pattern, explicitCapture, ignorePatternWhitespace);
            AssignGroupNumbers(tokens, regex);

            var (errorPos, errorAtEnd) = parseError is null ? (-1, false) : ErrorPosition(pattern, parseError);
            var segments = new List<PatternSegment>();
            foreach (var t in tokens)
            {
                var cls = t.Group is { } g
                    ? (g.Capture && g.Number > 0 ? "gc" + SlotFor(g.Number) : "tok-meta")
                    : t.CssClass;

                if (errorPos >= t.Start && errorPos < t.End)
                {
                    // Split so only the offending character is underlined.
                    Add(segments, pattern[t.Start..errorPos], cls);
                    Add(segments, pattern[errorPos..(errorPos + 1)], Join(cls, "tok-err"));
                    Add(segments, pattern[(errorPos + 1)..t.End], cls);
                }
                else
                {
                    Add(segments, pattern[t.Start..t.End], cls);
                }
            }
            if (errorAtEnd)
            {
                segments.Add(new PatternSegment(" ", "tok-err tok-err-end"));
            }
            return segments;
        }

        /// <summary>
        /// Index of the character to underline for <paramref name="error"/>, or <c>atEnd</c> when the
        /// problem is a missing closing bracket/parenthesis (shown as a marker after the pattern).
        /// <see cref="RegexParseException.Offset"/> points just past the character that failed.
        /// </summary>
        public static (int Position, bool AtEnd) ErrorPosition(string pattern, RegexParseException error)
        {
            ArgumentNullException.ThrowIfNull(pattern);
            ArgumentNullException.ThrowIfNull(error);

            var atEnd = error.Error is RegexParseError.InsufficientClosingParentheses or RegexParseError.UnterminatedBracket
                        || error.Offset <= 0
                        || pattern.Length == 0;
            if (atEnd)
            {
                return (-1, true);
            }
            return (Math.Min(error.Offset, pattern.Length) - 1, false);
        }

        /// <summary>"InsufficientClosingParentheses" → "Insufficient closing parentheses".</summary>
        public static string DescribeParseError(RegexParseError error)
        {
            var name = error.ToString();
            var sb = new StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (i > 0 && char.IsUpper(c))
                {
                    sb.Append(' ').Append(char.ToLowerInvariant(c));
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static void Add(List<PatternSegment> segments, string text, string? cls)
        {
            if (text.Length == 0)
            {
                return;
            }
            // Merge adjacent plain/same-class runs to keep the DOM small.
            if (segments.Count > 0 && segments[^1].CssClass == cls && cls is null)
            {
                segments[^1] = segments[^1] with { Text = segments[^1].Text + text };
                return;
            }
            segments.Add(new PatternSegment(text, cls));
        }

        private static string? Join(string? a, string b) => string.IsNullOrEmpty(a) ? b : a + " " + b;

        private sealed class GroupInfo
        {
            public bool Capture;
            public string? Name;
            public int Number;
        }

        private sealed record Token(int Start, int End, string? CssClass, GroupInfo? Group = null);

        private static List<Token> Tokenize(string p, bool explicitCapture, bool ignoreWhitespace)
        {
            var tokens = new List<Token>();
            var stack = new Stack<GroupInfo>();
            var x = ignoreWhitespace;
            var n = explicitCapture;
            var i = 0;

            while (i < p.Length)
            {
                var c = p[i];

                if (x && char.IsWhiteSpace(c))
                {
                    tokens.Add(new Token(i, i + 1, null));
                    i++;
                    continue;
                }
                if (x && c == '#')
                {
                    var nl = p.IndexOf('\n', i);
                    var end = nl < 0 ? p.Length : nl;
                    tokens.Add(new Token(i, end, "tok-comment"));
                    i = end;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        i = ReadEscape(p, i, tokens);
                        continue;
                    case '[':
                        i = ReadClass(p, i, tokens);
                        continue;
                    case '(':
                        i = ReadGroupOpen(p, i, tokens, stack, ref n, ref x);
                        continue;
                    case ')':
                        tokens.Add(new Token(i, i + 1, "tok-meta", stack.Count > 0 ? stack.Pop() : null));
                        i++;
                        continue;
                    case '*':
                    case '+':
                    case '?':
                        i = ReadQuantifier(p, i, i + 1, tokens);
                        continue;
                    case '{':
                        var m = Regex.Match(p[i..], @"^\{\d+(,\d*)?\}", RegexOptions.None, TimeSpan.FromSeconds(1));
                        if (m.Success)
                        {
                            i = ReadQuantifier(p, i, i + m.Length, tokens);
                            continue;
                        }
                        break;
                    case '|':
                    case '^':
                    case '$':
                    case '.':
                        tokens.Add(new Token(i, i + 1, "tok-meta"));
                        i++;
                        continue;
                }

                tokens.Add(new Token(i, i + 1, null));
                i++;
            }
            return tokens;
        }

        private static int ReadQuantifier(string p, int start, int end, List<Token> tokens)
        {
            if (end < p.Length && p[end] == '?')
            {
                end++; // lazy
            }
            tokens.Add(new Token(start, end, "tok-quant"));
            return end;
        }

        private static int ReadEscape(string p, int i, List<Token> tokens)
        {
            if (i + 1 >= p.Length)
            {
                tokens.Add(new Token(i, p.Length, "tok-esc"));
                return p.Length;
            }

            var d = p[i + 1];
            var end = i + 2;
            switch (d)
            {
                case 'p' or 'P' when end < p.Length && p[end] == '{':
                    end = CloseAt(p, end, '}');
                    break;
                case 'k' when end < p.Length && (p[end] == '<' || p[end] == '\''):
                    end = CloseAt(p, end, p[end] == '<' ? '>' : '\'');
                    break;
                case 'u':
                    end = Math.Min(p.Length, i + 6);
                    break;
                case 'x':
                    end = Math.Min(p.Length, i + 4);
                    break;
                case 'c':
                    end = Math.Min(p.Length, i + 3);
                    break;
            }
            tokens.Add(new Token(i, end, "tok-esc"));
            return end;
        }

        // Index just past the first `close` after `from`, or the end of the pattern.
        private static int CloseAt(string p, int from, char close)
        {
            var at = p.IndexOf(close, from + 1);
            return at < 0 ? p.Length : at + 1;
        }

        private static int ReadClass(string p, int i, List<Token> tokens)
        {
            // Handles escapes and .NET character class subtraction: [a-z-[aeiou]].
            var j = i + 1;
            var depth = 1;
            if (j < p.Length && p[j] == '^') j++;
            if (j < p.Length && p[j] == ']') j++; // a leading ] is literal
            while (j < p.Length && depth > 0)
            {
                var c = p[j];
                if (c == '\\')
                {
                    j += 2;
                    continue;
                }
                if (c == '-' && j + 1 < p.Length && p[j + 1] == '[')
                {
                    depth++;
                    j += 2;
                    if (j < p.Length && p[j] == '^') j++;
                    continue;
                }
                if (c == ']')
                {
                    depth--;
                }
                j++;
            }
            var end = Math.Min(j, p.Length);
            tokens.Add(new Token(i, end, "tok-class"));
            return end;
        }

        private static int ReadGroupOpen(string p, int i, List<Token> tokens, Stack<GroupInfo> stack, ref bool explicitCapture, ref bool ignoreWhitespace)
        {
            var rest = p.AsSpan(i);
            var g = new GroupInfo();
            int len;

            if (rest.StartsWith("(?#"))
            {
                var close = p.IndexOf(')', i);
                var end = close < 0 ? p.Length : close + 1;
                tokens.Add(new Token(i, end, "tok-comment"));
                return end;
            }

            var named = Regex.Match(p[i..], @"^\(\?(?:<(?<name>\w*)(?:-\w+)?>|'(?<name>\w*)(?:-\w+)?')", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (named.Success && !rest.StartsWith("(?<=") && !rest.StartsWith("(?<!"))
            {
                // (?<name>...), (?'name'...), balancing (?<a-b>...) captures as "a"; (?<-b>...) captures nothing.
                var name = named.Groups["name"].Value;
                g.Capture = name.Length > 0;
                g.Name = name.Length > 0 ? name : null;
                len = named.Length;
            }
            else if (rest.StartsWith("(?<=") || rest.StartsWith("(?<!"))
            {
                len = 4;
            }
            else if (rest.StartsWith("(?=") || rest.StartsWith("(?!") || rest.StartsWith("(?:") || rest.StartsWith("(?>"))
            {
                len = 3;
            }
            else if (rest.StartsWith("(?("))
            {
                // Conditional (?(name)yes|no) or (?(expr)yes|no): the test is not a capture group.
                len = CloseAt(p, i + 2, ')') - i;
            }
            else if (Regex.Match(p[i..], @"^\(\?([imnsx]*)(?:-([imnsx]*))?(\)|:)", RegexOptions.None, TimeSpan.FromSeconds(1)) is { Success: true } opt
                     && (opt.Groups[1].Length > 0 || opt.Groups[2].Length > 0))
            {
                var on = opt.Groups[1].Value;
                var off = opt.Groups[2].Value;
                if (on.Contains('n')) explicitCapture = true;
                if (off.Contains('n')) explicitCapture = false;
                if (on.Contains('x')) ignoreWhitespace = true;
                if (off.Contains('x')) ignoreWhitespace = false;
                if (opt.Groups[3].Value == ")")
                {
                    tokens.Add(new Token(i, i + opt.Length, "tok-meta"));
                    return i + opt.Length;
                }
                len = opt.Length;
            }
            else if (rest.StartsWith("(?"))
            {
                len = 2; // invalid construct; the parser reports it
            }
            else
            {
                g.Capture = !explicitCapture;
                len = 1;
            }

            tokens.Add(new Token(i, i + len, "tok-meta", g));
            stack.Push(g);
            return i + len;
        }

        private static void AssignGroupNumbers(List<Token> tokens, Regex? regex)
        {
            var groups = tokens.Select(t => t.Group).Where(g => g is { Capture: true }).Distinct().Cast<GroupInfo>().ToList();

            // .NET numbers unnamed groups first (left to right), then named groups in order of
            // first appearance; a name that is a number uses that number.
            var next = 1;
            foreach (var g in groups.Where(g => g.Name is null))
            {
                g.Number = next++;
            }

            var byName = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var g in groups.Where(g => g.Name is not null))
            {
                var name = g.Name!;
                if (regex is not null)
                {
                    g.Number = regex.GroupNumberFromName(name);
                    continue;
                }
                if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var explicitNumber))
                {
                    g.Number = explicitNumber;
                    continue;
                }
                if (!byName.TryGetValue(name, out var number))
                {
                    number = next++;
                    byName[name] = number;
                }
                g.Number = number;
            }
        }
    }
}
