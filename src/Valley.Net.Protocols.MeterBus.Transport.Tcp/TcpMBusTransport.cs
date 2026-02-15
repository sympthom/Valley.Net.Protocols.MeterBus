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

            if (TryParseFrame(buffer, out var frame, out var consumed))
            {
                _reader.AdvanceTo(consumed);
                return frame;
            }

            _reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                throw new InvalidOperationException("Connection closed before complete frame received");
        }
    }

    private static bool TryParseFrame(ReadOnlySequence<byte> buffer, out ReadOnlyMemory<byte> frame, out SequencePosition consumed)
    {
        frame = default;
        consumed = buffer.Start;

        if (buffer.Length == 0)
            return false;

        var reader = new SequenceReader<byte>(buffer);
        if (!reader.TryPeek(out var startByte))
            return false;

        int frameLength;
        switch (startByte)
        {
            case MBusConstants.FRAME_ACK_START:
                frameLength = 1;
                break;

            case MBusConstants.FRAME_SHORT_START:
                frameLength = 5;
                break;

            case MBusConstants.FRAME_LONG_START:
                if (buffer.Length < 4)
                    return false;

                reader.Advance(1);
                reader.TryRead(out var len);
                frameLength = len + 6;
                break;

            default:
                // Skip unknown byte
                consumed = buffer.GetPosition(1);
                return false;
        }

        if (buffer.Length < frameLength)
            return false;

        frame = buffer.Slice(0, frameLength).ToArray();
        consumed = buffer.GetPosition(frameLength);
        return true;
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
