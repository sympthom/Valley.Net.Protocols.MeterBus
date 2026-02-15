namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class FrameSerializerTests
{
    private readonly FrameSerializer _serializer = new();
    private readonly FrameParser _parser = new();

    [TestMethod]
    public void Serialize_AckFrame_SingleByte()
    {
        var bytes = _serializer.Serialize(new AckFrame());
        CollectionAssert.AreEqual(new byte[] { 0xE5 }, bytes);
    }

    [TestMethod]
    public void Serialize_ShortFrame_RoundTrips()
    {
        var original = new ShortFrame(ControlMask.SND_NKE, 0x01, 0x41);
        var bytes = _serializer.Serialize(original);
        var result = _parser.Parse(bytes);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(original, result.Value);
    }

    [TestMethod]
    public void GetSerializedLength_AckFrame_Returns1()
    {
        Assert.AreEqual(1, _serializer.GetSerializedLength(new AckFrame()));
    }

    [TestMethod]
    public void GetSerializedLength_ShortFrame_Returns5()
    {
        Assert.AreEqual(5, _serializer.GetSerializedLength(new ShortFrame(ControlMask.SND_NKE, 0x01, 0x41)));
    }
}
