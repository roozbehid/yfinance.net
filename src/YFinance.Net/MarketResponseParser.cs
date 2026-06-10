using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class MarketResponseParser
{
    public static MarketSummaryResult ParseSummary(string json, MarketRegion region)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseSummary(Encoding.UTF8.GetBytes(json), region);
    }

    public static MarketSummaryResult ParseSummary(ReadOnlyMemory<byte> json, MarketRegion region)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance market summary response did not contain a marketSummaryResponse object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market summary response did not contain a marketSummaryResponse object.");
            }

            if (reader.ValueTextEquals("marketSummaryResponse"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance market summary response did not contain a marketSummaryResponse object.");
                }

                return ParseMarketSummaryResponseObject(ref reader, region);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance market summary response did not contain a marketSummaryResponse object.");
    }

    public static MarketStatus? ParseStatus(string json, MarketRegion region)
    {
        ArgumentNullException.ThrowIfNull(json);
        return ParseStatus(Encoding.UTF8.GetBytes(json), region);
    }

    public static MarketStatus? ParseStatus(ReadOnlyMemory<byte> json, MarketRegion region)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance market time response did not contain a finance object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market time response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("finance"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance market time response did not contain a finance object.");
                }

                return ParseFinanceObject(ref reader, region);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance market time response did not contain a finance object.");
    }

    private static MarketSummaryResult ParseMarketSummaryResponseObject(ref Utf8JsonReader reader, MarketRegion region)
    {
        string? categoryName = null;
        Dictionary<string, MarketSummaryEntry>? exchanges = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market summary response did not contain a marketSummaryResponse object.");
            }

            var isError = reader.ValueTextEquals("error"u8);
            var isResult = reader.ValueTextEquals("result"u8);
            var isCategoryName = reader.ValueTextEquals("marketCategoryLongName"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isError)
            {
                if (reader.TokenType != JsonTokenType.Null)
                {
                    errorMessage = ReadJsonValueAsString(ref reader);
                }

                continue;
            }

            if (isResult)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance market summary endpoint returned no result.");
                }

                exchanges = ParseMarketSummaryEntries(ref reader);
                continue;
            }

            if (isCategoryName)
            {
                categoryName = ReadNullableString(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance market summary endpoint returned an error: {errorMessage}");
        }

        if (exchanges is null)
        {
            throw new InvalidOperationException("Yahoo Finance market summary endpoint returned no result.");
        }

        return new MarketSummaryResult(region, categoryName, exchanges);
    }

    private static Dictionary<string, MarketSummaryEntry> ParseMarketSummaryEntries(ref Utf8JsonReader reader)
    {
        var exchanges = new Dictionary<string, MarketSummaryEntry>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }

                continue;
            }

            var entry = ParseMarketSummaryEntry(ref reader);
            if (!string.IsNullOrWhiteSpace(entry.Exchange))
            {
                exchanges[entry.Exchange] = entry;
            }
        }

        return exchanges;
    }

    private static MarketSummaryEntry ParseMarketSummaryEntry(ref Utf8JsonReader reader)
    {
        string? exchange = null;
        string? symbol = null;
        string? shortName = null;
        string? fullExchangeName = null;
        string? quoteType = null;
        string? marketState = null;
        string? exchangeTimezoneName = null;
        string? exchangeTimezoneShortName = null;
        decimal? regularMarketPrice = null;
        decimal? regularMarketChange = null;
        decimal? regularMarketChangePercent = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market summary endpoint returned an invalid result row.");
            }

            var isExchange = reader.ValueTextEquals("exchange"u8);
            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isShortName = reader.ValueTextEquals("shortName"u8);
            var isFullExchangeName = reader.ValueTextEquals("fullExchangeName"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            var isMarketState = reader.ValueTextEquals("marketState"u8);
            var isExchangeTimezoneName = reader.ValueTextEquals("exchangeTimezoneName"u8);
            var isExchangeTimezoneShortName = reader.ValueTextEquals("exchangeTimezoneShortName"u8);
            var isRegularMarketPrice = reader.ValueTextEquals("regularMarketPrice"u8);
            var isRegularMarketChange = reader.ValueTextEquals("regularMarketChange"u8);
            var isRegularMarketChangePercent = reader.ValueTextEquals("regularMarketChangePercent"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isExchange)
            {
                exchange = ReadNullableString(ref reader);
            }
            else if (isSymbol)
            {
                symbol = ReadNullableString(ref reader);
            }
            else if (isShortName)
            {
                shortName = ReadNullableString(ref reader);
            }
            else if (isFullExchangeName)
            {
                fullExchangeName = ReadNullableString(ref reader);
            }
            else if (isQuoteType)
            {
                quoteType = ReadNullableString(ref reader);
            }
            else if (isMarketState)
            {
                marketState = ReadNullableString(ref reader);
            }
            else if (isExchangeTimezoneName)
            {
                exchangeTimezoneName = ReadNullableString(ref reader);
            }
            else if (isExchangeTimezoneShortName)
            {
                exchangeTimezoneShortName = ReadNullableString(ref reader);
            }
            else if (isRegularMarketPrice)
            {
                regularMarketPrice = ReadNullableDecimal(ref reader);
            }
            else if (isRegularMarketChange)
            {
                regularMarketChange = ReadNullableDecimal(ref reader);
            }
            else if (isRegularMarketChangePercent)
            {
                regularMarketChangePercent = ReadNullableDecimal(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new MarketSummaryEntry(
            exchange ?? string.Empty,
            symbol,
            shortName,
            fullExchangeName,
            quoteType,
            marketState,
            exchangeTimezoneName,
            exchangeTimezoneShortName,
            regularMarketPrice,
            regularMarketChange,
            regularMarketChangePercent);
    }

    private static MarketStatus? ParseFinanceObject(ref Utf8JsonReader reader, MarketRegion region)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market time response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("marketTimes"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
                }

                return ParseMarketTimesArray(ref reader, region);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
    }

    private static MarketStatus? ParseMarketTimesArray(ref Utf8JsonReader reader, MarketRegion region)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
        }

        MarketStatus? result = null;
        var foundMarketTime = false;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTimes data.");
            }

            if (reader.ValueTextEquals("marketTime"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTime entries.");
                }

                foundMarketTime = true;
                result = ParseMarketTimeArray(ref reader, region);
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        SkipRemainingArrayValues(ref reader);
        if (!foundMarketTime)
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTime entries.");
        }

        return result;
    }

    private static MarketStatus? ParseMarketTimeArray(ref Utf8JsonReader reader, MarketRegion region)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTime entries.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTime entries.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance market time endpoint returned no marketTime entries.");
        }

        var result = ParseMarketTimeItem(ref reader, region);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static MarketStatus? ParseMarketTimeItem(ref Utf8JsonReader reader, MarketRegion region)
    {
        string? id = null;
        string? name = null;
        string? status = null;
        string? message = null;
        string? yfitMarketId = null;
        string? yfitMarketStatus = null;
        DateTimeOffset? open = null;
        DateTimeOffset? close = null;
        string? timezoneName = null;
        string? timezoneShortName = null;
        TimeSpan? utcOffset = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market time endpoint returned an invalid marketTime entry.");
            }

            var isId = reader.ValueTextEquals("id"u8);
            var isName = reader.ValueTextEquals("name"u8);
            var isStatus = reader.ValueTextEquals("status"u8);
            var isMessage = reader.ValueTextEquals("message"u8);
            var isYfitMarketId = reader.ValueTextEquals("yfit_market_id"u8);
            var isYfitMarketStatus = reader.ValueTextEquals("yfit_market_status"u8);
            var isOpen = reader.ValueTextEquals("open"u8);
            var isClose = reader.ValueTextEquals("close"u8);
            var isTimezone = reader.ValueTextEquals("timezone"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isId)
            {
                id = ReadNullableString(ref reader);
            }
            else if (isName)
            {
                name = ReadNullableString(ref reader);
            }
            else if (isStatus)
            {
                status = ReadNullableString(ref reader);
            }
            else if (isMessage)
            {
                message = ReadNullableString(ref reader);
            }
            else if (isYfitMarketId)
            {
                yfitMarketId = ReadNullableString(ref reader);
            }
            else if (isYfitMarketStatus)
            {
                yfitMarketStatus = ReadNullableString(ref reader);
            }
            else if (isOpen)
            {
                open = ReadNullableDateTimeOffset(ref reader);
            }
            else if (isClose)
            {
                close = ReadNullableDateTimeOffset(ref reader);
            }
            else if (isTimezone && reader.TokenType == JsonTokenType.StartArray)
            {
                (timezoneName, timezoneShortName, utcOffset) = ParseTimezoneArray(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new InvalidOperationException("Yahoo Finance market time response did not contain an id.");
        }

        if (region != MarketRegion.US && string.Equals(id, "us", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new MarketStatus(region, id, name, status, message, yfitMarketId, yfitMarketStatus, open, close, timezoneName, timezoneShortName, utcOffset);
    }

    private static (string? Name, string? ShortName, TimeSpan? Offset) ParseTimezoneArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return (null, null, null);
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return (null, null, null);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            SkipRemainingArrayValues(ref reader);
            return (null, null, null);
        }

        string? name = null;
        string? shortName = null;
        TimeSpan? offset = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market time endpoint returned an invalid timezone entry.");
            }

            var isName = reader.ValueTextEquals("$text"u8);
            var isShortName = reader.ValueTextEquals("short"u8);
            var isOffset = reader.ValueTextEquals("gmtoffset"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isName)
            {
                name = ReadNullableString(ref reader);
            }
            else if (isShortName)
            {
                shortName = ReadNullableString(ref reader);
            }
            else if (isOffset)
            {
                offset = ReadUtcOffset(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        SkipRemainingArrayValues(ref reader);
        return (name, shortName, offset);
    }

    private static string? ReadNullableString(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedString(ref reader);
        }

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
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedDecimal(ref reader);
        }

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

    private static TimeSpan? ReadUtcOffset(ref Utf8JsonReader reader)
    {
        var rawOffset = ReadNullableString(ref reader);
        if (!long.TryParse(rawOffset, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return null;
        }

        if (Math.Abs(parsed) > 86_400)
        {
            return TimeSpan.FromMilliseconds(parsed);
        }

        return TimeSpan.FromSeconds(parsed);
    }

    private static string? ReadRawWrappedString(ref Utf8JsonReader reader)
    {
        string? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static decimal? ReadRawWrappedDecimal(ref Utf8JsonReader reader)
    {
        decimal? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance market response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableDecimal(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
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

        if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.PropertyName)
        {
            reader.Skip();
        }
    }

    private static void SkipRemainingArrayValues(ref Utf8JsonReader reader)
    {
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return;
            }

            reader.Skip();
        }
    }
}