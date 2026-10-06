using System.Buffers;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class MBusDeframerTests
{
    private const string Ack = "E5";
    private const string ShortFrame = "10 5B 01 5C 16";
    // SND_UD control frame: 68 L L 68 C A CI CS 16
    private const string ControlFrame = "68 03 03 68 53 FE 51 A2 16";
    // RSP_UD from address 1, CI 0x72, two data bytes; checksum 08+01+72+AB+CD = F3
    private const string LongFrame = "68 05 05 68 08 01 72 AB CD F3 16";

    [TestMethod]
    [DataRow(Ack, DisplayName = "ACK")]
    [DataRow(ShortFrame, DisplayName = "Short frame")]
    [DataRow(ControlFrame, DisplayName = "Control frame")]
    [DataRow(LongFrame, DisplayName = "Long frame")]
    public void TryReadFrame_OneFrame_ReturnsItAndConsumesAll(string frame)
    {
        AssertReads(frame, [frame], remaining: "");
    }

    // Each candidate starts with a start byte but fails one check. Only that byte may be skipped, so the
    // ACK behind it must still come out and nothing of the candidate may be returned.
    [TestMethod]
    [DataRow("10 5B 01 5D 16", DisplayName = "Short frame checksum wrong")]
    [DataRow("10 5B 01 5C 17", DisplayName = "Short frame stop byte wrong")]
    [DataRow("68 02 02 68 08 01 09 16", DisplayName = "L below 3")]
    [DataRow("68 05 07 68 08 01 72 00 00 7B 16", DisplayName = "L differs from L'")]
    [DataRow("68 05 05 69 08 01 72 AB CD F3 16", DisplayName = "Second start byte wrong")]
    [DataRow("68 05 05 68 08 01 72 AB CD F4 16", DisplayName = "Long frame checksum wrong")]
    [DataRow("68 05 05 68 08 01 72 AB CD F3 17", DisplayName = "Long frame stop byte wrong")]
    [DataRow("68 03 03 68 53 FE 51 A3 16", DisplayName = "Control frame checksum wrong")]
    [DataRow("68 03 03 68 53 FE 51 A2 00", DisplayName = "Control frame stop byte wrong")]
    public void TryReadFrame_InvalidCandidate_SkipsItAndFindsFollowingFrame(string candidate)
    {
        AssertReads(candidate, [], remaining: "");
        AssertReads(candidate + " " + Ack, [Ack], remaining: "");
    }

    // A bad header is visible after four bytes; waiting for the L + 6 bytes it announces would swallow the reply behind it.
    [TestMethod]
    [DataRow("68 FF 10 5B 01 5C 16", DisplayName = "68 FF then short frame")]
    [DataRow("68 FF FF 00 10 5B 01 5C 16", DisplayName = "68 FF FF without second start then short frame")]
    [DataRow("10 5B 01 5C 10 5B 01 5C 16", DisplayName = "Short frame cut short then short frame")]
    public void TryReadFrame_StrayStartByteBeforeFrame_RejectsWithoutWaiting(string input)
    {
        AssertReads(input, [ShortFrame], remaining: "");
    }

    [TestMethod]
    public void TryReadFrame_GarbageBeforeAndBetweenFrames_ReturnsEveryFrame()
    {
        AssertReads(
            $"00 FF 33 {Ack} 01 02 {ShortFrame} 68 FF FE {ControlFrame} 10 99 {LongFrame} 7F",
            [Ack, ShortFrame, ControlFrame, LongFrame],
            remaining: "");
    }

    [TestMethod]
    public void TryReadFrame_BackToBackFrames_ReturnsEachInOrder()
    {
        AssertReads(
            $"{Ack} {ShortFrame} {ControlFrame} {LongFrame} {Ack}",
            [Ack, ShortFrame, ControlFrame, LongFrame, Ack],
            remaining: "");
    }

    [TestMethod]
    public void TryReadFrame_EveryPrefixOfFrame_KeepsPartialFrameAndDropsNoiseBeforeIt()
    {
        var frame = LongFrame.HexToBytes();
        for (var length = 1; length < frame.Length; length++)
        {
            var prefix = Convert.ToHexString(frame, 0, length);
            AssertReads("00 33 " + prefix, [], remaining: prefix);
        }
    }

    [TestMethod]
    public void TryReadFrame_MaximumFrame_ReturnsAll261Bytes()
    {
        var frame = BuildLongFrame(byte.MaxValue);
        Assert.HasCount(MBusDeframer.MaxFrameLength, frame);

        AssertReads("00 " + Convert.ToHexString(frame) + " E5", [Convert.ToHexString(frame), Ack], remaining: "");
    }

    [TestMethod]
    public void TryReadFrame_MaximumFrameOneByteShort_KeepsAllOfIt()
    {
        var partial = Convert.ToHexString(BuildLongFrame(byte.MaxValue).AsSpan(0, MBusDeframer.MaxFrameLength - 1));

        AssertReads(partial, [], remaining: partial);
    }

    [TestMethod]
    public void TryReadFrame_ContinuousJunk_KeepsLessThanOneFrame()
    {
        // Contains every start byte, but never a valid header, checksum or stop byte
        var junk = new byte[16 * 1024];
        ReadOnlySpan<byte> pattern = [0x00, 0x68, 0xFF, 0x10, 0x55, 0xE6];
        for (var i = 0; i < junk.Length; i++)
            junk[i] = pattern[i % pattern.Length];

        var sequence = Segmented(junk, 4096);
        Assert.IsFalse(MBusDeframer.TryReadFrame(ref sequence, out _));
        Assert.IsLessThan(MBusDeframer.MaxFrameLength, sequence.Length);

        ReadOnlySpan<byte> span = junk;
        Assert.IsFalse(MBusDeframer.TryReadFrame(ref span, out _));
        Assert.IsLessThan(MBusDeframer.MaxFrameLength, span.Length);
    }

    [TestMethod]
    public void TryReadFrame_FrameSplitAtEveryPoint_ReturnsSameFrames()
    {
        var input = $"00 {ShortFrame} 68 FF {LongFrame} {Ack}".HexToBytes();
        for (var cut = 1; cut < input.Length; cut++)
        {
            var sequence = Split(input, cut);
            CollectionAssert.AreEqual(
                new[] { Hex(ShortFrame), Hex(LongFrame), Ack },
                ReadAll(ref sequence),
                $"Split at {cut}");
        }
    }

    public static IEnumerable<object[]> CorpusFrames() =>
        new[] { Corpus.TestFrames, Corpus.ErrorFrames, Corpus.UnsupportedFrames }
            .SelectMany(folder => Corpus.Names(folder).Select(name => new object[] { folder, name }));

    // The deframer and FrameParser must agree on every recorded frame, even with noise around it.
    [TestMethod]
    [DynamicData(nameof(CorpusFrames))]
    public void TryReadFrame_CorpusFrameInNoise_FindsExactlyTheFramesFrameParserAccepts(string folder, string name)
    {
        var frame = Corpus.ReadHex(folder, name);
        var parsed = new FrameParser().Parse(frame);

        var input = "00 68 FF".HexToBytes().Concat(frame).Concat("33".HexToBytes()).ToArray();
        var sequence = Segmented(input, 7);
        var frames = ReadAll(ref sequence);

        if (parsed.IsSuccess)
            CollectionAssert.AreEqual(new[] { Convert.ToHexString(frame) }, frames);
        else
            Assert.DoesNotContain(Convert.ToHexString(frame), frames, $"Rejected by FrameParser ({parsed.Error?.Code}) but deframed");
    }

    /// <summary>
    /// Reads <paramref name="input"/> as one span, as one segment and as one-byte segments,
    /// and checks each gives the same frames and leaves the same partial frame.
    /// </summary>
    private static void AssertReads(string input, string[] expectedFrames, string remaining)
    {
        var bytes = input.HexToBytes();
        var expected = expectedFrames.Select(Hex).ToArray();

        ReadOnlySpan<byte> span = bytes;
        var spanFrames = new List<string>();
        while (MBusDeframer.TryReadFrame(ref span, out var frame))
            spanFrames.Add(Convert.ToHexString(frame));
        CollectionAssert.AreEqual(expected, spanFrames, "span");
        Assert.AreEqual(Hex(remaining), Convert.ToHexString(span), "span remaining");

        foreach (var (label, sequence) in new[] { ("one segment", new ReadOnlySequence<byte>(bytes)), ("one-byte segments", Segmented(bytes, 1)) })
        {
            var buffer = sequence;
            CollectionAssert.AreEqual(expected, ReadAll(ref buffer), label);
            Assert.AreEqual(Hex(remaining), Convert.ToHexString(buffer.ToArray()), $"{label} remaining");
        }
    }

    private static List<string> ReadAll(ref ReadOnlySequence<byte> buffer)
    {
        var frames = new List<string>();
        while (MBusDeframer.TryReadFrame(ref buffer, out var frame))
            frames.Add(Convert.ToHexString(frame.ToArray()));
        return frames;
    }

    private static byte[] BuildLongFrame(byte length)
    {
        var frame = new byte[length + MBusConstants.FRAME_FIXED_SIZE_LONG];
        frame[0] = frame[3] = MBusConstants.FRAME_LONG_START;
        frame[1] = frame[2] = length;
        frame[4] = 0x08;
        frame[5] = 0x01;
        frame[6] = 0x72;
        for (var i = 7; i < length + 4; i++)
            frame[i] = (byte)i;

        byte checksum = 0;
        for (var i = 4; i < length + 4; i++)
            checksum += frame[i];
        frame[^2] = checksum;
        frame[^1] = MBusConstants.FRAME_STOP;
        return frame;
    }

    private static string Hex(string spaced) => spaced.Replace(" ", "");

    private static ReadOnlySequence<byte> Segmented(byte[] data, int segmentLength) =>
        Split(data, Enumerable.Range(1, (data.Length - 1) / segmentLength).Select(i => i * segmentLength).ToArray());

    private static ReadOnlySequence<byte> Split(byte[] data, params int[] cuts)
    {
        if (data.Length == 0)
            return ReadOnlySequence<byte>.Empty;

        var bounds = cuts.Prepend(0).Append(data.Length).ToArray();
        var first = new Segment(data.AsMemory(0, bounds[1]), 0);
        var last = first;
        for (var i = 1; i < bounds.Length - 1; i++)
            last = last.Append(data.AsMemory(bounds[i], bounds[i + 1] - bounds[i]));

        return new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, long runningIndex)
        {
            Memory = memory;
            RunningIndex = runningIndex;
        }

        public Segment Append(ReadOnlyMemory<byte> memory)
        {
            var next = new Segment(memory, RunningIndex + Memory.Length);
            Next = next;
            return next;
        }
    }
}
