using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace YFinance.Net.Tests;

public sealed class YahooLiveStreamTests
{
    private const string ValidBase64Message = "CgdCVEMtVVNEFYoMuUcYwLCVgIplIgNVU0QqA0NDQzApOAFFPWrEP0iAgOrxvANVx/25R12csrRHZYD8skR9/7i0R7ABgIDq8bwD2AEE4AGAgOrxvAPoAYCA6vG8A/IBA0JUQ4ECAAAAwPrjckGJAgAA2P5ZT3tC";

    [Fact]
    public void TryDecodeBase64_ValidPricingMessage_ReturnsTypedUpdate()
    {
        var decoded = YahooLiveMessageDecoder.TryDecodeBase64(ValidBase64Message, out var update);

        Assert.True(decoded);
        Assert.NotNull(update);
        Assert.Equal("BTC-USD", update!.Symbol);
        Assert.Equal("USD", update.Currency);
        Assert.Equal("CCC", update.Exchange);
        Assert.Equal(94745.08f, update.Price, 2);
        Assert.Equal(59712028672L, update.DayVolume);
        Assert.Equal(95227.555f, update.DayHigh, 3);
        Assert.Equal(92517.22f, update.DayLow, 2);
        Assert.Equal("BTC", update.FromCurrency);
        Assert.True(update.MarketCap > 0);
    }

    [Fact]
    public void TryDecodeBase64_InvalidBase64_ReturnsFalse()
    {
        var decoded = YahooLiveMessageDecoder.TryDecodeBase64("invalid_base64_string", out var update);

        Assert.False(decoded);
        Assert.Null(update);
    }

    [Fact]
    public async Task SubscribeAsync_UsesChannelAndDecodesMessages()
    {
        var envelope = Encoding.UTF8.GetBytes($"{{\"message\":\"{ValidBase64Message}\"}}");
        var transport = new FakeWebSocketTransport(envelope);
        await using var stream = new YahooLiveStream(transport, new YahooLiveStreamOptions
        {
            ResubscribeInterval = TimeSpan.FromMinutes(5),
            ChannelCapacity = 4
        });

        await stream.SubscribeAsync(new[] { "BTC-USD" });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var update = await stream.Messages.ReadAsync(cts.Token);

        Assert.Equal("BTC-USD", update.Symbol);
        Assert.True(update.Price > 0);
        Assert.Contains(transport.SentMessages.ToArray(), message => message.Contains("subscribe", StringComparison.OrdinalIgnoreCase) && message.Contains("BTC-USD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task UnsubscribeAsync_SendsUnsubscribeCommand()
    {
        var envelope = Encoding.UTF8.GetBytes($"{{\"message\":\"{ValidBase64Message}\"}}");
        var transport = new FakeWebSocketTransport(envelope);
        await using var stream = new YahooLiveStream(transport, new YahooLiveStreamOptions
        {
            ResubscribeInterval = TimeSpan.FromMinutes(5)
        });

        await stream.SubscribeAsync(new[] { "BTC-USD" });
        await stream.UnsubscribeAsync(new[] { "BTC-USD" });

        Assert.Contains(transport.SentMessages.ToArray(), message => message.Contains("unsubscribe", StringComparison.OrdinalIgnoreCase) && message.Contains("BTC-USD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReceiveLoop_CloseFrame_ReconnectsAndResubscribes()
    {
        var firstEnvelope = Encoding.UTF8.GetBytes($"{{\"message\":\"{ValidBase64Message}\"}}");
        var secondEnvelope = Encoding.UTF8.GetBytes($"{{\"message\":\"{ValidBase64Message}\"}}");
        var transport = new ReconnectingFakeWebSocketTransport(firstEnvelope, secondEnvelope);
        await using var stream = new YahooLiveStream(transport, new YahooLiveStreamOptions
        {
            ReconnectDelay = TimeSpan.FromMilliseconds(10),
            ResubscribeInterval = TimeSpan.FromMinutes(5)
        });

        await stream.SubscribeAsync(new[] { "BTC-USD" });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var first = await stream.Messages.ReadAsync(cts.Token);
        var second = await stream.Messages.ReadAsync(cts.Token);

        Assert.Equal("BTC-USD", first.Symbol);
        Assert.Equal("BTC-USD", second.Symbol);
        Assert.True(transport.ConnectCount >= 2);
        Assert.True(transport.SentMessages.ToArray().Count(message => message.Contains("subscribe", StringComparison.OrdinalIgnoreCase)) >= 2);
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

    private sealed class ReconnectingFakeWebSocketTransport(byte[] firstMessage, byte[] secondMessage) : IYahooWebSocketTransport
    {
        private readonly byte[] _firstMessage = firstMessage;
        private readonly byte[] _secondMessage = secondMessage;
        private int _receiveIndex;

        public int ConnectCount { get; private set; }

        public ConcurrentQueue<string> SentMessages { get; } = new();

        public WebSocketState State { get; private set; } = WebSocketState.None;

        public ValueTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            ConnectCount++;
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
            _receiveIndex++;
            return _receiveIndex switch
            {
                1 => Copy(_firstMessage, buffer),
                2 => ValueTask.FromResult(new YahooWebSocketReceiveResult(0, true, WebSocketMessageType.Close, WebSocketCloseStatus.EndpointUnavailable, "reconnect")),
                3 => Copy(_secondMessage, buffer),
                _ => ValueTask.FromResult(new YahooWebSocketReceiveResult(0, true, WebSocketMessageType.Close, WebSocketCloseStatus.NormalClosure, "done"))
            };
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

        private static ValueTask<YahooWebSocketReceiveResult> Copy(byte[] payload, Memory<byte> buffer)
        {
            payload.CopyTo(buffer);
            return ValueTask.FromResult(new YahooWebSocketReceiveResult(payload.Length, true, WebSocketMessageType.Text, null, null));
        }
    }
}