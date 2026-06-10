using System.Net;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooFinanceTickerNewsTests
{
    [Fact]
    public async Task GetNewsAsync_ParsesResponseAndPostsTickerStreamRequest()
    {
        const string payload = """
            {
              "data": {
                "tickerStream": {
                  "stream": [
                    {
                      "id": "story-1",
                      "content": {
                        "id": "story-1",
                        "contentType": "STORY",
                        "title": "Apple launches ticker news",
                        "summary": "A short summary.",
                        "pubDate": "2026-06-09T12:34:56Z",
                        "isHosted": false,
                        "provider": {
                          "displayName": "Reuters"
                        },
                        "canonicalUrl": {
                          "url": "https://example.com/story-1"
                        },
                        "clickThroughUrl": {
                          "url": "https://example.com/click-1"
                        },
                        "thumbnail": {
                          "originalUrl": "https://example.com/thumb-1.jpg"
                        },
                        "metadata": {
                          "editorsPick": true
                        }
                      }
                    },
                    {
                      "ad": {
                        "id": "ad-1"
                      }
                    }
                  ]
                }
              }
            }
            """;

        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        using var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            capturedRequest = request;
            capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
        }));
        using var client = new YahooFinanceClient(httpClient);

        var result = await client.GetNewsAsync("AAPL", count: 2);

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest!.Method);
        Assert.Equal("finance.yahoo.com", capturedRequest.RequestUri!.Host);
        Assert.Equal("/xhr/ncp", capturedRequest.RequestUri.AbsolutePath);
        Assert.Contains("queryRef=latestNews", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.Contains("serviceKey=ncp_fin", capturedRequest.RequestUri.Query, StringComparison.Ordinal);
        Assert.NotNull(capturedBody);
        Assert.Contains("\"snippetCount\":2", capturedBody!, StringComparison.Ordinal);
        Assert.Contains("\"AAPL\"", capturedBody!, StringComparison.Ordinal);

        Assert.Single(result);
        Assert.Equal("story-1", result[0].Id);
        Assert.Equal("STORY", result[0].ContentType);
        Assert.Equal("Apple launches ticker news", result[0].Title);
        Assert.Equal("A short summary.", result[0].Summary);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T12:34:56Z"), result[0].PublishedAt);
        Assert.Equal("Reuters", result[0].Provider);
        Assert.Equal("https://example.com/story-1", result[0].CanonicalUrl);
        Assert.Equal("https://example.com/click-1", result[0].ClickThroughUrl);
        Assert.Equal("https://example.com/thumb-1.jpg", result[0].ThumbnailUrl);
        Assert.False(result[0].IsHosted);
        Assert.True(result[0].EditorsPick);
    }
}