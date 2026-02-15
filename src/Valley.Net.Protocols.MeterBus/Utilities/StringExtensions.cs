namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// String extension methods for hex conversions.
/// </summary>
public static class StringExtensions
{
    public static byte[] HexToBytes(this string hex)
    {
        var cleaned = hex.Replace(" ", "").Replace("-", "");
        var bytes = new byte[cleaned.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(cleaned.Substring(i * 2, 2), 16);
        return bytes;
    }
}
