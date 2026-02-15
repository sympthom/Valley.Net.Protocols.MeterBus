using Microsoft.Extensions.DependencyInjection;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Extension methods for registering M-Bus services with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core M-Bus protocol services (parser, serializer, packet mapper, VIF lookup).
    /// Does NOT register a transport - call AddMBusTcpTransport, AddMBusUdpTransport,
    /// or AddMBusSerialTransport separately.
    /// </summary>
    public static IServiceCollection AddMBusCore(this IServiceCollection services)
    {
        services.AddSingleton<VifLookupService>();
        services.AddSingleton<IFrameParser, FrameParser>();
        services.AddSingleton<IFrameSerializer, FrameSerializer>();
        services.AddSingleton<IPacketMapper, PacketMapper>();
        services.AddTransient<IMBusMaster, MBusMaster>();
        return services;
    }
}
