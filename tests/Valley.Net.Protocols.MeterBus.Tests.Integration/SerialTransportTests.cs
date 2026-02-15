namespace Valley.Net.Protocols.MeterBus.Tests.Integration;

[TestClass]
[TestCategory("Integration")]
public sealed class SerialTransportTests
{
    [TestMethod]
    [Ignore("Requires physical serial port with M-Bus device")]
    public async Task ConnectAndPing_WithSerialDevice()
    {
        await using var transport = new SerialMBusTransport("/dev/ttyUSB0", 2400);
        var parser = new FrameParser();
        var serializer = new FrameSerializer();
        var mapper = new PacketMapper(new VifLookupService());

        await using var master = new MBusMaster(transport, parser, serializer, mapper);
        await transport.ConnectAsync();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result = await master.PingAsync(0x01, cts.Token);
    }
}
