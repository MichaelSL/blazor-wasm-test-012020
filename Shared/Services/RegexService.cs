using BlazorWasmRegexTest.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegexTest.Shared.Services
{
    public class RegexService : IRegexService
    {
        public IDictionary<string, MatchCollection> GetMatches(IEnumerable<string> tests, Regex testRegex)
        {
            return tests.ToDictionary(input => input, input => testRegex.Matches(input));
        }

        public IEnumerable<string> GetMatchedStrings(IEnumerable<string> tests, Regex testRegex)
        {
            return tests.Where(item => testRegex.IsMatch(item));
        }

        public IEnumerable<string[]> GetSplitList(IEnumerable<string> tests, Regex testRegex)
        {
            return tests
                .Select(item => testRegex.Split(item))
                .Where(splitGroup => splitGroup.Any(i => !string.IsNullOrEmpty(i)));
        }
    }
}
