using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class ShortFrame : Frame
    {
        public byte Start => Constants.MBUS_FRAME_SHORT_START;

        public ControlMask Control { get; }

        public byte Address { get; }

        public byte Crc { get; }

        public byte Stop => Constants.MBUS_FRAME_STOP;

        public ShortFrame(byte control, byte address)
        {
            Control = (ControlMask)control;
            Address = address;
            Crc = new byte[] { control, address }.CheckSum();
        }
    }
}
