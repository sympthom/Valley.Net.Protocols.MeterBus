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
        Assert.AreEqual("PREMATURE_END", result.Error!.Code);
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
    public void MapToPacket_CombinableExtensionVife_ResolvesNextVifeFromFcTable()
    {
        // Energy Wh, VIFE FC (combinable extension), then FC-table 01 = at phase L1
        var record = MapVariable("02 83 FC 01 10 00").Records.Single();

        Assert.AreEqual(VariableDataQuantityUnit.CombinableExtension, record.Units[1].Units);
        Assert.AreEqual(VariableDataQuantityUnit.AtPhaseL1, record.Units[2].Units);
        Assert.AreEqual(record.Units[0].Magnitude, record.Magnitude);
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
        Assert.AreEqual(1L, packet.Counter1);
        Assert.AreEqual(135L, packet.Counter2);
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

    [TestMethod]
    public void MapToPacket_Records_HaveNoValueError()
    {
        var packet = MapVariable("04 13 01 00 00 00 0C 13 78 56 34 12 02 6C DF 1C");

        Assert.IsTrue(packet.Records.All(r => r.ValueError is null));
    }

    [TestMethod]
    public void MapToPacket_InvalidBcd_SetsValueError()
    {
        // ELS_Elster-F96-Plus record 4: value during error state filled with non-decimal digits
        var record = MapVariable("3C 2A BD EB DD DD").Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_BCD", record.ValueError);
    }

    [TestMethod]
    public void MapToPacket_NegativeBcd_IsNegativeLong()
    {
        // SLB_CF-Compact-Integral-MK-MaXX record 6 style temperature difference, sign nibble F
        var record = MapVariable("0A 62 18 F0").Records.Single();

        Assert.AreEqual(-18L, record.Value);
        Assert.IsNull(record.ValueError);
    }

    // ---- Date/time (EN 13757-3 Annex A types G, J, F, I) ----

    [TestMethod]
    [DataRow("02 6C DF 1C", 2014, 12, 31, DisplayName = "VIF 6C 2014-12-31")]
    [DataRow("02 EC 00 9D 12", 2012, 2, 29, DisplayName = "VIF EC with VIFE, leap day")]
    [DataRow("02 6C 6F C6", 1999, 6, 15, DisplayName = "Year 99 is 1999")]
    [DataRow("02 6C 21 01", 2001, 1, 1, DisplayName = "Year 01 is 2001")]
    [DataRow("02 6C 01 A1", 2080, 1, 1, DisplayName = "Year 80 is 2080")]
    [DataRow("02 6C 21 A1", 1981, 1, 1, DisplayName = "Year 81 is 1981")]
    public void MapToPacket_TypeG_DecodesToDateOnly(string records, int year, int month, int day)
    {
        var record = MapVariable(records).Records.Single();

        Assert.AreEqual(new DateOnly(year, month, day), record.Value);
        Assert.IsNull(record.ValueError);
    }

    [TestMethod]
    [DataRow("02 6C 00 00", DisplayName = "All zero")]
    [DataRow("02 6C BE 12", DisplayName = "30 February")]
    [DataRow("02 6C 01 0D", DisplayName = "Month 13")]
    [DataRow("02 6C FF FF", DisplayName = "Year 127 wildcard")]
    public void MapToPacket_TypeGOutOfRange_IsInvalidDate(string records)
    {
        var record = MapVariable(records).Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_DATE", record.ValueError);
    }

    [TestMethod]
    [DataRow("04 6D 22 10 8D 11", "2012-01-13T16:34:00", DisplayName = "abb_f95 record 7")]
    [DataRow("04 6D 34 37 21 01", "2001-01-01T23:52:00", DisplayName = "Mbus_DEM example, hundred-year 1")]
    [DataRow("04 6D 1E 0C 6F C6", "1999-06-15T12:30:00", DisplayName = "Hundred-year 0, year 99")]
    [DataRow("04 6D 1E 4C AF 06", "2105-06-15T12:30:00", DisplayName = "Hundred-year 2, year 05")]
    [DataRow("04 6D 00 80 8D 11", "2012-01-13T00:00:00", DisplayName = "Summer time bit ignored")]
    public void MapToPacket_TypeF_DecodesToDateTime(string records, string expected)
    {
        var record = MapVariable(records).Records.Single();

        Assert.AreEqual(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), record.Value);
        Assert.IsNull(record.ValueError);
    }

    [TestMethod]
    [DataRow("04 6D A2 10 8D 11", DisplayName = "IV bit set")]
    [DataRow("04 6D A1 15 E9 17", DisplayName = "REL-Relay-Padpuls2 record 1, IV bit set")]
    [DataRow("04 6D 00 18 8D 11", DisplayName = "Hour 24")]
    [DataRow("04 6D 3C 10 8D 11", DisplayName = "Minute 60")]
    [DataRow("04 6D 00 00 E1 F1", DisplayName = "landis+gyr_ultraheat_t230 record 32, year 127")]
    [DataRow("04 6D 00 00 00 00", DisplayName = "All zero")]
    public void MapToPacket_TypeFInvalid_IsInvalidDate(string records)
    {
        var record = MapVariable(records).Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_DATE", record.ValueError);
    }

    [TestMethod]
    public void MapToPacket_TypeI_DecodesToDateTimeWithSeconds()
    {
        // LGB_G350 record 1 (2016-07-22T08:00:00) with 30 seconds added
        var record = MapVariable("06 6D 1E 00 08 16 27 00").Records.Single();

        Assert.AreEqual(new DateTime(2016, 7, 22, 8, 0, 30), record.Value);
        Assert.IsNull(record.ValueError);
    }

    [TestMethod]
    [DataRow("06 6D 00 80 08 16 27 00", DisplayName = "IV bit set")]
    [DataRow("06 6D 3C 00 08 16 27 00", DisplayName = "Second 60")]
    public void MapToPacket_TypeIInvalid_IsInvalidDate(string records)
    {
        var record = MapVariable(records).Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_DATE", record.ValueError);
    }

    [TestMethod]
    public void MapToPacket_TypeJ_DecodesToTimeOnly()
    {
        var record = MapVariable("03 6D 1E 0C 0D").Records.Single();

        Assert.AreEqual(new TimeOnly(13, 12, 30), record.Value);
        Assert.IsNull(record.ValueError);
    }

    [TestMethod]
    public void MapToPacket_TypeJOutOfRange_IsInvalidDate()
    {
        var record = MapVariable("03 6D 3F 3F 1F").Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_DATE", record.ValueError);
    }

    [TestMethod]
    [DataRow("94 10 DA 6F 32 14 7A 18", "2011-08-26T20:50:00", DisplayName = "VIFE 6F date/time of maximum flow temperature (landis+gyr_ultraheat_t230 record 21)")]
    [DataRow("04 93 42 22 10 8D 11", "2012-01-13T16:34:00", DisplayName = "VIFE 42 date/time of limit exceed")]
    [DataRow("04 FD 30 22 10 8D 11", "2012-01-13T16:34:00", DisplayName = "FD 30 start of tariff")]
    [DataRow("04 FD 70 22 10 8D 11", "2012-01-13T16:34:00", DisplayName = "FD 70 battery change")]
    public void MapToPacket_DateTimeVife_DecodesToDateTime(string records, string expected)
    {
        var record = MapVariable(records).Records.Single();

        Assert.AreEqual(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), record.Value);
    }

    [TestMethod]
    public void MapToPacket_StartDateOfVife_DecodesToDateOnly()
    {
        var record = MapVariable("02 93 39 DF 1C").Records.Single();

        Assert.AreEqual(new DateOnly(2014, 12, 31), record.Value);
    }

    [TestMethod]
    public void MapToPacket_TimePointWithBcdData_StaysNumeric()
    {
        var record = MapVariable("0C 6D 78 56 34 12").Records.Single();

        Assert.AreEqual(12345678L, record.Value);
    }

    [TestMethod]
    public void MapToPacket_IntegerWithoutDateVif_StaysInteger()
    {
        var record = MapVariable("04 13 22 10 8D 11").Records.Single();

        Assert.AreEqual(0x118D1022, record.Value);
    }

    // libmbus reference decodes in DataExamples/test-frames/*.xml
    [TestMethod]
    [DataRow("abb_f95", 7, "2012-01-13T16:34:00", DisplayName = "abb_f95 type F")]
    [DataRow("abb_f95", 12, "2011-12-31T23:59:00", DisplayName = "abb_f95 type F storage 2")]
    [DataRow("LGB_G350", 1, "2016-07-22T08:00:00", DisplayName = "LGB_G350 type I")]
    [DataRow("els_falcon", 1, "2007-02-06T13:58:00", DisplayName = "els_falcon type F")]
    public void MapToPacket_ReferenceFrame_DecodesDateTime(string name, int index, string expected)
    {
        var record = MapReferenceFrame(name).Records[index];

        Assert.AreEqual(DateTime.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), record.Value);
    }

    [TestMethod]
    [DataRow("EFE_Engelmann-WaterStar", 5, "2013-12-31", DisplayName = "EFE_Engelmann-WaterStar")]
    [DataRow("els_falcon", 2, "2007-01-01", DisplayName = "els_falcon")]
    public void MapToPacket_ReferenceFrame_DecodesDate(string name, int index, string expected)
    {
        var record = MapReferenceFrame(name).Records[index];

        Assert.AreEqual(DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), record.Value);
    }

    // ---- Variable-length data (DIF 0x0D, LVAR per EN 13757-3) ----

    [TestMethod]
    [DataRow("siemens_wfh21", 6, "WFH21", DisplayName = "siemens_wfh21 parameter set")]
    [DataRow("LGB_G350", 2, "G0017591208205814", DisplayName = "LGB_G350 fabrication number")]
    [DataRow("itron_cyble_m-bus_v1.4_water", 1, "TEST CYBLE", DisplayName = "Cyble customer ID")]
    [DataRow("itron_cyble_m-bus_v1.4_cold_water", 1, "", DisplayName = "Cyble all-space customer ID")]
    public void MapToPacket_ReferenceFrame_DecodesText(string name, int index, string expected)
    {
        var record = MapReferenceFrame(name).Records[index];

        Assert.AreEqual(expected, record.Value);
    }

    [TestMethod]
    public void MapToPacket_LvarText_IsReversedLatin1()
    {
        // "Aé" sent last character first; EN 13757-3 text is ISO 8859-1
        var record = MapVariable("0D 78 02 E9 41").Records.Single();

        Assert.AreEqual("A\u00E9", record.Value);
    }

    [TestMethod]
    [DataRow("0D 13 C2 34 12", 1234L, DisplayName = "C2 positive BCD")]
    [DataRow("0D 13 D2 34 12", -1234L, DisplayName = "D2 negative BCD")]
    [DataRow("0D 13 C9 01 00 00 00 00 00 00 00 00", 1L, DisplayName = "C9 positive BCD, 9 bytes")]
    public void MapToPacket_LvarBcd_DecodesToLongAndKeepsNextRecord(string lvarRecord, long expected)
    {
        var packet = MapVariable($"{lvarRecord} 04 13 01 00 00 00");

        Assert.HasCount(2, packet.Records);
        Assert.AreEqual(expected, packet.Records[0].Value);
        Assert.AreEqual(1, packet.Records[1].Value);
    }

    [TestMethod]
    [DataRow("0D 13 C2 3A 12", DisplayName = "Non-decimal digit")]
    [DataRow("0D 13 C2 34 F2", DisplayName = "Sign nibble in a positive BCD")]
    public void MapToPacket_LvarBcdInvalid_IsInvalidBcd(string records)
    {
        var record = MapVariable(records).Records.Single();

        Assert.IsNull(record.Value);
        Assert.AreEqual("INVALID_BCD", record.ValueError);
    }

    [TestMethod]
    [DataRow(0xE4, 4, DisplayName = "E4 binary, 4 bytes")]
    [DataRow(0xEF, 15, DisplayName = "EF binary, 15 bytes")]
    [DataRow(0xF0, 16, DisplayName = "F0 binary, 16 bytes")]
    [DataRow(0xF4, 32, DisplayName = "F4 binary, 32 bytes")]
    [DataRow(0xF5, 48, DisplayName = "F5 binary, 48 bytes")]
    [DataRow(0xF6, 64, DisplayName = "F6 binary, 64 bytes")]
    public void MapToPacket_LvarBinary_ReturnsRawBytesAndKeepsNextRecord(int lvar, int length)
    {
        var value = Enumerable.Range(1, length).Select(i => (byte)i).ToArray();
        var packet = MapVariable($"0D 13 {lvar:X2} {Convert.ToHexString(value)} 04 13 01 00 00 00");

        Assert.HasCount(2, packet.Records);
        CollectionAssert.AreEqual(value, (byte[])packet.Records[0].Value!);
        Assert.AreEqual(1, packet.Records[1].Value);
    }

    [TestMethod]
    [DataRow("CA", DisplayName = "CA")]
    [DataRow("DF", DisplayName = "DF")]
    [DataRow("F7", DisplayName = "F7")]
    [DataRow("FF", DisplayName = "FF")]
    public void MapToPacket_ReservedLvar_Fails(string lvar)
    {
        var result = Map($"04 13 01 00 00 00 0D 13 {lvar} 41 42 43 44");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("RESERVED_LVAR", result.Error!.Code);
    }

    // ---- Malformed records (EN 13757-3 limits, truncation, reserved DIFs) ----

    // DataExamples/error-frames: libmbus rejects these with "Premature end of record", "Too many DIFE/VIFE"
    [TestMethod]
    [DataRow("premature_end_of_data1", "PREMATURE_END", DisplayName = "premature_end_of_data1: value missing")]
    [DataRow("premature_end_of_data2", "PREMATURE_END", DisplayName = "premature_end_of_data2: value cut short")]
    [DataRow("premature_end_of_dif1", "PREMATURE_END", DisplayName = "premature_end_of_dif1: DIFE missing")]
    [DataRow("premature_end_of_dif2", "PREMATURE_END", DisplayName = "premature_end_of_dif2: DIFE chain cut")]
    [DataRow("premature_end_of_vif1", "PREMATURE_END", DisplayName = "premature_end_of_vif1: VIF missing")]
    [DataRow("premature_end_of_var_vif1", "PREMATURE_END", DisplayName = "premature_end_of_var_vif1: plain-text unit cut")]
    [DataRow("too_long_var_vif", "PREMATURE_END", DisplayName = "too_long_var_vif: plain-text length F3h")]
    [DataRow("too_many_dife", "TOO_MANY_DIFE", DisplayName = "too_many_dife: 12 DIFEs")]
    [DataRow("too_many_vife", "TOO_MANY_VIFE", DisplayName = "too_many_vife: 11 VIFEs")]
    [DataRow("too_short_header", "VAR_FRAME_TOO_SHORT", DisplayName = "too_short_header")]
    public void MapToPacket_ErrorFrame_FailsWithCode(string name, string expectedCode)
    {
        var result = _mapper.MapToPacket(ParseErrorFrame(name));

        Assert.IsFalse(result.IsSuccess, $"'{name}' mapped to {result.Value}");
        Assert.AreEqual(expectedCode, result.Error!.Code);
    }

    [TestMethod]
    [DataRow("04 13 01 00", DisplayName = "Value cut short")]
    [DataRow("02 13 01 00 04 13 01 00", DisplayName = "Second record cut short")]
    [DataRow("04 13 01 00 00 00 04", DisplayName = "DIF without VIF")]
    [DataRow("84", DisplayName = "DIF extension bit without DIFE")]
    [DataRow("04 93", DisplayName = "VIF extension bit without VIFE")]
    [DataRow("0D 13", DisplayName = "Variable length without LVAR")]
    [DataRow("0D 13 04 41 42 43", DisplayName = "Text one byte short")]
    public void MapToPacket_TruncatedRecord_FailsPrematureEnd(string records)
    {
        var result = Map(records);

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("PREMATURE_END", result.Error!.Code);
    }

    [TestMethod]
    public void MapToPacket_TenDifes_Succeeds()
    {
        // 84 + nine 80 DIFEs + 00, then volume 1
        var record = MapVariable($"84 {Repeat("80", 9)} 00 13 01 00 00 00").Records.Single();

        Assert.AreEqual(1, record.Value);
        Assert.AreEqual(0UL, record.StorageNumber);
    }

    [TestMethod]
    [DataRow(10, DisplayName = "11 DIFEs")]
    [DataRow(11, DisplayName = "12 DIFEs")]
    public void MapToPacket_MoreThanTenDifes_FailsTooManyDife(int extendedDifes)
    {
        var result = Map($"84 {Repeat("80", extendedDifes)} 00 13 01 00 00 00");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("TOO_MANY_DIFE", result.Error!.Code);
    }

    [TestMethod]
    public void MapToPacket_TenVifes_Succeeds()
    {
        // Manufacturer-specific VIF FF, nine 80 VIFEs + 00, then a 32-bit value
        var record = MapVariable($"04 FF {Repeat("80", 9)} 00 01 00 00 00").Records.Single();

        Assert.HasCount(11, record.Units);
        Assert.AreEqual(1, record.Value);
    }

    [TestMethod]
    [DataRow(10, DisplayName = "11 VIFEs")]
    [DataRow(12, DisplayName = "13 VIFEs")]
    public void MapToPacket_MoreThanTenVifes_FailsTooManyVife(int extendedVifes)
    {
        var result = Map($"04 FF {Repeat("80", extendedVifes)} 00 01 00 00 00");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("TOO_MANY_VIFE", result.Error!.Code);
    }

    [TestMethod]
    [DataRow("3F", DisplayName = "3F")]
    [DataRow("4F", DisplayName = "4F")]
    [DataRow("5F", DisplayName = "5F")]
    [DataRow("6F", DisplayName = "6F")]
    [DataRow("8F", DisplayName = "8F (data field F with extension)")]
    [DataRow("FF", DisplayName = "FF")]
    public void MapToPacket_ReservedDif_FailsReservedDif(string dif)
    {
        var result = Map($"04 13 01 00 00 00 {dif} 0C 13 78 56 34 12");

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("RESERVED_DIF", result.Error!.Code);
    }

    [TestMethod]
    public void MapToPacket_GlobalReadoutDif_IsSkipped()
    {
        var record = MapVariable("7F 0C 13 78 56 34 12").Records.Single();

        Assert.AreEqual(VariableDataQuantityUnit.Volume_m3, record.Units[0].Units);
        Assert.AreEqual(12345678L, record.Value);
    }

    // ---- Application error (CI 70h) and alarm status (CI 71h) ----

    [TestMethod]
    [DataRow("unspecified_error", ApplicationErrorCode.Unspecified)]
    [DataRow("unimplemented_ci", ApplicationErrorCode.Unimplemented_CI)]
    [DataRow("buffer_too_long", ApplicationErrorCode.BufferTooLong)]
    [DataRow("too_many_records", ApplicationErrorCode.TooManyRecords)]
    [DataRow("premature_end_of_record", ApplicationErrorCode.PrematureEnd)]
    [DataRow("too_many_difes", ApplicationErrorCode.TooManyDIFEs)]
    [DataRow("too_many_vifes", ApplicationErrorCode.TooManyVIFEs)]
    [DataRow("application_busy", ApplicationErrorCode.Busy)]
    [DataRow("too_many_readouts", ApplicationErrorCode.TooManyReadouts)]
    [DataRow("error", ApplicationErrorCode.Unspecified, DisplayName = "error (no status byte, L = 3)")]
    public void MapToPacket_ApplicationErrorFrame_ReturnsApplicationErrorPacket(string name, ApplicationErrorCode expected)
    {
        var result = _mapper.MapToPacket(ParseErrorFrame(name));

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        Assert.AreEqual(new ApplicationErrorPacket(0x01, expected), result.Value);
    }

    [TestMethod]
    public void MapToPacket_AlarmStatus_ReturnsAlarmStatusPacket()
    {
        var result = _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.STATUS_ALARM, 0x05, new byte[] { 0x13 }, 0));

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        Assert.AreEqual(new AlarmStatusPacket(0x05, 0x13), result.Value);
    }

    [TestMethod]
    public void MapToPacket_AlarmStatusWithoutData_IsZero()
    {
        var result = _mapper.MapToPacket(new ControlFrame(ControlMask.RSP_UD, ControlInformation.STATUS_ALARM, 0x05, 0));

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        Assert.AreEqual(new AlarmStatusPacket(0x05, 0), result.Value);
    }

    [TestMethod]
    public void MapToPacket_ApplicationErrorFromMaster_FailsWrongDirection()
    {
        var result = _mapper.MapToPacket(new ControlFrame(ControlMask.SND_UD, ControlInformation.ERROR_GENERAL, 0x05, 0));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("WRONG_DIRECTION", result.Error!.Code);
    }

    // ---- Identification number ----

    [TestMethod]
    public void MapToPacket_IdentificationNo_IsBcdAndRawKeepsBytes()
    {
        var packet = MapVariable("04 13 01 00 00 00");

        Assert.AreEqual(12345678u, packet.IdentificationNo);
        Assert.AreEqual(0x12345678u, packet.IdentificationRaw);
    }

    // libmbus reads non-decimal ID digits positionally rather than switching to binary
    [TestMethod]
    [DataRow("electricity-meter-1", 5000244u, 0x0500023Eu, DisplayName = "electricity-meter-1 (3E 02 00 05)")]
    [DataRow("electricity-meter-2", 5000345u, 0x050002E5u, DisplayName = "electricity-meter-2 (E5 02 00 05)")]
    public void MapToPacket_NonDecimalIdentificationNo_MatchesLibmbus(string name, uint expected, uint expectedRaw)
    {
        var packet = MapReferenceFrame(name);

        Assert.AreEqual(expected, packet.IdentificationNo);
        Assert.AreEqual(expectedRaw, packet.IdentificationRaw);
    }

    [TestMethod]
    public void MapToPacket_IdentificationNo_MatchesLibmbusForEveryReferenceFrame()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "DataExamples", "test-frames");
        var checkedFrames = 0;

        foreach (var xmlPath in Directory.GetFiles(dir, "*.xml").Order(StringComparer.Ordinal))
        {
            var id = System.Xml.Linq.XDocument.Load(xmlPath).Descendants("Id").FirstOrDefault()?.Value;
            var hexPath = Path.ChangeExtension(xmlPath, ".hex");
            if (id is null || !File.Exists(hexPath))
                continue;

            var frame = _parser.Parse(File.ReadAllText(hexPath).HexToBytes());
            if (!frame.IsSuccess)
                continue;

            var packet = _mapper.MapToPacket(frame.Value!).Value;
            var actual = packet switch
            {
                VariableDataPacket v => v.IdentificationNo,
                FixedDataPacket f => f.IdentificationNo,
                _ => (uint?)null,
            };
            if (actual is null)
                continue;

            Assert.AreEqual(uint.Parse(id, System.Globalization.CultureInfo.InvariantCulture), actual, Path.GetFileName(xmlPath));
            checkedFrames++;
        }

        Assert.IsGreaterThanOrEqualTo(70, checkedFrames);
    }

    // ---- Fixed data structure (CI 73h) ----

    [TestMethod]
    public void MapToPacket_FixedDataFrame_KeepsStatus()
    {
        // Power low, permanent error, temporary error and one manufacturer bit
        var packet = MapFixedPacket("78 56 34 12 0A 3C E9 7E 01 00 00 00 35 01 00 00");

        Assert.AreEqual((byte)0x3C, packet.Status);
        Assert.IsFalse(packet.CountersFixed);
        Assert.AreEqual(1L, packet.Counter1);
    }

    [TestMethod]
    public void MapToPacket_FixedDataFrameBinaryCounters_AreSigned()
    {
        var packet = MapFixedPacket("78 56 34 12 0A 01 E9 7E FF FF FF FF 9C FF FF FF");

        Assert.AreEqual(-1L, packet.Counter1);
        Assert.AreEqual(-100L, packet.Counter2);
        Assert.AreEqual(0x12345678u, packet.IdentificationRaw);
    }

    [TestMethod]
    public void MapToPacket_FixedDataFrameInvalidBcdCounter_IsNull()
    {
        // 1A is not BCD; it used to fall back to binary 26, the same as BCD 26 00 00 00
        var packet = MapFixedPacket("78 56 34 12 0A 00 E9 7E 1A 00 00 00 26 00 00 00");

        Assert.IsNull(packet.Counter1);
        Assert.AreEqual(26L, packet.Counter2);
    }

    [TestMethod]
    public void MapToPacket_FixedDataFrameBcdSignNibble_IsNegative()
    {
        var packet = MapFixedPacket("78 56 34 12 0A 00 E9 7E 01 00 00 F0 35 01 00 00");

        Assert.AreEqual(-1L, packet.Counter1);
    }

    [TestMethod]
    [DataRow("manual_frame2", FixedDataMedium.Water, DeviceType.Water, 1L, 135L, 12345678u)]
    [DataRow("sen_pollusonic_2", FixedDataMedium.Heat, DeviceType.Heat, 6531L, 69L, 90919293u)]
    public void MapToPacket_FixedReferenceFrame_MatchesLibmbus(string name, FixedDataMedium medium, DeviceType deviceType, long counter1, long counter2, uint identificationNo)
    {
        var hex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DataExamples", "test-frames", name + ".hex"));
        var result = _mapper.MapToPacket(_parser.Parse(hex.HexToBytes()).Value!);

        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        var packet = (FixedDataPacket)result.Value!;
        Assert.AreEqual(medium, packet.Medium);
        Assert.AreEqual(deviceType, packet.DeviceType);
        Assert.AreEqual(counter1, packet.Counter1);
        Assert.AreEqual(counter2, packet.Counter2);
        Assert.AreEqual(identificationNo, packet.IdentificationNo);
        Assert.AreEqual((byte)0, packet.Status);
    }

    [TestMethod]
    [DataRow("69 E9", FixedDataMedium.WaterMode2, DeviceType.Water, DisplayName = "D water mode 2")]
    [DataRow("A9 A9", FixedDataMedium.GasMode2, DeviceType.Gas, DisplayName = "A gas mode 2")]
    [DataRow("A9 E9", FixedDataMedium.HCAMode2, DeviceType.HeatCostAllocator, DisplayName = "E HCA mode 2")]
    public void MapToPacket_FixedDataFrameMode2Medium_ReadsCountersHighByteFirst(string mediumUnit, FixedDataMedium medium, DeviceType deviceType)
    {
        var packet = MapFixedPacket($"78 56 34 12 0A 00 {mediumUnit} 00 00 00 01 00 00 01 35");

        Assert.AreEqual(medium, packet.Medium);
        Assert.AreEqual(deviceType, packet.DeviceType);
        Assert.AreEqual(1L, packet.Counter1);
        Assert.AreEqual(135L, packet.Counter2);
    }

    [TestMethod]
    [DataRow("69 A9", FixedDataMedium.Reserved_0x09, DisplayName = "9")]
    [DataRow("E9 FE", FixedDataMedium.Reserved_0x0F, DisplayName = "F")]
    public void MapToPacket_FixedDataFrameReservedMedium_IsUnknownDeviceType(string mediumUnit, FixedDataMedium medium)
    {
        var packet = MapFixedPacket($"78 56 34 12 0A 00 {mediumUnit} 01 00 00 00 35 01 00 00");

        Assert.AreEqual(medium, packet.Medium);
        Assert.AreEqual(DeviceType.Unknown, packet.DeviceType);
        Assert.AreEqual(1L, packet.Counter1);
    }

    // ---- Encrypted records (configuration field, EN 13757-7) ----

    [TestMethod]
    [DataRow("10 05", 5, DisplayName = "Mode 5 AES-CBC")]
    [DataRow("20 07", 7, DisplayName = "Mode 7 AES-CBC ephemeral key")]
    [DataRow("00 0D", 13, DisplayName = "Mode 13 TLS")]
    [DataRow("00 01", 1, DisplayName = "Mode 1 manufacturer specific")]
    public void MapToPacket_EncryptedRecords_FailsEncrypted(string configuration, int mode)
    {
        var result = _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_VARIABLE, 0x01,
            $"78 56 34 12 24 40 01 07 55 00 {configuration} 04 13 01 00 00 00".HexToBytes(), 0));

        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("ENCRYPTED", result.Error!.Code);
        Assert.Contains($"security mode {mode} ", result.Error.Message);
    }

    [TestMethod]
    [DataRow("27 B6", DisplayName = "B627h, mode 22 (example_data_01)")]
    [DataRow("00 06", DisplayName = "Reserved mode 6")]
    [DataRow("FF 00", DisplayName = "Mode 0 with other bits set")]
    public void MapToPacket_UnencryptedConfiguration_DecodesRecords(string configuration)
    {
        var packet = AsVariablePacket(_mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_VARIABLE, 0x01,
            $"78 56 34 12 24 40 01 07 55 00 {configuration} 04 13 01 00 00 00".HexToBytes(), 0)));

        Assert.AreEqual(1, packet.Records.Single().Value);
    }

    private static string Repeat(string hexByte, int count) => string.Join(" ", Enumerable.Repeat(hexByte, count));

    private MBusFrame ParseErrorFrame(string name)
    {
        var hex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DataExamples", "error-frames", name + ".hex"));
        var frameResult = _parser.Parse(hex.HexToBytes());
        Assert.IsTrue(frameResult.IsSuccess, $"Frame parse failed: {frameResult.Error?.Message}");
        return frameResult.Value!;
    }

    private FixedDataPacket MapFixedPacket(string data)
    {
        var result = MapFixed(data);
        Assert.IsTrue(result.IsSuccess, result.Error?.Message);
        return (FixedDataPacket)result.Value!;
    }

    private MBusParseResult<MBusPacket> Map(string records) =>
        _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_VARIABLE, 0x01, $"{VariableHeader} {records}".HexToBytes(), 0));

    private MBusParseResult<MBusPacket> MapFixed(string data) =>
        _mapper.MapToPacket(new LongFrame(ControlMask.RSP_UD, ControlInformation.RESP_FIXED, 0x01, data.HexToBytes(), 0));

    private VariableDataPacket MapVariable(string records) => AsVariablePacket(Map(records));

    private VariableDataPacket MapVariable(MBusFrame frame) => AsVariablePacket(_mapper.MapToPacket(frame));

    private VariableDataPacket MapReferenceFrame(string name)
    {
        var hex = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "DataExamples", "test-frames", name + ".hex"));
        var frameResult = _parser.Parse(hex.HexToBytes());
        Assert.IsTrue(frameResult.IsSuccess, $"Frame parse failed: {frameResult.Error?.Message}");
        return MapVariable(frameResult.Value!);
    }

    private static VariableDataPacket AsVariablePacket(MBusParseResult<MBusPacket> result)
    {
        Assert.IsTrue(result.IsSuccess, $"Packet map failed: {result.Error?.Message}");
        return (VariableDataPacket)result.Value!;
    }
}
