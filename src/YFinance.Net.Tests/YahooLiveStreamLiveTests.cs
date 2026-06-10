namespace YFinance.Net.Tests;

public sealed class YahooLiveStreamLiveTests
{
    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task SubscribeAsync_BtcUsd_ReceivesLiveMessage()
    {
        await using var stream = new YahooLiveStream(new YahooLiveStreamOptions
        {
            ChannelCapacity = 128,
            ResubscribeInterval = TimeSpan.FromSeconds(30)
        });

        await stream.SubscribeAsync(new[] { "BTC-USD" });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var update = await stream.Messages.ReadAsync(cts.Token);

        Assert.Equal("BTC-USD", update.Symbol);
        Assert.True(update.Price > 0);
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task ReadAllAsync_BtcUsd_YieldsLiveMessage()
    {
        await using var stream = new YahooLiveStream(new YahooLiveStreamOptions
        {
            ChannelCapacity = 128,
            ResubscribeInterval = TimeSpan.FromSeconds(30)
        });

        await stream.SubscribeAsync(new[] { "BTC-USD" });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await foreach (var update in stream.ReadAllAsync(cts.Token))
        {
            Assert.Equal("BTC-USD", update.Symbol);
            Assert.True(update.Price > 0);
            return;
        }

        throw new Xunit.Sdk.XunitException("Expected ReadAllAsync to yield at least one live BTC-USD update.");
    }
}