using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Wraps a transport and logs every frame sent and received as hex, with how long each receive took, so
/// collisions, CRC errors and slow meters can be diagnosed from the wire. Frames and receive timeouts are
/// logged at Debug, connects and input discards at Trace.
/// </summary>
/// <remarks>
/// Put it between the master and the real transport, e.g.
/// <c>services.AddSingleton&lt;IMBusTransport&gt;(sp =&gt; new TracingMBusTransport(new TcpMBusTransport(host, port),
/// sp.GetRequiredService&lt;ILogger&lt;TracingMBusTransport&gt;&gt;()))</c>.
/// It owns the wrapped transport and disposes it. Bytes the wrapped transport skips while it looks for a frame
/// never reach this decorator.
/// </remarks>
public sealed partial class TracingMBusTransport : IMBusTransport, IDisposable
{
    private readonly IMBusTransport _inner;
    private readonly ILogger _logger;
    private int _disposed;

    public TracingMBusTransport(IMBusTransport inner, ILogger logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        await _inner.ConnectAsync(ct);
        LogConnected();
    }

    public ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
            LogSent(frameBytes.Length, frameBytes.ToHex());
        return _inner.SendFrameAsync(frameBytes, ct);
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default)
    {
        long started = Stopwatch.GetTimestamp();
        try
        {
            var frame = await _inner.ReceiveFrameAsync(ct);
            if (_logger.IsEnabled(LogLevel.Debug))
                LogReceived(frame.Length, ElapsedMs(started), frame.ToHex());
            return frame;
        }
        // Some third-party transports time out by cancelling a token of their own; the master treats that as a timeout too.
        catch (Exception e) when (e is TimeoutException || (e is OperationCanceledException && !ct.IsCancellationRequested))
        {
            LogTimedOut(ElapsedMs(started));
            throw;
        }
    }

    public async ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        await _inner.DiscardInputAsync(ct);
        LogDiscarded();
    }

    public ValueTask DisposeAsync()
        => Interlocked.Exchange(ref _disposed, 1) == 0 ? _inner.DisposeAsync() : ValueTask.CompletedTask;

    // A container disposing synchronously calls this; a transport without IDisposable is disposed by blocking.
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        if (_inner is IDisposable disposable)
            disposable.Dispose();
        else
            _inner.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static double ElapsedMs(long started) => Stopwatch.GetElapsedTime(started).TotalMilliseconds;

    [LoggerMessage(Level = LogLevel.Debug, Message = "TX {Length} bytes: {Frame}")]
    private partial void LogSent(int length, string frame);

    [LoggerMessage(Level = LogLevel.Debug, Message = "RX {Length} bytes after {ElapsedMs:F1} ms: {Frame}")]
    private partial void LogReceived(int length, double elapsedMs, string frame);

    [LoggerMessage(Level = LogLevel.Debug, Message = "RX timeout after {ElapsedMs:F1} ms")]
    private partial void LogTimedOut(double elapsedMs);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Connected")]
    private partial void LogConnected();

    [LoggerMessage(Level = LogLevel.Trace, Message = "Input discarded")]
    private partial void LogDiscarded();
}
