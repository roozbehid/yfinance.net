namespace YFinance.Net;

/// <summary>
/// Convenience facade for Yahoo Finance calendar endpoints.
/// </summary>
public sealed class Calendars : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    /// <summary>
    /// Initializes a calendar facade with optional default date bounds.
    /// </summary>
    public Calendars(DateOnly? start = null, DateOnly? end = null, YahooFinanceClient? client = null)
    {
        DefaultStart = start;
        DefaultEnd = end;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    /// <summary>
    /// Gets the default start date applied to requests that omit one.
    /// </summary>
    public DateOnly? DefaultStart { get; }

    /// <summary>
    /// Gets the default end date applied to requests that omit one.
    /// </summary>
    public DateOnly? DefaultEnd { get; }

    /// <summary>
    /// Gets the Yahoo Finance earnings calendar.
    /// </summary>
    public Task<CalendarResult<EarningsCalendarEntry>> GetEarningsAsync(EarningsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EarningsCalendarRequest();
        return _client.GetEarningsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    /// <summary>
    /// Gets the Yahoo Finance IPO calendar.
    /// </summary>
    public Task<CalendarResult<IpoCalendarEntry>> GetIposAsync(IpoCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new IpoCalendarRequest();
        return _client.GetIpoCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    /// <summary>
    /// Gets the Yahoo Finance economic events calendar.
    /// </summary>
    public Task<CalendarResult<EconomicEventCalendarEntry>> GetEconomicEventsAsync(EconomicEventCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EconomicEventCalendarRequest();
        return _client.GetEconomicEventsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    /// <summary>
    /// Gets the Yahoo Finance stock splits calendar.
    /// </summary>
    public Task<CalendarResult<SplitCalendarEntry>> GetSplitsAsync(SplitsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new SplitsCalendarRequest();
        return _client.GetSplitsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    /// <summary>
    /// Disposes the owned client when this facade created it.
    /// </summary>
    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    private EarningsCalendarRequest ApplyDefaults(EarningsCalendarRequest request)
    {
        return request with
        {
            Start = request.Start ?? DefaultStart,
            End = request.End ?? DefaultEnd
        };
    }

    private IpoCalendarRequest ApplyDefaults(IpoCalendarRequest request)
    {
        return request with
        {
            Start = request.Start ?? DefaultStart,
            End = request.End ?? DefaultEnd
        };
    }

    private EconomicEventCalendarRequest ApplyDefaults(EconomicEventCalendarRequest request)
    {
        return request with
        {
            Start = request.Start ?? DefaultStart,
            End = request.End ?? DefaultEnd
        };
    }

    private SplitsCalendarRequest ApplyDefaults(SplitsCalendarRequest request)
    {
        return request with
        {
            Start = request.Start ?? DefaultStart,
            End = request.End ?? DefaultEnd
        };
    }
}