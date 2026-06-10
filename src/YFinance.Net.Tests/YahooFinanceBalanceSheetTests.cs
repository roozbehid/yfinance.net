using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceBalanceSheetTests
{
    [Fact]
    public async Task GetBalanceSheetAsync_ParsesAnnualRowsAndBuildsTimeseriesQuery()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualTotalAssets"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualTotalAssets": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 352583000000 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 364980000000 }
                      }
                    ]
                  },
                  {
                    "meta": {
                      "symbol": ["AAPL"],
                      "type": ["annualNetPPE"]
                    },
                    "timestamp": [1696032000, 1727654400],
                    "annualNetPPE": [
                      {
                        "asOfDate": "2023-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 43715000000 }
                      },
                      {
                        "asOfDate": "2024-09-30",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 45680000000 }
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

        var result = await client.GetBalanceSheetAsync("AAPL");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal("query2.finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/ws/fundamentals-timeseries/v1/finance/timeseries/AAPL", capturedRequest.RequestUri.AbsolutePath);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest.RequestUri.Query);
        Assert.Contains("symbol=AAPL", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("type=annualTreasurySharesNumber", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualTotalAssets", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("annualNetPPE", decodedQuery, StringComparison.Ordinal);

        Assert.Equal("AAPL", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Annual, result.Frequency);
        Assert.Equal(2, result.Periods.Length);
        Assert.Equal(new DateOnly(2024, 9, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2023, 9, 30), result.Periods[1].AsOfDate);
        Assert.Equal("12M", result.Periods[0].PeriodType);
        Assert.Equal(2, result.LineItems.Length);
        Assert.Equal("TotalAssets", result.LineItems[0].Key);
        Assert.Equal("USD", result.LineItems[0].CurrencyCode);
        Assert.Equal(364980000000m, result.LineItems[0].Values[0]);
        Assert.Equal(352583000000m, result.LineItems[0].Values[1]);
        Assert.Equal("NetPPE", result.LineItems[1].Key);
        Assert.Equal(45680000000m, result.LineItems[1].Values[0]);
        Assert.Equal(43715000000m, result.LineItems[1].Values[1]);
    }

    [Fact]
    public async Task GetBalanceSheetAsync_ParsesQuarterlyRows()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["MSFT"],
                      "type": ["quarterlyCurrentAssets"]
                    },
                    "quarterlyCurrentAssets": [
                      {
                        "asOfDate": "2025-03-31",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 159734000000 }
                      },
                      {
                        "asOfDate": "2025-06-30",
                        "periodType": "3M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 161280000000 }
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

        var result = await client.GetBalanceSheetAsync("MSFT", FinancialStatementFrequency.Quarterly);

        Assert.NotNull(capturedRequest);
        var decodedQuery = Uri.UnescapeDataString(capturedRequest!.RequestUri!.Query);
        Assert.Contains("type=quarterlyTreasurySharesNumber", decodedQuery, StringComparison.Ordinal);
        Assert.Contains("quarterlyCurrentAssets", decodedQuery, StringComparison.Ordinal);
        Assert.Equal("MSFT", result.Symbol);
        Assert.Equal(FinancialStatementFrequency.Quarterly, result.Frequency);
        Assert.Single(result.LineItems);
        Assert.Equal(new DateOnly(2025, 6, 30), result.Periods[0].AsOfDate);
        Assert.Equal(new DateOnly(2025, 3, 31), result.Periods[1].AsOfDate);
        Assert.Equal(161280000000m, result.LineItems[0].Values[0]);
        Assert.Equal(159734000000m, result.LineItems[0].Values[1]);
    }

    [Fact]
    public async Task TickerFacade_GetBalanceSheetAsync_DelegatesThroughClient()
    {
        const string payload = """
            {
              "timeseries": {
                "result": [
                  {
                    "meta": {
                      "symbol": ["IBM"],
                      "type": ["annualStockholdersEquity"]
                    },
                    "annualStockholdersEquity": [
                      {
                        "asOfDate": "2024-12-31",
                        "periodType": "12M",
                        "currencyCode": "USD",
                        "reportedValue": { "raw": 26118000000 }
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

        var result = await ticker.GetBalanceSheetAsync();

        Assert.Single(result.LineItems);
        Assert.Equal("StockholdersEquity", result.LineItems[0].Key);
        Assert.Equal(26118000000m, result.LineItems[0].Values[0]);
    }
}