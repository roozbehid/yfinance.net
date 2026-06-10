using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceDomainTests
{
    [Fact]
    public void Sector_NormalizesRegion()
    {
        using var sector = new Sector("technology", " gb ");

        Assert.Equal("GB", sector.Region);
    }

    [Fact]
    public void Industry_NormalizesRegion()
    {
        using var industry = new Industry("software-infrastructure", "de");

        Assert.Equal("DE", industry.Region);
    }

    [Fact]
    public async Task GetSectorAsync_ParsesCommonAndSectorSpecificFields()
    {
        const string payload = """
            {
              "data": {
                "name": "Technology",
                "symbol": "^YH311",
                "overview": {
                  "companiesCount": 50,
                  "marketCap": { "raw": 12345.67 },
                  "messageBoardId": "finmb_123",
                  "description": "Sector description",
                  "industriesCount": 3,
                  "employeeCount": { "raw": 1000 },
                  "marketWeight": { "raw": 0.42 }
                },
                "topCompanies": [
                  {
                    "symbol": "NVDA",
                    "name": "NVIDIA Corporation",
                    "rating": "Strong Buy",
                    "marketWeight": { "raw": 0.18 },
                    "marketCap": { "raw": 3300000000000 },
                    "lastPrice": { "raw": 142.5 },
                    "targetPrice": { "raw": 180.0 },
                    "ytdReturn": { "raw": 0.11 },
                    "regMarketChangePercent": { "raw": -0.01 }
                  }
                ],
                "researchReports": [
                  {
                    "id": "report-1",
                    "headHtml": "Analyst Report",
                    "provider": "Argus Research",
                    "targetPrice": 375.0,
                    "targetPriceStatus": "Increased",
                    "investmentRating": "Bullish",
                    "reportDate": "2026-06-09T17:41:15Z",
                    "reportTitle": "Apple note",
                    "reportType": "Analyst Report"
                  }
                ],
                "topETFs": [
                  { "symbol": "VGT", "name": "Vanguard Information Tech ETF" }
                ],
                "topMutualFunds": [
                  { "symbol": "VITAX", "name": "Vanguard Information Technology" }
                ],
                "industries": [
                  { "name": "All Industries", "marketWeight": { "raw": 1.0 } },
                  { "key": "software-infrastructure", "name": "Software - Infrastructure", "symbol": "^YH31110030", "marketWeight": { "raw": 0.20 } }
                ]
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateProtectedEndpointHttpClient(payload, request => capturedRequest = request);
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetSectorAsync("technology", "gb");

        Assert.NotNull(capturedRequest);
        Assert.Equal("query1.finance.yahoo.com", capturedRequest!.RequestUri!.Host);
        Assert.Contains("region=GB", capturedRequest.RequestUri.Query);
        Assert.Contains("crumb=crumb-123", capturedRequest.RequestUri.Query);

        Assert.Equal("technology", result.Key);
        Assert.Equal("GB", result.Region);
        Assert.Equal("Technology", result.Name);
        Assert.Equal("^YH311", result.Symbol);
        Assert.NotNull(result.Overview);
        Assert.Equal(50, result.Overview!.CompaniesCount);
        Assert.Equal(12345.67m, result.Overview.MarketCap);
        Assert.Single(result.TopCompanies);
        Assert.Equal("NVDA", result.TopCompanies[0].Symbol);
        Assert.Equal(0.18m, result.TopCompanies[0].MarketWeight);
        Assert.Single(result.ResearchReports);
        Assert.Equal("Argus Research", result.ResearchReports[0].Provider);
        Assert.Single(result.TopEtfs);
        Assert.Equal("VGT", result.TopEtfs[0].Symbol);
        Assert.Single(result.TopMutualFunds);
        Assert.Equal("VITAX", result.TopMutualFunds[0].Symbol);
        Assert.Single(result.Industries);
        Assert.Equal("software-infrastructure", result.Industries[0].Key);
    }

    [Fact]
    public async Task SectorFacade_GetAsync_DelegatesThroughYahooFinanceClient()
    {
        const string payload = """
            {
              "data": {
                "name": "Technology",
                "symbol": "^YH311",
                "topCompanies": [
                  {
                    "symbol": "AAPL",
                    "name": "Apple Inc.",
                    "rating": "Buy",
                    "marketWeight": { "raw": 0.16 }
                  }
                ]
              }
            }
            """;

        using var httpClient = CreateProtectedEndpointHttpClient(payload);
        using var client = new YahooFinanceClient(httpClient);
        using var sector = new Sector("technology", client: client);

        var result = await sector.GetAsync();

        Assert.Single(result.TopCompanies);
        Assert.Equal("AAPL", result.TopCompanies[0].Symbol);
    }

    [Fact]
    public async Task GetIndustryAsync_ParsesCommonAndIndustrySpecificFields()
    {
        const string payload = """
            {
              "data": {
                "name": "Software - Infrastructure",
                "symbol": "^YH31110030",
                "sectorKey": "technology",
                "sectorName": "Technology",
                "overview": {
                  "companiesCount": 50,
                  "marketCap": { "raw": 9999.5 },
                  "description": "Industry description",
                  "marketWeight": { "raw": 0.25 }
                },
                "topCompanies": [
                  {
                    "symbol": "MSFT",
                    "name": "Microsoft Corporation",
                    "rating": "Strong Buy",
                    "marketWeight": { "raw": 0.58 }
                  }
                ],
                "researchReports": [
                  {
                    "id": "report-2",
                    "provider": "Argus Research",
                    "reportDate": "2026-06-09T17:41:15Z"
                  }
                ],
                "topPerformingCompanies": [
                  {
                    "symbol": "TCGL",
                    "name": "TechCreate Group Ltd.",
                    "ytdReturn": { "raw": 32.2385 },
                    "lastPrice": { "raw": 172.84 },
                    "targetPrice": null
                  }
                ],
                "topGrowthCompanies": [
                  {
                    "symbol": "OSPN",
                    "name": "OneSpan Inc.",
                    "ytdReturn": { "raw": 0.088 },
                    "growthEstimate": { "raw": 2.0 }
                  }
                ]
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        using var httpClient = CreateProtectedEndpointHttpClient(payload, request => capturedRequest = request);
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetIndustryAsync("software-infrastructure", "de");

        Assert.NotNull(capturedRequest);
        Assert.Contains("region=DE", capturedRequest!.RequestUri!.Query);
        Assert.Equal("software-infrastructure", result.Key);
        Assert.Equal("DE", result.Region);
        Assert.Equal("technology", result.SectorKey);
        Assert.Equal("Technology", result.SectorName);
        Assert.Single(result.TopPerformingCompanies);
        Assert.Equal(32.2385m, result.TopPerformingCompanies[0].YearToDateReturn);
        Assert.Single(result.TopGrowthCompanies);
        Assert.Equal(2.0m, result.TopGrowthCompanies[0].GrowthEstimate);
    }

    [Fact]
    public async Task GetSectorAsync_ThrowsWhenYahooReturnsStructuredError()
    {
        const string payload = """
            {
              "finance": {
                "error": {
                  "code": "Unauthorized",
                  "description": "Invalid Crumb"
                }
              }
            }
            """;

        using var httpClient = CreateProtectedEndpointHttpClient(payload);
        using var client = new YahooFinanceClient(httpClient);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetSectorAsync("technology"));

        Assert.Contains("returned an error", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

  [Fact]
  public async Task GetSectorAsync_UsesDomainCacheByDefault()
  {
    const string payload = """
      {
        "data": {
        "name": "Technology",
        "symbol": "^YH311"
        }
      }
      """;

    var domainRequestCount = 0;
    using var httpClient = CreateProtectedEndpointHttpClient(payload, request =>
    {
      if (request.RequestUri!.AbsolutePath.Contains("/v1/finance/sectors/", StringComparison.OrdinalIgnoreCase))
      {
        domainRequestCount++;
      }
    });
    using var client = new YahooFinanceClient(httpClient);

    var first = await client.GetSectorAsync("technology", "US");
    var second = await client.GetSectorAsync("technology", "US");

    Assert.Equal(1, domainRequestCount);
    Assert.Equal(first.Name, second.Name);
  }

  [Fact]
  public async Task GetSectorAsync_BypassCache_HitsEndpointAgain()
  {
    const string payload = """
      {
        "data": {
        "name": "Technology",
        "symbol": "^YH311"
        }
      }
      """;

    var domainRequestCount = 0;
    using var httpClient = CreateProtectedEndpointHttpClient(payload, request =>
    {
      if (request.RequestUri!.AbsolutePath.Contains("/v1/finance/sectors/", StringComparison.OrdinalIgnoreCase))
      {
        domainRequestCount++;
      }
    });
    using var client = new YahooFinanceClient(httpClient);

    _ = await client.GetSectorAsync("technology", "US");
    _ = await client.GetSectorAsync("technology", "US", YFinanceCacheMode.BypassCache);

    Assert.Equal(2, domainRequestCount);
  }

  [Fact]
  public async Task GetSectorAsync_RefreshMode_UpdatesCachedValue()
  {
    const string refreshedPayload = """
      {
        "data": {
        "name": "Refreshed Technology",
        "symbol": "^YH311"
        }
      }
      """;

    var cacheStore = new TestCacheStore();
    cacheStore.Set(
      "domain:sector:US:technology",
      new SectorDetails("technology", "US", "Cached Technology", "^YH311", null, [], [], [], [], []),
      TimeSpan.FromMinutes(15));

    var domainRequestCount = 0;
    using var httpClient = CreateProtectedEndpointHttpClient(refreshedPayload, request =>
    {
      if (request.RequestUri!.AbsolutePath.Contains("/v1/finance/sectors/", StringComparison.OrdinalIgnoreCase))
      {
        domainRequestCount++;
      }
    });
    using var client = new YahooFinanceClient(
      httpClient,
      new YahooFinanceClientOptions
      {
        Cache = new YahooFinanceCacheOptions
        {
          Store = cacheStore,
          EnableDomainCache = true,
          DomainCacheTtl = TimeSpan.FromMinutes(15)
        }
      });

    var refreshed = await client.GetSectorAsync("technology", "US", YFinanceCacheMode.Refresh);
    var reused = await client.GetSectorAsync("technology", "US");

    Assert.Equal(1, domainRequestCount);
    Assert.Equal("Refreshed Technology", refreshed.Name);
    Assert.Equal(refreshed.Name, reused.Name);
  }

  [Fact]
  public async Task GetSectorAsync_UsesInjectedCacheStore()
  {
    var cacheStore = new TestCacheStore();
    cacheStore.Set(
      "domain:sector:US:technology",
      new SectorDetails("technology", "US", "Cached Technology", "^YH311", null, [], [], [], [], []),
      TimeSpan.FromMinutes(15));

    var domainRequestCount = 0;
    using var httpClient = CreateProtectedEndpointHttpClient("{}", request =>
    {
      if (request.RequestUri!.AbsolutePath.Contains("/v1/finance/sectors/", StringComparison.OrdinalIgnoreCase))
      {
        domainRequestCount++;
      }
    });
    using var client = new YahooFinanceClient(
      httpClient,
      new YahooFinanceClientOptions
      {
        Cache = new YahooFinanceCacheOptions
        {
          Store = cacheStore,
          EnableDomainCache = true,
          DomainCacheTtl = TimeSpan.FromMinutes(15)
        }
      });

    var result = await client.GetSectorAsync("technology", "US");

    Assert.Equal(0, domainRequestCount);
    Assert.Equal("Cached Technology", result.Name);
    Assert.True(cacheStore.TryGetCalls > 0);
  }

    private static HttpClient CreateProtectedEndpointHttpClient(string payload, Action<HttpRequestMessage>? onDomainRequest = null)
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

            onDomainRequest?.Invoke(request);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
    }

        private sealed class TestCacheStore : IYFinanceCacheStore
        {
          private readonly Dictionary<string, object?> _entries = new(StringComparer.Ordinal);

          public int TryGetCalls { get; private set; }

          public bool TryGetValue<T>(string key, out T? value)
          {
            TryGetCalls++;
            if (_entries.TryGetValue(key, out var stored) && stored is T typed)
            {
              value = typed;
              return true;
            }

            value = default;
            return false;
          }

          public void Set<T>(string key, T value, TimeSpan? ttl = null)
          {
            _entries[key] = value;
          }

          public void Remove(string key)
          {
            _entries.Remove(key);
          }
        }
}