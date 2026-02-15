namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Maps parsed M-Bus frames to application-layer packets.
/// </summary>
public interface IPacketMapper
{
    MBusParseResult<MBusPacket> MapToPacket(MBusFrame frame);
}
