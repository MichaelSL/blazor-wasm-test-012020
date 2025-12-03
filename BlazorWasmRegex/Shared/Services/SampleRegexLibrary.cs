namespace BlazorWasmRegex.Shared.Services;

public static class SampleRegexLibrary
{
    public static string EmailPattern => "^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$";
    public static string PhonePattern => "^\\+?[1-9]\\d{1,14}$";
    public static string UrlPattern => "^(https?|ftp)://[^\\s/$.?#].[^\\s]*$";

    public static string EmailSampleData => @"user@example.com
john.doe@company.org
invalid-email
test@test.co.uk
not an email address";

    public static string PhoneSampleData => @"+14155551234
+441234567890
12345
+919876543210
abc123";

    public static string UrlSampleData => @"https://www.example.com
http://api.service.io/path?query=1
ftp://files.server.net/download
not-a-url
https://github.com/user/repo";
}
