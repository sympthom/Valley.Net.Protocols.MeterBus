using System;
using System.IO;
using Valley.Net.Protocols.MeterBus.EN13757_2;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    /// <summary>
    /// Static helper for parsing raw MeterBus value bytes into CLR types.
    /// </summary>
    public static class ValueParser
    {
        /// <summary>
        /// Parse the raw data bytes for a variable-data record into the
        /// appropriate CLR value according to the DIF data-type.
        /// </summary>
        public static object? ParseValue(DataTypes dataType, byte[] data)
        {
            switch (dataType)
            {
                case DataTypes._No_data:
                    return null;
                case DataTypes._8_Bit_Integer:
                    if (data.Length < 1) return null;
                    return data[0];
                case DataTypes._16_Bit_Integer:
                    if (data.Length < 2) return null;
                    return BitConverter.ToInt16(data, 0);
                case DataTypes._24_Bit_Integer:
                    {
                        if (data.Length < 3) return null;
                        var padded = new byte[4];
                        Array.Copy(data, padded, 3);
                        return BitConverter.ToInt32(padded, 0);
                    }
                case DataTypes._32_Bit_Integer:
                    if (data.Length < 4) return null;
                    return BitConverter.ToInt32(data, 0);
                case DataTypes._32_Bit_Real:
                    if (data.Length < 4) return null;
                    return BitConverter.ToSingle(data, 0);
                case DataTypes._48_Bit_Integer:
                    {
                        if (data.Length < 6) return null;
                        var padded = new byte[8];
                        Array.Copy(data, padded, 6);
                        return BitConverter.ToInt64(padded, 0);
                    }
                case DataTypes._64_Bit_Integer:
                    if (data.Length < 8) return null;
                    return BitConverter.ToInt64(data, 0);
                case DataTypes._Selection_for_Readout:
                    return null;
                case DataTypes._2_digit_BCD:
                    if (data.Length < 1) return null;
                    return TryParseBcd(data.BCDDecode(1), out var bcd2) ? bcd2 : (object?)data.BCDDecode(1);
                case DataTypes._4_digit_BCD:
                    if (data.Length < 2) return null;
                    return TryParseBcd(data.BCDDecode(2), out var bcd4) ? bcd4 : (object?)data.BCDDecode(2);
                case DataTypes._6_digit_BCD:
                    if (data.Length < 3) return null;
                    return TryParseBcd(data.BCDDecode(3), out var bcd6) ? bcd6 : (object?)data.BCDDecode(3);
                case DataTypes._8_digit_BCD:
                    if (data.Length < 4) return null;
                    return TryParseBcd(data.BCDDecode(4), out var bcd8) ? bcd8 : (object?)data.BCDDecode(4);
                case DataTypes._variable_length:
                    if (data == null || data.Length == 0)
                        return null;
                    // Variable-length data in M-Bus is typically ASCII text (e.g., serial numbers, IDs)
                    return System.Text.Encoding.ASCII.GetString(data).TrimEnd('\0');
                case DataTypes._12_digit_BCD:
                    if (data.Length < 6) return null;
                    {
                        var bcdStr = data.BCDToString();
                        return long.TryParse(bcdStr, out var bcd12) ? bcd12 : (object?)bcdStr;
                    }
                default:
                    return null;
            }
        }

        private static bool TryParseBcd(string bcdString, out long result)
        {
            return long.TryParse(bcdString, out result);
        }

        /// <summary>
        /// Parse a MeterBus TimePoint value (CP16 / CP32 / CP48) into a
        /// <see cref="DateTime"/>.
        /// </summary>
        public static DateTime ParseDateTime(DataTypes dataType, byte[] valueData)
        {
            switch (dataType)
            {
                case DataTypes._16_Bit_Integer: // Type G: Compound CP16: Date
                    {
                        var temp = valueData;
                        var day = temp[0] & 0x1f;
                        var month = (temp[1] & 0x0f);
                        var year = 100 + (((temp[0] & 0xe0) >> 5) | ((temp[1] & 0xf0) >> 1));

                        if (year < 70)
                            year += 2000;
                        else
                            year += 1900;

                        if (month == 0 || day == 0)
                            return DateTime.MinValue;
                        else
                            return new DateTime(year, month, day);
                    }
                case DataTypes._32_Bit_Integer: //data type G (date) 4 bytes (32 bit)
                    {
                        var temp = valueData;
                        var minute = temp[0] & 0x3f;
                        var hour = temp[1] & 0x1f;
                        var day = temp[2] & 0x1f;
                        var month = (temp[3] & 0x0f);
                        var year = 100 + (((temp[2] & 0xe0) >> 5) | ((temp[3] & 0xf0) >> 1));

                        if (year < 70)
                            year += 2000;
                        else
                            year += 1900;

                        if (month == 0 || day == 0)
                            return DateTime.MinValue;
                        else
                            return new DateTime(year, month, day, hour, minute, 0);
                    }
                case DataTypes._48_Bit_Integer: //data type F (time & date) 6 bytes (48 bit)
                    {
                        var temp = valueData;
                        var second = temp[0] & 0x3f;
                        var minute = temp[1] & 0x3f;
                        var hour = temp[2] & 0x1f;
                        var day = temp[3] & 0x1f;
                        var month = (temp[4] & 0x0f);
                        var year = 100 + (((temp[3] & 0xe0) >> 5) | ((temp[4] & 0xf0) >> 1));

                        if (year < 70)
                            year += 2000;
                        else
                            year += 1900;

                        var valid = (temp[1] & 0x80) == 0;
                        var summer = (temp[1] & 0x8000) == 0;

                        if (month == 0 || day == 0)
                            return DateTime.MinValue;
                        else
                            return new DateTime(year, month, day, hour, minute, second);
                    }
                default:
                    throw new InvalidDataException();
            }
        }
    }
}
