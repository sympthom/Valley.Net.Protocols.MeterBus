namespace Valley.Net.Protocols.MeterBus;

public enum VifExtensionTable
{
    Primary,
    FB,
    FD,

    /// <summary>
    /// Combinable (orthogonal) VIFE extension table, used for the VIFE after a combinable VIFE 0xFC.
    /// </summary>
    FC,
}
