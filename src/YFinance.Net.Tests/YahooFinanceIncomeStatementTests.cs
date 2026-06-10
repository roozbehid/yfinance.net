using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceIncomeStatementTests
{
    [Fact]
    public async Task GetIncomeStatementAsync_ParsesAnnualRowsAndBuildsTimeseriesQuery()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualTotalRevenue"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualTotalRevenue": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 383285000000 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 391035000000 }
                      }
                    ]
                  },
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualBasicEPS"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualBasicEPS": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 6.16 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 6.11 }
                      }
                    ]
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

        var result = await client.GetIncomeStatementAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/ws/fundamentals-timeseries/v1/finance/timeseries/AAPL", capturedRequest.RequestUri.AbsolutePath);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest.RequestUri.Query);
        Assert.Contains("symbol=AAPL", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("type=annualTaxEffectOfUnusualItems", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualBasicEPS", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualTotalRevenue", decodedQuery, StringComparison.Ordinal);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Annual, result.Frequency);
        Assert.Equal(2, result.Periods.Length);
        Assert.Equal(new DateOnly(2024, 9, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2023, 9, 30), result.Periods[1].AsOfDate);
        Assert.Equal("12M", result.Periods[0].PeriodType);
        Assert.Equal(2, result.LineItems.Length);
        Assert.Equal("TotalRevenue", result.LineItems[0].Key);
        Assert.Equal("USD", result.LineItems[0].CurrencyCode);
        Assert.Equal(391035000000m, result.LineItems[0].Values[0]);
        Assert.Equal(383285000000m, result.LineItems[0].Values[1]);
        Assert.Equal("BasicEPS", result.LineItems[1].Key);
        Assert.Equal(6.11m, result.LineItems[1].Values[0]);
        Assert.Equal(6.16m, result.LineItems[1].Values[1]);
    }

    [Fact]
    public async Task GetIncomeStatementAsync_ParsesQuarterlyRows()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["MSFT"],
                      "type": ["quarterlyTotalRevenue"]
                    },
                    "quarterlyTotalRevenue": [
                      {
                        "asOfDate": "2025-03-31",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 70066000000 }
                      },
                      {
                        "asOfDate": "2025-06-30",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 76441000000 }
                      }
                    ]
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

        var result = await client.GetIncomeStatementAsync("MSFT", FinancialStatementFrequency.Quarterly);

        Assert.NotNull(capturedRequest);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest!.RequestUri!.Query);
        Assert.Contains("type=quarterlyTaxEffectOfUnusualItems", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("quarterlyTotalRevenue", decodedQuery, StringComparison.Ordinal);
        Assert.Equal("MSFT", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Quarterly, result.Frequency);
        Assert.Single(result.LineItems);
        Assert.Equal(new DateOnly(2025, 6, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2025, 3, 31), result.Periods[1].AsOfDate);
        Assert.Equal(76441000000m, result.LineItems[0].Values[0]);
        Assert.Equal(70066000000m, result.LineItems[0].Values[1]);
    }

    [Fact]
    public async Task TickerFacade_GetIncomeStatementAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["IBM"],
                      "type": ["annualPretaxIncome"]
                    },
                    "annualPretaxIncome": [
                      {
                        "asOfDate": "2024-12-31",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 10521000000 }
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
        using var ticker = new Ticker("IBM", client);

        var result = await ticker.GetIncomeStatementAsync();

        Assert.Single(result.LineItems);
        Assert.Equal("PretaxIncome", result.LineItems[0].Key);
        Assert.Equal(10521000000m, result.LineItems[0].Values[0]);
    }

    [Fact]
    public async Task GetTrailingIncomeStatementAsync_ParsesTrailingRowsAndUsesConvenienceFacade()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["trailingTotalRevenue"]
                    },
                    "trailingTotalRevenue": [
                      {
                        "asOfDate": "2025-06-30",
                        "periodType": "TTM",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 408625000000 }
                      },
                      {
                        "asOfDate": "2026-03-31",
                        "periodType": "TTM",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 451442000000 }
                      }
                    ]
                  },
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["trailingPretaxIncome"]
                    },
                    "trailingPretaxIncome": [
                      {
                        "asOfDate": "2025-06-30",
                        "periodType": "TTM",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 129535000000 }
                      },
                      {
                        "asOfDate": "2026-03-31",
                        "periodType": "TTM",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 147670000000 }
                      }
                    ]
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
        using var ticker = new Ticker("AAPL", client);

        var result = await ticker.GetTrailingIncomeStatementAsync();

        Assert.NotNull(capturedRequest);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest!.RequestUri!.Query);
        Assert.Contains("type=trailingTaxEffectOfUnusualItems", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("trailingTotalRevenue", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("trailingPretaxIncome", decodedQuery, StringComparison.Ordinal);
        Assert.Equal(FinancialStatementFrequency.Trailing, result.Frequency);
        Assert.Equal(2, result.Periods.Length);
        Assert.Equal("TTM", result.Periods[0].PeriodType);
        Assert.Equal(new DateOnly(2026, 3, 31), result.Periods[0].AsOfDate);
        Assert.Equal("TotalRevenue", result.LineItems[0].Key);
        Assert.Equal(451442000000m, result.LineItems[0].Values[0]);
        Assert.Equal(408625000000m, result.LineItems[0].Values[1]);
        Assert.Equal("PretaxIncome", result.LineItems[1].Key);
        Assert.Equal(147670000000m, result.LineItems[1].Values[0]);
    }
}