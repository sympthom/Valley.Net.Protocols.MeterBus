namespace Valley.Net.Protocols.MeterBus.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public sealed class TcpTransportTests
{
    [TestMethod]
    [Ignore("Requires running M-Bus TCP gateway")]
    public async Task ConnectAndPing_WithRealGateway()
    {
        await using var transport = new TcpMBusTransport("127.0.0.1", 10001);
        var parser = new FrameParser();
        var serializer = new FrameSerializer();
        var mapper = new PacketMapper(new VifLookupService());

        await using var master = new MBusMaster(transport, parser, serializer, mapper);
        await transport.ConnectAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await master.PingAsync(0x01, cts.Token);
        // Result depends on whether a device is connected
    }
}
