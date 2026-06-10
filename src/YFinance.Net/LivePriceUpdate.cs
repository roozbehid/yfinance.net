using System.Threading.Channels;

namespace YFinance.Net;

public sealed record LivePriceUpdate(
    string Symbol,
    float Price,
    DateTimeOffset Timestamp,
    string? Currency,
    string? Exchange,
    int QuoteType,
    int MarketHours,
    float ChangePercent,
    long DayVolume,
    float DayHigh,
    float DayLow,
    float Change,
    string? ShortName,
    DateTimeOffset? ExpireDate,
    float OpenPrice,
    float PreviousClose,
    float StrikePrice,
    string? UnderlyingSymbol,
    long OpenInterest,
    long OptionsType,
    long MiniOption,
    long LastSize,
    float Bid,
    long BidSize,
    float Ask,
    long AskSize,
    long PriceHint,
    long Volume24Hour,
    long VolumeAllCurrencies,
    string? FromCurrency,
    string? LastMarket,
    double CirculatingSupply,
    double MarketCap);

public sealed record YahooLiveStreamOptions
{
    public Uri StreamUri { get; init; } = new("wss://streamer.finance.yahoo.com/?version=2", UriKind.Absolute);

    public int ChannelCapacity { get; init; } = 1024;

    public BoundedChannelFullMode ChannelFullMode { get; init; } = BoundedChannelFullMode.DropOldest;

    public TimeSpan ResubscribeInterval { get; init; } = TimeSpan.FromSeconds(15);

    public bool AutoReconnect { get; init; } = true;

    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(3);
}