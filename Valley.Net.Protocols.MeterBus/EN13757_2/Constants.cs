using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public sealed class Constants
    {
        //------------------------------------------------------------------------------
        // FRAME types
        //
        public const byte MBUS_FRAME_TYPE_ANY = 0x00;
        public const byte MBUS_FRAME_TYPE_ACK = 0x01;
        public const byte MBUS_FRAME_TYPE_SHORT = 0x02;
        public const byte MBUS_FRAME_TYPE_CONTROL = 0x03;
        public const byte MBUS_FRAME_TYPE_LONG = 0x04;

        public const int MBUS_FRAME_ACK_BASE_SIZE = 1;
        public const int MBUS_FRAME_SHORT_BASE_SIZE = 5;
        public const int MBUS_FRAME_CONTROL_BASE_SIZE = 9;
        public const int MBUS_FRAME_LONG_BASE_SIZE = 9;

        public const int MBUS_FRAME_BASE_SIZE_ACK = 1;
        public const int MBUS_FRAME_BASE_SIZE_SHORT = 5;
        public const int MBUS_FRAME_BASE_SIZE_CONTROL = 9;
        public const int MBUS_FRAME_BASE_SIZE_LONG = 9;

        public const int MBUS_FRAME_FIXED_SIZE_ACK = 1;
        public const int MBUS_FRAME_FIXED_SIZE_SHORT = 5;
        public const int MBUS_FRAME_FIXED_SIZE_CONTROL = 6;
        public const int MBUS_FRAME_FIXED_SIZE_LONG = 6;

        //
        // Frame start/stop bits
        //
        public const byte MBUS_FRAME_ACK_START = 0xE5;
        public const byte MBUS_FRAME_SHORT_START = 0x10;
        public const byte MBUS_FRAME_CONTROL_START = 0x68;
        public const byte MBUS_FRAME_LONG_START = 0x68;
        public const byte MBUS_FRAME_STOP = 0x16;

        //
        //
        //
        public const int MBUS_MAX_PRIMARY_SLAVES = 250;

        //
        // Control field
        //
        public const byte MBUS_CONTROL_FIELD_DIRECTION = 0x07;
        public const byte MBUS_CONTROL_FIELD_FCB = 0x06;
        public const byte MBUS_CONTROL_FIELD_ACD = 0x06;
        public const byte MBUS_CONTROL_FIELD_FCV = 0x05;
        public const byte MBUS_CONTROL_FIELD_DFC = 0x05;
        public const byte MBUS_CONTROL_FIELD_F3 = 0x04;
        public const byte MBUS_CONTROL_FIELD_F2 = 0x03;
        public const byte MBUS_CONTROL_FIELD_F1 = 0x02;
        public const byte MBUS_CONTROL_FIELD_F0 = 0x01;

        public const byte MBUS_CONTROL_MASK_SND_NKE = 0x40; // Initialization of Slave                                  (SHORT FRAME)
        public const byte MBUS_CONTROL_MASK_SND_UD = 0x53;  // Send User Data to Slave                                  (LONG/CONTROL FRAME)
        public const byte MBUS_CONTROL_MASK_REQ_UD2 = 0x7B; // Request for Class 2 Data: 0x4b | 0x5b | 0x6b | 0x7b      (SHORT FRAME)
        public const byte MBUS_CONTROL_MASK_REQ_UD1 = 0x7A; // Request for Class 1 Data: 0x5a | 0x7a                     (SHORT FRAME)
        public const byte MBUS_CONTROL_MASK_RSP_UD = 0x08;  // Data Transfer from Slave: 08 | 18 | 28 | 38              (LONG/CONTROL FRAME)

        public const byte MBUS_CONTROL_MASK_FCB = 0x20;
        public const byte MBUS_CONTROL_MASK_FCV = 0x10;

        public const byte MBUS_CONTROL_MASK_ACD = 0x20;
        public const byte MBUS_CONTROL_MASK_DFC = 0x10;

        public const byte MBUS_CONTROL_MASK_DIR = 0x40;
        public const byte MBUS_CONTROL_MASK_DIR_M2S = 0x40;
        public const byte MBUS_CONTROL_MASK_DIR_S2M = 0x00;

        //
        // Address field
        //
        public const byte MBUS_ADDRESS_BROADCAST_REPLY = 0xFE;
        public const byte MBUS_ADDRESS_BROADCAST_NOREPLY = 0xFF;
        public const byte MBUS_ADDRESS_NETWORK_LAYER = 0xFD;

        //
        // Control Information field
        //
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

        public const byte MBUS_CONTROL_INFO_DATA_SEND = 0x51;
        public const byte MBUS_CONTROL_INFO_DATA_SEND_MSB = 0x55;
        public const byte MBUS_CONTROL_INFO_SELECT_SLAVE = 0x52;
        public const byte MBUS_CONTROL_INFO_SELECT_SLAVE_MSB = 0x56;
        public const byte MBUS_CONTROL_INFO_APPLICATION_RESET = 0x50;
        public const byte MBUS_CONTROL_INFO_SYNC_ACTION = 0x54;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_300 = 0xB8;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_600 = 0xB9;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_1200 = 0xBA;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_2400 = 0xBB;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_4800 = 0xBC;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_9600 = 0xBD;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_19200 = 0xBE;
        public const byte MBUS_CONTROL_INFO_SET_BAUDRATE_38400 = 0xBF;
        public const byte MBUS_CONTROL_INFO_REQUEST_RAM_READ = 0xB1;
        public const byte MBUS_CONTROL_INFO_SEND_USER_DATA = 0xB2;
        public const byte MBUS_CONTROL_INFO_INIT_TEST_CALIB = 0xB3;
        public const byte MBUS_CONTROL_INFO_EEPROM_READ = 0xB4;
        public const byte MBUS_CONTROL_INFO_SW_TEST_START = 0xB6;

        //Mode 1 Mode 2                   Application                   Definition in
        // 70h             report of general application errors     Usergroup March 94
        // 71h                      report of alarm status          Usergroup March 94
        // 72h   76h                variable data respond                EN1434-3
        // 73h   77h                 fixed data respond                  EN1434-3
        public const byte MBUS_CONTROL_INFO_ERROR_GENERAL = 0x70;
        public const byte MBUS_CONTROL_INFO_STATUS_ALARM = 0x71;

        public const byte MBUS_CONTROL_INFO_RESP_FIXED = 0x73;
        public const byte MBUS_CONTROL_INFO_RESP_FIXED_MSB = 0x77;

        public const byte MBUS_CONTROL_INFO_RESP_VARIABLE = 0x72;
        public const byte MBUS_CONTROL_INFO_RESP_VARIABLE_MSB = 0x76;

        //
        // DATA BITS
        //
        public const byte MBUS_DATA_FIXED_STATUS_FORMAT_MASK = 0x80;
        public const byte MBUS_DATA_FIXED_STATUS_FORMAT_BCD = 0x00;
        public const byte MBUS_DATA_FIXED_STATUS_FORMAT_INT = 0x80;
        public const byte MBUS_DATA_FIXED_STATUS_DATE_MASK = 0x40;
        public const byte MBUS_DATA_FIXED_STATUS_DATE_STORED = 0x40;
        public const byte MBUS_DATA_FIXED_STATUS_DATE_CURRENT = 0x00;


        //
        // data record fields
        //
        public const byte MBUS_DATA_RECORD_DIF_MASK_INST = 0x00;
        public const byte MBUS_DATA_RECORD_DIF_MASK_MIN = 0x10;

        public const byte MBUS_DATA_RECORD_DIF_MASK_TYPE_INT32 = 0x04;
        public const byte MBUS_DATA_RECORD_DIF_MASK_DATA = 0x0F;
        public const byte MBUS_DATA_RECORD_DIF_MASK_FUNCTION = 0x30;
        public const byte MBUS_DATA_RECORD_DIF_MASK_STORAGE_NO = 0x40;
        public const byte MBUS_DATA_RECORD_DIF_MASK_EXTENTION = 0x80;
        public const byte MBUS_DATA_RECORD_DIF_MASK_NON_DATA = 0xF0;

        public const byte MBUS_DATA_RECORD_DIFE_MASK_STORAGE_NO = 0x0F;
        public const byte MBUS_DATA_RECORD_DIFE_MASK_TARIFF = 0x30;
        public const byte MBUS_DATA_RECORD_DIFE_MASK_DEVICE = 0x40;
        public const byte MBUS_DATA_RECORD_DIFE_MASK_EXTENSION = 0x80;

        //
        // GENERAL APPLICATION ERRORS
        //
        public const byte MBUS_ERROR_DATA_UNSPECIFIED = 0x00;
        public const byte MBUS_ERROR_DATA_UNIMPLEMENTED_CI = 0x01;
        public const byte MBUS_ERROR_DATA_BUFFER_TOO_LONG = 0x02;
        public const byte MBUS_ERROR_DATA_TOO_MANY_RECORDS = 0x03;
        public const byte MBUS_ERROR_DATA_PREMATURE_END = 0x04;
        public const byte MBUS_ERROR_DATA_TOO_MANY_DIFES = 0x05;
        public const byte MBUS_ERROR_DATA_TOO_MANY_VIFES = 0x06;
        public const byte MBUS_ERROR_DATA_RESERVED = 0x07;
        public const byte MBUS_ERROR_DATA_APPLICATION_BUSY = 0x08;
        public const byte MBUS_ERROR_DATA_TOO_MANY_READOUTS = 0x09;

        //
        // FIXED DATA FLAGS
        //

        //
        // VARIABLE DATA FLAGS
        //
        public const byte MBUS_VARIABLE_DATA_MEDIUM_OTHER = 0x00;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_OIL = 0x01;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_ELECTRICITY = 0x02;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_GAS = 0x03;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_HEAT_OUT = 0x04;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_STEAM = 0x05;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_HOT_WATER = 0x06;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_WATER = 0x07;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_HEAT_COST = 0x08;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_COMPR_AIR = 0x09;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_COOL_OUT = 0x0A;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_COOL_IN = 0x0B;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_HEAT_IN = 0x0C;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_HEAT_COOL = 0x0D;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_BUS = 0x0E;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_UNKNOWN = 0x0F;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_COLD_WATER = 0x16;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_DUAL_WATER = 0x17;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_PRESSURE = 0x18;
        public const byte MBUS_VARIABLE_DATA_MEDIUM_ADC = 0x19;
    }
}
