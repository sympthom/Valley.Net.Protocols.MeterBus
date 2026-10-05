using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// Compares the decode of every test frame with its libmbus reference (DataExamples/test-frames/*.xml).
/// Golden/known-differences.txt lists every field where the two disagree today; a difference that is not
/// listed is a regression and a listed one that no longer occurs is stale, and both fail. After a deliberate
/// change, regenerate the list with GOLDEN_UPDATE=1 and review its diff.
/// </summary>
[TestClass]
public sealed class GoldenLibmbusTests
{
    private const string KnownDifferencesFile = "known-differences.txt";

    private static readonly bool UpdateMode = Environment.GetEnvironmentVariable("GOLDEN_UPDATE") == "1";

    private static readonly Lazy<HashSet<string>> KnownDifferences = new(() => ReadKnownDifferences(KnownDifferencesPath()));

    public TestContext TestContext { get; set; } = null!;

    public static IEnumerable<TestDataRow<string>> ReferenceFrames() =>
        ReferenceFrameNames().Select(name => new TestDataRow<string>(name) { DisplayName = name });

    private static IEnumerable<string> ReferenceFrameNames() =>
        Corpus.Names(Corpus.TestFrames).Where(name => Corpus.XmlPath(Corpus.TestFrames, name) is not null);

    [TestMethod]
    [DynamicData(nameof(ReferenceFrames))]
    public void Frame_MatchesLibmbus_ExceptKnownDifferences(string name)
    {
        var result = Compare(name);
        foreach (var d in result.Differences)
            TestContext.WriteLine($"{d.Entry}: {d.Detail}");

        // KnownDifferences_FileIsCurrent rewrites the file in update mode.
        if (UpdateMode)
            return;

        var listed = KnownDifferences.Value.Where(e => e.StartsWith(name + "#", StringComparison.Ordinal)).ToHashSet();
        var unexpected = result.Differences.Where(d => !listed.Contains(d.Entry)).ToList();
        var stale = listed.Except(result.Differences.Select(d => d.Entry)).Order(StringComparer.Ordinal).ToList();

        if (unexpected.Count > 0 || stale.Count > 0)
        {
            Assert.Fail(
                $"'{name}' no longer matches {KnownDifferencesFile} (run with GOLDEN_UPDATE=1 to regenerate it after a deliberate change)." +
                string.Concat(unexpected.Select(d => $"\n  new:   {d.Entry}: {d.Detail}")) +
                string.Concat(stale.Select(e => $"\n  stale: {e} (no longer occurs)")));
        }
    }

    [TestMethod]
    public void KnownDifferences_FileIsCurrent()
    {
        var results = ReferenceFrameNames().Select(Compare).ToList();
        var current = results.SelectMany(r => r.Differences).Select(d => d.Entry).ToList();

        var summary = Summary(results);
        TestContext.WriteLine(summary);
        WriteCiSummary(summary);

        if (UpdateMode)
        {
            var path = SourceKnownDifferencesPath();
            File.WriteAllLines(path, Header.Concat(current));
            TestContext.WriteLine($"Wrote {current.Count} entries to {path}");
            return;
        }

        // The per-frame tests report which frame changed; this catches entries for frames that left the corpus.
        var frames = results.Select(r => r.Frame + "#").ToList();
        var orphaned = KnownDifferences.Value
            .Where(e => !frames.Any(f => e.StartsWith(f, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToList();
        Assert.IsEmpty(orphaned, $"{KnownDifferencesFile} lists frames without a reference decode:\n  {string.Join("\n  ", orphaned)}");

        var unexpected = current.Except(KnownDifferences.Value).Count();
        var stale = KnownDifferences.Value.Except(current).Count();
        Assert.IsTrue(unexpected == 0 && stale == 0,
            $"{KnownDifferencesFile} is out of date: {unexpected} new and {stale} stale entries (the failing per-frame tests list them); " +
            "run with GOLDEN_UPDATE=1 to regenerate it after a deliberate change");
    }

    // The comparer itself: change one libmbus field of a frame and exactly that difference must appear.
    [TestMethod]
    [DataRow("example_data_01", "0", "Value", "1389818", "example_data_01#0:Value numeric")]
    [DataRow("example_data_01", "4", "Value", "41.737435", "example_data_01#4:Value real")]
    [DataRow("example_data_01", "0", "Unit", "Energy (Wh)", "example_data_01#0:Scale scale")]
    [DataRow("example_data_01", "0", "Unit", "Energy (MJ)", "example_data_01#0:Quantity quantity")]
    [DataRow("example_data_01", "0", "Unit", "Energy (kWH)", "example_data_01#0:Quantity unmapped-unit")]
    [DataRow("example_data_01", "0", "Function", "Maximum value", "example_data_01#0:Function function")]
    [DataRow("example_data_01", "0", "StorageNumber", "1", "example_data_01#0:StorageNumber storage")]
    [DataRow("example_data_01", "0", "Tariff", "1", "example_data_01#0:Tariff tariff")]
    [DataRow("example_data_01", "0", "Device", "1", "example_data_01#0:Device device")]
    [DataRow("example_data_01", "slave", "Id", "3575846", "example_data_01#slave:Id id")]
    [DataRow("example_data_01", "slave", "Medium", "Water", "example_data_01#slave:Medium medium")]
    [DataRow("Elster-F2", "6", "Unit", "On time (days)", "Elster-F2#6:TimeUnit time-unit")]
    [DataRow("ACW_Itron-BM-plus-m", "2", "Unit", "Time Point (time & date)", "ACW_Itron-BM-plus-m#2:VifCode vif-code")]
    [DataRow("ACW_Itron-BM-plus-m", "8", "Value", "00 01 75 14", "ACW_Itron-BM-plus-m#8:ManufacturerData manufacturer-data")]
    [DataRow("ACW_Itron-BM-plus-m", "8", "Function", "More records follow", "ACW_Itron-BM-plus-m#8:MoreRecordsFollow more-records-follow")]
    [DataRow("manual_frame2", "1", "Value", "136", "manual_frame2#1:Value numeric")]
    [DataRow("manual_frame2", "0", "Unit", "10 l", "manual_frame2#0:Unit quantity")]
    [DataRow("manual_frame2", "0", "Function", "Stored value", "manual_frame2#0:Function function")]
    [DataRow("manual_frame2", "slave", "Medium", "Heat", "manual_frame2#slave:Medium medium")]
    [DataRow("ELV-Elvaco-CMa10", "1", "Unit", "1e-3  %RH", "ELV-Elvaco-CMa10#1:Scale scale")]
    [DataRow("ELV-Elvaco-CMa10", "1", "Unit", "1e-2  %rH", "ELV-Elvaco-CMa10#1:PlainTextUnit plain-text-unit")]
    public void Compare_OneLibmbusFieldChanged_ReportsExactlyThatDifference(string frame, string record, string field, string value, string expected)
    {
        var reference = LibmbusReference.Load(Corpus.XmlPath(Corpus.TestFrames, frame)!);
        var changed = record == "slave"
            ? reference with { Slave = field switch
            {
                "Id" => reference.Slave with { Id = value },
                "Medium" => reference.Slave with { Medium = value },
                _ => throw new ArgumentException(field),
            } }
            : reference with { Records = reference.Records.Select(r => r.Id != record ? r : field switch
            {
                "Value" => r with { Value = value },
                "Unit" => r with { Unit = value },
                "Function" => r with { Function = value },
                "StorageNumber" => r with { StorageNumber = value },
                "Tariff" => r with { Tariff = value },
                "Device" => r with { Device = value },
                _ => throw new ArgumentException(field),
            }).ToList() };
        var bytes = Corpus.ReadHex(Corpus.TestFrames, frame);

        var before = LibmbusComparer.Compare(frame, bytes, reference).Differences.Select(d => d.Entry);
        var after = LibmbusComparer.Compare(frame, bytes, changed).Differences.Select(d => d.Entry);

        CollectionAssert.AreEqual(new[] { expected }, after.Except(before).ToList());
        CollectionAssert.AreEqual(Array.Empty<string>(), before.Except(after).ToList());
    }

    [TestMethod]
    public void Compare_LibmbusHasAnExtraRecord_ReportsCountAndMissingRecord()
    {
        var reference = LibmbusReference.Load(Corpus.XmlPath(Corpus.TestFrames, "example_data_01")!);
        var extra = reference with { Records = [.. reference.Records, reference.Records[0] with { Id = "6" }] };

        var result = LibmbusComparer.Compare("example_data_01", Corpus.ReadHex(Corpus.TestFrames, "example_data_01"), extra);

        CollectionAssert.AreEqual(
            new[] { "example_data_01#frame:RecordCount record-count", "example_data_01#6:Missing missing-record" },
            result.Differences.Select(d => d.Entry).ToList());
    }

    // The value types and ValueError of the stage-1 contract do not occur in today's decode, so the corpus cannot
    // exercise these branches yet; a branch that wrongly matched would hide a real difference after the merge.
    public static IEnumerable<TestDataRow<(string Libmbus, object? Value, DataTypes Type, string? ValueError, string? Category)>> ContractValues() =>
    [
        Row("2014-03-13", new DateOnly(2014, 3, 13), DataTypes._16_Bit_Integer, null, null),
        Row("2014-03-13T11:11:00", new DateTime(2014, 3, 13, 11, 11, 0), DataTypes._32_Bit_Integer, null, null),
        Row("2014-03-13T11:11:00", new DateTime(2014, 3, 13, 11, 12, 0), DataTypes._32_Bit_Integer, null, "date"),
        Row("2014-03-13", new DateTime(2014, 3, 13), DataTypes._32_Bit_Integer, null, "type"),
        Row("2014-03-13T11:11:00", new DateOnly(2014, 3, 13), DataTypes._16_Bit_Integer, null, "type"),
        Row("2014-03-13T11:11:00", new TimeOnly(11, 11), DataTypes._24_Bit_Integer, null, "type"),
        Row("2014-03-13T11:11:00", 332204811, DataTypes._32_Bit_Integer, null, "date-not-decoded"),
        Row("2000-00-00", null, DataTypes._16_Bit_Integer, "INVALID_DATE", "invalid-date"),
        Row("1234", null, DataTypes._24_Bit_Integer, "INVALID_DATE", "invalid-date"),
        Row("1389818", 1389818L, DataTypes._8_digit_BCD, null, null),
        Row("1500018", -18L, DataTypes._6_digit_BCD, null, "negative-bcd"),
        Row("144445223", null, DataTypes._8_digit_BCD, "INVALID_BCD", "invalid-bcd"),
        Row("-5", 5, DataTypes._32_Bit_Integer, null, "sign"),
        Row("-12.500000", -12.5f, DataTypes._32_Bit_Real, null, null),
        Row("12345678", "12345678", DataTypes._variable_length, null, null),
        Row("12345678", "87654321", DataTypes._variable_length, null, "string-reversed"),
        Row("12", new byte[] { 0x12 }, DataTypes._variable_length, null, null),
        Row("01 02", new byte[] { 0x02, 0x01 }, DataTypes._variable_length, null, "binary-order"),
        Row("", null, DataTypes._No_data, null, null),
    ];

    private static TestDataRow<(string, object?, DataTypes, string?, string?)> Row(string libmbus, object? value, DataTypes type, string? valueError, string? category) =>
        new((libmbus, value, type, valueError, category))
        {
            DisplayName = FormattableString.Invariant($"'{libmbus}' vs {value?.GetType().Name ?? "null"} {value} {valueError} -> {category ?? "match"}"),
        };

    [TestMethod]
    [DynamicData(nameof(ContractValues))]
    public void ValueCategory_ContractValue_ReportsCategory(string libmbus, object? value, DataTypes type, string? valueError, string? category)
    {
        var record = new DataRecord(default, Function.Instantaneous, 0, 0, 0, type, value, ImmutableArray<UnitInfo>.Empty);

        Assert.AreEqual(category, LibmbusComparer.ValueCategory(libmbus, record, valueError));
    }

    [TestMethod]
    [DataRow("Volume (1e-2  m^3)", VariableDataQuantityUnit.Volume_m3, -2, null)]
    [DataRow("Energy (10 kWh)", VariableDataQuantityUnit.EnergyWh, 4, null)]
    [DataRow("Energy (MJ)", VariableDataQuantityUnit.EnergyJ, 6, null)]
    [DataRow("Volume flow (m m^3/h)", VariableDataQuantityUnit.VolumeFlowM3_per_h, -3, null)]
    [DataRow("Volume (my m^3)", VariableDataQuantityUnit.Volume_m3, -6, null)]
    [DataRow("Temperature Difference ( deg C)", VariableDataQuantityUnit.TemperatureDifferenceK, 0, null)]
    [DataRow("On time (hours)", VariableDataQuantityUnit.OnTime, 0, "h")]
    [DataRow("Averaging Duration (seconds)", VariableDataQuantityUnit.AveragingDuration, 0, "s")]
    [DataRow("m A", VariableDataQuantityUnit.Amperes, -3, null)]
    [DataRow(" V", VariableDataQuantityUnit.Volts, 0, null)]
    [DataRow("Fabrication number", VariableDataQuantityUnit.FabricationNo, 0, null)]
    public void LibmbusText_Unit_ParsesQuantityScaleAndTimeUnit(string text, VariableDataQuantityUnit quantity, int exponent, string? timeUnit)
    {
        var unit = LibmbusText.Unit(text);

        Assert.IsNotNull(unit);
        Assert.AreEqual(quantity, unit.Quantity);
        Assert.AreEqual(exponent, unit.Exponent);
        Assert.AreEqual(timeUnit, unit.TimeUnit);
    }

    [TestMethod]
    [DataRow("Unrecognized VIF extension: 0x67", "67h")]
    [DataRow("Unknown (VIF=0x7B)", "7Bh")]
    public void LibmbusText_Unit_UnknownCode_KeepsTheCode(string text, string code)
    {
        Assert.AreEqual(code, LibmbusText.Unit(text)?.VifCode);
    }

    [TestMethod]
    [DataRow("l", FixedDataUnits.l)]
    [DataRow("10 kWh", FixedDataUnits.kWh10)]
    [DataRow("100 m^3/h", FixedDataUnits.m3_per_h100)]
    [DataRow("reserved but historic", FixedDataUnits.sameButHistoric)]
    public void LibmbusText_FixedUnit_ParsesUnit(string text, FixedDataUnits expected)
    {
        Assert.AreEqual(expected, LibmbusText.FixedUnit(text));
    }

    private static GoldenFrameResult Compare(string name) =>
        LibmbusComparer.Compare(
            name,
            Corpus.ReadHex(Corpus.TestFrames, name),
            LibmbusReference.Load(Corpus.XmlPath(Corpus.TestFrames, name)!));

    private static string Summary(IReadOnlyList<GoldenFrameResult> results)
    {
        int Sum(Func<GoldenFrameResult, int> f) => results.Sum(f);
        var differences = results.SelectMany(r => r.Differences).ToList();
        var categories = differences
            .GroupBy(d => d.Category)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => $"{g.Key} {g.Count()}");
        return $"""
            libmbus parity over {results.Count} reference frames:
              frames fully matching:  {results.Count(r => r.Differences.Count == 0)}/{results.Count}
              slave info matching:    {results.Count(r => r.SlaveMatches)}/{results.Count}
              records fully matching: {Sum(r => r.RecordsMatching)}/{Sum(r => r.Records)}
              values matching:        {Sum(r => r.ValuesMatching)}/{Sum(r => r.Values)}
              known differences:      {differences.Count} ({string.Join(", ", categories)})
            """;
    }

    // GitHub Actions shows the step summary on the run page, so the parity numbers are visible without opening logs.
    private static void WriteCiSummary(string summary)
    {
        var path = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(path))
            File.AppendAllText(path, $"```\n{summary}\n```\n");
    }

    private static readonly string[] Header =
    [
        "# Fields where the library's decode differs from the libmbus reference in DataExamples/test-frames/*.xml.",
        "# One \"<frame>#<record>:<field> <category>\" per line. <record> is the libmbus DataRecord id, \"slave\" or \"frame\".",
        "# GoldenLibmbusTests fails on a difference missing here and on an entry that no longer occurs.",
        "# Regenerate after a deliberate change and review the diff:",
        "#   GOLDEN_UPDATE=1 dotnet test --project tests/Valley.Net.Protocols.MeterBus.Tests/Valley.Net.Protocols.MeterBus.Tests.csproj",
    ];

    private static HashSet<string> ReadKnownDifferences(string path) =>
        File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            .ToHashSet(StringComparer.Ordinal);

    // The csproj copies the file next to the test assembly; the source copy is the fallback when running from the IDE
    // without a build.
    private static string KnownDifferencesPath()
    {
        var output = Path.Combine(AppContext.BaseDirectory, "Golden", KnownDifferencesFile);
        return File.Exists(output) ? output : SourceKnownDifferencesPath();
    }

    private static string SourceKnownDifferencesPath([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "Golden", KnownDifferencesFile);
}
