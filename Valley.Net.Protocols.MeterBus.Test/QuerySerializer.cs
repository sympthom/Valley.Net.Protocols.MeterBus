using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valley.Net.Bindings;

namespace Valley.Net.Protocols.MeterBus.Test
{
    internal sealed class QuerySerializer : IPacketSerializer
    {
        public INetworkPacket Deserialize(byte[] data, int index, int count)
        {
            throw new NotImplementedException();
        }

        public INetworkPacket Deserialize(Stream stream)
        {
            throw new NotImplementedException();
        }

        public INetworkPacket Deserialize(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(byte[] data, int index, int count) where T : INetworkPacket
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(Stream stream) where T : INetworkPacket
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(BinaryReader reader) where T : INetworkPacket
        {
            throw new NotImplementedException();
        }

        public INetworkPacket Deserialize(byte[] data)
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(byte[] data) where T : INetworkPacket
        {
            throw new NotImplementedException();
        }

        public int Serialize(INetworkPacket package, Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
                return Serialize(package, writer);
        }

        public int Serialize(INetworkPacket packet, BinaryWriter writer)
        {
            var queryPackage = packet as QueryPacket;

            writer.Write(queryPackage.Data);

            return queryPackage.Data.Length;
        }
    }
}
