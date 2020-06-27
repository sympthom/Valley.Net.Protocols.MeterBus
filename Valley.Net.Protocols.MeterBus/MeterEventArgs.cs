using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    public sealed class MeterEventArgs : EventArgs
    {
        public byte Address { get; }

        public MeterEventArgs(byte address)
        {
            Address = address;
        }

        public MeterEventArgs()
        {

        }

    }
}
