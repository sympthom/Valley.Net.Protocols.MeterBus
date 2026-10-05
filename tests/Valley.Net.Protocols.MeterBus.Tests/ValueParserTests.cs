namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class ValueParserTests
{
    [TestMethod]
    public void ParseValue_8BitInteger_ReturnsCorrectValue()
    {
        var result = ValueParser.ParseValue(DataTypes._8_Bit_Integer, [0x42]);
        Assert.AreEqual((sbyte)0x42, result);
    }

    [TestMethod]
    public void ParseValue_16BitInteger_ReturnsCorrectValue()
    {
        var result = ValueParser.ParseValue(DataTypes._16_Bit_Integer, [0x01, 0x00]);
        Assert.AreEqual((short)1, result);
    }

    [TestMethod]
    [DataRow(new byte[] { 0xFF }, (sbyte)-1, DisplayName = "-1")]
    [DataRow(new byte[] { 0x80 }, sbyte.MinValue, DisplayName = "8-bit minimum")]
    [DataRow(new byte[] { 0x7F }, sbyte.MaxValue, DisplayName = "8-bit maximum")]
    public void ParseValue_8BitInteger_IsSigned(byte[] data, sbyte expected)
    {
        var result = ValueParser.ParseValue(DataTypes._8_Bit_Integer, data);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseValue_16BitInteger_Negative_IsSigned()
    {
        var result = ValueParser.ParseValue(DataTypes._16_Bit_Integer, [0xFE, 0xFF]);
        Assert.AreEqual((short)-2, result);
    }

    [TestMethod]
    [DataRow(new byte[] { 0xBE, 0xFF, 0xFF }, -66, DisplayName = "EMU Professional 375 current -66 mA")]
    [DataRow(new byte[] { 0x00, 0x00, 0x80 }, -8388608, DisplayName = "24-bit minimum")]
    [DataRow(new byte[] { 0xFF, 0xFF, 0x7F }, 8388607, DisplayName = "24-bit maximum")]
    public void ParseValue_24BitInteger_IsSignExtended(byte[] data, int expected)
    {
        var result = ValueParser.ParseValue(DataTypes._24_Bit_Integer, data);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseValue_32BitInteger_Negative_IsSigned()
    {
        var result = ValueParser.ParseValue(DataTypes._32_Bit_Integer, [0xFE, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(-2, result);
    }

    [TestMethod]
    [DataRow(new byte[] { 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, -2L, DisplayName = "-2")]
    [DataRow(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 }, -140737488355328L, DisplayName = "48-bit minimum")]
    [DataRow(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F }, 140737488355327L, DisplayName = "48-bit maximum")]
    public void ParseValue_48BitInteger_IsSignExtended(byte[] data, long expected)
    {
        var result = ValueParser.ParseValue(DataTypes._48_Bit_Integer, data);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseValue_64BitInteger_Negative_IsSigned()
    {
        var result = ValueParser.ParseValue(DataTypes._64_Bit_Integer, [0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
        Assert.AreEqual(-2L, result);
    }

    [TestMethod]
    [DataRow(DataTypes._2_digit_BCD, new byte[] { 0x42 }, 42L, DisplayName = "2-digit")]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0x34, 0x12 }, 1234L, DisplayName = "4-digit")]
    [DataRow(DataTypes._6_digit_BCD, new byte[] { 0x56, 0x34, 0x12 }, 123456L, DisplayName = "6-digit")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0x78, 0x56, 0x34, 0x12 }, 12345678L, DisplayName = "8-digit")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x12, 0x90, 0x78, 0x56, 0x34, 0x12 }, 123456789012L, DisplayName = "12-digit")]
    public void ParseValue_Bcd_DecodesAllDigits(DataTypes dataType, byte[] data, long expected)
    {
        var result = ValueParser.ParseValue(dataType, data);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void ParseValue_8DigitBcd_ItronCybleFabricationNumber()
    {
        // ACW_Itron-CYBLE-M-Bus-14: record 0C 78 23 15 01 09, libmbus decodes Fabrication number 9011523.
        var result = ValueParser.ParseValue(DataTypes._8_digit_BCD, [0x23, 0x15, 0x01, 0x09]);
        Assert.AreEqual(9011523L, result);
    }

    [TestMethod]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0x34, 0x12, 0x99 }, 1234L, DisplayName = "4-digit")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x12, 0x90, 0x78, 0x56, 0x34, 0x12, 0x99 }, 123456789012L, DisplayName = "12-digit")]
    public void ParseValue_Bcd_IgnoresBytesBeyondWidth(DataTypes dataType, byte[] data, long expected)
    {
        var result = ValueParser.ParseValue(dataType, data);
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow(DataTypes._2_digit_BCD, new byte[0], DisplayName = "2-digit")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0x56, 0x34, 0x12 }, DisplayName = "8-digit")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x90, 0x78, 0x56, 0x34, 0x12 }, DisplayName = "12-digit")]
    public void ParseValue_Bcd_InsufficientData_ReturnsNull(DataTypes dataType, byte[] data)
    {
        var result = ValueParser.ParseValue(dataType, data);
        Assert.IsNull(result);
    }

    [TestMethod]
    [DataRow(DataTypes._2_digit_BCD, new byte[] { 0x05 }, DisplayName = "2-digit")]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0x05, 0x00 }, DisplayName = "4-digit")]
    [DataRow(DataTypes._6_digit_BCD, new byte[] { 0x05, 0x00, 0x00 }, DisplayName = "6-digit")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0x05, 0x00, 0x00, 0x00 }, DisplayName = "8-digit")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x05, 0x00, 0x00, 0x00, 0x00, 0x00 }, DisplayName = "12-digit")]
    public void ParseValue_Bcd_IsAlwaysLong(DataTypes dataType, byte[] data)
    {
        var result = ValueParser.ParseValue(dataType, data, out var error);

        Assert.IsInstanceOfType<long>(result);
        Assert.AreEqual(5L, result);
        Assert.IsNull(error);
    }

    // EN 13757-3 Annex A: 0xF in the most significant nibble is a minus sign.
    [TestMethod]
    [DataRow(DataTypes._2_digit_BCD, new byte[] { 0xF5 }, -5L, DisplayName = "2-digit")]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0x18, 0xF0 }, -18L, DisplayName = "4-digit")]
    [DataRow(DataTypes._6_digit_BCD, new byte[] { 0x02, 0x00, 0xF5 }, -50002L, DisplayName = "6-digit")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0x23, 0x01, 0x00, 0xF0 }, -123L, DisplayName = "8-digit")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x18, 0x00, 0x00, 0x00, 0x00, 0xF0 }, -18L, DisplayName = "12-digit")]
    public void ParseValue_Bcd_SignNibble_IsNegative(DataTypes dataType, byte[] data, long expected)
    {
        var result = ValueParser.ParseValue(dataType, data, out var error);

        Assert.AreEqual(expected, result);
        Assert.IsNull(error);
    }

    [TestMethod]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0xAA, 0xAA }, DisplayName = "Error nibbles AAAA")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, DisplayName = "All F")]
    [DataRow(DataTypes._4_digit_BCD, new byte[] { 0xF1, 0x00 }, DisplayName = "F outside the most significant nibble")]
    [DataRow(DataTypes._2_digit_BCD, new byte[] { 0x0F }, DisplayName = "F in the low nibble")]
    [DataRow(DataTypes._8_digit_BCD, new byte[] { 0xDD, 0xB4, 0xEB, 0xDD }, DisplayName = "abb_f95 record 2, value during error state")]
    [DataRow(DataTypes._12_digit_BCD, new byte[] { 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00 }, DisplayName = "12-digit")]
    public void ParseValue_Bcd_NonDecimalDigit_IsInvalidBcd(DataTypes dataType, byte[] data)
    {
        var result = ValueParser.ParseValue(dataType, data, out var error);

        Assert.IsNull(result);
        Assert.AreEqual("INVALID_BCD", error);
    }

    [TestMethod]
    public void ParseValue_VariableLength_ReversesText()
    {
        // siemens_wfh21 record 6 sends "WFH21" last character first
        var result = ValueParser.ParseValue(DataTypes._variable_length, [0x31, 0x32, 0x48, 0x46, 0x57]);

        Assert.AreEqual("WFH21", result);
    }

    [TestMethod]
    public void ParseValue_VariableLength_DoesNotModifyInput()
    {
        byte[] data = [0x31, 0x32];

        ValueParser.ParseValue(DataTypes._variable_length, data);

        CollectionAssert.AreEqual(new byte[] { 0x31, 0x32 }, data);
    }

    [TestMethod]
    public void ParseValue_32BitReal_ReturnsCorrectValue()
    {
        var bytes = BitConverter.GetBytes(3.14f);
        var result = ValueParser.ParseValue(DataTypes._32_Bit_Real, bytes);
        Assert.AreEqual(3.14f, result);
    }

    [TestMethod]
    public void ParseValue_NoData_ReturnsNull()
    {
        var result = ValueParser.ParseValue(DataTypes._No_data, []);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void ParseValue_InsufficientData_ReturnsNull()
    {
        var result = ValueParser.ParseValue(DataTypes._32_Bit_Integer, [0x01]);
        Assert.IsNull(result);
    }
}
