using System.Text;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Parses raw M-Bus data record values into CLR types.
/// </summary>
public static class ValueParser
{
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
                if (data.Length < 3) return null;
                return data[0] | (data[1] << 8) | (data[2] << 16);

            case DataTypes._32_Bit_Integer:
                if (data.Length < 4) return null;
                return BitConverter.ToInt32(data, 0);

            case DataTypes._32_Bit_Real:
                if (data.Length < 4) return null;
                return BitConverter.ToSingle(data, 0);

            case DataTypes._48_Bit_Integer:
                if (data.Length < 6) return null;
                {
                    long val = 0;
                    for (int i = 5; i >= 0; i--)
                        val = (val << 8) | data[i];
                    return val;
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
                if (data is null || data.Length == 0) return null;
                return Encoding.ASCII.GetString(data).TrimEnd('\0');

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

    public static object? ParseDateTime(DataTypes dataType, byte[]? data)
    {
        if (data is null || data.Length == 0)
            return null;

        try
        {
            if (dataType == DataTypes._16_Bit_Integer && data.Length >= 2)
            {
                // Type G: Date only (2 bytes)
                var day = data[0] & 0x1F;
                var month = data[1] & 0x0F;
                var year = ((data[0] & 0xE0) >> 5) | ((data[1] & 0xF0) >> 1);
                if (year < 100) year += 2000;
                if (day > 0 && month > 0 && month <= 12 && day <= 31)
                    return new DateTime(year, month, day);
            }
            else if (dataType == DataTypes._32_Bit_Integer && data.Length >= 4)
            {
                // Type F: Date and time (4 bytes)
                var min = data[0] & 0x3F;
                var hour = data[1] & 0x1F;
                var day = data[2] & 0x1F;
                var month = data[3] & 0x0F;
                var year = ((data[2] & 0xE0) >> 5) | ((data[3] & 0xF0) >> 1);
                if (year < 100) year += 2000;
                if (day > 0 && month > 0 && month <= 12 && day <= 31 && hour < 24 && min < 60)
                    return new DateTime(year, month, day, hour, min, 0);
            }
        }
        catch
        {
            // Invalid date, return null
        }

        return null;
    }

    private static bool TryParseBcd(string bcdString, out long result)
    {
        return long.TryParse(bcdString, out result);
    }
}
