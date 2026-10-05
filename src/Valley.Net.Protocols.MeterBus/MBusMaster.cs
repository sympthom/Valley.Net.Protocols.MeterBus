using System.Runtime.CompilerServices;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Modern M-Bus master implementation using async/await, CancellationToken, and DI.
/// Replaces the old event-driven MBusMaster class.
/// </summary>
/// <remarks>
/// The master does not own the transport: disposing the master leaves the transport open for
/// whoever created it.
/// </remarks>
public sealed class MBusMaster : IMBusMaster, IDisposable
{
    private readonly IMBusTransport _transport;
    private readonly IFrameParser _parser;
    private readonly IFrameSerializer _serializer;
    private readonly IPacketMapper _mapper;
    private bool _disposed;

    public MBusMaster(
        IMBusTransport transport,
        IFrameParser parser,
        IFrameSerializer serializer,
        IPacketMapper mapper)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<bool> PingAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = new ShortFrame(ControlMask.SND_NKE, address, ComputeShortCrc(ControlMask.SND_NKE, address));
        await SendFrameAsync(frame, ct);

        try
        {
            var response = await ReceiveFrameAsync(ct);
            return response is AckFrame;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    public async Task<MBusPacket> RequestDataAsync(byte address, CancellationToken ct = default)
    {
        return await RequestDataInternalAsync(address, ControlMask.REQ_UD2, ct);
    }

    public async Task<MBusPacket> RequestAlarmAsync(byte address, CancellationToken ct = default)
    {
        return await RequestDataInternalAsync(address, ControlMask.REQ_UD1, ct);
    }

    public async Task SetAddressAsync(byte address, byte newAddress, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // 251-255 are reserved or broadcast; a meter given one of them can no longer be reached by primary address.
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newAddress, MBusConstants.ADDRESS_PRIMARY_MAX);

        byte[] data = [MBusConstants.SET_ADDRESS_DIF, MBusConstants.SET_ADDRESS_VIF, newAddress];
        var frame = BuildLongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, address, data);
        await SendExpectAckAsync(frame, address, ct);
    }

    public async Task InitializeAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = new ShortFrame(ControlMask.SND_NKE, address, ComputeShortCrc(ControlMask.SND_NKE, address));
        await SendExpectAckAsync(frame, address, ct);
    }

    public async Task ResetApplicationAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var ci = ControlInformation.APPLICATION_RESET;
        byte control = (byte)ControlMask.SND_UD;
        byte crc = (byte)(control + address + (byte)ci);
        var frame = new ControlFrame(ControlMask.SND_UD, ci, address, crc);
        await SendExpectAckAsync(frame, address, ct);
    }

    public async IAsyncEnumerable<MeterInfo> ScanAsync(
        IEnumerable<byte> addresses,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var address in addresses)
        {
            ct.ThrowIfCancellationRequested();

            // Send SND_NKE
            var pingFrame = new ShortFrame(ControlMask.SND_NKE, address, ComputeShortCrc(ControlMask.SND_NKE, address));
            await SendFrameAsync(pingFrame, ct);

            MBusFrame? response;
            try
            {
                response = await ReceiveFrameAsync(ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                continue;
            }

            if (response is not AckFrame)
                continue;

            // Request data
            var dataFrame = new ShortFrame(ControlMask.REQ_UD2, address, ComputeShortCrc(ControlMask.REQ_UD2, address));
            await SendFrameAsync(dataFrame, ct);

            MBusFrame? dataResponse = null;
            bool dataFailed = false;
            try
            {
                dataResponse = await ReceiveFrameAsync(ct);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                dataFailed = true;
            }

            if (dataFailed)
            {
                yield return new MeterInfo(address, null);
            }
            else if (dataResponse is LongFrame lf)
            {
                // Another meter's reply (a late one, or a collision) says nothing about this address.
                if (!IsReplyFrom(lf, address))
                    continue;

                var result = _mapper.MapToPacket(lf);
                yield return new MeterInfo(address, result.IsSuccess ? result.Value : null);
            }
            else
            {
                yield return new MeterInfo(address, null);
            }
        }
    }

    public async Task SendDataAsync(byte address, ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = BuildLongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, address, data.Span);
        await SendExpectAckAsync(frame, address, ct);
    }

    public async Task SelectSlaveAsync(byte address, SecondaryAddress secondary, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Encode secondary address: 4 bytes ID (BCD) + 2 bytes manufacturer + 1 byte version + 1 byte device type
        var data = new byte[8];
        var id = secondary.IdentificationNo;
        data[0] = (byte)(((id / 1) % 10) | (((id / 10) % 10) << 4));
        data[1] = (byte)(((id / 100) % 10) | (((id / 1000) % 10) << 4));
        data[2] = (byte)(((id / 10000) % 10) | (((id / 100000) % 10) << 4));
        data[3] = (byte)(((id / 1000000) % 10) | (((id / 10000000) % 10) << 4));
        data[4] = (byte)(secondary.ManufacturerId & 0xFF);
        data[5] = (byte)((secondary.ManufacturerId >> 8) & 0xFF);
        data[6] = secondary.Version;
        data[7] = (byte)secondary.DeviceType;

        var frame = BuildLongFrame(ControlMask.SND_UD, ControlInformation.SELECT_SLAVE, address, data);
        await SendExpectAckAsync(frame, address, ct);
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public void Dispose() => _disposed = true;

    // ---- Private helpers ----

    private async Task<MBusPacket> RequestDataInternalAsync(byte address, ControlMask controlMask, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = new ShortFrame(controlMask, address, ComputeShortCrc(controlMask, address));
        await SendFrameAsync(frame, ct);

        MBusFrame response;
        try
        {
            response = await ReceiveFrameAsync(ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"No reply from M-Bus address {address}");
        }

        // An E5 is never a valid answer to REQ_UD1/REQ_UD2; accepting it would shift every later reply by one.
        if (response is not LongFrame lf)
            throw new InvalidOperationException($"Expected RSP_UD from M-Bus address {address}, got {response.GetType().Name}");

        if (!IsReplyFrom(lf, address))
            throw new InvalidOperationException($"Reply from M-Bus address {lf.Address} does not match requested address {address}");

        var result = _mapper.MapToPacket(lf);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to map response: {result.Error?.Message}");

        return result.Value!;
    }

    /// <summary>
    /// Sends an SND_NKE or SND_UD and consumes the slave's E5, so it cannot be taken as the reply
    /// to the next request. Broadcast 0xFF is never answered, so nothing is read for it.
    /// </summary>
    private async Task SendExpectAckAsync(MBusFrame frame, byte address, CancellationToken ct)
    {
        await SendFrameAsync(frame, ct);

        if (address == MBusConstants.ADDRESS_BROADCAST_NOREPLY)
            return;

        MBusFrame response;
        try
        {
            response = await ReceiveFrameAsync(ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"No ACK from M-Bus address {address}");
        }

        if (response is not AckFrame)
            throw new InvalidOperationException($"Expected ACK from M-Bus address {address}, got {response.GetType().Name}");
    }

    private async Task SendFrameAsync(MBusFrame frame, CancellationToken ct)
    {
        var bytes = _serializer.Serialize(frame);

        // Drop anything a slave sent after an earlier exchange gave up, so it is not read as this frame's reply.
        await _transport.DiscardInputAsync(ct);
        await _transport.SendFrameAsync(bytes, ct);
    }

    private async Task<MBusFrame> ReceiveFrameAsync(CancellationToken ct)
    {
        var data = await _transport.ReceiveFrameAsync(ct);
        var result = _parser.Parse(data.Span);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to parse response: {result.Error?.Message}");

        return result.Value!;
    }

    // A slave reached through the network layer (0xFD) or the test broadcast (0xFE) answers with its own primary address.
    private static bool IsReplyFrom(LongFrame reply, byte address)
        => address is MBusConstants.ADDRESS_NETWORK_LAYER or MBusConstants.ADDRESS_BROADCAST_REPLY
           || reply.Address == address;

    private static byte ComputeShortCrc(ControlMask control, byte address)
        => (byte)((byte)control + address);

    private static LongFrame BuildLongFrame(ControlMask control, ControlInformation ci, byte address, ReadOnlySpan<byte> data)
    {
        byte crc = (byte)((byte)control + address + (byte)ci);
        for (int i = 0; i < data.Length; i++)
            crc += data[i];
        return new LongFrame(control, ci, address, data.ToArray(), crc);
    }
}
