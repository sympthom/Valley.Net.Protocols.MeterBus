using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    public sealed class VariableDataPacket : DataPacket
    {
        public byte Version { get; set; }

        public byte Status { get; set; }

        public UInt16 Signature { get; set; }

        public sealed class Record
        {            
            public VariableDataRecordType RecordType { get; set; }

            public Function Function { get; set; }

            public UInt64 StorageNumber { get; set; }

            public UInt32 Tariff { get; set; }

            public UInt16 SubUnit { get; set; }

            public DataTypes ValueDataType { get; set; }

            public byte[] ValueData { get; set; }

            public object Value { get; set; }

            public sealed class UnitData
            {
                public VariableDataQuantityUnit Units { get; set; }

                public string Unit { get; set; }

                public int Magnitude { get; set; }

                public string Quantity { get; set; }

                public string VIF_string { get; set; }

                public override string ToString()
                {
                    if (VIF_string == null)
                        return string.Format("Unit: {0}, Magnitude: {1}", Units, Magnitude);
                    else
                        return string.Format("Unit: {0} ({2}), Magnitude: {1}", Units, Magnitude, VIF_string);
                }
            }

            public UnitData[] Units { get; set; }

            public int Magnitude => Units
                        .Where(u => u.Units != VariableDataQuantityUnit.AdditiveCorrectionConstant)
                        .Sum(u => u.Magnitude);

            public int Offset => Units
                        .Where(u => (u.Units == VariableDataQuantityUnit.AdditiveCorrectionConstant))
                        .Sum(u => u.Magnitude);

            public string Name => string
                        .Format("{0}.{1}.Tariff{2}.SubUnit{3}", string.Join(":", Units
                        .Where(u =>
                            (u.Units != VariableDataQuantityUnit.MultiplicativeCorrectionFactor) &&
                            (u.Units != VariableDataQuantityUnit.MultiplicativeCorrectionFactor1000) &&
                            (u.Units != VariableDataQuantityUnit.AdditiveCorrectionConstant))
                        .Select(u => u.VIF_string == null ? string.Empty : $"{u.VIF_string}." + u.Units.ToString())), Function, Tariff, SubUnit);

            public Tuple<string, object> NormalizedValue
            {
                get
                {
                    if (ValueDataType == DataTypes._No_data)
                        return null;

                    if (RecordType == VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC)
                        return new Tuple<string, object>($"ManufacturerSpecific.{Function}.Tariff{Tariff}.SubUnit{SubUnit}", $"{ValueData.ToHex()}");
                                        
                    if (Value is string)
                        return new Tuple<string, object>(Name, Value);                                    

                    if (Value is Single)
                        return new Tuple<string, object>(Name, ((Single)Value) * Math.Pow(10, Magnitude));

                    if (Value is Double)
                        return new Tuple<string, object>(Name, ((Double)Value) * Math.Pow(10, Magnitude));

                    Decimal decValue = 0;

                    if (Value is byte)
                        decValue = (byte)Value;
                    else if (Value is sbyte)
                        decValue = (sbyte)Value;
                    else if (Value is Int16)
                        decValue = (Int16)Value;
                    else if (Value is Int32)
                        decValue = (Int32)Value;
                    else if (Value is Int64)
                        decValue = (Int64)Value;

                    switch (Units[0].Units)
                    {
                        case VariableDataQuantityUnit.OnTime:
                        case VariableDataQuantityUnit.OperatingTime:
                        case VariableDataQuantityUnit.AveragingDuration:
                        case VariableDataQuantityUnit.ActualityDuration:
                        case VariableDataQuantityUnit.StorageInterval:
                        case VariableDataQuantityUnit.DurationSinceLastReadout:
                        case VariableDataQuantityUnit.PeriodOfTariff:
                            {
                                TimeSpan span;

                                switch ((TimeMagnitudes)Magnitude)
                                {
                                    case TimeMagnitudes.Seconds: span = new TimeSpan(hours: 0, minutes: 0, seconds: (int)decValue); break;
                                    case TimeMagnitudes.Minutes: span = new TimeSpan(hours: 0, minutes: (int)decValue, seconds: 0); break;
                                    case TimeMagnitudes.Hours: span = new TimeSpan(hours: (int)decValue, minutes: 0, seconds: 0); break;
                                    case TimeMagnitudes.Days: span = new TimeSpan(days: (int)decValue, hours: 0, minutes: 0, seconds: 0); break;
                                    default: throw new InvalidDataException();
                                }

                                return new Tuple<string, object>(Name, span);
                            }
                        case VariableDataQuantityUnit.TimePoint:
                            {
                                var dateTime = ValueParser.ParseDateTime(ValueDataType, ValueData);

                                return new Tuple<string, object>(Name, dateTime);
                            }
                    }

                    return new Tuple<string, object>(Name, decValue * (Decimal)Math.Pow(10, Magnitude) + Offset);
                }
            }

            public override string ToString()
            {
                //return string.Format("NormalizedValue: {7} (" + "DataType: {0}, Function: {1}, StorageNumber: {2}, Tariff: {3}, SubUnit: {4}, " + "Value: {5}, Units: {6})", DataType, Function, StorageNumber, Tariff, SubUnit, Value, string.Join(", ", Units.Select(u => u.ToString())), NormalizedValue);
                return string.Format("NormalizedValue: {0}", NormalizedValue);
            }
        }

        public List<Record> Records { get; set; }

        public VariableDataPacket(byte address) : base(address)
        {

        }

        public override string ToString()
        {
            return string.Format("IdentificationNo: {0}, AccessNo: {1}, Medium: {2}, " + "Items: {3}", IdentificationNo, TransmissionCounter, DeviceType, string.Join("\n", Records.Select(i => i.ToString())));
        }
    }   
}