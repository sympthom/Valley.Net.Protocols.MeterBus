using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class Value : Part
    {
        public byte[] Data { get; private set; }

        public Value(byte[] data)
        {
            Data = data;
        }
    }
}
