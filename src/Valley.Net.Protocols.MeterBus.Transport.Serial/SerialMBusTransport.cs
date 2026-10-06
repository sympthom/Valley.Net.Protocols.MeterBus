using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.IO.Ports;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Serial port transport for M-Bus communication, wrapping System.IO.Ports with PipeReader.
/// </summary>
public sealed class SerialMBusTransport : IMBusTransport, IDisposable
{
    // Slack for USB adapter latency timers and thread scheduling on top of the bus timing
    private static readonly TimeSpan Margin = TimeSpan.FromMilliseconds(100);

    // How late, at most, a receive timeout or cancellation takes effect on Windows (see SerialReadStream)
    private static readonly TimeSpan ReadPollInterval = TimeSpan.FromMilliseconds(20);

    private readonly string _portName;
    private readonly SerialMBusTransportOptions _options;
    private readonly Lock _sync = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private Connection? _connection;
    private byte[]? _echo;
    private bool _disposed;

    public SerialMBusTransport(string portName, int baudRate = 2400)
        : this(portName, new SerialMBusTransportOptions { BaudRate = baudRate })
    {
    }

    public SerialMBusTransport(string portName, SerialMBusTransportOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.BaudRate, nameof(options.BaudRate));

        _portName = portName;
        _options = options;

        var maxFrameTime = BitTimes(CharacterBits(options) * MBusDeframer.MaxFrameLength);

        ResponseTimeout = Positive(options.ResponseTimeout, nameof(options.ResponseTimeout))
            ?? BitTimes(330) + TimeSpan.FromMilliseconds(50) + Margin;
        InterCharacterTimeout = Positive(options.InterCharacterTimeout, nameof(options.InterCharacterTimeout))
            ?? BitTimes(33) + Margin;
        WriteTimeout = Positive(options.WriteTimeout, nameof(options.WriteTimeout))
            ?? maxFrameTime + Margin;
        FrameTimeout = ResponseTimeout + 2 * maxFrameTime;
    }

    /// <summary>
    /// How long a receive waits for the first byte of the reply.
    /// </summary>
    public TimeSpan ResponseTimeout { get; }

    /// <summary>
    /// The longest gap allowed between the bytes of a frame; after it the partial frame is dropped.
    /// </summary>
    public TimeSpan InterCharacterTimeout { get; }

    /// <summary>
    /// The longest a receive may take in all: <see cref="ResponseTimeout"/> plus twice the time to
    /// transmit a maximum-size frame, which leaves room for the frame and as much noise again before
    /// it. A 261-byte frame at 300 baud (11-bit characters) takes 9.57 s, so this is 20.4 s there and
    /// 2.7 s at 2400 baud. It only ends a receive that keeps getting bytes without a valid frame.
    /// </summary>
    public TimeSpan FrameTimeout { get; }

    /// <summary>
    /// How long a send may take.
    /// </summary>
    public TimeSpan WriteTimeout { get; }

    /// <summary>
    /// Whether the port is open and has not been seen to close.
    /// </summary>
    public bool IsConnected => _connection is { Closed: false, EndOfStream: false } connection && connection.Port.IsOpen;

    private TimeSpan BitTimes(double bits) =>
        TimeSpan.FromTicks((long)Math.Round(bits * TimeSpan.TicksPerSecond / _options.BaudRate));

    private static double CharacterBits(SerialMBusTransportOptions options) =>
        1 + options.DataBits + (options.Parity == Parity.None ? 0 : 1) + options.StopBits switch
        {
            StopBits.OnePointFive => 1.5,
            StopBits.Two => 2,
            _ => 1,
        };

    private static int Milliseconds(TimeSpan timeout) => (int)Math.Min(int.MaxValue, Math.Ceiling(timeout.TotalMilliseconds));

    private static TimeSpan? Positive(TimeSpan? value, string name)
    {
        if (value is { } timeout)
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero, name);
        return value;
    }

    /// <summary>
    /// Opens the port, closing any previous one first. SerialPort.Open is synchronous and can take a
    /// while on some USB drivers, so it runs on a thread-pool thread rather than the caller's; a
    /// cancellation that arrives during Open takes effect once it returns.
    /// </summary>
    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        Connection? previous;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            previous = _connection;
            _connection = null;
        }
        Close(previous);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
        var port = new SerialPort(_portName, _options.BaudRate, _options.Parity, _options.DataBits, _options.StopBits)
        {
            Handshake = Handshake.None,
            DtrEnable = _options.DtrEnable,
            RtsEnable = _options.RtsEnable,

            // The transport's own timers end reads and writes. On Windows, whose SerialStream ignores
            // the token of an async read and write, a read that sees no byte for ReadPollInterval ends
            // with 0 bytes and SerialReadStream checks the token and reads again; it still returns as
            // soon as a byte arrives. Unix ignores both values for async reads and writes.
            ReadTimeout = Milliseconds(ReadPollInterval),
            WriteTimeout = Milliseconds(WriteTimeout),
        };

        Connection connection;
        try
        {
            await Task.Run(() =>
            {
                port.Open();
                port.DiscardInBuffer();
                port.DiscardOutBuffer();
            }, connectCts.Token);
            connectCts.Token.ThrowIfCancellationRequested();

            connection = new Connection(port);
        }
        catch (Exception ex)
        {
            port.Dispose();
            if (ex is OperationCanceledException && _disposeCts.IsCancellationRequested && !ct.IsCancellationRequested)
                throw new ObjectDisposedException(GetType().FullName);
            throw;
        }

        bool disposed;
        lock (_sync)
        {
            disposed = _disposed;
            if (!disposed)
            {
                // Another ConnectAsync may have finished in the meantime; the last one wins
                previous = _connection;
                _connection = connection;
            }
        }

        if (disposed)
        {
            Close(connection);
            throw new ObjectDisposedException(GetType().FullName);
        }

        Close(previous);
    }

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        var connection = GetConnection();

        // Set before writing: the converter echoes while the bytes go out
        _echo = _options.EchoSuppression ? frameBytes.ToArray() : null;

        // WriteTimeout does not bound async writes on every platform
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(WriteTimeout);

        try
        {
            var stream = connection.Port.BaseStream;
            await stream.WriteAsync(frameBytes, timeoutCts.Token);

            // On Unix the flush waits for the output to drain and ignores its token, so stop waiting on our own
            await stream.FlushAsync(timeoutCts.Token).WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested && !connection.Closed)
        {
            throw new TimeoutException($"M-Bus frame not sent within {WriteTimeout.TotalMilliseconds} ms");
        }
        catch (Exception ex) when (connection.Closed && IsAbort(ex))
        {
            throw ClosedWhilePending(ex);
        }
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default)
    {
        var connection = BeginReceive();
        try
        {
            return await ReceiveFrameAsync(connection, ct);
        }
        catch (Exception ex) when (connection.Closed && IsAbort(ex))
        {
            throw ClosedWhilePending(ex);
        }
        finally
        {
            EndReceive(connection);
        }
    }

    /// <summary>
    /// Waits up to <see cref="ResponseTimeout"/> for a reply to start. Once a frame has started, each
    /// gap may last up to <see cref="InterCharacterTimeout"/>, and the whole receive up to
    /// <see cref="FrameTimeout"/>. Noise that does not start a frame does not shorten the wait for the reply.
    /// </summary>
    private async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(Connection connection, CancellationToken ct)
    {
        var reader = connection.Reader;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(ResponseTimeout);
        var started = Stopwatch.GetTimestamp();
        var frameStarted = false;

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(timeoutCts.Token);
                var buffer = result.Buffer;

                while (MBusDeframer.TryReadFrame(ref buffer, out var frame))
                {
                    // The echo, if any, comes before the reply. A request is never a valid reply,
                    // so a frame equal to it can only be the echo.
                    if (Interlocked.Exchange(ref _echo, null) is { } echo && frame.Length == echo.Length && frame.ToArray().AsSpan().SequenceEqual(echo))
                    {
                        // The slave's answer window starts at the end of the request
                        started = Stopwatch.GetTimestamp();
                        continue;
                    }

                    var bytes = frame.ToArray();
                    reader.AdvanceTo(buffer.Start);
                    return bytes;
                }

                // buffer.Start is past any noise, so at most one partial frame stays buffered
                reader.AdvanceTo(buffer.Start, result.Buffer.End);

                if (result.IsCompleted)
                {
                    connection.EndOfStream = true;
                    throw new IOException("Serial port closed before a complete frame was received");
                }

                frameStarted = !buffer.IsEmpty;
                var elapsed = Stopwatch.GetElapsedTime(started);
                var remaining = frameStarted
                    ? TimeSpan.FromTicks(Math.Min(InterCharacterTimeout.Ticks, (FrameTimeout - elapsed).Ticks))
                    : ResponseTimeout - elapsed;
                timeoutCts.CancelAfter(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
            }
        }
        catch (OperationCanceledException) when (!connection.Closed)
        {
            // The exchange is over: a partial frame left in the pipe would swallow the next reply
            DiscardBufferedBytes(reader);

            if (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
            {
                throw new TimeoutException(frameStarted
                    ? $"M-Bus frame incomplete: no further byte within {InterCharacterTimeout.TotalMilliseconds} ms, or not complete within {FrameTimeout.TotalMilliseconds} ms"
                    : $"No M-Bus reply started within {ResponseTimeout.TotalMilliseconds} ms");
            }

            throw;
        }
    }

    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        var connection = GetConnection();

        DiscardBufferedBytes(connection.Reader);

        // Bytes the driver holds have not reached the pipe yet; they are stale too
        connection.Port.DiscardInBuffer();

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Drops the bytes the pipe holds but has not returned as a frame.
    /// </summary>
    private static void DiscardBufferedBytes(PipeReader reader)
    {
        // After a receive every buffered byte counts as examined, and TryRead only
        // hands such bytes back when the read is cancelled
        reader.CancelPendingRead();
        if (reader.TryRead(out var result))
            reader.AdvanceTo(result.Buffer.End);
    }

    private Connection GetConnection()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _connection ?? throw new InvalidOperationException("Transport is not connected");
        }
    }

    private Connection BeginReceive()
    {
        lock (_sync)
        {
            var connection = GetConnection();
            if (connection.Receiving)
                throw new InvalidOperationException("A receive is already in progress");

            connection.Receiving = true;
            return connection;
        }
    }

    private void EndReceive(Connection connection)
    {
        bool closed;
        lock (_sync)
        {
            connection.Receiving = false;
            closed = connection.Closed;
        }

        // Close left the reader to us
        if (closed)
            connection.Reader.Complete();
    }

    /// <summary>
    /// Closes the port, which ends a pending read or write on it.
    /// </summary>
    private void Close(Connection? connection)
    {
        if (connection is null)
            return;

        bool receiving;
        lock (_sync)
        {
            connection.Closed = true;
            receiving = connection.Receiving;
        }

        connection.Port.Dispose();

        // Completing the reader while a receive is inside ReadAsync would race it; the receive does it on its way out
        if (!receiving)
            connection.Reader.Complete();
    }

    private static bool IsAbort(Exception ex) =>
        ex is OperationCanceledException or IOException or ObjectDisposedException or InvalidOperationException;

    /// <summary>
    /// The exception for an operation whose port was closed under it by Dispose or ConnectAsync.
    /// </summary>
    private Exception ClosedWhilePending(Exception ex) => _disposed
        ? new ObjectDisposedException(GetType().FullName)
        : new IOException("The serial port was closed by a reconnect", ex);

    public void Dispose()
    {
        Connection? connection;
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            connection = _connection;
            _connection = null;
        }

        // Ends a ConnectAsync in progress
        _disposeCts.Cancel();
        Close(connection);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    private sealed class Connection
    {
        public Connection(SerialPort port)
        {
            Port = port;
            Reader = PipeReader.Create(new SerialReadStream(port.BaseStream, () => port.IsOpen), new StreamPipeReaderOptions(leaveOpen: true));
        }

        public SerialPort Port { get; }
        public PipeReader Reader { get; }

        // Written under the transport's lock, read without it by exception filters
        public volatile bool Closed;
        public volatile bool Receiving;
        public volatile bool EndOfStream;
    }
}
