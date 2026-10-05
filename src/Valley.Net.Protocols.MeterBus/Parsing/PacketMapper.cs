using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Text;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Maps parsed M-Bus frames to application-layer packets.
/// Replaces the old FrameExtensions.ToPacket() and GetRecords() methods.
/// </summary>
public sealed class PacketMapper : IPacketMapper
{
    // EN 13757-3 allows at most ten DIFEs and ten VIFEs per record
    private const int MaxExtensions = 10;

    private const byte GlobalReadoutRequest = 0x7F;

    private readonly VifLookupService _vifLookup;

    public PacketMapper(VifLookupService vifLookup)
    {
        _vifLookup = vifLookup ?? throw new ArgumentNullException(nameof(vifLookup));
    }

    public MBusParseResult<MBusPacket> MapToPacket(MBusFrame frame)
    {
        return frame switch
        {
            AckFrame => MBusParseResult<MBusPacket>.Ok(new EmptyPacket(0)),
            LongFrame lf => MapUserData(lf.Control, lf.ControlInformation, lf.Address, lf.Data.Span),
            // A reply with a CI field and no data (L = 3), such as an application error without its status byte
            ControlFrame cf => MapUserData(cf.Control, cf.ControlInformation, cf.Address, ReadOnlySpan<byte>.Empty),
            _ => MBusParseResult<MBusPacket>.Fail("UNSUPPORTED_FRAME", $"Cannot map frame type {frame.GetType().Name} to packet")
        };
    }

    private MBusParseResult<MBusPacket> MapUserData(ControlMask control, ControlInformation controlInformation, byte address, ReadOnlySpan<byte> data)
    {
        if ((control & ControlMask.DIR) != ControlMask.DIR_S2M)
            return MBusParseResult<MBusPacket>.Fail("WRONG_DIRECTION", "Frame direction is not slave-to-master");

        return controlInformation switch
        {
            ControlInformation.RESP_VARIABLE => MapVariableDataFrame(address, data),
            ControlInformation.RESP_FIXED => MapFixedDataFrame(address, data),
            ControlInformation.ERROR_GENERAL => MBusParseResult<MBusPacket>.Ok(new ApplicationErrorPacket(
                address,
                data.Length > 0 ? (ApplicationErrorCode)data[0] : ApplicationErrorCode.Unspecified)),
            ControlInformation.STATUS_ALARM => MBusParseResult<MBusPacket>.Ok(new AlarmStatusPacket(
                address,
                data.Length > 0 ? data[0] : (byte)0)),
            _ => MBusParseResult<MBusPacket>.Fail("UNSUPPORTED_CI", $"Unsupported control information: {controlInformation}")
        };
    }

    private MBusParseResult<MBusPacket> MapVariableDataFrame(byte address, ReadOnlySpan<byte> data)
    {
        if (data.Length < 12)
            return MBusParseResult<MBusPacket>.Fail("VAR_FRAME_TOO_SHORT", "Variable data frame requires at least 12 bytes");

        var identificationNo = ParseIdentificationNo(data.Slice(0, 4));
        var manufacturer = BitConverter.ToUInt16(data.Slice(4, 2));
        var version = data[6];
        var deviceType = (DeviceType)data[7];
        var transmissionCounter = data[8];
        var status = data[9];
        var signature = BitConverter.ToUInt16(data.Slice(10, 2));

        // The signature is the configuration field of EN 13757-7, whose bits 8-12 give the security mode.
        // Ciphertext would decode as plausible garbage records, so refuse it.
        var securityMode = (signature >> 8) & 0x1F;
        if (IsEncrypted(securityMode))
            return MBusParseResult<MBusPacket>.Fail("ENCRYPTED", $"Records are encrypted with security mode {securityMode} (configuration field {signature:X4}h); decryption is not supported");

        var body = ParseDataRecords(data.Slice(12));
        if (!body.IsSuccess)
            return MBusParseResult<MBusPacket>.Fail(body.Error!);

        return MBusParseResult<MBusPacket>.Ok(new VariableDataPacket(
            address,
            identificationNo,
            manufacturer,
            version,
            deviceType,
            transmissionCounter,
            status,
            signature,
            body.Value!.Records)
        {
            IdentificationRaw = BinaryPrimitives.ReadUInt32LittleEndian(data),
            ManufacturerData = body.Value.ManufacturerData,
            MoreRecordsFollow = body.Value.MoreRecordsFollow,
        });
    }

    // The security modes EN 13757-7 assigns to a cipher (1 is manufacturer specific). The reserved values are
    // left alone: meters built to EN 13757-3:2004 fill this field with an arbitrary signature, such as B627h
    // (mode 22) in DataExamples/test-frames/example_data_01, and send plaintext.
    private static bool IsEncrypted(int securityMode) =>
        securityMode is (>= 1 and <= 5) or (>= 7 and <= 10) or 13;

    private static MBusParseResult<MBusPacket> MapFixedDataFrame(byte address, ReadOnlySpan<byte> data)
    {
        if (data.Length != 16)
            return MBusParseResult<MBusPacket>.Fail("FIXED_FRAME_INVALID_LENGTH", $"Fixed data frame requires exactly 16 bytes, got {data.Length}");

        var identificationNo = ParseIdentificationNo(data.Slice(0, 4));
        var transmissionCounter = data[4];
        var status = data[5];
        var countersBinary = (status & 0x01) != 0;
        var countersFixed = (status & 0x02) != 0;

        var buf6 = data[6];
        var buf7 = data[7];
        var units1 = (FixedDataUnits)(buf6 & 0x3F);
        var units2 = (FixedDataUnits)(buf7 & 0x3F);
        var medium = (FixedDataMedium)(((buf6 & 0xC0) >> 6) | ((buf7 & 0xC0) >> 4));

        // The "Mode 2" media mark meters that send the counters high byte first despite CI 73h (MBDOC48 8.3.1)
        Span<byte> counters = stackalloc byte[8];
        data.Slice(8, 8).CopyTo(counters);
        if (medium is >= FixedDataMedium.GasMode2 and <= FixedDataMedium.HCAMode2)
        {
            counters.Slice(0, 4).Reverse();
            counters.Slice(4, 4).Reverse();
        }

        return MBusParseResult<MBusPacket>.Ok(new FixedDataPacket(
            address,
            identificationNo,
            ToDeviceType(medium),
            transmissionCounter,
            countersFixed,
            units1,
            units2,
            ParseCounter(counters.Slice(0, 4), countersBinary),
            ParseCounter(counters.Slice(4, 4), countersBinary))
        {
            IdentificationRaw = BinaryPrimitives.ReadUInt32LittleEndian(data),
            Status = status,
            Medium = medium,
        });
    }

    // Status bit 0 selects signed binary or BCD for both counters (MBDOC48 fig. 16). A BCD counter with a
    // non-decimal digit is an error indication, so it becomes null rather than a number.
    private static long? ParseCounter(ReadOnlySpan<byte> counter, bool binary)
    {
        if (binary)
            return BinaryPrimitives.ReadInt32LittleEndian(counter);

        return counter.TryDecodeBcd(out var value) ? value : null;
    }

    // The fixed-structure medium table differs from the variable one from 9 up: 9 and F are reserved and
    // A-E are the mode 2 variants of gas, heat, hot water, water and HCA.
    private static DeviceType ToDeviceType(FixedDataMedium medium) => medium switch
    {
        FixedDataMedium.GasMode2 => DeviceType.Gas,
        FixedDataMedium.HeatMode2 => DeviceType.Heat,
        FixedDataMedium.HotWaterMode2 => DeviceType.WarmWater,
        FixedDataMedium.WaterMode2 => DeviceType.Water,
        FixedDataMedium.HCAMode2 => DeviceType.HeatCostAllocator,
        FixedDataMedium.Reserved_0x09 or FixedDataMedium.Reserved_0x0F => DeviceType.Unknown,
        _ => (DeviceType)medium,
    };

    private MBusParseResult<DataRecordBlock> ParseDataRecords(ReadOnlySpan<byte> data)
    {
        var records = ImmutableArray.CreateBuilder<DataRecord>();
        var offset = 0;

        while (offset < data.Length)
        {
            var type = data[offset++];

            switch ((VariableDataRecordType)type)
            {
                case VariableDataRecordType.MBUS_DIB_DIF_IDLE_FILLER:
                    continue;

                case VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC:
                case VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW:
                    // The rest of the user data is manufacturer specific, not records
                    return MBusParseResult<DataRecordBlock>.Ok(new DataRecordBlock(
                        records.ToImmutable(),
                        ImmutableArray.Create(data.Slice(offset)),
                        (VariableDataRecordType)type == VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW));

                case (VariableDataRecordType)GlobalReadoutRequest:
                    // A master-to-slave selection with no VIF or value; nothing to read in a response
                    continue;
            }

            // The other special-function DIFs (data field 0xF) are reserved and their length is unknown
            if ((type & 0x0F) == 0x0F)
                return MBusParseResult<DataRecordBlock>.Fail("RESERVED_DIF", $"Record {records.Count} has reserved DIF {type:X2}h at offset {offset - 1}");

            // Parse DIF
            var difByte = type;
            var dataType = (DataTypes)(difByte & 0x0F);
            var function = (Function)(difByte & 0x30);
            var storageLsb = (difByte & 0x40) != 0;
            var difExtension = (difByte & 0x80) != 0;

            ulong storageNumber = storageLsb ? 1UL : 0UL;
            uint tariff = 0;
            ushort subUnit = 0;

            // Parse DIFEs
            var difeIndex = 0;
            while (difExtension)
            {
                if (offset >= data.Length)
                    return PrematureEnd(records.Count, "DIFE");
                if (difeIndex == MaxExtensions)
                    return MBusParseResult<DataRecordBlock>.Fail("TOO_MANY_DIFE", $"Record {records.Count} has more than {MaxExtensions} DIFEs");

                var difeByte = data[offset++];
                difExtension = (difeByte & 0x80) != 0;

                var snPart = (ulong)(difeByte & 0x0F);
                snPart <<= (difeIndex * 4 + 1);
                storageNumber |= snPart;

                var tPart = (uint)((difeByte >> 4) & 0x03);
                tPart <<= (difeIndex * 2);
                tariff |= tPart;

                var suPart = (ushort)((difeByte >> 6) & 0x01);
                suPart <<= difeIndex;
                subUnit |= suPart;

                difeIndex++;
            }

            if (offset >= data.Length)
                return PrematureEnd(records.Count, "VIF");

            // Parse VIF
            var vifByte = data[offset++];
            var vifInfo = _vifLookup.Resolve(vifByte);

            var vifUnit = new UnitInfo(vifInfo.Units, vifInfo.Unit, vifInfo.Magnitude, vifInfo.Quantity, vifInfo.VifString);

            // Plain-text VIF: a length byte and the ASCII unit (rightmost character first) come before any VIFEs
            if (vifInfo.Type == VifType.PlainTextVIF)
            {
                if (offset >= data.Length || offset + 1 + data[offset] > data.Length)
                    return PrematureEnd(records.Count, "plain-text unit");

                var textLength = data[offset++];
                var text = data.Slice(offset, textLength).ToArray();
                Array.Reverse(text);
                vifUnit = vifUnit with { Unit = Encoding.ASCII.GetString(text) };
                offset += textLength;
            }

            var units = ImmutableArray.CreateBuilder<UnitInfo>();
            units.Add(vifUnit);

            // Only the first VIFE after FB/FD is the true VIF from that table; later ones are combinable VIFEs
            var extensionTable = vifInfo.Type switch
            {
                VifType.LinearVIFExtensionFB => VifExtensionTable.FB,
                VifType.LinearVIFExtensionFD => VifExtensionTable.FD,
                _ => VifExtensionTable.Primary,
            };

            // After VIF or VIFE 7Fh the remaining VIFEs have manufacturer-specific coding and no standard meaning
            var manufacturerSpecific = vifInfo.Type == VifType.ManufacturerSpecific;

            // Parse VIFEs
            var vifeExtension = vifInfo.HasExtension;
            var vifeCount = 0;
            while (vifeExtension)
            {
                if (offset >= data.Length)
                    return PrematureEnd(records.Count, "VIFE");
                if (vifeCount == MaxExtensions)
                    return MBusParseResult<DataRecordBlock>.Fail("TOO_MANY_VIFE", $"Record {records.Count} has more than {MaxExtensions} VIFEs");

                var vifeByte = data[offset++];
                vifeExtension = (vifeByte & 0x80) != 0;
                vifeCount++;

                if (manufacturerSpecific)
                {
                    units.Add(new UnitInfo(VariableDataQuantityUnit.ManufacturerSpecific, null, 0, null, $"{vifeByte & 0x7F:X2}h"));
                    continue;
                }

                var vifeInfo = _vifLookup.ResolveExtension(vifeByte, extensionTable);
                units.Add(new UnitInfo(vifeInfo.Units, vifeInfo.Unit, vifeInfo.Magnitude, vifeInfo.Quantity, vifeInfo.VifString));
                manufacturerSpecific = extensionTable == VifExtensionTable.Primary && (vifeByte & 0x7F) == 0x7F;
                // A combinable VIFE FCh says the next VIFE comes from the combinable extension table
                extensionTable = extensionTable == VifExtensionTable.Primary && (vifeByte & 0x7F) == 0x7C
                    ? VifExtensionTable.FC
                    : VifExtensionTable.Primary;
            }

            // Parse value. A variable-length value starts with its LVAR byte, which gives both its kind and its length.
            byte lvar = 0;
            int valueLength;
            if (dataType == DataTypes._variable_length)
            {
                if (offset >= data.Length)
                    return PrematureEnd(records.Count, "LVAR");
                lvar = data[offset++];
                if (ValueParser.VariableLength(lvar) is not { } lvarLength)
                    return MBusParseResult<DataRecordBlock>.Fail("RESERVED_LVAR", $"Record {records.Count} has reserved LVAR {lvar:X2}h");
                valueLength = lvarLength;
            }
            else
            {
                valueLength = MBusConstants.LengthsInBitsTable.GetValueOrDefault(dataType, 0) / 8;
            }

            if (offset + valueLength > data.Length)
                return PrematureEnd(records.Count, "value");

            byte[]? valueData = null;
            if (valueLength > 0)
            {
                valueData = data.Slice(offset, valueLength).ToArray();
                offset += valueLength;
            }

            object? value = null;
            string? valueError = null;
            if (valueData != null)
            {
                value = dataType == DataTypes._variable_length ? ValueParser.ParseVariableLength(lvar, valueData, out valueError)
                    : units.Any(u => IsDateTime(u.Units)) ? ValueParser.ParseDateTime(dataType, valueData, out valueError)
                    : ValueParser.ParseValue(dataType, valueData, out valueError);
            }

            records.Add(new DataRecord(
                (VariableDataRecordType)difByte,
                function,
                storageNumber,
                tariff,
                subUnit,
                dataType,
                value,
                units.ToImmutable())
            {
                ValueError = valueError,
            });
        }

        return MBusParseResult<DataRecordBlock>.Ok(new DataRecordBlock(records.ToImmutable(), ImmutableArray<byte>.Empty, false));
    }

    private static MBusParseResult<DataRecordBlock> PrematureEnd(int record, string part) =>
        MBusParseResult<DataRecordBlock>.Fail("PREMATURE_END", $"The {part} of record {record} runs past the end of the data");

    // Time point VIF 6Ch/6Dh and the VIFEs whose value is a date/time (EN 13757-3), coded as type G, J, F or I
    private static bool IsDateTime(VariableDataQuantityUnit unit) => unit is
        VariableDataQuantityUnit.TimePoint or
        VariableDataQuantityUnit.StartDateTimeOf or
        VariableDataQuantityUnit.DateTimeOfLimitExceed or
        VariableDataQuantityUnit.DateTimeOfLimitAbove or
        VariableDataQuantityUnit.StartDateTimeOfTariff or
        VariableDataQuantityUnit.DateTimeOfBatteryChange;

    // Eight BCD digits, decoded as each nibble times its decimal position the way libmbus does, so a
    // non-decimal digit still gives a stable number (3E 02 00 05 -> 5000244) instead of a binary reading.
    // IdentificationRaw keeps the bytes for anything that needs them exactly.
    private static uint ParseIdentificationNo(ReadOnlySpan<byte> identificationNo)
    {
        uint value = 0;
        for (int i = identificationNo.Length - 1; i >= 0; i--)
            value = (value * 10 + (uint)(identificationNo[i] >> 4)) * 10 + (uint)(identificationNo[i] & 0x0F);
        return value;
    }

    private sealed record DataRecordBlock(
        ImmutableArray<DataRecord> Records,
        ImmutableArray<byte> ManufacturerData,
        bool MoreRecordsFollow);
}
