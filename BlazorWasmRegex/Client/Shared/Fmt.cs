namespace BlazorWasmRegex.Client.Shared;

internal static class Fmt
{
    public static string Plural(int count, string one, string many) => $"{count} {(count == 1 ? one : many)}";
}
