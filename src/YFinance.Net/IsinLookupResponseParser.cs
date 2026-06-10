using System.Text.RegularExpressions;

namespace YFinance.Net;

internal static partial class IsinLookupResponseParser
{
    public static bool IsValidIsin(string value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.Length == 12
            && IsinRegex().IsMatch(value);
    }

    public static string? ParseForSymbol(string responseText, string symbol)
    {
        ArgumentNullException.ThrowIfNull(responseText);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        foreach (Match match in SuggestRowRegex().Matches(responseText))
        {
            var keywords = match.Groups["keywords"].Value;
            if (string.IsNullOrWhiteSpace(keywords))
            {
                continue;
            }

            var parts = keywords.Split('|');
            if (parts.Length < 2)
            {
                continue;
            }

            var candidateSymbol = parts[0];
            var candidateIsin = parts[1];

            if (!string.Equals(candidateSymbol, symbol, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (LooksLikeIsin(candidateIsin))
            {
                return candidateIsin;
            }
        }

        return null;
    }

    private static bool LooksLikeIsin(string value)
    {
        return IsValidIsin(value);
    }

    [GeneratedRegex("new Array\\(\"(?<name>[^\"]*)\",\\s*\"(?<category>[^\"]*)\",\\s*\"(?<keywords>[^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex SuggestRowRegex();

    [GeneratedRegex("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.IgnoreCase)]
    private static partial Regex IsinRegex();
}