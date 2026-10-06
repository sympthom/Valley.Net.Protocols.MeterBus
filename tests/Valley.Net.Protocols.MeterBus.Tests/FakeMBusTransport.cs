namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// Scripted transport: each receive returns the next queued reply. When the queue is empty or holds a
/// timeout it waits out its timeout and throws <see cref="TimeoutException"/>, as the IMBusTransport contract
/// asks, or, with <c>cancelOnTimeout</c>, cancels a linked token the way some third-party transports do.
/// </summary>
internal sealed class FakeMBusTransport : IMBusTransport
{
    private readonly Queue<(byte[]? Frame, Task? Gate)> _replies = new();
    private readonly TimeSpan _timeout;
    private readonly bool _cancelOnTimeout;

    public FakeMBusTransport(TimeSpan? timeout = null, bool cancelOnTimeout = false)
    {
        _timeout = timeout ?? TimeSpan.Zero;
        _cancelOnTimeout = cancelOnTimeout;
    }

    public List<byte[]> Sent { get; } = [];
    public int ReceiveCount { get; private set; }
    public int DiscardCount { get; private set; }
    public bool IsDisposed => DisposeCount > 0;
    public int DisposeCount { get; private set; }

    /// <summary>Raised when a receive starts, before it looks at the queue.</summary>
    public event Action? Receiving;

    public FakeMBusTransport Reply(byte[] frame)
    {
        _replies.Enqueue((frame, null));
        return this;
    }

    /// <summary>Queues a reply that is only returned once <paramref name="gate"/> completes.</summary>
    public FakeMBusTransport ReplyAfter(Task gate, byte[] frame)
    {
        _replies.Enqueue((frame, gate));
        return this;
    }

    public FakeMBusTransport ReplyAck() => Reply([MBusConstants.FRAME_ACK_START]);

    public FakeMBusTransport NoReply()
    {
        _replies.Enqueue((null, null));
        return this;
    }

    public FakeMBusTransport NoReply(int count)
    {
        for (int i = 0; i < count; i++)
            NoReply();
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
        Receiving?.Invoke();

        if (_replies.TryDequeue(out var reply) && reply.Frame is not null)
        {
            if (reply.Gate is not null)
                await reply.Gate.WaitAsync(ct);
            return reply.Frame;
        }

        // A zero timeout has already run out, whatever happened to ct meanwhile.
        if (_timeout == TimeSpan.Zero)
            throw _cancelOnTimeout ? new OperationCanceledException() : new TimeoutException("No reply");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);
        try
        {
            await Task.Delay(Timeout.Infinite, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested && !_cancelOnTimeout)
        {
        }

        throw new TimeoutException("No reply");
    }

    // The queue is what the slave will send, not what is already buffered, so there is nothing to drop.
    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        DiscardCount++;
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        DisposeCount++;
        return ValueTask.CompletedTask;
    }
}
