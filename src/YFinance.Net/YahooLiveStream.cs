using System.Buffers;
using System.Text.Json;
using System.Threading.Channels;
using System.Net.WebSockets;

namespace YFinance.Net;

/// <summary>
/// Streams live Yahoo Finance price updates over the Yahoo websocket feed.
/// </summary>
public sealed class YahooLiveStream : IAsyncDisposable
{
    private readonly IYahooWebSocketTransport _transport;
    private readonly YahooLiveStreamOptions _options;
    private readonly Channel<LivePriceUpdate> _channel;
    private readonly HashSet<string> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly object _subscriptionGate = new();
    private readonly CancellationTokenSource _cts = new();

    private Task? _receiveLoopTask;
    private Task? _resubscribeLoopTask;
    private bool _started;
    private int _disposeSignaled;

    /// <summary>
    /// Initializes a live stream using the default Yahoo websocket transport.
    /// </summary>
    /// <param name="options">Optional stream configuration.</param>
    public YahooLiveStream(YahooLiveStreamOptions? options = null)
        : this(new ClientWebSocketTransport(), options)
    {
    }

    internal YahooLiveStream(IYahooWebSocketTransport transport, YahooLiveStreamOptions? options = null)
    {
        _transport = transport;
        _options = options ?? new YahooLiveStreamOptions();
        _channel = Channel.CreateBounded<LivePriceUpdate>(new BoundedChannelOptions(_options.ChannelCapacity)
        {
            SingleReader = false,
            SingleWriter = true,
            FullMode = _options.ChannelFullMode,
            AllowSynchronousContinuations = false
        });
    }

    /// <summary>
    /// Gets a channel reader for consuming live price updates.
    /// </summary>
    public ChannelReader<LivePriceUpdate> Messages => _channel.Reader;

    /// <summary>
    /// Reads live price updates as an async stream.
    /// </summary>
    /// <param name="cancellationToken">Token used to stop enumeration.</param>
    /// <returns>An async sequence of live price updates.</returns>
    public IAsyncEnumerable<LivePriceUpdate> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Establishes the websocket connection if it has not already been started.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the connection attempt.</param>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_started)
            {
                return;
            }

            await _transport.ConnectAsync(_options.StreamUri, cancellationToken).ConfigureAwait(false);
            _started = true;
            _receiveLoopTask = Task.Run(ReceiveLoopAsync, CancellationToken.None);
            _resubscribeLoopTask = Task.Run(ResubscribeLoopAsync, CancellationToken.None);
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Subscribes one or more symbols to the live stream.
    /// </summary>
    /// <param name="symbols">Symbols to subscribe.</param>
    /// <param name="cancellationToken">Token used to cancel the subscription request.</param>
    public async Task SubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        var snapshot = NormalizeSymbols(symbols);
        if (snapshot.Length == 0)
        {
            return;
        }

        await ConnectAsync(cancellationToken).ConfigureAwait(false);
        lock (_subscriptionGate)
        {
            foreach (var symbol in snapshot)
            {
                _subscriptions.Add(symbol);
            }
        }

        await SendSubscriptionCommandAsync("subscribe", snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Unsubscribes one or more symbols from the live stream.
    /// </summary>
    /// <param name="symbols">Symbols to unsubscribe.</param>
    /// <param name="cancellationToken">Token used to cancel the unsubscription request.</param>
    public async Task UnsubscribeAsync(IEnumerable<string> symbols, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        var snapshot = NormalizeSymbols(symbols);
        if (snapshot.Length == 0)
        {
            return;
        }

        lock (_subscriptionGate)
        {
            foreach (var symbol in snapshot)
            {
                _subscriptions.Remove(symbol);
            }
        }

        await SendSubscriptionCommandAsync("unsubscribe", snapshot, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stops background loops, closes the websocket, and releases owned resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeSignaled, 1) != 0)
        {
            return;
        }

        _cts.Cancel();

        try
        {
            if (_receiveLoopTask is not null)
            {
                await _receiveLoopTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }

        try
        {
            if (_resubscribeLoopTask is not null)
            {
                await _resubscribeLoopTask.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }

        await _transport.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disposing", CancellationToken.None).ConfigureAwait(false);
        await _transport.DisposeAsync().ConfigureAwait(false);
        _cts.Dispose();
        _connectionLock.Dispose();
    }

    private async Task ReceiveLoopAsync()
    {
        var receiveBuffer = ArrayPool<byte>.Shared.Rent(4096);
        using var messageBuffer = new MemoryStream(4096);
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                messageBuffer.SetLength(0);
                YahooWebSocketReceiveResult receiveResult;

                do
                {
                    receiveResult = await _transport.ReceiveAsync(receiveBuffer, _cts.Token).ConfigureAwait(false);
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        if (!await TryReconnectAsync(_cts.Token).ConfigureAwait(false))
                        {
                            _channel.Writer.TryComplete();
                            return;
                        }

                        messageBuffer.SetLength(0);
                        continue;
                    }

                    if (receiveResult.Count > 0)
                    {
                        messageBuffer.Write(receiveBuffer, 0, receiveResult.Count);
                    }
                }
                while (!receiveResult.EndOfMessage);

                if (receiveResult.MessageType != WebSocketMessageType.Text || messageBuffer.Length == 0)
                {
                    continue;
                }

                if (YahooLiveMessageDecoder.TryDecodeEnvelope(messageBuffer.GetBuffer().AsMemory(0, (int)messageBuffer.Length), out var update) && update is not null)
                {
                    await _channel.Writer.WriteAsync(update, _cts.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (!await TryReconnectAsync(_cts.Token).ConfigureAwait(false))
            {
                _channel.Writer.TryComplete(ex);
                return;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(receiveBuffer);
            if (_cts.IsCancellationRequested)
            {
                _channel.Writer.TryComplete();
            }
        }
    }

    private async Task ResubscribeLoopAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(_options.ResubscribeInterval);
            while (await timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                string[] subscriptions;
                lock (_subscriptionGate)
                {
                    subscriptions = _subscriptions.ToArray();
                }

                if (subscriptions.Length == 0)
                {
                    continue;
                }

                try
                {
                    await SendSubscriptionCommandAsync("subscribe", subscriptions, _cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                }
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
    }

    private async Task<bool> TryReconnectAsync(CancellationToken cancellationToken)
    {
        if (!_options.AutoReconnect || _cts.IsCancellationRequested)
        {
            return false;
        }

        await _connectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await _transport.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnect", CancellationToken.None).ConfigureAwait(false);
                    await _transport.ConnectAsync(_options.StreamUri, cancellationToken).ConfigureAwait(false);

                    string[] subscriptions;
                    lock (_subscriptionGate)
                    {
                        subscriptions = _subscriptions.ToArray();
                    }

                    if (subscriptions.Length > 0)
                    {
                        await SendSubscriptionCommandAsync("subscribe", subscriptions, cancellationToken).ConfigureAwait(false);
                    }

                    return true;
                }
                catch (OperationCanceledException) when (_cts.IsCancellationRequested || cancellationToken.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    await Task.Delay(_options.ReconnectDelay, cancellationToken).ConfigureAwait(false);
                }
            }

            return false;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task SendSubscriptionCommandAsync(string command, IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new Dictionary<string, IReadOnlyList<string>>(1)
        {
            [command] = symbols
        });

        await _transport.SendTextAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    private static string[] NormalizeSymbols(IEnumerable<string> symbols)
    {
        return symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}