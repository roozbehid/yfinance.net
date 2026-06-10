using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class PredefinedScreenerResponseParser
{
    public static ScreenerResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static ScreenerResult Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance screener response did not contain a finance object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance screener response did not contain a finance object.");
            }

            if (reader.ValueTextEquals("finance"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance screener response did not contain a finance object.");
                }

                return ParseFinanceObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance screener response did not contain a finance object.");
    }

    private static ScreenerResult ParseFinanceObject(ref Utf8JsonReader reader)
    {
        ScreenerResult? result = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance screener response did not contain a finance object.");
            }

            var isError = reader.ValueTextEquals("error"u8);
            var isResult = reader.ValueTextEquals("result"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isError)
            {
                if (reader.TokenType != JsonTokenType.Null)
                {
                    throw new InvalidOperationException($"Yahoo Finance screener endpoint returned an error: {ReadJsonValueAsString(ref reader)}");
                }

                continue;
            }

            if (isResult)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance screener endpoint returned no result.");
                }

                result = ParseResultArray(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return result ?? throw new InvalidOperationException("Yahoo Finance screener endpoint returned no result.");
    }

    private static ScreenerResult? ParseResultArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            SkipRemainingArrayValues(ref reader);
            return null;
        }

        var result = ParseResultObject(ref reader);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static ScreenerResult ParseResultObject(ref Utf8JsonReader reader)
    {
        string? id = null;
        string? title = null;
        string? description = null;
        string? canonicalName = null;
        var start = 0;
        var count = 0;
        var total = 0;
        var isPremium = false;
        string? iconUrl = null;
        var criteria = default(ScreenerCriteria);
        var quotes = Array.Empty<ScreenerQuote>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance screener result contained an invalid result item.");
            }

            var isId = reader.ValueTextEquals("id"u8);
            var isTitle = reader.ValueTextEquals("title"u8);
            var isDescription = reader.ValueTextEquals("description"u8);
            var isCanonicalName = reader.ValueTextEquals("canonicalName"u8);
            var isCriteriaMeta = reader.ValueTextEquals("criteriaMeta"u8);
            var isStart = reader.ValueTextEquals("start"u8);
            var isCount = reader.ValueTextEquals("count"u8);
            var isTotal = reader.ValueTextEquals("total"u8);
            var isQuotes = reader.ValueTextEquals("quotes"u8);
            var isPremiumProperty = reader.ValueTextEquals("isPremium"u8);
            var isIconUrl = reader.ValueTextEquals("iconUrl"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isId)
            {
                id = ReadNullableString(ref reader);
            }
            else if (isTitle)
            {
                title = ReadNullableString(ref reader);
            }
            else if (isDescription)
            {
                description = ReadNullableString(ref reader);
            }
            else if (isCanonicalName)
            {
                canonicalName = ReadNullableString(ref reader);
            }
            else if (isCriteriaMeta && reader.TokenType == JsonTokenType.StartObject)
            {
                criteria = ParseCriteriaMeta(ref reader);
            }
            else if (isStart)
            {
                start = ReadNullableInt32(ref reader) ?? 0;
            }
            else if (isCount)
            {
                count = ReadNullableInt32(ref reader) ?? 0;
            }
            else if (isTotal)
            {
                total = ReadNullableInt32(ref reader) ?? 0;
            }
            else if (isQuotes && reader.TokenType == JsonTokenType.StartArray)
            {
                quotes = ParseQuotesArray(ref reader, count);
            }
            else if (isPremiumProperty)
            {
                isPremium = reader.TokenType is JsonTokenType.True or JsonTokenType.False && reader.GetBoolean();
            }
            else if (isIconUrl)
            {
                iconUrl = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return new ScreenerResult(id, title, description, canonicalName, start, count, total, isPremium, iconUrl, criteria, quotes);
    }

    private static ScreenerCriteria ParseCriteriaMeta(ref Utf8JsonReader reader)
    {
        var size = 0;
        var offset = 0;
        string? sortField = null;
        string? sortType = null;
        string? quoteType = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance screener result contained an invalid criteriaMeta object.");
            }

            var isSize = reader.ValueTextEquals("size"u8);
            var isOffset = reader.ValueTextEquals("offset"u8);
            var isSortField = reader.ValueTextEquals("sortField"u8);
            var isSortType = reader.ValueTextEquals("sortType"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSize)
            {
                size = ReadNullableInt32(ref reader) ?? 0;
            }
            else if (isOffset)
            {
                offset = ReadNullableInt32(ref reader) ?? 0;
            }
            else if (isSortField)
            {
                sortField = ReadNullableString(ref reader);
            }
            else if (isSortType)
            {
                sortType = ReadNullableString(ref reader);
            }
            else if (isQuoteType)
            {
                quoteType = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
            {
                reader.Skip();
            }
        }

        return new ScreenerCriteria(size, offset, sortField, sortType, quoteType);
    }

    private static ScreenerQuote[] ParseQuotesArray(ref Utf8JsonReader reader, int expectedCount)
    {
        var quotes = expectedCount > 0 ? new List<ScreenerQuote>(expectedCount) : [];
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

            string? symbol = null;
            string? shortName = null;
            string? longName = null;
            string? exchange = null;
            string? fullExchangeName = null;
            string? quoteType = null;
            string? typeDisplayName = null;
            string? currency = null;
            decimal? regularMarketPrice = null;
            decimal? regularMarketChange = null;
            decimal? regularMarketChangePercent = null;
            long? regularMarketVolume = null;
            decimal? marketCap = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance screener quotes array contained an invalid quote item.");
                }

                var isSymbol = reader.ValueTextEquals("symbol"u8);
                var isShortName = reader.ValueTextEquals("shortName"u8);
                var isLongName = reader.ValueTextEquals("longName"u8);
                var isExchange = reader.ValueTextEquals("exchange"u8);
                var isFullExchangeName = reader.ValueTextEquals("fullExchangeName"u8);
                var isQuoteType = reader.ValueTextEquals("quoteType"u8);
                var isTypeDisplayName = reader.ValueTextEquals("typeDisp"u8);
                var isCurrency = reader.ValueTextEquals("currency"u8);
                var isRegularMarketPrice = reader.ValueTextEquals("regularMarketPrice"u8);
                var isRegularMarketChange = reader.ValueTextEquals("regularMarketChange"u8);
                var isRegularMarketChangePercent = reader.ValueTextEquals("regularMarketChangePercent"u8);
                var isRegularMarketVolume = reader.ValueTextEquals("regularMarketVolume"u8);
                var isMarketCap = reader.ValueTextEquals("marketCap"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isSymbol)
                {
                    symbol = ReadNullableString(ref reader);
                }
                else if (isShortName)
                {
                    shortName = ReadNullableString(ref reader);
                }
                else if (isLongName)
                {
                    longName = ReadNullableString(ref reader);
                }
                else if (isExchange)
                {
                    exchange = ReadNullableString(ref reader);
                }
                else if (isFullExchangeName)
                {
                    fullExchangeName = ReadNullableString(ref reader);
                }
                else if (isQuoteType)
                {
                    quoteType = ReadNullableString(ref reader);
                }
                else if (isTypeDisplayName)
                {
                    typeDisplayName = ReadNullableString(ref reader);
                }
                else if (isCurrency)
                {
                    currency = ReadNullableString(ref reader);
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
                else if (isRegularMarketVolume)
                {
                    regularMarketVolume = ReadNullableInt64(ref reader);
                }
                else if (isMarketCap)
                {
                    marketCap = ReadNullableDecimal(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject)
                {
                    reader.Skip();
                }
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                continue;
            }

            quotes.Add(new ScreenerQuote(
                symbol,
                shortName,
                longName,
                exchange,
                fullExchangeName,
                quoteType,
                typeDisplayName,
                currency,
                regularMarketPrice,
                regularMarketChange,
                regularMarketChangePercent,
                regularMarketVolume,
                marketCap));
        }

        return quotes.ToArray();
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

    private static int? ReadNullableInt32(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt32(out var value) => value,
            JsonTokenType.String when int.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static long? ReadNullableInt64(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var value) => value,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
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