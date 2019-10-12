using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum FixedDataUnits : byte
    {
        hms = 0x0, // h,m,s  
        DMY = 0x1, // D,M,Y  
        Wh = 0x2, // Wh  
        Wh10 = 0x3, // Wh * 10
        Wh100 = 0x4, // Wh * 100
        kWh = 0x5, // kWh  
        kWh10 = 0x6, // kWh * 10
        kWh100 = 0x7, // kWh * 100
        MWh = 0x8, // MWh  
        MWh10 = 0x9, // MWh * 10
        MWh100 = 0x0A, // MWh * 100
        kJ = 0x0B, // kJ  
        kJ10 = 0x0C, // kJ * 10
        kJ100 = 0x0D, // kJ * 100
        MJ = 0x0E, // MJ  
        MJ10 = 0x0F, // MJ * 10
        MJ100 = 0x10, // MJ * 100
        GJ = 0x11, // GJ  
        GJ10 = 0x12, // GJ * 10
        GJ100 = 0x13, // GJ * 100
        W = 0x14, // W  
        W10 = 0x15, // W * 10
        W100 = 0x16, // W * 100
        kW = 0x17, // kW  
        kW10 = 0x18, // kW * 10
        kW100 = 0x19, // kW * 100
        MW = 0x1A, // MW  
        MW10 = 0x1B, // MW * 10
        MW100 = 0x1C, // MW * 100
        kJ_per_h = 0x1D, // kJ/h  
        kJ_per_h10 = 0x1E, // kJ/h * 10
        kJ_per_h100 = 0x1F, // kJ/h * 100
        MJ_per_h = 0x20, // MJ/h  
        MJ_per_h10 = 0x21, // MJ/h * 10
        MJ_per_h100 = 0x22, // MJ/h * 100
        GJ_per_h = 0x23, // GJ/h  
        GJ_per_h10 = 0x24, // GJ/h * 10
        GJ_per_h100 = 0x25, // GJ/h * 100
        ml = 0x26, // ml  
        ml10 = 0x27, // ml * 10
        ml100 = 0x28, // ml * 100
        l = 0x29, // l  
        l10 = 0x2A, // l * 10
        l100 = 0x2B, // l * 100
        m3 = 0x2C, // m3  
        m310 = 0x2D, // m3 * 10
        m3100 = 0x2E, // m3 * 100
        ml_per_h = 0x2F, // ml/h  
        ml_per_h10 = 0x30, // ml/h * 10
        ml_per_h100 = 0x31, // ml/h * 100
        l_per_h = 0x32, // l/h  
        l_per_h10 = 0x33, // l/h * 10
        l_per_h100 = 0x34, // l/h * 100
        m3_per_h = 0x35, // m3/h  
        m3_per_h10 = 0x36, // m3/h * 10
        m3_per_h100 = 0x37, // m3/h * 100
        C001 = 0x38, // �C * 10-3
        UnitsForHCA = 0x39, // units for HCA
        reserved3A = 0x3A, // reserved  
        reserved3B = 0x3B, // reserved  
        reserved3C = 0x3C, // reserved  
        reserved3D = 0x3D, // reserved  
        sameButHistoric = 0x3E, // same but historic
        withoutUnits = 0x3F, // without units
    }
}
