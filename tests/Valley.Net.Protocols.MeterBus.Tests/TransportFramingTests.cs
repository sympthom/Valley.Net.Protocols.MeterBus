using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class TransportFramingTests
{
    // SND_UD control frame: 68 L L 68 C A CI CS 16
    private const string ControlFrame = "68 03 03 68 53 FE 51 A2 16";

    // The header of a 16-byte RSP_UD: left in the pipe, it would swallow the next reply
    private const string HalfFrame = "68 0A 0A 68 08 01 72";

    public enum Link
    {
        Tcp,
        Serial,
    }

    [TestMethod]
    [DataRow("00 E5", "E5", DisplayName = "Noise byte before ACK")]
    [DataRow("FF A3 00 10 5B 01 5C 16", "10 5B 01 5C 16", DisplayName = "Noise bytes before short frame")]
    [DataRow("10 E5 10 5B 01 5C 16", "E5", DisplayName = "Stray 0x10 without stop byte before ACK")]
    [DataRow("68 00 E5", "E5", DisplayName = "Stray 0x68 with L < 3 before ACK")]
    [DataRow("68 05 07 68 08 01 72 00 00 7B 16 E5", "E5", DisplayName = "Stray 0x68 with L != L' before ACK")]
    [DataRow("68 04 04 00 10 5B 01 5C 16", "10 5B 01 5C 16", DisplayName = "Stray 0x68 without second start before short frame")]
    [DataRow("68 03 03 68 53 FE 51 A2 00 " + ControlFrame, ControlFrame, DisplayName = "Long frame without stop byte before control frame")]
    [DataRow("10 5B 01 5D 16 E5", "E5", DisplayName = "Short frame with wrong checksum before ACK")]
    [DataRow("68 03 03 68 53 FE 51 A3 16 " + ControlFrame, ControlFrame, DisplayName = "Control frame with wrong checksum before control frame")]
    public async Task ReceiveFrame_NoiseBeforeFrame_ReturnsFrame(string sent, string expected)
    {
        await using var loopback = await Loopback.OpenAsync(Link.Tcp, TimeSpan.FromSeconds(2));

        await loopback.SendAsync(sent);
        var frame = await loopback.Transport.ReceiveFrameAsync();

        Assert.AreEqual(Hex(expected), Convert.ToHexString(frame.Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_NoiseBetweenFramesInOneWrite_ReturnsBothFrames()
    {
        await using var loopback = await Loopback.OpenAsync(Link.Tcp, TimeSpan.FromSeconds(2));

        await loopback.SendAsync("E5 00 10 5B 01 5C 16");

        Assert.AreEqual("E5", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_OneByteAtATime_ReturnsWholeFrame()
    {
        await using var loopback = await Loopback.OpenAsync(Link.Tcp, TimeSpan.FromSeconds(2));

        var receive = loopback.Transport.ReceiveFrameAsync().AsTask();
        foreach (var b in ControlFrame.HexToBytes())
        {
            await loopback.SendAsync([b]);
            await Task.Delay(5);
        }

        Assert.AreEqual(Hex(ControlFrame), Convert.ToHexString((await receive).Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_ContinuousJunk_TimesOutAndKeepsNothing()
    {
        await using var loopback = await Loopback.OpenAsync(Link.Tcp, TimeSpan.FromMilliseconds(500));

        // Contains every start byte, but never a valid header or stop byte
        var junk = new byte[256 * 1024];
        ReadOnlySpan<byte> pattern = [0x00, 0x68, 0xFF, 0x10, 0x55];
        for (var i = 0; i < junk.Length; i++)
            junk[i] = pattern[i % pattern.Length];
        await loopback.SendAsync(junk);

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await loopback.Transport.ReceiveFrameAsync());

        Assert.AreEqual(0L, loopback.BufferedByteCount());

        await loopback.SendAsync(ControlFrame);
        Assert.AreEqual(Hex(ControlFrame), Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task ReceiveFrame_ChecksumWrong_SkipsCandidate(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(2));

        await loopback.SendAsync("10 5B 01 5D 16 00 10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task ReceiveFrame_NoReply_ThrowsTimeoutException(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await loopback.Transport.ReceiveFrameAsync());
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task ReceiveFrame_CallerCancels_ThrowsOperationCanceledException(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await loopback.Transport.ReceiveFrameAsync(cts.Token));
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task ReceiveFrame_TimeoutAfterPartialFrame_NextReceiveReturnsNewFrame(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromMilliseconds(200));

        await loopback.SendAsync(HalfFrame);
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await loopback.Transport.ReceiveFrameAsync());
        Assert.AreEqual(0L, loopback.BufferedByteCount());

        await loopback.SendAsync("10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task ReceiveFrame_CallerCancelsAfterPartialFrame_NextReceiveReturnsNewFrame(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(30));

        await loopback.SendAsync(HalfFrame);
        using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await loopback.Transport.ReceiveFrameAsync(cts.Token));

        await loopback.SendAsync("10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    // A peer that stops reading fills the socket buffers; Socket.SendTimeout does not bound async sends
    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task SendFrame_PeerStopsReading_ThrowsTimeoutException(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromMilliseconds(200));

        // Without a send timeout this never completes, so fail instead of hanging the run
        var send = loopback.Transport.SendFrameAsync(new byte[64 * 1024 * 1024]).AsTask();
        Assert.AreSame(send, await Task.WhenAny(send, Task.Delay(TimeSpan.FromSeconds(10))), "Send did not time out");

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => send);
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task SendFrame_CallerCancels_ThrowsOperationCanceledException(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(30));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await loopback.Transport.SendFrameAsync(new byte[64 * 1024 * 1024], cts.Token));
    }

    [TestMethod]
    public async Task DiscardInput_LateReply_NextReceiveReturnsNewFrame()
    {
        await using var loopback = await Loopback.OpenAsync(Link.Tcp, TimeSpan.FromMilliseconds(300));

        // The rest of a late reply reaches the socket after the receive gave up. Left there,
        // it would be returned instead of the new frame.
        await loopback.SendAsync(HalfFrame);
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await loopback.Transport.ReceiveFrameAsync());
        await loopback.SendAsync("01 02 03 04 05 06 07 97 16 E5");
        await Task.Delay(200);

        await loopback.Transport.DiscardInputAsync();
        await loopback.SendAsync("10 5B 01 5C 16");

        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Udp_DiscardInput_StaleDatagram_NextReceiveReturnsNewFrame()
    {
        using var gateway = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        gateway.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)gateway.LocalEndPoint!).Port;

        await using IMBusTransport transport = new UdpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        await transport.ConnectAsync();

        await transport.SendFrameAsync("10 5B 01 5C 16".HexToBytes());
        var request = await gateway.ReceiveFromAsync(new byte[512], SocketFlags.None, new IPEndPoint(IPAddress.Any, 0));

        await gateway.SendToAsync(ControlFrame.HexToBytes(), SocketFlags.None, request.RemoteEndPoint);
        await Task.Delay(200);

        await transport.DiscardInputAsync();
        await gateway.SendToAsync(new byte[] { 0xE5 }, SocketFlags.None, request.RemoteEndPoint);

        Assert.AreEqual("E5", Convert.ToHexString((await transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task Dispose_DuringReceive_ThrowsObjectDisposedException(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(30));

        var receive = loopback.Transport.ReceiveFrameAsync().AsTask();
        await loopback.Transport.DisposeAsync();

        Assert.AreSame(receive, await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5))), "Receive did not end");
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => receive);
    }

    [TestMethod]
    [DataRow(Link.Tcp)]
    [DataRow(Link.Serial)]
    public async Task Dispose_ConcurrentAndRepeated_IsIdempotent(Link link)
    {
        await using var loopback = await Loopback.OpenAsync(link, TimeSpan.FromSeconds(2));
        var transport = (IDisposable)loopback.Transport;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(async () =>
        {
            if (i % 2 == 0)
                transport.Dispose();
            else
                await loopback.Transport.DisposeAsync();
        })));
        transport.Dispose();

        Assert.IsFalse(IsConnected(loopback.Transport));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await loopback.Transport.ReceiveFrameAsync());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await loopback.Transport.ConnectAsync());
    }

    [TestMethod]
    public async Task Tcp_IsConnected_FollowsConnectionState()
    {
        using var listener = Listen(IPAddress.Loopback, out var port);
        await using var transport = new TcpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));
        Assert.IsFalse(transport.IsConnected);

        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var gateway = await accept;
        Assert.IsTrue(transport.IsConnected);

        gateway.Shutdown(SocketShutdown.Both);
        await Assert.ThrowsExactlyAsync<IOException>(async () => await transport.ReceiveFrameAsync());
        Assert.IsFalse(transport.IsConnected);

        await transport.DisposeAsync();
        Assert.IsFalse(transport.IsConnected);
    }

    [TestMethod]
    public async Task Tcp_ConnectAgain_ClosesPreviousConnection()
    {
        using var listener = Listen(IPAddress.Loopback, out var port);
        await using var transport = new TcpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));

        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var first = await accept;

        accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var second = await accept;

        // A gateway that takes one client at a time needs the first session gone
        var read = first.ReceiveAsync(new byte[16], SocketFlags.None);
        Assert.AreSame(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(5))), "First connection still open");
        Assert.AreEqual(0, await read);

        await second.SendAsync(new byte[] { 0xE5 }, SocketFlags.None);
        Assert.AreEqual("E5", Convert.ToHexString((await transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Tcp_Dispose_ClosesConnection()
    {
        using var listener = Listen(IPAddress.Loopback, out var port);
        var transport = new TcpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));

        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var gateway = await accept;

        using (transport)
        {
        }

        var read = gateway.ReceiveAsync(new byte[16], SocketFlags.None);
        Assert.AreSame(read, await Task.WhenAny(read, Task.Delay(TimeSpan.FromSeconds(5))), "Connection still open");
        Assert.AreEqual(0, await read);
    }

    [TestMethod]
    public async Task Tcp_ConnectFails_StaysDisconnectedAndCanConnectLater()
    {
        int port;
        using (Listen(IPAddress.Loopback, out port))
        {
        }

        await using var transport = new TcpMBusTransport("127.0.0.1", port, TimeSpan.FromSeconds(2));

        await Assert.ThrowsAsync<SocketException>(async () => await transport.ConnectAsync());
        Assert.IsFalse(transport.IsConnected);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await transport.SendFrameAsync(new byte[] { 0xE5 }));

        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var gateway = await accept;

        Assert.IsTrue(transport.IsConnected);
    }

    [TestMethod]
    public async Task Tcp_Hostname_Connects()
    {
        // localhost may resolve to ::1 first; the transport must go on to 127.0.0.1
        using var listener = Listen(IPAddress.Loopback, out var port);
        await using var transport = new TcpMBusTransport("localhost", port, TimeSpan.FromSeconds(2));

        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var gateway = await accept;
        await gateway.SendAsync(new byte[] { 0xE5 }, SocketFlags.None);

        Assert.AreEqual("E5", Convert.ToHexString((await transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Tcp_IPv6Literal_Connects()
    {
        using var listener = ListenIPv6Loopback(out var port);
        await using var transport = new TcpMBusTransport("::1", port, TimeSpan.FromSeconds(2));

        var accept = listener.AcceptSocketAsync();
        await transport.ConnectAsync();
        using var gateway = await accept;
        await gateway.SendAsync(new byte[] { 0xE5 }, SocketFlags.None);

        Assert.AreEqual("E5", Convert.ToHexString((await transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(65536)]
    public void Tcp_PortOutOfRange_Throws(int port)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new TcpMBusTransport("127.0.0.1", port));
    }

    [TestMethod]
    public void Serial_DefaultOptions_Are8E1At2400WithTimeoutsFromBaudRate()
    {
        var options = new SerialMBusTransportOptions();
        var transport = new SerialMBusTransport("COM1");

        Assert.AreEqual(2400, options.BaudRate);
        Assert.AreEqual(System.IO.Ports.Parity.Even, options.Parity);
        Assert.AreEqual(8, options.DataBits);
        Assert.AreEqual(System.IO.Ports.StopBits.One, options.StopBits);
        Assert.IsFalse(options.EchoSuppression);

        // 330 bit times + 50 ms + 100 ms margin
        Assert.AreEqual(TimeSpan.FromMilliseconds(137.5 + 50 + 100), transport.ResponseTimeout);
        // 33 bit times + 100 ms margin
        Assert.AreEqual(TimeSpan.FromMilliseconds(13.75 + 100), transport.InterCharacterTimeout);
        // A 261-byte frame of 11-bit characters takes 1196.25 ms
        Assert.AreEqual(TimeSpan.FromMilliseconds(1196.25 + 100), transport.WriteTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(287.5 + 2 * 1196.25), transport.FrameTimeout);
    }

    [TestMethod]
    public void Serial_300Baud_MaximumFrameFitsInFrameTimeout()
    {
        var transport = new SerialMBusTransport("COM1", 300);

        Assert.AreEqual(TimeSpan.FromMilliseconds(1100 + 50 + 100), transport.ResponseTimeout);

        // 261 bytes x 11 bits / 300 baud = 9.57 s, after the reply started as late as allowed
        var maxFrame = TimeSpan.FromSeconds(261 * 11 / 300.0);
        Assert.IsGreaterThanOrEqualTo(TimeSpan.FromSeconds(330 / 300.0 + 0.05) + maxFrame, transport.FrameTimeout);
        Assert.IsGreaterThanOrEqualTo(maxFrame, transport.WriteTimeout);
    }

    [TestMethod]
    public void Serial_CharacterFormat_ScalesTimeouts()
    {
        // 8N1 is 10 bits per character
        var transport = new SerialMBusTransport("COM1", new SerialMBusTransportOptions
        {
            BaudRate = 9600,
            Parity = System.IO.Ports.Parity.None,
        });

        Assert.AreEqual(TimeSpan.FromMilliseconds(261 * 10 / 9.6 + 100), transport.WriteTimeout);
    }

    [TestMethod]
    public void Serial_ExplicitTimeouts_OverrideDerivedOnes()
    {
        var transport = new SerialMBusTransport("COM1", new SerialMBusTransportOptions
        {
            ResponseTimeout = TimeSpan.FromSeconds(2),
            InterCharacterTimeout = TimeSpan.FromMilliseconds(500),
            WriteTimeout = TimeSpan.FromSeconds(3),
        });

        Assert.AreEqual(TimeSpan.FromSeconds(2), transport.ResponseTimeout);
        Assert.AreEqual(TimeSpan.FromMilliseconds(500), transport.InterCharacterTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(3), transport.WriteTimeout);
        Assert.AreEqual(TimeSpan.FromSeconds(2) + TimeSpan.FromMilliseconds(2 * 1196.25), transport.FrameTimeout);
    }

    [TestMethod]
    public void Serial_InvalidOptions_Throw()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SerialMBusTransport("COM1", 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SerialMBusTransport("COM1", new SerialMBusTransportOptions { ResponseTimeout = TimeSpan.Zero }));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SerialMBusTransport("COM1", null!));
    }

    [TestMethod]
    public async Task Serial_GapInsideFrame_TimesOutAtInterCharacterTimeout()
    {
        await using var loopback = await PtyLoopback.OpenAsync(new SerialMBusTransportOptions
        {
            ResponseTimeout = TimeSpan.FromSeconds(30),
            InterCharacterTimeout = TimeSpan.FromMilliseconds(50),
        });

        await loopback.SendAsync(HalfFrame);
        var receive = loopback.Transport.ReceiveFrameAsync().AsTask();
        Assert.AreSame(receive, await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5))), "Partial frame was not dropped");
        await Assert.ThrowsExactlyAsync<TimeoutException>(() => receive);
        Assert.AreEqual(0L, loopback.BufferedByteCount());

        await loopback.SendAsync("10 5B 01 5C 16");
        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Serial_NoiseBeforeReply_KeepsWaitingForResponseTimeout()
    {
        var responseTimeout = TimeSpan.FromMilliseconds(300);
        await using var loopback = await PtyLoopback.OpenAsync(new SerialMBusTransportOptions
        {
            ResponseTimeout = responseTimeout,
            InterCharacterTimeout = TimeSpan.FromMilliseconds(10),
        });

        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        await loopback.SendAsync("00 FF 00");
        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await loopback.Transport.ReceiveFrameAsync());

        Assert.IsGreaterThanOrEqualTo(responseTimeout - TimeSpan.FromMilliseconds(20), System.Diagnostics.Stopwatch.GetElapsedTime(started));
    }

    [TestMethod]
    public async Task Serial_EchoSuppression_DropsEchoedRequest()
    {
        await using var loopback = await PtyLoopback.OpenAsync(new SerialMBusTransportOptions { EchoSuppression = true });

        await loopback.RequestAsync("10 40 01 41 16");
        await loopback.SendAsync("10 40 01 41 16 E5");

        Assert.AreEqual("E5", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Serial_EchoSuppression_NoEcho_ReturnsReply()
    {
        await using var loopback = await PtyLoopback.OpenAsync(new SerialMBusTransportOptions { EchoSuppression = true });

        await loopback.RequestAsync("10 5B 01 5C 16");
        await loopback.SendAsync(ControlFrame);
        Assert.AreEqual(Hex(ControlFrame), Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));

        // Only the frame right after the request can be its echo
        await loopback.SendAsync("10 5B 01 5C 16");
        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Serial_EchoSuppressionOff_ReturnsEcho()
    {
        await using var loopback = await PtyLoopback.OpenAsync(new SerialMBusTransportOptions());

        await loopback.RequestAsync("10 40 01 41 16");
        await loopback.SendAsync("10 40 01 41 16 E5");

        Assert.AreEqual("1040014116", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task Serial_IsConnected_FollowsPortState()
    {
        await using var loopback = await PtyLoopback.OpenAsync(TimeSpan.FromSeconds(2));
        Assert.IsTrue(IsConnected(loopback.Transport));

        await loopback.Transport.ConnectAsync();
        Assert.IsTrue(IsConnected(loopback.Transport));

        await loopback.Transport.DisposeAsync();
        Assert.IsFalse(IsConnected(loopback.Transport));
    }

    // A Windows serial read that times out in the driver returns 0 bytes; a PipeReader would take that as end of stream
    [TestMethod]
    public async Task Serial_ReadStream_DriverTimeoutWithoutBytes_IsNotEndOfStream()
    {
        var reader = PipeReader.Create(new SerialReadStream(new WindowsSerialStream([], [], [0xE5]), () => true));

        var result = await reader.ReadAsync();

        Assert.IsFalse(result.IsCompleted);
        Assert.AreEqual("E5", Convert.ToHexString(result.Buffer.FirstSpan));
    }

    [TestMethod]
    public async Task Serial_ReadStream_DriverTimeouts_StillEndOnCancellation()
    {
        var reader = PipeReader.Create(new SerialReadStream(new WindowsSerialStream(), () => true));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await reader.ReadAsync(cts.Token));
    }

    [TestMethod]
    public async Task Serial_ReadStream_PortClosed_IsEndOfStream()
    {
        var reader = PipeReader.Create(new SerialReadStream(new WindowsSerialStream(), () => false));

        Assert.IsTrue((await reader.ReadAsync()).IsCompleted);
    }

    private static bool IsConnected(IMBusTransport transport) => transport switch
    {
        TcpMBusTransport tcp => tcp.IsConnected,
        SerialMBusTransport serial => serial.IsConnected,
        UdpMBusTransport udp => udp.IsConnected,
        _ => throw new ArgumentOutOfRangeException(nameof(transport)),
    };

    private static TcpListener Listen(IPAddress address, out int port)
    {
        var listener = new TcpListener(address, 0);
        listener.Start();
        port = ((IPEndPoint)listener.LocalEndpoint).Port;
        return listener;
    }

    private static TcpListener ListenIPv6Loopback(out int port)
    {
        if (!Socket.OSSupportsIPv6)
            Assert.Inconclusive("IPv6 is not available");

        try
        {
            return Listen(IPAddress.IPv6Loopback, out port);
        }
        catch (SocketException ex)
        {
            Assert.Inconclusive($"No IPv6 loopback: {ex.SocketErrorCode}");
            throw;
        }
    }

    private static string Hex(string spaced) => spaced.Replace(" ", "");

    /// <summary>
    /// Reads the way SerialStream does on Windows: each read returns the next queued chunk, an empty
    /// chunk or an empty queue is a driver timeout (0 bytes), and the token is ignored.
    /// </summary>
    private sealed class WindowsSerialStream(params byte[][] reads) : Stream
    {
        private readonly Queue<byte[]> _reads = new(reads);

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (!_reads.TryDequeue(out var chunk))
                return 0;

            chunk.CopyTo(buffer);
            return chunk.Length;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    /// <summary>
    /// A transport connected to a local peer that plays the gateway or the bus.
    /// </summary>
    private abstract class Loopback : IAsyncDisposable
    {
        protected Loopback(IMBusTransport transport) => Transport = transport;

        // Typed as the interface, the way MBusMaster sees it, so default members resolve the same way
        public IMBusTransport Transport { get; }

        public static async Task<Loopback> OpenAsync(Link link, TimeSpan timeout) => link switch
        {
            Link.Tcp => await TcpLoopback.ConnectAsync(timeout),
            Link.Serial => await PtyLoopback.OpenAsync(timeout),
            _ => throw new ArgumentOutOfRangeException(nameof(link)),
        };

        public Task SendAsync(string hex) => SendAsync(hex.HexToBytes());

        public abstract Task SendAsync(byte[] bytes);

        /// <summary>
        /// Reads what the transport sent. A serial write only completes once the bus side has read it.
        /// </summary>
        public abstract Task<byte[]> ReadAsync(int count);

        /// <summary>
        /// Sends a request through the transport and reads it on the bus side.
        /// </summary>
        public async Task RequestAsync(string hex)
        {
            var request = hex.HexToBytes();
            var send = Transport.SendFrameAsync(request).AsTask();
            CollectionAssert.AreEqual(request, await ReadAsync(request.Length));
            await send;
        }

        /// <summary>
        /// Bytes the transport's PipeReader holds without having returned them as a frame.
        /// </summary>
        public long BufferedByteCount()
        {
            var connection = Transport.GetType()
                .GetField("_connection", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(Transport)!;
            var reader = (PipeReader)connection.GetType().GetProperty("Reader")!.GetValue(connection)!;

            // A cancelled read hands back the buffer even when the transport has examined all of it
            reader.CancelPendingRead();
            Assert.IsTrue(reader.TryRead(out var result));

            var length = result.Buffer.Length;
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            return length;
        }

        public abstract ValueTask DisposeAsync();
    }

    private sealed class TcpLoopback : Loopback
    {
        private readonly TcpListener _listener;
        private readonly Socket _gateway;

        private TcpLoopback(TcpListener listener, IMBusTransport transport, Socket gateway)
            : base(transport)
        {
            _listener = listener;
            _gateway = gateway;
        }

        public static async Task<Loopback> ConnectAsync(TimeSpan timeout)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var transport = new TcpMBusTransport("127.0.0.1", port, timeout);
            var accept = listener.AcceptSocketAsync();
            await transport.ConnectAsync();

            var gateway = await accept;
            gateway.NoDelay = true;

            return new TcpLoopback(listener, transport, gateway);
        }

        public override async Task SendAsync(byte[] bytes) => await _gateway.SendAsync(bytes, SocketFlags.None);

        public override async Task<byte[]> ReadAsync(int count)
        {
            var bytes = new byte[count];
            for (var read = 0; read < count;)
                read += await _gateway.ReceiveAsync(bytes.AsMemory(read), SocketFlags.None);
            return bytes;
        }

        public override async ValueTask DisposeAsync()
        {
            await Transport.DisposeAsync();
            _gateway.Dispose();
            _listener.Dispose();
        }
    }

    /// <summary>
    /// A SerialMBusTransport on the slave side of a pseudo-terminal; the test writes the bus side to the master.
    /// </summary>
    private sealed class PtyLoopback : Loopback
    {
        private readonly FileStream _bus;
        private readonly SafeFileHandle _slave;

        private PtyLoopback(IMBusTransport transport, FileStream bus, SafeFileHandle slave)
            : base(transport)
        {
            _bus = bus;
            _slave = slave;
        }

        public static Task<Loopback> OpenAsync(TimeSpan timeout) => OpenAsync(new SerialMBusTransportOptions
        {
            ResponseTimeout = timeout,
            InterCharacterTimeout = timeout,
            WriteTimeout = timeout,
        });

        public static async Task<Loopback> OpenAsync(SerialMBusTransportOptions options)
        {
            if (OperatingSystem.IsWindows())
                Assert.Inconclusive("Serial loopback needs a Unix pseudo-terminal");

            var name = new byte[256];
            int master, slave;
            try
            {
                if (openpty(out master, out slave, name, IntPtr.Zero, IntPtr.Zero) != 0)
                    throw new IOException($"errno {Marshal.GetLastPInvokeError()}");
            }
            catch (Exception ex) when (ex is IOException or DllNotFoundException or EntryPointNotFoundException)
            {
                Assert.Inconclusive($"No pseudo-terminal: {ex.Message}");
                throw;
            }

            // The slave stays open until disposal so the master does not see a hang-up in between
            var slaveHandle = new SafeFileHandle(slave, ownsHandle: true);
            var bus = new FileStream(new SafeFileHandle(master, ownsHandle: true), FileAccess.ReadWrite, bufferSize: 0);

            var transport = new SerialMBusTransport(Encoding.ASCII.GetString(name).TrimEnd('\0'), options);
            await transport.ConnectAsync();

            return new PtyLoopback(transport, bus, slaveHandle);
        }

        public override Task SendAsync(byte[] bytes)
        {
            _bus.Write(bytes);
            return Task.CompletedTask;
        }

        public override async Task<byte[]> ReadAsync(int count)
        {
            var bytes = new byte[count];
            await _bus.ReadExactlyAsync(bytes);
            return bytes;
        }

        public override async ValueTask DisposeAsync()
        {
            // Closing the bus side first ends any write still waiting for it to read; until then
            // SerialPort.Close blocks on that write
            await _bus.DisposeAsync();
            await Transport.DisposeAsync();
            _slave.Dispose();
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int openpty(out int master, out int slave, byte[] name, IntPtr termios, IntPtr winsize);
    }
}
