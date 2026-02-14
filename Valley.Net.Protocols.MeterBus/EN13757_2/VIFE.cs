using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIFE : Part, IValueInformationField
    {
        public bool Extension { get; }

        public VariableDataQuantityUnit Units { get; }

        public string Unit { get; }

        public string Quantity { get; }

        public string Prefix { get; }

        public int Magnitude { get; }

        public byte Data { get; }

        public sealed class VifeTableRecord
        {
            public byte VIFE { get; }

            public VariableDataQuantityUnit Units { get; }

            public Func<byte, int> Magnitude { get; }

            public VifeTableRecord(byte vife, VariableDataQuantityUnit units, Func<byte, int> magnitude)
            {
                VIFE = vife;
                Units = units;
                Magnitude = magnitude;
            }
        }

        public VIFE(byte data)
        {
            Data = data;
            Extension = (data & 0x80) != 0;

            var masked = (byte)(data & (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION); // clear Extension bit

            if (!VifeLookup.TryGetValue(masked, out var record))
                throw new InvalidDataException();

            Units = record.Units;
            Magnitude = record.Magnitude(masked);
            Prefix = UnitPrefix.GetUnitPrefix(Magnitude);
        }

        public static readonly VifeTableRecord[] VifeTable =
        {
            /* 0x00-0x1f: Error codes VIFE */
            new VifeTableRecord(0x00, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x01, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x02, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x03, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x04, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x05, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x06, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x07, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x08, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x09, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0a, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0b, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0c, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0d, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0e, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x0f, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x10, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x11, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x12, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x13, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x14, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x15, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x16, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x17, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x18, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x19, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1a, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1b, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1c, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1d, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1e, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),
            new VifeTableRecord(0x1f, VariableDataQuantityUnit.ErrorCodesVIFE, b => b & 0x1f),

            /* 0x20-0x3c: Per unit / multiplier / accumulation (magnitude 0) */
            new VifeTableRecord(0x20, VariableDataQuantityUnit.Per_second, b => 0),
            new VifeTableRecord(0x21, VariableDataQuantityUnit.Per_minute, b => 0),
            new VifeTableRecord(0x22, VariableDataQuantityUnit.Per_hour, b => 0),
            new VifeTableRecord(0x23, VariableDataQuantityUnit.Per_day, b => 0),
            new VifeTableRecord(0x24, VariableDataQuantityUnit.Per_week, b => 0),
            new VifeTableRecord(0x25, VariableDataQuantityUnit.Per_month, b => 0),
            new VifeTableRecord(0x26, VariableDataQuantityUnit.Per_year, b => 0),
            new VifeTableRecord(0x27, VariableDataQuantityUnit.Per_RevolutionMeasurement, b => 0),
            new VifeTableRecord(0x28, VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel0, b => 0),
            new VifeTableRecord(0x29, VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel1, b => 0),
            new VifeTableRecord(0x2a, VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel0, b => 0),
            new VifeTableRecord(0x2b, VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel1, b => 0),
            new VifeTableRecord(0x2c, VariableDataQuantityUnit.Per_liter, b => 0),
            new VifeTableRecord(0x2d, VariableDataQuantityUnit.Per_m3, b => 0),
            new VifeTableRecord(0x2e, VariableDataQuantityUnit.Per_kg, b => 0),
            new VifeTableRecord(0x2f, VariableDataQuantityUnit.Per_Kelvin, b => 0),
            new VifeTableRecord(0x30, VariableDataQuantityUnit.Per_kWh, b => 0),
            new VifeTableRecord(0x31, VariableDataQuantityUnit.Per_GJ, b => 0),
            new VifeTableRecord(0x32, VariableDataQuantityUnit.Per_kW, b => 0),
            new VifeTableRecord(0x33, VariableDataQuantityUnit.Per_KelvinLiter, b => 0),
            new VifeTableRecord(0x34, VariableDataQuantityUnit.Per_Volt, b => 0),
            new VifeTableRecord(0x35, VariableDataQuantityUnit.Per_Ampere, b => 0),
            new VifeTableRecord(0x36, VariableDataQuantityUnit.MultipliedBySek, b => 0),
            new VifeTableRecord(0x37, VariableDataQuantityUnit.MultipliedBySek_per_V, b => 0),
            new VifeTableRecord(0x38, VariableDataQuantityUnit.MultipliedBySek_per_A, b => 0),
            new VifeTableRecord(0x39, VariableDataQuantityUnit.StartDateTimeOf, b => 0),
            new VifeTableRecord(0x3a, VariableDataQuantityUnit.UncorrectedUnit, b => 0),
            new VifeTableRecord(0x3b, VariableDataQuantityUnit.AccumulationPositive, b => 0),
            new VifeTableRecord(0x3c, VariableDataQuantityUnit.AccumulationNegative, b => 0),

            /* 0x3d-0x3f: Reserved VIFE 3D */
            new VifeTableRecord(0x3d, VariableDataQuantityUnit.ReservedVIFE_3D, b => b - 0x3d),
            new VifeTableRecord(0x3e, VariableDataQuantityUnit.ReservedVIFE_3D, b => b - 0x3d),
            new VifeTableRecord(0x3f, VariableDataQuantityUnit.ReservedVIFE_3D, b => b - 0x3d),

            /* 0x40, 0x48: Limit value */
            new VifeTableRecord(0x40, VariableDataQuantityUnit.LimitValue, b => (b & 0x08) >> 3),
            new VifeTableRecord(0x48, VariableDataQuantityUnit.LimitValue, b => (b & 0x08) >> 3),

            /* 0x41, 0x49: Nr of limit exceeds */
            new VifeTableRecord(0x41, VariableDataQuantityUnit.NrOfLimitExceeds, b => (b & 0x08) >> 3),
            new VifeTableRecord(0x49, VariableDataQuantityUnit.NrOfLimitExceeds, b => (b & 0x08) >> 3),

            /* Date/time of limit exceed: bytes where (b & 0x72) == 0x42 */
            new VifeTableRecord(0x42, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x43, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x46, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x47, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x4a, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x4b, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x4e, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),
            new VifeTableRecord(0x4f, VariableDataQuantityUnit.DateTimeOfLimitExceed, b => b & 0x0d),

            /* Note: 0x44, 0x45, 0x4c, 0x4d are invalid (not in table → InvalidDataException) */

            /* 0x50-0x5f: Duration of limit exceed */
            new VifeTableRecord(0x50, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x51, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x52, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x53, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x54, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x55, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x56, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x57, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x58, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x59, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5a, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5b, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5c, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5d, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5e, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),
            new VifeTableRecord(0x5f, VariableDataQuantityUnit.DurationOfLimitExceed, b => b & 0x0f),

            /* 0x60-0x67: Duration of limit above */
            new VifeTableRecord(0x60, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x61, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x62, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x63, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x64, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x65, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x66, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),
            new VifeTableRecord(0x67, VariableDataQuantityUnit.DurationOfLimitAbove, b => b & 0x07),

            /* Reserved VIFE 68: bytes where (b & 0x7a) == 0x68 */
            new VifeTableRecord(0x68, VariableDataQuantityUnit.ReservedVIFE_68, b => b - 0x05),
            new VifeTableRecord(0x69, VariableDataQuantityUnit.ReservedVIFE_68, b => b - 0x05),
            new VifeTableRecord(0x6c, VariableDataQuantityUnit.ReservedVIFE_68, b => b - 0x05),
            new VifeTableRecord(0x6d, VariableDataQuantityUnit.ReservedVIFE_68, b => b - 0x05),

            /* Date/time of limit above: bytes where (b & 0x7a) == 0x6a */
            new VifeTableRecord(0x6a, VariableDataQuantityUnit.DateTimeOfLimitAbove, b => b & 0x05),
            new VifeTableRecord(0x6b, VariableDataQuantityUnit.DateTimeOfLimitAbove, b => b & 0x05),
            new VifeTableRecord(0x6e, VariableDataQuantityUnit.DateTimeOfLimitAbove, b => b & 0x05),
            new VifeTableRecord(0x6f, VariableDataQuantityUnit.DateTimeOfLimitAbove, b => b & 0x05),

            /* 0x70-0x77: Multiplicative correction factor */
            new VifeTableRecord(0x70, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x71, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x72, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x73, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x74, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x75, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x76, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),
            new VifeTableRecord(0x77, VariableDataQuantityUnit.MultiplicativeCorrectionFactor, b => (b & 0x07) - 6),

            /* 0x78-0x7b: Additive correction constant */
            new VifeTableRecord(0x78, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3),
            new VifeTableRecord(0x79, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3),
            new VifeTableRecord(0x7a, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3),
            new VifeTableRecord(0x7b, VariableDataQuantityUnit.AdditiveCorrectionConstant, b => (b & 0x03) - 3),

            /* 0x7c: Reserved */
            new VifeTableRecord(0x7c, VariableDataQuantityUnit.ReservedVIFE_7C, b => 0),

            /* 0x7d: Multiplicative correction factor 1000 */
            new VifeTableRecord(0x7d, VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000, b => 3),

            /* 0x7e: Reserved */
            new VifeTableRecord(0x7e, VariableDataQuantityUnit.ReservedVIFE_7E, b => 0),

            /* 0x7f: Manufacturer specific */
            new VifeTableRecord(0x7f, VariableDataQuantityUnit.ManufacturerSpecific, b => 0),
        };

        private static readonly Dictionary<byte, VifeTableRecord> VifeLookup =
            VifeTable.ToDictionary(x => x.VIFE);
    }
}
