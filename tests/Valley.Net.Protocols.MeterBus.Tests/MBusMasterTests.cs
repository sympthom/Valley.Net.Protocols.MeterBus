using Microsoft.Extensions.DependencyInjection;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class MBusMasterTests
{
    private const string VariableDataFrame =
        "68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 9E 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 22 F3 26 42 05 5F C7 DA 0D 42 FA 16";

    private static readonly FrameParser Parser = new();
    private static readonly FrameSerializer Serializer = new();

    private static MBusMaster CreateMaster(FakeMBusTransport transport)
        => new(transport, Parser, Serializer, new PacketMapper(new VifLookupService()));

    // The example RSP_UD re-addressed; the serializer recomputes the checksum.
    private static byte[] RspUd(byte address)
    {
        var frame = (LongFrame)Parser.Parse(VariableDataFrame.HexToBytes()).Value!;
        return Serializer.Serialize(frame with { Address = address });
    }

    [TestMethod]
    public async Task InitializeAsync_ConsumesAck_NextRequestGetsRealReply()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.InitializeAsync(5);
        var packet = await master.RequestDataAsync(5);

        var data = Assert.IsInstanceOfType<VariableDataPacket>(packet);
        Assert.AreEqual((byte)5, data.Address);
    }

    [TestMethod]
    public async Task SetAddressAsync_ConsumesAck_NextRequestGetsRealReply()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);

        await master.SetAddressAsync(5, 6);
        var packet = await master.RequestDataAsync(6);

        Assert.IsInstanceOfType<VariableDataPacket>(packet);
    }

    [TestMethod]
    public async Task ResetApplicationAsync_NoAck_ThrowsTimeout()
    {
        var transport = new FakeMBusTransport().NoReply();
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.ResetApplicationAsync(5));
        StringAssert.Contains(ex.Message, "5");
    }

    [TestMethod]
    public async Task InitializeAsync_NonAckReply_Throws()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => master.InitializeAsync(5));
        StringAssert.Contains(ex.Message, "5");
    }

    [TestMethod]
    public async Task InitializeAsync_Broadcast_DoesNotWaitForReply()
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await master.InitializeAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY);

        Assert.HasCount(1, transport.Sent);
        Assert.AreEqual(0, transport.ReceiveCount);
    }

    [TestMethod]
    public async Task RequestDataAsync_AckReply_Throws()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => master.RequestDataAsync(5));
    }

    [TestMethod]
    public async Task RequestAlarmAsync_AckReply_Throws()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => master.RequestAlarmAsync(5));
    }

    [TestMethod]
    public async Task RequestDataAsync_ReplyFromOtherAddress_Throws()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(7));
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => master.RequestDataAsync(5));
    }

    [TestMethod]
    [DataRow(MBusConstants.ADDRESS_NETWORK_LAYER, DisplayName = "0xFD secondary-selected")]
    [DataRow(MBusConstants.ADDRESS_BROADCAST_REPLY, DisplayName = "0xFE test broadcast")]
    public async Task RequestDataAsync_ViaSharedAddress_AcceptsSlavePrimaryAddress(byte address)
    {
        var transport = new FakeMBusTransport().Reply(RspUd(7));
        var master = CreateMaster(transport);

        var packet = await master.RequestDataAsync(address);

        Assert.AreEqual((byte)7, packet.Address);
    }

    [TestMethod]
    public async Task RequestDataAsync_NoReply_ThrowsTimeout()
    {
        var transport = new FakeMBusTransport().NoReply();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
    }

    [TestMethod]
    public async Task RequestDataAsync_DiscardsInputBeforeEachSend()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.InitializeAsync(5);
        await master.RequestDataAsync(5);

        Assert.AreEqual(2, transport.DiscardCount);
    }

    [TestMethod]
    public async Task PingAsync_NoReply_ReturnsFalse()
    {
        var transport = new FakeMBusTransport().NoReply();
        var master = CreateMaster(transport);

        Assert.IsFalse(await master.PingAsync(5));
    }

    [TestMethod]
    public async Task PingAsync_CallerCancels_Throws()
    {
        var transport = new FakeMBusTransport(TimeSpan.FromSeconds(30));
        var master = CreateMaster(transport);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(() => master.PingAsync(5, cts.Token));
    }

    [TestMethod]
    public async Task ScanAsync_CallerCancels_ThrowsWithoutPhantomMeter()
    {
        // Meter 1 ACKs, then the caller cancels while it waits for the RSP_UD.
        var transport = new FakeMBusTransport(TimeSpan.FromSeconds(30)).ReplyAck();
        var master = CreateMaster(transport);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var found = new List<MeterInfo>();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await foreach (var meter in master.ScanAsync([1], cts.Token))
                found.Add(meter);
        });

        Assert.IsEmpty(found);
    }

    [TestMethod]
    public async Task ScanAsync_ReplyFromOtherAddress_SkipsAddress()
    {
        // Address 1 is silent, address 5 ACKs but the RSP_UD comes from 7, address 6 answers properly.
        var transport = new FakeMBusTransport()
            .NoReply()
            .ReplyAck().Reply(RspUd(7))
            .ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);
        var found = new List<MeterInfo>();

        await foreach (var meter in master.ScanAsync([1, 5, 6]))
            found.Add(meter);

        Assert.HasCount(1, found);
        Assert.AreEqual((byte)6, found[0].Address);
        Assert.AreEqual((byte)6, found[0].Packet?.Address);
    }

    [TestMethod]
    public async Task SetAddressAsync_NewAddressAbove250_Throws()
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => master.SetAddressAsync(5, 251));
        Assert.IsEmpty(transport.Sent);
    }

    [TestMethod]
    public async Task SendDataAsync_DataOver252Bytes_ThrowsBeforeSending()
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => master.SendDataAsync(5, new byte[253]));
        Assert.IsEmpty(transport.Sent);
        Assert.AreEqual(0, transport.DiscardCount);
    }

    [TestMethod]
    public async Task DisposeAsync_LeavesTransportUsable()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var first = CreateMaster(transport);

        await first.DisposeAsync();

        Assert.IsFalse(transport.IsDisposed);
        Assert.IsTrue(await CreateMaster(transport).PingAsync(5));
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
    }
}
