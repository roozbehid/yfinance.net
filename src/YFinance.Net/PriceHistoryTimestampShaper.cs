namespace YFinance.Net;

internal static class PriceHistoryTimestampShaper
{
    public static PriceHistoryResult Apply(PriceHistoryResult history, PriceTimestampMode mode)
    {
        if (mode == PriceTimestampMode.Utc || string.IsNullOrWhiteSpace(history.ExchangeTimeZone))
        {
            return history;
        }

        var timeZone = ResolveTimeZone(history.ExchangeTimeZone);
        if (timeZone is null)
        {
            return history;
        }

        var bars = new PriceBar[history.Bars.Length];
        for (var index = 0; index < history.Bars.Length; index++)
        {
            bars[index] = history.Bars[index] with { Timestamp = TimeZoneInfo.ConvertTime(history.Bars[index].Timestamp, timeZone) };
        }

        var dividends = new DividendEvent[history.Dividends.Length];
        for (var index = 0; index < history.Dividends.Length; index++)
        {
            dividends[index] = history.Dividends[index] with { Timestamp = TimeZoneInfo.ConvertTime(history.Dividends[index].Timestamp, timeZone) };
        }

        var splits = new SplitEvent[history.Splits.Length];
        for (var index = 0; index < history.Splits.Length; index++)
        {
            splits[index] = history.Splits[index] with { Timestamp = TimeZoneInfo.ConvertTime(history.Splits[index].Timestamp, timeZone) };
        }

        var capitalGains = new CapitalGainEvent[history.CapitalGains.Length];
        for (var index = 0; index < history.CapitalGains.Length; index++)
        {
            capitalGains[index] = history.CapitalGains[index] with { Timestamp = TimeZoneInfo.ConvertTime(history.CapitalGains[index].Timestamp, timeZone) };
        }

        return history with
        {
            Bars = bars,
            Dividends = dividends,
            Splits = splits,
            CapitalGains = capitalGains
        };
    }

    private static TimeZoneInfo? ResolveTimeZone(string exchangeTimeZone)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(exchangeTimeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(exchangeTimeZone, out var windowsId))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                }
                catch (TimeZoneNotFoundException)
                {
                    return null;
                }
            }

            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }
}