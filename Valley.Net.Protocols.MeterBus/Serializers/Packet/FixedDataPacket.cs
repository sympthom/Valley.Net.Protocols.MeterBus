using Valley.Net.Protocols.MeterBus.Frames;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.Serializers.Packet
{
    public sealed class FixedDataPacket : DataPacket
    {
        public bool CountersFixed { get; set; }

        public FixedDataUnits Units1 { get; set; }

        public FixedDataUnits Units2 { get; set; }

        public UInt32 Counter1 { get; set; }

        public UInt32 Counter2 { get; set; }

        public FixedDataPacket(byte address) : base(address)
        {
           
        }       
    }
}
