namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// High-level M-Bus master operations.
/// </summary>
/// <remarks>
/// <para>
/// Each operation holds the bus for its whole exchange, so concurrent calls on one master run one after
/// another. A missing or garbled reply is retried (see <c>MBusMasterOptions.Retries</c>) before it is reported.
/// </para>
/// <para>
/// Failures are reported the same way everywhere:
/// <see cref="TimeoutException"/> when no reply arrived on any attempt;
/// <see cref="MBusException"/> when a reply arrived but cannot be used (garbled, wrong frame type, another
/// address, unmappable), with the cause in <see cref="MBusException.Error"/>;
/// <see cref="OperationCanceledException"/> only when <c>ct</c> is cancelled;
/// <see cref="ArgumentOutOfRangeException"/> for the reserved addresses 251 and 252, before anything is sent.
/// </para>
/// <para>
/// Addresses: 0-250 are slaves, 253 is the slave selected by <see cref="SelectSlaveAsync(SecondaryAddress, CancellationToken)"/>, and 254 is
/// answered by every slave, so it only works with one slave on the bus; with more, the replies collide.
/// 255 is a broadcast that no slave answers: commands sent to it return as soon as the frame is sent.
/// </para>
/// <para>
/// Frame count bit: the master keeps the FCB of EN 13757-2 per address, one for requests and one for SND_UD.
/// It toggles after each answered exchange and is repeated unchanged on a retry, so a slave whose reply was
/// lost sends it again. A SND_NKE (<see cref="PingAsync"/>, <see cref="InitializeAsync"/>, <see cref="ScanAsync"/>)
/// restarts the sequence at FCB = 1 for that address, or for all of them when sent to 254 or 255, and a
/// selection restarts it for 253. A SND_UD to 255 goes out with FCV cleared (43h), since nothing answers it.
/// Call <see cref="InitializeAsync"/> before the first exchange with a meter so meter and master agree on it.
/// </para>
/// Disposing the master does not dispose the transport.
/// </remarks>
public interface IMBusMaster : IAsyncDisposable
{
    /// <summary>
    /// Sends SND_NKE; returns true when the slave answers E5 and false when nothing answers or the reply is
    /// garbled or something else. Sent to 255 it returns false without waiting. Use <see cref="ScanAsync"/>
    /// to tell a collision from silence.
    /// </summary>
    Task<bool> PingAsync(byte address, CancellationToken ct = default);

    /// <summary>
    /// Sends REQ_UD2 and returns the RSP_UD. An <see cref="ApplicationErrorPacket"/> (CI 70h) or
    /// <see cref="AlarmStatusPacket"/> (CI 71h) is returned like any other packet.
    /// </summary>
    /// <exception cref="TimeoutException">No reply arrived.</exception>
    /// <exception cref="MBusException">
    /// The reply is garbled, is not an RSP_UD (an E5 included), comes from another address, or cannot be mapped.
    /// Replies to 0xFD and 0xFE carry the slave's own primary address and are not address-checked.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="address"/> is 251, 252 or 255.</exception>
    Task<MBusPacket> RequestDataAsync(byte address, CancellationToken ct = default);

    /// <summary>
    /// Sends REQ_UD1 and returns the RSP_UD, normally an <see cref="AlarmStatusPacket"/>. An E5 means no alarm
    /// data is pending and returns an <see cref="EmptyPacket"/> for <paramref name="address"/>. Otherwise fails
    /// like <see cref="RequestDataAsync"/>.
    /// </summary>
    Task<MBusPacket> RequestAlarmAsync(byte address, CancellationToken ct = default);

    /// <summary>
    /// Reads a multi-telegram answer: sends REQ_UD2 with a toggled FCB for as long as the last telegram is a
    /// <see cref="VariableDataPacket"/> with <see cref="VariableDataPacket.MoreRecordsFollow"/>, and returns the
    /// telegrams in order. The sequence also ends when a follow-up request is answered with an E5, which is not
    /// returned. The bus is held for the whole sequence. Fails like <see cref="RequestDataAsync"/> on any telegram.
    /// </summary>
    /// <exception cref="MBusException">
    /// More records still follow after <c>MBusMasterOptions.MaxTelegrams</c> telegrams (<c>TELEGRAM_LIMIT</c>),
    /// or a telegram comes from another meter than the first (<c>TELEGRAM_MISMATCH</c>), possible via 0xFD and 0xFE.
    /// </exception>
    Task<IReadOnlyList<MBusPacket>> RequestAllTelegramsAsync(byte address, CancellationToken ct = default);

    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="newAddress"/> is above 250, or <paramref name="address"/> is 251 or 252.
    /// </exception>
    Task SetAddressAsync(byte address, byte newAddress, CancellationToken ct = default);

    /// <summary>
    /// Sends SND_NKE. Sent to 0xFD it is answered only by a selected slave, so it times out when none is selected.
    /// </summary>
    /// <exception cref="TimeoutException">No E5 arrived.</exception>
    /// <exception cref="MBusException">Something other than an E5 arrived (<c>UNEXPECTED_FRAME</c>), or only garbled replies.</exception>
    Task InitializeAsync(byte address, CancellationToken ct = default);

    /// <summary>Sends SND_UD with CI 50h; fails like <see cref="InitializeAsync"/>.</summary>
    Task ResetApplicationAsync(byte address, CancellationToken ct = default);

    /// <summary>
    /// Pings each address and reads the ones that answer. Silent addresses yield nothing; every address that
    /// answered in any way yields one <see cref="MeterInfo"/> whose <see cref="MeterInfo.Status"/> says how.
    /// A problem at one address never ends the scan; a transport failure or cancellation does.
    /// The bus is held per address, not for the whole scan.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">An address is 251, 252 or 255, thrown when the scan reaches it.</exception>
    IAsyncEnumerable<MeterInfo> ScanAsync(IEnumerable<byte> addresses, CancellationToken ct = default);

    /// <summary>Sends SND_UD with CI 51h; fails like <see cref="InitializeAsync"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="data"/> is longer than 252 bytes.</exception>
    Task SendDataAsync(byte address, ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Selects the slave with this secondary address: sends SND_UD with CI 52h to 253 (0xFD). The selected slave is
    /// then read with <c>RequestDataAsync(MBusConstants.ADDRESS_NETWORK_LAYER)</c>. Fails like
    /// <see cref="InitializeAsync"/>; a timeout means no slave matched.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The identification number has more than eight digits.</exception>
    Task SelectSlaveAsync(SecondaryAddress secondary, CancellationToken ct = default);

    /// <summary>
    /// Like <see cref="SelectSlaveAsync(SecondaryAddress, CancellationToken)"/>, with the identification number
    /// as its four bytes read little-endian, as in <see cref="VariableDataPacket.IdentificationRaw"/>. Its hex
    /// digits are the BCD digits, so 0x12345678 selects 12345678. Every nibble is sent as given, which selects
    /// meters whose ID has non-decimal digits, and an F nibble is a wildcard (0x1234FFFF matches 1234xxxx).
    /// Manufacturer 0xFFFF, version 0xFF and device type 0xFF are wildcards too.
    /// </summary>
    Task SelectSlaveAsync(uint identificationRaw, ushort manufacturerId, byte version, DeviceType deviceType, CancellationToken ct = default);
}

/// <summary>
/// A meter's secondary address. <see cref="IdentificationNo"/> is the decimal ID, at most 99999999; use the raw
/// <see cref="IMBusMaster.SelectSlaveAsync(uint, ushort, byte, DeviceType, CancellationToken)"/> for wildcards.
/// </summary>
public sealed record SecondaryAddress(
    uint IdentificationNo,
    ushort ManufacturerId,
    byte Version,
    DeviceType DeviceType);

/// <summary>
/// One address that answered a scan. <see cref="Packet"/> is set only for <see cref="ScanStatus.Found"/>;
/// <see cref="Error"/> says what went wrong otherwise.
/// </summary>
public sealed record MeterInfo(
    byte Address,
    MBusPacket? Packet,
    ScanStatus Status = ScanStatus.Found,
    MBusError? Error = null);

public enum ScanStatus
{
    /// <summary>The meter ACKed SND_NKE and its RSP_UD mapped to <see cref="MeterInfo.Packet"/>.</summary>
    Found,

    /// <summary>
    /// Only garbled replies came back, to SND_NKE or REQ_UD2. Usually two slaves share the address;
    /// <see cref="MeterInfo.Error"/> holds the parse failure.
    /// </summary>
    Collision,

    /// <summary>
    /// The address answered but gave no usable data: no RSP_UD after the ACK (<c>NO_REPLY</c>), a frame of the
    /// wrong kind (<c>UNEXPECTED_FRAME</c>), a reply from another address (<c>ADDRESS_MISMATCH</c>), or one the
    /// mapper rejected (its own code, e.g. <c>ENCRYPTED</c>).
    /// </summary>
    Error,
}
