namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// M-Bus protocol constants.
/// </summary>
public static class MBusConstants
{
    public const byte FRAME_ACK_START = 0xE5;
    public const byte FRAME_SHORT_START = 0x10;
    public const byte FRAME_LONG_START = 0x68;
    public const byte FRAME_CONTROL_START = 0x68;
    public const byte FRAME_STOP = 0x16;

    public const byte SET_ADDRESS_DIF = 0x01;
    public const byte SET_ADDRESS_VIF = 0x7A;

    public const int FRAME_FIXED_SIZE_SHORT = 5;
    public const int FRAME_FIXED_SIZE_LONG = 6;

    public const byte ADDRESS_BROADCAST_NOREPLY = 0xFF;
    public const byte ADDRESS_BROADCAST_REPLY = 0xFE;
    public const byte ADDRESS_NETWORK_LAYER = 0xFD;

    public static readonly Dictionary<DataTypes, int> LengthsInBitsTable = new()
    {
        { DataTypes._No_data, 0 },
        { DataTypes._8_Bit_Integer, 8 },
        { DataTypes._16_Bit_Integer, 16 },
        { DataTypes._24_Bit_Integer, 24 },
        { DataTypes._32_Bit_Integer, 32 },
        { DataTypes._32_Bit_Real, 32 },
        { DataTypes._48_Bit_Integer, 48 },
        { DataTypes._64_Bit_Integer, 64 },
        { DataTypes._Selection_for_Readout, 0 },
        { DataTypes._2_digit_BCD, 8 },
        { DataTypes._4_digit_BCD, 16 },
        { DataTypes._6_digit_BCD, 24 },
        { DataTypes._8_digit_BCD, 32 },
        { DataTypes._variable_length, 0 },
        { DataTypes._12_digit_BCD, 48 },
    };
}
