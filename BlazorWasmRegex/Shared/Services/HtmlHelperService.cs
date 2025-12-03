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

            var result = new StringBuilder();
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    // Add the text before the match (HTML encoded)
                    if (match.Index > lastIndex)
                    {
                        result.Append(WebUtility.HtmlEncode(input.Substring(lastIndex, match.Index - lastIndex)));
                    }

                    // Add the matched text wrapped in a span (content HTML encoded)
                    result.Append($"<span class='{className}'>");
                    result.Append(WebUtility.HtmlEncode(match.Value));
                    result.Append("</span>");

                    lastIndex = match.Index + match.Length;
                }
            }

            // Add any remaining text after the last match (HTML encoded)
            if (lastIndex < input.Length)
            {
                result.Append(WebUtility.HtmlEncode(input.Substring(lastIndex)));
            }

            return result.ToString();
        }
    }
}
