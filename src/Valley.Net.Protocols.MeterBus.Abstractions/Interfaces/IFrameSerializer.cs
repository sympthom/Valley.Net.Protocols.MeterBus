namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Serializes M-Bus frames into raw bytes.
/// </summary>
public interface IFrameSerializer
{
    int Serialize(MBusFrame frame, Span<byte> destination);
    int GetSerializedLength(MBusFrame frame);
    byte[] Serialize(MBusFrame frame);
}
