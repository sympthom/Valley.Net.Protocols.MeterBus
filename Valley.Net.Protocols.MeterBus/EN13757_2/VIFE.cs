using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIFE : Part
    {
        public bool Extension { get; }

        public VariableDataQuantityUnit Units { get; }

        public string Unit { get; }

        public string Quantity { get; }

        public string Prefix { get; }

        public int Magnitude { get; }

        public byte Data { get; }

        public VIFE(byte data)
        {
            Data = data;
            Extension = (data & 0x80) != 0;

            data &= (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION; // clear Extension bit

            if ((data >= 0) && (data <= 0x1f))
            {
                Units = VariableDataQuantityUnit.ErrorCodesVIFE;
                Magnitude = (data & 0x1f);
            }
            else if (data == 0x20)
                Units = VariableDataQuantityUnit.Per_second;
            else if (data == 0x21)
                Units = VariableDataQuantityUnit.Per_minute;
            else if (data == 0x22)
                Units = VariableDataQuantityUnit.Per_hour;
            else if (data == 0x23)
                Units = VariableDataQuantityUnit.Per_day;
            else if (data == 0x24)
                Units = VariableDataQuantityUnit.Per_week;
            else if (data == 0x25)
                Units = VariableDataQuantityUnit.Per_month;
            else if (data == 0x26)
                Units = VariableDataQuantityUnit.Per_year;
            else if (data == 0x27)
                Units = VariableDataQuantityUnit.Per_RevolutionMeasurement;
            else if (data == 0x28)
                Units = VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel0;
            else if (data == 0x29)
                Units = VariableDataQuantityUnit.Increment_per_inputPulseOnInputChannel1;
            else if (data == 0x2a)
                Units = VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel0;
            else if (data == 0x2b)
                Units = VariableDataQuantityUnit.Increment_per_outputPulseOnOutputChannel1;
            else if (data == 0x2c)
                Units = VariableDataQuantityUnit.Per_liter;
            else if (data == 0x2d)
                Units = VariableDataQuantityUnit.Per_m3;
            else if (data == 0x2e)
                Units = VariableDataQuantityUnit.Per_kg;
            else if (data == 0x2f)
                Units = VariableDataQuantityUnit.Per_Kelvin;
            else if (data == 0x30)
                Units = VariableDataQuantityUnit.Per_kWh;
            else if (data == 0x31)
                Units = VariableDataQuantityUnit.Per_GJ;
            else if (data == 0x32)
                Units = VariableDataQuantityUnit.Per_kW;
            else if (data == 0x33)
                Units = VariableDataQuantityUnit.Per_KelvinLiter;
            else if (data == 0x34)
                Units = VariableDataQuantityUnit.Per_Volt;
            else if (data == 0x35)
                Units = VariableDataQuantityUnit.Per_Ampere;
            else if (data == 0x36)
                Units = VariableDataQuantityUnit.MultipliedBySek;
            else if (data == 0x37)
                Units = VariableDataQuantityUnit.MultipliedBySek_per_V;
            else if (data == 0x38)
                Units = VariableDataQuantityUnit.MultipliedBySek_per_A;
            else if (data == 0x39)
                Units = VariableDataQuantityUnit.StartDateTimeOf;
            else if (data == 0x3a)
                Units = VariableDataQuantityUnit.UncorrectedUnit;
            else if (data == 0x3b)
                Units = VariableDataQuantityUnit.AccumulationPositive;
            else if (data == 0x3c)
                Units = VariableDataQuantityUnit.AccumulationNegative;
            else if ((data >= 0x3d) && (data <= 0x3f))
            {
                Units = VariableDataQuantityUnit.ReservedVIFE_3D;
                Magnitude = (data - 0x3d);
            }
            else if ((data == 0x40) || (data == 0x48))
            {
                Units = VariableDataQuantityUnit.LimitValue;
                Magnitude = ((data & 0x08) >> 3);
            }
            else if ((data == 0x41) || (data == 0x49))
            {
                Units = VariableDataQuantityUnit.NrOfLimitExceeds;
                Magnitude = ((data & 0x08) >> 3);
            }
            else if ((data & 0x72) == 0x42)
            {
                Units = VariableDataQuantityUnit.DateTimeOfLimitExceed;
                Magnitude = (data & 0x0d);
            }
            else if ((data >= 0x50) && (data <= 0x5f))
            {
                Units = VariableDataQuantityUnit.DurationOfLimitExceed;
                Magnitude = (data & 0x0f);
            }
            else if ((data >= 0x60) && (data <= 0x67))
            {
                Units = VariableDataQuantityUnit.DurationOfLimitAbove;
                Magnitude = (data & 0x07);
            }
            else if ((data & 0x7a) == 0x68)
            {
                Units = VariableDataQuantityUnit.ReservedVIFE_68;
                Magnitude = (data - 0x05);
            }
            else if ((data & 0x7a) == 0x6a)
            {
                Units = VariableDataQuantityUnit.DateTimeOfLimitAbove;
                Magnitude = (data & 0x05);
            }
            else if ((data >= 0x70) && (data <= 0x77))
            {
                Units = VariableDataQuantityUnit.MultiplicativeCorrectionFactor;
                Magnitude = (data & 0x07) - 6;
            }
            else if ((data >= 0x78) && (data <= 0x7b))
            {
                Units = VariableDataQuantityUnit.AdditiveCorrectionConstant;
                Magnitude = (data & 0x03) - 3;
            }
            else if (data == 0x7c)
            {
                Units = VariableDataQuantityUnit.ReservedVIFE_7C;
            }
            else if (data == 0x7d)
            {
                Units = VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000;
                Magnitude = 3;
            }
            else if (data == 0x7e)
            {
                Units = VariableDataQuantityUnit.ReservedVIFE_7E;
            }
            else if (data == 0x7f)
            {
                Units = VariableDataQuantityUnit.ManufacturerSpecific;
            }
            else
                throw new InvalidDataException();

            Prefix = UnitPrefix.GetUnitPrefix(Magnitude);
        }
    }
}
