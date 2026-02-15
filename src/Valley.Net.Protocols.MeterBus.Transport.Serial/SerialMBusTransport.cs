using System.Buffers;
using System.IO.Pipelines;
using System.IO.Ports;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Serial port transport for M-Bus communication, wrapping System.IO.Ports with PipeReader.
/// </summary>
public sealed class SerialMBusTransport : IMBusTransport
{
    private readonly string _portName;
    private readonly int _baudRate;
    private readonly TimeSpan _timeout;
    private SerialPort? _serialPort;
    private PipeReader? _reader;
    private bool _disposed;

    public SerialMBusTransport(string portName, int baudRate = 2400, TimeSpan? timeout = null)
    {
        _portName = portName ?? throw new ArgumentNullException(nameof(portName));
        _baudRate = baudRate;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _serialPort = new SerialPort(_portName, _baudRate, Parity.Even, 8, StopBits.One)
        {
            ReadTimeout = (int)_timeout.TotalMilliseconds,
            WriteTimeout = (int)_timeout.TotalMilliseconds,
            Handshake = Handshake.None,
        };

        _serialPort.Open();
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();

        _reader = PipeReader.Create(_serialPort.BaseStream, new StreamPipeReaderOptions(leaveOpen: true));

        return ValueTask.CompletedTask;
    }

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_serialPort is null || !_serialPort.IsOpen)
            throw new InvalidOperationException("Transport is not connected");

        await _serialPort.BaseStream.WriteAsync(frameBytes, ct);
        await _serialPort.BaseStream.FlushAsync(ct);
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
                throw new InvalidOperationException("Serial port closed before complete frame received");
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

            if (_serialPort is not null)
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
                _serialPort.Dispose();
            }
        }
    }
}
