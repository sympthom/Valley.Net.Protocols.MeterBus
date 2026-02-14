using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Bindings.Udp;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus.Test;

/// <summary>
/// Integration tests requiring a real M-Bus UDP collector.
/// These tests are excluded from CI runs.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class UdpTests
{
    private const string CollectorIpAddress = "192.168.1.135";
    private const int CollectorPort = 502;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    [TestMethod]
    public async Task Meter_Should_Respond_With_Ack_When_Sending_SND_NKE()
    {
        var resetEvent = new AutoResetEvent(false);

        var binding = new UdpBinding(new IPEndPoint(IPAddress.Parse(CollectorIpAddress), CollectorPort), new MeterbusFrameSerializer());
        binding.PacketReceived += (sender, e) => resetEvent.Set();

        await binding.ConnectAsync();

        await binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));

        Assert.IsTrue(resetEvent.WaitOne(Timeout));
    }

    [TestMethod]
    public async Task Meter_Should_Respond_When_Pinging_The_Meter()
    {
        var binding = new UdpBinding(new IPEndPoint(IPAddress.Parse(CollectorIpAddress), CollectorPort), new MeterbusFrameSerializer());

        var master = new MBusMaster(binding);

        var result = await master.Ping(0x0a, Timeout);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task Meter_Scanner_Should_Find_Meter_When_Connected()
    {
        var binding = new UdpBinding(new IPEndPoint(IPAddress.Parse(CollectorIpAddress), CollectorPort), new MeterbusFrameSerializer());

        var master = new MBusMaster(binding);
        master.Meter += (sender, e) => System.Diagnostics.Debug.WriteLine($"Found meter on address {e.Address:x2}.");

        await master.Scan([0x09, 0x0a, 0x0b], Timeout);
    }

    [TestMethod]
    public async Task Meter_Should_Have_Address_Changed_When_Reconfigured()
    {
        var binding = new UdpBinding(new IPEndPoint(IPAddress.Parse(CollectorIpAddress), CollectorPort), new MeterbusFrameSerializer());

        var master = new MBusMaster(binding);

        await master.SetMeterAddress(0x0a, 0x09);
    }

    [TestMethod]
    public async Task Meter_Telemetry_Should_Be_Retrieved_When_Querying()
    {
        var binding = new UdpBinding(new IPEndPoint(IPAddress.Parse(CollectorIpAddress), CollectorPort), new MeterbusFrameSerializer());

        var master = new MBusMaster(binding);

        var response = await master.RequestData(0x0a, Timeout);

        response = await master.RequestData(0x0a, Timeout);

        Assert.IsNotNull(response);
    }

    [TestMethod]
    public void Collector_Should_Respond_When_Querying_Broadcast_Domain()
    {
        var binding = new UdpBroadcastBinding(30718, new QuerySerializer());

        binding.Send(new QueryPacket { Data = "00 00 00 F6".HexToBytes() });
    }
}
