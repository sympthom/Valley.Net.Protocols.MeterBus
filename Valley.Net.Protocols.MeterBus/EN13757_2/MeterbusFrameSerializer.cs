using Valley.Net.Protocols.MeterBus;
using Valley.Net.Protocols.MeterBus.EN13757_2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Valley.Net.Bindings;

namespace Valley.Net.Protocols.MeterBus
{
    public sealed class MeterbusFrameSerializer : IPacketSerializer
    {
        public INetworkPacket Deserialize(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var stream = new MemoryStream(data))
                return Deserialize(stream);
        }

        public T Deserialize<T>(byte[] data) where T : INetworkPacket
        {
            return (T)Deserialize(data);
        }

        public INetworkPacket Deserialize(byte[] data, int index, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            using (var stream = new MemoryStream(data, index, count))
                return Deserialize(stream);
        }

        public T Deserialize<T>(byte[] data, int index, int count) where T : INetworkPacket
        {
            return (T)Deserialize(data, index, count);
        }

        public INetworkPacket Deserialize(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var reader = new BinaryReader(stream))
                return Deserialize(reader);
        }

        public T Deserialize<T>(Stream stream) where T : INetworkPacket
        {
            return (T)Deserialize(stream);
        }

        public INetworkPacket Deserialize(BinaryReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            INetworkPacket packet = null;

            var start1 = reader.ReadByte();

            switch (start1)
            {
                case Constants.MBUS_FRAME_ACK_START:
                    {
                        packet = new AckFrame();
                    }
                    break;
                case Constants.MBUS_FRAME_SHORT_START:
                    {
                        //if (stream.Length != 5)
                        //    return false;

                        if (start1 != Constants.MBUS_FRAME_SHORT_START)
                            return packet;

                        var control = reader.ReadByte();
                        var address = reader.ReadByte();
                        var crc = reader.ReadByte();

                        if (crc != new byte[] { control, address }.CheckSum())
                            return packet;

                        var stop = reader.ReadByte();

                        if (stop != Constants.MBUS_FRAME_STOP)
                            return packet;

                        packet = new ShortFrame(control, address);
                    }
                    break;
                case Constants.MBUS_FRAME_LONG_START:
                    {
                        const int MBUS_FRAME_FIXED_SIZE_LONG = 6;

                        var length1 = reader.ReadByte();
                        var length2 = reader.ReadByte();

                        if (length1 < 3)
                            return packet;

                        if (length1 != length2)
                            return packet;

                        //if (stream.Length < length1 + MBUS_FRAME_FIXED_SIZE_LONG)
                        //    return false;

                        var start2 = reader.ReadByte();

                        if (start2 != Constants.MBUS_FRAME_LONG_START)
                            return packet;

                        var control = reader.ReadByte();
                        var address = reader.ReadByte();
                        var controlInformation = reader.ReadByte();

                        var data = reader.ReadBytes(length1 - 3);

                        if (data.Length != length1 - 3)
                            return packet;

                        var crc = reader.ReadByte();
                        var stop = reader.ReadByte();

                        if (crc != new byte[] { control, address, controlInformation }.Merge(data).CheckSum())
                            return packet;

                        if (length1 - 3 == 0)
                            packet = new ControlFrame(control, controlInformation, address);
                        else if ((ControlInformation)controlInformation == ControlInformation.RESP_VARIABLE)
                            packet = new VariableDataLongFrame(control, controlInformation, address, data, length1);
                        else if ((ControlInformation)controlInformation == ControlInformation.RESP_FIXED)
                            packet = new FixedDataLongFrame(control, controlInformation, address, data, length1);
                        else
                            throw new NotImplementedException();
                    }
                    break;
            }

            return packet;
        }

        public T Deserialize<T>(BinaryReader reader) where T : INetworkPacket
        {
            return (T)Deserialize(reader);
        }

        public int Serialize(INetworkPacket packet, Stream stream)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            using (var writer = new BinaryWriter(stream))
                return Serialize(packet, writer);
        }

        public int Serialize(INetworkPacket packet, BinaryWriter writer)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            if (writer == null)
                throw new ArgumentNullException(nameof(writer));

            switch (packet)
            {
                case AckFrame ackFrame: return Serialize(ackFrame, writer);
                case ShortFrame shortFrame: return Serialize(shortFrame, writer);
                case LongFrame longFrame: return Serialize(longFrame, writer);
                case ControlFrame controlFrame: return Serialize(controlFrame, writer);
                default: throw new NotImplementedException();
            }
        }

        private static int Serialize(ShortFrame frame, BinaryWriter writer)
        {
            writer.Write(frame.Start);
            writer.Write((byte)frame.Control);
            writer.Write(frame.Address);
            writer.Write(frame.Crc);
            writer.Write(frame.Stop);

            return 5;
        }

        private static int Serialize(LongFrame frame, BinaryWriter writer)
        {
            writer.Write(frame.Start);
            writer.Write((byte)(frame.Data.Length + 3));
            writer.Write((byte)(frame.Data.Length + 3));
            writer.Write(frame.Start);
            writer.Write((byte)frame.Control);
            writer.Write(frame.Address);
            writer.Write((byte)frame.ControlInformation);
            writer.Write(frame.Data, 0, frame.Data.Length);
            writer.Write(frame.Crc);
            writer.Write(frame.Stop);

            return 9 + frame.Data.Length;
        }

        private static int Serialize(AckFrame frame, BinaryWriter writer)
        {
            writer.Write(Constants.MBUS_FRAME_ACK_START);

            return 1;
        }

        private static int Serialize(ControlFrame frame, BinaryWriter writer)
        {
            writer.Write(frame.Start);
            writer.Write(frame.Length);
            writer.Write(frame.Length);
            writer.Write(frame.Start);
            writer.Write(frame.Crc);
            writer.Write(frame.Stop);

            return 6;
        }
    }
}
