using System.IO.Ports;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Line settings and timing for <see cref="SerialMBusTransport"/>. The defaults are the
/// EN 13757-2 character format (8 data bits, even parity, 1 stop bit) at 2400 baud, with
/// timeouts derived from the baud rate.
/// </summary>
public sealed record SerialMBusTransportOptions
{
    public int BaudRate { get; init; } = 2400;
    public Parity Parity { get; init; } = Parity.Even;
    public int DataBits { get; init; } = 8;
    public StopBits StopBits { get; init; } = StopBits.One;

    /// <summary>
    /// Asserts DTR once the port is open. Some USB M-Bus masters power their level converter from it.
    /// </summary>
    public bool DtrEnable { get; init; }

    /// <summary>
    /// Asserts RTS once the port is open. Some USB M-Bus masters power their level converter from it.
    /// </summary>
    public bool RtsEnable { get; init; }

    /// <summary>
    /// How long to wait for the first byte of a reply. <see langword="null"/> derives it from the
    /// baud rate: the slave answers within 330 bit times + 50 ms (EN 13757-2, M-Bus documentation
    /// 5.4), plus 100 ms for USB adapter latency and scheduling. That is 288 ms at 2400 baud and
    /// 1.25 s at 300 baud, close to libmbus's per-baud read timeouts (0.3 s and 1.3 s).
    /// </summary>
    public TimeSpan? ResponseTimeout { get; init; }

    /// <summary>
    /// The longest gap allowed between the bytes of a frame once it has started; after it the
    /// partial frame is dropped. <see langword="null"/> derives it from the baud rate: the 33 bit
    /// times of line idle that end an exchange, plus 100 ms for USB adapter latency and scheduling.
    /// </summary>
    public TimeSpan? InterCharacterTimeout { get; init; }

    /// <summary>
    /// How long a send may take. <see langword="null"/> derives it from the baud rate: the time to
    /// transmit a maximum-size frame, plus 100 ms.
    /// </summary>
    public TimeSpan? WriteTimeout { get; init; }

    /// <summary>
    /// Drops the copy of each request that some level converters echo back, before looking for the
    /// reply. Off by default: a standard M-Bus level converter does not echo.
    /// </summary>
    public bool EchoSuppression { get; init; }
}
