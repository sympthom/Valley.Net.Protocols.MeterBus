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
        Func<byte, int> MagnitudeFunc);

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
            _ => VifeLookup,
        };

        if (!lookup.TryGetValue(masked, out var entry))
            return new VifInfo(VariableDataQuantityUnit.Undefined, null, null, 0, hasExtension, VifType.PrimaryVIF, null);

        return new VifInfo(
            entry.Units,
            null,
            null,
            entry.MagnitudeFunc(masked),
            hasExtension,
            VifType.PrimaryVIF,
            null);
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
            list.Add(new(i, VariableDataQuantityUnit.OnTime, "s", "On time", VifType.PrimaryVIF, b => (b & 0x03)));

        // E010 01nn Operating Time
        for (byte i = 0x24; i <= 0x27; i++)
            list.Add(new(i, VariableDataQuantityUnit.OperatingTime, "s", "Operating time", VifType.PrimaryVIF, b => (b & 0x03)));

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

        // Time point
        list.Add(new(0x6C, VariableDataQuantityUnit.TimePoint, "-", "Time point (date)", VifType.PrimaryVIF, b => b & 0x01));
        list.Add(new(0x6D, VariableDataQuantityUnit.TimePoint, "-", "Time point (date & time)", VifType.PrimaryVIF, b => b & 0x01));

        // HCA
        list.Add(new(0x6E, VariableDataQuantityUnit.UnitsForHCA, "Units for H.C.A.", "H.C.A.", VifType.PrimaryVIF, _ => 0));
        list.Add(new(0x6F, VariableDataQuantityUnit.Reserved, "Reserved", "Reserved", VifType.PrimaryVIF, _ => 0));

        // Averaging Duration
        for (byte i = 0x70; i <= 0x73; i++)
            list.Add(new(i, VariableDataQuantityUnit.AveragingDuration, "s", "Averaging Duration", VifType.PrimaryVIF, b => (b & 0x03)));

        // Actuality Duration
        for (byte i = 0x74; i <= 0x77; i++)
            list.Add(new(i, VariableDataQuantityUnit.AveragingDuration, "s", "Actuality Duration", VifType.PrimaryVIF, b => (b & 0x03)));

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

        // 0x00-0x1f: Error codes
        for (byte i = 0x00; i <= 0x1F; i++)
            list.Add(new(i, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f));

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
        list.Add(new(0x36, VariableDataQuantityUnit.MultipliedBySek, _ => 0));
        list.Add(new(0x37, VariableDataQuantityUnit.MultipliedBySek_per_V, _ => 0));
        list.Add(new(0x38, VariableDataQuantityUnit.MultipliedBySek_per_A, _ => 0));
        list.Add(new(0x39, VariableDataQuantityUnit.StartDateTimeOf, _ => 0));
        list.Add(new(0x3A, VariableDataQuantityUnit.UncorrectedUnit, _ => 0));
        list.Add(new(0x3B, VariableDataQuantityUnit.AccumulationPositive, _ => 0));
        list.Add(new(0x3C, VariableDataQuantityUnit.AccumulationNegative, _ => 0));

        // Reserved
        for (byte i = 0x3D; i <= 0x3F; i++)
            list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_3D, b => b - 0x3d));

        // Limit
        list.Add(new(0x40, VariableDataQuantityUnit.LimitValue, b => (b & 0x08) >> 3));
        list.Add(new(0x48, VariableDataQuantityUnit.LimitValue, b => (b & 0x08) >> 3));
        list.Add(new(0x41, VariableDataQuantityUnit.NrOfLimitExceeds, b => (b & 0x08) >> 3));
        list.Add(new(0x49, VariableDataQuantityUnit.NrOfLimitExceeds, b => (b & 0x08) >> 3));

        // Date/time of limit exceed
        foreach (byte b in new byte[] { 0x42, 0x43, 0x46, 0x47, 0x4A, 0x4B, 0x4E, 0x4F })
            list.Add(new(b, VariableDataQuantityUnit.DateTimeOfLimitExceed, x => x & 0x0d));

        // Duration of limit exceed
        for (byte i = 0x50; i <= 0x5F; i++)
            list.Add(new(i, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f));

        // Duration of limit above
        for (byte i = 0x60; i <= 0x67; i++)
            list.Add(new(i, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07));

        // Reserved 68
        foreach (byte b in new byte[] { 0x68, 0x69, 0x6C, 0x6D })
            list.Add(new(b, VariableDataQuantityUnit.ReservedVIFE_68, x => x - 0x05));

        // Date/time of limit above
        foreach (byte b in new byte[] { 0x6A, 0x6B, 0x6E, 0x6F })
            list.Add(new(b, VariableDataQuantityUnit.DateTimeOfLimitAbove, x => x & 0x05));

        // Multiplicative correction
        for (byte i = 0x70; i <= 0x77; i++)
            list.Add(new(i, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6));

        // Additive correction
        for (byte i = 0x78; i <= 0x7B; i++)
            list.Add(new(i, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3));

        list.Add(new(0x7C, VariableDataQuantityUnit.ReservedVIFE_7C, _ => 0));
        list.Add(new(0x7D, VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000, _ => 3));
        list.Add(new(0x7E, VariableDataQuantityUnit.ReservedVIFE_7E, _ => 0));
        list.Add(new(0x7F, VariableDataQuantityUnit.ManufacturerSpecific, _ => 0));

        return [.. list];
    }

    private static VifeTableEntry[] BuildVifeFbTable()
    {
        var list = new List<VifeTableEntry>();

        list.Add(new(0x00, VariableDataQuantityUnit.EnergyMWh, b => (b & 0x01) - 1));
        list.Add(new(0x01, VariableDataQuantityUnit.EnergyMWh, b => (b & 0x01) - 1));
        for (byte i = 0x02; i <= 0x03; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_02, b => b & 0x01));
        for (byte i = 0x04; i <= 0x07; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_04, b => b & 0x01));
        list.Add(new(0x08, VariableDataQuantityUnit.EnergyGJ, b => (b & 0x01) - 1));
        list.Add(new(0x09, VariableDataQuantityUnit.EnergyGJ, b => (b & 0x01) - 1));
        for (byte i = 0x0A; i <= 0x0B; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_0a, b => b & 0x01));
        for (byte i = 0x0C; i <= 0x0F; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_0c, b => b & 0x03));
        list.Add(new(0x10, VariableDataQuantityUnit.Volume_m3, b => (b & 0x01) + 2));
        list.Add(new(0x11, VariableDataQuantityUnit.Volume_m3, b => (b & 0x01) + 2));
        for (byte i = 0x12; i <= 0x13; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_12, b => b & 0x01));
        for (byte i = 0x14; i <= 0x17; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_14, b => b & 0x03));
        list.Add(new(0x18, VariableDataQuantityUnit.Mass_t, b => (b & 0x01) + 2));
        list.Add(new(0x19, VariableDataQuantityUnit.Mass_t, b => (b & 0x01) + 2));
        for (byte i = 0x1A; i <= 0x20; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => b - 0x1a));
        list.Add(new(0x21, VariableDataQuantityUnit.Volume_feet3, _ => -1));
        list.Add(new(0x22, VariableDataQuantityUnit.Volume_american_gallon, b => b - 0x23));
        list.Add(new(0x23, VariableDataQuantityUnit.Volume_american_gallon, b => b - 0x23));
        list.Add(new(0x24, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, _ => -3));
        list.Add(new(0x25, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, _ => 0));
        list.Add(new(0x26, VariableDataQuantityUnit.Volume_flow_american_gallon_per_h, _ => 0));
        list.Add(new(0x27, VariableDataQuantityUnit.ReservedVIFE_FB_27, _ => 0));
        list.Add(new(0x28, VariableDataQuantityUnit.Power_MW, b => (b & 0x01) - 1));
        list.Add(new(0x29, VariableDataQuantityUnit.Power_MW, b => (b & 0x01) - 1));
        for (byte i = 0x2A; i <= 0x2B; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_2a, b => b & 0x01));
        for (byte i = 0x2C; i <= 0x2F; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_2c, b => b & 0x03));
        list.Add(new(0x30, VariableDataQuantityUnit.Power_GJ_per_h, b => (b & 0x01) - 1));
        list.Add(new(0x31, VariableDataQuantityUnit.Power_GJ_per_h, b => (b & 0x01) - 1));
        for (byte i = 0x32; i <= 0x57; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => b - 0x32));
        for (byte i = 0x58; i <= 0x5B; i++) list.Add(new(i, VariableDataQuantityUnit.FlowTemperature_F, b => b - 0x32));
        for (byte i = 0x5C; i <= 0x5F; i++) list.Add(new(i, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3));
        for (byte i = 0x60; i <= 0x63; i++) list.Add(new(i, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3));
        for (byte i = 0x64; i <= 0x67; i++) list.Add(new(i, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3));
        for (byte i = 0x68; i <= 0x6F; i++) list.Add(new(i, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => b & 0x07));
        for (byte i = 0x70; i <= 0x73; i++) list.Add(new(i, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3));
        for (byte i = 0x74; i <= 0x77; i++) list.Add(new(i, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3));
        for (byte i = 0x78; i <= 0x7F; i++) list.Add(new(i, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3));

        return [.. list];
    }

    private static VifeTableEntry[] BuildVifeFdTable()
    {
        var list = new List<VifeTableEntry>();

        for (byte i = 0x00; i <= 0x03; i++) list.Add(new(i, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3));
        for (byte i = 0x04; i <= 0x07; i++) list.Add(new(i, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3));
        list.Add(new(0x08, VariableDataQuantityUnit.AccessNumber, _ => 0));
        list.Add(new(0x09, VariableDataQuantityUnit.Medium, _ => 0));
        list.Add(new(0x0A, VariableDataQuantityUnit.Manufacturer, _ => 0));
        list.Add(new(0x0B, VariableDataQuantityUnit.EnhancedIdentification, _ => 0));
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
        list.Add(new(0x19, VariableDataQuantityUnit.ReservedVIFE_FD_19, _ => 0));
        list.Add(new(0x1A, VariableDataQuantityUnit.DigitalOutput, _ => 0));
        list.Add(new(0x1B, VariableDataQuantityUnit.DigitalInput, _ => 0));
        list.Add(new(0x1C, VariableDataQuantityUnit.Baudrate, _ => 0));
        list.Add(new(0x1D, VariableDataQuantityUnit.ResponseDelayTime, _ => 0));
        list.Add(new(0x1E, VariableDataQuantityUnit.Retry, _ => 0));
        list.Add(new(0x1F, VariableDataQuantityUnit.ReservedVIFE_FD_1f, _ => 0));
        list.Add(new(0x20, VariableDataQuantityUnit.FirstStorageNr, _ => 0));
        list.Add(new(0x21, VariableDataQuantityUnit.LastStorageNr, _ => 0));
        list.Add(new(0x22, VariableDataQuantityUnit.SizeOfStorage, _ => 0));
        list.Add(new(0x23, VariableDataQuantityUnit.ReservedVIFE_FD_23, _ => 0));
        for (byte i = 0x24; i <= 0x27; i++) list.Add(new(i, VariableDataQuantityUnit.StorageInterval, b => b & 0x03));
        list.Add(new(0x28, VariableDataQuantityUnit.StorageIntervalMmnth, _ => 0));
        list.Add(new(0x29, VariableDataQuantityUnit.StorageIntervalYear, _ => 0));
        list.Add(new(0x2A, VariableDataQuantityUnit.ReservedVIFE_FD_2a, _ => 0));
        list.Add(new(0x2B, VariableDataQuantityUnit.ReservedVIFE_FD_2b, _ => 0));
        for (byte i = 0x2C; i <= 0x2F; i++) list.Add(new(i, VariableDataQuantityUnit.DurationSinceLastReadout, _ => 0));
        list.Add(new(0x30, VariableDataQuantityUnit.StartDateTimeOfTariff, _ => 0));
        for (byte i = 0x31; i <= 0x33; i++) list.Add(new(i, VariableDataQuantityUnit.DurationOfTariff, b => b & 0x03));
        for (byte i = 0x34; i <= 0x37; i++) list.Add(new(i, VariableDataQuantityUnit.PeriodOfTariff, b => b & 0x03));
        list.Add(new(0x38, VariableDataQuantityUnit.PeriodOfTariffMonths, _ => 0));
        list.Add(new(0x39, VariableDataQuantityUnit.PeriodOfTariffYear, _ => 0));
        list.Add(new(0x3A, VariableDataQuantityUnit.Dimensionless, _ => 0));
        for (byte i = 0x3B; i <= 0x3F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved_FD_3c, _ => 0));
        for (byte i = 0x40; i <= 0x4F; i++) list.Add(new(i, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9));
        for (byte i = 0x50; i <= 0x5F; i++) list.Add(new(i, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12));
        list.Add(new(0x60, VariableDataQuantityUnit.ResetCounter, _ => 0));
        list.Add(new(0x61, VariableDataQuantityUnit.CumulationCounter, _ => 0));
        list.Add(new(0x62, VariableDataQuantityUnit.ControlSignal, _ => 0));
        list.Add(new(0x63, VariableDataQuantityUnit.DayOfWeek, _ => 0));
        list.Add(new(0x64, VariableDataQuantityUnit.WeekNumber, _ => 0));
        list.Add(new(0x65, VariableDataQuantityUnit.TimePointOfDayChange, _ => 0));
        list.Add(new(0x66, VariableDataQuantityUnit.StateOfParameterActivation, _ => 0));
        list.Add(new(0x67, VariableDataQuantityUnit.SpecialSupplierInformation, _ => 0));
        for (byte i = 0x68; i <= 0x6B; i++) list.Add(new(i, VariableDataQuantityUnit.DurationSinceLastCumulation, _ => 0));
        for (byte i = 0x6C; i <= 0x6F; i++) list.Add(new(i, VariableDataQuantityUnit.OperatingTimeBattery, _ => 0));
        list.Add(new(0x70, VariableDataQuantityUnit.DateTimeOfBatteryChange, _ => 0));
        for (byte i = 0x71; i <= 0x7F; i++) list.Add(new(i, VariableDataQuantityUnit.Reserved_FD_71, _ => 0));

        return [.. list];
    }
}
