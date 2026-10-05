![M-Bus Logo](MBusLogo240.svg)

# Valley.Net.Protocols.MeterBus

[![Build & Test](https://github.com/sympthom/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml/badge.svg)](https://github.com/sympthom/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml)

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

v3 is not yet published to NuGet. nuget.org has only `Valley.Net.Protocols.MeterBus` up to 1.0.3 (.NET Standard 2.0), which has the old v1 API and none of the types used below, and no Abstractions or Transport packages. Until v3 is released, build from source and reference the projects directly:

```bash
git clone https://github.com/sympthom/Valley.Net.Protocols.MeterBus.git
dotnet add reference Valley.Net.Protocols.MeterBus/src/Valley.Net.Protocols.MeterBus/Valley.Net.Protocols.MeterBus.csproj
dotnet add reference Valley.Net.Protocols.MeterBus/src/Valley.Net.Protocols.MeterBus.Transport.Tcp/Valley.Net.Protocols.MeterBus.Transport.Tcp.csproj  # or .Udp / .Serial
```

## Usage

### With Dependency Injection

```csharp
services.AddMBusCore();
services.AddSingleton<IMBusTransport>(sp =>
    new TcpMBusTransport("192.168.1.135", 10001));

// The transport must be connected before the master uses it. The master is a singleton
// and never disposes the transport; the container (or whoever created it) does.
await provider.GetRequiredService<IMBusTransport>().ConnectAsync();
var master = provider.GetRequiredService<IMBusMaster>();
```

### Retrieving meter telemetry

```csharp
await using var transport = new TcpMBusTransport("192.168.1.135", 10001);
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
// Primary addresses 0-250 (0 = unconfigured; 251-255 are reserved, network-layer or broadcast)
var addresses = Enumerable.Range(0, 251).Select(i => (byte)i);
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
- **Span-based parsing** -- `FrameParser` validates the `ReadOnlySpan<byte>` input in place; each frame allocates only its result record, plus a copy of the payload for long frames

## Building from source

```bash
dotnet restore Valley.Net.Protocols.MeterBus.sln
dotnet build Valley.Net.Protocols.MeterBus.sln --configuration Release
dotnet test --solution Valley.Net.Protocols.MeterBus.sln --configuration Release
```

## Changelog

### v3.0.0 (unreleased)

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
- MSTest 4.x unit tests, with `[DynamicData]` over the 73 meter frames in `DataExamples/test-frames` (70 of them with libmbus reference decodes)
- Separate integration test project

### v2.0.0 (not published to NuGet)

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

### v1.0.3 (2019.12.28)

- Extension methods for deserializing frames and packets
- Fixed Actuality Duration and VIF naming

### v1.0.2 (2019.10.13)

- Serial communication capability

### v1.0.1 (2019.10.12, not published to NuGet)

- Bug fixes

### v1.0.0 (2018.09.29)

- Initial release

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
