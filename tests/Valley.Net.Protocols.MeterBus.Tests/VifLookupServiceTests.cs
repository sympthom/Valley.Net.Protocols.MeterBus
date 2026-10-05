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
    [DataRow((byte)0x1C, VariableDataQuantityUnit.ErrorCodesVIFE, 0, DisplayName = "Error code 28")]
    [DataRow((byte)0x12, VariableDataQuantityUnit.ErrorCodesVIFE, 0, DisplayName = "Error code 18")]
    [DataRow((byte)0x13, VariableDataQuantityUnit.InverseCompactProfile, 0, DisplayName = "Inverse compact profile (OMS Annex N.13 83 13)")]
    [DataRow((byte)0x1D, VariableDataQuantityUnit.StandardConformDataContent, 0, DisplayName = "Standard conform data content (OMS MM03 FD 97 1D)")]
    [DataRow((byte)0x1E, VariableDataQuantityUnit.CompactProfileWithRegisters, 0, DisplayName = "Compact profile with registers (OMS Annex B)")]
    [DataRow((byte)0x1F, VariableDataQuantityUnit.CompactProfileWithoutRegisters, 0, DisplayName = "Compact profile without registers (OMS Annex N 93 1F)")]
    [DataRow((byte)0x3D, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 3D")]
    [DataRow((byte)0x3E, VariableDataQuantityUnit.ValueAtBaseConditions, 0, DisplayName = "Value at base conditions")]
    [DataRow((byte)0x3F, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 3F")]
    [DataRow((byte)0x48, VariableDataQuantityUnit.LimitValue, 0, DisplayName = "Upper limit value")]
    [DataRow((byte)0x49, VariableDataQuantityUnit.NrOfLimitExceeds, 0, DisplayName = "Number of upper limit exceeds")]
    [DataRow((byte)0x4F, VariableDataQuantityUnit.DateTimeOfLimitExceed, 0, DisplayName = "Date/time of end of last upper limit exceed")]
    [DataRow((byte)0x58, VariableDataQuantityUnit.DurationOfLimitExceed, 0, DisplayName = "Duration of upper limit exceed")]
    [DataRow((byte)0x67, VariableDataQuantityUnit.DurationOfLimitAbove, 0, DisplayName = "Duration of D")]
    [DataRow((byte)0x68, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 68")]
    [DataRow((byte)0x6D, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 6D")]
    [DataRow((byte)0x6F, VariableDataQuantityUnit.DateTimeOfLimitAbove, 0, DisplayName = "Date/time of D")]
    [DataRow((byte)0x70, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, -6, DisplayName = "Multiplicative 10^-6")]
    [DataRow((byte)0x77, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, 1, DisplayName = "Multiplicative 10^1")]
    [DataRow((byte)0x7A, VariableDataQuantityUnit.AdditiveCorrectionConstant, -1, DisplayName = "Additive 10^-1")]
    [DataRow((byte)0xFC, VariableDataQuantityUnit.CombinableExtension, 0, DisplayName = "Combinable extension")]
    [DataRow((byte)0xFD, VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000, 3, DisplayName = "Multiplicative x1000")]
    [DataRow((byte)0x7E, VariableDataQuantityUnit.FutureValue, 0, DisplayName = "Future value")]
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
    [DataRow((byte)0x01, VariableDataQuantityUnit.EnergyMWh, 0, DisplayName = "Energy 10^0 MWh")]
    [DataRow((byte)0x02, VariableDataQuantityUnit.ReactiveEnergy_kVARh, 0, DisplayName = "Reactive energy 10^0 kVARh (EN 13757-3:2013, jMBus, OMS RE01)")]
    [DataRow((byte)0x03, VariableDataQuantityUnit.ReactiveEnergy_kVARh, 1, DisplayName = "Reactive energy 10^1 kVARh")]
    [DataRow((byte)0x04, VariableDataQuantityUnit.ApparentEnergy_kVAh, 0, DisplayName = "Apparent energy 10^0 kVAh (jMBus)")]
    [DataRow((byte)0x05, VariableDataQuantityUnit.ApparentEnergy_kVAh, 1, DisplayName = "Apparent energy 10^1 kVAh")]
    [DataRow((byte)0x0C, VariableDataQuantityUnit.EnergyMCal, -1, DisplayName = "Energy 10^-1 MCal (jMBus, OMS EC01)")]
    [DataRow((byte)0x0F, VariableDataQuantityUnit.EnergyMCal, 2, DisplayName = "Energy 10^2 MCal")]
    [DataRow((byte)0x14, VariableDataQuantityUnit.ReactivePower_kVAR, -3, DisplayName = "Reactive power 10^-3 kVAR (jMBus, OMS RP01)")]
    [DataRow((byte)0x17, VariableDataQuantityUnit.ReactivePower_kVAR, 0, DisplayName = "Reactive power 10^0 kVAR")]
    [DataRow((byte)0x20, VariableDataQuantityUnit.Volume_feet3, 0, DisplayName = "Volume feet^3 (jMBus)")]
    [DataRow((byte)0x21, VariableDataQuantityUnit.Volume_feet3, -1, DisplayName = "Volume 0.1 feet^3")]
    [DataRow((byte)0x22, VariableDataQuantityUnit.Volume_american_gallon, -1, DisplayName = "Volume 0.1 american gallon")]
    [DataRow((byte)0x23, VariableDataQuantityUnit.Volume_american_gallon, 0, DisplayName = "Volume 1 american gallon")]
    [DataRow((byte)0x2A, VariableDataQuantityUnit.PhaseVoltageToVoltage, -1, DisplayName = "Phase U-U 0.1 degree (jMBus, OMS PD01)")]
    [DataRow((byte)0x2B, VariableDataQuantityUnit.PhaseVoltageToCurrent, -1, DisplayName = "Phase U-I 0.1 degree (OMS PD04)")]
    [DataRow((byte)0x2C, VariableDataQuantityUnit.Frequency_Hz, -3, DisplayName = "Frequency 10^-3 Hz (jMBus, OMS FR01)")]
    [DataRow((byte)0x2E, VariableDataQuantityUnit.Frequency_Hz, -1, DisplayName = "Frequency 10^-1 Hz (GNM3D)")]
    [DataRow((byte)0x34, VariableDataQuantityUnit.ApparentPower_kVA, -3, DisplayName = "Apparent power 10^-3 kVA (jMBus)")]
    [DataRow((byte)0xB7, VariableDataQuantityUnit.ApparentPower_kVA, 0, DisplayName = "Apparent power 10^0 kVA with extension bit (GNM3D)")]
    [DataRow((byte)0x06, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 06")]
    [DataRow((byte)0x0B, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 0B")]
    [DataRow((byte)0x13, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 13")]
    [DataRow((byte)0x1C, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 1C")]
    [DataRow((byte)0x1F, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 1F")]
    [DataRow((byte)0x27, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 27")]
    [DataRow((byte)0x32, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 32")]
    [DataRow((byte)0x38, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 38")]
    [DataRow((byte)0x57, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 57")]
    [DataRow((byte)0x6F, VariableDataQuantityUnit.Reserved, 0, DisplayName = "Reserved 6F")]
    [DataRow((byte)0x7F, VariableDataQuantityUnit.CumulCountMaxPower_W, 4, DisplayName = "Cumulative count max power 10^4 W")]
    public void ResolveExtension_FbTable_ReturnsMagnitude(byte vifeByte, VariableDataQuantityUnit expectedUnits, int expectedMagnitude)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.FB);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(expectedMagnitude, result.Magnitude);
    }

    [TestMethod]
    [DataRow((byte)0x0B, VariableDataQuantityUnit.ParameterSetIdentification, DisplayName = "Parameter set identification")]
    [DataRow((byte)0x19, VariableDataQuantityUnit.SecurityKey, DisplayName = "Security key (jMBus)")]
    [DataRow((byte)0x1F, VariableDataQuantityUnit.RemoteControl, DisplayName = "Remote control (jMBus, OMS CL01)")]
    [DataRow((byte)0x23, VariableDataQuantityUnit.TariffAndSubunitDescriptor, DisplayName = "Descriptor tariff and subunit (OMS YD01)")]
    [DataRow((byte)0x2A, VariableDataQuantityUnit.OperatorSpecificData, DisplayName = "Operator specific data (jMBus, OMS MM04)")]
    [DataRow((byte)0x3A, VariableDataQuantityUnit.Dimensionless, DisplayName = "Dimensionless")]
    [DataRow((byte)0x3B, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 3B")]
    [DataRow((byte)0x3E, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 3E")]
    [DataRow((byte)0x5F, VariableDataQuantityUnit.Amperes, DisplayName = "Amperes")]
    [DataRow((byte)0x70, VariableDataQuantityUnit.DateTimeOfBatteryChange, DisplayName = "Date and time of battery change")]
    [DataRow((byte)0x71, VariableDataQuantityUnit.RfLevel_dBm, DisplayName = "RF level dBm (jMBus, OMS MM01)")]
    [DataRow((byte)0x72, VariableDataQuantityUnit.DaylightSaving, DisplayName = "Daylight saving (jMBus)")]
    [DataRow((byte)0x73, VariableDataQuantityUnit.ListeningWindowManagement, DisplayName = "Listening window management (jMBus)")]
    [DataRow((byte)0x75, VariableDataQuantityUnit.NumberOfMeterStops, DisplayName = "Number of times the meter was stopped (jMBus)")]
    [DataRow((byte)0x76, VariableDataQuantityUnit.ManufacturerSpecificDataContainer, DisplayName = "Manufacturer specific data container (jMBus)")]
    [DataRow((byte)0x77, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 77")]
    [DataRow((byte)0xFF, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 7F with extension bit")]
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
    [DataRow(VifExtensionTable.FD, (byte)0x28, VariableDataQuantityUnit.StorageInterval, "month", DisplayName = "Storage interval months")]
    [DataRow(VifExtensionTable.FD, (byte)0x29, VariableDataQuantityUnit.StorageInterval, "year", DisplayName = "Storage interval years")]
    [DataRow(VifExtensionTable.FD, (byte)0x2B, VariableDataQuantityUnit.TimePointSecond, "s", DisplayName = "Time point second (jMBus)")]
    [DataRow(VifExtensionTable.FD, (byte)0x2C, VariableDataQuantityUnit.DurationSinceLastReadout, "s", DisplayName = "Duration since last readout seconds")]
    [DataRow(VifExtensionTable.FD, (byte)0x31, VariableDataQuantityUnit.DurationOfTariff, "min", DisplayName = "Duration of tariff minutes")]
    [DataRow(VifExtensionTable.FD, (byte)0x36, VariableDataQuantityUnit.PeriodOfTariff, "h", DisplayName = "Period of tariff hours")]
    [DataRow(VifExtensionTable.FD, (byte)0x38, VariableDataQuantityUnit.PeriodOfTariff, "month", DisplayName = "Period of tariff months")]
    [DataRow(VifExtensionTable.FD, (byte)0x39, VariableDataQuantityUnit.PeriodOfTariff, "year", DisplayName = "Period of tariff years")]
    [DataRow(VifExtensionTable.FD, (byte)0x3C, VariableDataQuantityUnit.PeriodOfNominalTransmission, "s", DisplayName = "Period of nominal transmission seconds (OMS DP03)")]
    [DataRow(VifExtensionTable.FD, (byte)0x3D, VariableDataQuantityUnit.PeriodOfNominalTransmission, "min", DisplayName = "Period of nominal transmission minutes")]
    [DataRow(VifExtensionTable.FD, (byte)0x68, VariableDataQuantityUnit.DurationSinceLastCumulation, "h", DisplayName = "Duration since last cumulation hours")]
    [DataRow(VifExtensionTable.FD, (byte)0x6B, VariableDataQuantityUnit.DurationSinceLastCumulation, "year", DisplayName = "Duration since last cumulation years")]
    [DataRow(VifExtensionTable.FD, (byte)0x6E, VariableDataQuantityUnit.OperatingTimeBattery, "month", DisplayName = "Operating time battery months")]
    [DataRow(VifExtensionTable.FD, (byte)0x74, VariableDataQuantityUnit.RemainingBatteryLifetime, "d", DisplayName = "Remaining battery lifetime days (jMBus, OMS MM09)")]
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
    [DataRow(VifExtensionTable.FC)]
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

    // Reserved codes must never scale a value or claim a unit.
    [TestMethod]
    [DataRow(VifExtensionTable.Primary)]
    [DataRow(VifExtensionTable.FB)]
    [DataRow(VifExtensionTable.FD)]
    [DataRow(VifExtensionTable.FC)]
    public void ResolveExtension_ReservedCode_HasNoMagnitudeOrUnit(VifExtensionTable table)
    {
        for (var b = 0x00; b <= 0x7F; b++)
        {
            var result = _service.ResolveExtension((byte)b, table);
            if (result.Units != VariableDataQuantityUnit.Reserved)
                continue;
            Assert.AreEqual(0, result.Magnitude, $"{table} VIFE {b:X2}h");
            Assert.IsNull(result.Unit, $"{table} VIFE {b:X2}h");
            Assert.AreEqual($"{b:X2}h", result.VifString);
        }
    }

    [TestMethod]
    public void Resolve_ReservedVif_HasNoMagnitudeOrUnit()
    {
        for (var b = 0x00; b <= 0x7F; b++)
        {
            var result = _service.Resolve((byte)b);
            if (result.Units != VariableDataQuantityUnit.Reserved)
                continue;
            Assert.AreEqual(0, result.Magnitude, $"VIF {b:X2}h");
            Assert.IsNull(result.Unit, $"VIF {b:X2}h");
        }
        Assert.AreEqual(VariableDataQuantityUnit.Reserved, _service.Resolve(0x6F).Units);
    }

    private static readonly HashSet<VariableDataQuantityUnit> DurationQuantities =
    [
        VariableDataQuantityUnit.OnTime,
        VariableDataQuantityUnit.OperatingTime,
        VariableDataQuantityUnit.AveragingDuration,
        VariableDataQuantityUnit.ActualityDuration,
        VariableDataQuantityUnit.DurationOfLimitExceed,
        VariableDataQuantityUnit.DurationOfLimitAbove,
        VariableDataQuantityUnit.StorageInterval,
        VariableDataQuantityUnit.DurationSinceLastReadout,
        VariableDataQuantityUnit.DurationOfTariff,
        VariableDataQuantityUnit.PeriodOfTariff,
        VariableDataQuantityUnit.PeriodOfNominalTransmission,
        VariableDataQuantityUnit.DurationSinceLastCumulation,
        VariableDataQuantityUnit.OperatingTimeBattery,
        VariableDataQuantityUnit.RemainingBatteryLifetime,
        VariableDataQuantityUnit.TimePointSecond,
    ];

    // Every duration code carries its time unit in Unit and never a power of ten (findings 39, 63).
    [TestMethod]
    public void EveryDurationCode_HasTimeUnitAndNoMagnitude()
    {
        string[] timeUnits = ["s", "min", "h", "d", "month", "year"];
        var resolved = Enumerable.Range(0x00, 0x80)
            .Select(b => ("VIF", b, _service.Resolve((byte)b)))
            .Concat(new[] { VifExtensionTable.Primary, VifExtensionTable.FB, VifExtensionTable.FD, VifExtensionTable.FC }
                .SelectMany(table => Enumerable.Range(0x00, 0x80)
                    .Select(b => (table.ToString(), b, _service.ResolveExtension((byte)b, table)))));

        var durations = 0;
        foreach (var (table, code, info) in resolved)
        {
            if (!DurationQuantities.Contains(info.Units))
                continue;
            durations++;
            CollectionAssert.Contains(timeUnits, info.Unit, $"{table} {code:X2}h ({info.Units})");
            Assert.AreEqual(0, info.Magnitude, $"{table} {code:X2}h ({info.Units})");
        }

        // VIF 0x20-0x27, 0x70-0x77; VIFE 0x50-0x67; FD 0x24-0x29, 0x2B-0x2F, 0x31-0x39, 0x3C-0x3D, 0x68-0x6F, 0x74
        Assert.AreEqual(16 + 24 + 6 + 5 + 9 + 2 + 8 + 1, durations);
    }

    [TestMethod]
    [DataRow((byte)0x01, VariableDataQuantityUnit.AtPhaseL1, DisplayName = "At phase L1 (OMS CA01)")]
    [DataRow((byte)0x82, VariableDataQuantityUnit.AtPhaseL2, DisplayName = "At phase L2 with extension bit")]
    [DataRow((byte)0x03, VariableDataQuantityUnit.AtPhaseL3, DisplayName = "At phase L3")]
    [DataRow((byte)0x04, VariableDataQuantityUnit.AtNeutral, DisplayName = "At neutral (OMS CA04)")]
    [DataRow((byte)0x05, VariableDataQuantityUnit.BetweenPhasesL1L2, DisplayName = "Between L1 and L2 (OMS PD01)")]
    [DataRow((byte)0x06, VariableDataQuantityUnit.BetweenPhasesL2L3, DisplayName = "Between L2 and L3")]
    [DataRow((byte)0x07, VariableDataQuantityUnit.BetweenPhasesL3L1, DisplayName = "Between L3 and L1")]
    [DataRow((byte)0x0C, VariableDataQuantityUnit.DeltaImportExport, DisplayName = "Delta import/export (OMS PW09)")]
    [DataRow((byte)0x10, VariableDataQuantityUnit.AccumulationAbsolute, DisplayName = "Absolute accumulation (OMS EW07)")]
    [DataRow((byte)0x11, VariableDataQuantityUnit.UnsignedValue, DisplayName = "Unsigned value (OMS HC02)")]
    [DataRow((byte)0x00, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 00")]
    [DataRow((byte)0x08, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 08 (quadrant codes unconfirmed)")]
    [DataRow((byte)0x7F, VariableDataQuantityUnit.Reserved, DisplayName = "Reserved 7F")]
    public void ResolveExtension_FcTable_ReturnsQualifier(byte vifeByte, VariableDataQuantityUnit expectedUnits)
    {
        var result = _service.ResolveExtension(vifeByte, VifExtensionTable.FC);
        Assert.AreEqual(expectedUnits, result.Units);
        Assert.AreEqual(0, result.Magnitude);
    }

    // Electricity codes from the Carlo Gavazzi GNM3D protocol document (DataExamples/Docs, Table 2).
    [TestMethod]
    [DataRow("04 FB 2E 10 00 00 00", VariableDataQuantityUnit.Frequency_Hz, -1, DisplayName = "Hz * 0.1: FB 2E")]
    [DataRow("04 FB 82 75 10 00 00 00", VariableDataQuantityUnit.ReactiveEnergy_kVARh, -1, DisplayName = "kvarh * 0.1: FB 82 75")]
    [DataRow("04 FB 97 72 10 00 00 00", VariableDataQuantityUnit.ReactivePower_kVAR, -4, DisplayName = "kvar * 0.0001: FB 97 72")]
    [DataRow("04 FB B7 72 10 00 00 00", VariableDataQuantityUnit.ApparentPower_kVA, -4, DisplayName = "kVA * 0.0001: FB B7 72")]
    [DataRow("04 FD BA 73 10 00 00 00", VariableDataQuantityUnit.Dimensionless, -3, DisplayName = "PF * 0.001: FD BA 73")]
    public void MapToPacket_Gnm3dElectricityCode_DecodesQuantityAndMagnitude(string recordHex, VariableDataQuantityUnit expectedUnits, int expectedMagnitude)
    {
        var frame = new FrameParser().Parse(BuildRspUd(recordHex.HexToBytes()));
        Assert.IsTrue(frame.IsSuccess, $"Frame parse failed: {frame.Error?.Message}");

        var packet = new PacketMapper(_service).MapToPacket(frame.Value!);
        Assert.IsTrue(packet.IsSuccess, $"Packet map failed: {packet.Error?.Message}");

        var record = ((VariableDataPacket)packet.Value!).Records.Single();
        Assert.AreEqual(expectedUnits, record.Units[1].Units);
        Assert.AreEqual(expectedMagnitude, record.Magnitude);
    }

    // Names per EN 13757-7:2018 Table 13 / OMS Vol. 2 Tables 2-4 (findings 70, 134, 187).
    [TestMethod]
    [DataRow((byte)0x04, "Heat")]
    [DataRow((byte)0x06, "WarmWater")]
    [DataRow((byte)0x08, "HeatCostAllocator")]
    [DataRow((byte)0x0A, "CoolingLoadMeterOutlet")]
    [DataRow((byte)0x0B, "CoolingLoadMeterInlet")]
    [DataRow((byte)0x0C, "HeatInlet")]
    [DataRow((byte)0x0D, "HeatCoolingLoadMeter")]
    [DataRow((byte)0x0E, "BusSystemComponent")]
    [DataRow((byte)0x10, "IrrigationWater")]
    [DataRow((byte)0x13, "GasConverter")]
    [DataRow((byte)0x15, "BoilingWater")]
    [DataRow((byte)0x17, "DualRegisterWater")]
    [DataRow((byte)0x1D, "CarbonMonoxideAlarm")]
    [DataRow((byte)0x21, "Valve")]
    [DataRow((byte)0x25, "CustomerUnit")]
    [DataRow((byte)0x28, "WasteWater")]
    [DataRow((byte)0x29, "Garbage")]
    [DataRow((byte)0x31, "CommunicationController")]
    [DataRow((byte)0x32, "UnidirectionalRepeater")]
    [DataRow((byte)0x33, "BidirectionalRepeater")]
    [DataRow((byte)0x36, "RadioConverterSystemSide")]
    [DataRow((byte)0x37, "RadioConverterMeterSide")]
    [DataRow((byte)0x38, "WiredAdapter")]
    public void DeviceType_MediumCode_HasStandardName(byte code, string expectedName)
    {
        Assert.AreEqual(expectedName, ((DeviceType)code).ToString());
    }

    // ToString is only well defined when no two members share a value.
    [TestMethod]
    public void DeviceType_HasNoDuplicateValues()
    {
        var values = Enum.GetValues<DeviceType>();
        Assert.AreEqual(values.Length, values.Distinct().Count());
    }

    [TestMethod]
    [DataRow((byte)0x22, DisplayName = "Reserved for switching devices")]
    [DataRow((byte)0x30, DisplayName = "Reserved for system devices")]
    [DataRow((byte)0x3F, DisplayName = "Reserved for system devices")]
    public void DeviceType_ReservedCode_IsUndefinedButKeepsValue(byte code)
    {
        var deviceType = (DeviceType)code;
        Assert.IsFalse(Enum.IsDefined(deviceType));
        Assert.AreEqual(code, (byte)deviceType);
    }

    // libmbus <Medium> text in DataExamples/test-frames/*.xml -> expected member. The XMLs predate the
    // current libmbus table, which is why 0x06 still reads "Hot water" there.
    private static readonly Dictionary<string, DeviceType> LibmbusMedium = new()
    {
        ["Other"] = DeviceType.Other,
        ["Electricity"] = DeviceType.Electricity,
        ["Gas"] = DeviceType.Gas,
        ["Heat: Outlet"] = DeviceType.Heat,
        ["Hot water"] = DeviceType.WarmWater,
        ["Water"] = DeviceType.Water,
        ["Heat Cost Allocator"] = DeviceType.HeatCostAllocator,
        ["Heat: Inlet"] = DeviceType.HeatInlet,
        ["Heat / Cooling load meter"] = DeviceType.HeatCoolingLoadMeter,
        ["Bus/System"] = DeviceType.BusSystemComponent,
        ["Cold water"] = DeviceType.ColdWater,
    };

    [TestMethod]
    public void MapToPacket_CorpusVariableFrames_DeviceTypeMatchesLibmbusMedium()
    {
        var dir = FindTestFramesDir();
        var parser = new FrameParser();
        var mapper = new PacketMapper(_service);
        var checkedFrames = new List<string>();

        foreach (var xmlPath in Directory.GetFiles(dir, "*.xml").Order(StringComparer.Ordinal))
        {
            var medium = System.Text.RegularExpressions.Regex.Match(File.ReadAllText(xmlPath), "<Medium>([^<]*)</Medium>");
            var hexPath = Path.ChangeExtension(xmlPath, ".hex");
            if (!medium.Success || !File.Exists(hexPath))
                continue;

            var text = File.ReadAllText(hexPath);
            var frame = parser.Parse(Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c)))));
            if (!frame.IsSuccess)
                continue;
            // Fixed-structure frames (CI 73h) use a different medium table; only the variable header is checked here.
            if (mapper.MapToPacket(frame.Value!) is not { IsSuccess: true, Value: VariableDataPacket packet })
                continue;

            var name = Path.GetFileNameWithoutExtension(xmlPath);
            Assert.IsTrue(LibmbusMedium.TryGetValue(medium.Groups[1].Value, out var expected), $"{name}: unmapped medium '{medium.Groups[1].Value}'");
            Assert.AreEqual(expected, packet.DeviceType, name);
            checkedFrames.Add(name);
        }

        // itron_cf_51 and SEN_Pollustat are heat/cooling meters, frame1 a bus/system component.
        CollectionAssert.IsSubsetOf(new[] { "itron_cf_51", "SEN_Pollustat", "frame1" }, checkedFrames);
    }

    private static string FindTestFramesDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "DataExamples", "test-frames");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException($"DataExamples/test-frames not found above {AppContext.BaseDirectory}");
    }

    // RSP_UD long frame, CI 72h, with a zeroed 12-byte fixed header.
    private static byte[] BuildRspUd(byte[] records)
    {
        byte[] body = [0x08, 0x01, 0x72, .. new byte[12], .. records];
        var checksum = (byte)body.Sum(b => b);
        return [0x68, (byte)body.Length, (byte)body.Length, 0x68, .. body, checksum, 0x16];
    }
}
