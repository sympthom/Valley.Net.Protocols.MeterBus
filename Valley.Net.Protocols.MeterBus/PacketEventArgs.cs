using System;
using System.Collections.Generic;
using System.Text;
using Valley.Net.Bindings;

namespace Valley.Net.Protocols.MeterBus
{
    public sealed class PacketEventArgs : EventArgs
    {
        public INetworkPacket Packet { get; }

        public PacketEventArgs(INetworkPacket packet)
        {
            Packet = packet;
        }
    }
}
