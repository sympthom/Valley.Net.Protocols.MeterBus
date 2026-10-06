using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Valley.Net.Protocols.MeterBus;

/// <summary>
/// Extension methods for registering M-Bus services with Microsoft.Extensions.DependencyInjection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds core M-Bus protocol services (parser, serializer, packet mapper, VIF lookup) and the master.
    /// Does NOT register a transport: register an <see cref="IMBusTransport"/> singleton yourself,
    /// e.g. <c>services.AddSingleton&lt;IMBusTransport&gt;(_ =&gt; new TcpMBusTransport(host, port))</c>,
    /// and call <see cref="IMBusTransport.ConnectAsync"/> before using the master.
    /// Services that are already registered are left in place, including an <see cref="MBusMasterOptions"/>,
    /// in which case <paramref name="configure"/> is not applied.
    /// </summary>
    /// <remarks>
    /// The master is a singleton because a bus carries one exchange at a time and the master's lock only
    /// covers its own calls. It does not dispose the transport; the container does.
    /// </remarks>
    public static IServiceCollection AddMBusCore(this IServiceCollection services, Action<MBusMasterOptions>? configure = null)
    {
        services.TryAddSingleton<VifLookupService>();
        services.TryAddSingleton<IFrameParser, FrameParser>();
        services.TryAddSingleton<IFrameSerializer, FrameSerializer>();
        services.TryAddSingleton<IPacketMapper, PacketMapper>();
        services.TryAddSingleton(_ =>
        {
            var options = new MBusMasterOptions();
            configure?.Invoke(options);
            return options;
        });
        services.TryAddSingleton<IMBusMaster, MBusMaster>();
        return services;
    }
}
