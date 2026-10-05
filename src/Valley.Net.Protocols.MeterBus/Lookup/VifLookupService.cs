using System.Collections.Frozen;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// VIF type classification for primary VIF byte resolution.
/// </summary>
public enum VifType
{
    PrimaryVIF,
    PlainTextVIF,
    LinearVIFExtensionFD,
    LinearVIFExtensionFB,
    AnyVIF,
    ManufacturerSpecific,
}

/// <summary>
/// Resolved VIF information.
/// </summary>
public sealed record VifInfo(
    VariableDataQuantityUnit Units,
    string? Unit,
    string? Quantity,
    int Magnitude,
    bool HasExtension,
    VifType Type,
    string? VifString);

/// <summary>
/// Consolidated VIF/VIFE lookup service. Replaces the four separate VIF, VIFE, VIFE_FB, VIFE_FD classes.
/// </summary>
/// <remarks>
/// The tables implement EN 13757-3:2013 as far as it can be confirmed from the M-Bus documentation
/// rev 4.8 (MBDOC48), libmbus, jMBus 3.x and OMS Vol. 2 Issue 5.0.1 with Annex B (2023-12).
/// Codes none of these define stay <see cref="VariableDataQuantityUnit.Reserved"/> with magnitude 0.
/// </remarks>
public sealed class VifLookupService
{
    internal sealed record VifTableEntry(
        byte Key,
        VariableDataQuantityUnit Units,
        string? Unit,
        string? Quantity,
        VifType Type,
        Func<byte, int> MagnitudeFunc);

    internal sealed record VifeTableEntry(
        byte Key,
        VariableDataQuantityUnit Units,
        Func<byte, int> MagnitudeFunc,
        string? Unit = null);

    // In duration codes the low two bits select the time unit (EN 13757-3), they are not a power of ten.
    // nn is s/min/h/d; pp (FD 0x68-0x6F) is h/d/month/year.
    private static readonly string[] TimeUnits = ["s", "min", "h", "d"];
    private static readonly string[] LongTimeUnits = ["h", "d", "month", "year"];

    private static string TimeUnit(byte b) => TimeUnits[b & 0x03];

    private static string LongTimeUnit(byte b) => LongTimeUnits[b & 0x03];

    // ---- Primary VIF table ----
    private static readonly VifTableEntry[] VifTable = BuildVifTable();
    private static readonly FrozenDictionary<byte, VifTableEntry> VifLookup =
        VifTable.ToFrozenDictionary(x => x.Key);

    // ---- VIFE table (primary extensions) ----
    private static readonly VifeTableEntry[] VifeTableArray = BuildVifeTable();
    private static readonly FrozenDictionary<byte, VifeTableEntry> VifeLookup =
        VifeTableArray.ToFrozenDictionary(x => x.Key);

    // ---- VIFE_FB table ----
    private static readonly VifeTableEntry[] VifeFbTableArray = BuildVifeFbTable();
    private static readonly FrozenDictionary<byte, VifeTableEntry> VifeFbLookup =
        VifeFbTableArray.ToFrozenDictionary(x => x.Key);

    // ---- VIFE_FD table ----
    private static readonly VifeTableEntry[] VifeFdTableArray = BuildVifeFdTable();
    private static readonly FrozenDictionary<byte, VifeTableEntry> VifeFdLookup =
        VifeFdTableArray.ToFrozenDictionary(x => x.Key);

    // ---- VIFE_FC table (combinable extension) ----
    private static readonly VifeTableEntry[] VifeFcTableArray = BuildVifeFcTable();
    private static readonly FrozenDictionary<byte, VifeTableEntry> VifeFcLookup =
        VifeFcTableArray.ToFrozenDictionary(x => x.Key);

    /// <summary>
    /// Resolve a primary VIF byte.
    /// </summary>
    public VifInfo Resolve(byte vifByte)
    {
        var masked = (byte)(vifByte & 0x7F);
        var hasExtension = (vifByte & 0x80) != 0;

        if (!VifLookup.TryGetValue(masked, out var entry))
            return new VifInfo(VariableDataQuantityUnit.Undefined, null, null, 0, hasExtension, VifType.PrimaryVIF, $"{masked:X2}h");

        return new VifInfo(
            entry.Units,
            entry.Unit,
            entry.Quantity,
            entry.MagnitudeFunc(vifByte),
            hasExtension,
            entry.Type,
            $"{entry.Key:X2}h");
    }

    /// <summary>
    /// Resolve a VIFE byte from the specified extension table.
    /// </summary>
    public VifInfo ResolveExtension(byte vifeByte, VifExtensionTable table)
    {
        var masked = (byte)(vifeByte & 0x7F);
        var hasExtension = (vifeByte & 0x80) != 0;

        var lookup = table switch
        {
            VifExtensionTable.Primary => VifeLookup,
            VifExtensionTable.FB => VifeFbLookup,
            VifExtensionTable.FD => VifeFdLookup,
            VifExtensionTable.FC => VifeFcLookup,
            _ => VifeLookup,
        };

        // VifString carries the raw code, so error numbers and limit/date qualifiers, which have no
        // magnitude, stay readable.
        if (!lookup.TryGetValue(masked, out var entry))
            return new VifInfo(VariableDataQuantityUnit.Undefined, null, null, 0, hasExtension, VifType.PrimaryVIF, $"{masked:X2}h");

        return new VifInfo(
            entry.Units,
            entry.Unit,
            null,
            entry.MagnitudeFunc(masked),
            hasExtension,
            VifType.PrimaryVIF,
            $"{entry.Key:X2}h");
    }

    // ========== Table builders ==========

    private static VifTableEntry[] BuildVifTable()
    {
        var list = new List<VifTableEntry>();

        // E000 0nnn Energy Wh
        for (byte i = 0x00; i <= 0x07; i++)
            list.Add(new(i, VariableDataQuantityUnit.EnergyWh, "Wh", "Energy", VifType.PrimaryVIF, b => (b & 0x07) - 3));

        // E000 1nnn Energy J
        for (byte i = 0x08; i <= 0x0F; i++)
            list.Add(new(i, VariableDataQuantityUnit.EnergyJ, "J", "Energy", VifType.PrimaryVIF, b => (b & 0x07)));

        // E001 0nnn Volume m^3
        for (byte i = 0x10; i <= 0x17; i++)
            list.Add(new(i, VariableDataQuantityUnit.Volume_m3, "m^3", "Volume", VifType.PrimaryVIF, b => (b & 0x07) - 6));

        // E001 1nnn Mass kg
        for (byte i = 0x18; i <= 0x1F; i++)
            list.Add(new(i, VariableDataQuantityUnit.Mass_kg, "kg", "Mass", VifType.PrimaryVIF, b => (b & 0x07) - 3));

        // E010 00nn On Time
        for (byte i = 0x20; i <= 0x23; i++)
            list.Add(new(i, VariableDataQuantityUnit.OnTime, TimeUnit(i), "On time", VifType.PrimaryVIF, _ => 0));

        // E010 01nn Operating Time
        for (byte i = 0x24; i <= 0x27; i++)
            list.Add(new(i, VariableDataQuantityUnit.OperatingTime, TimeUnit(i), "Operating time", VifType.PrimaryVIF, _ => 0));

        // E010 1nnn Power W
        for (byte i = 0x28; i <= 0x2F; i++)
            list.Add(new(i, VariableDataQuantityUnit.PowerW, "W", "Power", VifType.PrimaryVIF, b => (b & 0x07) - 3));

        // E011 0nnn Power J/h
        for (byte i = 0x30; i <= 0x37; i++)
            list.Add(new(i, VariableDataQuantityUnit.PowerJ_per_h, "J/h", "Power", VifType.PrimaryVIF, b => (b & 0x07)));

        // E011 1nnn Volume flow m^3/h
        for (byte i = 0x38; i <= 0x3F; i++)
            list.Add(new(i, VariableDataQuantityUnit.VolumeFlowM3_per_h, "m^3/h", "Volume flow", VifType.PrimaryVIF, b => (b & 0x07) - 6));

        // E100 0nnn Volume flow ext m^3/min
        for (byte i = 0x40; i <= 0x47; i++)
            list.Add(new(i, VariableDataQuantityUnit.VolumeFlowExtM3_per_min, "m^3/min", "Volume flow", VifType.PrimaryVIF, b => (b & 0x07) - 7));

        // E100 1nnn Volume flow ext m^3/s
        for (byte i = 0x48; i <= 0x4F; i++)
            list.Add(new(i, VariableDataQuantityUnit.VolumeFlowExtM3_per_s, "m^3/s", "Volume flow", VifType.PrimaryVIF, b => (b & 0x07) - 9));

        // E101 0nnn Mass flow kg/h
        for (byte i = 0x50; i <= 0x57; i++)
            list.Add(new(i, VariableDataQuantityUnit.MassFlowKg_per_h, "kg/h", "Mass flow", VifType.PrimaryVIF, b => (b & 0x07) - 3));

        // E101 10nn Flow temperature C
        for (byte i = 0x58; i <= 0x5B; i++)
            list.Add(new(i, VariableDataQuantityUnit.FlowTemperatureC, "°C", "Flow temperature", VifType.PrimaryVIF, b => (b & 0x03) - 3));

        // E101 11nn Return temperature C
        for (byte i = 0x5C; i <= 0x5F; i++)
            list.Add(new(i, VariableDataQuantityUnit.ReturnTemperatureC, "°C", "Return temperature", VifType.PrimaryVIF, b => (b & 0x03) - 3));

        // E110 00nn Temperature difference K
        for (byte i = 0x60; i <= 0x63; i++)
            list.Add(new(i, VariableDataQuantityUnit.TemperatureDifferenceK, "K", "Temperature difference", VifType.PrimaryVIF, b => (b & 0x03) - 3));

        // E110 01nn External temperature C
        for (byte i = 0x64; i <= 0x67; i++)
            list.Add(new(i, VariableDataQuantityUnit.ExternalTemperatureC, "°C", "External temperature", VifType.PrimaryVIF, b => (b & 0x03) - 3));

        // E110 10nn Pressure bar
        for (byte i = 0x68; i <= 0x6B; i++)
            list.Add(new(i, VariableDataQuantityUnit.PressureBar, "bar", "Pressure", VifType.PrimaryVIF, b => (b & 0x03) - 3));

        // E110 110n Time point, n selects date (type G) or date & time (type F), not a scale
        list.Add(new(0x6C, VariableDataQuantityUnit.TimePoint, "-", "Time point (date)", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x6D, VariableDataQuantityUnit.TimePoint, "-", "Time point (date & time)", VifType.PrimaryVIF, _ => 0));

        // HCA
        list.Add(new(0x6E, VariableDataQuantityUnit.UnitsForHCA, "Units for H.C.A.", "H.C.A.", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x6F, VariableDataQuantityUnit.Reserved, null, "Reserved", VifType.PrimaryVIF, _ => 0));

        // E111 00nn Averaging Duration
        for (byte i = 0x70; i <= 0x73; i++)
            list.Add(new(i, VariableDataQuantityUnit.AveragingDuration, TimeUnit(i), "Averaging Duration", VifType.PrimaryVIF, _ => 0));

        // E111 01nn Actuality Duration
        for (byte i = 0x74; i <= 0x77; i++)
            list.Add(new(i, VariableDataQuantityUnit.ActualityDuration, TimeUnit(i), "Actuality Duration", VifType.PrimaryVIF, _ => 0));

        // Special VIFs
        list.Add(new(0x78, VariableDataQuantityUnit.FabricationNo, "", "Fabrication No", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x79, VariableDataQuantityUnit.EnhancedIdentification, "", "(Enhanced) Identification", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x7A, VariableDataQuantityUnit.BusAddress, "", "Bus Address", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x7B, VariableDataQuantityUnit.Extension_7B, "", "Extension 7b", VifType.LinearVIFExtensionFB, _ => 0));
        list.Add(new(0x7C, VariableDataQuantityUnit.CustomVIF, "", "Custom VIF", VifType.PlainTextVIF, _ => 0));
        list.Add(new(0x7D, VariableDataQuantityUnit.Extension_7D, "", "Extension 7d", VifType.LinearVIFExtensionFD, _ => 0));
        list.Add(new(0x7E, VariableDataQuantityUnit.AnyVIF, "", "Any VIF", VifType.AnyVIF, _ => 0));
        list.Add(new(0x7F, VariableDataQuantityUnit.ManufacturerSpecific, "", "Manufacturer specific", VifType.ManufacturerSpecific, _ => 0));

        return [.. list];
    }

    private static VifeTableEntry[] BuildVifeTable()
    {
        var list = new List<VifeTableEntry>();

        // Only 0x70-0x77, 0x7D (scale) and 0x78-0x7B (offset) are powers of ten. Every other
        // combinable VIFE is a code or qualifier and contributes magnitude 0 (EN 13757-3 Table 15).

        // E00x xxxx Record error codes. EN 13757-3:2018 reuses the reserved error numbers 0x13 and 0x1D-0x1F
        // as data qualifiers: OMS Vol. 2 Annex N names 0x13 "inverse compact profile" and 0x1F "compact profile
        // without registers", Annex B lists 0x13/0x1E/0x1F as the compact profile codes (so 0x1E is the one
        // with registers) and codes "error flags (standard)" as FD 97 1D.
        for (byte i = 0x00; i <= 0x1F; i++)
        {
            var units = i switch
            {
                0x13 => VariableDataQuantityUnit.InverseCompactProfile,
                0x1D => VariableDataQuantityUnit.StandardConformDataContent,
                0x1E => VariableDataQuantityUnit.CompactProfileWithRegisters,
                0x1F => VariableDataQuantityUnit.CompactProfileWithoutRegisters,
                _ => VariableDataQuantityUnit.ErrorCodesVIFE,
            };
            list.Add(new(i, units, _ => 0));
        }

        // 0x20-0x3c: Per unit / multiplier
        list.Add(new(0x20, VariableDataQuantityUnit.Per_second, _ => 0));
        list.Add(new(0x21, VariableDataQuantityUnit.Per_minute, _ => 0));
        list.Add(new(0x22, VariableDataQuantityUnit.Per_hour, _ => 0));
        list.Add(new(0x23, VariableDataQuantityUnit.Per_day, _ => 0));
        list.Add(new(0x24, VariableDataQuantityUnit.Per_week, _ => 0));
        list.Add(new(0x25, VariableDataQuantityUnit.Per_month, _ => 0));
        list.Add(new(0x26, VariableDataQuantityUnit.Per_year, _ => 0));
        list.Add(new(0x27, VariableDataQuantityUnit.Per_RevolutionMeasurement, _ => 0));
        list.Add(new(0x28, VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel0, _ => 0));
        list.Add(new(0x29, VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel1, _ => 0));
        list.Add(new(0x2A, VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel0, _ => 0));
        list.Add(new(0x2B, VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel1, _ => 0));
        list.Add(new(0x2C, VariableDataQuantityUnit.Per_liter, _ => 0));
        list.Add(new(0x2D, VariableDataQuantityUnit.Per_m3, _ => 0));
        list.Add(new(0x2E, VariableDataQuantityUnit.Per_kg, _ => 0));
        list.Add(new(0x2F, VariableDataQuantityUnit.Per_Kelvin, _ => 0));
        list.Add(new(0x30, VariableDataQuantityUnit.Per_kWh, _ => 0));
        list.Add(new(0x31, VariableDataQuantityUnit.Per_GJ, _ => 0));
        list.Add(new(0x32, VariableDataQuantityUnit.Per_kW, _ => 0));
        list.Add(new(0x33, VariableDataQuantityUnit.Per_KelvinLiter, _ => 0));
        list.Add(new(0x34, VariableDataQuantityUnit.Per_Volt, _ => 0));
        list.Add(new(0x35, VariableDataQuantityUnit.Per_Ampere, _ => 0));
        list.Add(new(0x36, VariableDataQuantityUnit.MultipliedBySecond, _ => 0));
        list.Add(new(0x37, VariableDataQuantityUnit.MultipliedBySecond_per_V, _ => 0));
        list.Add(new(0x38, VariableDataQuantityUnit.MultipliedBySecond_per_A, _ => 0));
        list.Add(new(0x39, VariableDataQuantityUnit.StartDateTimeOf, _ => 0));
        list.Add(new(0x3A, VariableDataQuantityUnit.UncorrectedUnit, _ => 0));
        list.Add(new(0x3B, VariableDataQuantityUnit.AccumulationPositive, _ => 0));
        list.Add(new(0x3C, VariableDataQuantityUnit.AccumulationNegative, _ => 0));

        // 0x3D (alternate non-metric unit system in MBDOC48) and 0x3F are reserved.
        // E011 1110 Value at base conditions (OMS Vol. 2 Annex B: PR01, TC03, VM05)
        list.Add(new(0x3D, VariableDataQuantityUnit.Reserved, _ => 0));
        list.Add(new(0x3E, VariableDataQuantityUnit.ValueAtBaseConditions, _ => 0));
        list.Add(new(0x3F, VariableDataQuantityUnit.Reserved, _ => 0));

        // E100 u000 Lower/upper limit value, E100 u001 number of exceeds
        list.Add(new(0x40, VariableDataQuantityUnit.LimitValue, _ => 0));
        list.Add(new(0x48, VariableDataQuantityUnit.LimitValue, _ => 0));
        list.Add(new(0x41, VariableDataQuantityUnit.NrOfLimitExceeds, _ => 0));
        list.Add(new(0x49, VariableDataQuantityUnit.NrOfLimitExceeds, _ => 0));

        // E100 uf1b Date/time of begin/end of first/last lower/upper limit exceed
        foreach (byte b in new byte[] { 0x42, 0x43, 0x46, 0x47, 0x4A, 0x4B, 0x4E, 0x4F })
            list.Add(new(b, VariableDataQuantityUnit.DateTimeOfLimitExceed, _ => 0));

        // E101 ufnn Duration of limit exceed
        for (byte i = 0x50; i <= 0x5F; i++)
            list.Add(new(i, VariableDataQuantityUnit.DurationOfLimitExceed, _ => 0, TimeUnit(i)));

        // E110 0fnn Duration of D
        for (byte i = 0x60; i <= 0x67; i++)
            list.Add(new(i, VariableDataQuantityUnit.DurationOfLimitAbove, _ => 0, TimeUnit(i)));

        // E110 1x0x Reserved (MBDOC48). EN 13757-3:2013 is said to define 0x68/0x6C as "value during
        // limit exceed", but no available source confirms it, so they stay reserved.
        foreach (byte b in new byte[] { 0x68, 0x69, 0x6C, 0x6D })
            list.Add(new(b, VariableDataQuantityUnit.Reserved, _ => 0));

        // E110 1f1b Date/time of D
        foreach (byte b in new byte[] { 0x6A, 0x6B, 0x6E, 0x6F })
            list.Add(new(b, VariableDataQuantityUnit.DateTimeOfLimitAbove, _ => 0));

        // Multiplicative correction
        for (byte i = 0x70; i <= 0x77; i++)
            list.Add(new(i, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6));

        // Additive correction
        for (byte i = 0x78; i <= 0x7B; i++)
            list.Add(new(i, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3));

        // E111 1100 Extension of combinable VIFE codes: the next VIFE comes from the FC table
        // (OMS Vol. 2 Annex B: CA01, PD01, EW07 code it as FCh followed by the qualifier)
        list.Add(new(0x7C, VariableDataQuantityUnit.CombinableExtension, _ => 0));
        list.Add(new(0x7D, VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000, _ => 3));
        // E111 1110 Future value (MBDOC48; OMS Vol. 2 Annex B DT05 "future date")
        list.Add(new(0x7E, VariableDataQuantityUnit.FutureValue, _ => 0));
        list.Add(new(0x7F, VariableDataQuantityUnit.ManufacturerSpecific, _ => 0));

        return [.. list];
    }

    // EN 13757-3:2013 Table 14 (true VIF after FBh). Rows MBDOC48 already had are unchanged; the newer
    // rows cite jMBus 3.x DataRecord.decodeAlternateExtendedVif, OMS Vol. 2 Annex B and the Carlo Gavazzi
    // GNM3D protocol document in DataExamples/Docs.
    private static VifeTableEntry[] BuildVifeFbTable()
    {
        var list = new List<VifeTableEntry>();

        // E000 000n Energy 10^(n-1) MWh
        for (byte i = 0x00; i <= 0x01; i++) list.Add(new(i, VariableDataQuantityUnit.EnergyMWh, b => (b & 0x01) - 1));
        // E000 001n Reactive energy 10^n kVARh (jMBus, OMS RE01, GNM3D "FBh 82h 75h")
        for (byte i = 0x02; i <= 0x03; i++) list.Add(new(i, VariableDataQuantityUnit.ReactiveEnergy_kVARh, b => b & 0x01));
        // E000 010n Apparent energy 10^n kVAh (jMBus)
        for (byte i = 0x04; i <= 0x05; i++) list.Add(new(i, VariableDataQuantityUnit.ApparentEnergy_kVAh, b => b & 0x01));
        for (byte i = 0x06; i <= 0x07; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        // E000 100n Energy 10^(n-1) GJ
        for (byte i = 0x08; i <= 0x09; i++) list.Add(new(i, VariableDataQuantityUnit.EnergyGJ, b => (b & 0x01) - 1));
        for (byte i = 0x0A; i <= 0x0B; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        // E000 11nn Energy 10^(nn-1) MCal (jMBus, OMS EC01)
        for (byte i = 0x0C; i <= 0x0F; i++) list.Add(new(i, VariableDataQuantityUnit.EnergyMCal, b => (b & 0x03) - 1));
        // E001 000n Volume 10^(n+2) m^3
        for (byte i = 0x10; i <= 0x11; i++) list.Add(new(i, VariableDataQuantityUnit.Volume_m3, b => (b & 0x01) + 2));
        for (byte i = 0x12; i <= 0x13; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        // E001 01nn Reactive power 10^(nn-3) kVAR (jMBus, OMS RP01, GNM3D "FBh 97h 72h")
        for (byte i = 0x14; i <= 0x17; i++) list.Add(new(i, VariableDataQuantityUnit.ReactivePower_kVAR, b => (b & 0x03) - 3));
        // E001 100n Mass 10^(n+2) t
        for (byte i = 0x18; i <= 0x19; i++) list.Add(new(i, VariableDataQuantityUnit.Mass_t, b => (b & 0x01) + 2));
        // E001 101n Relative humidity 10^(n-1) % (jMBus, OMS RH01)
        for (byte i = 0x1A; i <= 0x1B; i++) list.Add(new(i, VariableDataQuantityUnit.RelativeHumidity, b => (b & 0x01) - 1));
        for (byte i = 0x1C; i <= 0x1F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        // E010 0000 Volume feet^3 (jMBus), E010 0001 Volume 0.1 feet^3
        list.Add(new(0x20, VariableDataQuantityUnit.Volume_feet3, _ => 0));
        list.Add(new(0x21, VariableDataQuantityUnit.Volume_feet3, _ => -1));
        // E010 001n Volume 10^(n-1) american gallon
        for (byte i = 0x22; i <= 0x23; i++) list.Add(new(i, VariableDataQuantityUnit.Volume_american_gallon, b => (b & 0x01) - 1));
        list.Add(new(0x24, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, _ => -3));
        list.Add(new(0x25, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, _ => 0));
        list.Add(new(0x26, VariableDataQuantityUnit.Volume_flow_american_gallon_per_h, _ => 0));
        list.Add(new(0x27, VariableDataQuantityUnit.Reserved, _ => 0));
        // E010 100n Power 10^(n-1) MW
        for (byte i = 0x28; i <= 0x29; i++) list.Add(new(i, VariableDataQuantityUnit.Power_MW, b => (b & 0x01) - 1));
        // E010 1010 Phase U-U, E010 1011 Phase U-I, both 0.1 degree (jMBus, OMS PD01-PD06)
        list.Add(new(0x2A, VariableDataQuantityUnit.PhaseVoltageToVoltage, _ => -1));
        list.Add(new(0x2B, VariableDataQuantityUnit.PhaseVoltageToCurrent, _ => -1));
        // E010 11nn Frequency 10^(nn-3) Hz (jMBus, OMS FR01, GNM3D "FBh 2Eh")
        for (byte i = 0x2C; i <= 0x2F; i++) list.Add(new(i, VariableDataQuantityUnit.Frequency_Hz, b => (b & 0x03) - 3));
        // E011 000n Power 10^(n-1) GJ/h
        for (byte i = 0x30; i <= 0x31; i++) list.Add(new(i, VariableDataQuantityUnit.Power_GJ_per_h, b => (b & 0x01) - 1));
        for (byte i = 0x32; i <= 0x33; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        // E011 01nn Apparent power 10^(nn-3) kVA (jMBus, GNM3D "FBh B7h 72h")
        for (byte i = 0x34; i <= 0x37; i++) list.Add(new(i, VariableDataQuantityUnit.ApparentPower_kVA, b => (b & 0x03) - 3));
        for (byte i = 0x38; i <= 0x57; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        for (byte i = 0x58; i <= 0x5B; i++) list.Add(new(i, VariableDataQuantityUnit.FlowTemperature_F, b => (b & 0x03) - 3));
        for (byte i = 0x5C; i <= 0x5F; i++) list.Add(new(i, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3));
        for (byte i = 0x60; i <= 0x63; i++) list.Add(new(i, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3));
        for (byte i = 0x64; i <= 0x67; i++) list.Add(new(i, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3));
        for (byte i = 0x68; i <= 0x6F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        for (byte i = 0x70; i <= 0x73; i++) list.Add(new(i, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3));
        for (byte i = 0x74; i <= 0x77; i++) list.Add(new(i, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3));
        for (byte i = 0x78; i <= 0x7F; i++) list.Add(new(i, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3));

        return [.. list];
    }

    // EN 13757-3:2013 Table 13 (true VIF after FDh). Codes MBDOC48 lists as reserved but jMBus 3.x
    // (decodeMainExtendedVif) or OMS Vol. 2 Annex B/N define are cited on their rows.
    private static VifeTableEntry[] BuildVifeFdTable()
    {
        var list = new List<VifeTableEntry>();

        for (byte i = 0x00; i <= 0x03; i++) list.Add(new(i, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3));
        for (byte i = 0x04; i <= 0x07; i++) list.Add(new(i, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3));
        list.Add(new(0x08, VariableDataQuantityUnit.AccessNumber, _ => 0));
        list.Add(new(0x09, VariableDataQuantityUnit.Medium, _ => 0));
        list.Add(new(0x0A, VariableDataQuantityUnit.Manufacturer, _ => 0));
        list.Add(new(0x0B, VariableDataQuantityUnit.ParameterSetIdentification, _ => 0));
        list.Add(new(0x0C, VariableDataQuantityUnit.ModelVersion, _ => 0));
        list.Add(new(0x0D, VariableDataQuantityUnit.HardwareVersionNr, _ => 0));
        list.Add(new(0x0E, VariableDataQuantityUnit.FirmwareVersionNr, _ => 0));
        list.Add(new(0x0F, VariableDataQuantityUnit.SoftwareVersionNr, _ => 0));
        list.Add(new(0x10, VariableDataQuantityUnit.CustomerLocation, _ => 0));
        list.Add(new(0x11, VariableDataQuantityUnit.Customer, _ => 0));
        list.Add(new(0x12, VariableDataQuantityUnit.AccessCodeUser, _ => 0));
        list.Add(new(0x13, VariableDataQuantityUnit.AccessCodeOperator, _ => 0));
        list.Add(new(0x14, VariableDataQuantityUnit.AccessCodeSystemOperator, _ => 0));
        list.Add(new(0x15, VariableDataQuantityUnit.AccessCodeDeveloper, _ => 0));
        list.Add(new(0x16, VariableDataQuantityUnit.Password, _ => 0));
        list.Add(new(0x17, VariableDataQuantityUnit.ErrorFlags, _ => 0));
        list.Add(new(0x18, VariableDataQuantityUnit.ErrorMask, _ => 0));
        // E001 1001 Security key (jMBus)
        list.Add(new(0x19, VariableDataQuantityUnit.SecurityKey, _ => 0));
        list.Add(new(0x1A, VariableDataQuantityUnit.DigitalOutput, _ => 0));
        list.Add(new(0x1B, VariableDataQuantityUnit.DigitalInput, _ => 0));
        list.Add(new(0x1C, VariableDataQuantityUnit.Baudrate, _ => 0));
        list.Add(new(0x1D, VariableDataQuantityUnit.ResponseDelayTime, _ => 0));
        list.Add(new(0x1E, VariableDataQuantityUnit.Retry, _ => 0));
        // E001 1111 Remote control, device specific (jMBus, OMS Annex B CL01, Annex N)
        list.Add(new(0x1F, VariableDataQuantityUnit.RemoteControl, _ => 0));
        list.Add(new(0x20, VariableDataQuantityUnit.FirstStorageNr, _ => 0));
        list.Add(new(0x21, VariableDataQuantityUnit.LastStorageNr, _ => 0));
        list.Add(new(0x22, VariableDataQuantityUnit.SizeOfStorage, _ => 0));
        // E010 0011 Descriptor for tariff and subunit (OMS Annex B YD01)
        list.Add(new(0x23, VariableDataQuantityUnit.TariffAndSubunitDescriptor, _ => 0));
        // Storage interval and period of tariff are one quantity each; the unit runs from s to year
        for (byte i = 0x24; i <= 0x27; i++) list.Add(new(i, VariableDataQuantityUnit.StorageInterval, _ => 0, TimeUnit(i)));
        list.Add(new(0x28, VariableDataQuantityUnit.StorageInterval, _ => 0, "month"));
        list.Add(new(0x29, VariableDataQuantityUnit.StorageInterval, _ => 0, "year"));
        // E010 1010 Operator specific data (jMBus, OMS MM04), E010 1011 Time point second 0-59 (jMBus)
        list.Add(new(0x2A, VariableDataQuantityUnit.OperatorSpecificData, _ => 0));
        list.Add(new(0x2B, VariableDataQuantityUnit.TimePointSecond, _ => 0, "s"));
        for (byte i = 0x2C; i <= 0x2F; i++) list.Add(new(i, VariableDataQuantityUnit.DurationSinceLastReadout, _ => 0, TimeUnit(i)));
        list.Add(new(0x30, VariableDataQuantityUnit.StartDateTimeOfTariff, _ => 0));
        for (byte i = 0x31; i <= 0x33; i++) list.Add(new(i, VariableDataQuantityUnit.DurationOfTariff, _ => 0, TimeUnit(i)));
        for (byte i = 0x34; i <= 0x37; i++) list.Add(new(i, VariableDataQuantityUnit.PeriodOfTariff, _ => 0, TimeUnit(i)));
        list.Add(new(0x38, VariableDataQuantityUnit.PeriodOfTariff, _ => 0, "month"));
        list.Add(new(0x39, VariableDataQuantityUnit.PeriodOfTariff, _ => 0, "year"));
        list.Add(new(0x3A, VariableDataQuantityUnit.Dimensionless, _ => 0));
        list.Add(new(0x3B, VariableDataQuantityUnit.Reserved, _ => 0));
        // E011 110n Period of nominal data transmission, n = s or min (OMS Annex B DP03)
        for (byte i = 0x3C; i <= 0x3D; i++) list.Add(new(i, VariableDataQuantityUnit.PeriodOfNominalTransmission, _ => 0, TimeUnit(i)));
        for (byte i = 0x3E; i <= 0x3F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));
        for (byte i = 0x40; i <= 0x4F; i++) list.Add(new(i, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9));
        for (byte i = 0x50; i <= 0x5F; i++) list.Add(new(i, VariableDataQuantityUnit.Amperes, b => (b & 0x0f) - 12));
        list.Add(new(0x60, VariableDataQuantityUnit.ResetCounter, _ => 0));
        list.Add(new(0x61, VariableDataQuantityUnit.CumulationCounter, _ => 0));
        list.Add(new(0x62, VariableDataQuantityUnit.ControlSignal, _ => 0));
        list.Add(new(0x63, VariableDataQuantityUnit.DayOfWeek, _ => 0));
        list.Add(new(0x64, VariableDataQuantityUnit.WeekNumber, _ => 0));
        list.Add(new(0x65, VariableDataQuantityUnit.TimePointOfDayChange, _ => 0));
        list.Add(new(0x66, VariableDataQuantityUnit.StateOfParameterActivation, _ => 0));
        list.Add(new(0x67, VariableDataQuantityUnit.SpecialSupplierInformation, _ => 0));
        for (byte i = 0x68; i <= 0x6B; i++) list.Add(new(i, VariableDataQuantityUnit.DurationSinceLastCumulation, _ => 0, LongTimeUnit(i)));
        for (byte i = 0x6C; i <= 0x6F; i++) list.Add(new(i, VariableDataQuantityUnit.OperatingTimeBattery, _ => 0, LongTimeUnit(i)));
        list.Add(new(0x70, VariableDataQuantityUnit.DateTimeOfBatteryChange, _ => 0));
        // E111 0001-E111 0110 from jMBus; 0x71 and 0x74 also OMS Annex B MM01/MM09
        list.Add(new(0x71, VariableDataQuantityUnit.RfLevel_dBm, _ => 0));
        list.Add(new(0x72, VariableDataQuantityUnit.DaylightSaving, _ => 0));
        list.Add(new(0x73, VariableDataQuantityUnit.ListeningWindowManagement, _ => 0));
        list.Add(new(0x74, VariableDataQuantityUnit.RemainingBatteryLifetime, _ => 0, "d"));
        list.Add(new(0x75, VariableDataQuantityUnit.NumberOfMeterStops, _ => 0));
        list.Add(new(0x76, VariableDataQuantityUnit.ManufacturerSpecificDataContainer, _ => 0));
        for (byte i = 0x77; i <= 0x7F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved, _ => 0));

        return [.. list];
    }

    // Combinable VIFE extension table (VIFE after FCh). Only the qualifiers OMS Vol. 2 Annex B codes
    // are filled in (CA01-CA04, VV01-VV03, PD01-PD06, PW09, EW07, HC02); the rest stay reserved.
    private static VifeTableEntry[] BuildVifeFcTable()
    {
        var list = new List<VifeTableEntry>();

        for (byte i = 0x00; i <= 0x7F; i++)
        {
            var units = i switch
            {
                0x01 => VariableDataQuantityUnit.AtPhaseL1,
                0x02 => VariableDataQuantityUnit.AtPhaseL2,
                0x03 => VariableDataQuantityUnit.AtPhaseL3,
                0x04 => VariableDataQuantityUnit.AtNeutral,
                0x05 => VariableDataQuantityUnit.BetweenPhasesL1L2,
                0x06 => VariableDataQuantityUnit.BetweenPhasesL2L3,
                0x07 => VariableDataQuantityUnit.BetweenPhasesL3L1,
                0x0C => VariableDataQuantityUnit.DeltaImportExport,
                0x10 => VariableDataQuantityUnit.AccumulationAbsolute,
                0x11 => VariableDataQuantityUnit.UnsignedValue,
                _ => VariableDataQuantityUnit.Reserved,
            };
            list.Add(new(i, units, _ => 0));
        }

        return [.. list];
    }
}
