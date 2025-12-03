using BlazorWasmRegex.Shared.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BlazorWasmRegex.Shared.Services
{
    public class RegexService : IRegexService
    {
        public (long, IDictionary<string, MatchCollection>) GetMatches(IEnumerable<string> tests, Regex testRegex)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var matches = tests.ToDictionary(input => input, input => testRegex.Matches(input));
            stopwatch.Stop();
            return (stopwatch.ElapsedMilliseconds, matches);
        }

        public IEnumerable<string> GetMatchedStrings(IEnumerable<string> tests, Regex testRegex, Func<Match, string> highlighter = null)
        {
            var res = tests.Where(item => testRegex.IsMatch(item));
            if (highlighter != null)
            {
                return res
                    .Select(item => testRegex.Replace(item, new MatchEvaluator(highlighter)));
            }
            else
            {
                return res;
            }
        }

        public IEnumerable<string[]> GetSplitList(IEnumerable<string> tests, Regex testRegex)
        {
            return tests
                .Select(item => testRegex.Split(item))
                .Where(splitGroup => splitGroup.Length > 1 && splitGroup.Any(i => !string.IsNullOrEmpty(i)));
        }
    }
}
