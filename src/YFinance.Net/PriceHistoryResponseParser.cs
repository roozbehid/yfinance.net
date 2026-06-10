using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace YFinance.Net;

internal static class PriceHistoryResponseParser
{
    public static PriceHistoryResult Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return Parse(Encoding.UTF8.GetBytes(json));
    }

    public static PriceHistoryResult Parse(ReadOnlyMemory<byte> json)
    {
        var reader = new Utf8JsonReader(json.Span, isFinalBlock: true, state: default);

        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance chart response did not contain a chart object.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain a chart object.");
            }

            if (reader.ValueTextEquals("chart"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new InvalidOperationException("Yahoo Finance chart response did not contain a chart object.");
                }

                return ParseChartObject(ref reader);
            }

            SkipPropertyValue(ref reader);
        }

        throw new InvalidOperationException("Yahoo Finance chart response did not contain a chart object.");
    }

    private static PriceHistoryResult ParseChartObject(ref Utf8JsonReader reader)
    {
        PriceHistoryResult? result = null;
        string? errorMessage = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain a chart object.");
            }

            if (reader.ValueTextEquals("error"u8))
            {
                if (!reader.Read())
                {
                    throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
                }

                if (reader.TokenType != JsonTokenType.Null)
                {
                    errorMessage = ReadJsonValueAsString(ref reader);
                }

                continue;
            }

            if (reader.ValueTextEquals("result"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
                }

                result = ParseResultArray(ref reader);
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException($"Yahoo Finance chart endpoint returned an error: {errorMessage}");
        }

        return result ?? throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
    }

    private static PriceHistoryResult? ParseResultArray(ref Utf8JsonReader reader)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
        }

        var result = ParseResultObject(ref reader);
        SkipRemainingArrayValues(ref reader);
        return result;
    }

    private static PriceHistoryResult ParseResultObject(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        string? currency = null;
        string? exchangeTimeZone = null;
        string? instrumentType = null;
        string[] validRanges = [];
        var timestampCount = 0;
        var dividends = Array.Empty<DividendEvent>();
        var splits = Array.Empty<SplitEvent>();
        var capitalGains = Array.Empty<CapitalGainEvent>();
        var builders = default(BarBuilderBuffer);
        var hasIndicators = false;

        try
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance chart endpoint returned no result.");
                }

                if (reader.ValueTextEquals("meta"u8))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new InvalidOperationException("Yahoo Finance chart response did not contain meta.");
                    }

                    var meta = ParseMetaObject(ref reader);
                    symbol = meta.Symbol;
                    currency = meta.Currency;
                    exchangeTimeZone = meta.ExchangeTimeZone;
                    instrumentType = meta.InstrumentType;
                    validRanges = meta.ValidRanges;
                    continue;
                }

                if (reader.ValueTextEquals("timestamp"u8))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                    {
                        timestampCount = 0;
                    }
                    else
                    {
                        timestampCount = ParseInt64BarArray(ref reader, ref builders, PriceBarInt64Field.Timestamp);
                    }

                    continue;
                }

                if (reader.ValueTextEquals("events"u8))
                {
                    if (!reader.Read())
                    {
                        continue;
                    }

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        var events = ParseEventsObject(ref reader);
                        dividends = events.Dividends;
                        splits = events.Splits;
                        capitalGains = events.CapitalGains;
                    }

                    continue;
                }

                if (reader.ValueTextEquals("indicators"u8))
                {
                    if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                    {
                        throw new InvalidOperationException("Yahoo Finance chart response did not contain indicators.");
                    }

                    ParseIndicatorsObject(ref reader, ref builders);
                    hasIndicators = true;
                    continue;
                }

                SkipPropertyValue(ref reader);
            }

            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new InvalidOperationException("Yahoo Finance chart metadata did not contain a symbol.");
            }

            if (!hasIndicators)
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain indicators.");
            }

            var bars = BuildBars(timestampCount, ref builders);
            return new PriceHistoryResult(symbol, currency, exchangeTimeZone, instrumentType, validRanges, bars, dividends, splits, capitalGains);
        }
        finally
        {
            builders.Dispose();
        }
    }

    private static MetaFields ParseMetaObject(ref Utf8JsonReader reader)
    {
        string? symbol = null;
        string? currency = null;
        string? exchangeTimeZone = null;
        string? instrumentType = null;
        string[] validRanges = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain meta.");
            }

            var isSymbol = reader.ValueTextEquals("symbol"u8);
            var isCurrency = reader.ValueTextEquals("currency"u8);
            var isExchangeTimeZone = reader.ValueTextEquals("exchangeTimezoneName"u8);
            var isInstrumentType = reader.ValueTextEquals("instrumentType"u8);
            var isValidRanges = reader.ValueTextEquals("validRanges"u8);

            if (!reader.Read())
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain meta.");
            }

            if (isSymbol)
            {
                symbol = ReadNullableString(ref reader);
            }
            else if (isCurrency)
            {
                currency = ReadNullableString(ref reader);
            }
            else if (isExchangeTimeZone)
            {
                exchangeTimeZone = ReadNullableString(ref reader);
            }
            else if (isInstrumentType)
            {
                instrumentType = ReadNullableString(ref reader);
            }
            else if (isValidRanges)
            {
                validRanges = reader.TokenType == JsonTokenType.StartArray
                    ? ReadStringArray(ref reader)
                    : [];
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new MetaFields(symbol, currency, exchangeTimeZone, instrumentType, validRanges);
    }

    private static EventCollections ParseEventsObject(ref Utf8JsonReader reader)
    {
        var dividends = Array.Empty<DividendEvent>();
        var splits = Array.Empty<SplitEvent>();
        var capitalGains = Array.Empty<CapitalGainEvent>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid events.");
            }

            var isDividends = reader.ValueTextEquals("dividends"u8);
            var isSplits = reader.ValueTextEquals("splits"u8);
            var isCapitalGains = reader.ValueTextEquals("capitalGains"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isDividends && reader.TokenType == JsonTokenType.StartObject)
            {
                dividends = ParseDividendEvents(ref reader);
            }
            else if (isSplits && reader.TokenType == JsonTokenType.StartObject)
            {
                splits = ParseSplitEvents(ref reader);
            }
            else if (isCapitalGains && reader.TokenType == JsonTokenType.StartObject)
            {
                capitalGains = ParseCapitalGainEvents(ref reader);
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return new EventCollections(dividends, splits, capitalGains);
    }

    private static DividendEvent[] ParseDividendEvents(ref Utf8JsonReader reader)
    {
        var items = new List<DividendEvent>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid dividend events.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                SkipValueToken(ref reader);
                continue;
            }

            long? date = null;
            decimal? amount = null;
            string? currency = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance chart response contained invalid dividend events.");
                }

                var isDate = reader.ValueTextEquals("date"u8);
                var isAmount = reader.ValueTextEquals("amount"u8);
                var isCurrency = reader.ValueTextEquals("currency"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isDate)
                {
                    date = ReadNullableInt64(ref reader);
                }
                else if (isAmount)
                {
                    amount = ReadNullableDecimal(ref reader);
                }
                else if (isCurrency)
                {
                    currency = ReadNullableString(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            if (date is not null && amount is not null)
            {
                items.Add(new DividendEvent(DateTimeOffset.FromUnixTimeSeconds(date.Value), amount.Value, currency));
            }
        }

        if (items.Count > 1)
        {
            items.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));
        }

        return items.ToArray();
    }

    private static SplitEvent[] ParseSplitEvents(ref Utf8JsonReader reader)
    {
        var items = new List<SplitEvent>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid split events.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                SkipValueToken(ref reader);
                continue;
            }

            long? date = null;
            long? numerator = null;
            long? denominator = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance chart response contained invalid split events.");
                }

                var isDate = reader.ValueTextEquals("date"u8);
                var isNumerator = reader.ValueTextEquals("numerator"u8);
                var isDenominator = reader.ValueTextEquals("denominator"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isDate)
                {
                    date = ReadNullableInt64(ref reader);
                }
                else if (isNumerator)
                {
                    numerator = ReadNullableInt64(ref reader);
                }
                else if (isDenominator)
                {
                    denominator = ReadNullableInt64(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            if (date is not null && numerator is not null && denominator is not null && denominator != 0)
            {
                items.Add(new SplitEvent(
                    DateTimeOffset.FromUnixTimeSeconds(date.Value),
                    decimal.Divide(numerator.Value, denominator.Value),
                    numerator,
                    denominator));
            }
        }

        if (items.Count > 1)
        {
            items.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));
        }

        return items.ToArray();
    }

    private static CapitalGainEvent[] ParseCapitalGainEvents(ref Utf8JsonReader reader)
    {
        var items = new List<CapitalGainEvent>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid capital gain events.");
            }

            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            {
                SkipValueToken(ref reader);
                continue;
            }

            long? date = null;
            decimal? amount = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new InvalidOperationException("Yahoo Finance chart response contained invalid capital gain events.");
                }

                var isDate = reader.ValueTextEquals("date"u8);
                var isAmount = reader.ValueTextEquals("amount"u8);
                var isLongTermCapitalGain = reader.ValueTextEquals("longTermCapitalGain"u8);
                var isShortTermCapitalGain = reader.ValueTextEquals("shortTermCapitalGain"u8);

                if (!reader.Read())
                {
                    break;
                }

                if (isDate)
                {
                    date = ReadNullableInt64(ref reader);
                }
                else if (isAmount || isLongTermCapitalGain || isShortTermCapitalGain)
                {
                    amount ??= ReadNullableDecimal(ref reader);
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    reader.Skip();
                }
            }

            if (date is not null && amount is not null)
            {
                items.Add(new CapitalGainEvent(DateTimeOffset.FromUnixTimeSeconds(date.Value), amount.Value));
            }
        }

        if (items.Count > 1)
        {
            items.Sort(static (left, right) => left.Timestamp.CompareTo(right.Timestamp));
        }

        return items.ToArray();
    }

    private static void ParseIndicatorsObject(ref Utf8JsonReader reader, ref BarBuilderBuffer builders)
    {
        var hasQuote = false;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response did not contain indicators.");
            }

            if (reader.ValueTextEquals("quote"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new InvalidOperationException("Yahoo Finance chart response did not contain quote indicators.");
                }

                ParseQuoteArray(ref reader, ref builders);
                hasQuote = true;
                continue;
            }

            if (reader.ValueTextEquals("adjclose"u8))
            {
                if (!reader.Read())
                {
                    continue;
                }

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseAdjustedCloseArray(ref reader, ref builders);
                }
                continue;
            }

            SkipPropertyValue(ref reader);
        }

        if (!hasQuote)
        {
            throw new InvalidOperationException("Yahoo Finance chart response did not contain quote indicators.");
        }
    }

    private static void ParseQuoteArray(ref Utf8JsonReader reader, ref BarBuilderBuffer builders)
    {
        if (!reader.Read())
        {
            throw new InvalidOperationException("Yahoo Finance chart response contained no quote indicator entries.");
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            throw new InvalidOperationException("Yahoo Finance chart response contained no quote indicator entries.");
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidOperationException("Yahoo Finance chart response contained no quote indicator entries.");
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid quote indicators.");
            }

            var isOpen = reader.ValueTextEquals("open"u8);
            var isHigh = reader.ValueTextEquals("high"u8);
            var isLow = reader.ValueTextEquals("low"u8);
            var isClose = reader.ValueTextEquals("close"u8);
            var isVolume = reader.ValueTextEquals("volume"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isOpen)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseDecimalBarArray(ref reader, ref builders, PriceBarDecimalField.Open);
                }
            }
            else if (isHigh)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseDecimalBarArray(ref reader, ref builders, PriceBarDecimalField.High);
                }
            }
            else if (isLow)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseDecimalBarArray(ref reader, ref builders, PriceBarDecimalField.Low);
                }
            }
            else if (isClose)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseDecimalBarArray(ref reader, ref builders, PriceBarDecimalField.Close);
                }
            }
            else if (isVolume)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseInt64BarArray(ref reader, ref builders, PriceBarInt64Field.Volume);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        SkipRemainingArrayValues(ref reader);
    }

    private static void ParseAdjustedCloseArray(ref Utf8JsonReader reader, ref BarBuilderBuffer builders)
    {
        if (!reader.Read())
        {
            return;
        }

        if (reader.TokenType == JsonTokenType.EndArray)
        {
            return;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            SkipRemainingArrayValues(ref reader);
            return;
        }

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new InvalidOperationException("Yahoo Finance chart response contained invalid adjusted close indicators.");
            }

            var isAdjustedClose = reader.ValueTextEquals("adjclose"u8);

            if (!reader.Read())
            {
                break;
            }

            if (isAdjustedClose)
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    ParseDecimalBarArray(ref reader, ref builders, PriceBarDecimalField.AdjustedClose);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        SkipRemainingArrayValues(ref reader);
    }

    private static PriceBar[] BuildBars(int timestampCount, ref BarBuilderBuffer builders)
    {
        var bars = new PriceBar[timestampCount];
        for (var index = 0; index < timestampCount; index++)
        {
            ref var builder = ref builders[index];
            var timestamp = DateTimeOffset.FromUnixTimeSeconds(builder.TimestampSeconds ?? throw new InvalidOperationException("Yahoo Finance chart timestamp was null."));
            bars[index] = new PriceBar(
                timestamp,
                builder.Open,
                builder.High,
                builder.Low,
                builder.Close,
                builder.HasAdjustedClose ? builder.AdjustedClose : builder.Close,
                builder.Volume);
        }

        return bars;
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

    private static string[] ReadStringArray(ref Utf8JsonReader reader)
    {
        var items = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var text = reader.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    items.Add(text);
                }
            }
            else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
            {
                reader.Skip();
            }
        }

        return items.ToArray();
    }

    private static int ParseDecimalBarArray(ref Utf8JsonReader reader, ref BarBuilderBuffer builders, PriceBarDecimalField field)
    {
        var count = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            builders.EnsureCapacity(count + 1);
            ref var builder = ref builders[count];
            var value = ReadNullableDecimal(ref reader);
            switch (field)
            {
                case PriceBarDecimalField.Open:
                    builder.Open = value;
                    break;
                case PriceBarDecimalField.High:
                    builder.High = value;
                    break;
                case PriceBarDecimalField.Low:
                    builder.Low = value;
                    break;
                case PriceBarDecimalField.Close:
                    builder.Close = value;
                    break;
                case PriceBarDecimalField.AdjustedClose:
                    builder.AdjustedClose = value;
                    builder.HasAdjustedClose = true;
                    break;
            }

            count++;
        }

        return count;
    }

    private static int ParseInt64BarArray(ref Utf8JsonReader reader, ref BarBuilderBuffer builders, PriceBarInt64Field field)
    {
        var count = 0;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            builders.EnsureCapacity(count + 1);
            ref var builder = ref builders[count];
            var value = ReadNullableInt64(ref reader);
            switch (field)
            {
                case PriceBarInt64Field.Timestamp:
                    builder.TimestampSeconds = value;
                    break;
                case PriceBarInt64Field.Volume:
                    builder.Volume = value;
                    break;
            }

            count++;
        }

        return count;
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

        SkipValueToken(ref reader);
    }

    private static void SkipValueToken(ref Utf8JsonReader reader)
    {
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

    private readonly record struct MetaFields(
        string? Symbol,
        string? Currency,
        string? ExchangeTimeZone,
        string? InstrumentType,
        string[] ValidRanges);

    private readonly record struct EventCollections(
        DividendEvent[] Dividends,
        SplitEvent[] Splits,
        CapitalGainEvent[] CapitalGains);

    private struct PriceBarBuilder
    {
        public long? TimestampSeconds;
        public decimal? Open;
        public decimal? High;
        public decimal? Low;
        public decimal? Close;
        public decimal? AdjustedClose;
        public bool HasAdjustedClose;
        public long? Volume;
    }

    private struct BarBuilderBuffer
    {
        private PriceBarBuilder[]? _buffer;

        public void EnsureCapacity(int required)
        {
            if (required <= 0)
            {
                return;
            }

            if (_buffer is not null && _buffer.Length >= required)
            {
                return;
            }

            var newSize = _buffer is null ? Math.Max(required, 16) : Math.Max(required, _buffer.Length * 2);
            var newBuffer = ArrayPool<PriceBarBuilder>.Shared.Rent(newSize);
            if (_buffer is not null)
            {
                _buffer.AsSpan().CopyTo(newBuffer);
                ArrayPool<PriceBarBuilder>.Shared.Return(_buffer);
            }

            _buffer = newBuffer;
        }

        public ref PriceBarBuilder this[int index] => ref _buffer![index];

        public void Dispose()
        {
            if (_buffer is not null)
            {
                ArrayPool<PriceBarBuilder>.Shared.Return(_buffer);
                _buffer = null;
            }
        }
    }

    private enum PriceBarDecimalField
    {
        Open,
        High,
        Low,
        Close,
        AdjustedClose
    }

    private enum PriceBarInt64Field
    {
        Timestamp,
        Volume
    }
}