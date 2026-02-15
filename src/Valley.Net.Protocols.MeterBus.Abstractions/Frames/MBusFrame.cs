using System.Collections.Immutable;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Base record for all M-Bus frame types. Immutable discriminated union.
/// </summary>
public abstract record MBusFrame;

public sealed record AckFrame() : MBusFrame;

public sealed record ShortFrame(
    ControlMask Control,
    byte Address,
    byte Crc) : MBusFrame;

public sealed record ControlFrame(
    ControlMask Control,
    ControlInformation ControlInformation,
    byte Address,
    byte Crc) : MBusFrame;

public sealed record LongFrame(
    ControlMask Control,
    ControlInformation ControlInformation,
    byte Address,
    ReadOnlyMemory<byte> Data,
    byte Crc) : MBusFrame;
