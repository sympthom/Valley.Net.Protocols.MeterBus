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

    // nn in duration VIFs selects s/min/h/d, and n in 0x6C/0x6D selects date or date & time.
    // Neither is a power of ten.
    [TestMethod]
    [DataRow((byte)0x20, VariableDataQuantityUnit.OnTime, "s", DisplayName = "On time seconds")]
    [DataRow((byte)0x22, VariableDataQuantityUnit.OnTime, "h", DisplayName = "On time hours")]
    [DataRow((byte)0x25, VariableDataQuantityUnit.OperatingTime, "min", DisplayName = "Operating time minutes")]
    [DataRow((byte)0x27, VariableDataQuantityUnit.OperatingTime, "d", DisplayName = "Operating time days")]
    [DataRow((byte)0x6C, VariableDataQuantityUnit.TimePoint, "-", DisplayName = "Time point date")]
    [DataRow((byte)0x6D, VariableDataQuantityUnit.TimePoint, "-", DisplayName = "Time point date & time")]
    [DataRow((byte)0xED, VariableDataQuantityUnit.TimePoint, "-", DisplayName = "Time point date & time with extension bit")]
    [DataRow((byte)0x71, VariableDataQuantityUnit.AveragingDuration, "min", DisplayName = "Averaging duration minutes")]
    [DataRow((byte)0x74, VariableDataQuantityUnit.ActualityDuration, "s", DisplayName = "Actuality duration seconds")]
    [DataRow((byte)0x75, VariableDataQuantityUnit.ActualityDuration, "min", DisplayName = "Actuality duration minutes")]
    [DataRow((byte)0x76, VariableDataQuantityUnit.ActualityDuration, "h", DisplayName = "Actuality duration hours")]
    [DataRow((byte)0x77, VariableDataQuantityUnit.ActualityDuration, "d", DisplayName = "Actuality duration days")]
    public void Resolve_DurationOrTimePointVif_HasTimeUnitAndNoMagnitude(byte vifByte, VariableDataQuantityUnit expectedUnits, string expectedUnit)
    {
        var result = _service.Resolve(vifByte);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedUnit, result.Unit);
        Assert.AreEqual(0, result.Magnitude);
    }

    [TestMethod]
    [DataRow((byte)0x05, VariableDataQuantityUnit.ErrorCodesVIFE, 0, DisplayName = "Error code 5")]
    [DataRow((byte)0x95, VariableDataQuantityUnit.ErrorCodesVIFE, 0, DisplayName = "Error code 21 with extension bit")]
    [DataRow((byte)0x3F, VariableDataQuantityUnit.ReservedVIFE_3D, 0, DisplayName = "Reserved 3F")]
    [DataRow((byte)0x48, VariableDataQuantityUnit.LimitValue, 0, DisplayName = "Upper limit value")]
    [DataRow((byte)0x49, VariableDataQuantityUnit.NrOfLimitExceeds, 0, DisplayName = "Number of upper limit exceeds")]
    [DataRow((byte)0x4F, VariableDataQuantityUnit.DateTimeOfLimitExceed, 0, DisplayName = "Date/time of end of last upper limit exceed")]
    [DataRow((byte)0x58, VariableDataQuantityUnit.DurationOfLimitExceed, 0, DisplayName = "Duration of upper limit exceed")]
    [DataRow((byte)0x67, VariableDataQuantityUnit.DurationOfLimitAbove, 0, DisplayName = "Duration of D")]
    [DataRow((byte)0x68, VariableDataQuantityUnit.ReservedVIFE_68, 0, DisplayName = "Reserved 68")]
    [DataRow((byte)0x6D, VariableDataQuantityUnit.ReservedVIFE_68, 0, DisplayName = "Reserved 6D")]
    [DataRow((byte)0x6F, VariableDataQuantityUnit.DateTimeOfLimitAbove, 0, DisplayName = "Date/time of D")]
    [DataRow((byte)0x70, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, -6, DisplayName = "Multiplicative 10^-6")]
    [DataRow((byte)0x77, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, 1, DisplayName = "Multiplicative 10^1")]
    [DataRow((byte)0x7A, VariableDataQuantityUnit.AdditiveCorrectionConstant, -1, DisplayName = "Additive 10^-1")]
    [DataRow((byte)0xFD, VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000, 3, DisplayName = "Multiplicative x1000")]
    public void ResolveExtension_PrimaryTable_ReturnsMagnitude(byte vifeByte, VariableDataQuantityUnit expectedUnits, int expectedMagnitude)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.Primary);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedMagnitude, result.Magnitude);
    }

    [TestMethod]
    public void ResolveExtension_PrimaryTable_OnlyCorrectionFactorsHaveMagnitude()
    {
        for (var b = 0x00; b <= 0x7F; b++)
        {
            var result = _service.ResolveExtension((byte)b, VifExtensionTable.Primary);
            var scales = b is >= 0x70 and <= 0x7B or 0x7D;
            if (!scales)
                Assert.AreEqual(0, result.Magnitude, $"VIFE {b:X2}h ({result.Units})");
        }
    }

    [TestMethod]
    [DataRow((byte)0x58, VariableDataQuantityUnit.FlowTemperature_F, -3, DisplayName = "Flow temperature 10^-3 °F")]
    [DataRow((byte)0xDA, VariableDataQuantityUnit.FlowTemperature_F, -1, DisplayName = "Flow temperature 10^-1 °F with extension bit")]
    [DataRow((byte)0x5B, VariableDataQuantityUnit.FlowTemperature_F, 0, DisplayName = "Flow temperature 10^0 °F")]
    [DataRow((byte)0x5C, VariableDataQuantityUnit.ReturnTemperature_F, -3, DisplayName = "Return temperature 10^-3 °F")]
    [DataRow((byte)0x1A, VariableDataQuantityUnit.RelativeHumidity, -1, DisplayName = "Relative humidity 10^-1 %")]
    [DataRow((byte)0x1B, VariableDataQuantityUnit.RelativeHumidity, 0, DisplayName = "Relative humidity 10^0 %")]
    [DataRow((byte)0x1C, VariableDataQuantityUnit.ReservedVIFE_FB_1a, 0, DisplayName = "Reserved 1C")]
    [DataRow((byte)0x20, VariableDataQuantityUnit.ReservedVIFE_FB_1a, 0, DisplayName = "Reserved 20")]
    [DataRow((byte)0x03, VariableDataQuantityUnit.ReservedVIFE_FB_02, 0, DisplayName = "Reserved 03")]
    [DataRow((byte)0x0F, VariableDataQuantityUnit.ReservedVIFE_FB_0c, 0, DisplayName = "Reserved 0F")]
    [DataRow((byte)0x57, VariableDataQuantityUnit.ReservedVIFE_FB_32, 0, DisplayName = "Reserved 57")]
    [DataRow((byte)0x6F, VariableDataQuantityUnit.ReservedVIFE_FB_68, 0, DisplayName = "Reserved 6F")]
    public void ResolveExtension_FbTable_ReturnsMagnitude(byte vifeByte, VariableDataQuantityUnit expectedUnits, int expectedMagnitude)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.FB);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedMagnitude, result.Magnitude);
    }

    [TestMethod]
    [DataRow((byte)0x0B, VariableDataQuantityUnit.ParameterSetIdentification, DisplayName = "Parameter set identification")]
    [DataRow((byte)0x28, VariableDataQuantityUnit.StorageIntervalMonth, DisplayName = "Storage interval months")]
    [DataRow((byte)0x3B, VariableDataQuantityUnit.Reserved_FD_3b, DisplayName = "Reserved 3B")]
    [DataRow((byte)0x3C, VariableDataQuantityUnit.Reserved_FD_3c, DisplayName = "Reserved 3C")]
    [DataRow((byte)0x5F, VariableDataQuantityUnit.Amperes, DisplayName = "Amperes")]
    public void ResolveExtension_FdTable_ReturnsCorrectUnits(byte vifeByte, VariableDataQuantityUnit expectedUnits)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.FD);
        Assert.AreEqual(expectedUnits, result.Units);
    }

    [TestMethod]
    [DataRow(VifExtensionTable.Primary, (byte)0x50, VariableDataQuantityUnit.DurationOfLimitExceed, "s", DisplayName = "Duration of limit exceed seconds")]
    [DataRow(VifExtensionTable.Primary, (byte)0x5E, VariableDataQuantityUnit.DurationOfLimitExceed, "h", DisplayName = "Duration of limit exceed hours")]
    [DataRow(VifExtensionTable.Primary, (byte)0x61, VariableDataQuantityUnit.DurationOfLimitAbove, "min", DisplayName = "Duration of D minutes")]
    [DataRow(VifExtensionTable.FD, (byte)0x27, VariableDataQuantityUnit.StorageInterval, "d", DisplayName = "Storage interval days")]
    [DataRow(VifExtensionTable.FD, (byte)0x2C, VariableDataQuantityUnit.DurationSinceLastReadout, "s", DisplayName = "Duration since last readout seconds")]
    [DataRow(VifExtensionTable.FD, (byte)0x31, VariableDataQuantityUnit.DurationOfTariff, "min", DisplayName = "Duration of tariff minutes")]
    [DataRow(VifExtensionTable.FD, (byte)0x36, VariableDataQuantityUnit.PeriodOfTariff, "h", DisplayName = "Period of tariff hours")]
    [DataRow(VifExtensionTable.FD, (byte)0x68, VariableDataQuantityUnit.DurationSinceLastCumulation, "h", DisplayName = "Duration since last cumulation hours")]
    [DataRow(VifExtensionTable.FD, (byte)0x6B, VariableDataQuantityUnit.DurationSinceLastCumulation, "year", DisplayName = "Duration since last cumulation years")]
    [DataRow(VifExtensionTable.FD, (byte)0x6E, VariableDataQuantityUnit.OperatingTimeBattery, "month", DisplayName = "Operating time battery months")]
    public void ResolveExtension_DurationVife_HasTimeUnitAndNoMagnitude(VifExtensionTable table, byte vifeByte, VariableDataQuantityUnit expectedUnits, string expectedUnit)
    {
        var result = _service.ResolveExtension(vifeByte, table);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedUnit, result.Unit);
        Assert.AreEqual(0, result.Magnitude);
    }

    [TestMethod]
    [DataRow(VifExtensionTable.Primary)]
    [DataRow(VifExtensionTable.FB)]
    [DataRow(VifExtensionTable.FD)]
    public void ResolveExtension_EveryCode_HasPlausibleMagnitude(VifExtensionTable table)
    {
        for (var b = 0x00; b <= 0x7F; b++)
        {
            var result = _service.ResolveExtension((byte)b, table);
            Assert.IsTrue(result.Magnitude is >= -12 and <= 9, $"{table} VIFE {b:X2}h ({result.Units}) has magnitude {result.Magnitude}");
        }
    }

    // The raw code stays available for error numbers and limit qualifiers, which carry no magnitude.
    [TestMethod]
    [DataRow((byte)0x85, "05h", DisplayName = "Error code 5")]
    [DataRow((byte)0x4E, "4Eh", DisplayName = "Date/time of begin of last upper limit exceed")]
    [DataRow((byte)0x44, "44h", DisplayName = "Undefined code")]
    public void ResolveExtension_ReturnsRawCodeAsVifString(byte vifeByte, string expectedVifString)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.Primary);
        Assert.AreEqual(expectedVifString, result.VifString);
    }

    // SEN_Pollustat record 13: volume flow with a duration-of-limit-exceed VIFE. libmbus reports exponent 0.
    [TestMethod]
    [DataRow("04 BE 58 F4 02 00 00", 0, DisplayName = "Volume flow + duration of limit exceed")]
    [DataRow("04 83 05 10 00 00 00", 0, DisplayName = "Energy Wh + error code 5")]
    [DataRow("04 ED 00 0C 0C BD 16", 0, DisplayName = "Time point date & time + error code 0")]
    [DataRow("02 FB 58 10 00", -3, DisplayName = "Flow temperature 10^-3 °F")]
    [DataRow("04 93 75 10 00 00 00", -4, DisplayName = "Volume 10^-3 m^3 + multiplicative 10^-1")]
    public void MapToPacket_RecordMagnitude_SumsOnlyScalingCodes(string recordHex, int expectedMagnitude)
    {
        var frame = new FrameParser().Parse(BuildRspUd(recordHex.HexToBytes()));
        Assert.IsTrue(frame.IsSuccess, $"Frame parse failed: {frame.Error?.Message}");

        var packet = new PacketMapper(_service).MapToPacket(frame.Value!);
        Assert.IsTrue(packet.IsSuccess, $"Packet map failed: {packet.Error?.Message}");

        var record = ((VariableDataPacket)packet.Value!).Records.Single();
        Assert.AreEqual(expectedMagnitude, record.Magnitude);
    }

    // RSP_UD long frame, CI 72h, with a zeroed 12-byte fixed header.
    private static byte[] BuildRspUd(byte[] records)
    {
        byte[] body = [0x08, 0x01, 0x72, .. new byte[12], .. records];
        var checksum = (byte)body.Sum(b => b);
        return [0x68, (byte)body.Length, (byte)body.Length, 0x68, .. body, checksum, 0x16];
    }
}
