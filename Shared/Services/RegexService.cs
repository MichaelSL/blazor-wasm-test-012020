// Sample Regex Library
public static class SampleRegexLibrary {
    public static string EmailPattern => "^[^\s@]+@[^\s@]+\.[^\s@]+$";
    public static string PhonePattern => "^\+?[1-9]\d{1,14}$";
    public static string UrlPattern => "^(https?|ftp)://[^\s/$.?#].[^\s]*$";
    // Add more patterns as needed
}