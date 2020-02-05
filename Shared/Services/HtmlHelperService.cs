using BlazorWasmRegexTest.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegexTest.Shared.Services
{
    public class HtmlHelperService : IHtmlHelperService
    {
        public string GetMarkedSpans(string input, MatchCollection matches, string className)
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var m = matches[i];
                var indexAdjustment = ($"<span class='{className}'>".Length + "</span>".Length) * i;
                if (m.Success)
                {
                    input = input.Substring(0, m.Index + indexAdjustment) + $"<span class='{className}'>" + input.Substring(m.Index + indexAdjustment, m.Length) + "</span>" + input.Substring(m.Index + m.Length + indexAdjustment);
                }
            }
            return input;
        }
    }
}
