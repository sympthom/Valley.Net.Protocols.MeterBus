using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum ControlInformation
    {
        //Mode 1 Mode 2                   Application                   Definition in
        // 51h    55h                       data send                    EN1434-3
        // 52h    56h                  selection of slaves           Usergroup July  ́93
        // 50h                          application reset           Usergroup March  ́94
        // 54h                          synronize action                 suggestion
        // B8h                     set baudrate to 300 baud          Usergroup July  ́93
        // B9h                     set baudrate to 600 baud          Usergroup July  ́93
        // BAh                    set baudrate to 1200 baud          Usergroup July  ́93
        // BBh                    set baudrate to 2400 baud          Usergroup July  ́93
        // BCh                    set baudrate to 4800 baud          Usergroup July  ́93
        // BDh                    set baudrate to 9600 baud          Usergroup July  ́93
        // BEh                   set baudrate to 19200 baud              suggestion
        // BFh                   set baudrate to 38400 baud              suggestion
        // B1h           request readout of complete RAM content     Techem suggestion
        // B2h          send user data (not standardized RAM write) Techem suggestion
        // B3h                 initialize test calibration mode      Usergroup July  ́93
        // B4h                           EEPROM read                 Techem suggestion
        // B6h                         start software test           Techem suggestion
        // 90h to 97h              codes used for hashing           longer recommended
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

        //Mode 1 Mode 2                   Application                   Definition in
        // 70h             report of general application errors     Usergroup March 94
        // 71h                      report of alarm status          Usergroup March 94
        // 72h   76h                variable data respond                EN1434-3
        // 73h   77h                 fixed data respond                  EN1434-3
        ERROR_GENERAL = 0x70,
        STATUS_ALARM = 0x71,

        RESP_FIXED = 0x73,
        RESP_FIXED_MSB = 0x77,

        RESP_VARIABLE = 0x72,
        RESP_VARIABLE_MSB = 0x76,
    }
}
