using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using Valley.Net.Protocols.MeterBus.EN13757_3;

namespace Valley.Net.Protocols.MeterBus.Test
{
    [TestClass]
    public sealed class XmlSerializationTests
    {
        [TestMethod]
        public void Test()
        {
            var data = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data);

            Assert.IsNotNull(frame);

            var xml = frame.AsXml();

            Assert.IsNotNull(xml);
        }
    }

    public static class XmlSerialiser
    {
        public static string AsXml(this Frame frame)
        {
            return null;
        }
    }
}
