using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIF : Part, IValueInformationField
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
            var record = VifVariableLookup[(byte)(data & (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION)]; // clear Extension bit

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

        private static readonly Dictionary<byte, VifTableRecord> VifVariableLookup =
            VifVariableTable.ToDictionary(x => x.VIF);
    }
}
