namespace YFinance.Net.Tests;

public sealed class YahooFinanceCalendarsLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task Calendars_EarningsAndEconomicEvents_ReturnUpcomingEntries()
    {
        using var calendars = new Calendars();

        var earnings = await calendars.GetEarningsAsync(new EarningsCalendarRequest(Limit: 2));
        var events = await calendars.GetEconomicEventsAsync(new EconomicEventCalendarRequest(Limit: 2));

        Assert.NotEmpty(earnings.Entries);
        Assert.NotEmpty(events.Entries);
        Assert.All(earnings.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
        Assert.All(events.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Event)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task Calendars_IposAndSplits_ReturnEntriesForWiderDateWindows()
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        using var calendars = new Calendars(start, start.AddDays(180));

        var ipos = await calendars.GetIposAsync(new IpoCalendarRequest(Limit: 2));
        var splits = await calendars.GetSplitsAsync(new SplitsCalendarRequest(Limit: 2));

        Assert.NotEmpty(ipos.Entries);
        Assert.NotEmpty(splits.Entries);
        Assert.All(ipos.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
        Assert.All(splits.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_EarningsAndEconomicEvents_ReturnUpcomingEntries()
    {
        using var client = new YahooFinanceClient();

        var earnings = await client.GetEarningsCalendarAsync(new EarningsCalendarRequest(Limit: 2));
        var events = await client.GetEconomicEventsCalendarAsync(new EconomicEventCalendarRequest(Limit: 2));

        Assert.NotEmpty(earnings.Entries);
        Assert.NotEmpty(events.Entries);
        Assert.All(earnings.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
        Assert.All(events.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Event)));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task YahooFinanceClient_IposAndSplits_ReturnEntriesForWiderDateWindows()
    {
        using var client = new YahooFinanceClient();
        var start = DateOnly.FromDateTime(DateTime.UtcNow);

        var ipos = await client.GetIpoCalendarAsync(new IpoCalendarRequest(Start: start, End: start.AddDays(180), Limit: 2));
        var splits = await client.GetSplitsCalendarAsync(new SplitsCalendarRequest(Start: start, End: start.AddDays(180), Limit: 2));

        Assert.NotEmpty(ipos.Entries);
        Assert.NotEmpty(splits.Entries);
        Assert.All(ipos.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
        Assert.All(splits.Entries, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Symbol)));
    }
}