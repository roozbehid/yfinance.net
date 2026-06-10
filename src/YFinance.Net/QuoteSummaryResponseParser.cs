using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class QuoteSummaryResponseParser
{
    public static QuoteSummary Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static QuoteSummary Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
            }

            if (reader.ValueTextEquals("quoteSummary"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
                }

                return ParseQuoteSummaryObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
    }

    private static QuoteSummary ParseQuoteSummaryObject(ref Utf8JsonReader reader)
    {
        QuoteSummary? result = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a quoteSummary object.");
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
                    errorMessage = ReadJsonValueAsString(ref reader);
                }

                continue;
            }

            if (isResult)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
                }

                result = ParseResultArray(ref reader);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance quoteSummary endpoint returned an error: {errorMessage}");
        }

        return result ?? throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
    }

    private static QuoteSummary? ParseResultArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
        }

        var result = ParseResultObject(ref reader);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static QuoteSummary ParseResultObject(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        var price = default(PriceFields);
        var quoteType = default(QuoteTypeFields);
        var summary = default(SummaryDetailFields);

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary endpoint returned no result.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isPrice = reader.ValueTextEquals("price"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            var isSummaryDetail = reader.ValueTextEquals("summaryDetail"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol)
            {
                symbol = ReadNullableString(ref reader);
            }
            else if (isPrice)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    price = ParsePriceObject(ref reader);
                }
            }
            else if (isQuoteType)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    quoteType = ParseQuoteTypeObject(ref reader);
                }
            }
            else if (isSummaryDetail)
            {
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    summary = ParseSummaryDetailObject(ref reader);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        symbol = price.Symbol ?? symbol;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidOperationException("Yahoo Finance quoteSummary response did not contain a symbol.");
        }

        return new QuoteSummary(
            symbol,
            price.ShortName,
            price.LongName,
            quoteType.Value,
            price.ExchangeName ?? price.Exchange,
            price.Currency,
            price.RegularMarketPrice,
            summary.PreviousClose,
            summary.Open,
            summary.DayHigh,
            summary.DayLow,
            summary.Volume);
    }

    private static PriceFields ParsePriceObject(ref Utf8JsonReader reader)
    {
        var result = default(PriceFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid price object.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isShortName = reader.ValueTextEquals("shortName"u8);
            var isLongName = reader.ValueTextEquals("longName"u8);
            var isExchangeName = reader.ValueTextEquals("exchangeName"u8);
            var isExchange = reader.ValueTextEquals("exchange"u8);
            var isCurrency = reader.ValueTextEquals("currency"u8);
            var isRegularMarketPrice = reader.ValueTextEquals("regularMarketPrice"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol)
            {
                result.Symbol = ReadNullableString(ref reader);
            }
            else if (isShortName)
            {
                result.ShortName = ReadNullableString(ref reader);
            }
            else if (isLongName)
            {
                result.LongName = ReadNullableString(ref reader);
            }
            else if (isExchangeName)
            {
                result.ExchangeName = ReadNullableString(ref reader);
            }
            else if (isExchange)
            {
                result.Exchange = ReadNullableString(ref reader);
            }
            else if (isCurrency)
            {
                result.Currency = ReadNullableString(ref reader);
            }
            else if (isRegularMarketPrice)
            {
                result.RegularMarketPrice = ReadNullableDecimal(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static QuoteTypeFields ParseQuoteTypeObject(ref Utf8JsonReader reader)
    {
        var result = default(QuoteTypeFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid quoteType object.");
            }

            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isQuoteType)
            {
                result.Value = ReadNullableString(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
    }

    private static SummaryDetailFields ParseSummaryDetailObject(ref Utf8JsonReader reader)
    {
        var result = default(SummaryDetailFields);
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid summaryDetail object.");
            }

            var isPreviousClose = reader.ValueTextEquals("previousClose"u8);
            var isOpen = reader.ValueTextEquals("open"u8);
            var isDayHigh = reader.ValueTextEquals("dayHigh"u8);
            var isDayLow = reader.ValueTextEquals("dayLow"u8);
            var isVolume = reader.ValueTextEquals("volume"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isPreviousClose)
            {
                result.PreviousClose = ReadNullableDecimal(ref reader);
            }
            else if (isOpen)
            {
                result.Open = ReadNullableDecimal(ref reader);
            }
            else if (isDayHigh)
            {
                result.DayHigh = ReadNullableDecimal(ref reader);
            }
            else if (isDayLow)
            {
                result.DayLow = ReadNullableDecimal(ref reader);
            }
            else if (isVolume)
            {
                result.Volume = ReadNullableInt64(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return result;
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

    private static long? ReadNullableInt64(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadRawWrappedInt64(ref reader);
        }

        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var number) => number,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
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
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid wrapped value.");
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
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid wrapped value.");
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

    private static long? ReadRawWrappedInt64(ref Utf8JsonReader reader)
    {
        long? result = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance quoteSummary response contained an invalid wrapped value.");
            }

            var isRaw = reader.ValueTextEquals("raw"u8);
            if (!reader.Read())
            {
                break;
            }

            if (isRaw)
            {
                result = ReadNullableInt64(ref reader);
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

    private struct PriceFields
    {
        public string? Symbol;
        public string? ShortName;
        public string? LongName;
        public string? ExchangeName;
        public string? Exchange;
        public string? Currency;
        public decimal? RegularMarketPrice;
    }

    private struct QuoteTypeFields
    {
        public string? Value;
    }

    private struct SummaryDetailFields
    {
        public decimal? PreviousClose;
        public decimal? Open;
        public decimal? DayHigh;
        public decimal? DayLow;
        public long? Volume;
    }
}