using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceAnalystInsightsTests
{
    [Fact]
    public async Task GetAnalystInsightsAsync_ParsesRecommendationsAndEarningsAnalytics()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "price": {
                      "symbol": "AAPL"
                    },
                    "financialData": {
                      "currentPrice": { "raw": 203.27 },
                      "targetLowPrice": { "raw": 180.0 },
                      "targetHighPrice": { "raw": 260.0 },
                      "targetMeanPrice": { "raw": 225.5 },
                      "targetMedianPrice": { "raw": 220.0 }
                    },
                    "recommendationTrend": {
                      "trend": [
                        {
                          "period": "0m",
                          "strongBuy": 7,
                          "buy": 23,
                          "hold": 15,
                          "sell": 1,
                          "strongSell": 2
                        }
                      ]
                    },
                    "upgradeDowngradeHistory": {
                      "history": [
                        {
                          "epochGradeDate": 1781014385,
                          "firm": "TD Cowen",
                          "toGrade": "Buy",
                          "fromGrade": "Buy",
                          "action": "main",
                          "priceTargetAction": "Raises",
                          "currentPriceTarget": 350.0,
                          "priorPriceTarget": 335.0
                        }
                      ]
                    },
                    "earningsHistory": {
                      "history": [
                        {
                          "epsActual": { "raw": 1.57 },
                          "epsEstimate": { "raw": 1.42572 },
                          "epsDifference": { "raw": 0.14 },
                          "surprisePercent": { "raw": 0.1012 },
                          "quarter": { "raw": 1751241600, "fmt": "2025-06-30" },
                          "currency": "USD",
                          "period": "-4q"
                        }
                      ]
                    },
                    "earningsTrend": {
                      "trend": [
                        {
                          "period": "0q",
                          "endDate": "2026-06-30",
                          "growth": { "raw": 0.20729999 },
                          "earningsEstimate": {
                            "avg": { "raw": 1.89541 },
                            "low": { "raw": 1.83 },
                            "high": { "raw": 1.99 },
                            "yearAgoEps": { "raw": 1.57 },
                            "numberOfAnalysts": { "raw": 32 },
                            "growth": { "raw": 0.20729999 },
                            "earningsCurrency": "USD"
                          },
                          "revenueEstimate": {
                            "avg": { "raw": 109023928020 },
                            "low": { "raw": 107501000000 },
                            "high": { "raw": 112168000000 },
                            "numberOfAnalysts": { "raw": 28 },
                            "yearAgoRevenue": { "raw": 94036000000 },
                            "growth": { "raw": 0.1594 },
                            "revenueCurrency": "USD"
                          },
                          "epsTrend": {
                            "current": { "raw": 1.89541 },
                            "7daysAgo": { "raw": 1.89541 },
                            "30daysAgo": { "raw": 1.89077 },
                            "60daysAgo": { "raw": 1.72877 },
                            "90daysAgo": { "raw": 1.724 },
                            "epsTrendCurrency": "USD"
                          },
                          "epsRevisions": {
                            "upLast7days": { "raw": 0 },
                            "upLast30days": { "raw": 24 },
                            "downLast30days": { "raw": 0 },
                            "downLast7Days": { "raw": 0 },
                            "downLast90days": { "raw": 1 },
                            "epsRevisionsCurrency": "USD"
                          }
                        }
                      ]
                    },
                    "industryTrend": {
                      "estimates": [
                        { "period": "0q", "growth": 0.12 }
                      ]
                    },
                    "sectorTrend": {
                      "estimates": [
                        { "period": "0q", "growth": 0.18 }
                      ]
                    },
                    "indexTrend": {
                      "estimates": [
                        { "period": "0q", "growth": 0.26 }
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

        var result = await client.GetAnalystInsightsAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("financialData%2CrecommendationTrend%2CupgradeDowngradeHistory%2CearningsHistory%2CearningsTrend%2CindustryTrend%2CsectorTrend%2CindexTrend", capturedRequest.RequestUri.Query);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(203.27m, result.PriceTargets.Current);
        Assert.Equal(225.5m, result.PriceTargets.Mean);
        Assert.Single(result.Recommendations);
        Assert.Equal("0m", result.Recommendations[0].Period);
        Assert.Equal(7, result.Recommendations[0].StrongBuy);
        Assert.Single(result.UpgradesDowngrades);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1781014385), result.UpgradesDowngrades[0].GradeDate);
        Assert.Equal("TD Cowen", result.UpgradesDowngrades[0].Firm);
        Assert.Equal("Raises", result.UpgradesDowngrades[0].PriceTargetAction);
        Assert.Single(result.EarningsHistory);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1751241600), result.EarningsHistory[0].Quarter);
        Assert.Single(result.EarningsEstimates);
        Assert.Equal(32, result.EarningsEstimates[0].NumberOfAnalysts);
        Assert.Single(result.RevenueEstimates);
        Assert.Equal(109023928020m, result.RevenueEstimates[0].Average);
        Assert.Single(result.EpsTrends);
        Assert.Equal(1.724m, result.EpsTrends[0].NinetyDaysAgo);
        Assert.Single(result.EpsRevisions);
        Assert.Equal(24, result.EpsRevisions[0].UpLast30Days);
        Assert.Single(result.GrowthEstimates);
        Assert.Equal(0.20729999m, result.GrowthEstimates[0].Stock);
        Assert.Equal(0.12m, result.GrowthEstimates[0].Industry);
        Assert.Equal(0.18m, result.GrowthEstimates[0].Sector);
        Assert.Equal(0.26m, result.GrowthEstimates[0].Index);
    }

    [Fact]
    public async Task GetAnalystInsightsAsync_ReturnsEmptyCollectionsWhenAnalyticsAreMissing()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "price": {
                      "symbol": "NOPE"
                    },
                    "financialData": {}
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

        var result = await client.GetAnalystInsightsAsync("NOPE");

        Assert.Equal("NOPE", result.Symbol);
        Assert.Empty(result.Recommendations);
        Assert.Empty(result.UpgradesDowngrades);
        Assert.Empty(result.EarningsHistory);
        Assert.Empty(result.EarningsEstimates);
        Assert.Empty(result.RevenueEstimates);
        Assert.Empty(result.EpsTrends);
        Assert.Empty(result.EpsRevisions);
        Assert.Empty(result.GrowthEstimates);
    }

    [Fact]
    public async Task TickerFacade_GetAnalystInsightsAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "quoteSummary": {
                "result": [
                  {
                    "price": {
                      "symbol": "AAPL"
                    },
                    "upgradeDowngradeHistory": {
                      "history": [
                        {
                          "epochGradeDate": 1781014385,
                          "firm": "TD Cowen",
                          "toGrade": "Buy",
                          "fromGrade": "Hold",
                          "action": "up"
                        }
                      ]
                    },
                    "recommendationTrend": {
                      "trend": [
                        {
                          "period": "0m",
                          "strongBuy": 1,
                          "buy": 2,
                          "hold": 3,
                          "sell": 4,
                          "strongSell": 5
                        }
                      ]
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
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetAnalystInsightsAsync();

        Assert.Single(result.Recommendations);
        Assert.Equal(5, result.Recommendations[0].StrongSell);
        Assert.Single(result.UpgradesDowngrades);
        Assert.Equal("TD Cowen", result.UpgradesDowngrades[0].Firm);
    }
}