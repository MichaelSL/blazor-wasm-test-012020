using BlazorWasmRegexTest.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegexTest.Shared.Services
{
    public class HtmlHelperService : IHtmlHelperService
    {
        public string GetDelimeteredString(string[] input, string delimeter)
        {
            return $"{string.Join(delimeter, input.Select(WebUtility.HtmlEncode))}";
        }

        public string GetMarkedSpans(string input, MatchCollection matches, string className)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                if (m.Success)
                {
                    var indexAdjustment = ($"<span class='{className}'>".Length + "</span>".Length) * i;
                
                    input = 
                        (i == 0 ? WebUtility.HtmlEncode(input.Substring(0, m.Index + indexAdjustment)) : input.Substring(0, m.Index + indexAdjustment)) 
                        + $"<span class='{className}'>" 
                        + WebUtility.HtmlEncode(input.Substring(m.Index + indexAdjustment, m.Length)) 
                        + "</span>" 
                        + WebUtility.HtmlEncode(input.Substring(m.Index + m.Length + indexAdjustment));
                }
            }
            return input;
        }
    }
}
