using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class TickersLiveStreamTests
{
    private const string ValidBase64Message = "CgdCVEMtVVNEFYoMuUcYwLCVgIplIgNVU0QqA0NDQzApOAFFPWrEP0iAgOrxvANVx/25R12csrRHZYD8skR9/7i0R7ABgIDq8bwD2AEE4AGAgOrxvAPoAYCA6vG8A/IBA0JUQ4ECAAAAwPrjckGJAgAA2P5ZT3tC";

    [Fact]
    public async Task OpenLiveStreamAsync_SubscribesAllSymbolsAndReturnsDecodedUpdates()
    {
        var envelope = Encoding.UTF8.GetBytes($"{{\"message\":\"{ValidBase64Message}\"}}");
        var transport = new FakeWebSocketTransport(envelope);
        await using var stream = new YahooLiveStream(transport, new YahooLiveStreamOptions
        {
            ChannelCapacity = 4,
            ResubscribeInterval = TimeSpan.FromMinutes(5)
        });
        using var tickers = new Tickers(
            new[] { "BTC-USD", "ETH-USD" },
            new YahooFinanceClient(new HttpClient(new StubHttpMessageHandler(_ => throw new InvalidOperationException("Should not send HTTP request")))));

        await using var subscribed = await tickers.OpenLiveStreamAsync(stream);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var update = await subscribed.Messages.ReadAsync(cts.Token);

        Assert.Equal("BTC-USD", update.Symbol);
        Assert.True(update.Price > 0);
        var sent = transport.SentMessages.ToArray();
        Assert.Contains(sent, message => message.Contains("subscribe", StringComparison.OrdinalIgnoreCase) && message.Contains("BTC-USD", StringComparison.OrdinalIgnoreCase) && message.Contains("ETH-USD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("Category", "LiveYahoo")]
    public async Task OpenLiveStreamAsync_BtcUsd_ReceivesLiveMessageThroughFacade()
    {
        using var tickers = new Tickers(new[] { "BTC-USD" });
        await using var stream = await tickers.OpenLiveStreamAsync(new YahooLiveStreamOptions
        {
            ChannelCapacity = 128,
            ResubscribeInterval = TimeSpan.FromSeconds(30)
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var update = await stream.Messages.ReadAsync(cts.Token);

        Assert.Equal("BTC-USD", update.Symbol);
        Assert.True(update.Price > 0);
    }

    private sealed class FakeWebSocketTransport(byte[] firstMessage) : IYahooWebSocketTransport
    {
        private readonly Queue<byte[]> _messages = new([firstMessage]);

        public ConcurrentQueue<string> SentMessages { get; } = new();

        public WebSocketState State { get; private set; } = WebSocketState.None;

        public ValueTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            State = WebSocketState.Open;
            return ValueTask.CompletedTask;
        }

        public ValueTask SendTextAsync(string message, CancellationToken cancellationToken)
        {
            SentMessages.Enqueue(message);
            return ValueTask.CompletedTask;
        }

        public ValueTask<YahooWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_messages.TryDequeue(out var payload))
            {
                payload.CopyTo(buffer);
                return ValueTask.FromResult(new YahooWebSocketReceiveResult(payload.Length, true, WebSocketMessageType.Text, null, null));
            }

            return ValueTask.FromResult(new YahooWebSocketReceiveResult(0, true, WebSocketMessageType.Close, WebSocketCloseStatus.NormalClosure, "done"));
        }

        public ValueTask CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
        {
            State = WebSocketState.Closed;
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            State = WebSocketState.Closed;
            return ValueTask.CompletedTask;
        }
    }
}