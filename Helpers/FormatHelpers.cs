using System.Text;
using System.Text.RegularExpressions;

namespace ServicePlatform.Helpers;

public static class ValidationHelper
{
    public const string MobileRegex = @"^[6-9]\d{9}$";
    public const string MobileErrorMessage = "Mobile number must be 10 digits and start with 6, 7, 8, or 9.";
}

public static class CsvHelper
{
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";

        // CSV Injection prevention: escape leading =, +, -, @
        if (value.StartsWith("=") || value.StartsWith("+") || value.StartsWith("-") || value.StartsWith("@"))
        {
            value = "'" + value;
        }

        // Escape quotes and wrap in quotes
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    public static string BuildCsvRow(params string?[] values)
    {
        return string.Join(",", values.Select(Escape));
    }
}
