using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum VariableDataRecordType : byte
    {
        MBUS_DIB_DIF_WITHOUT_EXTENSION = 0x7F,

        MBUS_DIB_DIF_EXTENSION_BIT = 0x80,

        MBUS_DIB_VIF_WITHOUT_EXTENSION = 0x7F,

        MBUS_DIB_VIF_EXTENSION_BIT = 0x80,

        MBUS_DIB_DIF_MANUFACTURER_SPECIFIC = 0x0F,

        MBUS_DIB_DIF_MORE_RECORDS_FOLLOW = 0x1F,

        MBUS_DIB_DIF_IDLE_FILLER = 0x2F,
    }
}
