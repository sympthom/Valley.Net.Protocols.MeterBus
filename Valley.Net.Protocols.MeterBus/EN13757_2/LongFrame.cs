using System;
using System.Collections.Generic;
using System.IO;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public class LongFrame : Frame
    {
        public byte Start => Constants.MBUS_FRAME_LONG_START;

        public ControlMask Control { get; }

        public byte Address { get; }

        public ControlInformation ControlInformation { get; }

        public byte[] IdentificationNo { get; } = new byte[4];

        public byte DeviceType { get; protected set; }

        public byte TransmissionCounter { get; protected set; }

        public byte Status { get; protected set; }

        public byte[] Data { get; }

        public byte Length { get; }

        public byte Crc { get; }

        public byte Stop => Constants.MBUS_FRAME_STOP;

        protected static Dictionary<DataTypes, int> LenghtsInBitsTable => Constants.LengthsInBitsTable;

        public LongFrame(byte control, byte controlInformation, byte address, byte[] data, byte length)
        {
            Control = (ControlMask)control;
            ControlInformation = (ControlInformation)controlInformation;
            Address = address;
            Data = data ?? new byte[0];
            Length = length;
            Crc = new byte[] { control, address, controlInformation }.Merge(data).CheckSum();
        }
    }
}
