using System.Text;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Parses raw M-Bus data record values into CLR types.
/// </summary>
public static class ValueParser
{
    /// <summary>
    /// <see cref="DataRecord.ValueError"/> for a BCD value with a non-decimal digit.
    /// </summary>
    public const string InvalidBcd = "INVALID_BCD";

    /// <summary>
    /// <see cref="DataRecord.ValueError"/> for a date/time with the invalid bit set or a field out of range.
    /// </summary>
    public const string InvalidDate = "INVALID_DATE";

    public static object? ParseValue(DataTypes dataType, byte[] data) =>
        ParseValue(dataType, data, out _);

    /// <summary>
    /// Decodes a value by its DIF data type. BCD types decode to <see cref="long"/>; when a BCD digit is
    /// not decimal the result is null and <paramref name="error"/> is <see cref="InvalidBcd"/>.
    /// </summary>
    public static object? ParseValue(DataTypes dataType, byte[] data, out string? error)
    {
        error = null;

        switch (dataType)
        {
            case DataTypes._No_data:
                return null;

            case DataTypes._8_Bit_Integer:
                if (data.Length < 1) return null;
                // Type B integers are two's complement at every width, as in libmbus.
                return (sbyte)data[0];

            case DataTypes._16_Bit_Integer:
                if (data.Length < 2) return null;
                return BitConverter.ToInt16(data, 0);

            case DataTypes._24_Bit_Integer:
                if (data.Length < 3) return null;
                // Shift bit 23 into the sign bit and back to sign-extend.
                return (data[0] | (data[1] << 8) | (data[2] << 16)) << 8 >> 8;

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
                    // Sign-extend from bit 47.
                    return val << 16 >> 16;
                }

            case DataTypes._64_Bit_Integer:
                if (data.Length < 8) return null;
                return BitConverter.ToInt64(data, 0);

            case DataTypes._Selection_for_Readout:
                return null;

            case DataTypes._2_digit_BCD:
                return ParseBcd(data, 1, out error);

            case DataTypes._4_digit_BCD:
                return ParseBcd(data, 2, out error);

            case DataTypes._6_digit_BCD:
                return ParseBcd(data, 3, out error);

            case DataTypes._8_digit_BCD:
                return ParseBcd(data, 4, out error);

            case DataTypes._variable_length:
                // Without the LVAR byte the kind is unknown, so treat it as text (LVAR 00h-BFh).
                if (data is null || data.Length == 0) return null;
                return ParseText(data);

            case DataTypes._12_digit_BCD:
                return ParseBcd(data, 6, out error);

            default:
                return null;
        }
    }

    /// <summary>
    /// Byte length of a variable-length value (DIF data field 0x0D) from its LVAR byte, or null when the
    /// LVAR value is reserved (EN 13757-3).
    /// </summary>
    internal static int? VariableLength(byte lvar) => lvar switch
    {
        <= 0xBF => lvar,
        >= 0xC0 and <= 0xC9 => lvar - 0xC0,
        >= 0xD0 and <= 0xD9 => lvar - 0xD0,
        >= 0xE0 and <= 0xEF => lvar - 0xE0,
        >= 0xF0 and <= 0xF4 => 4 * (lvar - 0xEC),
        0xF5 => 48,
        0xF6 => 64,
        _ => null,
    };

    /// <summary>
    /// Decodes a variable-length value by its LVAR byte: text to <see cref="string"/>, BCD to <see cref="long"/>
    /// and binary numbers to the raw little-endian bytes.
    /// </summary>
    internal static object? ParseVariableLength(byte lvar, byte[] data, out string? error)
    {
        error = null;
        if (data.Length == 0) return null;

        switch (lvar)
        {
            case <= 0xBF:
                return ParseText(data);

            case >= 0xC0 and <= 0xC9:
            case >= 0xD0 and <= 0xD9:
                // The LVAR range carries the sign, so a 0xF sign nibble in the digits is invalid here.
                if (!ByteExtensions.TryDecodeBcd(data, allowSign: false, out var bcd))
                {
                    error = InvalidBcd;
                    return null;
                }
                return lvar >= 0xD0 ? -bcd : bcd;

            default:
                return data;
        }
    }

    /// <summary>
    /// Decodes a date/time-typed record (VIF 6Ch/6Dh and the date/time VIFEs) by its data type,
    /// per EN 13757-3 Annex A: 16-bit type G to <see cref="DateOnly"/>, 24-bit type J to <see cref="TimeOnly"/>,
    /// 32-bit type F and 48-bit type I to <see cref="DateTime"/>. Other data types decode as numbers.
    /// </summary>
    internal static object? ParseDateTime(DataTypes dataType, byte[] data, out string? error)
    {
        error = null;

        object? value;
        switch (dataType)
        {
            case DataTypes._16_Bit_Integer when data.Length >= 2:
                value = DecodeTypeG(data);
                break;

            case DataTypes._24_Bit_Integer when data.Length >= 3:
                value = DecodeTypeJ(data);
                break;

            case DataTypes._32_Bit_Integer when data.Length >= 4:
                value = DecodeTypeF(data);
                break;

            case DataTypes._48_Bit_Integer when data.Length >= 6:
                value = DecodeTypeI(data);
                break;

            default:
                return ParseValue(dataType, data, out error);
        }

        if (value is null)
            error = InvalidDate;
        return value;
    }

    // Type G: day in byte 0 bits 0-4, month in byte 1 bits 0-3, year split over byte 0 bits 5-7 (low) and byte 1 bits 4-7 (high).
    private static object? DecodeTypeG(byte[] data)
    {
        var day = data[0] & 0x1F;
        var month = data[1] & 0x0F;
        var year = FullYear(Year(data[0], data[1]), 0);

        return IsValidDate(year, month, day) ? new DateOnly(year!.Value, month, day) : null;
    }

    // Type J: seconds, minutes and hours in bytes 0-2.
    private static object? DecodeTypeJ(byte[] data)
    {
        var second = data[0] & 0x3F;
        var minute = data[1] & 0x3F;
        var hour = data[2] & 0x1F;

        return IsValidTime(hour, minute, second) ? new TimeOnly(hour, minute, second) : null;
    }

    // Type F: minute and IV (bit 7) in byte 0; hour, hundred-year (bits 5-6) and SU in byte 1; then the type G date.
    private static object? DecodeTypeF(byte[] data)
    {
        if ((data[0] & 0x80) != 0) return null;

        var minute = data[0] & 0x3F;
        var hour = data[1] & 0x1F;
        var day = data[2] & 0x1F;
        var month = data[3] & 0x0F;
        var year = FullYear(Year(data[2], data[3]), (data[1] >> 5) & 0x03);

        return IsValidDate(year, month, day) && IsValidTime(hour, minute, 0)
            ? new DateTime(year!.Value, month, day, hour, minute, 0)
            : null;
    }

    // Type I: second in byte 0, minute and IV (bit 7) in byte 1, hour in byte 2, then the type G date in bytes 3-4.
    private static object? DecodeTypeI(byte[] data)
    {
        if ((data[1] & 0x80) != 0) return null;

        var second = data[0] & 0x3F;
        var minute = data[1] & 0x3F;
        var hour = data[2] & 0x1F;
        var day = data[3] & 0x1F;
        var month = data[4] & 0x0F;
        var year = FullYear(Year(data[3], data[4]), 0);

        return IsValidDate(year, month, day) && IsValidTime(hour, minute, second)
            ? new DateTime(year!.Value, month, day, hour, minute, second)
            : null;
    }

    private static int Year(byte dayByte, byte monthByte) =>
        ((dayByte & 0xE0) >> 5) | ((monthByte & 0xF0) >> 1);

    // The 7-bit year is 0-99; 100-127 are wildcards or garbage. The type F hundred-year bits give the
    // century as 1900 + 100 * hy. Most meters leave them 0, so 0 falls back to the EN 13757-3 window
    // 0-80 -> 2000-2080 and 81-99 -> 1981-1999.
    private static int? FullYear(int year, int hundredYears)
    {
        if (year > 99) return null;
        if (hundredYears != 0) return 1900 + 100 * hundredYears + year;
        return year <= 80 ? 2000 + year : 1900 + year;
    }

    private static bool IsValidDate(int? year, int month, int day) =>
        year is not null && month is >= 1 and <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year.Value, month);

    private static bool IsValidTime(int hour, int minute, int second) =>
        hour <= 23 && minute <= 59 && second <= 59;

    // Text is sent last character first. EN 13757-3 specifies ISO 8859-1, a superset of ASCII. Fixed-width
    // fields are padded with NULs or spaces, which libmbus also drops (an all-space customer ID decodes to "").
    private static string ParseText(byte[] data)
    {
        var text = (byte[])data.Clone();
        Array.Reverse(text);
        return Encoding.Latin1.GetString(text).TrimEnd('\0', ' ');
    }

    private static long? ParseBcd(byte[] data, int length, out string? error)
    {
        error = null;
        if (data.Length < length) return null;
        if (data.AsSpan(0, length).TryDecodeBcd(out var result))
            return result;

        error = InvalidBcd;
        return null;
    }
}
