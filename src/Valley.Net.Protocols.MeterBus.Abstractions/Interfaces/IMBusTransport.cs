namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Transport abstraction for M-Bus communication.
/// Replaces the external Valley.Net.Bindings dependency.
/// </summary>
public interface IMBusTransport : IAsyncDisposable
{
    ValueTask ConnectAsync(CancellationToken ct = default);
    ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default);
    ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default);

    /// <summary>
    /// Discards any bytes already received but not yet returned as a frame, so a late or
    /// stale reply cannot be taken as the answer to the next request.
    /// </summary>
    ValueTask DiscardInputAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
}
