![M-Bus Logo](MBusLogo240.svg)

# Valley.Net.Protocols.MeterBus

[![Build & Test](https://github.com/valleynet/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml/badge.svg)](https://github.com/valleynet/Valley.Net.Protocols.MeterBus/actions/workflows/build.yml)

A .NET 10 library for M-Bus (Meter Bus) communication and frame parsing over UDP, TCP, and serial. Implements the EN 13757-2 (physical and link layer) and EN 13757-3 (application layer) standards.

## What is M-Bus?

M-Bus (Meter-Bus) is a European standard for the remote reading of utility meters such as gas, water, and electricity. It is designed for cost-effective two-wire communication and supports both wired (EN 13757-2/3) and wireless (EN 13757-4) variants.

Typical use cases include:

- Remote reading of utility meters in residential and commercial buildings
- Centralized data collection via gateways or hand-held readers
- Alarm systems, heating control, and building automation

## Architecture

The library is organized into two protocol layers mirroring the EN 13757 specification:

```
┌──────────────────────────────────────────────────┐
│              Application Layer (EN13757_3)        │
│  Packet, VariableDataPacket, FixedDataPacket,    │
│  AlarmStatusPacket, ApplicationErrorPacket        │
├──────────────────────────────────────────────────┤
│         Physical / Link Layer (EN13757_2)         │
│  Frame, ShortFrame, LongFrame, ControlFrame,     │
│  VariableDataLongFrame, FixedDataLongFrame,      │
│  DIF, DIFE, VIF, VIFE, VIFE_FB, VIFE_FD         │
├──────────────────────────────────────────────────┤
│              Transport Bindings                   │
│       UDP, TCP, Serial (via Valley.Net.Bindings) │
└──────────────────────────────────────────────────┘
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Installation

```bash
dotnet add package Valley.Net.Protocols.MeterBus
```

## Usage

### Retrieving meter telemetry

```csharp
var serializer = new MeterbusFrameSerializer();
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.135"), 502);

// Bind to the collector/gateway
var binding = new UdpBinding(endpoint, serializer);

// Request telemetry from meter at address 0x0a
var response = await new MBusMaster(binding)
    .RequestData(0x0a, TimeSpan.FromSeconds(3));
```

### Low-level control

```csharp
var serializer = new MeterbusFrameSerializer();
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.135"), 502);

// Bind to the collector/gateway
var binding = new UdpBinding(endpoint, serializer);
binding.PacketReceived += (sender, e) => Debug.WriteLine("M-Bus packet received.");

// Send a short frame / SND_NKE to the meter at address 0x0a
await binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));
```

### Deserialize an M-Bus frame and payload

```csharp
var packet = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
    .HexToBytes()
    .ToFrame()   // EN 13757-2: physical/link layer
    .ToPacket(); // EN 13757-3: application layer
```

## Building from source

```bash
dotnet restore Valley.Net.Protocols.MeterBus.sln
dotnet build Valley.Net.Protocols.MeterBus.sln --configuration Release
dotnet test Valley.Net.Protocols.MeterBus.sln --configuration Release
```

## Changelog

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
