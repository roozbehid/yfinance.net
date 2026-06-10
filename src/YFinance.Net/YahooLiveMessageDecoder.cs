using System.Buffers;
using System.Text.Json;
using YFinance.Net.Protos;

namespace YFinance.Net;

internal static class YahooLiveMessageDecoder
{
    public static bool TryDecodeEnvelope(ReadOnlyMemory<byte> envelopeUtf8, out LivePriceUpdate? update)
    {
        return TryDecodeEnvelope(envelopeUtf8.Span, out update);
    }

    public static bool TryDecodeEnvelope(ReadOnlySpan<byte> envelopeUtf8, out LivePriceUpdate? update)
    {
        update = null;

        var reader = new Utf8JsonReader(envelopeUtf8, isFinalBlock: true, state: default);
        string? encodedMessage = null;
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("message"u8))
            {
                if (!reader.Read() || reader.TokenType != JsonTokenType.String)
                {
                    return false;
                }

                encodedMessage = reader.GetString();
                break;
            }
        }

        return !string.IsNullOrWhiteSpace(encodedMessage) && TryDecodeBase64(encodedMessage!, out update);
    }

    public static bool TryDecodeBase64(string base64Message, out LivePriceUpdate? update)
    {
        update = null;
        if (string.IsNullOrWhiteSpace(base64Message))
        {
            return false;
        }

        var maxDecodedLength = (base64Message.Length * 3 + 3) / 4;
        var rented = ArrayPool<byte>.Shared.Rent(maxDecodedLength);
        try
        {
            if (!Convert.TryFromBase64String(base64Message, rented, out var bytesWritten))
            {
                return false;
            }

            using var stream = new MemoryStream(rented, 0, bytesWritten, writable: false, publiclyVisible: true);
            var data = PricingData.Parser.ParseFrom(stream);
            update = Map(data);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    private static LivePriceUpdate Map(PricingData data)
    {
        return new LivePriceUpdate(
            Symbol: data.Id,
            Price: data.Price,
            Timestamp: DateTimeOffset.FromUnixTimeMilliseconds(data.Time),
            Currency: EmptyToNull(data.Currency),
            Exchange: EmptyToNull(data.Exchange),
            QuoteType: data.QuoteType,
            MarketHours: data.MarketHours,
            ChangePercent: data.ChangePercent,
            DayVolume: data.DayVolume,
            DayHigh: data.DayHigh,
            DayLow: data.DayLow,
            Change: data.Change,
            ShortName: EmptyToNull(data.ShortName),
            ExpireDate: data.ExpireDate == 0 ? null : DateTimeOffset.FromUnixTimeSeconds(data.ExpireDate),
            OpenPrice: data.OpenPrice,
            PreviousClose: data.PreviousClose,
            StrikePrice: data.StrikePrice,
            UnderlyingSymbol: EmptyToNull(data.UnderlyingSymbol),
            OpenInterest: data.OpenInterest,
            OptionsType: data.OptionsType,
            MiniOption: data.MiniOption,
            LastSize: data.LastSize,
            Bid: data.Bid,
            BidSize: data.BidSize,
            Ask: data.Ask,
            AskSize: data.AskSize,
            PriceHint: data.PriceHint,
            Volume24Hour: data.Vol24Hr,
            VolumeAllCurrencies: data.VolAllCurrencies,
            FromCurrency: EmptyToNull(data.FromCurrency),
            LastMarket: EmptyToNull(data.LastMarket),
            CirculatingSupply: data.CirculatingSupply,
            MarketCap: data.MarketCap);
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}