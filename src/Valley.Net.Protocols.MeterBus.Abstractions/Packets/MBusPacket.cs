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

/// <summary>
/// Reply with CI 73h (MBDOC48 6.2). <see cref="DeviceType"/> is the <see cref="Medium"/> without its mode 2
/// marking, and <c>Unknown</c> for the reserved media 9 and F. The counters are null when their BCD coding
/// has a non-decimal digit.
/// </summary>
public sealed record FixedDataPacket(
    byte Address,
    uint IdentificationNo,
    DeviceType DeviceType,
    byte TransmissionCounter,
    bool CountersFixed,
    FixedDataUnits Units1,
    FixedDataUnits Units2,
    long? Counter1,
    long? Counter2) : MBusPacket(Address)
{
    /// <summary>
    /// The four identification bytes as sent, read little-endian. <see cref="IdentificationNo"/> is their BCD reading.
    /// </summary>
    public uint IdentificationRaw { get; init; }

    /// <summary>
    /// Bit 0 signed binary counters (else BCD), bit 1 stored at fixed date, bit 2 power low, bit 3 permanent
    /// error, bit 4 temporary error, bits 5-7 manufacturer specific.
    /// </summary>
    public byte Status { get; init; }

    /// <summary>
    /// The medium as coded in the fixed structure, which differs from <see cref="DeviceType"/> from 9 up.
    /// </summary>
    public FixedDataMedium Medium { get; init; }
}

public sealed record VariableDataPacket(
    byte Address,
    uint IdentificationNo,
    ushort Manufacturer,
    byte Version,
    DeviceType DeviceType,
    byte TransmissionCounter,
    byte Status,
    ushort Signature,
    ImmutableArray<DataRecord> Records) : MBusPacket(Address)
{
    /// <summary>
    /// The four identification bytes as sent, read little-endian. <see cref="IdentificationNo"/> is their BCD reading.
    /// </summary>
    public uint IdentificationRaw { get; init; }

    /// <summary>
    /// Bytes after a DIF 0x0F/0x1F, which have manufacturer-specific coding. Empty when the telegram has none.
    /// </summary>
    public ImmutableArray<byte> ManufacturerData { get; init; } = ImmutableArray<byte>.Empty;

    /// <summary>
    /// True when the records ended with DIF 0x1F: the meter has more records for the next REQ_UD2.
    /// </summary>
    public bool MoreRecordsFollow { get; init; }
}

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
    /// <summary>
    /// Null when the value decoded cleanly. Otherwise why <see cref="Value"/> is null: "INVALID_BCD" for a BCD
    /// value with a non-decimal digit, "INVALID_DATE" for a date/time with the invalid bit set or a field out of range.
    /// </summary>
    public string? ValueError { get; init; }

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
