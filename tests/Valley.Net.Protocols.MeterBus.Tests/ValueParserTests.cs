namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class ValueParserTests
{
    [TestMethod]
    public void ParseValue_8BitInteger_ReturnsCorrectValue()
    {
        var result = ValueParser.ParseValue(DataTypes._8_Bit_Integer, [0x42]);
        Assert.AreEqual((byte)0x42, result);
    }

    [TestMethod]
    public void ParseValue_16BitInteger_ReturnsCorrectValue()
    {
        var result = ValueParser.ParseValue(DataTypes._16_Bit_Integer, [0x01, 0x00]);
        Assert.AreEqual((short)1, result);
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
