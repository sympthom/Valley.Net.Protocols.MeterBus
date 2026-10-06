using System.Net;
using System.Net.Sockets;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// UDP transport for M-Bus communication using raw Socket. Received datagrams are treated as a
/// byte stream: gateways that cut a frame across datagrams, or put several frames in one, work too.
/// </summary>
public sealed class UdpMBusTransport : IMBusTransport, IDisposable
{
    // Larger than any UDP payload, so a datagram is never truncated
    private const int MaxDatagramLength = 65536;

    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private readonly Lock _sync = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private Connection? _connection;
    private bool _disposed;

    /// <param name="host">Host name, IPv4 or IPv6 address of the gateway.</param>
    /// <param name="port">UDP port of the gateway.</param>
    /// <param name="timeout">How long a send, or the wait for a complete reply frame, may take. Default 5 s.</param>
    public UdpMBusTransport(string host, int port, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfLessThan(port, IPEndPoint.MinPort);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);

        _host = host;
        _port = port;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// UDP has no connection: this is true from ConnectAsync until the transport is disposed.
    /// </summary>
    public bool IsConnected => _connection is { Closed: false };

    /// <summary>
    /// Resolves the gateway and binds a socket to it, closing any previous socket first. A host name
    /// uses the first address it resolves to, since UDP cannot tell whether anything answers there.
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
        Socket socket;
        try
        {
            if (!IPAddress.TryParse(_host, out var address))
            {
                var addresses = await Dns.GetHostAddressesAsync(_host, connectCts.Token);
                address = addresses.Length > 0 ? addresses[0] : throw new SocketException((int)SocketError.HostNotFound);
            }

            socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                // A connected socket only receives datagrams from the gateway; the OS drops any others
                await socket.ConnectAsync(new IPEndPoint(address, _port), connectCts.Token);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
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

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        var connection = GetConnection();

        // Socket.SendTimeout does not apply to async sends
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            await connection.Socket.SendAsync(frameBytes, SocketFlags.None, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested && !connection.Closed)
        {
            throw new TimeoutException($"M-Bus frame not sent within {_timeout.TotalMilliseconds} ms");
        }
        catch (SocketException ex) when (IsUnreachable(ex) && !connection.Closed)
        {
            throw Unreachable(connection, ex);
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
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        try
        {
            while (true)
            {
                // Bytes after a returned frame stay buffered for the next receive
                if (connection.TryTakeFrame() is { } frame)
                    return frame;

                var received = await connection.Socket.ReceiveAsync(
                    connection.Buffer.AsMemory(connection.Buffered), SocketFlags.None, timeoutCts.Token);
                connection.Buffered += received;
            }
        }
        catch (OperationCanceledException) when (!connection.Closed)
        {
            // The exchange is over: a partial frame left buffered would swallow the next reply
            connection.Buffered = 0;

            if (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
                throw new TimeoutException($"No M-Bus frame received within {_timeout.TotalMilliseconds} ms");

            throw;
        }
        catch (SocketException ex) when (IsUnreachable(ex) && !connection.Closed)
        {
            connection.Buffered = 0;
            throw Unreachable(connection, ex);
        }
    }

    public ValueTask DiscardInputAsync(CancellationToken ct = default)
    {
        var connection = GetConnection();

        connection.Buffered = 0;

        // Each Receive drops one queued datagram; Available > 0 means it will not block
        while (connection.Socket.Available > 0)
        {
            try
            {
                connection.Socket.Receive(connection.Buffer, SocketFlags.None);
            }
            catch (SocketException ex) when (IsUnreachable(ex))
            {
                // A stale ICMP error belongs to an earlier exchange
            }
        }

        return ValueTask.CompletedTask;
    }

    // An ICMP port unreachable from the gateway host: ConnectionRefused on Linux and macOS, ConnectionReset on Windows
    private static bool IsUnreachable(SocketException ex) =>
        ex.SocketErrorCode is SocketError.ConnectionRefused or SocketError.ConnectionReset;

    private static IOException Unreachable(Connection connection, SocketException ex) =>
        new($"Nothing is listening at the M-Bus gateway {connection.Socket.RemoteEndPoint} (ICMP port unreachable)", ex);

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
        lock (_sync)
            connection.Receiving = false;
    }

    /// <summary>
    /// Closes the socket, which ends a pending receive or send on it.
    /// </summary>
    private void Close(Connection? connection)
    {
        if (connection is null)
            return;

        lock (_sync)
            connection.Closed = true;

        connection.Socket.Dispose();
    }

    private static bool IsAbort(Exception ex) =>
        ex is OperationCanceledException or ObjectDisposedException or SocketException;

    /// <summary>
    /// The exception for an operation whose socket was closed under it by Dispose or ConnectAsync.
    /// </summary>
    private Exception ClosedWhilePending(Exception ex) => _disposed
        ? new ObjectDisposedException(GetType().FullName)
        : new IOException("The socket was closed by a reconnect", ex);

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

    private sealed class Connection(Socket socket)
    {
        public Socket Socket { get; } = socket;

        // Received bytes not yet returned as a frame, then room for one more datagram
        public byte[] Buffer { get; } = new byte[MBusDeframer.MaxFrameLength + MaxDatagramLength];
        public int Buffered;

        // Written under the transport's lock, read without it by exception filters
        public volatile bool Closed;
        public volatile bool Receiving;

        /// <summary>
        /// Returns the first valid frame in the buffered bytes, if there is one, and keeps only
        /// the bytes after it, or the partial frame at the end, for later.
        /// </summary>
        public byte[]? TryTakeFrame()
        {
            ReadOnlySpan<byte> buffered = Buffer.AsSpan(0, Buffered);
            var found = MBusDeframer.TryReadFrame(ref buffered, out var frame);
            var result = found ? frame.ToArray() : null;

            buffered.CopyTo(Buffer);
            Buffered = buffered.Length;
            return result;
        }
    }
}
