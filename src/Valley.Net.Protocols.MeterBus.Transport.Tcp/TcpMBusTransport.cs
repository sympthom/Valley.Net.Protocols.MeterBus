using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// TCP transport for M-Bus communication using System.IO.Pipelines for frame boundary detection.
/// </summary>
public sealed class TcpMBusTransport : IMBusTransport
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private Socket? _socket;
    private NetworkStream? _stream;
    private PipeReader? _reader;
    private bool _disposed;

    public TcpMBusTransport(string host, int port, TimeSpan? timeout = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.NoDelay = true;
        _socket.ReceiveTimeout = (int)_timeout.TotalMilliseconds;
        _socket.SendTimeout = (int)_timeout.TotalMilliseconds;

        await _socket.ConnectAsync(_host, _port, ct);

        _stream = new NetworkStream(_socket, ownsSocket: false);
        _reader = PipeReader.Create(_stream, new StreamPipeReaderOptions(leaveOpen: true));
    }

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_stream is null)
            throw new InvalidOperationException("Transport is not connected");

        await _stream.WriteAsync(frameBytes, ct);
        await _stream.FlushAsync(ct);
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_reader is null)
            throw new InvalidOperationException("Transport is not connected");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        while (true)
        {
            var result = await _reader.ReadAsync(timeoutCts.Token);
            var buffer = result.Buffer;

            if (TryReadFrame(ref buffer, out var frame))
            {
                _reader.AdvanceTo(buffer.Start);
                return frame;
            }

            // buffer.Start is past any noise, so at most one partial frame (< 261 bytes) stays buffered
            _reader.AdvanceTo(buffer.Start, result.Buffer.End);

            if (result.IsCompleted)
                throw new InvalidOperationException("Connection closed before complete frame received");
        }
    }

    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_reader is null || _socket is null)
            throw new InvalidOperationException("Transport is not connected");

        // After a timed-out receive every buffered byte counts as examined, and TryRead only
        // hands such bytes back when the read is cancelled
        _reader.CancelPendingRead();
        if (_reader.TryRead(out var result))
            _reader.AdvanceTo(result.Buffer.End);

        // Bytes the socket holds have not reached the pipe yet; they are stale too
        var scratch = new byte[512];
        while (_socket.Available > 0)
            _socket.Receive(scratch, SocketFlags.None);

        return ValueTask.CompletedTask;
    }

    private static ReadOnlySpan<byte> FrameStartBytes =>
        [MBusConstants.FRAME_ACK_START, MBusConstants.FRAME_SHORT_START, MBusConstants.FRAME_LONG_START];

    /// <summary>
    /// Finds the first frame in <paramref name="buffer"/>, skipping bytes that cannot start one.
    /// On return <paramref name="buffer"/> starts after the frame, or at the partial frame that needs more data.
    /// Checksums are left to the frame parser.
    /// </summary>
    private static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlyMemory<byte> frame)
    {
        frame = default;

        while (true)
        {
            var reader = new SequenceReader<byte>(buffer);
            if (!reader.TryAdvanceToAny(FrameStartBytes, advancePastDelimiter: false))
            {
                buffer = buffer.Slice(buffer.End);
                return false;
            }

            buffer = buffer.Slice(reader.Position);

            var frameLength = GetFrameLength(buffer);
            if (frameLength == 0)
                return false;

            if (frameLength < 0)
            {
                // Not a frame after all: resynchronise on the next byte
                buffer = buffer.Slice(1);
                continue;
            }

            frame = buffer.Slice(0, frameLength).ToArray();
            buffer = buffer.Slice(frameLength);
            return true;
        }
    }

    /// <summary>
    /// Returns the length of the frame starting at the first byte of <paramref name="buffer"/>,
    /// 0 when more bytes are needed to tell, or -1 when the header or stop byte rules it out.
    /// </summary>
    private static int GetFrameLength(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        reader.TryPeek(out var startByte);

        switch (startByte)
        {
            case MBusConstants.FRAME_ACK_START:
                return 1;

            case MBusConstants.FRAME_SHORT_START:
                if (!reader.TryPeek(MBusConstants.FRAME_FIXED_SIZE_SHORT - 1, out var shortStop))
                    return 0;

                return shortStop == MBusConstants.FRAME_STOP ? MBusConstants.FRAME_FIXED_SIZE_SHORT : -1;

            case MBusConstants.FRAME_LONG_START:
                // 68 L L 68, where L covers at least C, A and CI. Reject on the first wrong
                // header byte so a stray 0x68 does not hold back the bytes that follow it.
                if (!reader.TryPeek(1, out var len))
                    return 0;
                if (len < 3)
                    return -1;
                if (!reader.TryPeek(2, out var lenRepeated))
                    return 0;
                if (lenRepeated != len)
                    return -1;
                if (!reader.TryPeek(3, out var secondStart))
                    return 0;
                if (secondStart != MBusConstants.FRAME_LONG_START)
                    return -1;

                var frameLength = len + MBusConstants.FRAME_FIXED_SIZE_LONG;
                if (!reader.TryPeek(frameLength - 1, out var longStop))
                    return 0;

                return longStop == MBusConstants.FRAME_STOP ? frameLength : -1;

            default:
                return -1;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;

            if (_reader is not null)
                await _reader.CompleteAsync();

            _stream?.Dispose();
            _socket?.Dispose();
        }
    }
}
