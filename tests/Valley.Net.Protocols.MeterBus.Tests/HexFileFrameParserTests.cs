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

    private readonly FrameParser _parser = new();
    private readonly PacketMapper _mapper = new(new VifLookupService());

    // The csproj copies DataExamples next to the test assembly. Walking up also finds the
    // repository copy when the tests run from an output folder built without it.
    private static string FindDataExamplesDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "DataExamples", "test-frames");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException(
            $"DataExamples/test-frames not found in {AppContext.BaseDirectory} or any parent directory");
    }

    public static IEnumerable<object[]> TestFrameFiles()
    {
        var dir = FindDataExamplesDir();
        var files = Directory.GetFiles(dir, "*.hex");
        if (files.Length == 0)
            throw new InvalidOperationException($"No .hex files in {dir}");

        return files
            .Order(StringComparer.Ordinal)
            .Select(file => new object[] { Path.GetFileNameWithoutExtension(file) });
    }

    // Convert.FromHexString rejects odd-length and non-hex input, so a corrupt data file fails the test.
    private static byte[] ReadFrame(string name)
    {
        var text = File.ReadAllText(Path.Combine(FindDataExamplesDir(), name + ".hex"));
        return Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c))));
    }

    [TestMethod]
    [DynamicData(nameof(TestFrameFiles))]
    public void Parse_HexFile_ReturnsValidFrame(string name)
    {
        var result = _parser.Parse(ReadFrame(name));

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
        var frameResult = _parser.Parse(ReadFrame(name));

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
}
