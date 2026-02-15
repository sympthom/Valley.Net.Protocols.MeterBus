![M-Bus Logo](MBusLogo240.svg)

# Valley.Net.Protocols.MeterBus

[![Build & Test](https://github.com/valleynet/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml/badge.svg)](https://github.com/valleynet/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml)

A modern .NET 10 library for M-Bus (Meter Bus) communication and frame parsing over TCP, UDP, and serial. Implements the EN 13757-2 (physical and link layer) and EN 13757-3 (application layer) standards.

## What is M-Bus?

M-Bus (Meter-Bus) is a European standard for the remote reading of utility meters such as gas, water, and electricity. It is designed for cost-effective two-wire communication and supports both wired (EN 13757-2/3) and wireless (EN 13757-4) variants.

Typical use cases include:

- Remote reading of utility meters in residential and commercial buildings
- Centralized data collection via gateways or hand-held readers
- Alarm systems, heating control, and building automation

## Architecture

```
┌──────────────────────────────────────────────────────────┐
│                    IMBusMaster                            │
│  PingAsync, RequestDataAsync, ScanAsync, etc.            │
├──────────────────────────────────────────────────────────┤
│  IPacketMapper          │  IFrameParser / IFrameSerializer│
│  LongFrame → Packet     │  bytes ↔ MBusFrame records      │
├──────────────────────────────────────────────────────────┤
│                    IMBusTransport                         │
│  ConnectAsync, SendFrameAsync, ReceiveFrameAsync         │
├──────────┬──────────────┬────────────────────────────────┤
│  TCP     │  UDP         │  Serial                        │
│  Pipes   │  Socket      │  System.IO.Ports + PipeReader  │
└──────────┴──────────────┴────────────────────────────────┘
```

### Project Layout

| Project | Description |
|---------|-------------|
| `Valley.Net.Protocols.MeterBus.Abstractions` | Interfaces, record types, enums -- zero dependencies |
| `Valley.Net.Protocols.MeterBus` | Core implementation: FrameParser, FrameSerializer, PacketMapper, VifLookupService, MBusMaster |
| `Valley.Net.Protocols.MeterBus.Transport.Tcp` | TCP transport using `System.IO.Pipelines` |
| `Valley.Net.Protocols.MeterBus.Transport.Udp` | UDP transport using `Socket` |
| `Valley.Net.Protocols.MeterBus.Transport.Serial` | Serial transport wrapping `System.IO.Ports` with `PipeReader` |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Installation

```bash
dotnet add package Valley.Net.Protocols.MeterBus
dotnet add package Valley.Net.Protocols.MeterBus.Transport.Tcp  # or .Udp / .Serial
```

## Usage

### With Dependency Injection

```csharp
services.AddMBusCore();
services.AddSingleton<IMBusTransport>(sp =>
    new TcpMBusTransport("192.168.1.135", 502));
```

### Retrieving meter telemetry

```csharp
await using var transport = new TcpMBusTransport("192.168.1.135", 502);
await transport.ConnectAsync();

await using var master = new MBusMaster(
    transport,
    new FrameParser(),
    new FrameSerializer(),
    new PacketMapper(new VifLookupService()));

var packet = await master.RequestDataAsync(0x0a);
if (packet is VariableDataPacket vdp)
{
    Console.WriteLine($"Device: {vdp.DeviceType}, Records: {vdp.Records.Length}");
    foreach (var record in vdp.Records)
        Console.WriteLine($"  {record.Units[0].Quantity}: {record.Value}");
}
```

### Scanning for devices

```csharp
var addresses = Enumerable.Range(0, 250).Select(i => (byte)i);
await foreach (var meter in master.ScanAsync(addresses))
{
    Console.WriteLine($"Found meter at address {meter.Address}");
}
```

### Parse a raw M-Bus frame

```csharp
var parser = new FrameParser();
var mapper = new PacketMapper(new VifLookupService());

var bytes = "68 31 31 68 08 01 72 45 58 57 03 B4 05 ..."
    .HexToBytes();

var frame = parser.Parse(bytes);
if (frame.IsSuccess)
{
    var packet = mapper.MapToPacket(frame.Value!);
    // Use packet...
}
```

## Design Principles

- **Immutable data** -- All frames and packets are C# `record` types
- **Explicit errors** -- `MBusParseResult<T>` instead of exceptions for parsing
- **Async-first** -- `CancellationToken` everywhere, `IAsyncEnumerable` for scanning
- **Zero external dependencies** -- Abstractions project has no NuGet dependencies
- **Dependency Injection** -- All services are injectable via `IServiceCollection.AddMBusCore()`
- **Span-based parsing** -- `ReadOnlySpan<byte>` for zero-allocation frame parsing

## Building from source

```bash
dotnet restore Valley.Net.Protocols.MeterBus.sln
dotnet build Valley.Net.Protocols.MeterBus.sln --configuration Release
dotnet test Valley.Net.Protocols.MeterBus.sln --configuration Release
```

## Changelog

### v3.0.0

- **Full architectural rewrite** -- Multi-project solution with clean separation of concerns
- Replaced `Valley.Net.Bindings` dependency with native `IMBusTransport` abstraction
- Immutable `record` types for all frames (`MBusFrame`) and packets (`MBusPacket`)
- `MBusParseResult<T>` result type for explicit success/failure instead of exceptions
- `ReadOnlySpan<byte>`-based `FrameParser` replacing `BinaryReader`-based `MeterbusFrameSerializer`
- Consolidated VIF/VIFE/VIFE_FB/VIFE_FD into single `VifLookupService` with `FrozenDictionary`
- Async-first `MBusMaster` with `CancellationToken` and `IAsyncEnumerable<MeterInfo>` scanning
- TCP transport using `System.IO.Pipelines` for frame boundary detection
- UDP transport using raw `Socket`
- Serial transport wrapping `System.IO.Ports` with `PipeReader`
- `IServiceCollection.AddMBusCore()` for DI registration
- 168 unit tests using MSTest 4.x with `[DynamicData]` against 80+ real meter hex files
- Separate integration test project

### v2.0.0

- Upgraded to .NET 10 (from .NET Standard 2.0 / .NET Framework 4.6.1)
- Added GitHub Actions CI/CD workflows (build, test, NuGet publish)
- Fixed critical bug in `SelectSlave` (InvalidCastException at runtime)
- Fixed event handler memory leak in `MBusMaster`
- Implemented `SelectSlave` secondary address padding logic
- Removed ~800 lines of dead/commented-out code
- Extracted `IValueInformationField` interface for VIF/VIFE types
- Cached VIF/VIFE dictionary lookups for improved performance
- Extracted `MBusMaster` communication pattern into reusable helper
- Refactored VIFE if/else chain to table-driven approach
- Extracted value parsing into dedicated `ValueParser` class
- Consolidated magic numbers into `Constants.cs`
- Consolidated duplicate `LengthsInBitsTable`
- General code cleanup and modernization

### v1.0.2 (2019.10.13)

- Serial communication capability

### v1.0.1 (2019.10.12)

- Bug fixes

### v1.0.0 (2018.09.29)

- Initial release

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
