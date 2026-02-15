namespace Valley.Net.Protocols.MeterBus;

[Flags]
public enum ControlMask : byte
{
    SND_NKE = 0x40,
    SND_UD = 0x53,
    REQ_UD2 = 0x5B,
    REQ_UD1 = 0x5A,
    RSP_UD = 0x08,
    FCB = 0x20,
    FCV = 0x10,
    ACD = 0x20,
    DFC = 0x10,
    DIR = 0x40,
    DIR_M2S = 0x40,
    DIR_S2M = 0x00,
}
