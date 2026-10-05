using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class TransportFramingTests
{
    // SND_UD control frame: 68 L L 68 C A CI CS 16
    private const string ControlFrame = "68 03 03 68 53 FE 51 A2 16";

    [TestMethod]
    [DataRow("00 E5", "E5", DisplayName = "Noise byte before ACK")]
    [DataRow("FF A3 00 10 5B 01 5C 16", "10 5B 01 5C 16", DisplayName = "Noise bytes before short frame")]
    [DataRow("10 E5 10 5B 01 5C 16", "E5", DisplayName = "Stray 0x10 without stop byte before ACK")]
    [DataRow("68 00 E5", "E5", DisplayName = "Stray 0x68 with L < 3 before ACK")]
    [DataRow("68 05 07 68 08 01 72 00 00 7B 16 E5", "E5", DisplayName = "Stray 0x68 with L != L' before ACK")]
    [DataRow("68 04 04 00 10 5B 01 5C 16", "10 5B 01 5C 16", DisplayName = "Stray 0x68 without second start before short frame")]
    [DataRow("68 03 03 68 53 FE 51 A2 00 " + ControlFrame, ControlFrame, DisplayName = "Long frame without stop byte before control frame")]
    public async Task ReceiveFrame_NoiseBeforeFrame_ReturnsFrame(string sent, string expected)
    {
        await using var loopback = await TcpLoopback.ConnectAsync(TimeSpan.FromSeconds(2));

        await loopback.SendAsync(sent);
        var frame = await loopback.Transport.ReceiveFrameAsync();

        Assert.AreEqual(Hex(expected), Convert.ToHexString(frame.Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_NoiseBetweenFramesInOneWrite_ReturnsBothFrames()
    {
        await using var loopback = await TcpLoopback.ConnectAsync(TimeSpan.FromSeconds(2));

        await loopback.SendAsync("E5 00 10 5B 01 5C 16");

        Assert.AreEqual("E5", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
        Assert.AreEqual("105B015C16", Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_OneByteAtATime_ReturnsWholeFrame()
    {
        await using var loopback = await TcpLoopback.ConnectAsync(TimeSpan.FromSeconds(2));

        var receive = loopback.Transport.ReceiveFrameAsync().AsTask();
        foreach (var b in ControlFrame.HexToBytes())
        {
            await loopback.Gateway.SendAsync(new[] { b }, SocketFlags.None);
            await Task.Delay(5);
        }

        Assert.AreEqual(Hex(ControlFrame), Convert.ToHexString((await receive).Span));
    }

    [TestMethod]
    public async Task ReceiveFrame_ContinuousJunk_BuffersAtMostOnePartialFrame()
    {
        await using var loopback = await TcpLoopback.ConnectAsync(TimeSpan.FromMilliseconds(500));

        // Contains every start byte, but never a valid header or stop byte
        var junk = new byte[256 * 1024];
        ReadOnlySpan<byte> pattern = [0x00, 0x68, 0xFF, 0x10, 0x55];
        for (var i = 0; i < junk.Length; i++)
            junk[i] = pattern[i % pattern.Length];
        await loopback.Gateway.SendAsync(junk, SocketFlags.None);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await loopback.Transport.ReceiveFrameAsync());

        Assert.IsLessThan(MBusConstants.FRAME_FIXED_SIZE_LONG + 255, loopback.BufferedByteCount());

        await loopback.SendAsync(ControlFrame);
        Assert.AreEqual(Hex(ControlFrame), Convert.ToHexString((await loopback.Transport.ReceiveFrameAsync()).Span));
    }

    [TestMethod]
    public async Task DiscardInput_LateReply_NextReceiveReturnsNewFrame()
    {
        await using var loopback = await TcpLoopback.ConnectAsync(TimeSpan.FromMilliseconds(300));

        // The first half of a late 16-byte reply reaches the pipe before the receive times out,
        // the rest only reaches the socket. Left in the pipe, its header would swallow the new frame;
        // left in the socket, the late reply would be returned instead of the new frame.
        await loopback.SendAsync("68 0A 0A 68 08 01 72");
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await loopback.Transport.ReceiveFrameAsync());
        await loopback.SendAsync("01 02 03 04 05 06 07 97 16");
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

    private static string Hex(string spaced) => spaced.Replace(" ", "");

    /// <summary>
    /// A TcpMBusTransport connected to a local listener that plays the gateway.
    /// </summary>
    private sealed class TcpLoopback : IAsyncDisposable
    {
        private readonly TcpListener _listener;

        private TcpLoopback(TcpListener listener, IMBusTransport transport, Socket gateway)
        {
            _listener = listener;
            Transport = transport;
            Gateway = gateway;
        }

        // Typed as the interface, the way MBusMaster sees it, so default members resolve the same way
        public IMBusTransport Transport { get; }

        public Socket Gateway { get; }

        public static async Task<TcpLoopback> ConnectAsync(TimeSpan timeout)
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

        public async Task SendAsync(string hex) => await Gateway.SendAsync(hex.HexToBytes(), SocketFlags.None);

        /// <summary>
        /// Bytes the transport's PipeReader holds without having returned them as a frame.
        /// </summary>
        public long BufferedByteCount()
        {
            var reader = (PipeReader)typeof(TcpMBusTransport)
                .GetField("_reader", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(Transport)!;

            // A cancelled read hands back the buffer even when the transport has examined all of it
            reader.CancelPendingRead();
            Assert.IsTrue(reader.TryRead(out var result));

            var length = result.Buffer.Length;
            reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);
            return length;
        }

        public async ValueTask DisposeAsync()
        {
            await Transport.DisposeAsync();
            Gateway.Dispose();
            _listener.Dispose();
        }
    }
}
