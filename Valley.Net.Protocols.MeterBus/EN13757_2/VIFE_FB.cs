using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIFE_FB : Part
    {
        public bool Extension { get; }

        public VariableDataQuantityUnit Units { get; }

        public string Unit { get; }

        public string Quantity { get; }

        public string Prefix { get; }

        public int Magnitude { get; }

        public byte Data { get; }

        public sealed class VifeFbTableRecord
        {
            public byte VIFFB { get; }

            public VariableDataQuantityUnit Units { get; }

            public string Unit { get; set; }

            public string Quantity { get; set; }

            public Func<byte, int> Magnitude { get; }

            public VifeFbTableRecord(byte viffb, VariableDataQuantityUnit units, Func<byte, int> magnitude)
            {
                VIFFB = viffb;
                Units = units;
                Magnitude = magnitude;
            }
        };

        public VIFE_FB(byte b)
        {
            var record = VIFE_FB
                .VifeFbTable
                .ToDictionary(x => x.VIFFB)[(byte)(b & (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION)]; // clear Extension bit

            Data = b;
            Extension = (b & 0x80) != 0;
            Units = record.Units;
            Unit = record.Unit;
            Quantity = record.Quantity;
            Magnitude = record.Magnitude(b);
            Prefix = UnitPrefix.GetUnitPrefix(record.Magnitude(b));
        }

        public static readonly VifeFbTableRecord[] VifeFbTable =
        {
            new VifeFbTableRecord(0x00, VariableDataQuantityUnit.EnergyMWh, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x01, VariableDataQuantityUnit.EnergyMWh, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x02, VariableDataQuantityUnit.ReservedVIFE_FB_02, b => (b & 0x01)),
            new VifeFbTableRecord(0x03, VariableDataQuantityUnit.ReservedVIFE_FB_02, b => (b & 0x01)),
            new VifeFbTableRecord(0x04, VariableDataQuantityUnit.ReservedVIFE_FB_04, b => (b & 0x01)),
            new VifeFbTableRecord(0x05, VariableDataQuantityUnit.ReservedVIFE_FB_04, b => (b & 0x01)),
            new VifeFbTableRecord(0x06, VariableDataQuantityUnit.ReservedVIFE_FB_04, b => (b & 0x01)),
            new VifeFbTableRecord(0x07, VariableDataQuantityUnit.ReservedVIFE_FB_04, b => (b & 0x01)),
            new VifeFbTableRecord(0x08, VariableDataQuantityUnit.EnergyGJ, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x09, VariableDataQuantityUnit.EnergyGJ, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x0a, VariableDataQuantityUnit.ReservedVIFE_FB_0a, b => (b & 0x01)),
            new VifeFbTableRecord(0x0b, VariableDataQuantityUnit.ReservedVIFE_FB_0a, b => (b & 0x01)),
            new VifeFbTableRecord(0x0c, VariableDataQuantityUnit.ReservedVIFE_FB_0c, b => (b & 0x03)),
            new VifeFbTableRecord(0x0d, VariableDataQuantityUnit.ReservedVIFE_FB_0c, b => (b & 0x03)),
            new VifeFbTableRecord(0x0e, VariableDataQuantityUnit.ReservedVIFE_FB_0c, b => (b & 0x03)),
            new VifeFbTableRecord(0x0f, VariableDataQuantityUnit.ReservedVIFE_FB_0c, b => (b & 0x03)),
            new VifeFbTableRecord(0x10, VariableDataQuantityUnit.Volume_m3, b => (b & 0x01) + 2),
            new VifeFbTableRecord(0x11, VariableDataQuantityUnit.Volume_m3, b => (b & 0x01) + 2),
            new VifeFbTableRecord(0x12, VariableDataQuantityUnit.ReservedVIFE_FB_12, b => (b & 0x01)),
            new VifeFbTableRecord(0x13, VariableDataQuantityUnit.ReservedVIFE_FB_12, b => (b & 0x01)),
            new VifeFbTableRecord(0x14, VariableDataQuantityUnit.ReservedVIFE_FB_14, b => (b & 0x03)),
            new VifeFbTableRecord(0x15, VariableDataQuantityUnit.ReservedVIFE_FB_14, b => (b & 0x03)),
            new VifeFbTableRecord(0x16, VariableDataQuantityUnit.ReservedVIFE_FB_14, b => (b & 0x03)),
            new VifeFbTableRecord(0x17, VariableDataQuantityUnit.ReservedVIFE_FB_14, b => (b & 0x03)),
            new VifeFbTableRecord(0x18, VariableDataQuantityUnit.Mass_t, b => (b & 0x01) + 2),
            new VifeFbTableRecord(0x19, VariableDataQuantityUnit.Mass_t, b => (b & 0x01) + 2),
            new VifeFbTableRecord(0x1a, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x1b, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x1c, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x1d, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x1e, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x1f, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x20, VariableDataQuantityUnit.ReservedVIFE_FB_1a, b => (b - 0x1a)),
            new VifeFbTableRecord(0x21, VariableDataQuantityUnit.Volume_feet3, b => -1),
            new VifeFbTableRecord(0x22, VariableDataQuantityUnit.Volume_american_gallon, b => (b - 0x23)),
            new VifeFbTableRecord(0x23, VariableDataQuantityUnit.Volume_american_gallon, b => (b - 0x23)),
            new VifeFbTableRecord(0x24, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, b => -3),
            new VifeFbTableRecord(0x25, VariableDataQuantityUnit.Volume_flow_american_gallon_per_min, b => 0),
            new VifeFbTableRecord(0x26, VariableDataQuantityUnit.Volume_flow_american_gallon_per_h, b => 0),
            new VifeFbTableRecord(0x27, VariableDataQuantityUnit.ReservedVIFE_FB_27, b => 0),
            new VifeFbTableRecord(0x28, VariableDataQuantityUnit.Power_MW, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x29, VariableDataQuantityUnit.Power_MW, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x2a, VariableDataQuantityUnit.ReservedVIFE_FB_2a, b => (b & 0x01)),
            new VifeFbTableRecord(0x2b, VariableDataQuantityUnit.ReservedVIFE_FB_2a, b => (b & 0x01)),
            new VifeFbTableRecord(0x2c, VariableDataQuantityUnit.ReservedVIFE_FB_2c, b => (b & 0x03)),
            new VifeFbTableRecord(0x2d, VariableDataQuantityUnit.ReservedVIFE_FB_2c, b => (b & 0x03)),
            new VifeFbTableRecord(0x2e, VariableDataQuantityUnit.ReservedVIFE_FB_2c, b => (b & 0x03)),
            new VifeFbTableRecord(0x2f, VariableDataQuantityUnit.ReservedVIFE_FB_2c, b => (b & 0x03)),
            new VifeFbTableRecord(0x30, VariableDataQuantityUnit.Power_GJ_per_h, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x31, VariableDataQuantityUnit.Power_GJ_per_h, b => (b & 0x01) - 1),
            new VifeFbTableRecord(0x32, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x33, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x34, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x35, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x36, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x37, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x38, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x39, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3a, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3b, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3c, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3d, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3e, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x3f, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x40, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x41, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x42, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x43, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x44, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x45, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x46, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x47, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x48, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x49, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4a, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4b, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4c, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4d, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4e, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x4f, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x50, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x51, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x52, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x53, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x54, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x55, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x56, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x57, VariableDataQuantityUnit.ReservedVIFE_FB_32, b => (b - 0x32)),
            new VifeFbTableRecord(0x58, VariableDataQuantityUnit.FlowTemperature_F, b => (b - 0x32)),
            new VifeFbTableRecord(0x59, VariableDataQuantityUnit.FlowTemperature_F, b => (b - 0x32)),
            new VifeFbTableRecord(0x5a, VariableDataQuantityUnit.FlowTemperature_F, b => (b - 0x32)),
            new VifeFbTableRecord(0x5b, VariableDataQuantityUnit.FlowTemperature_F, b => (b - 0x32)),
            new VifeFbTableRecord(0x5c, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x5d, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x5e, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x5f, VariableDataQuantityUnit.ReturnTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x60, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x61, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x62, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x63, VariableDataQuantityUnit.TemperatureDifference_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x64, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x65, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x66, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x67, VariableDataQuantityUnit.ExternalTemperature_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x68, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x69, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6a, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6b, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6c, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6d, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6e, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x6f, VariableDataQuantityUnit.ReservedVIFE_FB_68, b => (b & 0x07)),
            new VifeFbTableRecord(0x70, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x71, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x72, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x73, VariableDataQuantityUnit.ColdWarmTemperatureLimit_F, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x74, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x75, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x76, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x77, VariableDataQuantityUnit.ColdWarmTemperatureLimit_C, b => (b & 0x03) - 3),
            new VifeFbTableRecord(0x78, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x79, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7a, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7b, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7c, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7d, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7e, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
            new VifeFbTableRecord(0x7f, VariableDataQuantityUnit.CumulCountMaxPower_W, b => (b & 0x07) - 3),
        };
    }
}
