using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// A libmbus XML decode (mbus_frame_xml output) of one frame in DataExamples.
/// </summary>
internal sealed record LibmbusReference(LibmbusSlave Slave, IReadOnlyList<LibmbusRecord> Records)
{
    public static LibmbusReference Load(string path)
    {
        // PreserveWhitespace keeps an all-blank ASCII value such as "          " instead of reading it as empty.
        var root = Parse(path, LoadOptions.PreserveWhitespace).Root!;
        var si = root.Element("SlaveInformation")!;
        var slave = new LibmbusSlave(
            Text(si, "Id"),
            Text(si, "Manufacturer"),
            Text(si, "Version"),
            Text(si, "Medium"),
            Text(si, "AccessNumber"),
            Text(si, "Status"),
            Text(si, "Signature"));
        var records = root.Elements("DataRecord")
            .Select(x => new LibmbusRecord(
                x.Attribute("id")!.Value,
                Text(x, "Function") ?? "",
                Text(x, "StorageNumber"),
                Text(x, "Tariff"),
                Text(x, "Device"),
                Text(x, "Unit") ?? "",
                Text(x, "Value") ?? ""))
            .ToList();
        return new LibmbusReference(slave, records);
    }

    /// <summary>
    /// libmbus declares ISO-8859-1 but writes its own texts as UTF-8 (the ´ in "More than 10 DIFE´s"), so read the
    /// file as UTF-8 and ignore the declaration.
    /// </summary>
    public static XDocument Parse(string path, LoadOptions options = LoadOptions.None) =>
        XDocument.Parse(File.ReadAllText(path, System.Text.Encoding.UTF8), options);

    private static string? Text(XElement parent, string name) => parent.Element(name)?.Value;
}

internal sealed record LibmbusSlave(
    string? Id,
    string? Manufacturer,
    string? Version,
    string? Medium,
    string? AccessNumber,
    string? Status,
    string? Signature);

/// <summary>
/// One libmbus DataRecord. Value is printed raw: integers unscaled with the power of ten in the Unit text,
/// reals with %f, dates as ISO strings, binary data as space-separated hex.
/// </summary>
internal sealed record LibmbusRecord(
    string Id,
    string Function,
    string? StorageNumber,
    string? Tariff,
    string? Device,
    string Unit,
    string Value)
{
    // libmbus reports the bytes after DIF 0x0F/0x1F as a record; the library exposes them on the packet.
    public bool IsManufacturerSpecificData => Function == "Manufacturer specific";

    public bool IsMoreRecordsFollow => Function == "More records follow";
}

/// <summary>
/// What a libmbus Unit text says about a record. Quantity null with VifCode set means libmbus did not know the
/// code and printed it raw; PlainText is a VIF 0x7C/0xFC unit that the meter sent as ASCII.
/// </summary>
internal sealed record LibmbusUnit(
    VariableDataQuantityUnit Quantity,
    int? Exponent,
    string? TimeUnit = null,
    string? VifCode = null,
    string? PlainText = null);

/// <summary>
/// Explicit translation tables from libmbus text to library values.
/// </summary>
internal static partial class LibmbusText
{
    // "<quantity> (<prefix><unit>)" from mbus_vib_unit_lookup for primary VIFs, e.g. "Volume (1e-2  m^3)".
    private static readonly Dictionary<(string Quantity, string Unit), VariableDataQuantityUnit> ScaledQuantities = new()
    {
        [("Energy", "Wh")] = VariableDataQuantityUnit.EnergyWh,
        [("Energy", "J")] = VariableDataQuantityUnit.EnergyJ,
        [("Volume", "m^3")] = VariableDataQuantityUnit.Volume_m3,
        [("Mass", "kg")] = VariableDataQuantityUnit.Mass_kg,
        [("Power", "W")] = VariableDataQuantityUnit.PowerW,
        [("Power", "J/h")] = VariableDataQuantityUnit.PowerJ_per_h,
        [("Volume flow", "m^3/h")] = VariableDataQuantityUnit.VolumeFlowM3_per_h,
        [("Volume flow", "m^3/min")] = VariableDataQuantityUnit.VolumeFlowExtM3_per_min,
        [("Volume flow", "m^3/s")] = VariableDataQuantityUnit.VolumeFlowExtM3_per_s,
        [("Mass flow", "kg/h")] = VariableDataQuantityUnit.MassFlowKg_per_h,
        [("Flow temperature", "deg C")] = VariableDataQuantityUnit.FlowTemperatureC,
        [("Return temperature", "deg C")] = VariableDataQuantityUnit.ReturnTemperatureC,
        [("Temperature Difference", "deg C")] = VariableDataQuantityUnit.TemperatureDifferenceK,
        [("External temperature", "deg C")] = VariableDataQuantityUnit.ExternalTemperatureC,
        [("Pressure", "bar")] = VariableDataQuantityUnit.PressureBar,
    };

    // "<prefix> <unit>" for the FD/FB codes libmbus prints without a quantity name, e.g. "1e-1  V", "m A".
    private static readonly Dictionary<string, VariableDataQuantityUnit> BareQuantities = new()
    {
        ["V"] = VariableDataQuantityUnit.Volts,
        ["A"] = VariableDataQuantityUnit.Amperes,
    };

    // Durations: the time unit is part of the unit, not a power of ten.
    private static readonly Dictionary<string, VariableDataQuantityUnit> Durations = new()
    {
        ["On time"] = VariableDataQuantityUnit.OnTime,
        ["Operating time"] = VariableDataQuantityUnit.OperatingTime,
        ["Averaging Duration"] = VariableDataQuantityUnit.AveragingDuration,
        ["Actuality Duration"] = VariableDataQuantityUnit.ActualityDuration,
    };

    private static readonly Dictionary<string, string> TimeUnits = new()
    {
        ["seconds"] = "s",
        ["minutes"] = "min",
        ["hours"] = "h",
        ["days"] = "d",
    };

    private static readonly Dictionary<string, LibmbusUnit> Named = new()
    {
        ["Fabrication number"] = new(VariableDataQuantityUnit.FabricationNo, 0),
        ["Units for H.C.A."] = new(VariableDataQuantityUnit.UnitsForHCA, 0),
        ["Time Point (date)"] = new(VariableDataQuantityUnit.TimePoint, 0, VifCode: "6Ch"),
        ["Time Point (time & date)"] = new(VariableDataQuantityUnit.TimePoint, 0, VifCode: "6Dh"),
        ["Manufacturer specific"] = new(VariableDataQuantityUnit.ManufacturerSpecific, 0),
        ["Error flags"] = new(VariableDataQuantityUnit.ErrorFlags, 0),
        ["Software version"] = new(VariableDataQuantityUnit.SoftwareVersionNr, 0),
        ["Firmware version"] = new(VariableDataQuantityUnit.FirmwareVersionNr, 0),
        ["Customer location"] = new(VariableDataQuantityUnit.CustomerLocation, 0),
        ["Digital input (binary)"] = new(VariableDataQuantityUnit.DigitalInput, 0),
        ["Digital output (binary)"] = new(VariableDataQuantityUnit.DigitalOutput, 0),
        ["Model / Version"] = new(VariableDataQuantityUnit.ModelVersion, 0),
        ["Medium (as in fixed header)"] = new(VariableDataQuantityUnit.Medium, 0),
        ["Parameter set identification"] = new(VariableDataQuantityUnit.ParameterSetIdentification, 0),
    };

    // mbus_data_variable_medium_lookup: the device type byte of the variable data header.
    private static readonly Dictionary<string, byte> VariableMedia = new()
    {
        ["Other"] = 0x00,
        ["Oil"] = 0x01,
        ["Electricity"] = 0x02,
        ["Gas"] = 0x03,
        ["Heat: Outlet"] = 0x04,
        ["Steam"] = 0x05,
        ["Hot water"] = 0x06,
        ["Water"] = 0x07,
        ["Heat Cost Allocator"] = 0x08,
        ["Compressed Air"] = 0x09,
        ["Cooling load meter: Outlet"] = 0x0A,
        ["Cooling load meter: Inlet"] = 0x0B,
        ["Heat: Inlet"] = 0x0C,
        ["Heat / Cooling load meter"] = 0x0D,
        ["Bus/System"] = 0x0E,
        ["Unknown Medium"] = 0x0F,
        ["Cold water"] = 0x16,
        ["Dual water"] = 0x17,
        ["Pressure"] = 0x18,
        ["A/D Converter"] = 0x19,
    };

    // mbus_data_fixed_medium: the 4-bit medium spread over the top bits of the two fixed-data unit bytes.
    private static readonly Dictionary<string, byte> FixedMedia = new()
    {
        ["Other"] = 0x00,
        ["Oil"] = 0x01,
        ["Electricity"] = 0x02,
        ["Gas"] = 0x03,
        ["Heat"] = 0x04,
        ["Steam"] = 0x05,
        ["Hot Water"] = 0x06,
        ["Water"] = 0x07,
        ["H.C.A."] = 0x08,
        ["Gas Mode 2"] = 0x0A,
        ["Heat Mode 2"] = 0x0B,
        ["Hot Water Mode 2"] = 0x0C,
        ["Water Mode 2"] = 0x0D,
        ["H.C.A. Mode 2"] = 0x0E,
    };

    // mbus_data_fixed_unit texts that do not follow the "<10|100 ><unit>" pattern.
    private static readonly Dictionary<string, FixedDataUnits> FixedUnitsNamed = new()
    {
        ["h,m,s"] = FixedDataUnits.hms,
        ["D,M,Y"] = FixedDataUnits.DMY,
        ["°C"] = FixedDataUnits.C001,
        ["units for HCA"] = FixedDataUnits.UnitsForHCA,
        ["reserved but historic"] = FixedDataUnits.sameButHistoric,
        ["without units"] = FixedDataUnits.withoutUnits,
    };

    public static readonly Dictionary<string, Function> Functions = new()
    {
        ["Instantaneous value"] = Function.Instantaneous,
        ["Maximum value"] = Function.Maximum,
        ["Minimum value"] = Function.Minimum,
        ["Value during error state"] = Function.ValueDuringError,
    };

    // libmbus <Error> texts for CI 0x70 (mbus_data_error_lookup).
    public static readonly Dictionary<string, ApplicationErrorCode> ApplicationErrors = new()
    {
        ["Unspecified error"] = ApplicationErrorCode.Unspecified,
        ["Unimplemented CI-Field"] = ApplicationErrorCode.Unimplemented_CI,
        ["Buffer too long, truncated"] = ApplicationErrorCode.BufferTooLong,
        ["Too many records"] = ApplicationErrorCode.TooManyRecords,
        ["Premature end of record"] = ApplicationErrorCode.PrematureEnd,
        ["More than 10 DIFE´s"] = ApplicationErrorCode.TooManyDIFEs,
        ["More than 10 VIFE´s"] = ApplicationErrorCode.TooManyVIFEs,
        ["Application busy"] = ApplicationErrorCode.Busy,
        ["Too many readouts"] = ApplicationErrorCode.TooManyReadouts,
    };

    public static byte? VariableMedium(string text) => VariableMedia.TryGetValue(text, out var b) ? b : null;

    public static byte? FixedMedium(string text) => FixedMedia.TryGetValue(text, out var b) ? b : null;

    /// <summary>
    /// Parses a libmbus variable-data Unit text; null when the table does not cover it.
    /// </summary>
    public static LibmbusUnit? Unit(string text)
    {
        if (Named.TryGetValue(text, out var named))
            return named;

        var m = ScaledPattern().Match(text);
        if (m.Success && ScaledQuantities.TryGetValue((m.Groups["q"].Value, m.Groups["u"].Value), out var q)
            && Prefix(m.Groups["p"].Value) is int exp)
            return new LibmbusUnit(q, exp);

        m = DurationPattern().Match(text);
        if (m.Success && Durations.TryGetValue(m.Groups["q"].Value, out var d))
            return new LibmbusUnit(d, 0, TimeUnits[m.Groups["t"].Value]);

        m = BarePattern().Match(text);
        if (m.Success && BareQuantities.TryGetValue(m.Groups["u"].Value, out var b) && Prefix(m.Groups["p"].Value) is int bexp)
            return new LibmbusUnit(b, bexp);

        // libmbus prints codes it has no text for; the library must at least have decoded the same code.
        m = UnknownCodePattern().Match(text);
        if (m.Success)
            return new LibmbusUnit(VariableDataQuantityUnit.Undefined, null, VifCode: $"{m.Groups["c"].Value.ToUpperInvariant()}h");

        return null;
    }

    /// <summary>
    /// libmbus prints a plain-text (VIF 0x7C/0xFC) unit as the ASCII the meter sent, after the prefix of any
    /// scaling VIFE, e.g. "1e-2  %RH". The library's unit text tells where the prefix ends.
    /// </summary>
    public static LibmbusUnit PlainText(string text, string libraryUnit)
    {
        if (libraryUnit.Length > 0 && text.EndsWith(libraryUnit, StringComparison.Ordinal)
            && Prefix(text[..^libraryUnit.Length]) is int exp)
            return new LibmbusUnit(VariableDataQuantityUnit.CustomVIF, exp, PlainText: libraryUnit);
        return new LibmbusUnit(VariableDataQuantityUnit.CustomVIF, null, PlainText: text);
    }

    /// <summary>
    /// Parses a libmbus fixed-data unit text (mbus_data_fixed_unit), e.g. "kWh", "10 l", "m^3/h".
    /// </summary>
    public static FixedDataUnits? FixedUnit(string text)
    {
        if (FixedUnitsNamed.TryGetValue(text, out var named))
            return named;

        var m = FixedUnitPattern().Match(text);
        if (!m.Success)
            return null;
        var name = m.Groups["u"].Value.Replace("m^3", "m3").Replace("/h", "_per_h") + m.Groups["p"].Value;
        return Enum.TryParse<FixedDataUnits>(name, out var unit) && unit.ToString() == name ? unit : null;
    }

    // mbus_unit_prefix: "" 0, "m" -3, "my" -6, "10 " 1, "100 " 2, "k" 3, "10 k" 4, "100 k" 5, "M" 6,
    // "T" 9 (libmbus' spelling of giga), otherwise "1e<n> ".
    private static int? Prefix(string text) => text.Replace(" ", "") switch
    {
        "" => 0,
        "m" => -3,
        "my" => -6,
        "10" => 1,
        "100" => 2,
        "k" => 3,
        "10k" => 4,
        "100k" => 5,
        "M" => 6,
        "T" => 9,
        var p when p.StartsWith("1e", StringComparison.Ordinal)
            && int.TryParse(p[2..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var e) => e,
        _ => null,
    };

    [GeneratedRegex(@"^(?<q>[A-Za-z ]+?) \((?<p>[^()]*?)(?<u>Wh|J/h|J|m\^3/h|m\^3/min|m\^3/s|m\^3|kg/h|kg|W|deg C|bar)\)$")]
    private static partial Regex ScaledPattern();

    [GeneratedRegex(@"^(?<q>[A-Za-z ]+) \((?<t>seconds|minutes|hours|days)\)$")]
    private static partial Regex DurationPattern();

    [GeneratedRegex(@"^(?<p>[^ ]*) +(?<u>V|A)$")]
    private static partial Regex BarePattern();

    [GeneratedRegex(@"^(?:Unrecognized VIF extension: |Unknown \(VIF=)0x(?<c>[0-9A-Fa-f]{2})\)?$")]
    private static partial Regex UnknownCodePattern();

    [GeneratedRegex(@"^(?:(?<p>10|100) )?(?<u>[A-Za-z^/0-9]+)$")]
    private static partial Regex FixedUnitPattern();
}
