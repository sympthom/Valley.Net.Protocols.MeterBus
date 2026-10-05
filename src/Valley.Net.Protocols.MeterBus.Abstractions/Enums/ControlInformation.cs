namespace Valley.Net.Protocols.MeterBus;

// Mode 2 (the *_MSB codes) sends multi-byte values most significant byte first. EN 13757-3 marks it
// with bit 2 (0x04) of the mode 1 code, e.g. 51h/55h data send, 72h/76h variable data respond.
public enum ControlInformation : byte
{
    DATA_SEND = 0x51,
    DATA_SEND_MSB = 0x55,
    SELECT_SLAVE = 0x52,
    SELECT_SLAVE_MSB = 0x56,
    APPLICATION_RESET = 0x50,
    SYNC_ACTION = 0x54,
    SET_BAUDRATE_300 = 0xB8,
    SET_BAUDRATE_600 = 0xB9,
    SET_BAUDRATE_1200 = 0xBA,
    SET_BAUDRATE_2400 = 0xBB,
    SET_BAUDRATE_4800 = 0xBC,
    SET_BAUDRATE_9600 = 0xBD,
    SET_BAUDRATE_19200 = 0xBE,
    SET_BAUDRATE_38400 = 0xBF,
    REQUEST_RAM_READ = 0xB1,
    SEND_USER_DATA = 0xB2,
    INIT_TEST_CALIB = 0xB3,
    EEPROM_READ = 0xB4,
    SW_TEST_START = 0xB6,
    ERROR_GENERAL = 0x70,
    STATUS_ALARM = 0x71,
    RESP_FIXED = 0x73,
    RESP_FIXED_MSB = 0x77,
    RESP_VARIABLE = 0x72,
    RESP_VARIABLE_MSB = 0x76,
}
