using System.Text;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Byte array extension methods for M-Bus protocol operations.
/// </summary>
public static class ByteExtensions
{
    public static byte CheckSum(this ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }

    public static byte CheckSum(this byte[] data) =>
        CheckSum(data.AsSpan());

    public static string ToHex(this byte[] data) =>
        BitConverter.ToString(data).Replace("-", " ");

    public static string ToHex(this ReadOnlyMemory<byte> data) =>
        BitConverter.ToString(data.ToArray()).Replace("-", " ");

    // BCD is little-endian with two digits per byte, so the most significant byte is last.
    public static string BCDToString(this ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder(data.Length * 2);
        for (int i = data.Length - 1; i >= 0; i--)
        {
            sb.Append(((data[i] >> 4) & 0x0F).ToString("X"));
            sb.Append((data[i] & 0x0F).ToString("X"));
        }
        return sb.ToString();
    }

    public static string BCDToString(this byte[] data) =>
        BCDToString(data.AsSpan());

    /// <summary>
    /// Decodes little-endian BCD (EN 13757-3 Annex A type A). A high nibble of 0xF in the most significant
    /// byte is a minus sign. Returns false when any other nibble is above 9, which meters use to flag errors,
    /// or when the digits do not fit in a <see cref="long"/>.
    /// </summary>
    public static bool TryDecodeBcd(this ReadOnlySpan<byte> data, out long value) =>
        TryDecodeBcd(data, allowSign: true, out value);

    internal static bool TryDecodeBcd(ReadOnlySpan<byte> data, bool allowSign, out long value)
    {
        value = 0;
        var negative = false;

        for (int i = data.Length - 1; i >= 0; i--)
        {
            var high = data[i] >> 4;
            var low = data[i] & 0x0F;

            if (allowSign && i == data.Length - 1 && high == 0x0F)
            {
                negative = true;
                high = 0;
            }

            if (high > 9 || low > 9 || value > (long.MaxValue - high * 10 - low) / 100)
            {
                value = 0;
                return false;
            }

            value = value * 100 + high * 10 + low;
        }

        if (negative)
            value = -value;
        return true;
    }
}
