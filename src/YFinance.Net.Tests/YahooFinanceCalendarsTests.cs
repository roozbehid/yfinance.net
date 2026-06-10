using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceCalendarsTests
{
    [Fact]
    public async Task GetEarningsCalendarAsync_ParsesResponseAndPostsVisualizationBody()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 391,
                    "documents": [
                      {
                        "columns": [
                          { "id": "ticker", "label": "Symbol", "type": "STRING" },
                          { "id": "companyshortname", "label": "Company Name", "type": "STRING" },
                          { "id": "intradaymarketcap", "label": "Market Cap (Intraday)", "type": "NUMBER" },
                          { "id": "eventname", "label": "Event Name", "type": "STRING" },
                          { "id": "startdatetime", "label": "Event Start Date", "type": "DATE" },
                          { "id": "startdatetimetype", "label": "Event Start Date", "type": "STRING" },
                          { "id": "epsestimate", "label": "EPS Estimate", "type": "NUMBER" },
                          { "id": "epsactual", "label": "Reported EPS", "type": "NUMBER" },
                          { "id": "epssurprisepct", "label": "Surprise (%)", "type": "NUMBER" }
                        ],
                        "rows": [
                          ["ORCL", "Oracle Corporation", 555249433738.4033, "Q4 2026 Earnings Announcement", "2026-06-10T20:00:00Z", "AMC", 1.96, null, 0.0]
                        ]
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        using var httpClient = CreateProtectedCalendarHttpClient(
            payload,
            request =>
            {
                capturedRequest = request;
                capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            });
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetEarningsCalendarAsync(new EarningsCalendarRequest(
            Start: new DateOnly(2026, 6, 9),
            End: new DateOnly(2026, 6, 16),
            Limit: 2,
            MinimumMarketCap: 1_000_000_000m));

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query);
        Assert.NotNull(capturedBody);
        Assert.Contains("\"entityIdType\":\"sp_earnings\"", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"size\":2", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"intradaymarketcap\"", capturedBody!, StringComparison.Ordinal);

        Assert.Equal(391, result.Total);
        Assert.Single(result.Entries);
        Assert.Equal("ORCL", result.Entries[0].Symbol);
        Assert.Equal("AMC", result.Entries[0].Timing);
        Assert.Equal(1.96m, result.Entries[0].EpsEstimate);
        Assert.Equal(0.0m, result.Entries[0].SurprisePercent);
    }

    [Fact]
    public async Task GetIpoCalendarAsync_ParsesOptionalFields()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 12,
                    "documents": [
                      {
                        "columns": [
                          { "id": "ticker", "label": "Symbol", "type": "STRING" },
                          { "id": "companyshortname", "label": "Company", "type": "STRING" },
                          { "id": "exchange_short_name", "label": "Exchange Short Name", "type": "STRING" },
                          { "id": "filingdate", "label": "Filing Date", "type": "DATE" },
                          { "id": "startdatetime", "label": "Date", "type": "DATE" },
                          { "id": "amendeddate", "label": "Amended Date", "type": "DATE" },
                          { "id": "pricefrom", "label": "Price From", "type": "NUMBER" },
                          { "id": "priceto", "label": "Price To", "type": "NUMBER" },
                          { "id": "offerprice", "label": "Price", "type": "NUMBER" },
                          { "id": "currencyname", "label": "Currency", "type": "STRING" },
                          { "id": "shares", "label": "Shares", "type": "NUMBER" },
                          { "id": "dealtype", "label": "Action", "type": "STRING" }
                        ],
                        "rows": [
                          ["QLEP", "Quantum Leap Acquisition Corp", "NYSE", null, "2026-06-22T04:00:00Z", null, null, null, null, "", null, "Expected"]
                        ]
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = CreateProtectedCalendarHttpClient(payload);
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetIpoCalendarAsync(new IpoCalendarRequest(Limit: 1));

        Assert.Equal(12, result.Total);
        Assert.Single(result.Entries);
        Assert.Equal("QLEP", result.Entries[0].Symbol);
        Assert.Equal("NYSE", result.Entries[0].Exchange);
        Assert.Equal(DateTimeOffset.Parse("2026-06-22T04:00:00Z"), result.Entries[0].Date);
        Assert.Null(result.Entries[0].Currency);
        Assert.Equal("Expected", result.Entries[0].Action);
    }

    [Fact]
    public async Task GetEconomicEventsCalendarAsync_ParsesRows()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 403,
                    "documents": [
                      {
                        "columns": [
                          { "id": "econ_release", "label": "Event", "type": "STRING" },
                          { "id": "country_code", "label": "Country Code", "type": "STRING" },
                          { "id": "startdatetime", "label": "Event Time", "type": "DATE" },
                          { "id": "period", "label": "For", "type": "STRING" },
                          { "id": "after_release_actual", "label": "Actual", "type": "STRING" },
                          { "id": "consensus_estimate", "label": "Market Expectation", "type": "STRING" },
                          { "id": "prior_release_actual", "label": "Prior to This", "type": "STRING" },
                          { "id": "originally_reported_actual", "label": "Revised from", "type": "STRING" }
                        ],
                        "rows": [
                          ["Urban Investment (YTD)YY", "CN", "2026-06-16T02:00:00Z", "May", null, null, "-1.6", null]
                        ]
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = CreateProtectedCalendarHttpClient(payload);
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetEconomicEventsCalendarAsync(new EconomicEventCalendarRequest(Limit: 1));

        Assert.Equal(403, result.Total);
        Assert.Single(result.Entries);
        Assert.Equal("Urban Investment (YTD)YY", result.Entries[0].Event);
        Assert.Equal("CN", result.Entries[0].CountryCode);
        Assert.Equal("May", result.Entries[0].Period);
        Assert.Equal("-1.6", result.Entries[0].Last);
    }

    [Fact]
    public async Task GetSplitsCalendarAsync_ParsesBooleanAndRatios()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 8,
                    "documents": [
                      {
                        "columns": [
                          { "id": "ticker", "label": "Symbol", "type": "STRING" },
                          { "id": "companyshortname", "label": "Company", "type": "STRING" },
                          { "id": "startdatetime", "label": "Payable On", "type": "DATE" },
                          { "id": "optionable", "label": "Optionable?", "type": "BOOLEAN" },
                          { "id": "old_share_worth", "label": "Old Share Worth", "type": "NUMBER" },
                          { "id": "share_worth", "label": "Share Worth", "type": "NUMBER" }
                        ],
                        "rows": [
                          ["021880.KQ", "Mason Capital Corp", "2026-07-09T04:00:00Z", false, 2, 1]
                        ]
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = CreateProtectedCalendarHttpClient(payload);
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetSplitsCalendarAsync(new SplitsCalendarRequest(Limit: 1));

        Assert.Equal(8, result.Total);
        Assert.Single(result.Entries);
        Assert.Equal("021880.KQ", result.Entries[0].Symbol);
        Assert.False(result.Entries[0].Optionable);
        Assert.Equal(2m, result.Entries[0].OldShareWorth);
        Assert.Equal(1m, result.Entries[0].ShareWorth);
    }

    [Fact]
    public async Task Calendars_Facade_AppliesDefaultDateWindow()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 0,
                    "documents": [
                      { "columns": [], "rows": [] }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        string? capturedBody = null;
        using var httpClient = CreateProtectedCalendarHttpClient(
            payload,
            request => capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
        using var client = new YahooFinanceClient(httpClient);
        using var calendars = new Calendars(new DateOnly(2026, 6, 9), new DateOnly(2026, 6, 16), client);

        await calendars.GetEconomicEventsAsync();

        Assert.NotNull(capturedBody);
        Assert.Contains("2026-06-09", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("2026-06-16", capturedBody!, StringComparison.Ordinal);
    }

    private static HttpClient CreateProtectedCalendarHttpClient(string payload, Action<HttpRequestMessage>? onCalendarRequest = null)
    {
        return new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.Host == "fc.yahoo.com")
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("sad panda", Encoding.UTF8, "text/html")
                };
            }

            if (request.RequestUri.Host == "query1.finance.yahoo.com" && request.RequestUri.AbsolutePath == "/v1/test/getcrumb")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
                };
            }

            onCalendarRequest?.Invoke(request);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
    }
}