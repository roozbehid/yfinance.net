using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceEarningsDatesTests
{
    [Fact]
    public async Task GetEarningsDatesAsync_ParsesVisualizationRows()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 133,
                    "documents": [
                      {
                        "columns": [
                          { "id": "startdatetime", "label": "Event Start Date", "type": "DATE" },
                          { "id": "timeZoneShortName", "label": "Timezone short name", "type": "STRING" },
                          { "id": "epsestimate", "label": "EPS Estimate", "type": "NUMBER" },
                          { "id": "epsactual", "label": "Reported EPS", "type": "NUMBER" },
                          { "id": "epssurprisepct", "label": "Surprise (%)", "type": "NUMBER" },
                          { "id": "eventtype", "label": "Event Type", "type": "STRING" }
                        ],
                        "rows": [
                          ["2025-04-23T20:18:00Z", "EDT", 1.4, 1.6, 14.1, "2"],
                          ["2025-01-29T21:08:00Z", "EST", 3.75, 3.92, 4.63, "2"]
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
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
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

            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetEarningsDatesAsync("IBM", limit: 2, offset: 3);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri!.Query);
        Assert.NotNull(capturedBody);
        Assert.Contains("\"entityIdType\":\"earnings\"", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"ticker\",\"IBM\"", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"offset\":3", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"size\":2", capturedBody!, StringComparison.Ordinal);

        Assert.Equal(133, result.Total);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(DateTimeOffset.Parse("2025-04-23T20:18:00Z"), result.Entries[0].EarningsDate);
        Assert.Equal("EDT", result.Entries[0].TimeZoneShortName);
        Assert.Equal(1.4m, result.Entries[0].EpsEstimate);
        Assert.Equal(1.6m, result.Entries[0].ReportedEps);
        Assert.Equal(14.1m, result.Entries[0].SurprisePercent);
        Assert.Equal("2", result.Entries[0].EventType);
    }

    [Fact]
    public async Task GetEarningsDatesAsync_ReturnsEmptyEntriesWhenYahooHasNoRows()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 0,
                    "documents": [
                      {
                        "columns": [],
                        "rows": []
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
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

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetEarningsDatesAsync("NOPE", limit: 1);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task TickerFacade_GetEarningsDatesAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "total": 1,
                    "documents": [
                      {
                        "columns": [
                          { "id": "startdatetime", "label": "Event Start Date", "type": "DATE" },
                          { "id": "timeZoneShortName", "label": "Timezone short name", "type": "STRING" },
                          { "id": "epsestimate", "label": "EPS Estimate", "type": "NUMBER" },
                          { "id": "epsactual", "label": "Reported EPS", "type": "NUMBER" },
                          { "id": "epssurprisepct", "label": "Surprise (%)", "type": "NUMBER" },
                          { "id": "eventtype", "label": "Event Type", "type": "STRING" }
                        ],
                        "rows": [
                          ["2025-01-29T21:08:00Z", "EST", 3.75, 3.92, 4.63, "2"]
                        ]
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
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

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var ticker = new Ticker("IBM", client);

        var result = await ticker.GetEarningsDatesAsync(limit: 1);

        Assert.Single(result.Entries);
        Assert.Equal(3.92m, result.Entries[0].ReportedEps);
    }
}