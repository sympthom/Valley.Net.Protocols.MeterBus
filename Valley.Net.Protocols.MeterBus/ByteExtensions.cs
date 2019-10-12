using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    public static class ByteExtensions
    {
        public static Frame AsMeterBusFrame(this byte[] buffer)
        {
            var frame = new MeterbusFrameSerializer()
                .Deserialize(buffer, 0, buffer.Length);

            return frame as Frame;
        }

        public static byte CheckSum(this byte[] buffer)
        {
            return (byte)buffer.Sum(b => b);
        }

        public static byte CheckSum(this byte[] buffer, byte control, byte address)
        {
            return (byte)new byte[] { control, address }.Merge(buffer).Sum(b => b);
        }

        public static byte CheckSum(this byte[] buffer, byte control, byte address, byte controlInformation)
        {
            return (byte)new byte[] { control, address, controlInformation }.Merge(buffer).Sum(b => b);
        }

        public static byte CheckSum(this byte[] buffer, int offset, int length)
        {
            return (byte)buffer.Skip(offset).Take(length).Sum(b => b);
        }

        public static byte[] Merge(this byte[] source, byte[] arrayB)
        {
            var buffer = new byte[source.Length + arrayB.Length];

            Buffer.BlockCopy(source.ToArray(), 0, buffer, 0, source.Count());
            Buffer.BlockCopy(arrayB, 0, buffer, source.Count(), arrayB.Length);

            return buffer;
        }

        public static string ToHex(this byte[] source, string separator = " ")
        {
            return source != null ? string.Join(separator, source.Select(x => x.ToString("x2"))) : null;
        }

        public static string BCDDecode(this byte[] bytes, int length)
        {
            long val = 0;

            for (int i = length; i > 0; i--)
            {
                val = (val * 10) + ((bytes[i - 1] >> 4) & 0xF);
                val = (val * 10) + (bytes[i - 1] & 0xF);
            }

            return val.ToString();
        }

        public static string BCDToString(this byte[] bytes)
        {
            return string.Join(string.Empty, bytes.Reverse().Select(b => b.ToString("x2")));
        }
    }
}
