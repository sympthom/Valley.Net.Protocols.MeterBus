using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class VIFE_FD : Part
    {
        public bool Extension { get; }

        public VariableDataQuantityUnit Units { get; }

        public string Unit { get; }

        public string Quantity { get; }

        public string Prefix { get; }

        public int Magnitude { get; }

        public byte Data { get; }

        public sealed class VifeFdTableRecord
        {
            public byte VIFFE { get; }

            public VariableDataQuantityUnit Units { get; }

            public string Unit { get; set; }

            public string Quantity { get; set; }

            public Func<byte, int> Magnitude { get; }

            public VifeFdTableRecord(byte viffe, VariableDataQuantityUnit units, Func<byte, int> magnitude)
            {
                VIFFE = viffe;
                Units = units;
                Magnitude = magnitude;
            }
        };

        public VIFE_FD(byte data)
        {
            var record = VIFE_FD
                .VifeFdTable
                .ToDictionary(x => x.VIFFE)[(byte)(data & (byte)VariableDataRecordType.MBUS_DIB_VIF_WITHOUT_EXTENSION)]; // clear Extension bit

            Data = data;
            Extension = (data & 0x80) != 0;
            Units = record.Units;
            Unit = record.Unit;
            Quantity = record.Quantity;
            Magnitude = record.Magnitude(data);
            Prefix = UnitPrefix.GetUnitPrefix(record.Magnitude(data));
        }

        public static readonly VifeFdTableRecord[] VifeFdTable =
        {
            new VifeFdTableRecord(0x00, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x01, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x02, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x03, VariableDataQuantityUnit.Credit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x04, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x05, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x06, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x07, VariableDataQuantityUnit.Debit, b => (b & 0x03) - 3),
            new VifeFdTableRecord(0x08, VariableDataQuantityUnit.AccessNumber, b => 0),
            new VifeFdTableRecord(0x09, VariableDataQuantityUnit.Medium, b => 0),
            new VifeFdTableRecord(0x0a, VariableDataQuantityUnit.Manufacturer, b => 0),
            new VifeFdTableRecord(0x0b, VariableDataQuantityUnit.EnhancedIdentification, b => 0),
            new VifeFdTableRecord(0x0c, VariableDataQuantityUnit.ModelVersion, b => 0),
            new VifeFdTableRecord(0x0d, VariableDataQuantityUnit.HardwareVersionNr, b => 0),
            new VifeFdTableRecord(0x0e, VariableDataQuantityUnit.FirmwareVersionNr, b => 0),
            new VifeFdTableRecord(0x0f, VariableDataQuantityUnit.SoftwareVersionNr, b => 0),
            new VifeFdTableRecord(0x10, VariableDataQuantityUnit.CustomerLocation, b => 0),
            new VifeFdTableRecord(0x11, VariableDataQuantityUnit.Customer, b => 0),
            new VifeFdTableRecord(0x12, VariableDataQuantityUnit.AccessCodeUser, b => 0),
            new VifeFdTableRecord(0x13, VariableDataQuantityUnit.AccessCodeOperator, b => 0),
            new VifeFdTableRecord(0x14, VariableDataQuantityUnit.AccessCodeSystemOperator, b => 0),
            new VifeFdTableRecord(0x15, VariableDataQuantityUnit.AccessCodeDeveloper, b => 0),
            new VifeFdTableRecord(0x16, VariableDataQuantityUnit.Password, b => 0),
            new VifeFdTableRecord(0x17, VariableDataQuantityUnit.ErrorFlags, b => 0),
            new VifeFdTableRecord(0x18, VariableDataQuantityUnit.ErrorMask, b => 0),
            new VifeFdTableRecord(0x19, VariableDataQuantityUnit.ReservedVIFE_FD_19, b => 0),
            new VifeFdTableRecord(0x1a, VariableDataQuantityUnit.DigitalOutput, b => 0),
            new VifeFdTableRecord(0x1b, VariableDataQuantityUnit.DigitalInput, b => 0),
            new VifeFdTableRecord(0x1c, VariableDataQuantityUnit.Baudrate, b => 0),
            new VifeFdTableRecord(0x1d, VariableDataQuantityUnit.ResponseDelayTime, b => 0),
            new VifeFdTableRecord(0x1e, VariableDataQuantityUnit.Retry, b => 0),
            new VifeFdTableRecord(0x1f, VariableDataQuantityUnit.ReservedVIFE_FD_1f, b => 0),
            new VifeFdTableRecord(0x20, VariableDataQuantityUnit.FirstStorageNr, b => 0),
            new VifeFdTableRecord(0x21, VariableDataQuantityUnit.LastStorageNr, b => 0),
            new VifeFdTableRecord(0x22, VariableDataQuantityUnit.SizeOfStorage, b => 0),
            new VifeFdTableRecord(0x23, VariableDataQuantityUnit.ReservedVIFE_FD_23, b => 0),
            new VifeFdTableRecord(0x24, VariableDataQuantityUnit.StorageInterval, b => 0),
            new VifeFdTableRecord(0x25, VariableDataQuantityUnit.StorageInterval, b => 0),
            new VifeFdTableRecord(0x26, VariableDataQuantityUnit.StorageInterval, b => 0),
            new VifeFdTableRecord(0x27, VariableDataQuantityUnit.StorageInterval, b => b & 0x03),
            new VifeFdTableRecord(0x28, VariableDataQuantityUnit.StorageIntervalMmnth, b => 0),
            new VifeFdTableRecord(0x29, VariableDataQuantityUnit.StorageIntervalYear, b => 0),
            new VifeFdTableRecord(0x2a, VariableDataQuantityUnit.ReservedVIFE_FD_2a, b => 0),
            new VifeFdTableRecord(0x2b, VariableDataQuantityUnit.ReservedVIFE_FD_2b, b => 0),
            new VifeFdTableRecord(0x2c, VariableDataQuantityUnit.DurationSinceLastReadout, b => 0),
            new VifeFdTableRecord(0x2d, VariableDataQuantityUnit.DurationSinceLastReadout, b => 0),
            new VifeFdTableRecord(0x2e, VariableDataQuantityUnit.DurationSinceLastReadout, b => 0),
            new VifeFdTableRecord(0x2f, VariableDataQuantityUnit.DurationSinceLastReadout, b => 0),
            new VifeFdTableRecord(0x30, VariableDataQuantityUnit.StartDateTimeOfTariff, b => 0),
            new VifeFdTableRecord(0x31, VariableDataQuantityUnit.DurationOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x32, VariableDataQuantityUnit.DurationOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x33, VariableDataQuantityUnit.DurationOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x34, VariableDataQuantityUnit.PeriodOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x35, VariableDataQuantityUnit.PeriodOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x36, VariableDataQuantityUnit.PeriodOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x37, VariableDataQuantityUnit.PeriodOfTariff, b => b & 0x03),
            new VifeFdTableRecord(0x38, VariableDataQuantityUnit.PeriodOfTariffMonths, b => 0),
            new VifeFdTableRecord(0x39, VariableDataQuantityUnit.PeriodOfTariffYear, b => 0),
            new VifeFdTableRecord(0x3a, VariableDataQuantityUnit.Dimensionless, b => 0),
            new VifeFdTableRecord(0x3b, VariableDataQuantityUnit.Reserved_FD_3b, b => 0),
            new VifeFdTableRecord(0x3c, VariableDataQuantityUnit.Reserved_FD_3c, b => 0),
            new VifeFdTableRecord(0x3d, VariableDataQuantityUnit.Reserved_FD_3c, b => 0),
            new VifeFdTableRecord(0x3e, VariableDataQuantityUnit.Reserved_FD_3c, b => 0),
            new VifeFdTableRecord(0x3f, VariableDataQuantityUnit.Reserved_FD_3c, b => 0),
            new VifeFdTableRecord(0x40, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x41, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x42, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x43, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x44, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x45, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x46, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x47, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x48, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x49, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4a, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4b, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4c, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4d, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4e, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x4f, VariableDataQuantityUnit.Volts, b => (b & 0x0f) - 9),
            new VifeFdTableRecord(0x50, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x51, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x52, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x53, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x54, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x55, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x56, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x57, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x58, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x59, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5a, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5b, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5c, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5d, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5e, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x5f, VariableDataQuantityUnit.Ampers, b => (b & 0x0f) - 12),
            new VifeFdTableRecord(0x60, VariableDataQuantityUnit.ResetCounter, b => 0),
            new VifeFdTableRecord(0x61, VariableDataQuantityUnit.CumulationCounter, b => 0),
            new VifeFdTableRecord(0x62, VariableDataQuantityUnit.ControlSignal, b => 0),
            new VifeFdTableRecord(0x63, VariableDataQuantityUnit.DayOfWeek, b => 0),
            new VifeFdTableRecord(0x64, VariableDataQuantityUnit.WeekNumber, b => 0),
            new VifeFdTableRecord(0x65, VariableDataQuantityUnit.TimePointOfDayChange, b => 0),
            new VifeFdTableRecord(0x66, VariableDataQuantityUnit.StateOfParameterActivation, b => 0),
            new VifeFdTableRecord(0x67, VariableDataQuantityUnit.SpecialSupplierInformation, b => 0),
            new VifeFdTableRecord(0x68, VariableDataQuantityUnit.DurationSinceLastCumulation, b => 0),
            new VifeFdTableRecord(0x69, VariableDataQuantityUnit.DurationSinceLastCumulation, b => 0),
            new VifeFdTableRecord(0x6a, VariableDataQuantityUnit.DurationSinceLastCumulation, b => 0),
            new VifeFdTableRecord(0x6b, VariableDataQuantityUnit.DurationSinceLastCumulation, b => 0),
            new VifeFdTableRecord(0x6c, VariableDataQuantityUnit.OperatingTimeBattery, b => 0),
            new VifeFdTableRecord(0x6d, VariableDataQuantityUnit.OperatingTimeBattery, b => 0),
            new VifeFdTableRecord(0x6e, VariableDataQuantityUnit.OperatingTimeBattery, b => 0),
            new VifeFdTableRecord(0x6f, VariableDataQuantityUnit.OperatingTimeBattery, b => 0),
            new VifeFdTableRecord(0x70, VariableDataQuantityUnit.DateTimeOfBatteryChange, b => 0),
            new VifeFdTableRecord(0x71, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x72, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x73, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x74, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x75, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x76, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x77, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x78, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x79, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7a, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7b, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7c, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7d, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7e, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
            new VifeFdTableRecord(0x7f, VariableDataQuantityUnit.Reserved_FD_71, b => 0),
        };
    }
}
