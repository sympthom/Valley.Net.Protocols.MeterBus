using System.Globalization;
using System.Text.RegularExpressions;

namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// One field where the library and libmbus disagree. Key is "&lt;frame&gt;#&lt;record&gt;:&lt;field&gt;" with the
/// libmbus record id, "slave" for SlaveInformation or "frame" for the frame as a whole.
/// </summary>
internal sealed record GoldenDifference(string Key, string Category, string Detail)
{
    public string Entry => $"{Key} {Category}";
}

internal sealed class GoldenFrameResult(string frame)
{
    public string Frame { get; } = frame;
    public List<GoldenDifference> Differences { get; } = [];
    public bool SlaveMatches { get; set; }
    public int Records { get; set; }
    public int RecordsMatching { get; set; }
    public int Values { get; set; }
    public int ValuesMatching { get; set; }
}

/// <summary>
/// Parses and maps a corpus frame with the library and compares the packet with the libmbus decode semantically:
/// the same quantity, power of ten, time unit and raw value, not the same text.
/// </summary>
internal static partial class LibmbusComparer
{
    private static readonly FrameParser Parser = new();
    private static readonly PacketMapper Mapper = new(new VifLookupService());

    public static GoldenFrameResult Compare(string frame, byte[] bytes, LibmbusReference reference)
    {
        var result = new GoldenFrameResult(frame);
        result.Records = reference.Records.Count(r => !r.IsManufacturerSpecificData && !r.IsMoreRecordsFollow);
        result.Values = result.Records;

        var parsed = Parser.Parse(bytes);
        if (!parsed.IsSuccess)
        {
            Add(result, "frame", "Parse", "parse-fail", $"{parsed.Error!.Code}: {parsed.Error.Message}");
            return result;
        }

        var mapped = Mapper.MapToPacket(parsed.Value!);
        if (!mapped.IsSuccess)
        {
            Add(result, "frame", "Map", "map-fail", $"{mapped.Error!.Code}: {mapped.Error.Message}");
            return result;
        }

        switch (mapped.Value)
        {
            case VariableDataPacket vp:
                CompareVariable(result, vp, reference);
                break;
            case FixedDataPacket fp:
                CompareFixed(result, fp, reference);
                break;
            default:
                Add(result, "frame", "PacketType", "packet-type", mapped.Value!.GetType().Name);
                break;
        }
        return result;
    }

    private static void CompareVariable(GoldenFrameResult result, VariableDataPacket vp, LibmbusReference reference)
    {
        var s = reference.Slave;
        var before = result.Differences.Count;
        Field(result, "slave", "Id", "id", s.Id, vp.IdentificationNo.ToString(CultureInfo.InvariantCulture));
        Field(result, "slave", "Manufacturer", "manufacturer", s.Manufacturer, ManufacturerParser.Parse(vp.Manufacturer));
        Field(result, "slave", "Version", "version", s.Version, vp.Version.ToString(CultureInfo.InvariantCulture));
        Field(result, "slave", "Medium", "medium", Code(s.Medium, LibmbusText.VariableMedium), Code((byte)vp.DeviceType));
        Field(result, "slave", "AccessNumber", "access-number", s.AccessNumber, vp.TransmissionCounter.ToString(CultureInfo.InvariantCulture));
        Field(result, "slave", "Status", "status", s.Status, vp.Status.ToString("X2", CultureInfo.InvariantCulture));
        Field(result, "slave", "Signature", "signature", s.Signature, vp.Signature.ToString("X4", CultureInfo.InvariantCulture));
        result.SlaveMatches = result.Differences.Count == before;

        // libmbus lists the bytes after DIF 0x0F/0x1F as a final pseudo-record; the library puts them on the packet.
        var trailer = reference.Records.LastOrDefault(r => r.IsManufacturerSpecificData || r.IsMoreRecordsFollow);
        if (trailer is null)
        {
            if (vp.MoreRecordsFollow || !vp.ManufacturerData.IsEmpty)
                Add(result, "frame", "ManufacturerData", "manufacturer-data",
                    $"libmbus has no trailer, library MoreRecordsFollow={vp.MoreRecordsFollow} data='{Hex(vp.ManufacturerData.AsSpan())}'");
        }
        else
        {
            Field(result, trailer.Id, "MoreRecordsFollow", "more-records-follow",
                trailer.IsMoreRecordsFollow ? "True" : "False", vp.MoreRecordsFollow ? "True" : "False");
            Field(result, trailer.Id, "ManufacturerData", "manufacturer-data", trailer.Value, Hex(vp.ManufacturerData.AsSpan()));
        }

        var expected = reference.Records.Where(r => !r.IsManufacturerSpecificData && !r.IsMoreRecordsFollow).ToList();
        if (expected.Count != vp.Records.Length)
            Add(result, "frame", "RecordCount", "record-count", $"libmbus={expected.Count} library={vp.Records.Length}");

        for (int i = 0; i < expected.Count; i++)
        {
            var x = expected[i];
            if (i >= vp.Records.Length)
            {
                Add(result, x.Id, "Missing", "missing-record", $"'{x.Unit}' = '{x.Value}'");
                continue;
            }

            var r = vp.Records[i];
            before = result.Differences.Count;
            Field(result, x.Id, "Function", "function",
                LibmbusText.Functions.TryGetValue(x.Function, out var f) ? f.ToString() : $"<{x.Function}>", r.Function.ToString());
            Field(result, x.Id, "StorageNumber", "storage", x.StorageNumber ?? "0", r.StorageNumber.ToString(CultureInfo.InvariantCulture));
            Field(result, x.Id, "Tariff", "tariff", x.Tariff ?? "0", r.Tariff.ToString(CultureInfo.InvariantCulture));
            Field(result, x.Id, "Device", "device", x.Device ?? "0", r.SubUnit.ToString(CultureInfo.InvariantCulture));
            CompareUnit(result, x, r);

            var valueBefore = result.Differences.Count;
            CompareValue(result, x, r);
            if (result.Differences.Count == valueBefore)
                result.ValuesMatching++;
            if (result.Differences.Count == before)
                result.RecordsMatching++;
        }
    }

    private static void CompareFixed(GoldenFrameResult result, FixedDataPacket fp, LibmbusReference reference)
    {
        var s = reference.Slave;
        var before = result.Differences.Count;
        Field(result, "slave", "Id", "id", s.Id, fp.IdentificationNo.ToString(CultureInfo.InvariantCulture));
        Field(result, "slave", "Medium", "medium", Code(s.Medium, LibmbusText.FixedMedium), Code((byte)fp.Medium));
        Field(result, "slave", "AccessNumber", "access-number", s.AccessNumber, fp.TransmissionCounter.ToString(CultureInfo.InvariantCulture));
        Field(result, "slave", "Status", "status", s.Status,
            fp.Status.ToString("X2", CultureInfo.InvariantCulture));
        result.SlaveMatches = result.Differences.Count == before;

        // mbus_data_fixed_function: status bit 1 set means the counters are stored (historic) values.
        var function = fp.CountersFixed ? "Stored value" : "Actual value";
        var counters = new (FixedDataUnits Units, object? Value)[] { (fp.Units1, fp.Counter1), (fp.Units2, fp.Counter2) };
        if (reference.Records.Count != counters.Length)
            Add(result, "frame", "RecordCount", "record-count", $"libmbus={reference.Records.Count} library={counters.Length}");

        for (int i = 0; i < Math.Min(counters.Length, reference.Records.Count); i++)
        {
            var x = reference.Records[i];
            before = result.Differences.Count;
            Field(result, x.Id, "Function", "function", x.Function, function);
            Field(result, x.Id, "Unit", "quantity", LibmbusText.FixedUnit(x.Unit)?.ToString() ?? $"<{x.Unit}>", counters[i].Units.ToString());
            var valueBefore = result.Differences.Count;
            // A counter with a non-decimal BCD nibble is null; libmbus prints its arithmetic value.
            Field(result, x.Id, "Value", counters[i].Value is null ? "invalid-bcd" : "numeric", x.Value,
                counters[i].Value is { } v ? Convert.ToString(v, CultureInfo.InvariantCulture)! : "null");
            if (result.Differences.Count == valueBefore)
                result.ValuesMatching++;
            if (result.Differences.Count == before)
                result.RecordsMatching++;
        }
    }

    private static void CompareUnit(GoldenFrameResult result, LibmbusRecord x, DataRecord r)
    {
        // The VIF of an FB/FD record is the first VIFE; Units[0] is only the 0xFB/0xFD marker.
        var main = r.Units.Length > 1 && r.Units[0].Units is VariableDataQuantityUnit.Extension_7B or VariableDataQuantityUnit.Extension_7D
            ? r.Units[1]
            : r.Units[0];

        var expected = main.Units == VariableDataQuantityUnit.CustomVIF
            ? LibmbusText.PlainText(x.Unit, main.Unit ?? "")
            : LibmbusText.Unit(x.Unit);
        if (expected is null)
        {
            Add(result, x.Id, "Quantity", "unmapped-unit", $"no table entry for libmbus '{x.Unit}', library {Describe(r)}");
            return;
        }

        if (expected.Quantity != VariableDataQuantityUnit.Undefined)
            Field(result, x.Id, "Quantity", "quantity", expected.Quantity.ToString(), main.Units.ToString(), $" ('{x.Unit}' vs {Describe(r)})");
        if (expected.VifCode is not null)
            Field(result, x.Id, "VifCode", "vif-code", expected.VifCode, main.VifString ?? "", $" ('{x.Unit}' vs {Describe(r)})");
        if (expected.PlainText is not null)
            Field(result, x.Id, "PlainTextUnit", "plain-text-unit", expected.PlainText, main.Unit ?? "");
        if (expected.Exponent is int exp && expected.Quantity == main.Units)
            Field(result, x.Id, "Scale", "scale", exp.ToString(CultureInfo.InvariantCulture), r.Magnitude.ToString(CultureInfo.InvariantCulture), $" ('{x.Unit}' vs {Describe(r)})");
        if (expected.TimeUnit is not null && expected.Quantity == main.Units)
            Field(result, x.Id, "TimeUnit", "time-unit", expected.TimeUnit, main.Unit ?? "");
    }

    private static void CompareValue(GoldenFrameResult result, LibmbusRecord x, DataRecord r)
    {
        var e = x.Value;
        var a = r.Value;
        var category = ValueCategory(e, r, r.ValueError);
        if (category is not null)
            Add(result, x.Id, "Value", category, $"libmbus '{e}' library {Show(a)} ({a?.GetType().Name ?? "null"}, {r.ValueDataType}, '{x.Unit}')");
    }

    /// <summary>
    /// Null when the values agree, otherwise the reason category for the known-differences file.
    /// </summary>
    internal static string? ValueCategory(string e, DataRecord r, string? valueError)
    {
        var a = r.Value;
        // libmbus prints a value for every record, so a ValueError is always a difference, whatever libmbus printed.
        if (valueError is not null)
            return valueError switch { "INVALID_BCD" => "invalid-bcd", "INVALID_DATE" => "invalid-date", _ => "value-error" };

        if (e.Length == 0)
            return a is null or "" || a is byte[] { Length: 0 } ? null : "empty";

        if (DatePattern().IsMatch(e))
        {
            // libmbus prints type G as a date and types F/I with the time, so the value type must agree as well.
            var actual = (a, e.Contains('T')) switch
            {
                (DateOnly d, false) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                (DateTime dt, true) => dt.ToString("s", CultureInfo.InvariantCulture),
                _ => null,
            };
            if (actual is null)
                return a is DateOnly or DateTime or TimeOnly ? "type" : a is null or string ? "date" : "date-not-decoded";
            return actual == e ? null : "date";
        }

        // Before the number test: libmbus prints one byte of binary data as two hex digits, and an ASCII string
        // may be all digits. A BCD field with a string value is today's invalid-BCD fallback, handled below.
        if (a is byte[] bytes && HexPattern().IsMatch(e))
            return Hex(bytes) == e ? null : Hex(bytes.Reverse().ToArray()) == e ? "binary-order" : "binary";

        if (a is string s && !IsBcd(r.ValueDataType))
        {
            if (s == e)
                return null;
            return new string(s.Reverse().ToArray()) == e ? "string-reversed" : "string";
        }

        if (long.TryParse(e, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var expected))
        {
            // libmbus decodes BCD digit by digit: A-F nibbles count as 10-15 and a leading F is not a minus sign.
            // Today's decode falls back to a string for such a field.
            if (a is string && IsBcd(r.ValueDataType))
                return "invalid-bcd";
            if (a is not (sbyte or byte or short or ushort or int or uint or long))
                return a is null ? "null" : "type";
            var actual = Convert.ToInt64(a, CultureInfo.InvariantCulture);
            if (actual == expected)
                return null;
            if (actual < 0 && expected >= 0 && IsBcd(r.ValueDataType))
                return "negative-bcd";
            return expected < 0 && actual > 0 ? "sign" : "numeric";
        }

        // libmbus prints a real with %f.
        if (RealPattern().IsMatch(e))
            return a is float f && ((double)f).ToString("F6", CultureInfo.InvariantCulture) == e ? null
                : a is float ? "real" : "type";

        return a is null ? "null" : "type";
    }

    private static bool IsBcd(DataTypes type) => type is DataTypes._2_digit_BCD or DataTypes._4_digit_BCD
        or DataTypes._6_digit_BCD or DataTypes._8_digit_BCD or DataTypes._12_digit_BCD;

    private static void Field(GoldenFrameResult result, string record, string field, string category, string? expected, string actual, string context = "")
    {
        // libmbus leaves out fields it does not decode (for example Manufacturer for a fixed-data frame).
        if (expected is null || expected == actual)
            return;
        Add(result, record, field, category, $"libmbus '{expected}' library '{actual}'{context}");
    }

    private static void Add(GoldenFrameResult result, string record, string field, string category, string detail) =>
        result.Differences.Add(new GoldenDifference($"{result.Frame}#{record}:{field}", category, detail));

    private static string? Code(string? text, Func<string, byte?> lookup) =>
        text is null ? null : lookup(text) is byte b ? Code(b) : $"<{text}>";

    private static string Code(byte b) => $"0x{b:X2}";

    private static string Hex(ReadOnlySpan<byte> bytes) =>
        string.Join(' ', bytes.ToArray().Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));

    private static string Show(object? v) => v switch
    {
        null => "null",
        float f => ((double)f).ToString("F6", CultureInfo.InvariantCulture),
        string s => $"\"{s}\"",
        byte[] b => $"[{Hex(b)}]",
        DateTime d => d.ToString("s", CultureInfo.InvariantCulture),
        IFormattable fmt => fmt.ToString(null, CultureInfo.InvariantCulture),
        _ => v.ToString() ?? "",
    };

    private static string Describe(DataRecord r) =>
        string.Join("+", r.Units.Select(u => $"{u.Units}({u.Magnitude}{(string.IsNullOrEmpty(u.Unit) ? "" : " " + u.Unit)})@{u.VifString}"));

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}(T\d{2}:\d{2}:\d{2})?$")]
    private static partial Regex DatePattern();

    [GeneratedRegex(@"^-?\d+\.\d{6}$")]
    private static partial Regex RealPattern();

    [GeneratedRegex(@"^[0-9A-F]{2}( [0-9A-F]{2})*$")]
    private static partial Regex HexPattern();
}
