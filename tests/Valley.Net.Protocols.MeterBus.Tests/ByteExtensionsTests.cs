namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class ByteExtensionsTests
{
    [TestMethod]
    [DataRow(new byte[] { 0x42 }, "42", DisplayName = "1 byte")]
    [DataRow(new byte[] { 0x78, 0x56, 0x34, 0x12 }, "12345678", DisplayName = "4 bytes")]
    [DataRow(new byte[] { 0x12, 0x90, 0x78, 0x56, 0x34, 0x12 }, "123456789012", DisplayName = "6 bytes")]
    public void BCDToString_DecodesEveryByteMostSignificantLast(byte[] data, string expected)
    {
        Assert.AreEqual(expected, data.BCDToString());
    }

    [TestMethod]
    public void BCDToString_Span_MatchesArray()
    {
        byte[] data = [0x78, 0x56, 0x34, 0x12];
        Assert.AreEqual(data.BCDToString(), ((ReadOnlySpan<byte>)data).BCDToString());
    }

    [TestMethod]
    public void BCDToString_Empty_ReturnsEmpty()
    {
        Assert.AreEqual("", Array.Empty<byte>().BCDToString());
    }

    [TestMethod]
    [DataRow(new byte[] { 0x42 }, 42L, DisplayName = "1 byte")]
    [DataRow(new byte[] { 0x78, 0x56, 0x34, 0x12 }, 12345678L, DisplayName = "4 bytes")]
    [DataRow(new byte[] { 0x23, 0x01, 0x00, 0xF0 }, -123L, DisplayName = "Sign nibble")]
    [DataRow(new byte[] { 0x00, 0xF0 }, 0L, DisplayName = "Negative zero")]
    [DataRow(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99 }, 999999999999999999L, DisplayName = "18 digits")]
    [DataRow(new byte[0], 0L, DisplayName = "Empty")]
    public void TryDecodeBcd_Valid_ReturnsValue(byte[] data, long expected)
    {
        Assert.IsTrue(((ReadOnlySpan<byte>)data).TryDecodeBcd(out var value));
        Assert.AreEqual(expected, value);
    }

    [TestMethod]
    [DataRow(new byte[] { 0x4A }, DisplayName = "A in the low nibble")]
    [DataRow(new byte[] { 0x00, 0xE0 }, DisplayName = "E in the most significant nibble")]
    [DataRow(new byte[] { 0xF0, 0x00 }, DisplayName = "F below the most significant byte")]
    [DataRow(new byte[] { 0xFF }, DisplayName = "Sign then F")]
    [DataRow(new byte[] { 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99, 0x99 }, DisplayName = "20 digits overflow long")]
    public void TryDecodeBcd_Invalid_ReturnsFalse(byte[] data)
    {
        Assert.IsFalse(((ReadOnlySpan<byte>)data).TryDecodeBcd(out var value));
        Assert.AreEqual(0L, value);
    }
}
