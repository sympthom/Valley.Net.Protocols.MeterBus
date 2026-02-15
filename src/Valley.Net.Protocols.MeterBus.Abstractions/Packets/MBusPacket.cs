using System.Collections.Immutable;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Base record for all M-Bus application-layer packets. Immutable.
/// </summary>
public abstract record MBusPacket(byte Address);

public sealed record EmptyPacket(byte Address) : MBusPacket(Address);

public sealed record AlarmStatusPacket(byte Address, byte Status) : MBusPacket(Address);

public sealed record ApplicationErrorPacket(byte Address, ApplicationErrorCode Code) : MBusPacket(Address);

public enum ApplicationErrorCode : byte
{
    Unspecified = 0x00,
    Unimplemented_CI = 0x01,
    BufferTooLong = 0x02,
    TooManyRecords = 0x03,
    PrematureEnd = 0x04,
    TooManyDIFEs = 0x05,
    TooManyVIFEs = 0x06,
    Reserved = 0x07,
    Busy = 0x08,
    TooManyReadouts = 0x09,
}

public sealed record FixedDataPacket(
    byte Address,
    uint IdentificationNo,
    DeviceType DeviceType,
    byte TransmissionCounter,
    bool CountersFixed,
    FixedDataUnits Units1,
    FixedDataUnits Units2,
    uint Counter1,
    uint Counter2) : MBusPacket(Address);

public sealed record VariableDataPacket(
    byte Address,
    uint IdentificationNo,
    ushort Manufacturer,
    byte Version,
    DeviceType DeviceType,
    byte TransmissionCounter,
    byte Status,
    ushort Signature,
    ImmutableArray<DataRecord> Records) : MBusPacket(Address);

public sealed record DataRecord(
    VariableDataRecordType RecordType,
    Function Function,
    ulong StorageNumber,
    uint Tariff,
    ushort SubUnit,
    DataTypes ValueDataType,
    object? Value,
    ImmutableArray<UnitInfo> Units)
{
    public int Magnitude => Units
        .Where(u => u.Units != VariableDataQuantityUnit.AdditiveCorrectionConstant)
        .Sum(u => u.Magnitude);

    public int Offset => Units
        .Where(u => u.Units == VariableDataQuantityUnit.AdditiveCorrectionConstant)
        .Sum(u => u.Magnitude);
}

public sealed record UnitInfo(
    VariableDataQuantityUnit Units,
    string? Unit,
    int Magnitude,
    string? Quantity,
    string? VifString);
