namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// SI unit prefix lookup.
/// </summary>
public static class UnitPrefix
{
    public static string GetUnitPrefix(int magnitude) => magnitude switch
    {
        0 => string.Empty,
        -3 => "m",
        -6 => "my",
        1 => "10 ",
        2 => "100 ",
        3 => "k",
        4 => "10 k",
        5 => "100 k",
        6 => "M",
        9 => "T",
        _ => $"1e{magnitude}",
    };
}
