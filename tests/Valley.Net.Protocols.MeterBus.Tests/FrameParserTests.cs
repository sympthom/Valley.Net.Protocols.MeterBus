using System.Collections.Immutable;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class FrameParserTests
{
    private readonly FrameParser _parser = new();

    [TestMethod]
    public void Parse_AckByte_ReturnsAckFrame()
    {
        var result = _parser.Parse([0xE5]);
        Assert.IsTrue(result.IsSuccess);
        Assert.IsInstanceOfType<AckFrame>(result.Value);
    }

    [TestMethod]
    public void Parse_EmptyData_ReturnsFail()
    {
        var result = _parser.Parse(ReadOnlySpan<byte>.Empty);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("EMPTY", result.Error?.Code);
    }

    [TestMethod]
    public void Parse_UnknownStartByte_ReturnsFail()
    {
        var result = _parser.Parse([0x99]);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("UNKNOWN_START", result.Error?.Code);
    }

    [TestMethod]
    [DataRow("10 40 01 41 16", ControlMask.SND_NKE, (byte)0x01, DisplayName = "SND_NKE address 1")]
    [DataRow("10 5B 01 5C 16", ControlMask.REQ_UD2, (byte)0x01, DisplayName = "REQ_UD2 address 1")]
    public void Parse_ShortFrame_Success(string hex, ControlMask expectedControl, byte expectedAddress)
    {
        var bytes = hex.HexToBytes();
        var result = _parser.Parse(bytes);
        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        var frame = result.Value as ShortFrame;
        Assert.IsNotNull(frame);
        Assert.AreEqual(expectedControl, frame.Control);
        Assert.AreEqual(expectedAddress, frame.Address);
    }

    [TestMethod]
    public void Parse_ShortFrame_CrcMismatch_ReturnsFail()
    {
        var result = _parser.Parse([0x10, 0x40, 0x01, 0xFF, 0x16]);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("CRC_MISMATCH", result.Error?.Code);
    }

    [TestMethod]
    public void Parse_ShortFrame_TooShort_ReturnsFail()
    {
        var result = _parser.Parse([0x10, 0x40]);
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("SHORT_FRAME_TOO_SHORT", result.Error?.Code);
    }
}
