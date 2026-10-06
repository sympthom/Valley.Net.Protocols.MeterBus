namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Link-layer behaviour of <see cref="MBusMaster"/>. The reply timeout belongs to the transport.
/// </summary>
public sealed class MBusMasterOptions
{
    /// <summary>
    /// Extra attempts after a request gets no reply or a garbled one. The default matches libmbus
    /// MBUS_OPTION_MAX_DATA_RETRY: slow or battery meters often miss the first frame after an idle bus.
    /// </summary>
    public int Retries { get; set; } = 3;

    /// <summary>
    /// Extra SND_NKE attempts per address in <see cref="IMBusMaster.ScanAsync"/>. Kept low because most scanned
    /// addresses are empty and each attempt costs a full timeout; the default matches libmbus
    /// MBUS_OPTION_MAX_SEARCH_RETRY. Reading a meter that answered uses <see cref="Retries"/>.
    /// </summary>
    public int ScanRetries { get; set; } = 1;

    /// <summary>
    /// Most telegrams <see cref="IMBusMaster.RequestAllTelegramsAsync"/> reads before it gives up on a meter that
    /// keeps signalling more records, so a faulty one cannot hold the bus forever. At least 1; the default is the
    /// MAXFRAMES of the libmbus multi-reply tools.
    /// </summary>
    public int MaxTelegrams { get; set; } = 16;
}
