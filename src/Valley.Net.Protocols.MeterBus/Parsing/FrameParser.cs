namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Parses raw M-Bus frame bytes into immutable frame records.
/// </summary>
public sealed class FrameParser : IFrameParser
{
    public MBusParseResult<MBusFrame> Parse(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return MBusParseResult<MBusFrame>.Fail("EMPTY", "No data to parse");

        return data[0] switch
        {
            MBusConstants.FRAME_ACK_START => MBusParseResult<MBusFrame>.Ok(new AckFrame()),
            MBusConstants.FRAME_SHORT_START => ParseShortFrame(data),
            MBusConstants.FRAME_LONG_START => ParseLongFrame(data),
            _ => MBusParseResult<MBusFrame>.Fail("UNKNOWN_START", $"Unknown start byte: 0x{data[0]:X2}")
        };
    }

    private static MBusParseResult<MBusFrame> ParseShortFrame(ReadOnlySpan<byte> data)
    {
        if (data.Length < MBusConstants.FRAME_FIXED_SIZE_SHORT)
            return MBusParseResult<MBusFrame>.Fail("SHORT_FRAME_TOO_SHORT", "Not enough data for short frame");

        var control = data[1];
        var address = data[2];
        var crc = data[3];
        var stop = data[4];

        if (crc != Checksum(data.Slice(1, 2)))
            return MBusParseResult<MBusFrame>.Fail("CRC_MISMATCH", "Short frame CRC mismatch");

        if (stop != MBusConstants.FRAME_STOP)
            return MBusParseResult<MBusFrame>.Fail("INVALID_STOP", "Short frame missing stop byte");

        return MBusParseResult<MBusFrame>.Ok(new ShortFrame((ControlMask)control, address, crc));
    }

    private static MBusParseResult<MBusFrame> ParseLongFrame(ReadOnlySpan<byte> data)
    {
        if (data.Length < 4)
            return MBusParseResult<MBusFrame>.Fail("LONG_FRAME_TOO_SHORT", "Not enough data for long frame header");

        var length1 = data[1];
        var length2 = data[2];

        if (length1 < 3)
            return MBusParseResult<MBusFrame>.Fail("INVALID_LENGTH", "Long frame length must be >= 3");

        if (length1 != length2)
            return MBusParseResult<MBusFrame>.Fail("LENGTH_MISMATCH", "Long frame length bytes do not match");

        var totalLength = length1 + MBusConstants.FRAME_FIXED_SIZE_LONG;
        if (data.Length < totalLength)
            return MBusParseResult<MBusFrame>.Fail("LONG_FRAME_INCOMPLETE", "Not enough data for complete long frame");

        var start2 = data[3];
        if (start2 != MBusConstants.FRAME_LONG_START)
            return MBusParseResult<MBusFrame>.Fail("INVALID_START2", "Long frame missing second start byte");

        var control = data[4];
        var address = data[5];
        var controlInformation = data[6];

        var dataLength = length1 - 3;
        var payload = data.Slice(7, dataLength);

        var crcStart = 4; // control, address, CI, plus data
        var crcLength = 3 + dataLength;
        var crc = data[4 + crcLength];
        var stop = data[4 + crcLength + 1];

        if (crc != Checksum(data.Slice(crcStart, crcLength)))
            return MBusParseResult<MBusFrame>.Fail("CRC_MISMATCH", "Long frame CRC mismatch");

        if (stop != MBusConstants.FRAME_STOP)
            return MBusParseResult<MBusFrame>.Fail("INVALID_STOP", "Long frame missing stop byte");

        if (dataLength == 0)
        {
            return MBusParseResult<MBusFrame>.Ok(new ControlFrame(
                (ControlMask)control,
                (ControlInformation)controlInformation,
                address,
                crc));
        }

        return MBusParseResult<MBusFrame>.Ok(new LongFrame(
            (ControlMask)control,
            (ControlInformation)controlInformation,
            address,
            payload.ToArray(),
            crc));
    }

    internal static byte Checksum(ReadOnlySpan<byte> data)
    {
        byte sum = 0;
        for (int i = 0; i < data.Length; i++)
            sum += data[i];
        return sum;
    }
}
