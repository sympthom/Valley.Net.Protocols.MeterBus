using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum VariableDataQuantityUnit
    {
        Undefined,
        EnergyWh,
        EnergyJ,
        Volume_m3,
        Mass_kg,
        OnTime,
        OperatingTime,
        PowerW,
        PowerJ_per_h,
        VolumeFlowM3_per_h,
        VolumeFlowExtM3_per_min,
        VolumeFlowExtM3_per_s,
        MassFlowKg_per_h,
        FlowTemperatureC,
        ReturnTemperatureC,
        TemperatureDifferenceK,
        ExternalTemperatureC,
        PressureBar,
        TimePoint,
        UnitsForHCA,
        Reserved,
        AveragingDuration,
        ActualityDuration,
        FabricationNo,
        EnhancedIdentification,
        BusAddress,
        Extension_7B,
        VIF_string, // (length in first byte)
        Extension_7D,
        AnyVIF,
        CustomVIF,
        ManufacturerSpecific,
        // VIFE
        ErrorCodesVIFE, // Reserved for object actions (master to slave): see table on page 75
                        // or for error codes (slave to master): see table on page 74"
        Per_second,
        Per_minute,
        Per_hour,
        Per_day,
        Per_week,
        Per_month,
        Per_year,
        Per_RevolutionMeasurement,
        Increment_per_inputPulseOnInputChannel0,
        Increment_per_inputPulseOnInputChannel1,
        Increment_per_outputPulseOnOutputChannel0,
        Increment_per_outputPulseOnOutputChannel1,
        Per_liter,
        Per_m3,
        Per_kg,
        Per_Kelvin,
        Per_kWh,
        Per_GJ,
        Per_kW,
        Per_KelvinLiter,
        Per_Volt,
        Per_Ampere,
        MultipliedBySek,
        MultipliedBySek_per_V,
        MultipliedBySek_per_A,
        StartDateTimeOf,
        UncorrectedUnit, //VIF contains uncorrected unit instead of corrected unit
        AccumulationPositive, //Accumulation only if positive contributions
        AccumulationNegative, //Accumulation of abs value only if negative contributions
        ReservedVIFE_3D, // 0x3d..0x3f - ReservedVIFE
        LimitValue,
        NrOfLimitExceeds,
        DateTimeOfLimitExceed,
        DurationOfLimitExceed,
        DurationOfLimitAbove,
        ReservedVIFE_68,
        DateTimeOfLimitAbove,
        MultiplicativeCorrectionFactor,
        AdditiveCorrectionConstant,
        ReservedVIFE_7C,
        MultiplicativeCorrectionFactor1000,
        ReservedVIFE_7E,
        //ManufacturerSpecific,
        // VIFE_FB
        EnergyMWh,
        ReservedVIFE_FB_02,
        ReservedVIFE_FB_04,
        EnergyGJ,
        ReservedVIFE_FB_0a,
        ReservedVIFE_FB_0c,
        //Volume_m3,
        ReservedVIFE_FB_12,
        ReservedVIFE_FB_14,
        Mass_t,
        ReservedVIFE_FB_1a,
        Volume_feet3,
        Volume_american_gallon,
        Volume_flow_american_gallon_per_min,
        Volume_flow_american_gallon_per_h,
        ReservedVIFE_FB_27,
        Power_MW,
        ReservedVIFE_FB_2a,
        ReservedVIFE_FB_2c,
        Power_GJ_per_h,
        ReservedVIFE_FB_32,
        FlowTemperature_F,
        ReturnTemperature_F,
        TemperatureDifference_F,
        ExternalTemperature_F,
        ReservedVIFE_FB_68,
        ColdWarmTemperatureLimit_F,
        ColdWarmTemperatureLimit_C,
        CumulCountMaxPower_W,
        // VIFE_FD
        Credit, // Credit of 10nn-3 of the nominal local legal currency units
        Debit, // Debit of 10nn-3 of the nominal local legal currency units
        AccessNumber, // Access Number (transmission count)
        Medium, // Medium (as in fixed header)
        Manufacturer, // Manufacturer (as in fixed header)
        //EnhancedIdentification, // Parameter set identification
        ModelVersion, // Model / Version
        HardwareVersionNr,
        FirmwareVersionNr,
        SoftwareVersionNr,
        CustomerLocation,
        Customer,
        AccessCodeUser,
        AccessCodeOperator,
        AccessCodeSystemOperator,
        AccessCodeDeveloper,
        Password,
        ErrorFlags, // (binary)
        ErrorMask,
        ReservedVIFE_FD_19,
        DigitalOutput, // (binary)
        DigitalInput, // (binary)
        Baudrate, // [Baud]
        ResponseDelayTime, // [bittimes]
        Retry,
        ReservedVIFE_FD_1f,
        FirstStorageNr, // for cyclic storage
        LastStorageNr, // for cyclic storage
        SizeOfStorage, // Size of storage block
        ReservedVIFE_FD_23,
        StorageInterval, // [sec(s)..day(s)]
        StorageIntervalMmnth, // month(s)
        StorageIntervalYear, // year(s)
        ReservedVIFE_FD_2a,
        ReservedVIFE_FD_2b,
        DurationSinceLastReadout, // [sec(s)..day(s)]
        StartDateTimeOfTariff,
        DurationOfTariff, // (nn = 01..11: min to days)
        PeriodOfTariff, // [sec(s) to day(s)]
        PeriodOfTariffMonths, // months(s)
        PeriodOfTariffYear, // year(s)
        Dimensionless, // no VIF
        Reserved_FD_3b,
        Reserved_FD_3c,
        Volts, // 10nnnn-9 
        Ampers, // 10nnnn-12
        ResetCounter,
        CumulationCounter,
        ControlSignal,
        DayOfWeek,
        WeekNumber,
        TimePointOfDayChange,
        StateOfParameterActivation,
        SpecialSupplierInformation,
        DurationSinceLastCumulation, // [hour(s)..years(s)]
        OperatingTimeBattery, // [hour(s)..years(s)]
        DateTimeOfBatteryChange,
        Reserved_FD_71,
    }
}
