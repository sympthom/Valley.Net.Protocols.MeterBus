using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    public sealed class ApplicationErrorPacket : Packet
    {
        public enum Codes : byte
        {
            Unspecified = 0, //Unspecified error: also if data field is missing
            Unimplemented_CI = 1, //Unimplemented CI-Field
            BufferTooLong = 2, //Buffer too long, truncated
            TooManyRecords = 3, //Too many records
            PrematureEnd = 4, //Premature end of record
            TooManyDIFEs = 5, //More than 10 DIFE´s
            TooManyVIFEs = 6, //More than 10 VIFE´s
            Reserved = 7, //Reserved
            Busy = 8, //Application too busy for handling readout request
            TooManyReadouts = 9, //Too many readouts(for slaves with limited readouts per time)            
        }

        public Codes Code { get; set; }

        public ApplicationErrorPacket(byte address, byte code)
        {
            Address = address;
            Code = (Codes)code;
        }

        public override string ToString()
        {
            return string.Format("{0}({1}):{2}", this.GetType().Name, base.ToString(), Code);
        }
    }
}
