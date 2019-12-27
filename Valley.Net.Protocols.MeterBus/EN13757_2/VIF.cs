using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIF : Part
    {
        public enum VifType
        {
            PrimaryVIF, // E000 0000b .. E111 1011b
                        // The unit and multiplier is taken from the table for primary VIF (chapter 8.4.3).
            PlainTextVIF, // E111 1100b
                          /// In case of VIF = 7Ch / FCh the true VIF is represented by the following ASCII string with the length given in the first byte. Please note that the byte order of the characters after the length byte depends on the used byte sequence.This plain text VIF allows the user to code units that are not included in the VIF tables.
            LinearVIFExtensionFD, // FDh and FBh
            LinearVIFExtensionFB, // FDh and FBh
                                  // In case of VIF = FDh and VIF = FBh the true VIF is given by the next byte and the coding is taken from the table for secondary VIF (chapter 8.4.4). This extends the available VIF�s by another 256 codes.
            AnyVIF, // 7Eh / FEh
                    // This VIF-Code can be used in direction master to slave for readout selection of all VIF�s.See chapter 6.4.3.
            ManufacturerSpecific, // 7Fh / FFh
        }

        public enum TimePointMagnitudes : byte
        {
            Date = 0,
            TimeDate = 1
        }

        public VifType Type { get; set; }

        public bool Extension { get; set; }

        public VariableDataQuantityUnit Units { get; set; }

        public string Unit { get; set; }

        public int Magnitude { get; set; }

        public string Quantity { get; set; }

        public string VIF_string { get;  }

        public string Name { get; set; }

        public byte Data { get; private set; }

        public sealed class VifTableRecord
        {
            public byte VIF { get; }

            public double Exponent { get; }

            public string Quantity { get; }

            public string Unit { get; set; }

            public VariableDataQuantityUnit Units { get; }

            public VIF.VifType Type { get; }

            public Func<byte, int> Magnitude { get; }

            public Func<int, string> Name { get; }

            public VifTableRecord(byte vif, double exponent, string unit, string quantity, VariableDataQuantityUnit units, VIF.VifType type, Func<byte, int> magnitude, Func<int, string> name)
            {
                VIF = vif;
                Exponent = exponent;
                Quantity = quantity;
                Unit = unit;
                Units = units;
                Type = type;
                Magnitude = magnitude;
                Name = name;
            }
        };

        public VIF(byte data)
        {
            var record = VIF
               .VifVariableTable
               .ToDictionary(x => x.VIF)[(byte)(data & (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION)]; // clear Extension bit

            Data = data;
            Extension = (data & 0x80) != 0;
            Units = record.Units;
            Unit = record.Unit;
            Type = record.Type;
            Magnitude = record.Magnitude(data);
            Name = record.Name(record.Magnitude(data));
            Quantity = record.Quantity;
            VIF_string = record.VIF.ToString("X2")+"h";
        }


        public static readonly VifTableRecord[] VifFixedTable =
        {
            /* 00, 01 left out */
            new VifTableRecord(0x02, 1.0e0, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x03, 1.0e1, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x04, 1.0e2, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x05, 1.0e3, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x06, 1.0e4, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x07, 1.0e5, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x08, 1.0e6, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x09, 1.0e7, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0A, 1.0e8, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0B, 1.0e3, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0C, 1.0e4, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0D, 1.0e5, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0E, 1.0e6, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x0F, 1.0e7, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x10, 1.0e8, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x11, 1.0e9, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x12, 1.0e10,"J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x13, 1.0e11,"J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x14, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x15, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x16, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x17, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x18, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x19, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1A, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1B, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1C, 1.0e0, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1D, 1.0e3, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1E, 1.0e4, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x1F, 1.0e5, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x20, 1.0e6, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x21, 1.0e7, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x22, 1.0e8, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x23, 1.0e9, "J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x24, 1.0e10,"J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x25, 1.0e11,"J/h", "Energy", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x26, 1.0e-6,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x27, 1.0e-5,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x28, 1.0e-4,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x29, 1.0e-3,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2A, 1.0e-2,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2B, 1.0e-1,"m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2C, 1.0e0, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2D, 1.0e1, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2E, 1.0e2, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x2F, 1.0e-5,"m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x31, 1.0e-4,"m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x32, 1.0e-3,"m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x33, 1.0e-2,"m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x34, 1.0e-1,"m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x35, 1.0e0, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x36, 1.0e1, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x37, 1.0e2, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x38, 1.0e-3, "°C", "Temperature", VariableDataQuantityUnit.ReturnTemperatureC, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x39, 1.0e0, "Units for H.C.A.", "H.C.A.", VariableDataQuantityUnit.UnitsForHCA, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3A, 0.0, "Reserved", "Reserved", VariableDataQuantityUnit.Reserved, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3B, 0.0, "Reserved", "Reserved", VariableDataQuantityUnit.Reserved, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3C, 0.0, "Reserved", "Reserved", VariableDataQuantityUnit.Reserved, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3D, 0.0, "Reserved", "Reserved", VariableDataQuantityUnit.Reserved, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3E, 1.0e0, "", "historic", VariableDataQuantityUnit.Undefined, VIF.VifType.PrimaryVIF, b => 0, n => $""),
            new VifTableRecord(0x3F, 1.0e0, "", "No units", VariableDataQuantityUnit.Undefined, VIF.VifType.PrimaryVIF, b => 0, n => $""),

            /* end of array */
            //new VifTableRecord( 0xFFFF, 0.0, "", "", UnitsVariableData.EnergyWh, VIF.Types.PrimaryVIF, b => 0, n => $""),
        };

        public static readonly VifTableRecord[] VifVariableTable =
        {
            /*  Primary VIFs (main table), range 0x00 - 0xFF */

            /*  E000 0nnn    Energy Wh (0.001Wh to 10000Wh) */
            new VifTableRecord ( 0x00, 1.0e-3, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x01, 1.0e-2, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x02, 1.0e-1, "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x03, 1.0e0,  "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x04, 1.0e1,  "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x05, 1.0e2,  "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x06, 1.0e3,  "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),
            new VifTableRecord ( 0x07, 1.0e4,  "Wh", "Energy", VariableDataQuantityUnit.EnergyWh, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}Wh)"),

            /* E000 1nnn    Energy  J (0.001kJ to 10000kJ) */
            new VifTableRecord ( 0x08, 1.0e0, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x09, 1.0e1, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0A, 1.0e2, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0B, 1.0e3, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0C, 1.0e4, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0D, 1.0e5, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0E, 1.0e6, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),
            new VifTableRecord ( 0x0F, 1.0e7, "J", "Energy", VariableDataQuantityUnit.EnergyJ, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Energy ({UnitPrefix.GetUnitPrefix(n)}J)"),

            /* E001 0nnn    Volume m^3 (0.001l to 10000l) */
            new VifTableRecord ( 0x10, 1.0e-6, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x11, 1.0e-5, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x12, 1.0e-4, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x13, 1.0e-3, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x14, 1.0e-2, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x15, 1.0e-1, "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x16, 1.0e0,  "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),
            new VifTableRecord ( 0x17, 1.0e1,  "m^3", "Volume", VariableDataQuantityUnit.Volume_m3, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume ({UnitPrefix.GetUnitPrefix(n)}m^3)"),

            /* E001 1nnn    Mass kg (0.001kg to 10000kg) */
            new VifTableRecord ( 0x18, 1.0e-3, "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x19, 1.0e-2, "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1A, 1.0e-1, "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1B, 1.0e0,  "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1C, 1.0e1,  "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1D, 1.0e2,  "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1E, 1.0e3,  "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),
            new VifTableRecord ( 0x1F, 1.0e4,  "kg", "Mass", VariableDataQuantityUnit.Mass_kg, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass ({UnitPrefix.GetUnitPrefix(n)}kg)"),

            /* E010 00nn    On Time s */
            new VifTableRecord ( 0x20,     1.0, "s", "On time", VariableDataQuantityUnit.OnTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "On Time (seconds)"),  /* seconds */
            new VifTableRecord ( 0x21,    60.0, "s", "On time", VariableDataQuantityUnit.OnTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "On Time (minutes)"),  /* minutes */
            new VifTableRecord ( 0x22,  3600.0, "s", "On time", VariableDataQuantityUnit.OnTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "On Time (hours)"),  /* hours   */
            new VifTableRecord ( 0x23, 86400.0, "s", "On time", VariableDataQuantityUnit.OnTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "On Time (days)"),  /* days    */

            /* E010 01nn    Operating Time s */
            new VifTableRecord ( 0x24,     1.0, "s", "Operating time", VariableDataQuantityUnit.OperatingTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Operating Time (seconds)"),  /* seconds */
            new VifTableRecord ( 0x25,    60.0, "s", "Operating time", VariableDataQuantityUnit.OperatingTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Operating Time (minutes)"),  /* minutes */
            new VifTableRecord ( 0x26,  3600.0, "s", "Operating time", VariableDataQuantityUnit.OperatingTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Operating Time (hours)"),  /* hours   */
            new VifTableRecord ( 0x27, 86400.0, "s", "Operating time", VariableDataQuantityUnit.OperatingTime, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Operating Time (days)"),  /* days    */

            /* E010 1nnn    Power W (0.001W to 10000W) */
            new VifTableRecord ( 0x28, 1.0e-3, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x29, 1.0e-2, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2A, 1.0e-1, "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2B, 1.0e0,  "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2C, 1.0e1,  "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2D, 1.0e2,  "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2E, 1.0e3,  "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),
            new VifTableRecord ( 0x2F, 1.0e4,  "W", "Power", VariableDataQuantityUnit.PowerW, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Power ({UnitPrefix.GetUnitPrefix(n)}W)"),

            /* E011 0nnn    Power J/h (0.001kJ/h to 10000kJ/h) */
            new VifTableRecord ( 0x30, 1.0e0, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x31, 1.0e1, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x32, 1.0e2, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x33, 1.0e3, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x34, 1.0e4, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x35, 1.0e5, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x36, 1.0e6, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),
            new VifTableRecord ( 0x37, 1.0e7, "J/h", "Power", VariableDataQuantityUnit.PowerJ_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07), n => $"Power ({UnitPrefix.GetUnitPrefix(n)}J/h)"),

            /* E011 1nnn    Volume Flow m3/h (0.001l/h to 10000l/h) */
            new VifTableRecord ( 0x38, 1.0e-6, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x39, 1.0e-5, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3A, 1.0e-4, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3B, 1.0e-3, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3C, 1.0e-2, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3D, 1.0e-1, "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3E, 1.0e0,  "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),
            new VifTableRecord ( 0x3F, 1.0e1,  "m^3/h", "Volume flow", VariableDataQuantityUnit.VolumeFlowM3_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 6, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/h)"),

            /* E100 0nnn     Volume Flow ext.  m^3/min (0.0001l/min to 1000l/min) */
            new VifTableRecord ( 0x40, 1.0e-7, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x41, 1.0e-6, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x42, 1.0e-5, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x43, 1.0e-4, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x44, 1.0e-3, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x45, 1.0e-2, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x46, 1.0e-1, "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),
            new VifTableRecord ( 0x47, 1.0e0,  "m^3/min", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_min, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 7, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/min)"),

            /* E100 1nnn     Volume Flow ext.  m^3/s (0.001ml/s to 10000ml/s) */
            new VifTableRecord ( 0x48, 1.0e-9, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x49, 1.0e-8, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4A, 1.0e-7, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4B, 1.0e-6, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4C, 1.0e-5, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4D, 1.0e-4, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4E, 1.0e-3, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),
            new VifTableRecord ( 0x4F, 1.0e-2, "m^3/s", "Volume flow", VariableDataQuantityUnit.VolumeFlowExtM3_per_s, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 9, n => $"Volume flow ({UnitPrefix.GetUnitPrefix(n)} m^3/s)"),

            /* E101 0nnn     Mass flow kg/h (0.001kg/h to 10000kg/h) */
            new VifTableRecord ( 0x50, 1.0e-3, "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x51, 1.0e-2, "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x52, 1.0e-1, "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x53, 1.0e0,  "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x54, 1.0e1,  "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x55, 1.0e2,  "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x56, 1.0e3,  "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),
            new VifTableRecord ( 0x57, 1.0e4,  "kg/h", "Mass flow", VariableDataQuantityUnit.MassFlowKg_per_h, VIF.VifType.PrimaryVIF, b => (b & 0x07) - 3, n => $"Mass flow ({UnitPrefix.GetUnitPrefix(n)} kg/h)"),

            /* E101 10nn     Flow Temperature °C (0.001°C to 1°C) */
            new VifTableRecord ( 0x58, 1.0e-3, "°C", "Flow temperature", VariableDataQuantityUnit.FlowTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Flow temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x59, 1.0e-2, "°C", "Flow temperature", VariableDataQuantityUnit.FlowTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Flow temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x5A, 1.0e-1, "°C", "Flow temperature", VariableDataQuantityUnit.FlowTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Flow temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x5B, 1.0e0,  "°C", "Flow temperature", VariableDataQuantityUnit.FlowTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Flow temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),

            /* E101 11nn Return Temperature °C (0.001°C to 1°C) */
            new VifTableRecord ( 0x5C, 1.0e-3, "°C", "Return temperature", VariableDataQuantityUnit.ReturnTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Return temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x5D, 1.0e-2, "°C", "Return temperature", VariableDataQuantityUnit.ReturnTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Return temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x5E, 1.0e-1, "°C", "Return temperature", VariableDataQuantityUnit.ReturnTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Return temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),
            new VifTableRecord ( 0x5F, 1.0e0,  "°C", "Return temperature", VariableDataQuantityUnit.ReturnTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Return temperature ({UnitPrefix.GetUnitPrefix(n)}deg C)"),

            /* E110 00nn    Temperature Difference  K   (mK to  K) */
            new VifTableRecord ( 0x60, 1.0e-3, "K", "Temperature difference", VariableDataQuantityUnit.TemperatureDifferenceK, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Temperature Difference ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x61, 1.0e-2, "K", "Temperature difference", VariableDataQuantityUnit.TemperatureDifferenceK, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Temperature Difference ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x62, 1.0e-1, "K", "Temperature difference", VariableDataQuantityUnit.TemperatureDifferenceK, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Temperature Difference ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x63, 1.0e0,  "K", "Temperature difference", VariableDataQuantityUnit.TemperatureDifferenceK, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Temperature Difference ({UnitPrefix.GetUnitPrefix(n)} deg C)"),

            /* E110 01nn     External Temperature °C (0.001°C to 1°C) */
            new VifTableRecord ( 0x64, 1.0e-3, "°C", "External temperature", VariableDataQuantityUnit.ExternalTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"External temperature ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x65, 1.0e-2, "°C", "External temperature", VariableDataQuantityUnit.ExternalTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"External temperature ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x66, 1.0e-1, "°C", "External temperature", VariableDataQuantityUnit.ExternalTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"External temperature ({UnitPrefix.GetUnitPrefix(n)} deg C)"),
            new VifTableRecord ( 0x67, 1.0e0,  "°C", "External temperature", VariableDataQuantityUnit.ExternalTemperatureC, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"External temperature ({UnitPrefix.GetUnitPrefix(n)} deg C)"),

            /* E110 10nn     Pressure bar (1mbar to 1000mbar) */
            new VifTableRecord ( 0x68, 1.0e-3, "bar", "Pressure", VariableDataQuantityUnit.PressureBar, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Pressure ({UnitPrefix.GetUnitPrefix(n)} bar)"),
            new VifTableRecord ( 0x69, 1.0e-2, "bar", "Pressure", VariableDataQuantityUnit.PressureBar, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Pressure ({UnitPrefix.GetUnitPrefix(n)} bar)"),
            new VifTableRecord ( 0x6A, 1.0e-1, "bar", "Pressure", VariableDataQuantityUnit.PressureBar, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Pressure ({UnitPrefix.GetUnitPrefix(n)} bar)"),
            new VifTableRecord ( 0x6B, 1.0e0,  "bar", "Pressure", VariableDataQuantityUnit.PressureBar, VIF.VifType.PrimaryVIF, b => (b & 0x03) - 3, n => $"Pressure ({UnitPrefix.GetUnitPrefix(n)} bar)"),

            /* E110 110n     Time Point */
            new VifTableRecord ( 0x6C, 1.0e0, "-", "Time point (date)", VariableDataQuantityUnit.TimePoint, VIF.VifType.PrimaryVIF, b => b & 0x01, n => "Time point (date)"),            /* n = 0        date, data type G */
            new VifTableRecord ( 0x6D, 1.0e0, "-", "Time point (date & time)", VariableDataQuantityUnit.TimePoint, VIF.VifType.PrimaryVIF, b => b & 0x01, n => "Time point (date & time)"),     /* n = 1 time & date, data type F */

            /* E110 1110     Units for H.C.A. dimensionless */
            new VifTableRecord ( 0x6E, 1.0e0,  "Units for H.C.A.", "H.C.A.", VariableDataQuantityUnit.UnitsForHCA, VIF.VifType.PrimaryVIF, b => 0, n => "Units for H.C.A."),

            /* E110 1111     Reserved */
            new VifTableRecord ( 0x6F, 0.0,  "Reserved", "Reserved", VariableDataQuantityUnit.Reserved, VIF.VifType.PrimaryVIF, b => 0, n => "Reserved"),

            /* E111 00nn     Averaging Duration s */
            new VifTableRecord ( 0x70,     1.0, "s", "Averaging Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Averaging Duration (seconds)"),  /* seconds */
            new VifTableRecord ( 0x71,    60.0, "s", "Averaging Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Averaging Duration (minutes)"),  /* minutes */
            new VifTableRecord ( 0x72,  3600.0, "s", "Averaging Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Averaging Duration (hours)"),  /* hours   */
            new VifTableRecord ( 0x73, 86400.0, "s", "Averaging Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Averaging Duration (days)"),  /* days    */

            /* E111 01nn     Actuality Duration s */
            new VifTableRecord ( 0x74,     1.0, "s", "Actuality Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Actuality Duration (seconds)"),  /* seconds */
            new VifTableRecord ( 0x75,    60.0, "s", "Actuality Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Actuality Duration (minutes)"),  /* minutes */
            new VifTableRecord ( 0x76,  3600.0, "s", "Actuality Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Actuality Duration (hours)"),  /* hours   */
            new VifTableRecord ( 0x77, 86400.0, "s", "Actuality Duration", VariableDataQuantityUnit.AveragingDuration, VIF.VifType.PrimaryVIF, b => (b & 0x03), n => "Actuality Duration (days)"),  /* days    */

            /* Fabrication No */
            new VifTableRecord ( 0x78, 1.0, "", "Fabrication No", VariableDataQuantityUnit.FabricationNo, VIF.VifType.PrimaryVIF, b => 0, n => "Fabrication No"),

            /* E111 1001 (Enhanced) Identification */
            new VifTableRecord ( 0x79, 1.0, "", "(Enhanced) Identification", VariableDataQuantityUnit.EnhancedIdentification, VIF.VifType.PrimaryVIF, b => 0, n => "(Enhanced) Identification"),

            /* E111 1010 Bus Address */
            new VifTableRecord ( 0x7a, 1.0, "", "Bus Address", VariableDataQuantityUnit.BusAddress, VIF.VifType.PrimaryVIF, b => 0, n => "Bus Address"),

            new VifTableRecord ( 0x7b, 1.0, "", "Extension 7b", VariableDataQuantityUnit.Extension_7B, VIF.VifType.LinearVIFExtensionFB, b => 0, n => "Extension 7b"),

            new VifTableRecord ( 0x7c, 1.0, "", "Custom VIF", VariableDataQuantityUnit.CustomVIF, VIF.VifType.PlainTextVIF, b => 0, n => "Custom VIF"),

            new VifTableRecord ( 0x7d, 1.0, "", "Extension 7d", VariableDataQuantityUnit.Extension_7D, VIF.VifType.LinearVIFExtensionFD, b => 0, n => "Extension 7d"),

            /* Any VIF: 7Eh */
            new VifTableRecord ( 0x7e, 1.0, "", "Any VIF", VariableDataQuantityUnit.AnyVIF, VIF.VifType.AnyVIF, b => 0, n => "Any VIF"),

            /* Manufacturer specific: 7Fh */
            new VifTableRecord ( 0x7f, 1.0, "", "Manufacturer specific", VariableDataQuantityUnit.ManufacturerSpecific, VIF.VifType.ManufacturerSpecific, b => 0, n => "Manufacturer specific"),

            /* Any VIF: 7Eh */
            new VifTableRecord ( 0xfe, 1.0, "", "Any VIF", VariableDataQuantityUnit.AnyVIF, VIF.VifType.AnyVIF, b => 0, n => "Any VIF"),

            /* Manufacturer specific: FFh */
            new VifTableRecord ( 0xff, 1.0, "", "Manufacturer specific", VariableDataQuantityUnit.ManufacturerSpecific, VIF.VifType.ManufacturerSpecific, b => 0, n => "Manufacturer specific"),
        };

        ///* Main VIFE-Code Extension table (following VIF=FDh for primary VIF)
        //See 8.4.4 a, only some of them are here. Using range 0x100 - 0x1FF */

        ///* E000 00nn   Credit of 10nn-3 of the nominal local legal currency units */
        //new mbus_variable_vif ( 0x100, 1.0e-3, "Currency units", "Credit" ),
        //new mbus_variable_vif ( 0x101, 1.0e-2, "Currency units", "Credit" ),
        //new mbus_variable_vif ( 0x102, 1.0e-1, "Currency units", "Credit" ),
        //new mbus_variable_vif ( 0x103, 1.0e0,  "Currency units", "Credit" ),

        ///* E000 01nn   Debit of 10nn-3 of the nominal local legal currency units */
        //new mbus_variable_vif ( 0x104, 1.0e-3, "Currency units", "Debit" ),
        //new mbus_variable_vif ( 0x105, 1.0e-2, "Currency units", "Debit" ),
        //new mbus_variable_vif ( 0x106, 1.0e-1, "Currency units", "Debit" ),
        //new mbus_variable_vif ( 0x107, 1.0e0,  "Currency units", "Debit" ),

        ///* E000 1000 Access Number (transmission count) */
        //new mbus_variable_vif ( 0x108, 1.0e0,  "", "Access Number (transmission count)" ),

        ///* E000 1001 Medium (as in fixed header) */
        //new mbus_variable_vif ( 0x109, 1.0e0,  "", "Device type" ),

        ///* E000 1010 Manufacturer (as in fixed header) */
        //new mbus_variable_vif ( 0x10A, 1.0e0,  "", "Manufacturer" ),

        ///* E000 1011 Parameter set identification */
        //new mbus_variable_vif ( 0x10B, 1.0e0,  "", "Parameter set identification" ),

        ///* E000 1100 Model / Version */
        //new mbus_variable_vif ( 0x10C, 1.0e0,  "", "Device type" ),

        ///* E000 1101 Hardware version # */
        //new mbus_variable_vif ( 0x10D, 1.0e0,  "", "Hardware version" ),

        ///* E000 1110 Firmware version # */
        //new mbus_variable_vif ( 0x10E, 1.0e0,  "", "Firmware version" ),

        ///* E000 1111 Software version # */
        //new mbus_variable_vif ( 0x10F, 1.0e0,  "", "Software version" ),


        ///* E001 0000 Customer location */
        //new mbus_variable_vif ( 0x110, 1.0e0,  "", "Customer location" ),

        ///* E001 0001 Customer */
        //new mbus_variable_vif ( 0x111, 1.0e0,  "", "Customer" ),

        ///* E001 0010 Access Code User */
        //new mbus_variable_vif ( 0x112, 1.0e0,  "", "Access Code User" ),

        ///* E001 0011 Access Code Operator */
        //new mbus_variable_vif ( 0x113, 1.0e0,  "", "Access Code Operator" ),

        ///* E001 0100 Access Code System Operator */
        //new mbus_variable_vif ( 0x114, 1.0e0,  "", "Access Code System Operator" ),

        ///* E001 0101 Access Code Developer */
        //new mbus_variable_vif ( 0x115, 1.0e0,  "", "Access Code Developer" ),

        ///* E001 0110 Password */
        //new mbus_variable_vif ( 0x116, 1.0e0,  "", "Password" ),

        ///* E001 0111 Error flags (binary) */
        //new mbus_variable_vif ( 0x117, 1.0e0,  "", "Error flags" ),

        ///* E001 1000 Error mask */
        //new mbus_variable_vif ( 0x118, 1.0e0,  "", "Error mask" ),

        ///* E001 1001 Reserved */
        //new mbus_variable_vif ( 0x119, 1.0e0,  "Reserved", "Reserved" ),


        ///* E001 1010 Digital Output (binary) */
        //new mbus_variable_vif ( 0x11A, 1.0e0,  "", "Digital Output" ),

        ///* E001 1011 Digital Input (binary) */
        //new mbus_variable_vif ( 0x11B, 1.0e0,  "", "Digital Input" ),

        ///* E001 1100 Baudrate [Baud] */
        //new mbus_variable_vif ( 0x11C, 1.0e0,  "Baud", "Baudrate" ),

        ///* E001 1101 Response delay time [bittimes] */
        //new mbus_variable_vif ( 0x11D, 1.0e0,  "Bittimes", "Response delay time" ),

        ///* E001 1110 Retry */
        //new mbus_variable_vif ( 0x11E, 1.0e0,  "", "Retry" ),

        ///* E001 1111 Reserved */
        //new mbus_variable_vif ( 0x11F, 1.0e0,  "Reserved", "Reserved" ),


        ///* E010 0000 First storage # for cyclic storage */
        //new mbus_variable_vif ( 0x120, 1.0e0,  "", "First storage # for cyclic storage" ),

        ///* E010 0001 Last storage # for cyclic storage */
        //new mbus_variable_vif ( 0x121, 1.0e0,  "", "Last storage # for cyclic storage" ),

        ///* E010 0010 Size of storage block */
        //new mbus_variable_vif ( 0x122, 1.0e0,  "", "Size of storage block" ),

        ///* E010 0011 Reserved */
        //new mbus_variable_vif ( 0x123, 1.0e0,  "Reserved", "Reserved" ),

        ///* E010 01nn Storage interval [sec(s)..day(s)] */
        //new mbus_variable_vif ( 0x124,        1.0,  "s", "Storage interval" ),   /* second(s) */
        //new mbus_variable_vif ( 0x125,       60.0,  "s", "Storage interval" ),   /* minute(s) */
        //new mbus_variable_vif ( 0x126,     3600.0,  "s", "Storage interval" ),   /* hour(s)   */
        //new mbus_variable_vif ( 0x127,    86400.0,  "s", "Storage interval" ),   /* day(s)    */
        //new mbus_variable_vif ( 0x128,  2629743.83, "s", "Storage interval" ),   /* month(s)  */
        //new mbus_variable_vif ( 0x129, 31556926.0,  "s", "Storage interval" ),   /* year(s)   */

        //												   /* E010 1010 Reserved */
        //new mbus_variable_vif ( 0x12A, 1.0e0,  "Reserved", "Reserved" ),

        ///* E010 1011 Reserved */
        //new mbus_variable_vif ( 0x12B, 1.0e0,  "Reserved", "Reserved" ),

        ///* E010 11nn Duration since last readout [sec(s)..day(s)] */
        //new mbus_variable_vif ( 0x12C,     1.0, "s", "Duration since last readout" ),  /* seconds */
        //new mbus_variable_vif ( 0x12D,    60.0, "s", "Duration since last readout" ),  /* minutes */
        //new mbus_variable_vif ( 0x12E,  3600.0, "s", "Duration since last readout" ),  /* hours   */
        //new mbus_variable_vif ( 0x12F, 86400.0, "s", "Duration since last readout" ),  /* days    */

        //														 /* E011 0000 Start (date/time) of tariff  */
        //														 /* The information about usage of data type F (date and time) or data type G (date) can */
        //														 /* be derived from the datafield (0010b: type G / 0100: type F). */
        //new mbus_variable_vif ( 0x130, 1.0e0,  "Reserved", "Reserved" ), /* ???? */

        //										   /* E011 00nn Duration of tariff (nn=01 ..11: min to days) */
        //new mbus_variable_vif ( 0x131,       60.0,  "s", "Storage interval" ),   /* minute(s) */
        //new mbus_variable_vif ( 0x132,     3600.0,  "s", "Storage interval" ),   /* hour(s)   */
        //new mbus_variable_vif ( 0x133,    86400.0,  "s", "Storage interval" ),   /* day(s)    */

        //												   /* E011 01nn Period of tariff [sec(s) to day(s)]  */
        //new mbus_variable_vif ( 0x134,        1.0, "s", "Period of tariff" ),  /* seconds  */
        //new mbus_variable_vif ( 0x135,       60.0, "s", "Period of tariff" ),  /* minutes  */
        //new mbus_variable_vif ( 0x136,     3600.0, "s", "Period of tariff" ),  /* hours    */
        //new mbus_variable_vif ( 0x137,    86400.0, "s", "Period of tariff" ),  /* days     */
        //new mbus_variable_vif ( 0x138,  2629743.83,"s", "Period of tariff" ),  /* month(s) */
        //new mbus_variable_vif ( 0x139, 31556926.0, "s", "Period of tariff" ),  /* year(s)  */

        //												 /* E011 1010 dimensionless / no VIF */
        //new mbus_variable_vif ( 0x13A, 1.0e0,  "", "Dimensionless" ),

        ///* E011 1011 Reserved */
        //new mbus_variable_vif ( 0x13B, 1.0e0,  "Reserved", "Reserved" ),

        ///* E011 11xx Reserved */
        //new mbus_variable_vif ( 0x13C, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x13D, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x13E, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x13F, 1.0e0,  "Reserved", "Reserved" ),

        ///* E100 nnnn   Volts electrical units */
        //new mbus_variable_vif ( 0x140, 1.0e-9, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x141, 1.0e-8, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x142, 1.0e-7, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x143, 1.0e-6, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x144, 1.0e-5, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x145, 1.0e-4, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x146, 1.0e-3, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x147, 1.0e-2, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x148, 1.0e-1, "V", "Voltage" ),
        //new mbus_variable_vif ( 0x149, 1.0e0,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14A, 1.0e1,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14B, 1.0e2,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14C, 1.0e3,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14D, 1.0e4,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14E, 1.0e5,  "V", "Voltage" ),
        //new mbus_variable_vif ( 0x14F, 1.0e6,  "V", "Voltage" ),

        ///* E101 nnnn   A */
        //new mbus_variable_vif ( 0x150, 1.0e-12, "A", "Current" ),
        //new mbus_variable_vif ( 0x151, 1.0e-11, "A", "Current" ),
        //new mbus_variable_vif ( 0x152, 1.0e-10, "A", "Current" ),
        //new mbus_variable_vif ( 0x153, 1.0e-9,  "A", "Current" ),
        //new mbus_variable_vif ( 0x154, 1.0e-8,  "A", "Current" ),
        //new mbus_variable_vif ( 0x155, 1.0e-7,  "A", "Current" ),
        //new mbus_variable_vif ( 0x156, 1.0e-6,  "A", "Current" ),
        //new mbus_variable_vif ( 0x157, 1.0e-5,  "A", "Current" ),
        //new mbus_variable_vif ( 0x158, 1.0e-4,  "A", "Current" ),
        //new mbus_variable_vif ( 0x159, 1.0e-3,  "A", "Current" ),
        //new mbus_variable_vif ( 0x15A, 1.0e-2,  "A", "Current" ),
        //new mbus_variable_vif ( 0x15B, 1.0e-1,  "A", "Current" ),
        //new mbus_variable_vif ( 0x15C, 1.0e0,   "A", "Current" ),
        //new mbus_variable_vif ( 0x15D, 1.0e1,   "A", "Current" ),
        //new mbus_variable_vif ( 0x15E, 1.0e2,   "A", "Current" ),
        //new mbus_variable_vif ( 0x15F, 1.0e3,   "A", "Current" ),

        ///* E110 0000 Reset counter */
        //new mbus_variable_vif ( 0x160, 1.0e0,  "", "Reset counter" ),

        ///* E110 0001 Cumulation counter */
        //new mbus_variable_vif ( 0x161, 1.0e0,  "", "Cumulation counter" ),

        ///* E110 0010 Control signal */
        //new mbus_variable_vif ( 0x162, 1.0e0,  "", "Control signal" ),

        ///* E110 0011 Day of week */
        //new mbus_variable_vif ( 0x163, 1.0e0,  "", "Day of week" ),

        ///* E110 0100 Week number */
        //new mbus_variable_vif ( 0x164, 1.0e0,  "", "Week number" ),

        ///* E110 0101 Time point of day change */
        //new mbus_variable_vif ( 0x165, 1.0e0,  "", "Time point of day change" ),

        ///* E110 0110 State of parameter activation */
        //new mbus_variable_vif ( 0x166, 1.0e0,  "", "State of parameter activation" ),

        ///* E110 0111 Special supplier information */
        //new mbus_variable_vif ( 0x167, 1.0e0,  "", "Special supplier information" ),

        ///* E110 10pp Duration since last cumulation [hour(s)..years(s)] */
        //new mbus_variable_vif ( 0x168,     3600.0, "s", "Duration since last cumulation" ),  /* hours    */
        //new mbus_variable_vif ( 0x169,    86400.0, "s", "Duration since last cumulation" ),  /* days     */
        //new mbus_variable_vif ( 0x16A,  2629743.83,"s", "Duration since last cumulation" ),  /* month(s) */
        //new mbus_variable_vif ( 0x16B, 31556926.0, "s", "Duration since last cumulation" ),  /* year(s)  */

        //															   /* E110 11pp Operating time battery [hour(s)..years(s)] */
        //new mbus_variable_vif ( 0x16C,     3600.0, "s", "Operating time battery" ),  /* hours    */
        //new mbus_variable_vif ( 0x16D,    86400.0, "s", "Operating time battery" ),  /* days     */
        //new mbus_variable_vif ( 0x16E,  2629743.83,"s", "Operating time battery" ),  /* month(s) */
        //new mbus_variable_vif ( 0x16F, 31556926.0, "s", "Operating time battery" ),  /* year(s)  */

        //													   /* E111 0000 Date and time of battery change */
        //new mbus_variable_vif ( 0x170, 1.0e0,  "", "Date and time of battery change" ),

        ///* E111 0001-1111 Reserved */
        //new mbus_variable_vif ( 0x171, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x172, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x173, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x174, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x175, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x176, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x177, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x178, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x179, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17A, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17B, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17C, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17D, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17E, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x17F, 1.0e0,  "Reserved", "Reserved" ),


        ///* Alternate VIFE-Code Extension table (following VIF=0FBh for primary VIF)
        //See 8.4.4 b, only some of them are here. Using range 0x200 - 0x2FF */

        ///* E000 000n Energy 10(n-1) MWh 0.1MWh to 1MWh */
        //new mbus_variable_vif ( 0x200, 1.0e5,  "Wh", "Energy" ),
        //new mbus_variable_vif ( 0x201, 1.0e6,  "Wh", "Energy" ),

        ///* E000 001n Reserved */
        //new mbus_variable_vif ( 0x202, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x203, 1.0e0,  "Reserved", "Reserved" ),

        ///* E000 01nn Reserved */
        //new mbus_variable_vif ( 0x204, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x205, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x206, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x207, 1.0e0,  "Reserved", "Reserved" ),

        ///* E000 100n Energy 10(n-1) GJ 0.1GJ to 1GJ */
        //new mbus_variable_vif ( 0x208, 1.0e8,  "Reserved", "Energy" ),
        //new mbus_variable_vif ( 0x209, 1.0e9,  "Reserved", "Energy" ),

        ///* E000 101n Reserved */
        //new mbus_variable_vif ( 0x20A, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x20B, 1.0e0,  "Reserved", "Reserved" ),

        ///* E000 11nn Reserved */
        //new mbus_variable_vif ( 0x20C, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x20D, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x20E, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x20F, 1.0e0,  "Reserved", "Reserved" ),

        ///* E001 000n Volume 10(n+2) m3 100m3 to 1000m3 */
        //new mbus_variable_vif ( 0x210, 1.0e2,  "m^3", "Volume" ),
        //new mbus_variable_vif ( 0x211, 1.0e3,  "m^3", "Volume" ),

        ///* E001 001n Reserved */
        //new mbus_variable_vif ( 0x212, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x213, 1.0e0,  "Reserved", "Reserved" ),

        ///* E001 01nn Reserved */
        //new mbus_variable_vif ( 0x214, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x215, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x216, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x217, 1.0e0,  "Reserved", "Reserved" ),

        ///* E001 100n Mass 10(n+2) t 100t to 1000t */
        //new mbus_variable_vif ( 0x218, 1.0e5,  "kg", "Mass" ),
        //new mbus_variable_vif ( 0x219, 1.0e6,  "kg", "Mass" ),

        ///* E001 1010 to E010 0000 Reserved */
        //new mbus_variable_vif ( 0x21A, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x21B, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x21C, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x21D, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x21E, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x21F, 1.0e0,  "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x220, 1.0e0,  "Reserved", "Reserved" ),

        ///* E010 0001 Volume 0,1 feet^3 */
        //new mbus_variable_vif ( 0x221, 1.0e-1, "feet^3", "Volume" ),

        ///* E010 001n Volume 0,1-1 american gallon */
        //new mbus_variable_vif ( 0x222, 1.0e-1, "American gallon", "Volume" ),
        //new mbus_variable_vif ( 0x223, 1.0e-0, "American gallon", "Volume" ),

        ///* E010 0100    Volume flow 0,001 american gallon/min */
        //new mbus_variable_vif ( 0x224, 1.0e-3, "American gallon/min", "Volume flow" ),

        ///* E010 0101 Volume flow 1 american gallon/min */
        //new mbus_variable_vif ( 0x225, 1.0e0,  "American gallon/min", "Volume flow" ),

        ///* E010 0110 Volume flow 1 american gallon/h */
        //new mbus_variable_vif ( 0x226, 1.0e0,  "American gallon/h", "Volume flow" ),

        ///* E010 0111 Reserved */
        //new mbus_variable_vif ( 0x227, 1.0e0, "Reserved", "Reserved" ),

        ///* E010 100n Power 10(n-1) MW 0.1MW to 1MW */
        //new mbus_variable_vif ( 0x228, 1.0e5, "W", "Power" ),
        //new mbus_variable_vif ( 0x229, 1.0e6, "W", "Power" ),

        ///* E010 101n Reserved */
        //new mbus_variable_vif ( 0x22A, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x22B, 1.0e0, "Reserved", "Reserved" ),

        ///* E010 11nn Reserved */
        //new mbus_variable_vif ( 0x22C, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x22D, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x22E, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x22F, 1.0e0, "Reserved", "Reserved" ),

        ///* E011 000n Power 10(n-1) GJ/h 0.1GJ/h to 1GJ/h */
        //new mbus_variable_vif ( 0x230, 1.0e8, "J", "Power" ),
        //new mbus_variable_vif ( 0x231, 1.0e9, "J", "Power" ),

        ///* E011 0010 to E101 0111 Reserved */
        //new mbus_variable_vif ( 0x232, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x233, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x234, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x235, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x236, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x237, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x238, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x239, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23A, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23B, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23C, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23D, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23E, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x23F, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x240, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x241, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x242, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x243, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x244, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x245, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x246, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x247, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x248, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x249, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24A, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24B, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24C, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24D, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24E, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x24F, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x250, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x251, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x252, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x253, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x254, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x255, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x256, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x257, 1.0e0, "Reserved", "Reserved" ),

        ///* E101 10nn Flow Temperature 10(nn-3) °F 0.001°F to 1°F */
        //new mbus_variable_vif ( 0x258, 1.0e-3, "°F", "Flow temperature" ),
        //new mbus_variable_vif ( 0x259, 1.0e-2, "°F", "Flow temperature" ),
        //new mbus_variable_vif ( 0x25A, 1.0e-1, "°F", "Flow temperature" ),
        //new mbus_variable_vif ( 0x25B, 1.0e0,  "°F", "Flow temperature" ),

        ///* E101 11nn Return Temperature 10(nn-3) °F 0.001°F to 1°F */
        //new mbus_variable_vif ( 0x25C, 1.0e-3, "°F", "Return temperature" ),
        //new mbus_variable_vif ( 0x25D, 1.0e-2, "°F", "Return temperature" ),
        //new mbus_variable_vif ( 0x25E, 1.0e-1, "°F", "Return temperature" ),
        //new mbus_variable_vif ( 0x25F, 1.0e0,  "°F", "Return temperature" ),

        ///* E110 00nn Temperature Difference 10(nn-3) °F 0.001°F to 1°F */
        //new mbus_variable_vif ( 0x260, 1.0e-3, "°F", "Temperature difference" ),
        //new mbus_variable_vif ( 0x261, 1.0e-2, "°F", "Temperature difference" ),
        //new mbus_variable_vif ( 0x262, 1.0e-1, "°F", "Temperature difference" ),
        //new mbus_variable_vif ( 0x263, 1.0e0,  "°F", "Temperature difference" ),

        ///* E110 01nn External Temperature 10(nn-3) °F 0.001°F to 1°F */
        //new mbus_variable_vif ( 0x264, 1.0e-3, "°F", "External temperature" ),
        //new mbus_variable_vif ( 0x265, 1.0e-2, "°F", "External temperature" ),
        //new mbus_variable_vif ( 0x266, 1.0e-1, "°F", "External temperature" ),
        //new mbus_variable_vif ( 0x267, 1.0e0,  "°F", "External temperature" ),

        ///* E110 1nnn Reserved */
        //new mbus_variable_vif ( 0x268, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x269, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26A, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26B, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26C, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26D, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26E, 1.0e0, "Reserved", "Reserved" ),
        //new mbus_variable_vif ( 0x26F, 1.0e0, "Reserved", "Reserved" ),

        ///* E111 00nn Cold / Warm Temperature Limit 10(nn-3) °F 0.001°F to 1°F */
        //new mbus_variable_vif ( 0x270, 1.0e-3, "°F", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x271, 1.0e-2, "°F", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x272, 1.0e-1, "°F", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x273, 1.0e0,  "°F", "Cold / Warm Temperature Limit" ),

        ///* E111 01nn Cold / Warm Temperature Limit 10(nn-3) °C 0.001°C to 1°C */
        //new mbus_variable_vif ( 0x274, 1.0e-3, "°C", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x275, 1.0e-2, "°C", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x276, 1.0e-1, "°C", "Cold / Warm Temperature Limit" ),
        //new mbus_variable_vif ( 0x277, 1.0e0,  "°C", "Cold / Warm Temperature Limit" ),

        ///* E111 1nnn cumul. count max power § 10(nnn-3) W 0.001W to 10000W */
        //new mbus_variable_vif ( 0x278, 1.0e-3, "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x279, 1.0e-3, "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27A, 1.0e-1, "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27B, 1.0e0,  "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27C, 1.0e1,  "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27D, 1.0e2,  "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27E, 1.0e3,  "W", "Cumul count max power" ),
        //new mbus_variable_vif ( 0x27F, 1.0e4,  "W", "Cumul count max power" ),

        ///* End of array */
        //new mbus_variable_vif ( 0xFFFF, 0.0, "", "" ),    
    }
}
