namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class VifLookupServiceTests
{
    private readonly VifLookupService _service = new();

    [TestMethod]
    [DataRow((byte)0x13, VariableDataQuantityUnit.Volume_m3, "m^3", DisplayName = "Volume m^3")]
    [DataRow((byte)0x58, VariableDataQuantityUnit.FlowTemperatureC, "°C", DisplayName = "Flow temperature")]
    [DataRow((byte)0x6C, VariableDataQuantityUnit.TimePoint, "-", DisplayName = "Time point date")]
    [DataRow((byte)0x78, VariableDataQuantityUnit.FabricationNo, "", DisplayName = "Fabrication No")]
    public void Resolve_KnownVif_ReturnsCorrectUnits(byte vifByte, VariableDataQuantityUnit expectedUnits, string expectedUnit)
    {
        var result = _service.Resolve(vifByte);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedUnit, result.Unit);
    }

    [TestMethod]
    public void Resolve_WithExtensionBit_SetsHasExtension()
    {
        var result = _service.Resolve(0x93); // 0x13 | 0x80
        Assert.IsTrue(result.HasExtension);
        Assert.AreEqual(VariableDataQuantityUnit.Volume_m3, result.Units);
    }

    [TestMethod]
    public void ResolveExtension_VifeTable_ReturnsCorrectUnits()
    {
        var result = _service.ResolveExtension(0x20, VifExtensionTable.Primary);
        Assert.AreEqual(VariableDataQuantityUnit.Per_second, result.Units);
    }
}
