using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceCashFlowTests
{
    [Fact]
    public async Task GetCashFlowAsync_ParsesAnnualRowsAndBuildsTimeseriesQuery()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualOperatingCashFlow"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualOperatingCashFlow": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 110543000000 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 118254000000 }
                      }
                    ]
                  },
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualCapitalExpenditure"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualCapitalExpenditure": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": -10959000000 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": -9447000000 }
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

        var result = await client.GetCashFlowAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/ws/fundamentals-timeseries/v1/finance/timeseries/AAPL", capturedRequest.RequestUri.AbsolutePath);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest.RequestUri.Query);
        Assert.Contains("symbol=AAPL", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("type=annualForeignSales", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualOperatingCashFlow", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualCapitalExpenditure", decodedQuery, StringComparison.Ordinal);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Annual, result.Frequency);
        Assert.Equal(2, result.Periods.Length);
        Assert.Equal(new DateOnly(2024, 9, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2023, 9, 30), result.Periods[1].AsOfDate);
        Assert.Equal("12M", result.Periods[0].PeriodType);
        Assert.Equal(2, result.LineItems.Length);
        Assert.Equal("OperatingCashFlow", result.LineItems[0].Key);
        Assert.Equal("USD", result.LineItems[0].CurrencyCode);
        Assert.Equal(118254000000m, result.LineItems[0].Values[0]);
        Assert.Equal(110543000000m, result.LineItems[0].Values[1]);
        Assert.Equal("CapitalExpenditure", result.LineItems[1].Key);
        Assert.Equal(-9447000000m, result.LineItems[1].Values[0]);
        Assert.Equal(-10959000000m, result.LineItems[1].Values[1]);
    }

    [Fact]
    public async Task GetCashFlowAsync_ParsesQuarterlyRows()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["MSFT"],
                      "type": ["quarterlyFreeCashFlow"]
                    },
                    "quarterlyFreeCashFlow": [
                      {
                        "asOfDate": "2025-03-31",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 20928000000 }
                      },
                      {
                        "asOfDate": "2025-06-30",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 25102000000 }
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

        var result = await client.GetCashFlowAsync("MSFT", FinancialStatementFrequency.Quarterly);

        Assert.NotNull(capturedRequest);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest!.RequestUri!.Query);
        Assert.Contains("type=quarterlyForeignSales", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("quarterlyFreeCashFlow", decodedQuery, StringComparison.Ordinal);
        Assert.Equal("MSFT", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Quarterly, result.Frequency);
        Assert.Single(result.LineItems);
        Assert.Equal(new DateOnly(2025, 6, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2025, 3, 31), result.Periods[1].AsOfDate);
        Assert.Equal(25102000000m, result.LineItems[0].Values[0]);
        Assert.Equal(20928000000m, result.LineItems[0].Values[1]);
    }

    [Fact]
    public async Task TickerFacade_GetCashFlowAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["IBM"],
                      "type": ["annualFreeCashFlow"]
                    },
                    "annualFreeCashFlow": [
                      {
                        "asOfDate": "2024-12-31",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 12987000000 }
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

        var result = await ticker.GetCashFlowAsync();

        Assert.Single(result.LineItems);
        Assert.Equal("FreeCashFlow", result.LineItems[0].Key);
        Assert.Equal(12987000000m, result.LineItems[0].Values[0]);
    }
}