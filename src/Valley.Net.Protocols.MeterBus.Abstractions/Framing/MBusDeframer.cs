using System.Buffers;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Finds validated EN 13757-2 link-layer frames in received bytes: an ACK (E5), a short frame
/// (10 C A CS 16) or a control/long frame (68 L L 68 C A CI ... CS 16). A candidate whose header,
/// checksum or stop byte is wrong is not a frame: only its first byte is skipped, so a stray
/// start byte in line noise never swallows the real frame behind it.
/// </summary>
public static class MBusDeframer
{
    /// <summary>
    /// The longest frame: L = 255 bytes of C, A, CI and data, plus 68 L L 68 and CS 16.
    /// </summary>
    public const int MaxFrameLength = byte.MaxValue + MBusConstants.FRAME_FIXED_SIZE_LONG;

    private static ReadOnlySpan<byte> FrameStartBytes =>
        [MBusConstants.FRAME_ACK_START, MBusConstants.FRAME_SHORT_START, MBusConstants.FRAME_LONG_START];

    /// <summary>
    /// Finds the first valid frame in <paramref name="buffer"/>, skipping bytes that cannot start one.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> with <paramref name="buffer"/> starting after the frame;
    /// <see langword="false"/> with <paramref name="buffer"/> starting at a partial frame that needs
    /// more bytes (always shorter than <see cref="MaxFrameLength"/>), or empty. Bytes skipped before
    /// that position are noise and can be consumed.
    /// </returns>
    public static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> frame)
    {
        Span<byte> scratch = stackalloc byte[MaxFrameLength];

        while (true)
        {
            var reader = new SequenceReader<byte>(buffer);
            if (!reader.TryAdvanceToAny(FrameStartBytes, advancePastDelimiter: false))
            {
                buffer = buffer.Slice(buffer.End);
                frame = default;
                return false;
            }

            buffer = buffer.Slice(reader.Position);

            var frameLength = GetFrameLength(Head(buffer, scratch));
            if (frameLength == 0)
            {
                frame = default;
                return false;
            }

            if (frameLength < 0)
            {
                buffer = buffer.Slice(1);
                continue;
            }

            frame = buffer.Slice(0, frameLength);
            buffer = buffer.Slice(frameLength);
            return true;
        }
    }

    /// <inheritdoc cref="TryReadFrame(ref ReadOnlySequence{byte}, out ReadOnlySequence{byte})"/>
    public static bool TryReadFrame(ref ReadOnlySpan<byte> buffer, out ReadOnlySpan<byte> frame)
    {
        while (true)
        {
            var start = buffer.IndexOfAny(FrameStartBytes);
            if (start < 0)
            {
                buffer = buffer[buffer.Length..];
                frame = default;
                return false;
            }

            buffer = buffer[start..];

            var frameLength = GetFrameLength(buffer);
            if (frameLength == 0)
            {
                frame = default;
                return false;
            }

            if (frameLength < 0)
            {
                buffer = buffer[1..];
                continue;
            }

            frame = buffer[..frameLength];
            buffer = buffer[frameLength..];
            return true;
        }
    }

    /// <summary>
    /// The first bytes of <paramref name="buffer"/>, up to one maximum frame, as one span.
    /// They are copied only when they straddle a segment boundary.
    /// </summary>
    private static ReadOnlySpan<byte> Head(ReadOnlySequence<byte> buffer, Span<byte> scratch)
    {
        var length = (int)Math.Min(buffer.Length, MaxFrameLength);
        if (buffer.FirstSpan.Length >= length)
            return buffer.FirstSpan[..length];

        buffer.Slice(0, length).CopyTo(scratch);
        return scratch[..length];
    }

    /// <summary>
    /// Returns the length of the valid frame at the start of <paramref name="data"/>,
    /// 0 when more bytes are needed to tell, or -1 when it cannot be a valid frame.
    /// Each check runs as soon as its byte is there, so a bad header is rejected without
    /// waiting for the L + 6 bytes it announces.
    /// </summary>
    private static int GetFrameLength(ReadOnlySpan<byte> data)
    {
        switch (data[0])
        {
            case MBusConstants.FRAME_ACK_START:
                return 1;

            case MBusConstants.FRAME_SHORT_START:
                if (data.Length < MBusConstants.FRAME_FIXED_SIZE_SHORT)
                    return 0;

                return data[3] == Checksum(data.Slice(1, 2)) && data[4] == MBusConstants.FRAME_STOP
                    ? MBusConstants.FRAME_FIXED_SIZE_SHORT
                    : -1;

            case MBusConstants.FRAME_LONG_START:
                // L covers at least C, A and CI
                if (data.Length < 2)
                    return 0;
                var length = data[1];
                if (length < 3)
                    return -1;

                if (data.Length < 3)
                    return 0;
                if (data[2] != length)
                    return -1;

                if (data.Length < 4)
                    return 0;
                if (data[3] != MBusConstants.FRAME_LONG_START)
                    return -1;

                var frameLength = length + MBusConstants.FRAME_FIXED_SIZE_LONG;
                if (data.Length < frameLength)
                    return 0;

                return data[frameLength - 2] == Checksum(data.Slice(4, length)) && data[frameLength - 1] == MBusConstants.FRAME_STOP
                    ? frameLength
                    : -1;

            default:
                return -1;
        }
    }

    private static byte Checksum(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        foreach (var b in data)
            sum += b;
        return sum;
    }
}
