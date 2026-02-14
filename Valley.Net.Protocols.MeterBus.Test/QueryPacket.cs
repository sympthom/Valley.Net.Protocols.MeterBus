using Valley.Net.Bindings;

namespace Valley.Net.Protocols.MeterBus.Test;

internal sealed class QueryPacket : INetworkPacket
{
    public byte[] Data { get; set; } = [];
}
