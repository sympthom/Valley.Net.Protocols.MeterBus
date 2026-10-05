namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Medium / device type byte of the variable data header (EN 13757-3, EN 13757-7:2018 Table 13).
/// </summary>
/// <remarks>
/// Names follow OMS Vol. 2 Issue 5.0.1 Tables 2-4 and libmbus. Reserved codes have no member;
/// any byte still casts to the enum and keeps its numeric value.
/// </remarks>
public enum DeviceType : byte
{
    Other = 0x00,
    Oil = 0x01,
    Electricity = 0x02,
    Gas = 0x03,

    /// <summary>Heat meter, volume measured at return temperature (outlet).</summary>
    Heat = 0x04,
    Steam = 0x05,

    /// <summary>Warm water meter (30 °C to 90 °C).</summary>
    WarmWater = 0x06,
    Water = 0x07,
    HeatCostAllocator = 0x08,
    CompressedAir = 0x09,

    /// <summary>Cooling load meter, volume measured at return temperature (outlet).</summary>
    CoolingLoadMeterOutlet = 0x0A,

    /// <summary>Cooling load meter, volume measured at flow temperature (inlet).</summary>
    CoolingLoadMeterInlet = 0x0B,

    /// <summary>Heat meter, volume measured at flow temperature (inlet).</summary>
    HeatInlet = 0x0C,

    /// <summary>Combined heat / cooling load meter.</summary>
    HeatCoolingLoadMeter = 0x0D,

    /// <summary>Bus / system component, for example an M-Bus level converter.</summary>
    BusSystemComponent = 0x0E,
    Unknown = 0x0F,
    IrrigationWater = 0x10,
    WaterDataLogger = 0x11,
    GasDataLogger = 0x12,
    GasConverter = 0x13,
    CalorificValue = 0x14,

    /// <summary>Hot water meter (90 °C and above).</summary>
    BoilingWater = 0x15,
    ColdWater = 0x16,

    /// <summary>Dual register (hot/cold) water meter.</summary>
    DualRegisterWater = 0x17,
    Pressure = 0x18,
    ADConverter = 0x19,

    /// <summary>Smoke alarm device.</summary>
    SmokeDetector = 0x1A,

    /// <summary>Room sensor, for example temperature or humidity.</summary>
    RoomSensor = 0x1B,
    GasDetector = 0x1C,
    CarbonMonoxideAlarm = 0x1D,
    HeatAlarm = 0x1E,
    Sensor = 0x1F,

    /// <summary>Breaker (electricity).</summary>
    CircuitBreaker = 0x20,

    /// <summary>Valve (gas or water).</summary>
    Valve = 0x21,

    /// <summary>Customer unit (display device).</summary>
    CustomerUnit = 0x25,
    WasteWater = 0x28,
    Garbage = 0x29,

    /// <summary>Reserved for carbon dioxide.</summary>
    CarbonDioxide = 0x2A,
    CommunicationController = 0x31,
    UnidirectionalRepeater = 0x32,
    BidirectionalRepeater = 0x33,
    RadioConverterSystemSide = 0x36,
    RadioConverterMeterSide = 0x37,
    WiredAdapter = 0x38,
}
