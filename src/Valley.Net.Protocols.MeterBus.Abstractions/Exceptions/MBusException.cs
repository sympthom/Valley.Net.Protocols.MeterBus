namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// A slave answered, but not with something the master could use: a reply that does not parse, a frame of
/// the wrong kind, a reply from another address, or user data that cannot be mapped.
/// </summary>
/// <remarks>
/// <see cref="Error"/> keeps the code of the layer that failed: the parser's or mapper's own code, or one of
/// the <c>MBusConstants.ERROR_*</c> codes for checks the master makes. A missing reply is a
/// <see cref="TimeoutException"/>, not this.
/// </remarks>
public sealed class MBusException : Exception
{
    public MBusException(byte address, MBusError error)
        : base($"M-Bus address {address}: {error?.Message}")
    {
        ArgumentNullException.ThrowIfNull(error);
        Address = address;
        Error = error;
    }

    /// <summary>The address the request went to (0xFD/0xFE included), not the address in the reply.</summary>
    public byte Address { get; }

    public MBusError Error { get; }
}
