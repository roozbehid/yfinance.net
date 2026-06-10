namespace YFinance.Net;

public sealed class Calendars : IDisposable
{
    private readonly YahooFinanceClient _client;
    private readonly bool _ownsClient;

    public Calendars(DateOnly? start = null, DateOnly? end = null, YahooFinanceClient? client = null)
    {
        DefaultStart = start;
        DefaultEnd = end;
        _client = client ?? new YahooFinanceClient();
        _ownsClient = client is null;
    }

    public DateOnly? DefaultStart { get; }

    public DateOnly? DefaultEnd { get; }

    public Task<CalendarResult<EarningsCalendarEntry>> GetEarningsAsync(EarningsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EarningsCalendarRequest();
        return _client.GetEarningsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    public Task<CalendarResult<IpoCalendarEntry>> GetIposAsync(IpoCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new IpoCalendarRequest();
        return _client.GetIpoCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    public Task<CalendarResult<EconomicEventCalendarEntry>> GetEconomicEventsAsync(EconomicEventCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new EconomicEventCalendarRequest();
        return _client.GetEconomicEventsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

    public Task<CalendarResult<SplitCalendarEntry>> GetSplitsAsync(SplitsCalendarRequest? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new SplitsCalendarRequest();
        return _client.GetSplitsCalendarAsync(ApplyDefaults(request), cancellationToken);
    }

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