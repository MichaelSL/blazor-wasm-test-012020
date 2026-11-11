#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace BlazorWasmRegex.Shared.Services;

/// <summary>A sample pattern with test lines; named groups show off the capture-group colors.</summary>
public sealed record RegexSample(string Key, string Label, string Pattern, string TestData);

public static class SampleRegexLibrary
{
    public static IReadOnlyList<RegexSample> All { get; } =
    [
        new("Email", "Email",
            @"(?<user>[\w.+-]+)@(?<domain>[\w-]+\.[\w.]+)",
            """
            john.doe@example.com
            contact: support@my-site.org, sales@my-site.org
            not an email
            a@b
            """),
        new("Phone", "Phone",
            @"(?<country>\+\d{1,3})?[\s.-]?\(?(?<area>\d{3})\)?[\s.-]?(?<exchange>\d{3})[\s.-]?(?<line>\d{4})",
            """
            +1 (555) 123-4567
            Office: 555.987.6543 ext 12
            call 555-0100 now
            12345
            """),
        new("Url", "URL",
            @"(?<scheme>https?)://(?<host>[\w.-]+)(?<path>/[^\s?#]*)?(?:\?(?<query>[^\s#]*))?",
            """
            https://dotnet-regex.com/about
            Visit http://example.org/docs?page=2 today
            ftp://files.example.com
            not a url
            """),
    ];

    public static RegexSample? Find(string? key) => All.FirstOrDefault(s => s.Key == key);
}
