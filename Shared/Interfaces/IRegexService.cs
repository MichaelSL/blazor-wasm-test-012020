using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegexTest.Shared.Interfaces
{
    public interface IRegexService
    {
        IEnumerable<MatchCollection> GetMatches(IEnumerable<string> tests, Regex testRegex);
        IEnumerable<string> GetMatchedStrings(IEnumerable<string> tests, Regex testRegex);
        IEnumerable<string[]> GetSplitList(IEnumerable<string> tests, Regex testRegex);
    }
}
