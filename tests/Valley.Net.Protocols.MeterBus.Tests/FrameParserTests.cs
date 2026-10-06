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

    // Parse takes exactly one frame, so a reply merged with more bytes (a collision, a second
    // frame in one datagram) is reported instead of the extra bytes being dropped.
    [TestMethod]
    [DataRow("E5 E5", DisplayName = "Two ACKs")]
    [DataRow("E5 FF FF 68 00", DisplayName = "ACK with trailing bytes")]
    [DataRow("E5 10 5B 01 5C 16", DisplayName = "ACK followed by a short frame")]
    [DataRow("10 40 01 41 16 99 99", DisplayName = "Short frame with trailing bytes")]
    [DataRow("68 03 03 68 53 01 50 A4 16 AA", DisplayName = "Control frame with a trailing byte")]
    [DataRow("68 05 05 68 08 01 72 AB CD F3 16 DE AD BE EF", DisplayName = "Long frame with trailing bytes")]
    [DataRow("68 05 05 68 08 01 72 AB CD F3 16 E5", DisplayName = "Long frame followed by an ACK")]
    public void Parse_TrailingBytesAfterFrame_ReturnsTrailingData(string hex)
    {
        var result = _parser.Parse(hex.HexToBytes());

        Assert.IsFalse(result.IsSuccess, $"Parsed as {result.Value}");
        Assert.AreEqual("TRAILING_DATA", result.Error?.Code);
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

    [TestMethod]
    public void Parse_ShortFrame_BadStopByte_ReturnsFail()
    {
        var result = _parser.Parse("10 40 01 41 17".HexToBytes());
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("INVALID_STOP", result.Error?.Code);
    }

    // RSP_UD from address 1, CI 0x72, two data bytes; checksum 08+01+72+AB+CD = F3.
    private const string LongFrameHex = "68 05 05 68 08 01 72 AB CD F3 16";

    [TestMethod]
    public void Parse_LongFrame_ReturnsFields()
    {
        var result = _parser.Parse(LongFrameHex.HexToBytes());

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        var frame = Assert.IsInstanceOfType<LongFrame>(result.Value);
        Assert.AreEqual(ControlMask.RSP_UD, frame.Control);
        Assert.AreEqual((byte)0x01, frame.Address);
        Assert.AreEqual(ControlInformation.RESP_VARIABLE, frame.ControlInformation);
        CollectionAssert.AreEqual(new byte[] { 0xAB, 0xCD }, frame.Data.ToArray());
        Assert.AreEqual((byte)0xF3, frame.Crc);
    }

    // L = 3 carries only C, A and CI.
    [TestMethod]
    public void Parse_LongFrameWithoutData_ReturnsControlFrame()
    {
        var result = _parser.Parse("68 03 03 68 53 FE 51 A2 16".HexToBytes());

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        var frame = Assert.IsInstanceOfType<ControlFrame>(result.Value);
        Assert.AreEqual(ControlMask.SND_UD, frame.Control);
        Assert.AreEqual((byte)0xFE, frame.Address);
        Assert.AreEqual(ControlInformation.DATA_SEND, frame.ControlInformation);
        Assert.AreEqual((byte)0xA2, frame.Crc);
    }

    [TestMethod]
    [DataRow("68 05 05", "LONG_FRAME_TOO_SHORT", DisplayName = "Header cut short")]
    [DataRow("68 02 02 68 08 01 09 16", "INVALID_LENGTH", DisplayName = "L below 3")]
    [DataRow("68 05 06 68 08 01 72 AB CD F3 16", "LENGTH_MISMATCH", DisplayName = "L differs from L'")]
    [DataRow("68 05 05 68 08 01 72 AB CD F3", "LONG_FRAME_INCOMPLETE", DisplayName = "Stop byte missing")]
    [DataRow("68 05 05 68 08 01 72 AB", "LONG_FRAME_INCOMPLETE", DisplayName = "Data cut short")]
    [DataRow("68 05 05 69 08 01 72 AB CD F3 16", "INVALID_START2", DisplayName = "Second start byte wrong")]
    [DataRow("68 05 05 68 08 01 72 AB CD F4 16", "CRC_MISMATCH", DisplayName = "Long frame checksum wrong")]
    [DataRow("68 03 03 68 53 FE 51 A3 16", "CRC_MISMATCH", DisplayName = "Control frame checksum wrong")]
    [DataRow("68 05 05 68 08 01 72 AB CD F3 17", "INVALID_STOP", DisplayName = "Long frame stop byte wrong")]
    [DataRow("68 03 03 68 53 FE 51 A2 00", "INVALID_STOP", DisplayName = "Control frame stop byte wrong")]
    public void Parse_MalformedLongFrame_ReturnsFail(string hex, string expectedCode)
    {
        var result = _parser.Parse(hex.HexToBytes());

        Assert.IsFalse(result.IsSuccess, $"Parsed as {result.Value}");
        Assert.AreEqual(expectedCode, result.Error?.Code);
    }
}
