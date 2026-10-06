using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Modern M-Bus master implementation using async/await, CancellationToken, and DI.
/// Replaces the old event-driven MBusMaster class.
/// </summary>
/// <remarks>
/// The master does not own the transport: disposing the master leaves the transport open for
/// whoever created it. One master serialises its own exchanges; two masters on one transport do not
/// see each other, so use one master per bus.
/// </remarks>
public sealed class MBusMaster : IMBusMaster, IDisposable
{
    private readonly IMBusTransport _transport;
    private readonly IFrameParser _parser;
    private readonly IFrameSerializer _serializer;
    private readonly IPacketMapper _mapper;
    private readonly int _retries;
    private readonly int _scanRetries;
    private readonly int _maxTelegrams;

    // The "next FCB image" bits of MBDOC48 5.5.2 and 7.2, indexed by address: one for requests and one for
    // SND_UD, as the slave keeps them apart. Set means the next FCV frame carries FCB = 1. They start set,
    // as after a SND_NKE, since nothing is known about the slaves yet. Only touched while the bus is held.
    private readonly bool[] _requestFcb = new bool[256];
    private readonly bool[] _sendFcb = new bool[256];

    // M-Bus is half duplex with one exchange at a time; overlapping callers would collide on the wire
    // and be handed each other's replies.
    private readonly SemaphoreSlim _bus = new(1, 1);
    private volatile bool _disposed;

    public MBusMaster(
        IMBusTransport transport,
        IFrameParser parser,
        IFrameSerializer serializer,
        IPacketMapper mapper)
        : this(transport, parser, serializer, mapper, new MBusMasterOptions())
    {
    }

    public MBusMaster(
        IMBusTransport transport,
        IFrameParser parser,
        IFrameSerializer serializer,
        IPacketMapper mapper,
        MBusMasterOptions options)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Retries, nameof(options.Retries));
        ArgumentOutOfRangeException.ThrowIfNegative(options.ScanRetries, nameof(options.ScanRetries));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxTelegrams, 1, nameof(options.MaxTelegrams));
        _retries = options.Retries;
        _scanRetries = options.ScanRetries;
        _maxTelegrams = options.MaxTelegrams;
        Array.Fill(_requestFcb, true);
        Array.Fill(_sendFcb, true);
    }

    public async Task<bool> PingAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfReserved(address);

        using var _ = await AcquireBusAsync(ct);

        var frame = ShortFrameFor(ControlMask.SND_NKE, address);
        ResetFcb(address);
        if (address == MBusConstants.ADDRESS_BROADCAST_NOREPLY)
        {
            await SendAsync(_serializer.Serialize(frame), ct);
            return false;
        }

        var reply = await ExchangeAsync(frame, _retries, ct);
        return reply.Frame is AckFrame;
    }

    public Task<MBusPacket> RequestDataAsync(byte address, CancellationToken ct = default)
        => RequestAsync(address, ControlMask.REQ_UD2, ct);

    public Task<MBusPacket> RequestAlarmAsync(byte address, CancellationToken ct = default)
        => RequestAsync(address, ControlMask.REQ_UD1, ct);

    public async Task<IReadOnlyList<MBusPacket>> RequestAllTelegramsAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfReserved(address);
        ThrowIfNoReply(address);

        // Held for the whole sequence: a request from another caller in between would take a telegram and move the FCB on.
        using var _ = await AcquireBusAsync(ct);

        var telegrams = new List<MBusPacket>();
        while (true)
        {
            // MBDOC48 6.4: the slave may end a sequence with an E5 instead of a telegram without DIF 1Fh.
            var packet = await RequestLockedAsync(address, ControlMask.REQ_UD2, ackIsEmpty: telegrams.Count > 0, ct);
            if (packet is EmptyPacket)
                return telegrams;

            // Replies via 0xFD/0xFE are not address-checked, so a different meter could slip in mid-sequence.
            if (telegrams is [VariableDataPacket first, ..] && packet is VariableDataPacket next && !IsSameMeter(first, next))
            {
                throw new MBusException(address, new MBusError(MBusConstants.ERROR_TELEGRAM_MISMATCH,
                    $"Telegram {telegrams.Count + 1} comes from meter {next.IdentificationRaw:X8}, not {first.IdentificationRaw:X8}"));
            }

            telegrams.Add(packet);
            if (packet is not VariableDataPacket { MoreRecordsFollow: true })
                return telegrams;

            if (telegrams.Count == _maxTelegrams)
            {
                throw new MBusException(address, new MBusError(MBusConstants.ERROR_TELEGRAM_LIMIT,
                    $"More records still follow after {_maxTelegrams} telegrams (MBusMasterOptions.MaxTelegrams)"));
            }
        }
    }

    public async Task SetAddressAsync(byte address, byte newAddress, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // 251-255 are reserved or broadcast; a meter given one of them can no longer be reached by primary address.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newAddress, MBusConstants.ADDRESS_PRIMARY_MAX);

        byte[] data = [MBusConstants.SET_ADDRESS_DIF, MBusConstants.SET_ADDRESS_VIF, newAddress];
        await SendUserDataAsync(ControlInformation.DATA_SEND, address, data, ct);
    }

    public async Task InitializeAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfReserved(address);

        using var _ = await AcquireBusAsync(ct);

        ResetFcb(address);
        await CommandAsync(ShortFrameFor(ControlMask.SND_NKE, address), address, ct);
    }

    public async Task ResetApplicationAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await SendUserDataAsync(ControlInformation.APPLICATION_RESET, address, ReadOnlyMemory<byte>.Empty, ct);
    }

    public async IAsyncEnumerable<MeterInfo> ScanAsync(
        IEnumerable<byte> addresses,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(addresses);

        foreach (var address in addresses)
        {
            ct.ThrowIfCancellationRequested();
            ObjectDisposedException.ThrowIf(_disposed, this);
            ThrowIfReserved(address);
            ThrowIfNoReply(address);

            // The bus is held per address and released before the yield, so a slow consumer does not block other callers.
            var meter = await ProbeAsync(address, ct);
            if (meter is not null)
                yield return meter;
        }
    }

    public async Task SendDataAsync(byte address, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // Checked here because the frame is only built once the bus is held; an oversized one must not touch it.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(data.Length, MBusConstants.FRAME_LONG_MAX_DATA_LENGTH, nameof(data));

        await SendUserDataAsync(ControlInformation.DATA_SEND, address, data, ct);
    }

    public Task SelectSlaveAsync(SecondaryAddress secondary, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(secondary);
        // Eight BCD digits; a larger number would lose its leading digits and select some other meter.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(secondary.IdentificationNo, MBusConstants.SECONDARY_ID_MAX, nameof(secondary));

        uint raw = 0;
        for (uint id = secondary.IdentificationNo, shift = 0; id > 0; id /= 10, shift += 4)
            raw |= (id % 10) << (int)shift;

        return SelectSlaveAsync(raw, secondary.ManufacturerId, secondary.Version, secondary.DeviceType, ct);
    }

    public async Task SelectSlaveAsync(uint identificationRaw, ushort manufacturerId, byte version, DeviceType deviceType, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // ID (4 bytes, least significant BCD byte first), manufacturer (little-endian), version, medium.
        var data = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(data, identificationRaw);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(4), manufacturerId);
        data[6] = version;
        data[7] = (byte)deviceType;

        const byte address = MBusConstants.ADDRESS_NETWORK_LAYER;
        using var _ = await AcquireBusAsync(ct);

        // Plain SND_UD as in MBDOC48 Fig. 29 and libmbus. A selection, matched or not, restarts the FCB sequence
        // on 253: the slave clears its FCB memory and the master starts again with FCB = 1 (MBDOC48 7.2).
        ResetFcb(address);
        await CommandAsync(BuildLongFrame(ControlMask.SND_UD, ControlInformation.SELECT_SLAVE, address, data), address, ct);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose() => _disposed = true;

    // ---- Private helpers ----

    private async Task<MBusPacket> RequestAsync(byte address, ControlMask request, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ThrowIfReserved(address);
        ThrowIfNoReply(address);

        using var _ = await AcquireBusAsync(ct);

        // E5 to REQ_UD1 means no alarm is pending (IEC 60870-5-2); to REQ_UD2 it is not an answer at all.
        return await RequestLockedAsync(address, request, ackIsEmpty: request == ControlMask.REQ_UD1, ct);
    }

    // The caller holds the bus.
    private async Task<MBusPacket> RequestLockedAsync(byte address, ControlMask request, bool ackIsEmpty, CancellationToken ct)
    {
        var reply = await RequestFrameAsync(address, request, ackIsEmpty, _retries, ct);
        if (reply.Frame is null)
        {
            if (reply.Garbled is not null)
                throw new MBusException(address, reply.Garbled);
            throw new TimeoutException($"No reply from M-Bus address {address}");
        }

        var result = ToPacket(reply.Frame, address, ackIsEmpty);
        return result.IsSuccess ? result.Value! : throw new MBusException(address, result.Error!);
    }

    /// <summary>
    /// Sends REQ_UD1/REQ_UD2 with the address's next FCB and toggles it once the slave answered, so the next
    /// request asks for new data (MBDOC48 5.5.2). Retries inside the exchange repeat the same FCB: a slave that
    /// answered but whose reply was lost then sends that reply again. The caller holds the bus.
    /// </summary>
    private async Task<Reply> RequestFrameAsync(byte address, ControlMask request, bool ackIsEmpty, int retries, CancellationToken ct)
    {
        bool fcb = _requestFcb[address];
        var reply = await ExchangeAsync(ShortFrameFor(WithFcb(request, fcb), address), retries, ct);
        if (reply.Frame is not null && IsAnswer(reply.Frame, address, ackIsEmpty))
            _requestFcb[address] = !fcb;
        return reply;
    }

    /// <summary>
    /// Sends SND_UD with the address's next SND_UD FCB, which is toggled once the slave ACKs (MBDOC48 5.5.2).
    /// </summary>
    private async Task SendUserDataAsync(ControlInformation ci, byte address, ReadOnlyMemory<byte> data, CancellationToken ct)
    {
        ThrowIfReserved(address);

        using var _ = await AcquireBusAsync(ct);

        // Nothing answers 255, so there is no exchange to count. MBDOC48 5.5.2 (4) has it sent with FCV cleared, so a
        // slave whose last SND_UD carried FCB = 0 does not take the broadcast for a repeat and drop it (libmbus sends 53h).
        if (address == MBusConstants.ADDRESS_BROADCAST_NOREPLY)
        {
            await CommandAsync(BuildLongFrame(ControlMask.SND_UD & ~ControlMask.FCV, ci, address, data.Span), address, ct);
            return;
        }

        bool fcb = _sendFcb[address];
        await CommandAsync(BuildLongFrame(WithFcb(ControlMask.SND_UD, fcb), ci, address, data.Span), address, ct);
        _sendFcb[address] = !fcb;
    }

    /// <summary>
    /// Sends an SND_NKE or SND_UD and consumes the slave's E5, so it cannot be taken as the reply
    /// to the next request. Broadcast 0xFF is never answered, so nothing is read for it. The caller holds the bus.
    /// </summary>
    private async Task CommandAsync(MBusFrame frame, byte address, CancellationToken ct)
    {
        var bytes = _serializer.Serialize(frame);

        if (address == MBusConstants.ADDRESS_BROADCAST_NOREPLY)
        {
            await SendAsync(bytes, ct);
            return;
        }

        var reply = await ExchangeAsync(bytes, _retries, ct);
        if (reply.Frame is null)
        {
            if (reply.Garbled is not null)
                throw new MBusException(address, reply.Garbled);
            throw new TimeoutException($"No ACK from M-Bus address {address}");
        }

        if (reply.Frame is not AckFrame)
            throw new MBusException(address, Unexpected("ACK", reply.Frame));
    }

    // Null when nothing answered. Mirrors libmbus scanning: a garbled reply is reported as a collision, not skipped.
    private async Task<MeterInfo?> ProbeAsync(byte address, CancellationToken ct)
    {
        using var _ = await AcquireBusAsync(ct);

        ResetFcb(address);
        var ping = await ExchangeAsync(ShortFrameFor(ControlMask.SND_NKE, address), _scanRetries, ct);
        if (ping.Frame is null)
            return ping.Garbled is null ? null : new MeterInfo(address, null, ScanStatus.Collision, ping.Garbled);

        if (ping.Frame is not AckFrame)
            return new MeterInfo(address, null, ScanStatus.Error, Unexpected("ACK", ping.Frame));

        var data = await RequestFrameAsync(address, ControlMask.REQ_UD2, ackIsEmpty: false, _retries, ct);
        if (data.Frame is null)
        {
            return data.Garbled is null
                ? new MeterInfo(address, null, ScanStatus.Error, new MBusError(MBusConstants.ERROR_NO_REPLY, "ACKed SND_NKE but did not answer REQ_UD2"))
                : new MeterInfo(address, null, ScanStatus.Collision, data.Garbled);
        }

        var result = ToPacket(data.Frame, address, ackIsEmpty: false);
        return result.IsSuccess
            ? new MeterInfo(address, result.Value)
            : new MeterInfo(address, null, ScanStatus.Error, result.Error);
    }

    private MBusParseResult<MBusPacket> ToPacket(MBusFrame reply, byte address, bool ackIsEmpty)
    {
        byte? from = ReplyAddress(reply);
        if (from is null)
        {
            return reply is AckFrame && ackIsEmpty
                ? MBusParseResult<MBusPacket>.Ok(new EmptyPacket(address))
                : MBusParseResult<MBusPacket>.Fail(Unexpected("RSP_UD", reply));
        }

        if (!IsReplyFrom(from.Value, address))
        {
            return MBusParseResult<MBusPacket>.Fail(
                MBusConstants.ERROR_ADDRESS_MISMATCH,
                $"Reply from M-Bus address {from} does not match requested address {address}");
        }

        return _mapper.MapToPacket(reply);
    }

    /// <summary>
    /// Sends <paramref name="request"/> until a reply parses or the attempts run out. Only a missing or
    /// garbled reply is retried, with the frame unchanged as EN 13757-2 asks for a repeat.
    /// The caller holds the bus.
    /// </summary>
    private Task<Reply> ExchangeAsync(MBusFrame request, int retries, CancellationToken ct)
        => ExchangeAsync(_serializer.Serialize(request), retries, ct);

    private async Task<Reply> ExchangeAsync(byte[] request, int retries, CancellationToken ct)
    {
        MBusError? garbled = null;

        for (int attempt = 0; attempt <= retries; attempt++)
        {
            // A transport may report a timeout that raced the caller's cancellation; never retry past it.
            ct.ThrowIfCancellationRequested();
            await SendAsync(request, ct);

            ReadOnlyMemory<byte> bytes;
            try
            {
                bytes = await _transport.ReceiveFrameAsync(ct);
            }
            catch (TimeoutException)
            {
                continue;
            }
            // A transport that times out by cancelling a linked token; the caller's own cancellation still propagates.
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                continue;
            }

            var parsed = _parser.Parse(bytes.Span);
            if (parsed.IsSuccess)
                return new Reply(parsed.Value, null);

            garbled = parsed.Error;
        }

        return new Reply(null, garbled);
    }

    private async Task SendAsync(byte[] bytes, CancellationToken ct)
    {
        // Drop anything a slave sent after an earlier exchange gave up, so it is not read as this frame's reply.
        await _transport.DiscardInputAsync(ct);
        await _transport.SendFrameAsync(bytes, ct);
    }

    private async Task<BusLease> AcquireBusAsync(CancellationToken ct)
    {
        await _bus.WaitAsync(ct);
        return new BusLease(_bus);
    }

    // A SND_NKE sets both FCB images of the address; to 254 or 255 it reaches every slave (MBDOC48 5.5.2).
    private void ResetFcb(byte address)
    {
        if (address is MBusConstants.ADDRESS_BROADCAST_REPLY or MBusConstants.ADDRESS_BROADCAST_NOREPLY)
        {
            Array.Fill(_requestFcb, true);
            Array.Fill(_sendFcb, true);
            return;
        }

        _requestFcb[address] = true;
        _sendFcb[address] = true;
    }

    // 251 and 252 are reserved by EN 13757-2; no slave may answer them.
    private static void ThrowIfReserved(byte address)
    {
        if (address is > MBusConstants.ADDRESS_PRIMARY_MAX and < MBusConstants.ADDRESS_NETWORK_LAYER)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Primary addresses 251 and 252 are reserved");
    }

    private static void ThrowIfNoReply(byte address)
    {
        if (address == MBusConstants.ADDRESS_BROADCAST_NOREPLY)
            throw new ArgumentOutOfRangeException(nameof(address), address, "Broadcast 255 is never answered; nothing can be read from it");
    }

    private static MBusError Unexpected(string expected, MBusFrame got)
        => new(MBusConstants.ERROR_UNEXPECTED_FRAME, $"Expected {expected}, got {got.GetType().Name}");

    // An application error or alarm status without data bytes (L = 3) arrives as a control frame.
    private static byte? ReplyAddress(MBusFrame reply) => reply switch
    {
        LongFrame lf => lf.Address,
        ControlFrame cf => cf.Address,
        _ => null,
    };

    // The "error free link layer RSP_UD" that moves the FCB on, or the E5 that answers REQ_UD1 when no alarm is
    // pending. A reply from another address was not this slave's answer.
    private static bool IsAnswer(MBusFrame reply, byte address, bool ackIsEmpty)
        => ReplyAddress(reply) is { } from ? IsReplyFrom(from, address) : reply is AckFrame && ackIsEmpty;

    // A slave reached through the network layer (0xFD) or the test broadcast (0xFE) answers with its own primary address.
    private static bool IsReplyFrom(byte replyAddress, byte address)
        => address is MBusConstants.ADDRESS_NETWORK_LAYER or MBusConstants.ADDRESS_BROADCAST_REPLY
           || replyAddress == address;

    private static bool IsSameMeter(VariableDataPacket a, VariableDataPacket b)
        => a.IdentificationRaw == b.IdentificationRaw && a.Manufacturer == b.Manufacturer
           && a.Version == b.Version && a.DeviceType == b.DeviceType;

    private static ControlMask WithFcb(ControlMask control, bool fcb)
        => fcb ? control | ControlMask.FCB : control;

    private static ShortFrame ShortFrameFor(ControlMask control, byte address)
        => new(control, address, (byte)((byte)control + address));

    private static LongFrame BuildLongFrame(ControlMask control, ControlInformation ci, byte address, ReadOnlySpan<byte> data)
    {
        byte crc = (byte)((byte)control + address + (byte)ci);
        for (int i = 0; i < data.Length; i++)
            crc += data[i];
        return new LongFrame(control, ci, address, data.ToArray(), crc);
    }

    // Frame is null when nothing usable came back; Garbled then holds the last parse failure, if any reply arrived.
    private readonly record struct Reply(MBusFrame? Frame, MBusError? Garbled);

    private readonly struct BusLease(SemaphoreSlim bus) : IDisposable
    {
        public void Dispose() => bus.Release();
    }
}
