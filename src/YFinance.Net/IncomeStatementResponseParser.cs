using System.Globalization;
using System.Text.Json;

namespace YFinance.Net;

internal static class IncomeStatementResponseParser
{
    public static IncomeStatement Parse(ReadOnlyMemory<byte> json, string requestedSymbol, FinancialStatementFrequency frequency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedSymbol);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("timeseries", out var timeseries) || timeseries.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException("Yahoo Finance fundamentals-timeseries response did not contain a timeseries object.");
        }

        if (timeseries.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Yahoo Finance fundamentals-timeseries endpoint returned an error: {error}");
        }

        if (!timeseries.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array)
        {
            return new IncomeStatement(requestedSymbol, frequency, [], []);
        }

        var prefix = frequency.ToWirePrefix();
        var symbol = requestedSymbol;
        var rawRows = new List<RawIncomeStatementLineItem>();

        foreach (var resultItem in resultArray.EnumerateArray())
        {
            if (resultItem.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var parsedRow = ParseResultItem(resultItem, prefix);
            if (parsedRow is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(parsedRow.Value.Symbol))
            {
                symbol = parsedRow.Value.Symbol!;
            }

            rawRows.Add(parsedRow.Value.LineItem);
        }

        if (rawRows.Count == 0)
        {
            return new IncomeStatement(symbol, frequency, [], []);
        }

        var periodTypesByDate = new Dictionary<DateOnly, string?>();
        foreach (var row in rawRows)
        {
            foreach (var point in row.Values)
            {
                if (!periodTypesByDate.ContainsKey(point.AsOfDate))
                {
                    periodTypesByDate[point.AsOfDate] = point.PeriodType;
                }
            }
        }

        var periods = periodTypesByDate
            .OrderByDescending(pair => pair.Key)
            .Select(pair => new FinancialStatementPeriod(pair.Key, pair.Value))
            .ToArray();

        var periodIndex = new Dictionary<DateOnly, int>(periods.Length);
        for (var index = 0; index < periods.Length; index++)
        {
            periodIndex[periods[index].AsOfDate] = index;
        }

        var lineItems = new IncomeStatementLineItem[rawRows.Count];
        for (var rowIndex = 0; rowIndex < rawRows.Count; rowIndex++)
        {
            var rawRow = rawRows[rowIndex];
            var values = new decimal?[periods.Length];
            foreach (var point in rawRow.Values)
            {
                if (periodIndex.TryGetValue(point.AsOfDate, out var index))
                {
                    values[index] = point.Value;
                }
            }

            lineItems[rowIndex] = new IncomeStatementLineItem(rawRow.Key, rawRow.CurrencyCode, values);
        }

        return new IncomeStatement(symbol, frequency, periods, lineItems);
    }

    private static ParsedIncomeStatementRow? ParseResultItem(JsonElement resultItem, string prefix)
    {
        string? symbol = null;
        string? type = null;
        string? propertyName = null;
        List<RawIncomeStatementValue>? values = null;

        foreach (var property in resultItem.EnumerateObject())
        {
            if (property.NameEquals("meta"))
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    (symbol, type) = ParseMeta(property.Value);
                }

                continue;
            }

            if (property.NameEquals("timestamp"))
            {
                continue;
            }

            if (!property.Name.StartsWith(prefix, StringComparison.Ordinal) || property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            propertyName = property.Name;
            values = ParseValues(property.Value);
        }

        var wireKey = type ?? propertyName;
        if (string.IsNullOrWhiteSpace(wireKey) || values is null)
        {
            return null;
        }

        var key = wireKey.StartsWith(prefix, StringComparison.Ordinal)
            ? wireKey[prefix.Length..]
            : wireKey;

        return new ParsedIncomeStatementRow(symbol, new RawIncomeStatementLineItem(key, values));
    }

    private static (string? Symbol, string? Type) ParseMeta(JsonElement meta)
    {
        string? symbol = null;
        string? type = null;

        if (meta.TryGetProperty("symbol", out var symbolArray) && symbolArray.ValueKind == JsonValueKind.Array && symbolArray.GetArrayLength() > 0)
        {
            var first = symbolArray[0];
            if (first.ValueKind == JsonValueKind.String)
            {
                symbol = first.GetString();
            }
        }

        if (meta.TryGetProperty("type", out var typeArray) && typeArray.ValueKind == JsonValueKind.Array && typeArray.GetArrayLength() > 0)
        {
            var first = typeArray[0];
            if (first.ValueKind == JsonValueKind.String)
            {
                type = first.GetString();
            }
        }

        return (symbol, type);
    }

    private static List<RawIncomeStatementValue> ParseValues(JsonElement valuesArray)
    {
        var values = new List<RawIncomeStatementValue>();

        foreach (var item in valuesArray.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!item.TryGetProperty("asOfDate", out var asOfDateElement) || asOfDateElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var asOfDateText = asOfDateElement.GetString();
            if (!DateOnly.TryParse(asOfDateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var asOfDate))
            {
                continue;
            }

            var periodType = item.TryGetProperty("periodType", out var periodTypeElement) && periodTypeElement.ValueKind == JsonValueKind.String
                ? periodTypeElement.GetString()
                : null;

            var currencyCode = item.TryGetProperty("currencyCode", out var currencyElement) && currencyElement.ValueKind == JsonValueKind.String
                ? currencyElement.GetString()
                : null;

            decimal? value = null;
            if (item.TryGetProperty("reportedValue", out var reportedValueElement) && reportedValueElement.ValueKind == JsonValueKind.Object)
            {
                value = ReadRawValue(reportedValueElement);
            }

            values.Add(new RawIncomeStatementValue(asOfDate, periodType, currencyCode, value));
        }

        return values;
    }

    private static decimal? ReadRawValue(JsonElement reportedValueElement)
    {
        if (!reportedValueElement.TryGetProperty("raw", out var rawElement))
        {
            return null;
        }

        return rawElement.ValueKind switch
        {
            JsonValueKind.Number when rawElement.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number => Convert.ToDecimal(rawElement.GetDouble(), CultureInfo.InvariantCulture),
            _ => null
        };
    }

    private readonly record struct ParsedIncomeStatementRow(string? Symbol, RawIncomeStatementLineItem LineItem);

    private readonly record struct RawIncomeStatementLineItem(string Key, List<RawIncomeStatementValue> Values)
    {
        public string? CurrencyCode => Values.FirstOrDefault().CurrencyCode;
    }

    private readonly record struct RawIncomeStatementValue(DateOnly AsOfDate, string? PeriodType, string? CurrencyCode, decimal? Value);
}