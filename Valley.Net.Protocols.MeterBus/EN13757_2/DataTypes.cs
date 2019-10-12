using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum DataTypes : byte
    {
        _No_data = 0, // No data
        _8_Bit_Integer = 1, // 8 Bit Integer
        _16_Bit_Integer = 2, // 16 Bit Integer
        _24_Bit_Integer = 3, // 24 Bit Integer
        _32_Bit_Integer = 4, // 32 Bit Integer
        _32_Bit_Real = 5, // 32 Bit Real
        _48_Bit_Integer = 6, // 48 Bit Integer
        _64_Bit_Integer = 7, // 64 Bit Integer
        _Selection_for_Readout = 8, // Selection for Readout
        _2_digit_BCD = 9, // 2 digit BCD
        _4_digit_BCD = 10, // 4 digit BCD
        _6_digit_BCD = 11, // 6 digit BCD
        _8_digit_BCD = 12, // 8 digit BCD
        _variable_length = 13, // variable length
        _12_digit_BCD = 14, // 12 digit BCD
        _Unknown = 15, // Special Functions
    }
}
