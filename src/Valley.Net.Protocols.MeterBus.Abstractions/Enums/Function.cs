namespace Valley.Net.Protocols.MeterBus;

public enum Function : byte
{
    Instantaneous = 0x00,
    Maximum = 0x10,
    Minimum = 0x20,
    ValueDuringError = 0x30,
}
