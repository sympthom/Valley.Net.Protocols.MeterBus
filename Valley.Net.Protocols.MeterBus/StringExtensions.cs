using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    public static class StringExtensions
    {
        public static byte[] HexToBytes(this string value)
        {
            value = value.Replace(" ", string.Empty);

            return Enumerable
                .Range(0, value.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(value.Substring(x, 2), 16))
                .ToArray();
        }
    }
}
