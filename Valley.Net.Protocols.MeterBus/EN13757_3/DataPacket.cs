using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    public abstract class DataPacket : Packet
    {
        public UInt16 Manufr { get; set; }

        public UInt32 IdentificationNo { get; set; }

        public byte TransmissionCounter { get; set; }

        public DeviceType DeviceType { get; set; }

        public DataPacket(byte address)
        {
            Address = address;
        }
    }
}
