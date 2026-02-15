namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// M-Bus manufacturer ID parser.
/// </summary>
public static class ManufacturerParser
{
    public static string Parse(ushort value)
    {
        Span<char> chr = stackalloc char[3];
        for (int i = 2; i >= 0; i--)
        {
            chr[i] = (char)((value % 32) + 64);
            value = (ushort)((value - (value % 32)) / 32);
        }
        return new string(chr);
    }

    public static int Encode(string manufacturer)
    {
        if (manufacturer is null || manufacturer.Length < 3)
            return 0;

        if (!char.IsLetter(manufacturer[0]) ||
            !char.IsLetter(manufacturer[1]) ||
            !char.IsLetter(manufacturer[2]))
            return 0;

        var id = (char.ToUpper(manufacturer[0]) - 64) * 32 * 32 +
                 (char.ToUpper(manufacturer[1]) - 64) * 32 +
                 (char.ToUpper(manufacturer[2]) - 64);

        return 0x0421 <= id && id <= 0x6b5a ? id : 0;
    }
}
