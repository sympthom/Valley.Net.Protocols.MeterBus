using System.IO.Ports;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Bindings.Serial;
using Valley.Net.Protocols.MeterBus.EN13757_2;

namespace Valley.Net.Protocols.MeterBus.Test;

/// <summary>
/// Integration tests requiring a real M-Bus serial connection.
/// These tests are excluded from CI runs.
/// </summary>
[TestClass]
[TestCategory("Integration")]
public sealed class SerialTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    [TestMethod]
    public async Task Meter_Should_Respond_With_Ack_When_Sending_SND_NKE()
    {
        var resetEvent = new AutoResetEvent(false);

        var port = new SerialPort
        {
            BaudRate = 1200
        };

        var endpoint = new SerialBinding(port, (x, y) => null!, new MeterbusFrameSerializer());

        endpoint.PacketReceived += (sender, e) => resetEvent.Set();

        await endpoint.ConnectAsync();

        await endpoint.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));

        Assert.IsTrue(resetEvent.WaitOne(Timeout));

        await endpoint.DisconnectAsync();
    }
}
