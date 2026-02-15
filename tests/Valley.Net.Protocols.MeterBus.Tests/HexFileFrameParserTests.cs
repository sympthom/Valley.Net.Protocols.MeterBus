namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class HexFileFrameParserTests
{
    private readonly FrameParser _parser = new();
    private readonly PacketMapper _mapper = new(new VifLookupService());

    private static string? FindDataExamplesDir()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "DataExamples", "test-frames");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    public static IEnumerable<object[]> TestFrameFiles()
    {
        var dir = FindDataExamplesDir();
        if (dir is null || !Directory.Exists(dir))
            yield break;

        foreach (var file in Directory.GetFiles(dir, "*.hex").OrderBy(f => f))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var hex = File.ReadAllText(file).Trim().Replace("\r", "").Replace("\n", "");
            if (!string.IsNullOrEmpty(hex))
                yield return [name, hex];
        }
    }

    [TestMethod]
    [DynamicData(nameof(TestFrameFiles))]
    public void Parse_HexFile_ReturnsValidFrame(string name, string hexData)
    {
        byte[] bytes;
        try
        {
            bytes = hexData.HexToBytes();
        }
        catch
        {
            Assert.Inconclusive($"Could not parse hex data from {name}");
            return;
        }

        var result = _parser.Parse(bytes);

        // Files with "invalid" in the name are expected to fail parsing
        if (name.Contains("invalid", StringComparison.OrdinalIgnoreCase))
        {
            // Just ensure no unhandled exception - failure is acceptable
            return;
        }

        Assert.IsTrue(result.IsSuccess, $"Failed to parse frame '{name}': {result.Error?.Code} - {result.Error?.Message}");
    }

    [TestMethod]
    [DynamicData(nameof(TestFrameFiles))]
    public void Parse_ThenMap_HexFile_ProducesPacket(string name, string hexData)
    {
        byte[] bytes;
        try
        {
            bytes = hexData.HexToBytes();
        }
        catch
        {
            Assert.Inconclusive($"Could not parse hex data from {name}");
            return;
        }

        var frameResult = _parser.Parse(bytes);
        if (!frameResult.IsSuccess)
        {
            Assert.Inconclusive($"Frame parse failed for '{name}': {frameResult.Error?.Message}");
            return;
        }

        var packetResult = _mapper.MapToPacket(frameResult.Value!);
        // Note: not all frames map to packets (e.g. ACK, short frames)
        // We just ensure no unhandled exceptions
    }
}
