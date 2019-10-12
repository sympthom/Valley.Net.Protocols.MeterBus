using System;
using System.Collections.Generic;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    public enum ControlMask
    {
        /// <summary>
        /// Initialization of Slave                                  (SHORT FRAME)
        /// </summary>
        SND_NKE = 0x40,

        /// <summary>
        /// Send User Data to Slave                                 (LONG/CONTROL FRAME)
        /// </summary>
        SND_UD = 0x53,

        /// <summary>
        /// Request for Class 2 Data: 0x4b | 0x5b | 0x6b | 0x7b      (SHORT FRAME)
        /// </summary>
        REQ_UD2 = 0x7b,

        /// <summary>
        /// Request for Class 1 Data: 0x5a | 0x7a                    (SHORT FRAME)
        /// </summary>
        REQ_UD1 = 0x7a,

        /// <summary>
        /// Data Transfer from Slave: 0x08 | 0x18 | 0x28 | 0x38      (LONG/CONTROL FRAME)
        /// </summary>
        RSP_UD = 0x08,

        FCB = 0x20,
        FCV = 0x10,

        ACD = 0x20,
        DFC = 0x10,

        DIR = 0x40,
        DIR_M2S = 0x40,
        DIR_S2M = 0x00,
    }
}
