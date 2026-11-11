namespace BlazorWasmRegex.Shared.Services
{
    /// <summary>
    /// Formats an assembly informational version ("2026.11.10.57+a1b2c3d...") for display.
    /// </summary>
    public static class AppVersionFormatter
    {
        public const int ShortHashLength = 7;

        public static string Format(string informationalVersion)
        {
            if (string.IsNullOrWhiteSpace(informationalVersion))
            {
                return string.Empty;
            }

            var plus = informationalVersion.IndexOf('+');
            if (plus < 0)
            {
                return informationalVersion;
            }

            var version = informationalVersion[..plus];
            var revision = informationalVersion[(plus + 1)..];
            if (revision.Length == 0)
            {
                return version;
            }

            return revision.Length > ShortHashLength
                ? $"{version}+{revision[..ShortHashLength]}"
                : informationalVersion;
        }
    }
}
