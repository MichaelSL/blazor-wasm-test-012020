namespace BlazorWasmRegex.Shared.Models
{
    /// <summary>
    /// The regex options chosen in the UI. Persisted as JSON under the <c>REGEX_OPTIONS</c>
    /// local-storage key. Every flag added after the original three has a <c>false</c> default
    /// so JSON written by older versions (only IgnoreCase/Multiline/Singleline) still deserializes.
    /// </summary>
    public record RegexOptionsData(
        bool IgnoreCase,
        bool Multiline,
        bool Singleline,
        bool AnyNewLine = false,
        bool NonBacktracking = false,
        bool ExplicitCapture = false,
        bool IgnorePatternWhitespace = false,
        bool RightToLeft = false,
        bool CultureInvariant = false,
        bool ECMAScript = false)
    {
        public static RegexOptionsData Default { get; } = new(false, false, false);
    }
}
