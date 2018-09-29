using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;

namespace Valley.Net.Protocols.MeterBus.Test
{
    internal sealed class QueryPacket : INetworkPacket
    {
        public byte[] Data { get; set; }
    }
}
