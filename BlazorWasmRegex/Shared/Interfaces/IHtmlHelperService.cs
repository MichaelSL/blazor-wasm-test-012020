using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegex.Shared.Interfaces
{
    public interface IHtmlHelperService
    {
        string GetMarkedSpans(string input, MatchCollection matches, string className);
        string GetDelimeteredString(string[] input, string delimeter);
    }
}
