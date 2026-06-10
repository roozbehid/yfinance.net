using System.Net.WebSockets;
using System.Text;

namespace YFinance.Net;

internal readonly record struct YahooWebSocketReceiveResult(
    int Count,
    bool EndOfMessage,
    WebSocketMessageType MessageType,
    WebSocketCloseStatus? CloseStatus,
    string? CloseStatusDescription);

internal interface IYahooWebSocketTransport : IAsyncDisposable
{
    WebSocketState State { get; }

    ValueTask ConnectAsync(Uri uri, CancellationToken cancellationToken);

    ValueTask SendTextAsync(string message, CancellationToken cancellationToken);

    ValueTask<YahooWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);

    ValueTask CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken);
}

internal sealed class ClientWebSocketTransport : IYahooWebSocketTransport
{
    private ClientWebSocket? _client;

    public WebSocketState State => _client?.State ?? WebSocketState.None;

    public async ValueTask ConnectAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (_client is { State: WebSocketState.Open })
        {
            return;
        }

        if (_client is not null)
        {
            _client.Dispose();
        }

        _client = new ClientWebSocket();
        await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask SendTextAsync(string message, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("WebSocket transport is not connected.");
        }

        return new ValueTask(_client.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, endOfMessage: true, cancellationToken));
    }

    public async ValueTask<YahooWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("WebSocket transport is not connected.");
        }

        var result = await _client.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
        return new YahooWebSocketReceiveResult(
            result.Count,
            result.EndOfMessage,
            result.MessageType,
            _client.CloseStatus,
            _client.CloseStatusDescription);
    }

    public ValueTask CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return ValueTask.CompletedTask;
        }

        if (_client.State == WebSocketState.Open || _client.State == WebSocketState.CloseReceived)
        {
            return new ValueTask(_client.CloseAsync(closeStatus, statusDescription, cancellationToken));
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        _client = null;
        return ValueTask.CompletedTask;
    }
}