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
            var start = Stopwatch.GetTimestamp();
            var matches = tests.ToDictionary(input => input, input =>
            {
                // MatchCollection is lazy: reading Count forces the whole scan here, so the
                // elapsed time covers the real matching and RegexMatchTimeoutException
                // surfaces from this method rather than later during rendering.
                var collection = testRegex.Matches(input);
                _ = collection.Count;
                return collection;
            });
            var elapsed = Stopwatch.GetElapsedTime(start);
            return ((long)elapsed.TotalMilliseconds, matches);
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

        public IDictionary<string, string> GetReplacedStrings(IEnumerable<string> tests, Regex testRegex, string replacement)
        {
            return tests.ToDictionary(
                input => input,
                input => testRegex.Replace(input, replacement)
            );
        }
    }
}
