namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// High-level M-Bus master operations.
/// </summary>
public interface IMBusMaster : IAsyncDisposable
{
    Task<bool> PingAsync(byte address, CancellationToken ct = default);
    Task<MBusPacket> RequestDataAsync(byte address, CancellationToken ct = default);
    Task<MBusPacket> RequestAlarmAsync(byte address, CancellationToken ct = default);
    Task SetAddressAsync(byte address, byte newAddress, CancellationToken ct = default);
    Task InitializeAsync(byte address, CancellationToken ct = default);
    Task ResetApplicationAsync(byte address, CancellationToken ct = default);
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
