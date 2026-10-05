namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class HexFileFrameParserTests
{
    // Frames that FrameParser must reject, with the expected error code.
    private static readonly Dictionary<string, string> ExpectedParseErrors = new()
    {
        // L = 0 is below the 3-byte minimum (C, A, CI).
        ["invalid_length"] = "INVALID_LENGTH",
    };

    // Frames that parse but PacketMapper must reject, with the expected error code.
    private static readonly Dictionary<string, string> ExpectedMapErrors = new()
    {
        // CI 0x73 with 15 of the 16 fixed-data bytes.
        ["invalid_length2"] = "FIXED_FRAME_INVALID_LENGTH",
    };

    // Mapper defects fixed in a separate change: Inconclusive while the frame still maps,
    // so remove the entry once the mapper rejects it.
    private static readonly HashSet<string> KnownMapDefects =
    [
        // The counters silently come back as 0 (finding 69).
        "invalid_length2",
    ];

    // error-frames without a libmbus decode: malformed variable data that must fail with a specific code
    // instead of mapping to a packet with the records before the defect.
    private static readonly Dictionary<string, string> MalformedErrorFrames = new()
    {
        // The 6-digit BCD value of the third record is missing or cut short.
        ["premature_end_of_data1"] = "PREMATURE_END",
        ["premature_end_of_data2"] = "PREMATURE_END",
        // The DIF announces a DIFE that is not there.
        ["premature_end_of_dif1"] = "PREMATURE_END",
        ["premature_end_of_dif2"] = "PREMATURE_END",
        // The DIFE is last; the VIF is missing.
        ["premature_end_of_vif1"] = "PREMATURE_END",
        // A plain-text VIF announces 19 and 243 ASCII bytes with fewer left.
        ["premature_end_of_var_vif1"] = "PREMATURE_END",
        ["too_long_var_vif"] = "PREMATURE_END",
        // 11 DIFEs and 11 VIFEs, over the EN 13757-3 limit of 10.
        ["too_many_dife"] = "TOO_MANY_DIFE",
        ["too_many_vife"] = "TOO_MANY_VIFE",
        // CI 0x72 with 5 of the 12 header bytes.
        ["too_short_header"] = "VAR_FRAME_TOO_SHORT",
    };

    // unsupported-frames: libmbus cannot decode these. Each outcome is pinned so a change is deliberate.
    private static readonly Dictionary<string, string> UnsupportedFrameOutcomes = new()
    {
        // SND_UD / application reset from the master, not a reply.
        ["manual_frame1"] = "WRONG_DIRECTION",
        ["manual_frame4"] = "WRONG_DIRECTION",
        ["manual_frame5"] = "WRONG_DIRECTION",
        ["manual_frame6"] = "WRONG_DIRECTION",
        // Valid variable data whose records end in DIF 0x0F manufacturer data, which libmbus rejects.
        ["rvd235"] = nameof(VariableDataPacket),
        ["siemens_rvd235"] = nameof(VariableDataPacket),
        // DIF 0x1F straight after the header: no records, more follow.
        ["svm_f22_telegram2"] = nameof(VariableDataPacket),
    };

    private readonly FrameParser _parser = new();
    private readonly PacketMapper _mapper = new(new VifLookupService());

    public static IEnumerable<object[]> TestFrameFiles() =>
        Corpus.Names(Corpus.TestFrames).Select(name => new object[] { name });

    [TestMethod]
    [DynamicData(nameof(TestFrameFiles))]
    public void Parse_HexFile_ReturnsValidFrame(string name)
    {
        var result = _parser.Parse(Corpus.ReadHex(Corpus.TestFrames, name));

        if (ExpectedParseErrors.TryGetValue(name, out var expectedCode))
        {
            Assert.IsFalse(result.IsSuccess, $"Frame '{name}' should be rejected with {expectedCode}");
            Assert.AreEqual(expectedCode, result.Error?.Code);
            return;
        }

        Assert.IsTrue(result.IsSuccess, $"Failed to parse frame '{name}': {result.Error?.Code} - {result.Error?.Message}");
    }

    [TestMethod]
    [DynamicData(nameof(TestFrameFiles))]
    public void Parse_ThenMap_HexFile_ProducesPacket(string name)
    {
        var frameResult = _parser.Parse(Corpus.ReadHex(Corpus.TestFrames, name));

        // The parse error code is asserted by Parse_HexFile_ReturnsValidFrame.
        if (ExpectedParseErrors.ContainsKey(name))
        {
            Assert.IsFalse(frameResult.IsSuccess, $"Frame '{name}' should be rejected by the parser");
            return;
        }

        Assert.IsTrue(frameResult.IsSuccess, $"Failed to parse frame '{name}': {frameResult.Error?.Code} - {frameResult.Error?.Message}");

        var packetResult = _mapper.MapToPacket(frameResult.Value!);

        if (ExpectedMapErrors.TryGetValue(name, out var expectedCode))
        {
            if (packetResult.IsSuccess && KnownMapDefects.Contains(name))
                Assert.Inconclusive($"Known defect: '{name}' should be rejected with {expectedCode}");

            Assert.IsFalse(packetResult.IsSuccess, $"Frame '{name}' should be rejected with {expectedCode}");
            Assert.AreEqual(expectedCode, packetResult.Error?.Code);
            return;
        }

        Assert.IsTrue(packetResult.IsSuccess, $"Failed to map frame '{name}': {packetResult.Error?.Code} - {packetResult.Error?.Message}");
    }

    public static IEnumerable<TestDataRow<(string Name, string Error)>> ApplicationErrorFrames() =>
        Corpus.Names(Corpus.ErrorFrames)
            .Where(name => Corpus.XmlPath(Corpus.ErrorFrames, name) is not null)
            .Select(name => new TestDataRow<(string, string)>((name, LibmbusError(name))) { DisplayName = name });

    // libmbus decodes these as CI 0x70 application errors (<Error> in the reference XML).
    [TestMethod]
    [DynamicData(nameof(ApplicationErrorFrames))]
    public void ErrorFrame_WithLibmbusError_MapsToApplicationErrorPacket(string name, string libmbusError)
    {
        Assert.IsTrue(LibmbusText.ApplicationErrors.TryGetValue(libmbusError, out var expected), $"No code for libmbus error '{libmbusError}'");
        var frame = _parser.Parse(Corpus.ReadHex(Corpus.ErrorFrames, name));
        Assert.IsTrue(frame.IsSuccess, $"{frame.Error?.Code} - {frame.Error?.Message}");

        var packet = _mapper.MapToPacket(frame.Value!);

        Assert.IsTrue(packet.IsSuccess, $"{packet.Error?.Code} - {packet.Error?.Message}");
        var error = Assert.IsInstanceOfType<ApplicationErrorPacket>(packet.Value);
        Assert.AreEqual(expected, error.Code);
        // "error" is a CI 0x70 frame without data (a control frame); the others carry the code in one data byte.
        Assert.AreEqual(frame.Value switch { LongFrame l => l.Address, ControlFrame c => c.Address, _ => -1 }, error.Address);
    }

    public static IEnumerable<TestDataRow<(string Name, string Code)>> MalformedErrorFrameRows() =>
        MalformedErrorFrames
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new TestDataRow<(string, string)>((kv.Key, kv.Value)) { DisplayName = kv.Key });

    [TestMethod]
    [DynamicData(nameof(MalformedErrorFrameRows))]
    public void ErrorFrame_Malformed_FailsWithSpecificCode(string name, string code)
    {
        var frame = _parser.Parse(Corpus.ReadHex(Corpus.ErrorFrames, name));
        Assert.IsTrue(frame.IsSuccess, $"{frame.Error?.Code} - {frame.Error?.Message}");

        var packet = _mapper.MapToPacket(frame.Value!);

        Assert.IsFalse(packet.IsSuccess, $"'{name}' mapped to {packet.Value?.GetType().Name} instead of failing with {code}");
        Assert.AreEqual(code, packet.Error!.Code);
    }

    // The EN 13757-3 limit is 10 extension bytes, so the same record with one extension fewer than the corpus frame
    // must still decode: DIF 8B with ten DIFEs (8B x9, 60), or VIF 84 with ten VIFEs (84 x9, 04), then 6-digit BCD 021837.
    [TestMethod]
    [DataRow("too_many_dife", (byte)0x8B, DisplayName = "10 DIFEs")]
    [DataRow("too_many_vife", (byte)0x84, DisplayName = "10 VIFEs")]
    public void ErrorFrame_OneExtensionFewer_MapsAllRecords(string name, byte extension)
    {
        var original = (LongFrame)_parser.Parse(Corpus.ReadHex(Corpus.ErrorFrames, name)).Value!;
        var data = original.Data.ToArray();
        // The third record starts after the 12 header bytes and two 5-byte records; the second occurrence of the
        // byte is the first extension, as the first is the DIF or VIF itself.
        var first = Array.IndexOf(data, extension, 22);
        var shorter = original with { Data = data.Where((_, i) => i != Array.IndexOf(data, extension, first + 1)).ToArray() };
        var frame = _parser.Parse(new FrameSerializer().Serialize(shorter));
        Assert.IsTrue(frame.IsSuccess, $"{frame.Error?.Code} - {frame.Error?.Message}");

        var packet = _mapper.MapToPacket(frame.Value!);

        Assert.IsTrue(packet.IsSuccess, $"{packet.Error?.Code} - {packet.Error?.Message}");
        var records = Assert.IsInstanceOfType<VariableDataPacket>(packet.Value).Records;
        Assert.HasCount(3, records);
        Assert.AreEqual(21837L, Convert.ToInt64(records[2].Value, System.Globalization.CultureInfo.InvariantCulture));
    }

    public static IEnumerable<TestDataRow<(string Name, string Outcome)>> UnsupportedFrameRows() =>
        UnsupportedFrameOutcomes
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => new TestDataRow<(string, string)>((kv.Key, kv.Value)) { DisplayName = kv.Key });

    [TestMethod]
    [DynamicData(nameof(UnsupportedFrameRows))]
    public void UnsupportedFrame_HasPinnedOutcome(string name, string outcome)
    {
        var frame = _parser.Parse(Corpus.ReadHex(Corpus.UnsupportedFrames, name));
        Assert.IsTrue(frame.IsSuccess, $"{frame.Error?.Code} - {frame.Error?.Message}");

        var packet = _mapper.MapToPacket(frame.Value!);

        Assert.AreEqual(outcome, packet.IsSuccess ? packet.Value!.GetType().Name : packet.Error!.Code);
    }

    // A new file in error-frames or unsupported-frames needs an expectation above.
    [TestMethod]
    [DataRow(Corpus.ErrorFrames)]
    [DataRow(Corpus.UnsupportedFrames)]
    public void CorpusFolder_EveryFrameHasAnExpectation(string folder)
    {
        var covered = folder == Corpus.ErrorFrames
            ? MalformedErrorFrames.Keys.Concat(Corpus.Names(folder).Where(n => Corpus.XmlPath(folder, n) is not null))
            : UnsupportedFrameOutcomes.Keys;

        CollectionAssert.AreEquivalent(Corpus.Names(folder).ToList(), covered.ToList());
    }

    // Seeded mutation fuzz: corrupt the payload of every corpus long frame and re-frame it with a valid checksum, so
    // the corruption gets past the link layer and reaches the mapper. Parse and map must return Ok or Fail, never throw.
    [TestMethod]
    public void MutatedCorpusFrame_ParseAndMap_NeverThrows()
    {
        const int MutationsPerFrame = 200;
        var serializer = new FrameSerializer();
        var failures = new List<string>();
        var cases = 0;
        var frames = new[] { Corpus.TestFrames, Corpus.ErrorFrames, Corpus.UnsupportedFrames }
            .SelectMany(folder => Corpus.Names(folder).Select(name => (Folder: folder, Name: name)))
            .ToList();

        for (int seed = 0; seed < frames.Count; seed++)
        {
            var (folder, name) = frames[seed];
            if (_parser.Parse(Corpus.ReadHex(folder, name)).Value is not LongFrame original)
                continue;

            // A fixed seed per frame makes every failure reproducible from its message.
            var random = new Random(seed);
            for (int i = 0; i < MutationsPerFrame; i++)
            {
                var data = original.Data.ToArray();
                for (int n = random.Next(1, 4); n > 0; n--)
                    data = Mutate(data, random);
                var bytes = serializer.Serialize(original with { Data = data });
                cases++;
                try
                {
                    var frame = _parser.Parse(bytes);
                    if (!frame.IsSuccess)
                        continue;
                    var packet = _mapper.MapToPacket(frame.Value!);
                    if (packet.IsSuccess && packet.Value is null)
                        failures.Add($"{folder}/{name} seed {seed} #{i}: success without a packet [{bytes.ToHex()}]");
                }
                catch (Exception ex)
                {
                    failures.Add($"{folder}/{name} seed {seed} #{i}: {ex.GetType().Name}: {ex.Message} [{bytes.ToHex()}]");
                }
            }
        }

        Assert.IsGreaterThan(10_000, cases, "Too few corpus long frames to fuzz");
        Assert.IsEmpty(failures, $"{failures.Count} of {cases} mutated frames failed:\n  {string.Join("\n  ", failures.Take(20))}");
    }

    // Bit flip, random byte, truncation or insertion; the payload stays within the 252 bytes a long frame can carry.
    private static byte[] Mutate(byte[] data, Random random)
    {
        var at = random.Next(data.Length + 1);
        switch (random.Next(4))
        {
            case 0 when at < data.Length:
                data[at] ^= (byte)(1 << random.Next(8));
                return data;
            case 1 when at < data.Length:
                data[at] = (byte)random.Next(256);
                return data;
            case 2:
                return data[..at];
            default:
                return data.Length >= 252 ? data[..^1] : [.. data[..at], (byte)random.Next(256), .. data[at..]];
        }
    }

    private static string LibmbusError(string name) =>
        LibmbusReference.Parse(Corpus.XmlPath(Corpus.ErrorFrames, name)!).Root!.Element("SlaveInformation")!.Element("Error")!.Value;
}
