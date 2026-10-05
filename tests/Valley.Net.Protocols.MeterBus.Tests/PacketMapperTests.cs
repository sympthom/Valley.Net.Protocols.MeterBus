namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class PacketMapperTests
{
    // Fixed data header of a variable data response: id 12345678, manufacturer, version, device type, access no, status, signature
    private const string VariableHeader = "78 56 34 12 24 40 01 07 55 00 00 00";

    private readonly FrameParser _parser = new();
    private readonly PacketMapper _mapper = new(new VifLookupService());

    [TestMethod]
    [DataRow("68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 9E 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 22 F3 26 42 05 5F C7 DA 0D 42 FA 16", DisplayName = "example_data_01")]
    public void MapToPacket_VariableDataFrame_Success(string hex)
    {
        var bytes = hex.HexToBytes();
        var frameResult = _parser.Parse(bytes);
        Assert.IsTrue(frameResult.IsSuccess, $"Frame parse failed: {frameResult.Error?.Message}");

        var packetResult = _mapper.MapToPacket(frameResult.Value!);
        Assert.IsTrue(packetResult.IsSuccess, $"Packet map failed: {packetResult.Error?.Message}");

        var packet = packetResult.Value as VariableDataPacket;
        Assert.IsNotNull(packet);
        Assert.IsNotEmpty(packet.Records);
    }

    // Expected counts are the libmbus record counts minus its "More records follow"/"Manufacturer specific" entry
    [TestMethod]
    [DataRow("68 A3 A3 68 08 00 72 00 00 00 00 42 04 02 02 00 00 00 00 0E 04 00 00 00 00 00 00 8E 10 04 00 00 00 00 00 00 8E 20 04 00 00 00 00 00 00 8E B0 00 04 00 00 00 00 00 00 8E 80 10 04 00 00 00 00 00 00 8E 80 40 04 00 00 00 00 00 00 8E 90 40 04 00 00 00 00 00 00 8E A0 40 04 00 00 00 00 00 00 8E B0 40 04 00 00 00 00 00 00 8E 80 50 04 00 00 00 00 00 00 01 FF 13 00 0B FF 12 00 00 00 0A FF 68 00 00 0A FF 69 00 00 07 FD 17 00 00 00 00 00 00 00 00 01 FF 18 00 1F 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FC 16", 16, true, 16, DisplayName = "berg_dz_plus")]
    [DataRow("68 90 90 68 08 01 72 57 26 80 00 CD 4E 08 04 46 00 00 00 04 06 98 14 00 00 04 14 6B D6 01 00 84 40 14 79 66 01 00 02 5B 1C 00 02 5F 22 00 02 62 00 00 04 22 B1 A1 00 00 04 26 B1 A1 00 00 04 3B 00 00 00 00 04 2C 00 00 00 00 04 6D 0C 0C BD 16 84 40 6E 00 00 00 00 84 80 40 6E 00 00 00 00 1F C4 09 01 01 12 00 01 01 01 07 57 26 80 00 CD 4E 08 04 07 A3 FF 03 57 26 80 00 04 04 0D 02 FF 0F 05 3C FF 62 E7 62 96 0A 89 0A 02 00 15 40 17 01 00 00 63 42 DE 16", 13, true, 52, DisplayName = "Elster-F2")]
    [DataRow("68 56 56 68 08 01 72 71 00 00 12 77 04 14 07 0A 30 00 00 0C 78 71 00 00 12 0D 7C 08 44 49 20 2E 74 73 75 63 0A 45 4C 42 59 43 20 54 53 45 54 04 6D 2B 0D 98 11 02 7C 09 65 6D 69 74 20 2E 74 61 62 F2 10 04 14 3D 30 00 00 04 94 7F 14 00 00 00 44 14 00 00 00 00 0F 10 01 1F 2F 16", 7, false, 3, DisplayName = "itron_cyble_m-bus_v1.4_water")]
    [DataRow("68 53 53 68 08 05 72 34 08 00 54 96 15 32 00 F2 00 00 00 01 FD 1B 00 02 FC 03 48 52 25 74 D4 11 22 FC 03 48 52 25 74 C8 11 12 FC 03 48 52 25 74 B4 16 02 65 D0 08 22 65 70 08 12 65 23 09 01 72 18 42 65 E4 08 82 01 65 DD 08 0C 78 34 08 00 54 03 FD 0F 00 00 04 1F 5D 16", 12, true, 0, DisplayName = "elv_temp_humid")]
    public void MapToPacket_ReferenceFrame_MatchesLibmbusRecordCount(string hex, int expectedRecords, bool expectedMoreRecordsFollow, int expectedManufacturerBytes)
    {
        var frameResult = _parser.Parse(hex.HexToBytes());
        Assert.IsTrue(frameResult.IsSuccess, $"Frame parse failed: {frameResult.Error?.Message}");

        var packet = MapVariable(frameResult.Value!);

        Assert.HasCount(expectedRecords, packet.Records);
        Assert.AreEqual(expectedMoreRecordsFollow, packet.MoreRecordsFollow);
        Assert.HasCount(expectedManufacturerBytes, packet.ManufacturerData);
    }

    [TestMethod]
    public void MapToPacket_MoreRecordsFollowDif_StopsRecordsAndKeepsManufacturerData()
    {
        // The bytes after 0x1F look like a valid record but are manufacturer specific
        var packet = MapVariable("04 13 01 00 00 00 1F 04 13 AA BB CC DD");

        Assert.HasCount(1, packet.Records);
        Assert.IsTrue(packet.MoreRecordsFollow);
        CollectionAssert.AreEqual(new byte[] { 0x04, 0x13, 0xAA, 0xBB, 0xCC, 0xDD }, packet.ManufacturerData.ToArray());
    }

    [TestMethod]
    public void MapToPacket_ManufacturerSpecificDif_KeepsManufacturerData()
    {
        var packet = MapVariable("04 13 01 00 00 00 0F 01 02");

        Assert.HasCount(1, packet.Records);
        Assert.IsFalse(packet.MoreRecordsFollow);
        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02 }, packet.ManufacturerData.ToArray());
    }

    [TestMethod]
    public void MapToPacket_IdleFiller_IsSkipped()
    {
        var packet = MapVariable("2F 04 13 01 00 00 00 2F 2F");

        Assert.HasCount(1, packet.Records);
        Assert.IsFalse(packet.MoreRecordsFollow);
        Assert.IsEmpty(packet.ManufacturerData);
    }

    [TestMethod]
    public void MapToPacket_PlainTextVifWithVife_ConsumesUnitBeforeVifes()
    {
        // FC: unit "%RH" (sent reversed), then VIFE 74 (x10^-2), then the 16-bit value 0x11D4
        var packet = MapVariable("02 FC 03 48 52 25 74 D4 11 01 13 07");

        Assert.HasCount(2, packet.Records);
        var humidity = packet.Records[0];
        Assert.AreEqual("%RH", humidity.Units[0].Unit);
        Assert.AreEqual(VariableDataQuantityUnit.MultiplicativeCorrectionFactor, humidity.Units[1].Units);
        Assert.AreEqual(-2, humidity.Magnitude);
        Assert.AreEqual(4564L, Convert.ToInt64(humidity.Value));
        Assert.AreEqual(7L, Convert.ToInt64(packet.Records[1].Value));
    }

    [TestMethod]
    public void MapToPacket_PlainTextVifWithVariableLengthValue_ReadsValueAfterUnit()
    {
        // 7C: unit "cust. ID", then an LVAR value of 10 bytes, then a volume record
        var packet = MapVariable("0D 7C 08 44 49 20 2E 74 73 75 63 0A 35 35 37 36 37 30 41 4C 39 30 04 13 01 00 00 00");

        Assert.HasCount(2, packet.Records);
        Assert.AreEqual("cust. ID", packet.Records[0].Units[0].Unit);
        Assert.AreEqual(VariableDataQuantityUnit.Volume_m3, packet.Records[1].Units[0].Units);
        Assert.AreEqual(1L, Convert.ToInt64(packet.Records[1].Value));
    }

    [TestMethod]
    [DataRow("02 7C", DisplayName = "Missing length")]
    [DataRow("02 7C 03 41 42", DisplayName = "Text one byte past end")]
    public void MapToPacket_PlainTextVifTruncated_Fails(string records)
    {
        var result = Map(records);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PLAIN_TEXT_VIF_TRUNCATED", result.Error!.Code);
    }

    [TestMethod]
    public void MapToPacket_PlainTextVifEndingAtEndOfData_Succeeds()
    {
        // DIF 00 has no value, so the unit text is the last thing in the frame
        var record = MapVariable("00 7C 02 42 41").Records.Single();

        Assert.AreEqual("AB", record.Units[0].Unit);
    }

    [TestMethod]
    public void MapToPacket_FdVifWithCombinableVife_ResolvesSecondVifeFromPrimaryTable()
    {
        // FD C8 = 10^-1 V, then combinable 74 = x10^-2
        var record = MapVariable("02 FD C8 74 10 00").Records.Single();

        Assert.AreEqual(VariableDataQuantityUnit.Volts, record.Units[1].Units);
        Assert.AreEqual(VariableDataQuantityUnit.MultiplicativeCorrectionFactor, record.Units[2].Units);
        Assert.AreEqual(-3, record.Magnitude);
    }

    [TestMethod]
    public void MapToPacket_FbVifWithCombinableVife_ResolvesSecondVifeFromPrimaryTable()
    {
        // FB 90 = volume, then combinable 75 = x10^-1
        var record = MapVariable("02 FB 90 75 01 00").Records.Single();

        Assert.AreEqual(VariableDataQuantityUnit.Volume_m3, record.Units[1].Units);
        Assert.AreEqual(VariableDataQuantityUnit.MultiplicativeCorrectionFactor, record.Units[2].Units);
        Assert.AreEqual(record.Units[1].Magnitude - 1, record.Magnitude);
    }

    [TestMethod]
    public void MapToPacket_ManufacturerSpecificVife_FollowingVifesAreOpaque()
    {
        // Power 10^1 W, VIFE FF, then a two-byte manufacturer chain (81 13) before the 16-bit value
        var packet = MapVariable("02 AC FF 81 13 4F 00 01 13 07");

        Assert.HasCount(2, packet.Records);
        var power = packet.Records[0];
        Assert.AreEqual(1, power.Magnitude);
        Assert.HasCount(4, power.Units);
        Assert.IsTrue(power.Units.Skip(1).All(u => u.Units == VariableDataQuantityUnit.ManufacturerSpecific && u.Magnitude == 0));
        Assert.AreEqual(79L, Convert.ToInt64(power.Value));
        Assert.AreEqual(7L, Convert.ToInt64(packet.Records[1].Value));
    }

    [TestMethod]
    public void MapToPacket_FdVifWithManufacturerSpecificVife_PhaseByteDoesNotScale()
    {
        // electricity-meter-1 record 4: FD C9 = V, FF = manufacturer specific, 01 = phase (libmbus: 237 V)
        var record = MapVariable("02 FD C9 FF 01 ED 00").Records.Single();

        Assert.AreEqual(0, record.Magnitude);
        Assert.AreEqual(VariableDataQuantityUnit.ManufacturerSpecific, record.Units[2].Units);
        Assert.AreEqual(VariableDataQuantityUnit.ManufacturerSpecific, record.Units[3].Units);
    }

    [TestMethod]
    public void MapToPacket_ManufacturerSpecificVif_VifesAreOpaque()
    {
        var record = MapVariable("02 FF 75 01 00").Records.Single();

        Assert.AreEqual(0, record.Magnitude);
        Assert.AreEqual(VariableDataQuantityUnit.ManufacturerSpecific, record.Units[1].Units);
    }

    [TestMethod]
    public void MapToPacket_FixedDataFrame_ReadsCounters()
    {
        var result = MapFixed("78 56 34 12 0A 00 E9 7E 01 00 00 00 35 01 00 00");

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        var packet = (FixedDataPacket)result.Value!;
        Assert.AreEqual(1u, packet.Counter1);
        Assert.AreEqual(135u, packet.Counter2);
    }

    [TestMethod]
    [DataRow("93 92 91 90 10 00 05 69", DisplayName = "8 bytes")]
    [DataRow("93 92 91 90 10 00 05 69 31 65 00 00 69 00 00", DisplayName = "15 bytes (invalid_length2)")]
    [DataRow("78 56 34 12 0A 00 E9 7E 01 00 00 00 35 01 00 00 00", DisplayName = "17 bytes")]
    public void MapToPacket_FixedDataFrameWrongLength_Fails(string data)
    {
        var result = MapFixed(data);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("FIXED_FRAME_INVALID_LENGTH", result.Error!.Code);
    }

    private MBusParseResult<MBusPacket> Map(string records) =>
        _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_VARIABLE, 0x01, $"{VariableHeader} {records}".HexToBytes(), 0));

    private MBusParseResult<MBusPacket> MapFixed(string data) =>
        _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_FIXED, 0x01, data.HexToBytes(), 0));

    private VariableDataPacket MapVariable(string records) => AsVariablePacket(Map(records));

    private VariableDataPacket MapVariable(MBusFrame frame) => AsVariablePacket(_mapper.MapToPacket(frame));

    private static VariableDataPacket AsVariablePacket(MBusParseResult<MBusPacket> result)
    {
        Assert.IsTrue(result.IsSuccess, $"Packet map failed: {result.Error?.Message}");
        return (VariableDataPacket)result.Value!;
    }
}
