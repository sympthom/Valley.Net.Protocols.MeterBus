using System.Collections.Immutable;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Maps parsed M-Bus frames to application-layer packets.
/// Replaces the old FrameExtensions.ToPacket() and GetRecords() methods.
/// </summary>
public sealed class PacketMapper : IPacketMapper
{
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
            LongFrame lf => MapLongFrame(lf),
            _ => MBusParseResult<MBusPacket>.Fail("UNSUPPORTED_FRAME", $"Cannot map frame type {frame.GetType().Name} to packet")
        };
    }

    private MBusParseResult<MBusPacket> MapLongFrame(LongFrame frame)
    {
        if ((frame.Control & ControlMask.DIR) != ControlMask.DIR_S2M)
            return MBusParseResult<MBusPacket>.Fail("WRONG_DIRECTION", "Frame direction is not slave-to-master");

        return frame.ControlInformation switch
        {
            ControlInformation.RESP_VARIABLE => MapVariableDataFrame(frame),
            ControlInformation.RESP_FIXED => MapFixedDataFrame(frame),
            _ => MBusParseResult<MBusPacket>.Fail("UNSUPPORTED_CI", $"Unsupported control information: {frame.ControlInformation}")
        };
    }

    private MBusParseResult<MBusPacket> MapVariableDataFrame(LongFrame frame)
    {
        var data = frame.Data.Span;
        if (data.Length < 12)
            return MBusParseResult<MBusPacket>.Fail("VAR_FRAME_TOO_SHORT", "Variable data frame requires at least 12 bytes");

        var identificationNo = ParseIdentificationNo(data.Slice(0, 4));
        var manufacturer = BitConverter.ToUInt16(data.Slice(4, 2));
        var version = data[6];
        var deviceType = (DeviceType)data[7];
        var transmissionCounter = data[8];
        var status = data[9];
        var signature = BitConverter.ToUInt16(data.Slice(10, 2));

        var records = ParseDataRecords(data.Slice(12));

        return MBusParseResult<MBusPacket>.Ok(new VariableDataPacket(
            frame.Address,
            identificationNo,
            manufacturer,
            version,
            deviceType,
            transmissionCounter,
            status,
            signature,
            records));
    }

    private MBusParseResult<MBusPacket> MapFixedDataFrame(LongFrame frame)
    {
        var data = frame.Data.Span;
        if (data.Length < 8)
            return MBusParseResult<MBusPacket>.Fail("FIXED_FRAME_TOO_SHORT", "Fixed data frame requires at least 8 bytes");

        var identificationNo = ParseIdentificationNo(data.Slice(0, 4));
        var transmissionCounter = data[4];
        var status = data[5];
        var countersBcd = (status & 0x01) == 0;
        var countersFixed = (status & 0x02) != 0;

        var buf6 = data[6];
        var buf7 = data[7];
        var units1 = (FixedDataUnits)(buf6 & 0x3F);
        var units2 = (FixedDataUnits)(buf7 & 0x3F);
        var deviceType = (DeviceType)(byte)(((buf6 & 0xC0) >> 6) | ((buf7 & 0xC0) >> 4));

        uint counter1 = 0, counter2 = 0;
        if (data.Length >= 16)
        {
            if (countersBcd)
            {
                counter1 = ParseBcdOrBinary(data.Slice(8, 4));
                counter2 = ParseBcdOrBinary(data.Slice(12, 4));
            }
            else
            {
                counter1 = BitConverter.ToUInt32(data.Slice(8, 4));
                counter2 = BitConverter.ToUInt32(data.Slice(12, 4));
            }
        }

        return MBusParseResult<MBusPacket>.Ok(new FixedDataPacket(
            frame.Address,
            identificationNo,
            deviceType,
            transmissionCounter,
            countersFixed,
            units1,
            units2,
            counter1,
            counter2));
    }

    private ImmutableArray<DataRecord> ParseDataRecords(ReadOnlySpan<byte> data)
    {
        var records = ImmutableArray.CreateBuilder<DataRecord>();
        var offset = 0;

        while (offset < data.Length)
        {
            var type = data[offset++];

            switch ((VariableDataRecordType)type)
            {
                case VariableDataRecordType.MBUS_DIB_DIF_IDLE_FILLER:
                case VariableDataRecordType.MBUS_DIB_DIF_MORE_RECORDS_FOLLOW:
                    continue;

                case VariableDataRecordType.MBUS_DIB_DIF_MANUFACTURER_SPECIFIC:
                    // Rest is manufacturer specific, skip
                    offset = data.Length;
                    continue;
            }

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
            while (difExtension && offset < data.Length)
            {
                if (difeIndex > 10) break;
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

            if (offset >= data.Length) break;

            // Parse VIF
            var vifByte = data[offset++];
            var vifInfo = _vifLookup.Resolve(vifByte);

            var units = ImmutableArray.CreateBuilder<UnitInfo>();
            units.Add(new UnitInfo(vifInfo.Units, vifInfo.Unit, vifInfo.Magnitude, vifInfo.Quantity, vifInfo.VifString));

            // Determine extension table
            var extensionTable = vifInfo.Type switch
            {
                VifType.LinearVIFExtensionFB => VifExtensionTable.FB,
                VifType.LinearVIFExtensionFD => VifExtensionTable.FD,
                _ => VifExtensionTable.Primary,
            };

            // Parse VIFEs
            var vifeExtension = vifInfo.HasExtension;
            var vifeCount = 0;
            while (vifeExtension && offset < data.Length)
            {
                if (vifeCount > 10) break;
                var vifeByte = data[offset++];
                var vifeInfo = _vifLookup.ResolveExtension(vifeByte, extensionTable);
                units.Add(new UnitInfo(vifeInfo.Units, vifeInfo.Unit, vifeInfo.Magnitude, vifeInfo.Quantity, vifeInfo.VifString));
                vifeExtension = vifeInfo.HasExtension;
                vifeCount++;
            }

            // Parse value
            var valueLength = dataType == DataTypes._variable_length
                ? (offset < data.Length ? data[offset++] : 0)
                : MBusConstants.LengthsInBitsTable.GetValueOrDefault(dataType, 0) / 8;

            byte[]? valueData = null;
            if (valueLength > 0 && offset + valueLength <= data.Length)
            {
                valueData = data.Slice(offset, valueLength).ToArray();
                offset += valueLength;
            }
            else if (valueLength > 0)
            {
                // Not enough data remaining
                break;
            }

            var value = valueData != null ? ValueParser.ParseValue(dataType, valueData) : null;

            records.Add(new DataRecord(
                (VariableDataRecordType)difByte,
                function,
                storageNumber,
                tariff,
                subUnit,
                dataType,
                value,
                units.ToImmutable()));
        }

        return records.ToImmutable();
    }

    private static uint ParseIdentificationNo(ReadOnlySpan<byte> identificationNo)
    {
        var bytes = identificationNo.ToArray();
        var bcdStr = bytes.BCDToString();
        if (uint.TryParse(bcdStr, out var result))
            return result;

        // Fallback: treat as little-endian binary value
        return BitConverter.ToUInt32(bytes, 0);
    }

    private static uint ParseBcdOrBinary(ReadOnlySpan<byte> data)
    {
        var bytes = data.ToArray();
        var bcdStr = bytes.BCDToString();
        if (uint.TryParse(bcdStr, out var result))
            return result;
        return BitConverter.ToUInt32(bytes, 0);
    }
}
