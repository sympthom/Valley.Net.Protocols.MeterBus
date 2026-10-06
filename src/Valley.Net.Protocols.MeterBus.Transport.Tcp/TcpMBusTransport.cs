using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// TCP transport for M-Bus communication using System.IO.Pipelines for frame boundary detection.
/// </summary>
public sealed class TcpMBusTransport : IMBusTransport, IDisposable
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private readonly Lock _sync = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private Connection? _connection;
    private bool _disposed;

    /// <param name="host">Host name, IPv4 or IPv6 address of the gateway.</param>
    /// <param name="port">TCP port of the gateway.</param>
    /// <param name="timeout">How long a send, or the wait for a complete reply frame, may take. Default 5 s.</param>
    public TcpMBusTransport(string host, int port, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, IPEndPoint.MinPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);

        _host = host;
        _port = port;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Whether a connection is open and has not been seen to fail or be closed by the gateway.
    /// </summary>
    public bool IsConnected => _connection is { Closed: false, EndOfStream: false } connection && connection.Socket.Connected;

    /// <summary>
    /// Connects to the gateway, closing any previous connection first.
    /// </summary>
    public async ValueTask ConnectAsync(CancellationToken ct = default)
    {
        // A gateway that takes one client at a time must see the old session end before the new one starts
        Connection? previous;
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            previous = _connection;
            _connection = null;
        }
        Close(previous);

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _disposeCts.Token);
        Socket socket;
        try
        {
            socket = await ConnectSocketAsync(connectCts.Token);
        }
        catch (OperationCanceledException) when (_disposeCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        var connection = new Connection(socket);
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

    /// <summary>
    /// Tries each address the host resolves to. A socket bound to one address family, as
    /// Socket.ConnectAsync(host, port) needs, would skip the other family's addresses.
    /// </summary>
    private async Task<Socket> ConnectSocketAsync(CancellationToken ct)
    {
        IPAddress[] addresses = IPAddress.TryParse(_host, out var address)
            ? [address]
            : await Dns.GetHostAddressesAsync(_host, ct);

        if (addresses.Length == 0)
            throw new SocketException((int)SocketError.HostNotFound);

        ExceptionDispatchInfo? lastError = null;
        foreach (var candidate in addresses)
        {
            var socket = new Socket(candidate.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(new IPEndPoint(candidate, _port), ct);
                return socket;
            }
            catch (SocketException ex)
            {
                socket.Dispose();
                lastError = ExceptionDispatchInfo.Capture(ex);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        lastError!.Throw();
        throw new UnreachableException();
    }

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        var connection = GetConnection();

        // Socket.SendTimeout does not apply to async sends, so a peer that stops reading would block forever
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            await connection.Stream.WriteAsync(frameBytes, timeoutCts.Token);
            await connection.Stream.FlushAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested && !connection.Closed)
        {
            throw new TimeoutException($"M-Bus frame not sent within {_timeout.TotalMilliseconds} ms");
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

    private async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(Connection connection, CancellationToken ct)
    {
        var reader = connection.Reader;
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            while (true)
            {
                var result = await reader.ReadAsync(timeoutCts.Token);
                var buffer = result.Buffer;

                if (MBusDeframer.TryReadFrame(ref buffer, out var frame))
                {
                    var bytes = frame.ToArray();
                    reader.AdvanceTo(buffer.Start);
                    return bytes;
                }

                // buffer.Start is past any noise, so at most one partial frame stays buffered
                reader.AdvanceTo(buffer.Start, result.Buffer.End);

                if (result.IsCompleted)
                {
                    connection.EndOfStream = true;
                    throw new IOException("Connection closed by the gateway before a complete frame was received");
                }
            }
        }
        catch (OperationCanceledException) when (!connection.Closed)
        {
            // The exchange is over: a partial frame left in the pipe would swallow the next reply
            DiscardBufferedBytes(reader);

            if (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
                throw new TimeoutException($"No M-Bus frame received within {_timeout.TotalMilliseconds} ms");

            throw;
        }
    }

    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        var connection = GetConnection();

        DiscardBufferedBytes(connection.Reader);

        // Bytes the socket holds have not reached the pipe yet; they are stale too
        var scratch = new byte[512];
        while (connection.Socket.Available > 0)
            connection.Socket.Receive(scratch, SocketFlags.None);

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
    /// Closes the socket, which ends a pending read or write on it.
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

        connection.Stream.Dispose();

        // Completing the reader while a receive is inside ReadAsync would race it; the receive does it on its way out
        if (!receiving)
            connection.Reader.Complete();
    }

    private static bool IsAbort(Exception ex) =>
        ex is OperationCanceledException or IOException or ObjectDisposedException or SocketException;

    /// <summary>
    /// The exception for an operation whose connection was closed under it by Dispose or ConnectAsync.
    /// </summary>
    private Exception ClosedWhilePending(Exception ex) => _disposed
        ? new ObjectDisposedException(GetType().FullName)
        : new IOException("The connection was closed by a reconnect", ex);

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
        public Connection(Socket socket)
        {
            Socket = socket;
            Stream = new NetworkStream(socket, ownsSocket: true);
            Reader = PipeReader.Create(Stream, new StreamPipeReaderOptions(leaveOpen: true));
        }

        public Socket Socket { get; }
        public NetworkStream Stream { get; }
        public PipeReader Reader { get; }

        // Written under the transport's lock, read without it by exception filters
        public volatile bool Closed;
        public volatile bool Receiving;
        public volatile bool EndOfStream;
    }
}
