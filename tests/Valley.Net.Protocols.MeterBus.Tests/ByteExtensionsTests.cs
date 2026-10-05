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
}
