using System.Net;
using System.Net.Sockets;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class UdpTransportTests
{
    // SND_UD control frame: 68 L L 68 C A CI CS 16
    private const string ControlFrame = "68 03 03 68 53 FE 51 A2 16";

    [TestMethod]
    public async Task ReceiveFrame_NoReply_ThrowsTimeoutException()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromMilliseconds(100));
        await transport.ConnectAsync();

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await transport.ReceiveFrameAsync());
    }

    [TestMethod]
    public async Task ReceiveFrame_CallerCancels_ThrowsOperationCanceledException()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(30));
        await transport.ConnectAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await transport.ReceiveFrameAsync(cts.Token));
    }

    [TestMethod]
    public async Task ReceiveFrame_ReplyAfterTimeout_IsReturnedByNextReceive()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromMilliseconds(100));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await transport.ReceiveFrameAsync());

        // A timed-out receive must leave the socket usable
        await ReplyAsync(gateway, peer, "E5");

        Assert.AreEqual("E5", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_FrameSplitAcrossDatagrams_ReturnsWholeFrame()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(gateway, peer, "68 03 03 68");
        await ReplyAsync(gateway, peer, "53 FE");
        await ReplyAsync(gateway, peer, "51 A2 16");

        Assert.AreEqual(Hex(ControlFrame), await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_TwoFramesInOneDatagram_ReturnsEach()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(gateway, peer, "E5 " + ControlFrame);

        Assert.AreEqual("E5", await ReceiveHexAsync(transport));
        Assert.AreEqual(Hex(ControlFrame), await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_GarbageDatagram_IsSkipped()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(gateway, peer, "00 01");
        await ReplyAsync(gateway, peer, "10 5B 01 5D 16");
        await ReplyAsync(gateway, peer, "10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_TimeoutAfterPartialFrame_NextReceiveReturnsNewFrame()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromMilliseconds(200));
        await transport.ConnectAsync();

        // The header of a 16-byte RSP_UD: kept, it would swallow the ACK as frame data
        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(gateway, peer, "68 0A 0A 68 08 01 72");
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await transport.ReceiveFrameAsync());

        await ReplyAsync(gateway, peer, "E5");

        Assert.AreEqual("E5", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_DatagramFromOtherEndpoint_IsIgnored()
    {
        using var gateway = BindGateway(out var port);
        using var stranger = BindGateway(out _);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(stranger, peer, "E5");
        await ReplyAsync(gateway, peer, "10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task ReceiveFrame_NothingListening_ThrowsIOException()
    {
        int port;
        using (BindGateway(out port))
        {
        }

        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(30));
        await transport.ConnectAsync();
        await transport.SendFrameAsync("10 5B 01 5C 16".HexToBytes());

        // The ICMP port unreachable arrives on the receive (or a later send), long before the timeout
        var receive = transport.ReceiveFrameAsync().AsTask();
        Assert.AreSame(receive, await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5))), "Receive did not fail");
        var ex = await Assert.ThrowsExactlyAsync<IOException>(() => receive);
        Assert.IsInstanceOfType<SocketException>(ex.InnerException);
    }

    [TestMethod]
    public async Task Connect_Hostname_ResolvesAndConnects()
    {
        // The transport takes the first address, so the gateway must listen there
        var address = (await Dns.GetHostAddressesAsync("localhost"))[0];
        using var gateway = BindGateway(out var port, address);
        await using IMBusTransport transport = new UdpMBusTransport("localhost", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        var peer = await RequestAsync(transport, gateway);
        await ReplyAsync(gateway, peer, "E5");

        Assert.AreEqual("E5", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task Connect_IPv6Literal_Connects()
    {
        if (!Socket.OSSupportsIPv6)
            Assert.Inconclusive("IPv6 is not available");

        Socket gateway;
        int port;
        try
        {
            gateway = BindGateway(out port, IPAddress.IPv6Loopback);
        }
        catch (SocketException ex)
        {
            Assert.Inconclusive($"No IPv6 loopback: {ex.SocketErrorCode}");
            throw;
        }

        using (gateway)
        {
            await using IMBusTransport transport = new UdpMBusTransport("::1", port, TimeSpan.FromSeconds(2));
            await transport.ConnectAsync();

            var peer = await RequestAsync(transport, gateway);
            await ReplyAsync(gateway, peer, "E5");

            Assert.AreEqual("E5", await ReceiveHexAsync(transport));
        }
    }

    [TestMethod]
    public async Task ConnectAgain_UsesNewSocket()
    {
        using var gateway = BindGateway(out var port);
        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();
        var first = await RequestAsync(transport, gateway);

        await transport.ConnectAsync();
        var second = await RequestAsync(transport, gateway);

        // The old socket is closed, not left bound
        Assert.AreNotEqual(first, second);
        await ReplyAsync(gateway, second, "E5");
        Assert.AreEqual("E5", await ReceiveHexAsync(transport));
    }

    [TestMethod]
    public async Task IsConnected_FollowsLifetime()
    {
        using var gateway = BindGateway(out var port);
        var transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        Assert.IsFalse(transport.IsConnected);

        await transport.ConnectAsync();
        Assert.IsTrue(transport.IsConnected);

        transport.Dispose();
        Assert.IsFalse(transport.IsConnected);
    }

    [TestMethod]
    public async Task Dispose_DuringReceive_ThrowsObjectDisposedException()
    {
        using var gateway = BindGateway(out var port);
        var transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(30));
        await transport.ConnectAsync();

        var receive = transport.ReceiveFrameAsync().AsTask();
        await transport.DisposeAsync();

        Assert.AreSame(receive, await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5))), "Receive did not end");
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => receive);
    }

    [TestMethod]
    public async Task Dispose_ConcurrentAndRepeated_IsIdempotent()
    {
        using var gateway = BindGateway(out var port);
        var transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
        {
            if (i % 2 == 0)
                transport.Dispose();
            else
                await transport.DisposeAsync();
        })));
        transport.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await transport.SendFrameAsync(new byte[] { 0xE5 }));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await transport.ConnectAsync());
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(65536)]
    public void PortOutOfRange_Throws(int port)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new UdpMBusTransport("127.0.0.1", port));
    }

    private static Socket BindGateway(out int port, IPAddress? address = null)
    {
        address ??= IPAddress.Loopback;
        var gateway = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        try
        {
            gateway.Bind(new IPEndPoint(address, 0));
        }
        catch
        {
            gateway.Dispose();
            throw;
        }

        port = ((IPEndPoint)gateway.LocalEndPoint!).Port;
        return gateway;
    }

    /// <summary>
    /// Sends a request from the transport and returns where the gateway saw it come from.
    /// </summary>
    private static async Task<EndPoint> RequestAsync(IMBusTransport transport, Socket gateway)
    {
        await transport.SendFrameAsync("10 5B 01 5C 16".HexToBytes());

        var any = gateway.AddressFamily == AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any;
        var request = await gateway.ReceiveFromAsync(new byte[512], SocketFlags.None, new IPEndPoint(any, 0));
        return request.RemoteEndPoint;
    }

    private static async Task ReplyAsync(Socket from, EndPoint to, string hex) =>
        await from.SendToAsync(hex.HexToBytes(), SocketFlags.None, to);

    private static async Task<string> ReceiveHexAsync(IMBusTransport transport) =>
        Convert.ToHexString((await transport.ReceiveFrameAsync()).Span);

    private static string Hex(string spaced) => spaced.Replace(" ", "");
}
