using BlazorWasmRegex.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegex.Shared.Services
{
    public class HtmlHelperService : IHtmlHelperService
    {
        public string GetDelimeteredString(string[] input, string delimeter)
        {
            return $"{string.Join(delimeter, input.Select(WebUtility.HtmlEncode))}";
        }

        public string GetMarkedSpans(string input, MatchCollection matches, string className)
        {
            if (matches.Count == 0)
            {
                return WebUtility.HtmlEncode(input);
            }

            // Pre-size the StringBuilder to reduce reallocations
            var result = new StringBuilder(input.Length + matches.Count * 50);
            ReadOnlySpan<char> inputSpan = input.AsSpan();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    // Add the text before the match (HTML encoded)
                    if (match.Index > lastIndex)
                    {
                        var beforeMatch = inputSpan.Slice(lastIndex, match.Index - lastIndex);
                        result.Append(WebUtility.HtmlEncode(beforeMatch.ToString()));
                    }

                    // Add the matched text wrapped in a span (content HTML encoded)
                    result.Append($"<span class='{className}'>");
                    var matchText = inputSpan.Slice(match.Index, match.Length);
                    result.Append(WebUtility.HtmlEncode(matchText.ToString()));
                    result.Append("</span>");

                    lastIndex = match.Index + match.Length;
                }
            }

            // Add any remaining text after the last match (HTML encoded)
            if (lastIndex < input.Length)
            {
                var remaining = inputSpan.Slice(lastIndex);
                result.Append(WebUtility.HtmlEncode(remaining.ToString()));
            }

            return result.ToString();
        }
    }
}
