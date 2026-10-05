namespace Valley.Net.Protocols.MeterBus.Tests;

/// <summary>
/// The DataExamples meter frames: test-frames (with libmbus .xml reference decodes), error-frames and unsupported-frames.
/// </summary>
internal static class Corpus
{
    public const string TestFrames = "test-frames";
    public const string ErrorFrames = "error-frames";
    public const string UnsupportedFrames = "unsupported-frames";

    private static readonly Lazy<string> Root = new(FindRoot);

    // The csproj copies DataExamples next to the test assembly. Walking up also finds the
    // repository copy when the tests run from an output folder built without it.
    private static string FindRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "DataExamples");
            if (Directory.Exists(Path.Combine(candidate, TestFrames)))
                return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new DirectoryNotFoundException(
            $"DataExamples/{TestFrames} not found in {AppContext.BaseDirectory} or any parent directory");
    }

    public static string FolderPath(string folder) => Path.Combine(Root.Value, folder);

    /// <summary>
    /// Frame names (file name without .hex) in a corpus folder, in ordinal order so test lists are stable.
    /// </summary>
    public static IReadOnlyList<string> Names(string folder)
    {
        var names = Directory.GetFiles(FolderPath(folder), "*.hex")
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (names.Count == 0)
            throw new InvalidOperationException($"No .hex files in {FolderPath(folder)}");
        return names;
    }

    // Convert.FromHexString rejects odd-length and non-hex input, so a corrupt data file fails the test.
    public static byte[] ReadHex(string folder, string name)
    {
        var text = File.ReadAllText(Path.Combine(FolderPath(folder), name + ".hex"));
        return Convert.FromHexString(string.Concat(text.Where(c => !char.IsWhiteSpace(c))));
    }

    public static string? XmlPath(string folder, string name)
    {
        var path = Path.Combine(FolderPath(folder), name + ".xml");
        return File.Exists(path) ? path : null;
    }
}
