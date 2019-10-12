using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    public sealed class AlarmStatusPacket : Packet
    {
        public byte Status { get; set; }

        public AlarmStatusPacket(byte address)
        {
            Address = address;
        }

        public override string ToString()
        {
            return string.Format("{0}({1}):{2:x2}", this.GetType().Name, base.ToString(), Status);
        }
    }
}
