namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class PacketMapperTests
{
    private readonly FrameParser _parser = new();
    private readonly PacketMapper _mapper = new(new VifLookupService());

    [TestMethod]
    [DataRow("68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 9E 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 22 F3 26 42 05 5F C7 DA 0D 42 FA 16", DisplayName = "example_data_01")]
    public void MapToPacket_VariableDataFrame_Success(string hex)
    {
        var bytes = hex.HexToBytes();
        var frameResult = _parser.Parse(bytes);
        Assert.IsTrue(frameResult.IsSuccess, $"Frame parse failed: {frameResult.Error?.Message}");

        var packetResult = _mapper.MapToPacket(frameResult.Value!);
        Assert.IsTrue(packetResult.IsSuccess, $"Packet map failed: {packetResult.Error?.Message}");

        var packet = packetResult.Value as VariableDataPacket;
        Assert.IsNotNull(packet);
        Assert.IsTrue(packet.Records.Length > 0);
    }
}
