using System.Diagnostics;

namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// Scripted transport: each receive returns the next queued reply, or times out like the real
/// transports do (by cancelling a linked token) when the queue is empty or holds a timeout.
/// </summary>
internal sealed class FakeMBusTransport : IMBusTransport
{
    private readonly Queue<byte[]?> _replies = new();
    private readonly TimeSpan _timeout;

    public FakeMBusTransport(TimeSpan? timeout = null)
    {
        _timeout = timeout ?? TimeSpan.FromMilliseconds(50);
    }

    public List<byte[]> Sent { get; } = [];
    public int ReceiveCount { get; private set; }
    public int DiscardCount { get; private set; }
    public bool IsDisposed { get; private set; }

    public FakeMBusTransport Reply(byte[] frame)
    {
        _replies.Enqueue(frame);
        return this;
    }

    public FakeMBusTransport ReplyAck() => Reply([MBusConstants.FRAME_ACK_START]);

    public FakeMBusTransport NoReply()
    {
        _replies.Enqueue(null);
        return this;
    }

    public ValueTask ConnectAsync(CancellationToken ct = default) => ValueTask.CompletedTask;

    public ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Sent.Add(frameBytes.ToArray());
        return ValueTask.CompletedTask;
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ReceiveCount++;

        if (_replies.TryDequeue(out var reply) && reply is not null)
            return reply;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        await Task.Delay(Timeout.Infinite, timeoutCts.Token);
        throw new UnreachableException();
    }

    // The queue is what the slave will send, not what is already buffered, so there is nothing to drop.
    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        DiscardCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}
