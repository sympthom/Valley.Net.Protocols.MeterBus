using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class ManufactureSpecific : Part
    {
        public byte Type { get; private set; }

        public byte[] Data { get; private set; }

        public ManufactureSpecific(byte type, byte[] data)
        {
            Type = type;
            Data = data;
        }
    }
}
