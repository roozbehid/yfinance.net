using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class OptionsResponseParser
{
    public static OptionChainResult Parse(string json, string requestedSymbol)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedSymbol);
        return Parse(Encoding.UTF8.GetBytes(json), requestedSymbol);
    }

    public static OptionChainResult Parse(ReadOnlyMemory<byte> json, string requestedSymbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedSymbol);

        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance options response did not contain an optionChain object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options response did not contain an optionChain object.");
            }

            if (reader.ValueTextEquals("optionChain"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance options response did not contain an optionChain object.");
                }

                return ParseOptionChainObject(ref reader, requestedSymbol);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance options response did not contain an optionChain object.");
    }

    private static OptionChainResult ParseOptionChainObject(ref Utf8JsonReader reader, string requestedSymbol)
    {
        OptionChainResult? result = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options response did not contain an optionChain object.");
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
                    throw new InvalidOperationException("Yahoo Finance options endpoint returned no result.");
                }

                result = ParseResultArray(ref reader, requestedSymbol);
                continue;
            }

            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance options endpoint returned an error: {errorMessage}");
        }

        return result ?? throw new InvalidOperationException("Yahoo Finance options endpoint returned no result.");
    }

    private static OptionChainResult? ParseResultArray(ref Utf8JsonReader reader, string requestedSymbol)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance options endpoint returned no result.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance options endpoint returned no result.");
        }

        var result = ParseResultObject(ref reader, requestedSymbol);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static OptionChainResult ParseResultObject(ref Utf8JsonReader reader, string requestedSymbol)
    {
        var expirationDates = Array.Empty<DateOnly>();
        DateOnly? expirationDate = null;
        OptionUnderlyingQuote? underlying = null;
        var calls = Array.Empty<OptionContract>();
        var puts = Array.Empty<OptionContract>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options endpoint returned no result.");
            }

            var isExpirationDates = reader.ValueTextEquals("expirationDates"u8);
            var isQuote = reader.ValueTextEquals("quote"u8);
            var isOptions = reader.ValueTextEquals("options"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isExpirationDates)
            {
                expirationDates = reader.TokenType == JsonTokenType.StartArray ? ParseExpirationDates(ref reader) : [];
            }
            else if (isQuote)
            {
                underlying = reader.TokenType == JsonTokenType.StartObject ? ParseUnderlyingQuote(ref reader) : null;
            }
            else if (isOptions)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    (expirationDate, calls, puts) = ParseOptionsArray(ref reader);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        var symbol = underlying?.Symbol;
        if (string.IsNullOrWhiteSpace(symbol))
        {
            symbol = requestedSymbol;
        }

        return new OptionChainResult(symbol!, expirationDates, expirationDate, calls, puts, underlying);
    }

    private static DateOnly[] ParseExpirationDates(ref Utf8JsonReader reader)
    {
        var items = new List<DateOnly>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            var expiration = ReadNullableDateOnlyFromUnixSeconds(ref reader);
            if (expiration is not null)
            {
                items.Add(expiration.Value);
            }
        }

        return items.ToArray();
    }

    private static (DateOnly? ExpirationDate, OptionContract[] Calls, OptionContract[] Puts) ParseOptionsArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            return (null, [], []);
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return (null, [], []);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            SkipRemainingArrayValues(ref reader);
            return (null, [], []);
        }

        DateOnly? expirationDate = null;
        var calls = Array.Empty<OptionContract>();
        var puts = Array.Empty<OptionContract>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options endpoint returned an invalid options payload.");
            }

            var isExpirationDate = reader.ValueTextEquals("expirationDate"u8);
            var isCalls = reader.ValueTextEquals("calls"u8);
            var isPuts = reader.ValueTextEquals("puts"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isExpirationDate)
            {
                expirationDate = ReadNullableDateOnlyFromUnixSeconds(ref reader);
            }
            else if (isCalls)
            {
                calls = reader.TokenType == JsonTokenType.StartArray ? ParseContractArray(ref reader) : [];
            }
            else if (isPuts)
            {
                puts = reader.TokenType == JsonTokenType.StartArray ? ParseContractArray(ref reader) : [];
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        SkipRemainingArrayValues(ref reader);
        return (expirationDate, calls, puts);
    }

    private static OptionContract[] ParseContractArray(ref Utf8JsonReader reader)
    {
        var items = new List<OptionContract>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }

                continue;
            }

            items.Add(ParseContractObject(ref reader));
        }

        return items.ToArray();
    }

    private static OptionContract ParseContractObject(ref Utf8JsonReader reader)
    {
        string? contractSymbol = null;
        DateTimeOffset? lastTradeDate = null;
        decimal? strike = null;
        decimal? lastPrice = null;
        decimal? bid = null;
        decimal? ask = null;
        decimal? change = null;
        decimal? percentChange = null;
        long? volume = null;
        long? openInterest = null;
        decimal? impliedVolatility = null;
        bool? inTheMoney = null;
        string? contractSize = null;
        string? currency = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options endpoint returned an invalid contract item.");
            }

            var isContractSymbol = reader.ValueTextEquals("contractSymbol"u8);
            var isLastTradeDate = reader.ValueTextEquals("lastTradeDate"u8);
            var isStrike = reader.ValueTextEquals("strike"u8);
            var isLastPrice = reader.ValueTextEquals("lastPrice"u8);
            var isBid = reader.ValueTextEquals("bid"u8);
            var isAsk = reader.ValueTextEquals("ask"u8);
            var isChange = reader.ValueTextEquals("change"u8);
            var isPercentChange = reader.ValueTextEquals("percentChange"u8);
            var isVolume = reader.ValueTextEquals("volume"u8);
            var isOpenInterest = reader.ValueTextEquals("openInterest"u8);
            var isImpliedVolatility = reader.ValueTextEquals("impliedVolatility"u8);
            var isInTheMoney = reader.ValueTextEquals("inTheMoney"u8);
            var isContractSize = reader.ValueTextEquals("contractSize"u8);
            var isCurrency = reader.ValueTextEquals("currency"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isContractSymbol) contractSymbol = ReadNullableString(ref reader);
            else if (isLastTradeDate) lastTradeDate = ReadNullableDateTimeOffsetFromUnixSeconds(ref reader);
            else if (isStrike) strike = ReadNullableDecimal(ref reader);
            else if (isLastPrice) lastPrice = ReadNullableDecimal(ref reader);
            else if (isBid) bid = ReadNullableDecimal(ref reader);
            else if (isAsk) ask = ReadNullableDecimal(ref reader);
            else if (isChange) change = ReadNullableDecimal(ref reader);
            else if (isPercentChange) percentChange = ReadNullableDecimal(ref reader);
            else if (isVolume) volume = ReadNullableInt64(ref reader);
            else if (isOpenInterest) openInterest = ReadNullableInt64(ref reader);
            else if (isImpliedVolatility) impliedVolatility = ReadNullableDecimal(ref reader);
            else if (isInTheMoney) inTheMoney = ReadNullableBoolean(ref reader);
            else if (isContractSize) contractSize = ReadNullableString(ref reader);
            else if (isCurrency) currency = ReadNullableString(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return new OptionContract(
            contractSymbol ?? string.Empty,
            lastTradeDate,
            strike,
            lastPrice,
            bid,
            ask,
            change,
            percentChange,
            volume,
            openInterest,
            impliedVolatility,
            inTheMoney,
            contractSize,
            currency);
    }

    private static OptionUnderlyingQuote ParseUnderlyingQuote(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        string? shortName = null;
        string? longName = null;
        string? quoteType = null;
        string? exchange = null;
        string? currency = null;
        decimal? regularMarketPrice = null;
        decimal? regularMarketChange = null;
        decimal? regularMarketChangePercent = null;
        DateTimeOffset? regularMarketTime = null;
        string? marketState = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance options endpoint returned an invalid underlying quote.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isShortName = reader.ValueTextEquals("shortName"u8);
            var isLongName = reader.ValueTextEquals("longName"u8);
            var isQuoteType = reader.ValueTextEquals("quoteType"u8);
            var isExchangeName = reader.ValueTextEquals("exchangeName"u8);
            var isExchange = reader.ValueTextEquals("exchange"u8);
            var isCurrency = reader.ValueTextEquals("currency"u8);
            var isRegularMarketPrice = reader.ValueTextEquals("regularMarketPrice"u8);
            var isRegularMarketChange = reader.ValueTextEquals("regularMarketChange"u8);
            var isRegularMarketChangePercent = reader.ValueTextEquals("regularMarketChangePercent"u8);
            var isRegularMarketTime = reader.ValueTextEquals("regularMarketTime"u8);
            var isMarketState = reader.ValueTextEquals("marketState"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isSymbol) symbol = ReadNullableString(ref reader);
            else if (isShortName) shortName = ReadNullableString(ref reader);
            else if (isLongName) longName = ReadNullableString(ref reader);
            else if (isQuoteType) quoteType = ReadNullableString(ref reader);
            else if (isExchangeName || isExchange) exchange = ReadNullableString(ref reader);
            else if (isCurrency) currency = ReadNullableString(ref reader);
            else if (isRegularMarketPrice) regularMarketPrice = ReadNullableDecimal(ref reader);
            else if (isRegularMarketChange) regularMarketChange = ReadNullableDecimal(ref reader);
            else if (isRegularMarketChangePercent) regularMarketChangePercent = ReadNullableDecimal(ref reader);
            else if (isRegularMarketTime) regularMarketTime = ReadNullableDateTimeOffsetFromUnixSeconds(ref reader);
            else if (isMarketState) marketState = ReadNullableString(ref reader);
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray) reader.Skip();
        }

        return new OptionUnderlyingQuote(
            symbol ?? string.Empty,
            shortName,
            longName,
            quoteType,
            exchange,
            currency,
            regularMarketPrice,
            regularMarketChange,
            regularMarketChangePercent,
            regularMarketTime,
            marketState);
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

    private static long? ReadNullableInt64(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.Number when reader.TryGetInt64(out var number) => number,
            JsonTokenType.String when long.TryParse(reader.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null
        };
    }

    private static bool? ReadNullableBoolean(ref Utf8JsonReader reader)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.String when bool.TryParse(reader.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static DateOnly? ReadNullableDateOnlyFromUnixSeconds(ref Utf8JsonReader reader)
    {
        var seconds = ReadNullableInt64(ref reader);
        return seconds is null ? null : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(seconds.Value).UtcDateTime);
    }

    private static DateTimeOffset? ReadNullableDateTimeOffsetFromUnixSeconds(ref Utf8JsonReader reader)
    {
        var seconds = ReadNullableInt64(ref reader);
        return seconds is null ? null : DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
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