namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Parses raw bytes into M-Bus frames.
/// </summary>
public interface IFrameParser
{
    MBusParseResult<MBusFrame> Parse(ReadOnlySpan<byte> data);
}
