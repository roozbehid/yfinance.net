using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceHoldersTests
{
    [Fact]
    public async Task GetHoldersAsync_ParsesMajorInstitutionalAndMutualFundHolders()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "majorHoldersBreakdown": {
                      "insidersPercentHeld": { "raw": 0.0056 },
                      "institutionsPercentHeld": { "raw": 0.7911 },
                      "institutionsFloatPercentHeld": { "raw": 0.7954 },
                      "institutionsCount": { "raw": 6118 }
                    },
                    "institutionOwnership": {
                      "ownershipList": [
                        {
                          "reportDate": { "raw": 1774915200 },
                          "organization": "Blackrock Inc.",
                          "pctHeld": { "raw": 0.0767 },
                          "position": { "raw": 446980992 },
                          "value": { "raw": 162817300510 },
                          "pctChange": { "raw": 0.0113 }
                        }
                      ]
                    },
                    "fundOwnership": {
                      "ownershipList": [
                        {
                          "reportDate": { "raw": 1774915200 },
                          "organization": "Vanguard Total Stock Market Index Fund",
                          "pctHeld": { "raw": 0.0321 },
                          "position": { "raw": 186900000 },
                          "value": { "raw": 68123000000 },
                          "pctChange": { "raw": -0.0042 }
                        }
                      ]
                    }
                  }
                ],
                "error": null
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
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
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetHoldersAsync("GOOGL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("modules=institutionOwnership%2CfundOwnership%2CmajorHoldersBreakdown", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query, StringComparison.Ordinal);

        Assert.Equal("GOOGL", result.Symbol);
        Assert.NotNull(result.MajorHolders);
        Assert.Equal(0.0056m, result.MajorHolders!.InsidersPercentHeld);
        Assert.Equal(0.7911m, result.MajorHolders.InstitutionsPercentHeld);
        Assert.Equal(0.7954m, result.MajorHolders.InstitutionsFloatPercentHeld);
        Assert.Equal(6118, result.MajorHolders.InstitutionsCount);

        Assert.Single(result.InstitutionalHolders);
        Assert.Equal("Blackrock Inc.", result.InstitutionalHolders[0].Holder);
        Assert.Equal(new DateOnly(2026, 3, 31), result.InstitutionalHolders[0].ReportDate);
        Assert.Equal(0.0767m, result.InstitutionalHolders[0].PercentHeld);
        Assert.Equal(446980992L, result.InstitutionalHolders[0].Shares);
        Assert.Equal(162817300510m, result.InstitutionalHolders[0].Value);
        Assert.Equal(0.0113m, result.InstitutionalHolders[0].PercentChange);

        Assert.Single(result.MutualFundHolders);
        Assert.Equal("Vanguard Total Stock Market Index Fund", result.MutualFundHolders[0].Holder);
        Assert.Equal(186900000L, result.MutualFundHolders[0].Shares);
    }

    [Fact]
    public async Task GetHoldersAsync_ReturnsEmptyCollectionsWhenModulesAreMissing()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
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

        var result = await client.GetHoldersAsync("NOPE");

        Assert.Null(result.MajorHolders);
        Assert.Empty(result.InstitutionalHolders);
        Assert.Empty(result.MutualFundHolders);
    }

    [Fact]
    public async Task TickerFacade_GetHoldersAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "majorHoldersBreakdown": {
                      "institutionsCount": { "raw": 123 }
                    }
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
        using var ticker = new Ticker("GOOGL", client);

        var result = await ticker.GetHoldersAsync();

        Assert.NotNull(result.MajorHolders);
        Assert.Equal(123, result.MajorHolders!.InstitutionsCount);
    }
}