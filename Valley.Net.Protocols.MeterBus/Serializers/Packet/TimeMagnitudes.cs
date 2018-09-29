using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.Serializers.Packet
{
    public enum TimeMagnitudes : byte
    {
        Seconds = 0,
        Minutes = 1,
        Hours = 2,
        Days = 3
    }
}
