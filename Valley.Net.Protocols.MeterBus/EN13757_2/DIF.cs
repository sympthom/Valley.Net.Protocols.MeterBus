using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus.EN13757_2
{
    /// <summary>
    /// Data Information Field.
    /// </summary>
    public sealed class DIF : Part
    {
        public VariableDataRecordType Data { get; private set; }

        public DataTypes DataType { get; private set; }

        public Function Function { get; private set; }

        public bool StorageLSB { get; private set; }

        public bool Extension { get; private set; }

        public DIF(byte data)
        {
            Data = (VariableDataRecordType)data;
            DataType = (DataTypes)(data & 0x0f);
            Function = (Function)(data & 0x30);
            StorageLSB = (data & 0x40) != 0;
            Extension = (data & 0x80) != 0;
        }
    }
}
