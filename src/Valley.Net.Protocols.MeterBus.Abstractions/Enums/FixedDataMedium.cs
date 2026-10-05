namespace Valley.Net.Protocols.MeterBus;

// Medium of the fixed data structure (MBDOC48 8.3.1). The mode 2 media are meters that send their counters
// high byte first although CI 73h says otherwise.
public enum FixedDataMedium : byte
{
    Other = 0x00,
    Oil = 0x01,
    Electricity = 0x02,
    Gas = 0x03,
    Heat = 0x04,
    Steam = 0x05,
    HotWater = 0x06,
    Water = 0x07,
    HCA = 0x08,
    Reserved_0x09 = 0x09,
    GasMode2 = 0x0A,
    HeatMode2 = 0x0B,
    HotWaterMode2 = 0x0C,
    WaterMode2 = 0x0D,
    HCAMode2 = 0x0E,
    Reserved_0x0F = 0x0F,
}
