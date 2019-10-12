using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum Function : byte
    {
        Instantaneous = 0x00,    //Instantaneous value

        Maximum = 0x10,          //Maximum value

        Minimum = 0x20,          //Minimum value

        ValueDuringError = 0x30, //Value during error state
    }
}
