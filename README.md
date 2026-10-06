![M-Bus Logo](https://raw.githubusercontent.com/sympthom/Valley.Net.Protocols.MeterBus/master/MBusLogo240.jpg)

# Valley.Net.Protocols.MeterBus

[![Build & Test](https://github.com/sympthom/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml/badge.svg)](https://github.com/sympthom/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/Valley.Net.Protocols.MeterBus.svg)](https://www.nuget.org/packages/Valley.Net.Protocols.MeterBus)

A .NET 10 library for wired M-Bus (Meter-Bus) over TCP, UDP and serial. It implements the EN 13757-2 link layer and the EN 13757-3 application layer. Every decode is checked against the libmbus reference output for 70 real meter telegrams.

## What is M-Bus?

M-Bus (Meter-Bus) is a European standard for reading utility meters remotely: heat, water, gas, electricity and heat cost allocators. It runs over a cheap two-wire bus, and in practice it is often reached through a TCP or UDP gateway. A wireless variant exists (EN 13757-4), but this library covers wired M-Bus.

Typical use cases include:

- Remote reading of utility meters in residential and commercial buildings
- Centralized data collection through gateways or hand-held readers
- Heating control and building automation

## Packages

| Package | Description |
|---------|-------------|
| `Valley.Net.Protocols.MeterBus` | Core: `MBusMaster`, `FrameParser`, `FrameSerializer`, `PacketMapper`, `VifLookupService`, DI and tracing |
| `Valley.Net.Protocols.MeterBus.Abstractions` | Interfaces, records, enums, `MBusDeframer`, `MBusException`. No dependencies. |
| `Valley.Net.Protocols.MeterBus.Transport.Tcp` | TCP gateway transport (hostname, IPv4, IPv6) |
| `Valley.Net.Protocols.MeterBus.Transport.Udp` | UDP gateway transport (hostname, IPv4, IPv6) |
| `Valley.Net.Protocols.MeterBus.Transport.Serial` | Serial / USB level-converter transport (`System.IO.Ports`) |

```bash
dotnet add package Valley.Net.Protocols.MeterBus
dotnet add package Valley.Net.Protocols.MeterBus.Transport.Tcp   # or .Udp / .Serial
```

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later. Version 4 replaces the 1.x packages, which targeted .NET Standard 2.0 and had a different API. Versions 2 and 3 were never published.

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│ IMBusMaster (MBusMaster)                                 │
│ bus lock · FCB per address · retries · multi-telegram    │
├──────────────────────────────┬───────────────────────────┤
│ IPacketMapper                │ IFrameParser /            │
│ LongFrame → MBusPacket       │ IFrameSerializer          │
│ (DIF/VIF/value decoding)     │ bytes ↔ MBusFrame         │
├──────────────────────────────┴───────────────────────────┤
│ IMBusTransport: Connect · Send · Receive · DiscardInput  │
│ MBusDeframer: validates and resyncs every frame          │
├──────────────────┬──────────────────┬────────────────────┤
│ TCP              │ UDP              │ Serial             │
└──────────────────┴──────────────────┴────────────────────┘
```

## Usage

### Read a meter

```csharp
await using var transport = new TcpMBusTransport("mbus-gateway.local", 10001);
await transport.ConnectAsync();

await using var master = new MBusMaster(
    transport,
    new FrameParser(),
    new FrameSerializer(),
    new PacketMapper(new VifLookupService()));

var packet = await master.RequestDataAsync(0x0A);
if (packet is VariableDataPacket vdp)
{
    Console.WriteLine($"Meter {vdp.IdentificationNo} ({vdp.DeviceType}), {vdp.Records.Length} records");
    foreach (var record in vdp.Records)
    {
        var unit = record.Units[0];
        Console.WriteLine($"  {unit.Units} {record.Function} storage {record.StorageNumber}: {record.Value} (10^{record.Magnitude} {unit.Unit})");
    }
}
```

The master never disposes the transport you give it. Whoever creates the transport disposes it.

### Meters with more than one telegram

Some meters split their data over several telegrams and mark each one with "more records follow" (DIF 0x1F). `RequestAllTelegramsAsync` keeps asking, toggling the FCB each time, until the last telegram arrives or `MaxTelegrams` is reached:

```csharp
IReadOnlyList<MBusPacket> telegrams = await master.RequestAllTelegramsAsync(0x0A);
```

### Scan the bus

```csharp
// Primary addresses 0-250 (0 = unconfigured). 251/252 are reserved, 253 is the secondary-address slot,
// 254 is test/broadcast with reply and 255 is broadcast without reply.
var addresses = Enumerable.Range(0, 251).Select(i => (byte)i);
await foreach (var meter in master.ScanAsync(addresses))
{
    Console.WriteLine(meter.Status switch
    {
        ScanStatus.Found => $"{meter.Address}: {meter.Packet}",
        ScanStatus.Collision => $"{meter.Address}: collision ({meter.Error?.Message})",
        _ => $"{meter.Address}: error {meter.Error?.Code}",
    });
}
```

A collision or garbled reply on one address does not stop the scan. Addresses that do not answer at all are left out.

### Secondary addressing

```csharp
await master.SelectSlaveAsync(new SecondaryAddress(12345678, ManufacturerId: 0x2C2D, Version: 1, DeviceType.Heat));
var packet = await master.RequestDataAsync(MBusConstants.ADDRESS_NETWORK_LAYER);   // 0xFD
```

Some meters have identification numbers with non-decimal digits. For those, use the overload that takes the raw ID (`VariableDataPacket.IdentificationRaw`).

### Dependency injection

```csharp
services.AddMBusCore(options =>
{
    options.Retries = 3;        // extra attempts after a timeout or garbled reply
    options.MaxTelegrams = 16;  // cap for RequestAllTelegramsAsync
});
services.AddSingleton<IMBusTransport>(_ => new TcpMBusTransport("mbus-gateway.local", 10001));

// Connect the transport before the master uses it. IMBusMaster is a singleton, and its bus lock
// serialises every caller on the half-duplex bus.
await provider.GetRequiredService<IMBusTransport>().ConnectAsync();
var master = provider.GetRequiredService<IMBusMaster>();
```

### Serial (USB level converter)

```csharp
await using var transport = new SerialMBusTransport("/dev/ttyUSB0", new SerialMBusTransportOptions
{
    BaudRate = 2400,          // 8E1 is the default framing
    EchoSuppression = true,   // for converters that echo the request back
});
```

The response, inter-character and whole-frame timeouts are worked out from the baud rate per EN 13757-2. A full-size frame still fits at 300 baud. You can override each timeout in the options.

### Tracing raw frames

```csharp
var traced = new TracingMBusTransport(transport, loggerFactory.CreateLogger("MBus"));
```

This logs every frame sent and received as hex, with timings, at Debug/Trace level.

### Parse a raw frame

```csharp
// A GWF water meter telegram from DataExamples/test-frames
var frame = new FrameParser().Parse("68 1B 1B 68 08 01 72 07 20 18 00 E6 1E 35 07 4C 00 00 00 0C 78 07 20 18 00 0C 16 69 02 00 00 96 16".HexToBytes());
if (frame.IsSuccess)
{
    var packet = new PacketMapper(new VifLookupService()).MapToPacket(frame.Value!);
    // packet.IsSuccess, or packet.Error.Code such as PREMATURE_END or ENCRYPTED
}
```

## Decoded values

`DataRecord.Value` has a fixed CLR type for each kind of data:

| Data | CLR type |
|------|----------|
| 8/16/24/32/48/64-bit integer | `sbyte` / `short` / `int` / `int` / `long` / `long` (all signed) |
| BCD (2-12 digits, LVAR 0xC0-0xD9) | `long`. An `F` high nibble makes the value negative. |
| 32-bit real | `float` |
| Date (type G) | `DateOnly` |
| Date and time (types F and I) | `DateTime` |
| Time (type J) | `TimeOnly` |
| LVAR text | `string` (put back into reading order) |
| LVAR binary | `byte[]` |

When the bytes cannot be decoded, `Value` is `null` and `DataRecord.ValueError` says why: `INVALID_BCD` or `INVALID_DATE`.

Scaling is kept separate from the value. `Magnitude` is the power of ten and `Offset` is the additive correction. For durations, the time unit (s/min/h/d) is in `UnitInfo.Unit`. Bytes after DIF 0x0F/0x1F are in `VariableDataPacket.ManufacturerData`, and `MoreRecordsFollow` is set when the telegram ends with 0x1F.

## Errors

| Situation | What you get |
|-----------|--------------|
| A malformed frame or record given to the parser or mapper | `MBusParseResult` with `Error.Code`: `PREMATURE_END`, `TOO_MANY_DIFE`, `TOO_MANY_VIFE`, `RESERVED_DIF`, `RESERVED_LVAR`, `ENCRYPTED`, `TRAILING_DATA`, … |
| No reply within the transport timeout | `TimeoutException` (after `Retries` further attempts) |
| A reply that is wrong (wrong address, unexpected frame type, cannot be decoded) | `MBusException` with `Error.Code` and `Error.Message` |
| The meter reports an application error or alarm (CI 0x70/0x71) | `ApplicationErrorPacket` / `AlarmStatusPacket`, returned as normal results |
| You cancel | `OperationCanceledException` |

## Conformance

`GoldenLibmbusTests` decodes every telegram in `DataExamples/test-frames` and compares it field by field with the libmbus XML reference: header, function, storage, tariff, subunit, unit, scale and value. Today 59 of 70 frames and 834 of 852 records match exactly. The remaining 18 differences are deliberate. They are listed with reasons in `tests/.../Golden/known-differences.txt`; most are invalid dates or BCD values, which libmbus turns into made-up numbers. The test fails both on a new difference and on a listed difference that no longer occurs.

## Building from source

```bash
dotnet build Valley.Net.Protocols.MeterBus.sln --configuration Release
dotnet test --solution Valley.Net.Protocols.MeterBus.sln --configuration Release
```

To release, push a tag `vX.Y.Z`. The publish workflow takes the package version from the tag.

## Changelog

### v4.0.0

This is the first release since 1.0.3. It contains the v3 rewrite, which was never published, and a full review and hardening pass.

**Decoding**
- BCD values of every width are decoded in full; 8-digit values used to keep only 4 digits. Values with an `F` sign nibble are negative, and invalid digits give `INVALID_BCD`.
- Signed 8/24/48-bit integers. Date/time types G/F/I/J, including the IV bit and the hundred-year bits. LVAR text, BCD and binary.
- Plain-text VIF (0x7C/0xFC). Manufacturer data after DIF 0x0F/0x1F. VIFE table selection after FB/FD/FC, with opaque VIFEs after 0xFF.
- VIF/VIFE tables follow EN 13757-3:2013/OMS: reserved codes have magnitude 0 and time units are no longer treated as powers of ten. `DeviceType` names follow the standard.
- Malformed records fail with a specific error code instead of returning partial data. Encrypted payloads are detected. CI 0x70/0x71 map to `ApplicationErrorPacket`/`AlarmStatusPacket`.
- `IdentificationNo` uses libmbus BCD arithmetic, and `IdentificationRaw` keeps the wire bytes. `FixedDataPacket` has `Status`, signed counters and `FixedDataMedium`.

**Master**
- A bus lock, an FCB per address (retries repeat the same FCB), `RequestAllTelegramsAsync`, and retries configured through `MBusMasterOptions`.
- The ACK is read after commands. The reply address is checked. Stale input is discarded before every request.
- `ScanAsync` continues past collisions and reports `ScanStatus`. 251 and 252 are rejected, and 255 is send-only.
- `MBusException` replaces `InvalidOperationException`. Timeouts throw `TimeoutException`, and cancellation always propagates.
- `SelectSlaveAsync` always addresses 0xFD and has a raw-ID overload. `TracingMBusTransport` logs frames. `AddMBusCore(options)` registers a singleton master.

**Transports**
- `MBusDeframer` validates every frame (checksum, L-field, stop byte) and resyncs past noise. A single noise byte used to stall TCP and Serial for good.
- Reconnecting no longer leaks the old connection. Transports have `IsConnected` and `IDisposable`, and dispose is thread-safe. TCP and UDP accept hostnames, IPv4 and IPv6. UDP deframes across datagrams.
- Serial: `SerialMBusTransportOptions`, timeouts worked out from the baud rate, echo suppression, and timeouts and cancellation that also work on Windows.

**Quality**
- 1160 unit tests, including the libmbus golden test, transport loopback tests, master tests against a fake transport, and fuzzing. CI runs on every push. Packages ship XML docs, SourceLink and symbols.

**Breaking changes from 1.x/3.x** include: `DataRecord.Value` types (see above), `MBusException`, `SelectSlaveAsync(SecondaryAddress)` without an address, `SerialMBusTransport` constructors, renamed `DeviceType`/`VariableDataQuantityUnit` members, `ControlInformation` MSB values, `FixedDataPacket` counters (`long?`), and the master no longer disposing the transport.

### v3.0.0 and v2.0.0 (never published)

Rewrite to .NET 10 with a multi-project layout, records, `MBusParseResult<T>`, span-based parsing, async `MBusMaster` and the TCP/UDP/Serial transports. All of it is included in 4.0.0.

### v1.0.3 (2019.12.28)

- Extension methods for deserializing frames and packets
- Fixed Actuality Duration and VIF naming

### v1.0.2 (2019.10.13)

- Serial communication capability

### v1.0.0 (2018.09.29)

- Initial release

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
