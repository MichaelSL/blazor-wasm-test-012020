using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegex.Shared.Interfaces
{
    public interface IRegexService
    {
        (long, IDictionary<string, MatchCollection>) GetMatches(IEnumerable<string> tests, Regex testRegex);
        IEnumerable<string> GetMatchedStrings(IEnumerable<string> tests, Regex testRegex, Func<Match, string> highlighter = null);
        IEnumerable<string[]> GetSplitList(IEnumerable<string> tests, Regex testRegex);
        IDictionary<string, string> GetReplacedStrings(IEnumerable<string> tests, Regex testRegex, string replacement);
    }
}
