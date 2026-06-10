using System.Threading.Channels;

namespace YFinance.Net;

/// <summary>
/// Represents a decoded live price update from Yahoo Finance's websocket feed.
/// </summary>
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

/// <summary>
/// Configures <see cref="YahooLiveStream"/> behavior.
/// </summary>
public sealed record YahooLiveStreamOptions
{
    /// <summary>
    /// Gets the websocket endpoint used for live Yahoo Finance updates.
    /// </summary>
    public Uri StreamUri { get; init; } = new("wss://streamer.finance.yahoo.com/?version=2", UriKind.Absolute);

    /// <summary>
    /// Gets the bounded channel capacity used for buffering live updates.
    /// </summary>
    public int ChannelCapacity { get; init; } = 1024;

    /// <summary>
    /// Gets how the bounded channel behaves when it is full.
    /// </summary>
    public BoundedChannelFullMode ChannelFullMode { get; init; } = BoundedChannelFullMode.DropOldest;

    /// <summary>
    /// Gets how frequently the stream resends current subscriptions while connected.
    /// </summary>
    public TimeSpan ResubscribeInterval { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets whether the live stream should attempt automatic reconnects.
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    /// Gets the delay before attempting a reconnect after a disconnect.
    /// </summary>
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(3);
}