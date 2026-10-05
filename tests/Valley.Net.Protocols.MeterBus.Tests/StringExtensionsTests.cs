namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class StringExtensionsTests
{
    [TestMethod]
    [DataRow("68 04 04 68", DisplayName = "Spaces")]
    [DataRow("68-04-04-68", DisplayName = "Dashes")]
    [DataRow("68:04:04:68", DisplayName = "Colons")]
    [DataRow("68 04\r\n04\t68\n", DisplayName = "Line breaks and tabs")]
    [DataRow("68040468", DisplayName = "No separators")]
    public void HexToBytes_IgnoresWhitespaceAndSeparators(string hex)
    {
        CollectionAssert.AreEqual(new byte[] { 0x68, 0x04, 0x04, 0x68 }, hex.HexToBytes());
    }

    [TestMethod]
    public void HexToBytes_LowerCase_Decodes()
    {
        CollectionAssert.AreEqual(new byte[] { 0xE5, 0xAB }, "e5 ab".HexToBytes());
    }

    [TestMethod]
    public void HexToBytes_Empty_ReturnsEmpty()
    {
        Assert.IsEmpty(" ".HexToBytes());
    }

    // A dropped nibble used to truncate or shift the bytes silently.
    [TestMethod]
    [DataRow("E5 1", DisplayName = "Trailing nibble")]
    [DataRow("ABC", DisplayName = "Odd length")]
    [DataRow("D 04 04 68", DisplayName = "Leading nibble (manual_frame1.hex)")]
    public void HexToBytes_OddDigitCount_Throws(string hex)
    {
        Assert.ThrowsExactly<FormatException>(() => hex.HexToBytes());
    }

    [TestMethod]
    [DataRow("ZZ", DisplayName = "Non-hex")]
    [DataRow("68,04", DisplayName = "Unsupported separator")]
    public void HexToBytes_NonHexCharacter_Throws(string hex)
    {
        Assert.ThrowsExactly<FormatException>(() => hex.HexToBytes());
    }
}
