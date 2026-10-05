namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// High-level M-Bus master operations.
/// </summary>
/// <remarks>
/// Commands (initialize, set address, reset, send data, select) wait for the slave's E5 and throw
/// <see cref="TimeoutException"/> when none arrives or <see cref="InvalidOperationException"/> when
/// something else does. Sent to broadcast 0xFF they return without waiting, since no slave answers.
/// Cancelling <c>ct</c> always surfaces as <see cref="OperationCanceledException"/>.
/// Disposing the master does not dispose the transport.
/// </remarks>
public interface IMBusMaster : IAsyncDisposable
{
    /// <summary>Sends SND_NKE; returns false when the slave does not answer with E5.</summary>
    Task<bool> PingAsync(byte address, CancellationToken ct = default);

    /// <summary>Sends REQ_UD2 and returns the RSP_UD.</summary>
    /// <exception cref="TimeoutException">No reply arrived.</exception>
    /// <exception cref="InvalidOperationException">
    /// The reply is not an RSP_UD, comes from another address, or cannot be mapped. Replies to 0xFD and 0xFE
    /// carry the slave's own primary address and are not address-checked.
    /// </exception>
    Task<MBusPacket> RequestDataAsync(byte address, CancellationToken ct = default);

    /// <summary>Sends REQ_UD1 and returns the RSP_UD; fails like <see cref="RequestDataAsync"/>.</summary>
    Task<MBusPacket> RequestAlarmAsync(byte address, CancellationToken ct = default);

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="newAddress"/> is above 250.</exception>
    Task SetAddressAsync(byte address, byte newAddress, CancellationToken ct = default);

    /// <summary>
    /// Sends SND_NKE. Sent to 0xFD it is answered only by a selected slave, so it times out when none is selected.
    /// </summary>
    Task InitializeAsync(byte address, CancellationToken ct = default);
    Task ResetApplicationAsync(byte address, CancellationToken ct = default);

    /// <summary>
    /// Pings each address and reads the ones that answer. A silent address is skipped, a reply from
    /// another address is skipped, and a meter that ACKs but sends no usable data yields a null packet.
    /// </summary>
    IAsyncEnumerable<MeterInfo> ScanAsync(IEnumerable<byte> addresses, CancellationToken ct = default);
    Task SendDataAsync(byte address, ReadOnlyMemory<byte> data, CancellationToken ct = default);
    Task SelectSlaveAsync(byte address, SecondaryAddress secondary, CancellationToken ct = default);
}

public sealed record SecondaryAddress(
    uint IdentificationNo,
    ushort ManufacturerId,
    byte Version,
    DeviceType DeviceType);

public sealed record MeterInfo(byte Address, MBusPacket? Packet);
