using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public static class ByteExtensions
    {
        public static Frame ToFrame(this byte[] data)
        {
            return new MeterbusFrameSerializer()
               .Deserialize<VariableDataLongFrame>(data, 0, data.Length);
        }
    }
}
