namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// String extension methods for hex conversions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts hex such as "68 04 04 68", "68-04-04-68" or "68:04:04:68" to bytes. Whitespace (including
    /// line breaks) and '-'/':' separators are ignored.
    /// </summary>
    /// <exception cref="FormatException">The remaining text has an odd number of digits or a non-hex character.</exception>
    public static byte[] HexToBytes(this string hex)
    {
        // Strip separators first and let FromHexString reject what is left, so a dropped nibble fails
        // instead of shifting every following byte.
        var cleaned = string.Concat(hex.Where(c => !char.IsWhiteSpace(c) && c != '-' && c != ':'));
        return Convert.FromHexString(cleaned);
    }
}
