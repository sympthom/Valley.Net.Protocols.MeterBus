using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    /// <summary>
    /// Serializer implementing EN1434-3.
    /// </summary>
    public static class FrameExtensions
    {
        /// <summary>
        /// Convert frame (physical and link layer) to packet (application layer).
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public static Packet ToPacket(this Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            switch (frame)
            {
                case VariableDataLongFrame longFrame:
                    {
                        if ((longFrame.Control & ControlMask.DIR) != ControlMask.DIR_S2M)
                            throw new InvalidDataException("Wrong direction.");

                        switch (longFrame.ControlInformation)
                        {
                            case ControlInformation.RESP_VARIABLE:
                                {
                                    return new VariableDataPacket(longFrame.Address)
                                    {
                                        IdentificationNo = ParseIdentificationNo(longFrame.IdentificationNo),
                                        Manufr = BitConverter.ToUInt16(longFrame.Manufr, 0),
                                        Version = longFrame.Version,
                                        DeviceType = (DeviceType)longFrame.DeviceType,
                                        TransmissionCounter = longFrame.TransmissionCounter,
                                        Status = longFrame.Status,
                                        Signature = BitConverter.ToUInt16(longFrame.Signature, 0),
                                        Records = GetRecords(longFrame.Parts).ToList(),
                                    };
                                }
                            default: throw new InvalidDataException();
                        }
                    }
                case FixedDataLongFrame longFrame:
                    {
                        if ((longFrame.Control & ControlMask.DIR) != ControlMask.DIR_S2M)
                            throw new InvalidDataException("Wrong direction.");

                        switch (longFrame.ControlInformation)
                        {
                            case ControlInformation.RESP_FIXED:
                                {
                                    return new FixedDataPacket(longFrame.Address)
                                    {
                                        IdentificationNo = ParseIdentificationNo(longFrame.IdentificationNo),
                                        DeviceType = (DeviceType)longFrame.DeviceType,
                                        TransmissionCounter = longFrame.TransmissionCounter,
                                        CountersFixed = longFrame.CountersFixed,
                                        Units1 = longFrame.Units1,
                                        Units2 = longFrame.Units2,
                                        Counter1 = longFrame.Counter1,
                                        Counter2 = longFrame.Counter2,
                                    };
                                }
                            default: throw new InvalidDataException();
                        }
                    }
                default: throw new InvalidDataException();
            }
        }

        private static IEnumerable<VariableDataPacket.Record> GetRecords(IEnumerable<Part> parts)
        {
            var result = parts
                .GroupAdjacentBy((x) => x is DIF)
                .Where(x => x.Any(y =>
                {
                    switch (y)
                    {
                        case DIF dif:
                            {
                                switch (dif.Data)
                                {
                                    case VariableDataRecordType.MBUS_DIB_DIF_IDLE_FILLER:
                                    case VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW:
                                    case VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC: return false;
                                    default: return true;
                                }

                            }
                        default: return false;
                    }
                }))
                .Select(x =>
                {
                    var dif = x.Single(y => y is DIF) as DIF;

                    var record = new VariableDataPacket.Record
                    {
                        RecordType = dif.Data,
                        ValueDataType = dif.DataType,
                        Function = dif.Function,
                        StorageNumber = dif.StorageLSB ? 1UL : 0UL,
                        SubUnit = 0,
                        Tariff = 0,
                    };

                    var i = 0;

                    foreach (var dife in x.OfType<DIFE>())
                    {
                        var snPart = (UInt64)dife.StorageNumber;
                        snPart <<= (i * 4 + 1);
                        record.StorageNumber |= snPart;
                        var tPart = (UInt32)dife.Tariff;
                        tPart <<= (i * 2);
                        record.Tariff |= tPart;
                        var suPart = (UInt16)dife.Device;
                        suPart <<= i++;
                        record.SubUnit |= suPart;
                    }

                    record.Units = x.OfType<IValueInformationField>()
                        .Select(y => new VariableDataPacket.Record.UnitData
                        {
                            Units = y.Units,
                            Unit = y.Unit,
                            Magnitude = y.Magnitude,
                            Quantity = y.Quantity,
                            VIF_string = y.VIF_string,
                        })
                        .ToArray();

                    var value = x.OfType<Value>().Single();

                    record.Value = ValueParser.ParseValue(dif.DataType, value.Data);

                    return record;
                });

            return result;
        }

        private static Dictionary<DataTypes, int> LenghtsInBitsTable => Constants.LengthsInBitsTable;

        /// <summary>
        /// Returns the manufacturer ID according to the manufacturer's 3 byte ASCII code or zero when there was an error.
        /// </summary>
        /// <param name="manufacturer"></param>
        /// <returns>Returns manufacturer ID.</returns>
        public static int mbus_manufacturer_id(string manufacturer)
        {
            /*
             * manufacturer must consist of at least 3 alphabetic characters,
             * additional chars are silently ignored.
             */

            if (manufacturer == null || manufacturer.Length < 3)
                return 0;

            if (!char.IsLetter(manufacturer[0]) ||
                !char.IsLetter(manufacturer[1]) ||
                !char.IsLetter(manufacturer[2]))
                return 0;

            var id = (char.ToUpper(manufacturer[0]) - 64) * 32 * 32 +
                 (char.ToUpper(manufacturer[1]) - 64) * 32 +
                 (char.ToUpper(manufacturer[2]) - 64);

            /*
             * Valid input data should be in the range of 'AAA' to 'ZZZ' according to
             * the FLAG Association (http://www.dlms.com/flag/), thus resulting in
             * an ID from 0x0421 to 0x6b5a. If the conversion results in anything not
             * in this range, simply discard it and return 0 instead.
             */
            return 0x0421 <= id && id <= 0x6b5a ? id : 0;
        }

        /// <summary>
        /// Parse identification number bytes, handling both BCD and binary encodings.
        /// Some meters encode the ID as BCD (each nibble 0-9), others as binary.
        /// </summary>
        private static uint ParseIdentificationNo(byte[] identificationNo)
        {
            var bcdStr = identificationNo.BCDToString();
            if (uint.TryParse(bcdStr, out var result))
                return result;

            // Fallback: treat as little-endian binary value
            return BitConverter.ToUInt32(identificationNo, 0);
        }
    }
}
