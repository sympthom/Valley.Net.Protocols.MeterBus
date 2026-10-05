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

    [TestMethod]
    public void Serialize_LongFrame_WritesLengthChecksumAndStop()
    {
        var frame = new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_VARIABLE, 0x01, new byte[] { 0xAB, 0xCD }, 0);

        var bytes = _serializer.Serialize(frame);

        Assert.AreEqual("68 05 05 68 08 01 72 AB CD F3 16", bytes.ToHex());
        Assert.AreEqual(bytes.Length, _serializer.GetSerializedLength(frame));
    }

    [TestMethod]
    public void Serialize_ControlFrame_RoundTrips()
    {
        var original = new ControlFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, 0xFE, 0xA2);

        var bytes = _serializer.Serialize(original);

        Assert.AreEqual("68 03 03 68 53 FE 51 A2 16", bytes.ToHex());
        Assert.AreEqual(9, _serializer.GetSerializedLength(original));
        Assert.AreEqual(original, _parser.Parse(bytes).Value);
    }

    [TestMethod]
    public void Serialize_IntoSpan_WritesFrameAndReturnsLength()
    {
        var frame = new LongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, 0x05, new byte[] { 0x01, 0x7A, 0x07 }, 0);
        var buffer = new byte[32];

        var written = _serializer.Serialize(frame, buffer);

        Assert.AreEqual(_serializer.GetSerializedLength(frame), written);
        CollectionAssert.AreEqual(_serializer.Serialize(frame), buffer[..written]);
    }

    public static IEnumerable<TestDataRow<(string Folder, string Name)>> CorpusFrames() =>
        new[] { Corpus.TestFrames, Corpus.ErrorFrames, Corpus.UnsupportedFrames }
            .SelectMany(folder => Corpus.Names(folder).Select(name => new TestDataRow<(string, string)>((folder, name))
            {
                DisplayName = $"{folder}/{name}",
            }));

    // Every corpus frame the parser accepts must serialize back to the bytes it came from.
    [TestMethod]
    [DynamicData(nameof(CorpusFrames))]
    public void ParseThenSerialize_CorpusFrame_ReproducesBytes(string folder, string name)
    {
        var bytes = Corpus.ReadHex(folder, name);
        var result = _parser.Parse(bytes);
        if (!result.IsSuccess)
            return; // Rejected frames are covered by HexFileFrameParserTests.

        var serialized = _serializer.Serialize(result.Value!);

        Assert.AreEqual(bytes.ToHex(), serialized.ToHex());
        Assert.AreEqual(bytes.Length, _serializer.GetSerializedLength(result.Value!));
    }
}
