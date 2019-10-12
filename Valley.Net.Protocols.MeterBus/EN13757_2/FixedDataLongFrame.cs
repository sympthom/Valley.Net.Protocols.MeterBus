using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class FixedDataLongFrame : LongFrame
    {
        public bool CountersFixed { get; set; }

        public FixedDataUnits Units1 { get; set; }

        public FixedDataUnits Units2 { get; set; }

        public UInt32 Counter1 { get; set; }

        public UInt32 Counter2 { get; set; }

        public FixedDataLongFrame(byte control, byte controlInformation, byte address, byte[] data, byte length) : base(control, controlInformation, address, data, length)
        {
            if ((ControlInformation)controlInformation != ControlInformation.RESP_FIXED)
                throw new InvalidDataException();

            using (var stream = new MemoryStream(data))
            using (var reader = new BinaryReader(stream))
            {
                if (reader.Read(IdentificationNo, 0, IdentificationNo.Length) != IdentificationNo.Length)
                    throw new InvalidDataException();

                TransmissionCounter = reader.ReadByte();
                Status = reader.ReadByte();
                var countersBCD = (Status & 0x01) == 0;

                CountersFixed = (Status & 0x02) != 0;

                var buf6 = reader.ReadByte();
                var buf7 = reader.ReadByte();

                Units1 = (FixedDataUnits)(buf6 & 0x3F);
                Units2 = (FixedDataUnits)(buf7 & 0x3F);
                DeviceType = (byte)(((buf6 & 0xc0) >> 6) | ((buf7 & 0xc0) >> 4));

                if (countersBCD)
                {
                    var buf8 = new byte[4];

                    if (reader.Read(buf8, 0, buf8.Length) != buf8.Length)
                        throw new InvalidDataException();

                    Counter1 = UInt32.Parse(buf8.BCDToString());

                    var buf12 = new byte[4];

                    if (reader.Read(buf12, 0, buf12.Length) != buf12.Length)
                        throw new InvalidDataException();

                    Counter2 = UInt32.Parse(buf12.BCDToString());
                }
                else
                {
                    Counter1 = reader.ReadUInt32();
                    Counter2 = reader.ReadUInt32();
                }
            }
        }
    }
}
