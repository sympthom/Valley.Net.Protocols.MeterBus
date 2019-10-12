using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Bindings.Tcp;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus.Test
{
    [TestClass]
    public sealed class TcpTests
    {
        private const string COLLECTOR_IP_ADDRESS = "192.168.1.66";
        private const int COLLECTOR_PORT = 502;
        private const int TIMEOUT_IN_SECONDS = 3;

        [TestMethod]
        public async Task Meter_Should_Respond_With_Ack_When_Sending_SND_NKE()
        {
            var resetEvent = new AutoResetEvent(false);

            var endpoint = new TcpBinding(new MeterbusFrameSerializer());
            endpoint.PacketReceived += (sender, e) => resetEvent.Set();

            await endpoint.ConnectAsync(new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));

            await endpoint.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));

            Assert.IsTrue(resetEvent.WaitOne(TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS)));

            await endpoint.DisconnectAsync();
        }

        [TestMethod]
        public async Task Meter_Should_Respond_When_Pinging_The_Meter()
        {
            var binding = new TcpBinding(new MeterbusFrameSerializer());

            var master = new MBusMaster(binding, new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));

            var result = await master.Ping(0x0a, TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task Meter_Scanner_Should_Find_Meter_When_Meter_Is_Connected_To_Collector()
        {
            var binding = new TcpBinding(new MeterbusFrameSerializer());

            var master = new MBusMaster(binding, new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));
            master.Meter += (sender, e) => Debug.WriteLine($"Found meter on address {e.Address.ToString("x2")}.");

            await master.Scan(new byte[] { 0x0a }, TimeSpan.FromSeconds(100));
        }

        [TestMethod]
        public async Task Meter_Should_Have_The_Address_Changed_When_Meter_Address_Is_Reconfigured()
        {
            var binding = new TcpBinding(new MeterbusFrameSerializer());

            var master = new MBusMaster(binding, new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));

            await master.SetMeterAddress(0x0a, 0x09);
        }

        [TestMethod]
        public async Task Meter_Telemetry_Should_Be_Retrieved_When_Querying_The_Collector()
        {
            var binding = new TcpBinding(new MeterbusFrameSerializer());

            var master = new MBusMaster(binding, new IPEndPoint(IPAddress.Parse(COLLECTOR_IP_ADDRESS), COLLECTOR_PORT));

            var response = await master.RequestData(0x0a, TimeSpan.FromSeconds(TIMEOUT_IN_SECONDS)) as VariableDataPacket;

            Assert.IsNotNull(response);
        }
    }
}
