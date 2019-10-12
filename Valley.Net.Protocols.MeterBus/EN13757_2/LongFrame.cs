using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

        protected static readonly Dictionary<DataTypes, int> LenghtsInBitsTable = new Dictionary<DataTypes, int>()
        {
            { DataTypes._No_data, 0 },
            { DataTypes._8_Bit_Integer, 8 },
            { DataTypes._16_Bit_Integer, 16 },
            { DataTypes._24_Bit_Integer, 24 },
            { DataTypes._32_Bit_Integer, 32 },
            { DataTypes._32_Bit_Real, 32 },
            { DataTypes._48_Bit_Integer, 48 },
            { DataTypes._64_Bit_Integer, 64 },
            { DataTypes._Selection_for_Readout, 0 },
            { DataTypes._2_digit_BCD, 8 },
            { DataTypes._4_digit_BCD, 16 },
            { DataTypes._6_digit_BCD, 24 },
            { DataTypes._8_digit_BCD, 32 },
            { DataTypes._variable_length, 0 /*N*/ },
            { DataTypes._12_digit_BCD, 48 },
            { DataTypes._Unknown, 64 },
        };

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
