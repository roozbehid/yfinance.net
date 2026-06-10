using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceInsiderDataTests
{
    [Fact]
    public async Task GetInsiderDataAsync_ParsesTransactionsPurchaseActivityAndRoster()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "insiderTransactions": {
                      "transactions": [
                        {
                          "shares": { "raw": 437500 },
                          "value": { "raw": 0 },
                          "filerUrl": "",
                          "transactionText": "Stock Gift at price 0.00 per share.",
                          "filerName": "SERGEY BRIN",
                          "filerRelation": "Director and Beneficial Owner of more than 10% of a Class of Security",
                          "moneyText": "",
                          "startDate": { "raw": 1771459200 },
                          "ownership": "D"
                        }
                      ]
                    },
                    "insiderHolders": {
                      "holders": [
                        {
                          "name": "SERGEY BRIN",
                          "relation": "Director and Beneficial Owner of more than 10% of a Class of Security",
                          "url": "",
                          "transactionDescription": "Stock Gift",
                          "latestTransDate": { "raw": 1771459200 },
                          "positionDirect": { "raw": 37469 },
                          "positionDirectDate": { "raw": 1771459200 }
                        }
                      ]
                    },
                    "netSharePurchaseActivity": {
                      "period": "6m",
                      "buyInfoCount": 4,
                      "buyInfoShares": 540508,
                      "buyPercentInsiderShares": 0.004,
                      "sellInfoCount": 2,
                      "sellInfoShares": 1200,
                      "sellPercentInsiderShares": 0.0,
                      "netInfoCount": 6,
                      "netInfoShares": 539308,
                      "netPercentInsiderShares": 0.004,
                      "netInstSharesBuying": -416152130,
                      "netInstBuyingPercent": -0.08803,
                      "totalInsiderShares": 142362992
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

        var result = await client.GetInsiderDataAsync("GOOGL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("modules=insiderTransactions%2CinsiderHolders%2CnetSharePurchaseActivity", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query, StringComparison.Ordinal);

        Assert.Equal("GOOGL", result.Symbol);
        Assert.Single(result.Transactions);
        Assert.Equal(new DateOnly(2026, 2, 19), result.Transactions[0].StartDate);
        Assert.Equal("SERGEY BRIN", result.Transactions[0].Insider);
        Assert.Equal(437500L, result.Transactions[0].Shares);
        Assert.Equal("D", result.Transactions[0].Ownership);
        Assert.Equal("Stock Gift at price 0.00 per share.", result.Transactions[0].Text);

        Assert.NotNull(result.PurchaseActivity);
        Assert.Equal("6m", result.PurchaseActivity!.Period);
        Assert.Equal(4, result.PurchaseActivity.BuyTransactionCount);
        Assert.Equal(540508L, result.PurchaseActivity.BuyShares);
        Assert.Equal(539308L, result.PurchaseActivity.NetShares);
        Assert.Equal(-416152130L, result.PurchaseActivity.NetInstitutionSharesBuying);

        Assert.Single(result.RosterHolders);
        Assert.Equal("SERGEY BRIN", result.RosterHolders[0].Name);
        Assert.Equal(new DateOnly(2026, 2, 19), result.RosterHolders[0].LatestTransactionDate);
        Assert.Equal(37469L, result.RosterHolders[0].SharesOwnedDirectly);
    }

    [Fact]
    public async Task GetInsiderDataAsync_ReturnsEmptyCollectionsWhenModulesAreMissing()
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

        var result = await client.GetInsiderDataAsync("NOPE");

        Assert.Empty(result.Transactions);
        Assert.Empty(result.RosterHolders);
        Assert.Null(result.PurchaseActivity);
    }

    [Fact]
    public async Task TickerFacade_GetInsiderDataAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "netSharePurchaseActivity": {
                      "period": "6m",
                      "netInfoShares": 10
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

        var result = await ticker.GetInsiderDataAsync();

        Assert.NotNull(result.PurchaseActivity);
        Assert.Equal("6m", result.PurchaseActivity!.Period);
        Assert.Equal(10L, result.PurchaseActivity.NetShares);
    }
}