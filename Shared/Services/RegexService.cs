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
        public IEnumerable<string> GetMatches(IEnumerable<string> tests, Regex testRegex)
        {
            return tests.Where(item => testRegex.IsMatch(item));
        }

        public IEnumerable<string[]> GetSplitList(IEnumerable<string> tests, Regex testRegex)
        {
            return tests
                .Select(item => testRegex.Split(item))
                .Where(splitGroup => splitGroup.Any(i => !String.IsNullOrEmpty(i)));
        }
    }
}
