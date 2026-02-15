namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Serializes immutable M-Bus frames into raw bytes.
/// </summary>
public sealed class FrameSerializer : IFrameSerializer
{
    public byte[] Serialize(MBusFrame frame) => frame switch
    {
        AckFrame => [MBusConstants.FRAME_ACK_START],
        ShortFrame sf => SerializeShort(sf),
        ControlFrame cf => SerializeControl(cf),
        LongFrame lf => SerializeLong(lf),
        _ => throw new ArgumentException($"Unknown frame type: {frame.GetType().Name}")
    };

    public int GetSerializedLength(MBusFrame frame) => frame switch
    {
        AckFrame => 1,
        ShortFrame => 5,
        ControlFrame => 9,
        LongFrame lf => lf.Data.Length + 9,
        _ => throw new ArgumentException($"Unknown frame type: {frame.GetType().Name}")
    };

    public int Serialize(MBusFrame frame, Span<byte> destination)
    {
        var bytes = Serialize(frame);
        bytes.CopyTo(destination);
        return bytes.Length;
    }

    private static byte[] SerializeShort(ShortFrame frame)
    {
        var control = (byte)frame.Control;
        var crc = (byte)(control + frame.Address);
        return [MBusConstants.FRAME_SHORT_START, control, frame.Address, crc, MBusConstants.FRAME_STOP];
    }

    private static byte[] SerializeControl(ControlFrame frame)
    {
        var control = (byte)frame.Control;
        var ci = (byte)frame.ControlInformation;
        var crc = (byte)(control + frame.Address + ci);
        return [MBusConstants.FRAME_LONG_START, 0x03, 0x03, MBusConstants.FRAME_LONG_START,
                control, frame.Address, ci, crc, MBusConstants.FRAME_STOP];
    }

    private static byte[] SerializeLong(LongFrame frame)
    {
        var data = frame.Data.Span;
        var length = (byte)(data.Length + 3);
        var control = (byte)frame.Control;
        var ci = (byte)frame.ControlInformation;

        var result = new byte[data.Length + 9];
        result[0] = MBusConstants.FRAME_LONG_START;
        result[1] = length;
        result[2] = length;
        result[3] = MBusConstants.FRAME_LONG_START;
        result[4] = control;
        result[5] = frame.Address;
        result[6] = ci;
        data.CopyTo(result.AsSpan(7));

        byte crc = (byte)(control + frame.Address + ci);
        for (int i = 0; i < data.Length; i++)
            crc += data[i];

        result[7 + data.Length] = crc;
        result[8 + data.Length] = MBusConstants.FRAME_STOP;
        return result;
    }
}
