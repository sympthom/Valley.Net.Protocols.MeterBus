using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class DIFE : Part
    {
        public int StorageNumber { get; private set; }

        public int Tariff { get; private set; }

        public int Device { get; private set; }

        public bool Extension { get; private set; }

        public byte Data { get; private set; }

        public DIFE(byte data)
        {
            Data = data;
            StorageNumber = data & 0x0f;
            Tariff = (data & 0x30) >> 4;
            Device = (data & 0x40) >> 6;
            Extension = (data & 0x80) != 0;
        }
    }
}
