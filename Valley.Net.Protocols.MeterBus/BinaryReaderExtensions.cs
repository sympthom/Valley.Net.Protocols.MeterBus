using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Valley.Net.Protocols.MeterBus
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadString2(this BinaryReader reader)
        {
            var Length = reader.ReadByte();
            var buffer = reader.ReadBytes(Length);

            Array.Reverse(buffer);

            return ASCIIEncoding.ASCII.GetString(buffer);
        }

        public static object ReadValue(this BinaryReader reader)
        {
            var Length = reader.ReadByte();

            if (Length >= 0x00 && Length <= 0xbf)
            { // ASCII string with LVAR characters
                var buf = reader.ReadBytes(Length);
                Array.Reverse(buf);
                return ASCIIEncoding.ASCII.GetString(buf);
            }
            else if ((Length >= 0xc0) && (Length <= 0xcf))
            { // positive BCD number with (LVAR - C0h) · 2 digits
                Length -= 0xc0;
                var buf = reader.ReadBytes(Length);

                if (Length <= 1)
                    return sbyte.Parse(buf.BCDToString());
                else if (Length <= 2)
                    return Int16.Parse(buf.BCDToString());
                else if (Length <= 4)
                    return Int32.Parse(buf.BCDToString());
                else if (Length <= 9)
                    return Int64.Parse(buf.BCDToString());
                else
                    throw new OverflowException();
            }
            else if ((Length >= 0xd0) && (Length <= 0xdf))
            { // negative BCD number with (LVAR - D0h) · 2 digits
                Length -= 0xd0;
                var buf = reader.ReadBytes(Length);

                if (Length <= 1)
                    return -sbyte.Parse(buf.BCDToString());
                else if (Length <= 2)
                    return -Int16.Parse(buf.BCDToString());
                else if (Length <= 4)
                    return -Int32.Parse(buf.BCDToString());
                else if (Length <= 9)
                    return -Int64.Parse(buf.BCDToString());
                else
                    throw new OverflowException();
            }
            else if ((Length >= 0xe0) && (Length <= 0xef))
            { // binary number with (LVAR - E0h) bytes
                Length -= 0xe0;
                var buf = reader.ReadBytes(Length);

                if (Length <= 1)
                    return (sbyte)(new byte[1 - buf.Length].Concat(buf).ToArray())[0];
                else if (Length <= 2)
                    return BitConverter.ToInt16(new byte[2 - buf.Length].Concat(buf).ToArray(), 0);
                else if (Length <= 4)
                    return BitConverter.ToInt32(new byte[4 - buf.Length].Concat(buf).ToArray(), 0);
                else if (Length <= 8)
                    return BitConverter.ToInt64(new byte[8 - buf.Length].Concat(buf).ToArray(), 0);
                else
                    throw new OverflowException();
            }
            else if ((Length >= 0xf0) && (Length <= 0xfa))
            { // floating point number with (LVAR - F0h) bytes [to be defined]
                Length -= 0xf0;
                var buf = reader.ReadBytes(Length);

                if (Length == sizeof(Single))
                    return BitConverter.ToSingle(buf, 0);
                else if (Length == sizeof(Double))
                    return BitConverter.ToDouble(buf, 0);
                else
                    throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }

        public static bool EOF(this BinaryReader reader)
        {
            var bs = reader.BaseStream;

            return bs.Position == bs.Length;
        }

        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;

            using (var stream = new MemoryStream())
            {
                var buffer = new byte[bufferSize];
                int count;

                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    stream.Write(buffer, 0, count);

                return stream.ToArray();
            }
        }
    }
}
