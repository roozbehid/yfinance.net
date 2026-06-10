using System.Net;
using System.Text;
using System.Text.Json;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceScreenerTests
{
    [Fact]
    public async Task GetPredefinedScreenerAsync_ParsesYahooResponse()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "id": "screen-1",
                    "title": "Day Gainers",
                    "description": "Discover the equities with the greatest gains in the trading day.",
                    "canonicalName": "DAY_GAINERS",
                    "criteriaMeta": {
                      "size": 2,
                      "offset": 0,
                      "sortField": "percentchange",
                      "sortType": "DESC",
                      "quoteType": "EQUITY"
                    },
                    "start": 0,
                    "count": 2,
                    "total": 347,
                    "quotes": [
                      {
                        "symbol": "NUVL",
                        "shortName": "Nuvalent, Inc.",
                        "longName": "Nuvalent, Inc.",
                        "exchange": "NMS",
                        "fullExchangeName": "NasdaqGS",
                        "quoteType": "EQUITY",
                        "typeDisp": "Equity",
                        "currency": "USD",
                        "regularMarketPrice": 123.25,
                        "regularMarketChange": 34.760002,
                        "regularMarketChangePercent": 39.28128,
                        "regularMarketVolume": 51678614,
                        "marketCap": 9736493056
                      },
                      {
                        "shortName": "Missing Symbol"
                      }
                    ],
                    "isPremium": false,
                    "iconUrl": "https://example.com/day-gainers.png"
                  }
                ],
                "error": null
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetPredefinedScreenerAsync(PredefinedScreenerId.DayGainers, new PredefinedScreenerOptions
        {
            Count = 2
        });

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Contains("scrIds=day_gainers", capturedRequest.RequestUri.Query);
        Assert.Contains("count=2", capturedRequest.RequestUri.Query);
        Assert.Contains("offset=0", capturedRequest.RequestUri.Query);
        Assert.Contains("formatted=false", capturedRequest.RequestUri.Query);

        Assert.Equal("Day Gainers", result.Title);
        Assert.Equal("DAY_GAINERS", result.CanonicalName);
        Assert.Equal(347, result.Total);
        Assert.True(result.HasMore);
        Assert.Equal(2, result.NextOffset);
        Assert.Equal("percentchange", result.Criteria.SortField);
        Assert.Equal("DESC", result.Criteria.SortType);
        Assert.Single(result.Quotes);
        Assert.Equal("NUVL", result.Quotes[0].Symbol);
        Assert.Equal("Nuvalent, Inc.", result.Quotes[0].ShortName);
        Assert.Equal(123.25m, result.Quotes[0].RegularMarketPrice);
        Assert.Equal(51678614L, result.Quotes[0].RegularMarketVolume);
        Assert.Equal(9736493056m, result.Quotes[0].MarketCap);
    }

    [Fact]
    public async Task ScreenAsync_SendsCustomQueryBodyAndParsesResponse()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "start": 0,
                    "count": 2,
                    "total": 3154,
                    "criteriaMeta": {
                      "size": 2,
                      "offset": 0,
                      "sortField": "ticker",
                      "sortType": "DESC",
                      "quoteType": "EQUITY"
                    },
                    "quotes": [
                      {
                        "symbol": "ZZHGF",
                        "shortName": "ZHONGAN ONLINE P & C INS CO LTD",
                        "fullExchangeName": "OTC Markets OTCPK",
                        "quoteType": "EQUITY",
                        "typeDisp": "Equity",
                        "regularMarketPrice": 2.51,
                        "regularMarketChangePercent": 67.333336,
                        "regularMarketVolume": 22,
                        "marketCap": 4228880384
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        HttpRequestMessage? screenerRequest = null;
        string? screenerRequestBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri is not null && request.RequestUri.AbsoluteUri.Contains("/v1/test/getcrumb", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
                };
            }

            screenerRequest = request;
      screenerRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.ScreenAsync(
            ScreenerQuery.And(
            ScreenerQuery.GreaterThan(ScreenerFields.Trading.PercentChange, 3),
            ScreenerQuery.Equal(ScreenerFields.Common.Region, "us")),
            new ScreenerOptions
            {
                Count = 2,
                QuoteType = ScreenerQuoteType.Equity
          }.WithSort(ScreenerFields.Common.Symbol));

        Assert.NotNull(screenerRequest);
        Assert.Equal(HttpMethod.Post, screenerRequest!.Method);
        Assert.Equal("query1.finance.yahoo.com", screenerRequest.RequestUri!.Host);
        Assert.Contains("crumb=crumb-123", screenerRequest.RequestUri.Query);

        Assert.NotNull(screenerRequestBody);
        using var bodyJson = JsonDocument.Parse(screenerRequestBody!);
        Assert.Equal(0, bodyJson.RootElement.GetProperty("offset").GetInt32());
        Assert.Equal(2, bodyJson.RootElement.GetProperty("size").GetInt32());
        Assert.Equal("ticker", bodyJson.RootElement.GetProperty("sortField").GetString());
        Assert.Equal("DESC", bodyJson.RootElement.GetProperty("sortType").GetString());
        Assert.Equal("EQUITY", bodyJson.RootElement.GetProperty("quoteType").GetString());

        var queryJson = bodyJson.RootElement.GetProperty("query");
        Assert.Equal("AND", queryJson.GetProperty("operator").GetString());
        var operands = queryJson.GetProperty("operands");
        Assert.Equal(2, operands.GetArrayLength());
        Assert.Equal("GT", operands[0].GetProperty("operator").GetString());
        Assert.Equal("percentchange", operands[0].GetProperty("operands")[0].GetString());
        Assert.Equal(3, operands[0].GetProperty("operands")[1].GetInt32());
        Assert.Equal("EQ", operands[1].GetProperty("operator").GetString());
        Assert.Equal("region", operands[1].GetProperty("operands")[0].GetString());
        Assert.Equal("us", operands[1].GetProperty("operands")[1].GetString());

        Assert.Equal(3154, result.Total);
        Assert.Equal("ticker", result.Criteria.SortField);
        Assert.Single(result.Quotes);
        Assert.Equal("ZZHGF", result.Quotes[0].Symbol);
        Assert.Equal(2.51m, result.Quotes[0].RegularMarketPrice);
        Assert.Equal(4228880384m, result.Quotes[0].MarketCap);
      }

    [Fact]
    public async Task ScreenAsync_DayGainersPreset_UsesPresetDefinitionBody()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "start": 0,
                    "count": 2,
                    "total": 100,
                    "quotes": [
                      {
                        "symbol": "NUVL"
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        string? screenerRequestBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri is not null && request.RequestUri.AbsoluteUri.Contains("/v1/test/getcrumb", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
                };
            }

            screenerRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.ScreenAsync(ScreenerPresets.DayGainers().WithCount(2));

        Assert.NotNull(screenerRequestBody);
        using var bodyJson = JsonDocument.Parse(screenerRequestBody!);
        Assert.Equal("percentchange", bodyJson.RootElement.GetProperty("sortField").GetString());
        Assert.Equal("DESC", bodyJson.RootElement.GetProperty("sortType").GetString());
        Assert.Equal("EQUITY", bodyJson.RootElement.GetProperty("quoteType").GetString());

        var queryJson = bodyJson.RootElement.GetProperty("query");
        Assert.Equal("AND", queryJson.GetProperty("operator").GetString());
        var operands = queryJson.GetProperty("operands");
        Assert.Equal(5, operands.GetArrayLength());
        Assert.Equal("GT", operands[0].GetProperty("operator").GetString());
        Assert.Equal("percentchange", operands[0].GetProperty("operands")[0].GetString());
        Assert.Equal(3m, operands[0].GetProperty("operands")[1].GetDecimal());
        Assert.Equal("EQ", operands[1].GetProperty("operator").GetString());
        Assert.Equal("region", operands[1].GetProperty("operands")[0].GetString());
        Assert.Equal("us", operands[1].GetProperty("operands")[1].GetString());
        Assert.Equal("GTE", operands[2].GetProperty("operator").GetString());
        Assert.Equal("intradaymarketcap", operands[2].GetProperty("operands")[0].GetString());
        Assert.Equal(2_000_000_000m, operands[2].GetProperty("operands")[1].GetDecimal());
        Assert.Equal("NUVL", result.Quotes[0].Symbol);
    }

    [Fact]
    public void DayLosersPreset_WithCountAndOffset_UpdatesOnlyPagingOptions()
    {
        var definition = ScreenerPresets.DayLosers().WithCount(50).WithOffset(25);

        Assert.Equal(50, definition.Options.Count);
        Assert.Equal(25, definition.Options.Offset);
        Assert.Equal(ScreenerQuoteType.Equity, definition.Options.QuoteType);
        Assert.Equal("percentchange", definition.Options.SortField);
        Assert.Equal(ScreenerSortOrder.Ascending, definition.Options.SortOrder);
    }

    [Fact]
    public async Task ScreenAsync_HighYieldBondPreset_UsesMutualFundDefinitionBody()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "start": 0,
                    "count": 1,
                    "total": 57,
                    "quotes": [
                      {
                        "symbol": "SPHIX"
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        string? screenerRequestBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri is not null && request.RequestUri.AbsoluteUri.Contains("/v1/test/getcrumb", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
                };
            }

            screenerRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.ScreenAsync(ScreenerPresets.HighYieldBond().WithCount(1));

        Assert.NotNull(screenerRequestBody);
        using var bodyJson = JsonDocument.Parse(screenerRequestBody!);
        Assert.Equal("fundnetassets", bodyJson.RootElement.GetProperty("sortField").GetString());
        Assert.Equal("DESC", bodyJson.RootElement.GetProperty("sortType").GetString());
        Assert.Equal("MUTUALFUND", bodyJson.RootElement.GetProperty("quoteType").GetString());

        var operands = bodyJson.RootElement.GetProperty("query").GetProperty("operands");
        Assert.Equal(6, operands.GetArrayLength());
        Assert.Equal("OR", operands[0].GetProperty("operator").GetString());
        Assert.Equal("LT", operands[1].GetProperty("operator").GetString());
        Assert.Equal("initialinvestment", operands[1].GetProperty("operands")[0].GetString());
        Assert.Equal("OR", operands[3].GetProperty("operator").GetString());
        Assert.Equal("EQ", operands[4].GetProperty("operator").GetString());
        Assert.Equal("High Yield Bond", operands[4].GetProperty("operands")[1].GetString());
        Assert.Equal("NAS", operands[5].GetProperty("operands")[1].GetString());
        Assert.Equal("SPHIX", result.Quotes[0].Symbol);
    }

    [Fact]
    public async Task ScreenAsync_TopPerformingEtfsPreset_UsesEtfDefinitionBody()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "start": 0,
                    "count": 1,
                    "total": 539,
                    "quotes": [
                      {
                        "symbol": "VTI"
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        string? screenerRequestBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri is not null && request.RequestUri.AbsoluteUri.Contains("/v1/test/getcrumb", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("crumb-123", Encoding.UTF8, "text/plain")
                };
            }

            screenerRequestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.ScreenAsync(ScreenerPresets.TopPerformingEtfs().WithCount(1));

        Assert.NotNull(screenerRequestBody);
        using var bodyJson = JsonDocument.Parse(screenerRequestBody!);
        Assert.Equal("annualreportnetexpenseratio", bodyJson.RootElement.GetProperty("sortField").GetString());
        Assert.Equal("ASC", bodyJson.RootElement.GetProperty("sortType").GetString());
        Assert.Equal("ETF", bodyJson.RootElement.GetProperty("quoteType").GetString());

        var operands = bodyJson.RootElement.GetProperty("query").GetProperty("operands");
        Assert.Equal(3, operands.GetArrayLength());
        Assert.Equal("EQ", operands[0].GetProperty("operator").GetString());
        Assert.Equal("region", operands[0].GetProperty("operands")[0].GetString());
        Assert.Equal("us", operands[0].GetProperty("operands")[1].GetString());
        Assert.Equal("OR", operands[1].GetProperty("operator").GetString());
        Assert.Equal("GT", operands[2].GetProperty("operator").GetString());
        Assert.Equal("VTI", result.Quotes[0].Symbol);
    }

      [Fact]
      public void AnyOf_WithDiscoverableFieldCatalog_SerializesToOrOfEqClauses()
      {
        var query = ScreenerQuery.AnyOf(ScreenerFields.Common.Exchange, "NMS", "NYQ");

        using var json = JsonDocument.Parse(query.Serialize());
        Assert.Equal("OR", json.RootElement.GetProperty("operator").GetString());
        var operands = json.RootElement.GetProperty("operands");
        Assert.Equal(2, operands.GetArrayLength());
        Assert.Equal("EQ", operands[0].GetProperty("operator").GetString());
        Assert.Equal("exchange", operands[0].GetProperty("operands")[0].GetString());
        Assert.Equal("NMS", operands[0].GetProperty("operands")[1].GetString());
        Assert.Equal("NYQ", operands[1].GetProperty("operands")[1].GetString());
      }

    [Fact]
    public async Task GetPredefinedScreenerAsync_ThrowsWhenYahooReturnsStructuredError()
    {
        const string payload = """
            {
              "finance": {
                "result": [],
                "error": {
                  "code": "Bad Request",
                  "description": "broken"
                }
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetPredefinedScreenerAsync("day_gainers", new PredefinedScreenerOptions
        {
          Count = 25
        }));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
      public async Task ScreenerFacade_GetAsync_DelegatesThroughYahooFinanceClient()
    {
        const string payload = """
            {
              "finance": {
                "result": [
                  {
                    "title": "Day Gainers",
                    "count": 1,
                    "total": 1,
                    "quotes": [
                      {
                        "symbol": "NUVL"
                      }
                    ]
                  }
                ],
                "error": null
              }
            }
            """;

        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        }));
        using var client = new YahooFinanceClient(httpClient);
        using var screener = new Screener(PredefinedScreenerId.DayGainers, client);

        var result = await screener.GetAsync(count: 1);

        Assert.Equal("Day Gainers", result.Title);
        Assert.Single(result.Quotes);
        Assert.Equal("NUVL", result.Quotes[0].Symbol);
    }

      [Fact]
      public async Task GetPredefinedScreenerAsync_ThrowsForCountOutsideYahooLimit()
      {
        using var httpClient = new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Request should not be sent.")));
        using var client = new YahooFinanceClient(httpClient);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => client.GetPredefinedScreenerAsync(PredefinedScreenerId.DayGainers, new PredefinedScreenerOptions
        {
          Count = 251
        }));
      }
}