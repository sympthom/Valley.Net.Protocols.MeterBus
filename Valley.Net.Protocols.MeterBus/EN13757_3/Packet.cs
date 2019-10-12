using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_3
{
    public abstract class Packet
    {
        public bool AccessDemand { get; set; }

        public bool DataFlowControl { get; set; }

        public byte Address { get; set; }

        internal Packet()
        {
           
        }

        public override string ToString()
        {
            return string.Format("Address={0:x2}, AccessDemand={1}, DataFlowControl={2}", Address, AccessDemand, DataFlowControl);
        }
    }
}
