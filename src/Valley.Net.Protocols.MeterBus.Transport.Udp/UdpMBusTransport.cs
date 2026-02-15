using System.Net;
using System.Net.Sockets;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// UDP transport for M-Bus communication using raw Socket.
/// Each M-Bus frame is one UDP datagram.
/// </summary>
public sealed class UdpMBusTransport : IMBusTransport
{
    private readonly string _host;
    private readonly int _port;
    private readonly TimeSpan _timeout;
    private Socket? _socket;
    private EndPoint? _remoteEp;
    private bool _disposed;

    public UdpMBusTransport(string host, int port, TimeSpan? timeout = null)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
        _timeout = timeout ?? TimeSpan.FromSeconds(5);
    }

    public ValueTask ConnectAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.ReceiveTimeout = (int)_timeout.TotalMilliseconds;
        _socket.SendTimeout = (int)_timeout.TotalMilliseconds;

        _remoteEp = new IPEndPoint(IPAddress.Parse(_host), _port);
        _socket.Connect(_remoteEp);

        return ValueTask.CompletedTask;
    }

    public async ValueTask SendFrameAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_socket is null)
            throw new InvalidOperationException("Transport is not connected");

        await _socket.SendAsync(frameBytes, SocketFlags.None, ct);
    }

    public async ValueTask<ReadOnlyMemory<byte>> ReceiveFrameAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_socket is null)
            throw new InvalidOperationException("Transport is not connected");

        var buffer = new byte[512];
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_timeout);

        var received = await _socket.ReceiveAsync(buffer, SocketFlags.None, timeoutCts.Token);
        return new ReadOnlyMemory<byte>(buffer, 0, received);
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _socket?.Dispose();
        }
        return ValueTask.CompletedTask;
    }
}
