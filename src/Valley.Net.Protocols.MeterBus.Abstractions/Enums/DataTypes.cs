namespace Valley.Net.Protocols.MeterBus;

public enum DataTypes : byte
{
    _No_data = 0x00,
    _8_Bit_Integer = 0x01,
    _16_Bit_Integer = 0x02,
    _24_Bit_Integer = 0x03,
    _32_Bit_Integer = 0x04,
    _32_Bit_Real = 0x05,
    _48_Bit_Integer = 0x06,
    _64_Bit_Integer = 0x07,
    _Selection_for_Readout = 0x08,
    _2_digit_BCD = 0x09,
    _4_digit_BCD = 0x0A,
    _6_digit_BCD = 0x0B,
    _8_digit_BCD = 0x0C,
    _variable_length = 0x0D,
    _12_digit_BCD = 0x0E,
    _Unknown = 0x0F,
}
