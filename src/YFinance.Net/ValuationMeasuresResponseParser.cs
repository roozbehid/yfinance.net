using System.Net;
using System.Text.RegularExpressions;

namespace YFinance.Net;

internal static partial class ValuationMeasuresResponseParser
{
    public static ValuationMeasures Parse(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var tableMatch = TableRegex().Match(html);
        if (!tableMatch.Success)
        {
            return new ValuationMeasures([], []);
        }

        var rowMatches = RowRegex().Matches(tableMatch.Groups[1].Value);
        if (rowMatches.Count == 0)
        {
            return new ValuationMeasures([], []);
        }

        var headerCells = ParseCells(rowMatches[0].Groups[1].Value);
        if (headerCells.Count < 2)
        {
            return new ValuationMeasures([], []);
        }

        var columns = headerCells.Skip(1).ToArray();
        var rows = new List<ValuationMeasureRow>();

        for (var rowIndex = 1; rowIndex < rowMatches.Count; rowIndex++)
        {
            var cells = ParseCells(rowMatches[rowIndex].Groups[1].Value);
            if (cells.Count == 0 || string.IsNullOrWhiteSpace(cells[0]))
            {
                continue;
            }

            var values = new string?[columns.Length];
            for (var valueIndex = 0; valueIndex < columns.Length; valueIndex++)
            {
                var cellIndex = valueIndex + 1;
                values[valueIndex] = cellIndex < cells.Count ? cells[cellIndex] : null;
            }

            rows.Add(new ValuationMeasureRow(cells[0], values));
        }

        return new ValuationMeasures(columns, rows.ToArray());
    }

    private static List<string> ParseCells(string rowHtml)
    {
        var matches = CellRegex().Matches(rowHtml);
        var cells = new List<string>(matches.Count);

        foreach (Match match in matches)
        {
            var withoutTags = TagRegex().Replace(match.Groups[1].Value, string.Empty);
            var decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00A0', ' ').Trim();
            cells.Add(decoded);
        }

        return cells;
    }

    [GeneratedRegex("<table\\b[^>]*>(.*?)</table>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TableRegex();

    [GeneratedRegex("<tr\\b[^>]*>(.*?)</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RowRegex();

    [GeneratedRegex("<t[hd]\\b[^>]*>(.*?)</t[hd]>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CellRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();
}