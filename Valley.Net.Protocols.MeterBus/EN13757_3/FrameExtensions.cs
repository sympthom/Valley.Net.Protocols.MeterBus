using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

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
                            throw new Exception("Wrong direction.");

                        switch (longFrame.ControlInformation)
                        {
                            case ControlInformation.RESP_VARIABLE:
                                {
                                    return new VariableDataPacket(longFrame.Address)
                                    {
                                        IdentificationNo = UInt32.Parse(longFrame.IdentificationNo.BCDToString()),
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
                            throw new Exception("Wrong direction.");

                        switch (longFrame.ControlInformation)
                        {
                            case ControlInformation.RESP_FIXED:
                                {
                                    return new FixedDataPacket(longFrame.Address)
                                    {
                                        IdentificationNo = UInt32.Parse(longFrame.IdentificationNo.BCDToString()),
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

                    record.Units = new List<VariableDataPacket.Record.UnitData>()
                        .Union(x.OfType<VIF>().Select(y => new VariableDataPacket.Record.UnitData
                        {
                            Units = y.Units,
                            Unit = y.Unit,
                            Magnitude = y.Magnitude,
                            Quantity = y.Quantity,
                            VIF_string = y.VIF_string,
                        }))
                        .Union(x.OfType<VIFE>().Select(y => new VariableDataPacket.Record.UnitData
                        {
                            Units = y.Units,
                            Unit = y.Unit,
                            Magnitude = y.Magnitude,
                            Quantity = y.Quantity,
                        }))
                        .Union(x.OfType<VIFE_FB>().Select(y => new VariableDataPacket.Record.UnitData
                        {
                            Units = y.Units,
                            Unit = y.Unit,
                            Magnitude = y.Magnitude,
                            Quantity = y.Quantity,
                        }))
                        .Union(x.OfType<VIFE_FD>().Select(y => new VariableDataPacket.Record.UnitData
                        {
                            Units = y.Units,
                            Unit = y.Unit,
                            Magnitude = y.Magnitude,
                            Quantity = y.Quantity,
                        }))
                        .ToArray();

                    var value = x.OfType<Value>().Single();
                     
                    switch (dif.DataType)
                    {
                        case DataTypes._No_data:
                            record.Value = null;
                            break;
                        case DataTypes._8_Bit_Integer:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 1);
                            record.Value = value.Data.Single();
                            break;
                        case DataTypes._16_Bit_Integer:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 2);
                            record.Value = BitConverter.ToInt16(value.Data, 0);
                            break;
                        case DataTypes._24_Bit_Integer:
                            {
                                System.Diagnostics.Debug.Assert(value.Data.Length == 3);

                                var data = new byte[4];
                                data[0] = value.Data[0];
                                data[1] = value.Data[1];
                                data[2] = value.Data[2];

                                record.Value = BitConverter.ToInt32(data, 0);
                            }
                            break;
                        case DataTypes._32_Bit_Integer:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 4);
                            record.Value = BitConverter.ToInt32(value.Data, 0);
                            break;
                        case DataTypes._32_Bit_Real:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 4);
                            record.Value = BitConverter.ToSingle(value.Data, 0);
                            break;
                        case DataTypes._48_Bit_Integer:
                            {
                                System.Diagnostics.Debug.Assert(value.Data.Length == 6);

                                var data = new byte[8];
                                data[0] = value.Data[0];
                                data[1] = value.Data[1];
                                data[2] = value.Data[2];
                                data[3] = value.Data[3];
                                data[4] = value.Data[4];
                                data[5] = value.Data[5];

                                record.Value = BitConverter.ToInt32(data, 0);
                            }
                            break;
                        case DataTypes._64_Bit_Integer:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 8);
                            record.Value = BitConverter.ToInt64(value.Data, 0);
                            break;
                        case DataTypes._Selection_for_Readout:
                            throw new NotImplementedException();
                        case DataTypes._2_digit_BCD:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 1);
                            record.Value = sbyte.Parse(value.Data.BCDDecode(1));
                            break;
                        case DataTypes._4_digit_BCD:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 2);
                            record.Value = Int16.Parse(value.Data.BCDDecode(2));
                            break;
                        case DataTypes._6_digit_BCD:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 3);
                            record.Value = Int32.Parse(value.Data.BCDDecode(3));
                            break;
                        case DataTypes._8_digit_BCD:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 4);
                            record.Value = Int32.Parse(value.Data.BCDDecode(4));
                            break;
                        case DataTypes._variable_length:
                            System.Diagnostics.Debug.Assert(value.Data == null);
                            //record.Value = reader.ReadValue();
                            break;
                        case DataTypes._12_digit_BCD:
                            System.Diagnostics.Debug.Assert(value.Data.Length == 6);
                            record.Value = Int64.Parse(value.Data.BCDToString());
                            break;
                    }

                    return record;
                });

            return result;
        }

        //public static Packet AsMeterBusPacket2(this INetworkPacket frame)
        //{
        //    if (frame == null)
        //        throw new ArgumentNullException(nameof(frame));

        //    switch (frame)
        //    {
        //        case LongFrame longFrame:
        //            {
        //                if ((longFrame.Control & ControlMask.DIR) != ControlMask.DIR_S2M)
        //                    throw new Exception("Wrong direction.");

        //                using (var stream = new MemoryStream(longFrame.Data))
        //                using (var reader = new BinaryReader(stream))
        //                {
        //                    if (stream.Length < 1)
        //                        return new EmptyPacket();

        //                    switch (longFrame.ControlInformation)
        //                    {
        //                        case ControlInformation.ERROR_GENERAL: // report of general application errors
        //                            {
        //                                return new ApplicationErrorPacket(longFrame.Address, reader.ReadByte());
        //                            }
        //                        case ControlInformation.STATUS_ALARM: // report of alarm status
        //                            {
        //                                return new AlarmStatusPacket(longFrame.Address)
        //                                {
        //                                    Status = reader.ReadByte(),
        //                                };
        //                            }
        //                        case ControlInformation.RESP_VARIABLE:
        //                            {
        //                                var buffer = new byte[4];

        //                                if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
        //                                    throw new InvalidDataException();

        //                                return new VariableDataPacket(longFrame.Address)
        //                                {
        //                                    IdentificationNo = UInt32.Parse(buffer.BCDToString()), // four bytes
        //                                    Manufr = reader.ReadUInt16(), // two bytes
        //                                    Version = reader.ReadByte(),
        //                                    DeviceType = (DeviceType)reader.ReadByte(),
        //                                    TransmissionCounter = reader.ReadByte(),
        //                                    Status = reader.ReadByte(),
        //                                    Signature = reader.ReadUInt16(), // two bytes
        //                                    Records = GetRecords(reader),
        //                                };
        //                            }
        //                        case ControlInformation.RESP_VARIABLE_MSB: // variable data respond
        //                            {
        //                                throw new NotImplementedException();
        //                            }
        //                        case ControlInformation.RESP_FIXED:
        //                            {
        //                                var packet = new FixedDataPacket(longFrame.Address);

        //                                var buffer = new byte[4];

        //                                if (reader.Read(buffer, 0, buffer.Length) != buffer.Length)
        //                                    throw new InvalidDataException();

        //                                packet.IdentificationNo = UInt32.Parse(buffer.BCDToString());

        //                                packet.TransmissionCounter = reader.ReadByte();

        //                                var status = reader.ReadByte();
        //                                var countersBCD = (status & 0x01) == 0;

        //                                packet.CountersFixed = (status & 0x02) != 0;

        //                                var buf6 = reader.ReadByte();
        //                                var buf7 = reader.ReadByte();

        //                                packet.Units1 = (FixedDataUnits)(buf6 & 0x3F);
        //                                packet.Units2 = (FixedDataUnits)(buf7 & 0x3F);
        //                                packet.DeviceType = (DeviceType)(byte)(((buf6 & 0xc0) >> 6) | ((buf7 & 0xc0) >> 4));

        //                                if (countersBCD)
        //                                {
        //                                    var buf8 = new byte[4];

        //                                    if (reader.Read(buf8, 0, buf8.Length) != buf8.Length)
        //                                        throw new InvalidDataException();

        //                                    packet.Counter1 = UInt32.Parse(buf8.BCDToString());

        //                                    var buf12 = new byte[4];

        //                                    if (reader.Read(buf12, 0, buf12.Length) != buf12.Length)
        //                                        throw new InvalidDataException();

        //                                    packet.Counter2 = UInt32.Parse(buf12.BCDToString());
        //                                }
        //                                else
        //                                {
        //                                    packet.Counter1 = reader.ReadUInt32();
        //                                    packet.Counter2 = reader.ReadUInt32();
        //                                }

        //                                return packet;
        //                            }
        //                        case ControlInformation.RESP_FIXED_MSB:
        //                            {
        //                                throw new NotImplementedException();
        //                            }
        //                        default:
        //                            {
        //                                throw new InvalidDataException();
        //                            }
        //                    }
        //                }
        //            }
        //        default:
        //            {
        //                throw new InvalidDataException();
        //            }
        //    }
        //}

        //private static List<VariableDataPacket.Record> GetRecords(BinaryReader reader)
        //{
        //    var result = new List<VariableDataPacket.Record>();

        //    while (!reader.EOF())
        //    {
        //        var type = (VariableDataRecordType)reader.ReadByte();

        //        switch (type)
        //        {
        //            case VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW:
        //            case VariableDataRecordType.MBUS_DIB_DIF_IDLE_FILLER: continue;
        //            case VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC:
        //                {
        //                    var record = new VariableDataPacket.Record
        //                    {
        //                        RecordType = type,
        //                        ValueData = reader.ReadAllBytes(),
        //                        ValueDataType = DataTypes._Unknown
        //                    };

        //                    result.Add(record);
        //                }
        //                break;
        //            default:
        //                {
        //                    var record = GetVariableDataRecord((byte)type, reader);

        //                    result.Add(record);
        //                }
        //                break;
        //        }
        //    }

        //    return result;
        //}

        //private static VariableDataPacket.Record GetVariableDataRecord(byte b, BinaryReader reader)
        //{
        //    var dif = new DIF(b);

        //    var record = new VariableDataPacket.Record
        //    {
        //        RecordType = dif.Data,
        //        ValueDataType = dif.DataType,
        //        Function = dif.Function,
        //        StorageNumber = dif.StorageLSB ? 1UL : 0UL,
        //        SubUnit = 0,
        //        Tariff = 0,
        //    };

        //    var extension = dif.Extension;

        //    for (int i = 0; extension; i++)
        //    {
        //        if (i > 10)
        //            throw new InvalidDataException();

        //        var dife = new DIFE(reader.ReadByte());
        //        var snPart = (UInt64)dife.StorageNumber;
        //        snPart <<= (i * 4 + 1);
        //        record.StorageNumber |= snPart;
        //        var tPart = (UInt32)dife.Tariff;
        //        tPart <<= (i * 2);
        //        record.Tariff |= tPart;
        //        var suPart = (UInt16)dife.Device;
        //        suPart <<= i;
        //        record.SubUnit |= suPart;
        //        extension = dife.Extension;
        //    }

        //    record.Units = GetUnits(reader);

        //    var length = dif.DataType == DataTypes._variable_length ? -1 : LenghtsInBitsTable[dif.DataType] / 8;

        //    // Parse value data
        //    record.ValueData = length > 0 ? reader.ReadBytes(length) : null;

        //    switch (record.ValueDataType)
        //    {
        //        case DataTypes._No_data:
        //            record.Value = null;
        //            break;
        //        case DataTypes._8_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 1);
        //            record.Value = record.ValueData.Single();
        //            break;
        //        case DataTypes._16_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 2);
        //            record.Value = BitConverter.ToInt16(record.ValueData, 0);
        //            break;
        //        case DataTypes._24_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 3);
        //            record.Value = BitConverter.ToInt32(new byte[] { 0 }.Concat(record.ValueData).ToArray(), 0);
        //            break;
        //        case DataTypes._32_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 4);
        //            record.Value = BitConverter.ToInt32(record.ValueData, 0);
        //            break;
        //        case DataTypes._32_Bit_Real:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 4);
        //            record.Value = BitConverter.ToSingle(record.ValueData, 0);
        //            break;
        //        case DataTypes._48_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 6);
        //            record.Value = BitConverter.ToInt64(new byte[] { 0, 0 }.Concat(record.ValueData).ToArray(), 0);
        //            break;
        //        case DataTypes._64_Bit_Integer:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 8);
        //            record.Value = BitConverter.ToInt64(record.ValueData, 0);
        //            break;
        //        case DataTypes._Selection_for_Readout:
        //            throw new NotImplementedException();
        //        case DataTypes._2_digit_BCD:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 1);
        //            record.Value = sbyte.Parse(record.ValueData.BCDDecode(1));
        //            break;
        //        case DataTypes._4_digit_BCD:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 2);
        //            record.Value = Int16.Parse(record.ValueData.BCDDecode(2));
        //            break;
        //        case DataTypes._6_digit_BCD:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 3);
        //            record.Value = Int32.Parse(record.ValueData.BCDDecode(3));
        //            break;
        //        case DataTypes._8_digit_BCD:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 4);
        //            record.Value = Int32.Parse(record.ValueData.BCDDecode(4));
        //            break;
        //        case DataTypes._variable_length:
        //            System.Diagnostics.Debug.Assert(record.ValueData == null);
        //            record.Value = reader.ReadValue();
        //            break;
        //        case DataTypes._12_digit_BCD:
        //            System.Diagnostics.Debug.Assert(record.ValueData.Length == 6);
        //            record.Value = Int64.Parse(record.ValueData.BCDToString());
        //            break;
        //    }

        //    return record;
        //}

        //private static VariableDataPacket.Record.UnitData[] GetUnits(BinaryReader reader)
        //{
        //    var units = new List<VariableDataPacket.Record.UnitData>();

        //    var vif = new VIF(reader.ReadByte());

        //    var extension = vif.Extension;

        //    switch (vif.Type)
        //    {
        //        case VIF.VifType.PrimaryVIF:
        //        case VIF.VifType.PlainTextVIF:
        //        case VIF.VifType.AnyVIF:
        //        case VIF.VifType.ManufacturerSpecific:
        //            {
        //                units.Add(new VariableDataPacket.Record.UnitData
        //                {
        //                    Units = vif.Units,
        //                    Unit = vif.Unit,
        //                    Magnitude = vif.Magnitude,
        //                    Quantity = vif.Quantity,
        //                    VIF_string = vif.VIF_string,
        //                });

        //                for (int i = 0; extension; i++)
        //                {
        //                    if (i > 10)
        //                        throw new InvalidDataException();

        //                    var vife = new VIFE(reader.ReadByte());
        //                    units.Add(new VariableDataPacket.Record.UnitData
        //                    {
        //                        Units = vife.Units,
        //                        Unit = vife.Unit,
        //                        Magnitude = vife.Magnitude,
        //                        Quantity = vife.Quantity,
        //                    });

        //                    extension = vife.Extension;
        //                }
        //            }
        //            break;
        //        case VIF.VifType.LinearVIFExtensionFD:
        //            {
        //                for (int i = 0; extension; i++)
        //                {
        //                    if (i > 10)
        //                        throw new InvalidDataException();

        //                    var vife = new VIFE_FD(reader.ReadByte());
        //                    units.Add(new VariableDataPacket.Record.UnitData
        //                    {
        //                        Units = vife.Units,
        //                        Unit = vife.Unit,
        //                        Magnitude = vife.Magnitude,
        //                        Quantity = vife.Quantity,
        //                    });

        //                    extension = vife.Extension;
        //                }
        //            }
        //            break;
        //        case VIF.VifType.LinearVIFExtensionFB:
        //            {
        //                for (int i = 0; extension; i++)
        //                {
        //                    if (i > 10)
        //                        throw new InvalidDataException();

        //                    var vife = new VIFE_FB(reader.ReadByte());
        //                    units.Add(new VariableDataPacket.Record.UnitData
        //                    {
        //                        Units = vife.Units,
        //                        Unit = vife.Unit,
        //                        Magnitude = vife.Magnitude,
        //                        Quantity = vife.Quantity,
        //                    });

        //                    extension = vife.Extension;
        //                }
        //            }
        //            break;
        //    }

        //    return units.ToArray();
        //}

        private static readonly Dictionary<DataTypes, int> LenghtsInBitsTable = new Dictionary<DataTypes, int>()
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

        //   Value         Field Medium/Unit              Medium
        // hexadecimal Bit 16  Bit 15    Bit 8  Bit 7
        //     0        0       0         0     0         Other
        //     1        0       0         0     1         Oil
        //     2        0       0         1     0         Electricity
        //     3        0       0         1     1         Gas
        //     4        0       1         0     0         Heat
        //     5        0       1         0     1         Steam
        //     6        0       1         1     0         Hot Water
        //     7        0       1         1     1         Water
        //     8        1       0         0     0         H.C.A.
        //     9        1       0         0     1         Reserved
        //     A        1       0         1     0         Gas Mode 2
        //     B        1       0         1     1         Heat Mode 2
        //     C        1       1         0     0         Hot Water Mode 2
        //     D        1       1         0     1         Water Mode 2
        //     E        1       1         1     0         H.C.A. Mode 2
        //     F        1       1         1     1         Reserved
        //

        /// <summary>
        /// For fixed-length frames, get a string describing the medium.
        /// </summary>
        /// <param name="medium"></param>
        /// <returns></returns>
        public static string mbus_data_fixed_medium(byte medium)
        {
            //        switch ((data->cnt1_type&0xC0)>>6 | (data->cnt2_type&0xC0)>>4 )
            //        {
            //            case 0x00:
            //                snprintf(buff, sizeof(buff), "Other");
            //                break;
            //            case 0x01:
            //                snprintf(buff, sizeof(buff), "Oil");
            //                break;
            //            case 0x02:
            //                snprintf(buff, sizeof(buff), "Electricity");
            //                break;
            //            case 0x03:
            //                snprintf(buff, sizeof(buff), "Gas");
            //                break;
            //            case 0x04:
            //                snprintf(buff, sizeof(buff), "Heat");
            //                break;
            //            case 0x05:
            //                snprintf(buff, sizeof(buff), "Steam");
            //                break;
            //            case 0x06:
            //                snprintf(buff, sizeof(buff), "Hot Water");
            //                break;
            //            case 0x07:
            //                snprintf(buff, sizeof(buff), "Water");
            //                break;
            //            case 0x08:
            //                snprintf(buff, sizeof(buff), "H.C.A.");
            //                break;
            //            case 0x09:
            //                snprintf(buff, sizeof(buff), "Reserved");
            //                break;
            //            case 0x0A:
            //                snprintf(buff, sizeof(buff), "Gas Mode 2");
            //                break;
            //            case 0x0B:
            //                snprintf(buff, sizeof(buff), "Heat Mode 2");
            //                break;
            //            case 0x0C:
            //                snprintf(buff, sizeof(buff), "Hot Water Mode 2");
            //                break;
            //            case 0x0D:
            //                snprintf(buff, sizeof(buff), "Water Mode 2");
            //                break;
            //            case 0x0E:
            //                snprintf(buff, sizeof(buff), "H.C.A. Mode 2");
            //                break;
            //            case 0x0F:
            //                snprintf(buff, sizeof(buff), "Reserved");
            //                break;
            //            default:
            //                snprintf(buff, sizeof(buff), "unknown");
            //                break;
            //        }

            //        return buff;

            return "";
        }
    }
}
