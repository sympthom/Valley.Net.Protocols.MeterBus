using System.Runtime.CompilerServices;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Modern M-Bus master implementation using async/await, CancellationToken, and DI.
/// Replaces the old event-driven MBusMaster class.
/// </summary>
public sealed class MBusMaster : IMBusMaster
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
        catch (OperationCanceledException)
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

        byte[] data = [MBusConstants.SET_ADDRESS_DIF, MBusConstants.SET_ADDRESS_VIF, newAddress];
        var frame = BuildLongFrame(ControlMask.SND_UD, ControlInformation.DATA_SEND, address, data);
        await SendFrameAsync(frame, ct);
    }

    public async Task InitializeAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = new ShortFrame(ControlMask.SND_NKE, address, ComputeShortCrc(ControlMask.SND_NKE, address));
        await SendFrameAsync(frame, ct);
    }

    public async Task ResetApplicationAsync(byte address, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var ci = ControlInformation.APPLICATION_RESET;
        byte control = (byte)ControlMask.SND_UD;
        byte crc = (byte)(control + address + (byte)ci);
        var frame = new ControlFrame(ControlMask.SND_UD, ci, address, crc);
        await SendFrameAsync(frame, ct);
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
            catch (OperationCanceledException)
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
            catch (OperationCanceledException)
            {
                dataFailed = true;
            }

            if (dataFailed)
            {
                yield return new MeterInfo(address, null);
            }
            else if (dataResponse is LongFrame lf)
            {
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
        await SendFrameAsync(frame, ct);

        var response = await ReceiveFrameAsync(ct);
        if (response is not AckFrame)
            throw new InvalidOperationException("Expected ACK response");
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
        await SendFrameAsync(frame, ct);

        var response = await ReceiveFrameAsync(ct);
        if (response is not AckFrame)
            throw new InvalidOperationException("Expected ACK response for slave selection");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            await _transport.DisposeAsync();
        }
    }

    // ---- Private helpers ----

    private async Task<MBusPacket> RequestDataInternalAsync(byte address, ControlMask controlMask, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var frame = new ShortFrame(controlMask, address, ComputeShortCrc(controlMask, address));
        await SendFrameAsync(frame, ct);

        var response = await ReceiveFrameAsync(ct);
        var result = _mapper.MapToPacket(response);

        if (!result.IsSuccess)
            throw new InvalidOperationException($"Failed to map response: {result.Error?.Message}");

        return result.Value!;
    }

    private async Task SendFrameAsync(MBusFrame frame, CancellationToken ct)
    {
        var bytes = _serializer.Serialize(frame);
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
