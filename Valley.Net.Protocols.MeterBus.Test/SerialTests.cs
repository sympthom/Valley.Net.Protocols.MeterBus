using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Bindings.Serial;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus.Test
{
    [TestClass]
    public sealed class SerialTests
    {
        private const int TIMEOUT_IN_SECONDS = 3;

        [TestMethod]
        public async Task Meter_Should_Respond_With_Ack_When_Sending_SND_NKE()
        {
            var resetEvent = new AutoResetEvent(false);

            var port = new SerialPort();
            port.BaudRate = 1200;

            var endpoint = new SerialBinding(port, (x, y) =>
            {
                return null;
            }, new MeterbusFrameSerializer());

            endpoint.PacketReceived += (sender, e) => resetEvent.Set();

            await endpoint.ConnectAsync();

            await endpoint.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));

            Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS)));

            await endpoint.DisconnectAsync();
        }
    }
}
