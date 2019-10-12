using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class ControlFrame : Frame
    {
        public byte Start => Constants.MBUS_FRAME_CONTROL_START;

        public ControlMask Control { get; }

        public ControlInformation ControlInformation { get; }

        public byte Address { get; }

        public byte Length => 0x03;

        public byte Crc { get; }

        public byte Stop => Constants.MBUS_FRAME_STOP;

        public ControlFrame(byte control, byte controlInformation, byte address)
        {
            Control = (ControlMask)control;
            ControlInformation = (ControlInformation)controlInformation;
            Address = address;
            Crc = new byte[] { control, address, controlInformation }.CheckSum();
        }
    }
}
