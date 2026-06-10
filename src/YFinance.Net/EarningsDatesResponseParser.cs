using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class EarningsDatesResponseParser
{
    public static EarningsDatesResult Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance earnings dates response did not contain a root object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance earnings dates response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("finance"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance earnings dates response did not contain a finance object.");
                }

                return ParseFinanceObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance earnings dates response did not contain a finance object.");
    }

    private static EarningsDatesResult ParseFinanceObject(ref Utf8JsonReader reader)
    {
        EarningsDatesResult? result = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance earnings dates response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("error"u8))
            {
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Yahoo Finance earnings dates endpoint returned no result.");
                }

                if (reader.TokenType != JsonTokenType.Null)
                {
                    throw new InvalidOperationException($"Yahoo Finance earnings dates endpoint returned an error: {ReadJsonValueAsString(ref reader)}");
                }

                continue;
            }

            if (reader.ValueTextEquals("result"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    return new EarningsDatesResult(0, []);
                }

                result = ParseResultArray(ref reader);
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        return result ?? new EarningsDatesResult(0, []);
    }

    private static EarningsDatesResult ParseResultArray(ref Utf8JsonReader reader)
    {
        var parsedFirst = false;
        var result = new EarningsDatesResult(0, []);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                SkipCurrentValue(ref reader);
                continue;
            }

            if (!parsedFirst)
            {
                result = ParseResultObject(ref reader);
                parsedFirst = true;
            }
            else
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static EarningsDatesResult ParseResultObject(ref Utf8JsonReader reader)
    {
        var total = 0;
        IReadOnlyList<EarningsDateEntry> entries = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance earnings dates result entry was invalid.");
            }

            if (reader.ValueTextEquals("total"u8))
            {
                if (reader.Read() && reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var parsedTotal))
                {
                    total = parsedTotal;
                }

                continue;
            }

            if (reader.ValueTextEquals("documents"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    continue;
                }

                entries = ParseDocumentsArray(ref reader);
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        return new EarningsDatesResult(total, entries);
    }

    private static IReadOnlyList<EarningsDateEntry> ParseDocumentsArray(ref Utf8JsonReader reader)
    {
        var parsedFirst = false;
        IReadOnlyList<EarningsDateEntry> entries = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                SkipCurrentValue(ref reader);
                continue;
            }

            if (!parsedFirst)
            {
                entries = ParseDocumentObject(ref reader);
                parsedFirst = true;
            }
            else
            {
                reader.Skip();
            }
        }

        return entries;
    }

    private static IReadOnlyList<EarningsDateEntry> ParseDocumentObject(ref Utf8JsonReader reader)
    {
        string[] columnIds = [];
        IReadOnlyList<EarningsDateEntry> entries = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance earnings dates document entry was invalid.");
            }

            if (reader.ValueTextEquals("columns"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    continue;
                }

                columnIds = ParseColumnIds(ref reader);
                continue;
            }

            if (reader.ValueTextEquals("rows"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    continue;
                }

                entries = ParseRowsArray(ref reader, columnIds);
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        return entries;
    }

    private static string[] ParseColumnIds(ref Utf8JsonReader reader)
    {
        var ids = new List<string>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                SkipCurrentValue(ref reader);
                continue;
            }

            string? id = null;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance earnings dates column entry was invalid.");
                }

                var isId = reader.ValueTextEquals("id"u8);
                if (!reader.Read())
                {
                    break;
                }

                if (isId)
                {
                    id = ReadNullableString(ref reader);
                }
                else
                {
                    SkipCurrentValue(ref reader);
                }
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                ids.Add(id);
            }
        }

        return ids.ToArray();
    }

    private static IReadOnlyList<EarningsDateEntry> ParseRowsArray(ref Utf8JsonReader reader, IReadOnlyList<string> columnIds)
    {
        var entries = new List<EarningsDateEntry>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                SkipCurrentValue(ref reader);
                continue;
            }

            var entry = ParseRowArray(ref reader, columnIds);
            entries.Add(entry);
        }

        return entries.ToArray();
    }

    private static EarningsDateEntry ParseRowArray(ref Utf8JsonReader reader, IReadOnlyList<string> columnIds)
    {
        DateTimeOffset? earningsDate = null;
        string? timeZoneShortName = null;
        decimal? epsEstimate = null;
        decimal? reportedEps = null;
        decimal? surprisePercent = null;
        string? eventType = null;
        var index = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var columnId = index < columnIds.Count ? columnIds[index] : null;
            switch (columnId)
            {
                case "startdatetime":
                    earningsDate = ReadNullableDateTimeOffset(ref reader);
                    break;
                case "timeZoneShortName":
                    timeZoneShortName = ReadNullableString(ref reader);
                    break;
                case "epsestimate":
                    epsEstimate = ReadNullableDecimal(ref reader);
                    break;
                case "epsactual":
                    reportedEps = ReadNullableDecimal(ref reader);
                    break;
                case "epssurprisepct":
                    surprisePercent = ReadNullableDecimal(ref reader);
                    break;
                case "eventtype":
                    eventType = ReadNullableString(ref reader);
                    break;
                default:
                    SkipCurrentValue(ref reader);
                    break;
            }

            index++;
        }

        return new EarningsDateEntry(earningsDate, timeZoneShortName, epsEstimate, reportedEps, surprisePercent, eventType);
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => ReadScalarText(ref reader),
            JsonTokenType.True => bool.TrueString,
            JsonTokenType.False => bool.FalseString,
            _ => null
        };
    }

    private static decimal? ReadNullableDecimal(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetDecimal(out var number) => number,
            JsonTokenType.String when decimal.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(ref Utf8JsonReader reader)
    {
        var value = ReadNullableString(ref reader);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : null;
    }

    private static string ReadJsonValueAsString(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => "null",
            JsonTokenType.String => reader.GetString() ?? string.Empty,
            JsonTokenType.Number => ReadScalarText(ref reader),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.StartObject or JsonTokenType.StartArray => JsonDocument.ParseValue(ref reader).RootElement.ToString(),
            _ => string.Empty
        };
    }

    private static string ReadScalarText(ref Utf8JsonReader reader)
    {
        if (reader.HasValueSequence)
        {
            return Encoding.UTF8.GetString(reader.ValueSequence.ToArray());
        }

        return Encoding.UTF8.GetString(reader.ValueSpan);
    }

    private static void SkipPropertyValue(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return;
        }

        SkipCurrentValue(ref reader);
    }

    private static void SkipCurrentValue(ref Utf8JsonReader reader)
    {
        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.PropertyName)
        {
            reader.Skip();
        }
    }
}