using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Valley.Net.Protocols.MeterBus.Frames;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Valley.Net.Protocols.MeterBus.Serializers.Packet;

namespace Valley.Net.Protocols.MeterBus.Test
{
    [TestClass]
    public sealed class PacketSerializationTests
    {
        [TestMethod]
        public void Test()
        {
            /*
             68 1F 1F 68 header of RSP_UD telegram (length 1Fh=31d bytes)
08 02 72 C field = 08 (RSP), address 2, CI field 72H (var.,LSByte first)
78 56 34 12 identification number = 12345678
24 40 01 07 manufacturer ID = 4024h (PAD in EN 61107), generation 1, water
55 00 00 00 TC = 55h = 85d, Status = 00h, Signature = 0000h
03 13 15 31 00 Data block 1: unit 0, storage No 0, no tariff, instantaneous volume,
12565 l (24 bit integer)
DA 02 3B 13 01 Data block 2: unit 0, storage No 5, no tariff, maximum volume flow,
113 l/h (4 digit BCD)
8B 60 04 37 18 02 Data block 3: unit 1, storage No 0, tariff 2, instantaneous energy,
44 6 Application Layer
218,37 kWh (6 digit BCD)
18 16 checksum and stopsign
             */
            var data = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_Elvaco_Water_Meter()
        {
            var data = "68 30 30 68 08 03 72 01 22 13 14 B4 09 05 07 9C 00 00 00 0C 78 42 03 00 62 02 75 01 00 01 FD 71 1E 0C 13 27 00 00 00 0F 0D 2E 00 00 00 00 13 02 10 01 08 18 32 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_Piigab_900S_1()
        {
            var data = "68 35 35 68 08 0A 72 64 81 02 15 B4 09 05 07 1C 00 00 00 0C 78 05 49 78 16 03 74 03 00 00 01 FD 71 A7 06 6D 1A 2F 4A 51 27 1D 0C 13 42 01 00 00 0F 09 2E 37 24 12 17 07 18 3B 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_Piigab_900S_2()
        {
            var data = "68 36 36 68 08 0a 72 69 21 00 00 b0 5c 01 1b 49 08 00 00 0c 78 05 49 78 16 03 74 81 00 00 01 fd 71 ac 06 6d 05 19 76 52 27 1d 2f 2f 0a 66 67 02 02 fd 97 1d 01 00 2f 2f 2f 2f 3e 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_abb_delta()
        {
            var data = "68 98 98 68 08 01 72 12 34 56 78 42 04 02 02 45 00 00 00 0E 84 00 00 00 00 00 00 00 8E 10 84 00 00 00 00 00 00 00 8E 20 84 00 00 00 00 00 00 00 8E B0 00 84 00 00 00 00 00 00 00 8E 80 10 84 00 00 00 00 00 00 00 8E 80 40 84 00 00 00 00 00 00 00 8E 90 40 84 00 00 00 00 00 00 00 8E A0 40 84 00 00 00 00 00 00 00 8E B0 40 84 00 00 00 00 00 00 00 8E 80 50 84 00 00 00 00 00 00 00 01 FF 93 00 00 0C FF 92 00 00 00 00 01 07 FD 97 00 00 00 00 00 00 00 00 00 01 FF 98 00 00 1F 75 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_abb_f95()
        {
            var data = "68 5E 5E 68 08 00 72 90 85 71 26 24 23 28 04 73 50 00 00 0C 05 00 00 00 00 0C 12 42 07 00 00 3C 2A DD B4 EB DD 3B 3A DD B4 EB 0A 5A 04 02 0A 5E 04 02 0A 62 00 00 04 6D 22 10 8D 11 4C 05 00 00 00 00 44 6D 3B 17 7E 14 44 ED 7E 3B 17 9E 14 8C 01 05 00 00 00 00 84 01 6D 3B 17 7F 1C 0B 26 53 65 08 04 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ACW_Itron_BM_plus_m()
        {
            var data = "68 3C 3C 68 08 08 72 78 03 49 11 77 04 0E 16 0A 00 00 00 0C 78 78 03 49 11 04 13 31 D4 00 00 42 6C 00 00 44 13 00 00 00 00 04 6D 0B 0B CD 13 02 27 00 00 09 FD 0E 02 09 FD 0F 06 0F 00 01 75 13 D3 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ACW_Itron_CYBLE_M_Bus_14()
        {
            var data = "68 56 56 68 08 01 72 23 15 01 09 77 04 14 07 25 00 00 00 0C 78 23 15 01 09 0D 7C 08 44 49 20 2E 74 73 75 63 0A 35 35 37 36 37 30 41 4C 39 30 04 6D 1A 0E CD 13 02 7C 09 65 6D 69 74 20 2E 74 61 62 D4 09 04 13 1F 00 00 00 04 93 7F 00 00 00 00 44 13 1F 00 00 00 0F 00 01 1F A9 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_allmess_cf50()
        {
            var data = "68 3D 3D 68 08 01 72 00 51 20 02 82 4D 02 04 00 88 00 00 04 07 00 00 00 00 0C 15 03 00 00 00 0B 2E 00 00 00 0B 3B 00 00 00 0A 5A 88 12 0A 5E 16 05 0B 61 23 77 00 02 6C 8C 11 02 27 37 0D 0F 60 00 67 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_berg_dz_plus()
        {
            var data = "68 A3 A3 68 08 00 72 00 00 00 00 42 04 02 02 00 00 00 00 0E 04 00 00 00 00 00 00 8E 10 04 00 00 00 00 00 00 8E 20 04 00 00 00 00 00 00 8E B0 00 04 00 00 00 00 00 00 8E 80 10 04 00 00 00 00 00 00 8E 80 40 04 00 00 00 00 00 00 8E 90 40 04 00 00 00 00 00 00 8E A0 40 04 00 00 00 00 00 00 8E B0 40 04 00 00 00 00 00 00 8E 80 50 04 00 00 00 00 00 00 01 FF 13 00 0B FF 12 00 00 00 0A FF 68 00 00 0A FF 69 00 00 07 FD 17 00 00 00 00 00 00 00 00 01 FF 18 00 1F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FC 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_EDC()
        {
            var data = "68 AE AE 68 28 01 72 95 08 12 11 83 14 02 04 17 00 00 00 84 00 86 3B 23 00 00 00 84 00 86 3C D1 01 00 00 84 40 86 3B 00 00 00 00 84 40 86 3C 00 00 00 00 85 00 5B 2B 4B AC 41 85 00 5F 20 D7 AC 41 85 40 5B 00 00 B8 42 85 40 5F 00 00 B8 42 85 00 3B 84 00 35 3F 85 40 3B 00 00 00 00 95 00 3B 95 CF B2 43 95 40 3B 00 00 00 00 85 00 2B 00 00 00 00 85 40 2B 00 00 00 00 95 00 2B D3 9F 90 46 95 40 2B 00 00 00 00 04 6D 19 0F 8A 17 84 00 7C 01 43 F3 0D 00 00 84 40 7C 01 43 9D 01 00 00 84 00 7C 01 63 01 00 00 00 84 40 7C 01 63 01 00 00 00 0F 2F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_EFE_Engelmann_Elster_SensoStar_2()
        {
            var data = "68 A1 A1 68 08 00 72 45 33 08 24 C5 14 00 04 66 27 00 00 04 78 91 7B 6F 01 04 6D 17 2E CC 13 04 15 00 00 00 00 44 15 00 00 00 00 84 01 15 00 00 00 00 04 06 00 00 00 00 44 06 00 00 00 00 84 01 06 00 00 00 00 84 10 06 00 00 00 00 C4 10 06 00 00 00 00 84 11 06 00 00 00 00 42 6C BF 1C 02 6C DF 1C 84 20 06 00 00 00 00 84 30 06 00 00 00 00 04 3B 00 00 00 00 14 3B 19 00 00 00 04 2B 00 00 00 00 14 2B 0B 00 00 00 02 5B 16 00 02 5F 15 00 04 61 09 00 00 00 02 23 0C 02 01 FD 17 00 04 90 28 0B 00 00 00 EB 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_EFE_Engelmann_WaterStar()
        {
            var data = "68 51 51 68 08 0B 72 54 02 99 04 C5 14 00 06 0C 27 00 00 04 78 2E 25 4C 00 04 6D 0A 0C CD 13 04 13 4C 01 00 00 44 13 4B 01 00 00 84 01 13 4C 01 00 00 42 6C BF 1C 02 6C DF 1C 04 3B 00 00 00 00 14 3B 16 08 00 00 02 23 A7 04 01 FD 17 00 04 90 28 08 00 00 00 3F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_electricity_meter_1()
        {
            var data = "68 92 92 68 08 01 72 3E 02 00 05 43 4C 12 02 13 00 00 00 8C 10 04 52 12 00 00 8C 11 04 52 12 00 00 8C 20 04 33 44 77 01 8C 21 04 33 44 77 01 02 FD C9 FF 01 ED 00 02 FD DB FF 01 20 00 02 AC FF 01 4F 00 82 40 AC FF 01 EE FF 02 FD C9 FF 02 E7 00 02 FD DB FF 02 23 00 02 AC FF 02 51 00 82 40 AC FF 02 F1 FF 02 FD C9 FF 03 E4 00 02 FD DB FF 03 45 00 02 AC FF 03 A0 00 82 40 AC FF 03 E0 FF 02 FF 68 00 00 02 AC FF 00 40 01 82 40 AC FF 00 BF FF 01 FF 13 04 D9 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_electricity_meter_2()
        {
            var data = "68 92 92 68 08 02 72 E5 02 00 05 00 00 12 02 25 00 00 00 8C 10 04 54 02 00 00 8C 11 04 54 02 00 00 8C 20 04 28 41 44 00 8C 21 04 28 41 44 00 02 FD C9 FF 01 E9 00 02 FD DB FF 01 01 00 02 AC FF 01 00 00 82 40 AC FF 01 00 00 02 FD C9 FF 02 EA 00 02 FD DB FF 02 00 00 02 AC FF 02 00 00 82 40 AC FF 02 00 00 02 FD C9 FF 03 EB 00 02 FD DB FF 03 01 00 02 AC FF 03 00 00 82 40 AC FF 03 00 00 02 FF 68 00 00 02 AC FF 00 00 00 82 40 AC FF 00 00 00 01 FF 13 04 E9 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_Elster_F2()
        {
            var data = "68 90 90 68 08 01 72 57 26 80 00 CD 4E 08 04 46 00 00 00 04 06 98 14 00 00 04 14 6B D6 01 00 84 40 14 79 66 01 00 02 5B 1C 00 02 5F 22 00 02 62 00 00 04 22 B1 A1 00 00 04 26 B1 A1 00 00 04 3B 00 00 00 00 04 2C 00 00 00 00 04 6D 0C 0C BD 16 84 40 6E 00 00 00 00 84 80 40 6E 00 00 00 00 1F C4 09 01 01 12 00 01 01 01 07 57 26 80 00 CD 4E 08 04 07 A3 FF 03 57 26 80 00 04 04 0D 02 FF 0F 05 3C FF 62 E7 62 96 0A 89 0A 02 00 15 40 17 01 00 00 63 42 DE 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ELS_Elster_F96_Plus()
        {
            var data = "68 68 68 68 08 00 72 51 39 49 44 93 15 2F 04 A1 70 00 00 0C 06 00 00 00 00 8C 10 06 00 00 00 00 8C 20 13 00 00 00 00 0C 13 00 00 00 00 3C 2B BD EB DD DD 3B 3B BD EB DD 0A 5A 27 02 0A 5E 26 02 0A 62 01 00 0A 27 30 07 04 6D 09 0D CD 13 4C 06 00 00 00 00 4C 13 00 00 00 00 CC 10 06 00 00 00 00 CC 20 13 00 00 00 00 42 6C BF 15 40 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_els_falcon()
        {
            var data = "68 4A 4A 68 08 01 72 45 23 11 70 93 15 0A 07 02 00 00 00 0C 13 67 45 23 01 04 6D 3A 0D E6 02 42 6C E1 01 4C 13 51 69 45 00 42 EC 7E 01 11 12 3B 39 17 42 6C 01 11 02 3B F9 17 0F 0E 42 20 01 01 01 00 05 08 5E 01 20 3D 12 08 3D 12 08 00 C0 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_els_tmpa_telegramm1()
        {
            var data = "68 2C 2C 68 08 01 72 45 23 11 70 93 15 02 07 02 00 00 00 0C 13 67 45 23 01 04 6D 3A 0D E6 02 42 6C E1 01 4C 13 51 69 45 00 42 EC 7E 01 11 0F 00 61 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ELV_Elvaco_CMa10()
        {
            var data = "68 53 53 68 08 0B 72 61 15 01 24 96 15 16 00 3F 00 00 00 01 FD 1B 02 02 FC 03 48 52 25 74 22 15 22 FC 03 48 52 25 74 24 0D 12 FC 03 48 52 25 74 C3 1C 02 65 2E 08 22 65 5C 05 12 65 A2 0B 01 72 18 42 65 2C 08 82 01 65 1F 08 0C 78 61 15 01 24 03 FD 0F 00 00 04 1F BD 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_elv_temp_humid()
        {
            var data = "68 53 53 68 08 05 72 34 08 00 54 96 15 32 00 F2 00 00 00 01 FD 1B 00 02 FC 03 48 52 25 74 D4 11 22 FC 03 48 52 25 74 C8 11 12 FC 03 48 52 25 74 B4 16 02 65 D0 08 22 65 70 08 12 65 23 09 01 72 18 42 65 E4 08 82 01 65 DD 08 0C 78 34 08 00 54 03 FD 0F 00 00 04 1F 5D 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_emh_diz()
        {
            var data = "68 21 21 68 08 01 72 02 37 62 00 A8 15 00 02 07 00 00 00 8C 10 04 09 04 00 00 C4 00 2A 00 00 00 00 01 FD 17 00 8C 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_EMU_EMU_Professional_375_M_Bus()
        {
            var data = "68 F4 F4 68 08 00 72 29 26 03 00 B5 15 10 02 02 00 00 00 0C 78 29 26 03 00 84 10 03 54 05 00 00 84 20 03 00 00 00 00 84 90 40 03 AE 1E 00 00 84 A0 40 03 00 00 00 00 04 AB FF 01 FE FF FF FF 04 AB FF 02 00 00 00 00 04 AB FF 03 00 00 00 00 04 2B FE FF FF FF 84 80 40 AB FF 01 0E 00 00 00 84 80 40 AB FF 02 00 00 00 00 84 80 40 AB FF 03 00 00 00 00 84 80 40 2B 0E 00 00 00 02 FD C8 FF 01 D1 08 02 FD C8 FF 02 00 00 02 FD C8 FF 03 00 00 22 FD C8 FF 01 52 07 22 FD C8 FF 02 00 00 22 FD C8 FF 03 00 00 12 FD C8 FF 01 6A 09 12 FD C8 FF 02 00 00 12 FD C8 FF 03 00 00 03 FD D9 FF 01 BE FF FF 03 FD D9 FF 02 00 00 00 03 FD D9 FF 03 00 00 00 03 FD 59 BE FF FF 01 FF E1 FF 01 0D 01 FF E1 FF 02 00 01 FF E1 FF 03 00 02 FF 52 F4 01 02 FD 60 38 00 01 FD 17 00 74 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_engelmann_sensostar2c()
        {
            var data = "68 A6 A6 68 08 03 72 10 00 38 10 C5 14 01 04 1E 00 00 00 04 78 EA 62 9E 00 04 6D 32 14 86 16 04 15 81 00 00 00 04 FB 00 08 00 00 00 84 20 FB 00 00 00 00 00 84 30 FB 00 00 00 00 00 04 3D 00 00 00 00 04 2D 00 00 00 00 02 5B 5F 00 02 5F 2B 00 04 61 8A 14 00 00 02 27 FA 01 01 FD 17 00 04 90 28 A0 86 01 00 42 6C 7F 1C 44 15 81 00 00 00 44 FB 00 08 00 00 00 C4 20 FB 00 00 00 00 00 C4 30 FB 00 00 00 00 00 82 01 6C 5F 1C 84 01 15 54 00 00 00 84 01 FB 00 05 00 00 00 84 21 FB 00 00 00 00 00 84 31 FB 00 00 00 00 00 B7 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_example_data_01()
        {
            var data = "68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 9E 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 22 F3 26 42 05 5F C7 DA 0D 42 FA 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_example_data_02()
        {
            var data = "68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 A1 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 1E D8 24 42 05 5F D9 8A 0D 42 9E 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_filler()
        {
            var data = "68 1F 1F 68 08 00 72 31 77 67 17 2D 2C 01 02 00 00 00 00 2F 2F 04 83 3B 88 13 00 00 2F 2F 2F 2F 2F 2F 2F 00 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_FIN_Finder_7E_23_8_230_0020()
        {
            var data = "68 38 38 68 08 19 72 07 62 00 23 2E 19 23 02 92 00 00 00 8C 10 04 68 28 17 00 8C 11 04 68 28 17 00 02 FD C9 FF 01 E6 00 02 FD DB FF 01 06 00 02 AC FF 01 09 00 82 40 AC FF 01 FD FF 5B 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_frame1()
        {
            var data = "68 54 54 68 08 00 72 58 09 06 10 65 32 16 0E 7B 00 00 00 0F 5F 42 01 11 FF FF FF FF 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 E6 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_frame2()
        {
            var data = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_gmc_emmod206()
        {
            var data = "68 91 91 68 08 03 72 78 56 34 12 A3 1D E6 02 02 00 00 00 82 40 FD 48 60 03 82 80 40 FD 48 BF 03 82 C0 40 FD 48 20 04 82 40 FD 59 BD 03 82 80 40 FD 59 1F 04 82 C0 40 FD 59 7E 04 82 40 2B E0 00 82 40 2B 36 FF 84 10 04 94 28 00 00 84 20 04 98 3A 00 00 84 50 04 BF 4E 00 00 84 60 04 A8 61 00 00 84 90 40 04 8B 75 00 00 84 A0 40 04 B8 88 00 00 84 D0 40 04 2D 9D 00 00 84 E0 40 04 C8 AF 00 00 82 41 2B E0 00 82 42 2B 00 00 82 43 2B 00 00 82 44 2B CA 00 42 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_GWF_MTKcoder()
        {
            var data = "68 1B 1B 68 08 01 72 07 20 18 00 E6 1E 35 07 4C 00 00 00 0C 78 07 20 18 00 0C 16 69 02 00 00 96 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_invalid_length()
        {
            var data = "68 00 00 68 08 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNull(frame);
        }

        [TestMethod]
        public void Test_invalid_length2()
        {
            var data = "68 12 12 68 08 01 73 93 92 91 90 10 00 05 69 31 65 00 00 69 00 00 3F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<FixedDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as FixedDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_bm_m()
        {
            var data = "68 3C 3C 68 08 08 72 78 03 49 11 77 04 0E 16 29 00 00 00 0C 78 78 03 49 11 04 13 31 D4 00 00 42 6C 00 00 44 13 00 00 00 00 04 6D 1D 0D 98 11 02 27 00 00 09 FD 0E 02 09 FD 0F 06 0F 00 00 8F 13 E8 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cf_51()
        {
            var data = "68 64 64 68 08 06 72 85 51 15 11 77 04 0A 0D 1B 10 00 00 0C 78 85 51 15 11 04 06 00 00 00 00 0C 14 00 00 00 00 3B 2D 99 99 99 0B 3B 00 00 00 3A 5A 99 99 3A 5E 99 99 3B 61 99 99 99 04 6D 18 0D 98 11 02 27 68 00 09 FD 0E 11 09 FD 0F 26 8C C0 00 16 21 03 00 00 8C 80 40 14 23 01 00 00 04 86 3C 00 00 00 00 0F 03 20 85 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cf_55()
        {
            var data = "68 4D 4D 68 08 07 72 67 76 12 11 77 04 0B 0C 0B 10 00 00 0C 78 67 76 12 11 04 07 00 00 00 00 0C 16 00 00 00 00 3B 2D 99 99 99 0B 3B 00 00 00 3A 5A 99 99 3A 5E 99 99 3B 61 99 99 99 04 6D 2F 0B 98 11 02 27 FC 00 09 FD 0E 10 09 FD 0F 21 0F 03 20 54 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cf_echo_2()
        {
            var data = "68 4D 4D 68 08 09 72 91 00 10 11 77 04 09 04 51 10 00 00 0C 78 91 00 10 11 04 06 00 00 00 00 0C 14 00 00 00 00 3B 2D 99 99 99 3B 3B 99 99 99 0A 5A 05 02 0A 5E 06 02 0B 61 09 00 00 04 6D 1D 0D 98 11 02 27 81 01 09 FD 0E 19 09 FD 0F 45 0F 20 00 E7 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cyble_m_bus_v1_4_cold_water()
        {
            var data = "68 56 56 68 08 08 72 80 03 02 10 77 04 14 16 A1 00 00 00 0C 78 80 03 02 10 0D 7C 08 44 49 20 2E 74 73 75 63 0A 20 20 20 20 20 20 20 20 20 20 04 6D 27 0F 79 1A 02 7C 09 65 6D 69 74 20 2E 74 61 62 D2 0F 04 15 B7 11 00 00 04 95 7F 00 00 00 00 44 15 B7 11 00 00 0F 00 04 1F 0D 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cyble_m_bus_v1_4_gas()
        {
            var data = "68 56 56 68 08 04 72 87 03 02 10 77 04 14 03 9A 00 00 00 0C 78 87 03 02 10 0D 7C 08 44 49 20 2E 74 73 75 63 0A 20 20 20 20 20 20 20 20 20 20 04 6D 2B 0F 79 1A 02 7C 09 65 6D 69 74 20 2E 74 61 62 D2 0F 04 14 1A 00 00 00 04 94 7F 00 00 00 00 44 14 19 00 00 00 0F 00 02 1F 9F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_cyble_m_bus_v1_4_water()
        {
            var data = "68 56 56 68 08 01 72 71 00 00 12 77 04 14 07 0A 30 00 00 0C 78 71 00 00 12 0D 7C 08 44 49 20 2E 74 73 75 63 0A 45 4C 42 59 43 20 54 53 45 54 04 6D 2B 0D 98 11 02 7C 09 65 6D 69 74 20 2E 74 61 62 F2 10 04 14 3D 30 00 00 04 94 7F 14 00 00 00 44 14 00 00 00 00 0F 10 01 1F 2F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_itron_integral_mk_maxx()
        {
            var data = "68 5C 5C 68 08 04 72 14 73 81 11 82 4D 06 04 5D 00 00 00 0C 78 14 73 81 11 0C 06 00 00 00 00 0C 14 02 00 00 00 0A 3B 00 00 0B 5A 12 02 00 0B 5E 11 02 00 0B 61 07 00 00 32 26 00 00 02 27 8D 01 04 6D 11 0E 98 11 84 40 14 7B 00 00 00 84 80 40 14 41 01 00 00 09 FD 0E 03 09 FD 0F 18 0F 00 16 E9 16                          "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_kamstrup_382_005()
        {
            var data = "68 46 46 68 08 78 72 20 91 83 14 2D 2C 01 02 04 00 00 00 04 06 00 00 00 00 04 22 09 00 00 00 04 2B 00 00 00 00 14 2B 00 00 00 00 84 50 06 00 00 00 00 84 60 06 00 00 00 00 0F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 10 24 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_kamstrup_multical_601()
        {
            var data = "68 F7 F7 68 08 11 72 17 58 85 06 2D 2C 08 04 04 00 00 00 0C 78 17 58 85 06 04 06 E7 91 00 00 04 14 2C DB 00 00 04 22 D9 03 00 00 04 59 B9 27 00 00 04 5D 08 12 00 00 04 61 B1 15 00 00 04 2D 5B 01 00 00 14 2D C0 01 00 00 04 3B 1F 02 00 00 14 3B 74 02 00 00 84 10 06 00 00 00 00 84 20 06 00 00 00 00 84 40 14 00 00 00 00 84 80 40 14 00 00 00 00 84 C0 40 06 00 00 00 00 04 6D 1A 2F 65 11 44 06 51 82 00 00 44 14 B2 C3 00 00 54 2D 26 02 00 00 54 3B 03 04 00 00 C4 10 06 00 00 00 00 C4 20 06 00 00 00 00 C4 40 14 00 00 00 00 C4 80 40 14 00 00 00 00 C4 C0 40 06 00 00 00 00 42 6C 5F 1C 0F 00 00 00 00 E7 E4 00 00 63 66 00 00 00 00 00 00 00 00 00 00 00 00 00 00 5B C9 A5 02 34 53 00 00 E0 B2 03 00 89 9C 68 00 00 00 00 00 01 00 01 07 07 09 01 03 00 00 00 00 00 98 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_landis_gyr_ultraheat_t230()
        {
            var data = "68 E2 E2 68 08 00 72 05 02 66 66 A7 32 07 04 01 10 00 00 09 74 04 09 70 08 0C 06 00 00 00 00 0C 14 00 00 00 00 0B 2D 00 00 00 0B 3B 00 00 00 0B 5A 95 01 00 0B 5E 97 01 00 0B 62 02 00 F0 0C 78 05 02 66 66 89 10 71 07 3C 22 69 37 00 00 0C 22 69 37 00 00 0C 26 00 00 00 00 8C 90 10 06 00 00 00 00 9B 10 2D 00 00 00 9B 10 3B 00 00 00 9B 10 5A 07 03 00 9B 10 5E 07 05 00 94 10 AD 6F 00 00 00 00 94 10 BB 6F 00 00 00 00 94 10 DA 6F 32 14 7A 18 94 10 DE 6F 2B 0B 69 18 4C 06 00 00 00 00 4C 14 00 00 00 00 7C 22 69 34 00 00 4C 26 00 00 00 00 CC 90 10 06 00 00 00 00 DB 10 2D 00 00 00 DB 10 3B 00 00 00 DB 10 5A 07 03 00 DB 10 5E 07 05 00 84 8F 0F 6D 00 00 E1 F1 04 6D 04 0C 8D 11 0F 09 07 00 66 01 7D 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_LGB_G350()
        {
            var data = "68 40 40 68 08 01 72 58 20 08 12 E2 30 40 03 40 00 00 00 2F 2F 4C 13 92 40 83 10 46 6D 00 00 08 16 27 00 0D 78 11 34 31 38 35 30 32 38 30 32 31 39 35 37 31 30 30 47 89 40 FD 1A 01 01 FD 17 00 01 FD 67 0F 38 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_manual_frame2()
        {
            var data = "68 13 13 68 08 05 73 78 56 34 12 0A 00 E9 7E 01 00 00 00 35 01 00 00 3C 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<FixedDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as FixedDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_manual_frame3()
        {
            var data = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_manual_frame7()
        {
            var data = "68 15 15 68 08 02 72 78 56 34 12 24 40 01 07 13 00 00 00 0C 78 04 03 02 01 9D 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_metrona_pollutherm()
        {
            var data = "68 42 42 68 08 00 72 46 01 95 44 18 4E 34 04 54 10 00 00 0C 07 00 00 00 00 0C 14 00 00 00 00 0C 3C 00 00 00 00 0C 2C 00 00 00 00 0A 5A 00 00 0A 5E 00 00 0B 60 00 00 00 0C 78 46 01 95 44 0C FD 10 46 01 95 44 1F 82 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_metrona_ultraheat_xs()
        {
            var data = "68 F8 F8 68 08 64 72 54 00 81 01 A7 32 02 04 0F 10 00 00 09 74 04 09 70 04 0C 06 69 99 01 00 0C 14 18 92 64 02 0B 2E 00 00 00 0B 3B 00 00 00 0A 5B 00 00 0A 5F 00 00 0A 62 00 00 4C 14 18 92 64 02 4C 06 69 99 01 00 0C 78 54 00 11 65 89 10 71 60 9B 10 2D 16 03 00 DB 10 2D 16 03 00 9B 10 3B 20 88 00 9A 10 5B 44 00 9A 10 5F 40 00 0C 22 67 00 07 00 3C 22 09 16 05 00 7C 22 17 78 04 00 42 6C 01 01 8C 20 06 00 00 00 00 8C 30 06 00 00 00 00 8C 80 10 06 00 00 00 00 CC 20 06 00 00 00 00 CC 30 06 00 00 00 00 CC 80 10 06 00 00 00 00 9A 11 5B 36 00 9A 11 5F 40 00 9B 11 3B 00 00 00 9B 11 2D 00 00 00 BC 01 22 65 14 05 00 8C 01 06 69 99 01 00 8C 21 06 00 00 00 00 8C 31 06 00 00 00 00 8C 81 10 06 00 00 00 00 8C 01 14 18 92 64 02 04 6D 26 00 87 16 0F 03 02 00 00 23 46 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_minol_minocal_c2()
        {
            var data = "68 EF EF 68 08 02 72 84 50 42 31 4D 6A 81 04 24 27 00 00 04 06 03 00 00 00 02 FD 17 00 00 84 04 6D 00 00 A1 11 84 04 06 03 00 00 00 84 05 06 00 00 00 00 04 13 49 00 00 00 04 3B 00 00 00 00 54 3B 2B 00 00 00 54 6D 1E 08 61 19 04 2D 00 00 00 00 94 01 2D 14 00 00 00 94 01 6D 1E 08 61 19 02 59 D9 07 02 5D 87 07 04 6D 35 0B 8D 11 82 80 01 6C 81 11 84 80 01 06 03 00 00 00 C2 80 01 6C 61 1C C4 80 01 06 03 00 00 00 82 81 01 6C 61 1B 84 81 01 06 03 00 00 00 C2 81 01 6C 61 1A C4 81 01 06 03 00 00 00 82 82 01 6C 61 19 84 82 01 06 00 00 00 00 C2 82 01 6C 61 18 C4 82 01 06 00 00 00 00 82 83 01 6C 61 17 84 83 01 06 00 00 00 00 C2 83 01 6C 61 16 C4 83 01 06 00 00 00 00 92 80 01 6C 81 11 94 80 01 3B 01 00 00 00 94 80 01 2D 00 00 00 00 8E 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_minol_minocal_wr3()
        {
            var data = "68 CD CD 68 08 00 72 59 27 80 31 4D 6A 82 04 2B 00 00 00 04 06 00 00 00 00 04 14 01 00 00 00 04 2D 00 00 00 00 04 3C 00 00 00 00 02 59 00 00 02 5D 00 00 84 04 06 00 00 00 00 C4 04 6D 00 00 81 11 94 01 2D 00 00 00 00 94 01 6D 1E 0B 8D 11 54 3C 01 00 00 00 54 6D 1E 07 78 13 8C 40 79 00 00 00 00 81 40 FD 09 07 84 40 13 01 00 00 00 C4 04 13 01 00 00 00 8C 80 40 79 00 00 00 00 84 80 40 13 01 00 00 00 81 80 40 FD 09 07 84 05 13 01 00 00 00 02 FD 17 04 00 04 6D 01 0C 8D 11 82 80 01 6C 81 11 84 80 01 06 00 00 00 00 84 C0 01 13 01 00 00 00 84 80 41 13 01 00 00 00 92 80 01 6C 81 11 94 80 01 3C 00 00 00 00 94 80 01 2D 00 00 00 00 AA 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_nzr_dhz_5_63()
        {
            var data = "68 32 32 68 08 05 72 08 06 10 30 52 3B 01 02 01 00 00 00 04 03 FA 04 00 00 04 83 7F FA 04 00 00 02 FD 48 44 09 02 FD 5B 00 00 02 2B 00 00 0C 78 08 06 10 30 0F 0E 71 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_oms_frame1()
        {
            var data = "68 20 20 68 08 FD 72 78 56 34 12 93 15 33 03 2A 00 00 00 0C 14 27 04 85 02 04 6D 32 37 1F 15 02 FD 17 00 00 89 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_oms_frame2()
        {
            var data = "68 29 29 68 08 FD 72 44 22 75 92 24 23 29 07 1F 00 00 00 0C 13 27 04 85 02 0B 3B 27 01 00 4C 13 19 54 44 01 42 6C FF 0C 02 FD 17 00 00 99 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_oms_frame3()
        {
            var data = "68 3C 3C 68 08 FD 72 78 56 34 12 24 23 2A 04 26 00 00 00 0C 06 27 04 85 02 0C 13 76 34 70 00 4C 06 19 54 44 01 42 6C FF 0C 0B 3B 27 01 00 0B 2A 97 32 00 0A 5A 43 04 0A 5E 51 02 02 FD 17 00 00 C8 16 "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ram_modularis()
        {
            var data = "68 C4 C4 68 08 00 72 76 57 02 00 2D 48 03 07 8B 00 00 00 04 13 84 27 00 00 04 6D 28 15 B2 1A 42 6C BC 19 44 13 C9 20 00 00 42 EC 7E DC 19 0C 78 76 57 02 00 82 01 6C BE 19 84 01 13 4F 21 00 00 C2 01 6C 9F 1A C4 01 13 FB E0 F5 05 82 02 6C 9E 1B 84 02 13 F9 E0 F5 05 C2 02 6C 9F 1C C4 02 13 0E 03 00 00 82 03 6C BF 11 84 03 13 89 07 00 00 C2 03 6C BC 12 C4 03 13 14 0C 00 00 82 04 6C BF 13 84 04 13 35 12 00 00 C2 04 6C BE 14 C4 04 13 9F 12 00 00 82 05 6C BF 15 84 05 13 04 14 00 00 C2 05 6C BE 16 C4 05 13 38 14 00 00 82 06 6C BF 17 84 06 13 7E 14 00 00 C2 06 6C BF 18 C4 06 13 24 16 00 00 0F 01 00 00 82 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_REL_Relay_Padpuls2()
        {
            var data = "68 2F 2F 68 08 16 72 01 63 21 11 AC 48 41 03 B1 00 00 00 0C 14 81 60 87 02 04 6D A1 15 E9 17 42 6C DF 1C 4C 14 82 73 59 02 42 EC 7E FF 1C 0F C0 01 01 0C BD 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_rel_padpuls2()
        {
            var data = "68 2F 2F 68 08 04 72 04 00 00 00 AC 48 12 00 01 00 00 00 0C 00 00 00 00 00 04 6D 10 0D 34 09 42 6C 1F 0C 4C 00 00 00 00 00 42 EC 7E 3F 0C 0F 43 01 01 00 D0 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_rel_padpuls3()
        {
            var data = "68 2F 2F 68 08 01 72 01 01 03 01 AC 48 40 08 1E 00 00 00 0C 6E 87 19 00 00 04 6D 29 0A 1F 0C 42 6C 1F 0C 4C 6E 02 13 00 00 42 EC 7E 3F 0C 0F C0 01 01 0C 40 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_SBC_Saia_Burgess_ALE3()
        {
            var data = "68 92 92 68 08 28 72 55 00 00 19 43 4C 16 02 BF 00 00 00 8C 10 04 93 02 00 00 8C 11 04 93 02 00 00 8C 20 04 06 00 00 00 8C 21 04 06 00 00 00 02 FD C9 FF 01 DF 00 02 FD DB FF 01 00 00 02 AC FF 01 00 00 82 40 AC FF 01 00 00 02 FD C9 FF 02 00 00 02 FD DB FF 02 00 00 02 AC FF 02 00 00 82 40 AC FF 02 00 00 02 FD C9 FF 03 00 00 02 FD DB FF 03 00 00 02 AC FF 03 00 00 82 40 AC FF 03 00 00 02 FF 68 00 00 02 AC FF 00 00 00 82 40 AC FF 00 00 00 01 FF 14 00 0A 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_sen_pollusonic_2()
        {
            var data = "68 13 13 68 08 01 73 93 92 91 90 10 00 05 69 31 65 00 00 69 00 00 00 3F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<FixedDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as FixedDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_SEN_Pollustat()
        {
            var data = "68 71 71 68 08 04 72 88 17 01 00 AE 4C 06 0D 3E 00 00 00 04 6D 3B 0E E7 14 34 6D 00 00 01 01 34 FD 17 00 00 00 04 04 20 92 22 F1 00 04 24 A4 1A E7 00 04 86 3B 97 9B 00 00 04 13 BE 09 5E 00 05 2E B1 D1 2E BE 05 3E F5 B8 4E 40 05 5B B8 2D F9 41 05 5F 78 8B F9 41 05 63 00 80 3B BD 04 BE 50 71 BB B0 00 04 BE 58 F4 02 00 00 0C 78 88 17 01 00 02 7F 10 B5 5D 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_sen_pollutherm()
        {
            var data = "68 42 42 68 08 08 72 76 00 05 21 18 4E 31 04 51 00 00 00 0C 07 64 08 00 00 0C 14 92 98 79 00 0C 7B 02 03 00 00 0C 2C 58 54 00 00 0A 5A 55 07 0A 5E 94 05 0B 60 76 60 01 0C 78 76 00 05 21 0C FD 10 76 00 05 21 1F B3 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_SEN_Sensus_PolluStat_E()
        {
            var data = "68 42 42 68 08 00 72 95 50 26 21 AE 4C 0E 04 B5 10 00 00 0C 06 00 00 00 00 0C 13 00 00 00 00 0C 3B 00 00 00 00 0C 2B 00 00 00 00 02 5A C9 00 02 5E CA 00 03 60 00 00 00 0C 78 95 50 26 21 0C FD 10 95 50 26 21 1F EC 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_SEN_Sensus_PolluTherm()
        {
            var data = "68 41 41 68 08 00 72 89 16 35 24 AE 4C 0B 04 54 10 00 00 0C 07 00 00 00 00 0C 14 00 00 00 00 0C 3C 00 00 00 00 0C 2C 00 00 00 00 32 5A 00 00 32 5E 00 00 33 60 00 00 00 0C 78 89 16 35 24 0C FD 10 89 16 35 24 CE 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_siemens_water()
        {
            var data = "68 59 59 68 08 00 72 82 13 02 08 65 32 99 06 EB 00 00 00 0C 13 01 01 00 00 0B 22 52 09 02 04 6D 38 08 6E 19 32 6C 00 00 0C 78 82 13 02 08 06 FD 0C 0A 00 01 00 FA 01 0D FD 0B 05 31 32 48 46 57 01 FD 0E 00 0B 3B 00 00 00 0F 37 FD 17 00 00 00 00 00 00 00 00 02 7A 0D 00 02 78 0D 00 11 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_siemens_wfh21()
        {
            var data = "68 5E 5E 68 08 05 72 91 64 00 08 65 32 99 06 DA 00 00 00 0C 13 00 00 00 00 0B 22 86 40 04 04 6D 24 0A 61 1C 32 6C 00 00 0C 78 91 64 00 08 06 FD 0C 0A 00 01 00 FA 01 0D FD 0B 05 31 32 48 46 57 01 FD 0E 00 4C 13 00 00 00 00 42 6C 5F 1C 0F 37 FD 17 00 00 00 00 00 00 00 00 02 7A 25 00 02 78 25 00 82 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_SLB_CF_Compact_Integral_MK_MaXX()
        {
            var data = "68 5C 5C 68 08 04 72 14 73 81 11 82 4D 06 04 03 00 00 00 0C 78 14 73 81 11 0C 06 00 00 00 00 0C 14 02 00 00 00 0A 3B 00 00 0B 5A 18 02 00 0B 5E 20 02 00 0B 61 18 00 F0 32 26 00 00 02 27 98 04 04 6D 02 0E CD 13 84 40 14 7B 00 00 00 84 80 40 14 41 01 00 00 09 FD 0E 03 09 FD 0F 18 0F 00 16 DB 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_sontex_supercal_531_telegram1()
        {
            var data = "68 51 51 68 08 01 72 24 06 42 08 EE 4D 0D 04 2C 30 00 00 04 0E 00 00 00 00 04 14 00 00 00 00 05 5B 00 00 00 00 05 5F 00 00 00 00 05 3E 00 00 00 00 05 2B 00 00 00 00 C4 00 0E 00 00 00 00 C4 00 14 00 00 00 00 C4 40 14 00 00 00 00 C4 80 40 14 00 00 00 00 1F 71 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_svm_f22_telegram1()
        {
            var data = "68 5C 5C 68 08 01 72 89 60 00 01 CD 4E 09 0C 94 70 00 00 04 06 6E 6D 00 00 04 13 45 C6 09 00 84 40 13 45 C6 09 00 02 5B F3 00 02 5F F3 00 02 62 00 00 04 22 E2 18 00 00 04 26 DB 18 00 00 04 3B 00 00 00 00 04 2C 00 00 00 00 04 6D 0C 15 A8 22 84 40 6E 00 00 00 00 84 80 40 6E 00 00 00 00 1F A7 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_tch_telegramm1()
        {
            var data = "68 3F 3F 68 08 4E 72 82 99 51 21 68 50 26 04 85 00 00 00 0C 05 00 00 00 00 04 6D 32 0D 1D 09 4C 05 00 00 00 00 42 6C 1D 05 0B 3A 00 00 00 0A 5A 34 02 0A 5E 24 02 0C 2A 00 00 00 00 0C 13 64 00 00 00 1F 09 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_test()
        {
            var data = "68 35 35 68 08 0A 72 64 81 02 15 B4 09 05 07 1C 00 00 00 0C 78 05 49 78 16 03 74 03 00 00 01 FD 71 A7 06 6D 1A 2F 4A 51 27 1D 0C 13 42 01 00 00 0F 09 2E 37 24 12 17 07 18 3B 16    "
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_THI_cma10()
        {
            var data = "68 52 52 68 08 E6 72 02 00 00 00 96 15 15 00 0D 00 00 00 01 FD 1B 02 02 FC 03 48 52 25 74 34 12 22 FC 03 48 52 25 74 C6 0E 12 FC 03 48 52 25 74 02 14 02 65 D6 08 22 65 CA 08 12 65 16 09 01 72 00 72 65 00 00 B2 01 65 00 00 0C 78 02 00 00 00 02 FD 0F 04 03 1F 96 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_wmbus_converted()
        {
            var data = "68 1f 1f 68 08 00 72 31 77 67 17 2d 2c 01 02 00 00 00 00 2f 2f 04 83 3b 88 13 00 00 2f 2f 2f 2f 2f 2f 2f 00 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }

        [TestMethod]
        public void Test_ZRM_Minol_Minocal_C2()
        {
            var data = "68 EF EF 68 08 02 72 84 50 42 31 4D 6A 81 04 73 27 00 00 04 06 03 00 00 00 02 FD 17 00 00 84 04 6D 00 00 E1 11 84 04 06 03 00 00 00 84 05 06 03 00 00 00 04 13 4A 00 00 00 04 3B 00 00 00 00 54 3B 2B 00 00 00 54 6D 1E 08 61 19 04 2D 00 00 00 00 94 01 2D 14 00 00 00 94 01 6D 1E 08 61 19 02 59 17 08 02 5D F6 07 04 6D 2D 0C CD 13 82 80 01 6C C1 13 84 80 01 06 03 00 00 00 C2 80 01 6C C1 12 C4 80 01 06 03 00 00 00 82 81 01 6C C1 11 84 81 01 06 03 00 00 00 C2 81 01 6C A1 1C C4 81 01 06 03 00 00 00 82 82 01 6C A1 1B 84 82 01 06 03 00 00 00 C2 82 01 6C A1 1A C4 82 01 06 03 00 00 00 82 83 01 6C A1 19 84 83 01 06 03 00 00 00 C2 83 01 6C A1 18 C4 83 01 06 03 00 00 00 92 80 01 6C C1 13 94 80 01 3B 00 00 00 00 94 80 01 2D 00 00 00 00 8F 16"
                .HexToBytes();

            var frame = new MeterbusFrameSerializer()
                .Deserialize<VariableDataLongFrame>(data, 0, data.Length);

            Assert.IsNotNull(frame);

            var packet = frame.AsPacket() as VariableDataPacket;

            Assert.IsNotNull(packet);
        }
    }
}
