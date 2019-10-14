![record screenshot](MBusLogo240.jpg)

M-Bus (Meter Bus) project for communicating and parsing M-Bus over udp, tcp and serial. Implementation of protocol EN 13757-2 and EN 13757-3 (https://ec.europa.eu/eip/ageing/standards/ict-and-communication/data/en-13757_en). 

M-Bus (Meter-Bus) is a European standard (EN 13757-2 physical and link layer, EN 13757-3 application layer) for the remote reading of gas, water or electricity meters. M-Bus is also usable for other types of consumption meters. The M-Bus interface is made for communication on two wires, making it cost-effective. A radio variant of M-Bus (Wireless M-Bus) is also specified in EN 13757-4.

The M-Bus was developed to fill the need for a system for the networking and remote reading of utility meters, for example to measure the consumption of gas or water in the home. This bus fulfills the special requirements of remotely powered or battery-driven systems, including consumer utility meters. When interrogated, the meters deliver the data they have collected to a common master, such as a hand-held computer, connected at periodic intervals to read all utility meters of a building. An alternative method of collecting data centrally is to transmit meter readings via a modem.

Other applications for the M-Bus such as alarm systems, flexible illumination installations, heating control, etc. are suitable.

## Retrieving meter telemetry

```
var serialiser = new MeterbusFrameSerializer();
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.135"), 502);

// binding to the collector/gateway
var binding = new UdpBinding(endpoint, serializer);

// reqeust for telemetry on meter with address 0x0a
var response = await new MeterBusMaster(binding)
  .RequestData(0x0a, TimeSpan.FromSeconds(3));
```

## Low level control

```
var serialiser = new MeterbusFrameSerializer();
var endpoint = new IPEndPoint(IPAddress.Parse("192.168.1.135"), 502);

// binding to the collector/gateway
var binding = new UdpBinding(endpoint, serialiser);
binding.PacketReceived += (sender, e) => Debug.WriteLine("M-Bus packet received.");
    
// send a short frame/SND_NKE to the meter with address 0x0a
await binding.SendAsync(new ShortFrame((byte)ControlMask.SND_NKE, 0x0a));
```

## Deserialize M-Bus frame and payload

```
var packet = "68 1F 1F 68 08 02 72 78 56 34 12 24 40 01 07 55 00 00 00 03 13 15 31 00 DA 02 3B 13 01 8B 60 04 37 18 02 18 16"
  .HexToBytes()
  .ToFrame()   //EN13757_2
  .ToPacket(); // EN13757_3
```

## Changelog

### v1.0.2 (2019.10.13)
* serial communication capability

### v1.0.1 (2019.10.12)
* bug fixes

### v1.0.0 (2018.09.29)
* initial release

