using System.Globalization;
using System.Text.Json;

namespace YFinance.Net;

internal static class CalendarResponseParser
{
    public static CalendarResult<EarningsCalendarEntry> ParseEarnings(string json)
    {
        return Parse(
            json,
            "earnings calendar",
            row =>
            {
                var symbol = row.GetString("ticker");
                return string.IsNullOrWhiteSpace(symbol)
                    ? null
                    : new EarningsCalendarEntry(
                        symbol,
                        row.GetString("companyshortname"),
                        row.GetDecimal("intradaymarketcap"),
                        row.GetString("eventname"),
                        row.GetDateTimeOffset("startdatetime"),
                        row.GetString("startdatetimetype"),
                        row.GetDecimal("epsestimate"),
                        row.GetDecimal("epsactual"),
                        row.GetDecimal("epssurprisepct"));
            });
    }

    public static CalendarResult<IpoCalendarEntry> ParseIpos(string json)
    {
        return Parse(
            json,
            "IPO calendar",
            row =>
            {
                var symbol = row.GetString("ticker");
                return string.IsNullOrWhiteSpace(symbol)
                    ? null
                    : new IpoCalendarEntry(
                        symbol,
                        row.GetString("companyshortname"),
                        row.GetString("exchange_short_name"),
                        row.GetDateTimeOffset("filingdate"),
                        row.GetDateTimeOffset("startdatetime"),
                        row.GetDateTimeOffset("amendeddate"),
                        row.GetDecimal("pricefrom"),
                        row.GetDecimal("priceto"),
                        row.GetDecimal("offerprice"),
                        row.GetString("currencyname"),
                        row.GetDecimal("shares"),
                        row.GetString("dealtype"));
            });
    }

    public static CalendarResult<EconomicEventCalendarEntry> ParseEconomicEvents(string json)
    {
        return Parse(
            json,
            "economic events calendar",
            row =>
            {
                var eventName = row.GetString("econ_release");
                return string.IsNullOrWhiteSpace(eventName)
                    ? null
                    : new EconomicEventCalendarEntry(
                        eventName,
                        row.GetString("country_code"),
                        row.GetDateTimeOffset("startdatetime"),
                        row.GetString("period"),
                        row.GetString("after_release_actual"),
                        row.GetString("consensus_estimate"),
                        row.GetString("prior_release_actual"),
                        row.GetString("originally_reported_actual"));
            });
    }

    public static CalendarResult<SplitCalendarEntry> ParseSplits(string json)
    {
        return Parse(
            json,
            "splits calendar",
            row =>
            {
                var symbol = row.GetString("ticker");
                return string.IsNullOrWhiteSpace(symbol)
                    ? null
                    : new SplitCalendarEntry(
                        symbol,
                        row.GetString("companyshortname"),
                        row.GetDateTimeOffset("startdatetime"),
                        row.GetBoolean("optionable"),
                        row.GetDecimal("old_share_worth"),
                        row.GetDecimal("share_worth"));
            });
    }

    private static CalendarResult<TEntry> Parse<TEntry>(string json, string endpointName, Func<CalendarRowReader, TEntry?> rowParser)
        where TEntry : class
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (!root.TryGetProperty("finance", out var finance) || finance.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"Yahoo Finance {endpointName} response did not contain a finance object.");
        }

        if (finance.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null && error.ValueKind != JsonValueKind.Undefined)
        {
            throw new InvalidOperationException($"Yahoo Finance {endpointName} endpoint returned an error: {error}");
        }

        if (!finance.TryGetProperty("result", out var resultArray) || resultArray.ValueKind != JsonValueKind.Array || resultArray.GetArrayLength() == 0)
        {
            return new CalendarResult<TEntry>(0, []);
        }

        var firstResult = resultArray[0];
        var total = GetInt32(firstResult, "total") ?? 0;

        if (!firstResult.TryGetProperty("documents", out var documentsArray) || documentsArray.ValueKind != JsonValueKind.Array || documentsArray.GetArrayLength() == 0)
        {
            return new CalendarResult<TEntry>(total, []);
        }

        var firstDocument = documentsArray[0];
        if (!firstDocument.TryGetProperty("columns", out var columnsArray) || columnsArray.ValueKind != JsonValueKind.Array)
        {
            return new CalendarResult<TEntry>(total, []);
        }

        var columnIds = new List<string>();
        foreach (var column in columnsArray.EnumerateArray())
        {
            var id = GetString(column, "id");
            if (!string.IsNullOrWhiteSpace(id))
            {
                columnIds.Add(id);
            }
        }

        if (!firstDocument.TryGetProperty("rows", out var rowsArray) || rowsArray.ValueKind != JsonValueKind.Array)
        {
            return new CalendarResult<TEntry>(total, []);
        }

        var entries = new List<TEntry>();
        foreach (var row in rowsArray.EnumerateArray())
        {
            var reader = CalendarRowReader.Create(columnIds, row);
            var entry = rowParser(reader);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return new CalendarResult<TEntry>(total, entries);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => value.ToString()
        };
    }

    private static int? GetInt32(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private readonly struct CalendarRowReader(IReadOnlyDictionary<string, JsonElement> values)
    {
        private readonly IReadOnlyDictionary<string, JsonElement> _values = values;

        public static CalendarRowReader Create(IReadOnlyList<string> columnIds, JsonElement row)
        {
            var values = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
            if (row.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in row.EnumerateArray())
                {
                    if (index >= columnIds.Count)
                    {
                        break;
                    }

                    values[columnIds[index]] = item.Clone();
                    index++;
                }
            }

            return new CalendarRowReader(values);
        }

        public string? GetString(string columnId)
        {
            if (!_values.TryGetValue(columnId, out var value))
            {
                return null;
            }

            var text = value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => value.GetString(),
                _ => value.ToString()
            };

            return string.IsNullOrWhiteSpace(text) ? null : text;
        }

        public decimal? GetDecimal(string columnId)
        {
            if (!_values.TryGetValue(columnId, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.Number when value.TryGetDecimal(out var number) => number,
                JsonValueKind.String when decimal.TryParse(value.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
                _ => null
            };
        }

        public bool? GetBoolean(string columnId)
        {
            if (!_values.TryGetValue(columnId, out var value))
            {
                return null;
            }

            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        public DateTimeOffset? GetDateTimeOffset(string columnId)
        {
            var value = GetString(columnId);
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                ? parsed
                : null;
        }
    }
}