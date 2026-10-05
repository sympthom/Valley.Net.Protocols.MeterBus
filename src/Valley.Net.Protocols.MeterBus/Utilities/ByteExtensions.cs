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
}
