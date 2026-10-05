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

    [TestMethod]
    public void Serialize_LongFrameWith252DataBytes_RoundTrips()
    {
        var frame = new LongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, 0x01, new byte[252], 0);

        var bytes = _serializer.Serialize(frame);

        Assert.AreEqual((byte)0xFF, bytes[1]);
        Assert.IsTrue(_parser.Parse(bytes).IsSuccess);
    }

    [TestMethod]
    public void Serialize_LongFrameWith253DataBytes_Throws()
    {
        var frame = new LongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, 0x01, new byte[253], 0);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _serializer.Serialize(frame));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _serializer.GetSerializedLength(frame));
    }

    // EN 13757-3 CI table; a mode 2 (MSB-first) code is the mode 1 code | 0x04, not | 0x80.
    [TestMethod]
    [DataRow(ControlInformation.DATA_SEND_MSB, (byte)0x55)]
    [DataRow(ControlInformation.SELECT_SLAVE_MSB, (byte)0x56)]
    [DataRow(ControlInformation.RESP_VARIABLE_MSB, (byte)0x76)]
    [DataRow(ControlInformation.RESP_FIXED_MSB, (byte)0x77)]
    [DataRow(ControlInformation.INIT_TEST_CALIB, (byte)0xB3)]
    [DataRow(ControlInformation.EEPROM_READ, (byte)0xB4)]
    [DataRow(ControlInformation.SW_TEST_START, (byte)0xB6)]
    public void Serialize_ControlFrame_WritesSpecCiByte(ControlInformation ci, byte expected)
    {
        var bytes = _serializer.Serialize(new ControlFrame(ControlMask.SND_UD, ci, 0x01, 0));

        Assert.AreEqual(expected, bytes[6]);
    }
}
