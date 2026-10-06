using Microsoft.Extensions.DependencyInjection;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class ServiceCollectionExtensionsTests
{
    private static (IMBusMaster Master, FakeMBusTransport Transport) Resolve(Action<IServiceCollection> register)
    {
        var transport = new FakeMBusTransport();
        var services = new ServiceCollection();
        services.AddSingleton<IMBusTransport>(transport);
        register(services);
        return (services.BuildServiceProvider().GetRequiredService<IMBusMaster>(), transport);
    }

    [TestMethod]
    public void AddMBusCore_RegistersMasterAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddMBusCore();

        var master = services.Single(d => d.ServiceType == typeof(IMBusMaster));
        Assert.AreEqual(ServiceLifetime.Singleton, master.Lifetime);
    }

    [TestMethod]
    public void AddMBusCore_KeepsExistingRegistrations()
    {
        var parser = new FrameParser();
        var services = new ServiceCollection();
        services.AddSingleton<IFrameParser>(parser);

        services.AddMBusCore();
        services.AddMBusCore();

        var parsers = services.Where(d => d.ServiceType == typeof(IFrameParser)).ToList();
        Assert.HasCount(1, parsers);
        Assert.AreSame(parser, parsers[0].ImplementationInstance);
        Assert.HasCount(1, services.Where(d => d.ServiceType == typeof(IMBusMaster)).ToList());
        Assert.HasCount(1, services.Where(d => d.ServiceType == typeof(MBusMasterOptions)).ToList());
    }

    [TestMethod]
    public async Task AddMBusCore_Default_MasterUsesDefaultRetries()
    {
        var (master, transport) = Resolve(s => s.AddMBusCore());

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
        Assert.HasCount(new MBusMasterOptions().Retries + 1, transport.Sent);
    }

    [TestMethod]
    public async Task AddMBusCore_Configure_ReachesMaster()
    {
        var (master, transport) = Resolve(s => s.AddMBusCore(o => o.Retries = 0));

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task AddMBusCore_ExistingOptions_AreKept()
    {
        var (master, transport) = Resolve(s =>
        {
            s.AddSingleton(new MBusMasterOptions { Retries = 1 });
            s.AddMBusCore(o => o.Retries = 5);
        });

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    public void AddMBusCore_Configure_IsAppliedOnceToSingletonOptions()
    {
        int calls = 0;
        var services = new ServiceCollection();
        services.AddMBusCore(o => { calls++; o.MaxTelegrams = 4; });
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<MBusMasterOptions>();
        var second = provider.GetRequiredService<MBusMasterOptions>();

        Assert.AreSame(first, second);
        Assert.AreEqual(4, first.MaxTelegrams);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void AddMBusCore_InvalidOptions_FailWhenMasterIsResolved()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMBusTransport>(new FakeMBusTransport());
        services.AddMBusCore(o => o.MaxTelegrams = 0);
        using var provider = services.BuildServiceProvider();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => provider.GetRequiredService<IMBusMaster>());
    }
}
