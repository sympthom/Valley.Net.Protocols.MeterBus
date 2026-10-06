using Microsoft.Extensions.Logging;

namespace Valley.Net.Protocols.MeterBus.Tests;

[TestClass]
public sealed class MBusMasterTests
{
    private const string VariableDataFrame =
        "68 31 31 68 08 01 72 45 58 57 03 B4 05 34 04 9E 00 27 B6 03 06 F9 34 15 03 15 C6 00 4D 05 2E 00 00 00 00 05 3D 00 00 00 00 05 5B 22 F3 26 42 05 5F C7 DA 0D 42 FA 16";

    // A short frame with a wrong checksum, as a collision leaves it.
    private static readonly byte[] Garbled = "10 5B 05 00 16".HexToBytes();

    private static readonly FrameParser Parser = new();
    private static readonly FrameSerializer Serializer = new();

    // Retries are off unless a test is about them, so each queued reply answers exactly one request.
    private static MBusMaster CreateMaster(IMBusTransport transport, int retries = 0, int scanRetries = 0)
        => new(transport, Parser, Serializer, new PacketMapper(new VifLookupService()),
            new MBusMasterOptions { Retries = retries, ScanRetries = scanRetries });

    private static MBusMaster CreateDefaultMaster(FakeMBusTransport transport)
        => new(transport, Parser, Serializer, new PacketMapper(new VifLookupService()));

    // The example RSP_UD re-addressed; the serializer recomputes the checksum.
    private static byte[] RspUd(byte address)
    {
        var frame = (LongFrame)Parser.Parse(VariableDataFrame.HexToBytes()).Value!;
        return Serializer.Serialize(frame with { Address = address });
    }

    private static byte[] Reply(byte address, ControlInformation ci, params byte[] data)
        => data.Length == 0
            ? Serializer.Serialize(new ControlFrame(ControlMask.RSP_UD, ci, address, 0))
            : Serializer.Serialize(new LongFrame(ControlMask.RSP_UD, ci, address, data, 0));

    // The example RSP_UD with DIF 1Fh appended when the meter has more telegrams; idLow changes the meter ID.
    private static byte[] Telegram(byte address, bool more, byte idLow = 0x45)
    {
        var frame = (LongFrame)Parser.Parse(VariableDataFrame.HexToBytes()).Value!;
        var data = frame.Data.ToArray();
        data[0] = idLow;
        if (more)
            data = [.. data, 0x1F];
        return Serializer.Serialize(frame with { Address = address, Data = data });
    }

    // The C field of each frame sent: byte 1 of a short frame, byte 4 of a long one.
    private static byte[] Controls(FakeMBusTransport transport)
        => transport.Sent.Select(f => f[0] == MBusConstants.FRAME_SHORT_START ? f[1] : f[4]).ToArray();

    private static async Task<List<MeterInfo>> ScanAll(IMBusMaster master, params byte[] addresses)
    {
        var found = new List<MeterInfo>();
        await foreach (var meter in master.ScanAsync(addresses))
            found.Add(meter);
        return found;
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
    public async Task InitializeAsync_NonAckReply_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.InitializeAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_UNEXPECTED_FRAME, ex.Error.Code);
        Assert.AreEqual((byte)5, ex.Address);
        StringAssert.Contains(ex.Message, "5");
    }

    [TestMethod]
    public async Task SendDataAsync_GarbledReply_ThrowsMBusExceptionWithParserCode()
    {
        var transport = new FakeMBusTransport().Reply(Garbled);
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.SendDataAsync(5, new byte[] { 0x01 }));
        Assert.AreEqual("CRC_MISMATCH", ex.Error.Code);
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
    public async Task RequestDataAsync_AckReply_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_UNEXPECTED_FRAME, ex.Error.Code);
    }

    [TestMethod]
    public async Task RequestDataAsync_ReplyFromOtherAddress_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(7));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_ADDRESS_MISMATCH, ex.Error.Code);
    }

    [TestMethod]
    public async Task RequestDataAsync_GarbledReply_ThrowsMBusExceptionWithParserCode()
    {
        var transport = new FakeMBusTransport().Reply(Garbled);
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        Assert.AreEqual("CRC_MISMATCH", ex.Error.Code);
    }

    [TestMethod]
    public async Task RequestDataAsync_UnmappableReply_KeepsMapperCode()
    {
        var transport = new FakeMBusTransport().Reply(Reply(5, ControlInformation.RESP_VARIABLE, 1, 2, 3));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        Assert.AreEqual("VAR_FRAME_TOO_SHORT", ex.Error.Code);
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
    public async Task RequestDataAsync_TransportTimesOutByCancelling_ThrowsTimeout()
    {
        var transport = new FakeMBusTransport(cancelOnTimeout: true);
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
    }

    [TestMethod]
    public async Task PingAsync_TransportTimesOutByCancelling_ReturnsFalse()
    {
        var transport = new FakeMBusTransport(cancelOnTimeout: true);
        var master = CreateMaster(transport);

        Assert.IsFalse(await master.PingAsync(5));
    }

    [TestMethod]
    public async Task RequestDataAsync_CallerCancels_ThrowsCanceledWithoutRetry()
    {
        var transport = new FakeMBusTransport(TimeSpan.FromSeconds(30));
        var master = CreateDefaultMaster(transport);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(() => master.RequestDataAsync(5, cts.Token));
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task RequestDataAsync_TimeoutRacingCallerCancel_DoesNotRetry()
    {
        using var cts = new CancellationTokenSource();
        var transport = new FakeMBusTransport();
        transport.Receiving += cts.Cancel;
        var master = CreateDefaultMaster(transport);

        await Assert.ThrowsAsync<OperationCanceledException>(() => master.RequestDataAsync(5, cts.Token));
        Assert.HasCount(1, transport.Sent);
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
    public async Task PingAsync_GarbledReply_ReturnsFalse()
    {
        var transport = new FakeMBusTransport().Reply(Garbled);
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

    // ---- Retries ----

    [TestMethod]
    public async Task RequestDataAsync_SlaveMissesFirstRequests_RetriesSameFrame()
    {
        var transport = new FakeMBusTransport().NoReply(2).Reply(RspUd(5));
        var master = CreateDefaultMaster(transport);

        var packet = await master.RequestDataAsync(5);

        Assert.AreEqual((byte)5, packet.Address);
        Assert.HasCount(3, transport.Sent);
        Assert.IsTrue(transport.Sent.All(f => f.SequenceEqual(transport.Sent[0])));
    }

    [TestMethod]
    public async Task RequestDataAsync_DefaultRetries_GivesUpAfterFourAttempts()
    {
        var transport = new FakeMBusTransport();
        var master = CreateDefaultMaster(transport);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
        Assert.HasCount(4, transport.Sent);
    }

    [TestMethod]
    public async Task RequestDataAsync_GarbledThenValid_Retries()
    {
        var transport = new FakeMBusTransport().Reply(Garbled).Reply(RspUd(5));
        var master = CreateMaster(transport, retries: 1);

        var packet = await master.RequestDataAsync(5);

        Assert.AreEqual((byte)5, packet.Address);
    }

    [TestMethod]
    public async Task RequestDataAsync_ReplyFromOtherAddress_IsNotRetried()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(7)).Reply(RspUd(5));
        var master = CreateMaster(transport, retries: 3);

        await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task InitializeAsync_AckOnSecondAttempt_Succeeds()
    {
        var transport = new FakeMBusTransport().NoReply().ReplyAck();
        var master = CreateMaster(transport, retries: 1);

        await master.InitializeAsync(5);

        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    public void Constructor_NegativeRetries_Throws()
    {
        var transport = new FakeMBusTransport();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CreateMaster(transport, retries: -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => CreateMaster(transport, scanRetries: -1));
    }

    // ---- Bus lock ----

    [TestMethod]
    public async Task ConcurrentRequests_RunOneAfterAnother()
    {
        var slave1 = new TaskCompletionSource();
        var transport = new FakeMBusTransport().ReplyAfter(slave1.Task, RspUd(1)).Reply(RspUd(2));
        var master = CreateMaster(transport);
        var firstWaiting = new TaskCompletionSource();
        transport.Receiving += () => firstWaiting.TrySetResult();

        var first = master.RequestDataAsync(1);
        await firstWaiting.Task;
        var second = master.RequestDataAsync(2);

        // Without the lock the second REQ_UD2 would already be on the wire and take slave 1's reply.
        Assert.HasCount(1, transport.Sent);

        slave1.SetResult();
        Assert.AreEqual((byte)1, (await first).Address);
        Assert.AreEqual((byte)2, (await second).Address);
        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    public async Task CallerCancelsWhileWaitingForBus_Throws()
    {
        var slave1 = new TaskCompletionSource();
        var transport = new FakeMBusTransport().ReplyAfter(slave1.Task, RspUd(1));
        var master = CreateMaster(transport);
        var first = master.RequestDataAsync(1);
        using var cts = new CancellationTokenSource();

        var second = master.PingAsync(2, cts.Token);
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => second);
        slave1.SetResult();
        await first;
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task ScanAsync_ReleasesBusBetweenAddresses()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(1)).ReplyAck();
        var master = CreateMaster(transport);

        await using var scan = master.ScanAsync([1, 2]).GetAsyncEnumerator();
        Assert.IsTrue(await scan.MoveNextAsync());

        // The scan is paused on its yield; holding the bus there would block this ping forever.
        Assert.IsTrue(await master.PingAsync(9).WaitAsync(TimeSpan.FromSeconds(5)));
    }

    // ---- Scan ----

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
    public async Task ScanAsync_SilentAddress_YieldsNothing()
    {
        var transport = new FakeMBusTransport().NoReply().ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 1, 6);

        Assert.HasCount(1, found);
        Assert.AreEqual(new MeterInfo(6, found[0].Packet, ScanStatus.Found), found[0]);
        Assert.AreEqual((byte)6, found[0].Packet?.Address);
    }

    [TestMethod]
    public async Task ScanAsync_ReplyFromOtherAddress_ReportsErrorAndContinues()
    {
        // Address 5 ACKs but the RSP_UD comes from 7, address 6 answers properly.
        var transport = new FakeMBusTransport()
            .ReplyAck().Reply(RspUd(7))
            .ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 5, 6);

        Assert.HasCount(2, found);
        Assert.AreEqual(ScanStatus.Error, found[0].Status);
        Assert.AreEqual(MBusConstants.ERROR_ADDRESS_MISMATCH, found[0].Error?.Code);
        Assert.IsNull(found[0].Packet);
        Assert.AreEqual(ScanStatus.Found, found[1].Status);
    }

    [TestMethod]
    public async Task ScanAsync_GarbledAck_ReportsCollisionAndContinues()
    {
        var transport = new FakeMBusTransport()
            .ReplyAck().Reply(RspUd(3))
            .Reply(Garbled)
            .ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 3, 4, 6);

        CollectionAssert.AreEqual(new byte[] { 3, 4, 6 }, found.Select(m => m.Address).ToArray());
        Assert.AreEqual(ScanStatus.Collision, found[1].Status);
        Assert.AreEqual("CRC_MISMATCH", found[1].Error?.Code);
        Assert.AreEqual(ScanStatus.Found, found[2].Status);
    }

    [TestMethod]
    public async Task ScanAsync_GarbledData_ReportsCollisionAndContinues()
    {
        var garbledLong = RspUd(4);
        garbledLong[^2] ^= 0xFF;
        var transport = new FakeMBusTransport()
            .ReplyAck().Reply(garbledLong)
            .ReplyAck().Reply(RspUd(6));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 4, 6);

        Assert.HasCount(2, found);
        Assert.AreEqual(ScanStatus.Collision, found[0].Status);
        Assert.AreEqual(ScanStatus.Found, found[1].Status);
    }

    [TestMethod]
    public async Task ScanAsync_AckWithoutData_ReportsNoReply()
    {
        var transport = new FakeMBusTransport().ReplyAck().NoReply();
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 4);

        Assert.HasCount(1, found);
        Assert.AreEqual(ScanStatus.Error, found[0].Status);
        Assert.AreEqual(MBusConstants.ERROR_NO_REPLY, found[0].Error?.Code);
    }

    [TestMethod]
    public async Task ScanAsync_UnmappableData_ReportsMapperCode()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(Reply(4, ControlInformation.RESP_VARIABLE, 1, 2, 3));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 4);

        Assert.AreEqual(ScanStatus.Error, found[0].Status);
        Assert.AreEqual("VAR_FRAME_TOO_SHORT", found[0].Error?.Code);
    }

    [TestMethod]
    public async Task ScanAsync_NonAckToSndNke_ReportsUnexpectedFrame()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(4));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 4);

        Assert.AreEqual(ScanStatus.Error, found[0].Status);
        Assert.AreEqual(MBusConstants.ERROR_UNEXPECTED_FRAME, found[0].Error?.Code);
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task ScanAsync_ApplicationError_IsFound()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(Reply(4, ControlInformation.ERROR_GENERAL, 0x08));
        var master = CreateMaster(transport);

        var found = await ScanAll(master, 4);

        Assert.AreEqual(ScanStatus.Found, found[0].Status);
        Assert.AreEqual(new ApplicationErrorPacket(4, ApplicationErrorCode.Busy), found[0].Packet);
    }

    [TestMethod]
    public async Task ScanAsync_SlaveMissesFirstSndNke_IsFound()
    {
        var transport = new FakeMBusTransport().NoReply().ReplyAck().Reply(RspUd(2));
        var master = CreateDefaultMaster(transport);

        var found = await ScanAll(master, 2);

        Assert.HasCount(1, found);
        Assert.AreEqual(ScanStatus.Found, found[0].Status);
    }

    [TestMethod]
    public async Task ScanAsync_DefaultScanRetries_PingsSilentAddressTwice()
    {
        var transport = new FakeMBusTransport();
        var master = CreateDefaultMaster(transport);

        var found = await ScanAll(master, 2);

        Assert.IsEmpty(found);
        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    [DataRow((byte)251)]
    [DataRow((byte)252)]
    [DataRow(MBusConstants.ADDRESS_BROADCAST_NOREPLY)]
    public async Task ScanAsync_AddressThatCannotAnswer_Throws(byte address)
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => ScanAll(master, address));
        Assert.IsEmpty(transport.Sent);
    }

    // ---- Addresses ----

    public static IEnumerable<object[]> AllOperations =>
    [
        ["Ping", (Func<IMBusMaster, byte, Task>)((m, a) => m.PingAsync(a))],
        ["RequestData", (Func<IMBusMaster, byte, Task>)((m, a) => m.RequestDataAsync(a))],
        ["RequestAlarm", (Func<IMBusMaster, byte, Task>)((m, a) => m.RequestAlarmAsync(a))],
        ["Initialize", (Func<IMBusMaster, byte, Task>)((m, a) => m.InitializeAsync(a))],
        ["ResetApplication", (Func<IMBusMaster, byte, Task>)((m, a) => m.ResetApplicationAsync(a))],
        ["SetAddress", (Func<IMBusMaster, byte, Task>)((m, a) => m.SetAddressAsync(a, 7))],
        ["SendData", (Func<IMBusMaster, byte, Task>)((m, a) => m.SendDataAsync(a, new byte[] { 0x01 }))],
        ["RequestAllTelegrams", (Func<IMBusMaster, byte, Task>)((m, a) => m.RequestAllTelegramsAsync(a))],
    ];

    public static IEnumerable<object[]> Commands => AllOperations.Where(o => (string)o[0] is not ("RequestData" or "RequestAlarm" or "RequestAllTelegrams"));

    [TestMethod]
    [DynamicData(nameof(AllOperations))]
    public async Task ReservedAddress_ThrowsBeforeSending(string operation, Func<IMBusMaster, byte, Task> call)
    {
        foreach (byte address in new byte[] { 251, 252 })
        {
            var transport = new FakeMBusTransport();
            var master = CreateMaster(transport);

            await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => call(master, address), operation);
            Assert.IsEmpty(transport.Sent, operation);
        }
    }

    [TestMethod]
    [DynamicData(nameof(Commands))]
    public async Task BroadcastNoReply_SendsWithoutWaiting(string operation, Func<IMBusMaster, byte, Task> call)
    {
        var transport = new FakeMBusTransport(TimeSpan.FromSeconds(30));
        var master = CreateDefaultMaster(transport);

        await call(master, MBusConstants.ADDRESS_BROADCAST_NOREPLY);

        Assert.HasCount(1, transport.Sent, operation);
        Assert.AreEqual(0, transport.ReceiveCount, operation);
    }

    [TestMethod]
    public async Task PingAsync_BroadcastNoReply_ReturnsFalse()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        Assert.IsFalse(await master.PingAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY));
    }

    [TestMethod]
    public async Task RequestDataAsync_BroadcastNoReply_Throws()
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => master.RequestDataAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY));
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => master.RequestAlarmAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY));
        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(() => master.RequestAllTelegramsAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY));
        Assert.IsEmpty(transport.Sent);
    }

    [TestMethod]
    public async Task PingAsync_BroadcastReply_WaitsForAck()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        Assert.IsTrue(await master.PingAsync(MBusConstants.ADDRESS_BROADCAST_REPLY));
        Assert.AreEqual(1, transport.ReceiveCount);
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

    // ---- Application errors and alarms ----

    [TestMethod]
    public async Task RequestDataAsync_ApplicationError_ReturnsPacket()
    {
        var transport = new FakeMBusTransport().Reply(Reply(5, ControlInformation.ERROR_GENERAL, 0x08));
        var master = CreateMaster(transport);

        var packet = await master.RequestDataAsync(5);

        Assert.AreEqual(new ApplicationErrorPacket(5, ApplicationErrorCode.Busy), packet);
    }

    [TestMethod]
    public async Task RequestDataAsync_ApplicationErrorWithoutStatusByte_ReturnsPacket()
    {
        // L = 3: the parser returns a control frame.
        var transport = new FakeMBusTransport().Reply(Reply(5, ControlInformation.ERROR_GENERAL));
        var master = CreateMaster(transport);

        var packet = await master.RequestDataAsync(5);

        Assert.AreEqual(new ApplicationErrorPacket(5, ApplicationErrorCode.Unspecified), packet);
    }

    [TestMethod]
    public async Task RequestAlarmAsync_AlarmStatus_ReturnsPacket()
    {
        var transport = new FakeMBusTransport().Reply(Reply(5, ControlInformation.STATUS_ALARM, 0x05));
        var master = CreateMaster(transport);

        var packet = await master.RequestAlarmAsync(5);

        Assert.AreEqual(new AlarmStatusPacket(5, 0x05), packet);
    }

    [TestMethod]
    public async Task RequestAlarmAsync_Ack_ReturnsEmptyPacketForRequestedAddress()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        var packet = await master.RequestAlarmAsync(5);

        Assert.AreEqual(new EmptyPacket(5), packet);
    }

    [TestMethod]
    public async Task RequestAlarmAsync_ControlFrameFromOtherAddress_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().Reply(Reply(7, ControlInformation.ERROR_GENERAL));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestAlarmAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_ADDRESS_MISMATCH, ex.Error.Code);
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

    // ---- Frame count bit ----

    [TestMethod]
    public async Task RequestDataAsync_SuccessiveRequests_ToggleFcbStartingAtOne()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).Reply(RspUd(5)).Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.RequestDataAsync(5);
        await master.RequestDataAsync(5);

        Assert.AreEqual("10 7B 05 80 16", transport.Sent[0].ToHex());
        Assert.AreEqual("10 5B 05 60 16", transport.Sent[1].ToHex());
        Assert.AreEqual("10 7B 05 80 16", transport.Sent[2].ToHex());
    }

    [TestMethod]
    public async Task InitializeAsync_RestartsFcbAtOne()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).ReplyAck().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.InitializeAsync(5);
        await master.RequestDataAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x40, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task PingAsync_RestartsFcbEvenWithoutAck()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).NoReply().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.PingAsync(5);
        await master.RequestDataAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x40, 0x7B }, Controls(transport));
    }

    [TestMethod]
    [DataRow(MBusConstants.ADDRESS_BROADCAST_NOREPLY)]
    [DataRow(MBusConstants.ADDRESS_BROADCAST_REPLY)]
    public async Task PingAsync_Broadcast_RestartsFcbOfEveryAddress(byte broadcast)
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).Reply(RspUd(6));
        // 255 is not answered, so no E5 is read for it.
        if (broadcast == MBusConstants.ADDRESS_BROADCAST_REPLY)
            transport.ReplyAck();
        transport.Reply(RspUd(5)).Reply(RspUd(6));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.RequestDataAsync(6);
        await master.PingAsync(broadcast);
        await master.RequestDataAsync(5);
        await master.RequestDataAsync(6);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x7B, 0x40, 0x7B, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestDataAsync_FcbIsKeptPerAddress()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).Reply(RspUd(6)).Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.RequestDataAsync(6);
        await master.RequestDataAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x7B, 0x5B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestDataAsync_RetryAfterTimeout_RepeatsSameFcb()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).NoReply().Reply(RspUd(5)).Reply(RspUd(5));
        var master = CreateMaster(transport, retries: 1);

        await master.RequestDataAsync(5);
        await master.RequestDataAsync(5);
        await master.RequestDataAsync(5);

        // The repeated 5B tells the slave its reply was lost, so it sends the same telegram again.
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x5B, 0x5B, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestDataAsync_RetryAfterGarbledReply_RepeatsSameFcb()
    {
        var transport = new FakeMBusTransport().Reply(Garbled).Reply(RspUd(5)).Reply(RspUd(5));
        var master = CreateMaster(transport, retries: 1);

        await master.RequestDataAsync(5);
        await master.RequestDataAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x7B, 0x5B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestDataAsync_NoAnswer_DoesNotToggleFcb()
    {
        var transport = new FakeMBusTransport().NoReply().Reply(RspUd(7)).Reply(RspUd(5));
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.RequestDataAsync(5));
        await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        await master.RequestDataAsync(5);

        // Neither the timeout nor a reply from address 7 was this slave's answer.
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x7B, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestDataAsync_UnmappableReply_StillTogglesFcb()
    {
        var transport = new FakeMBusTransport().Reply(Reply(5, ControlInformation.RESP_VARIABLE, 1, 2, 3)).Reply(RspUd(5));
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestDataAsync(5));
        await master.RequestDataAsync(5);

        // The link layer delivered the RSP_UD; only its application data was bad.
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x5B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestAlarmAsync_AckTogglesFcbSharedWithRequestData()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(5)).ReplyAck();
        var master = CreateMaster(transport);

        await master.RequestAlarmAsync(5);
        await master.RequestDataAsync(5);
        await master.RequestAlarmAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7A, 0x5B, 0x7A }, Controls(transport));
    }

    [TestMethod]
    public async Task SendDataAsync_TogglesSndUdFcbIndependentlyOfRequests()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).ReplyAck().ReplyAck().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        await master.SendDataAsync(5, new byte[] { 0x01 });
        await master.SetAddressAsync(5, 5);
        await master.RequestDataAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x73, 0x53, 0x5B }, Controls(transport));
    }

    [TestMethod]
    public async Task SendDataAsync_NoAck_RetriesAndNextCommandRepeatSameFcb()
    {
        var transport = new FakeMBusTransport().NoReply(2).ReplyAck().ReplyAck();
        var master = CreateMaster(transport, retries: 1);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => master.SendDataAsync(5, new byte[] { 0x01 }));
        await master.SendDataAsync(5, new byte[] { 0x01 });
        await master.ResetApplicationAsync(5);

        CollectionAssert.AreEqual(new byte[] { 0x73, 0x73, 0x73, 0x53 }, Controls(transport));
    }

    [TestMethod]
    public async Task SendDataAsync_BroadcastNoReply_ClearsFcvWithoutCounting()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        await master.SendDataAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY, new byte[] { 0x01 });
        await master.SendDataAsync(MBusConstants.ADDRESS_BROADCAST_NOREPLY, new byte[] { 0x01 });
        await master.SendDataAsync(5, new byte[] { 0x01 });

        // MBDOC48 5.5.2 (4): with FCV set a slave could take the second broadcast for a repeat of the first.
        CollectionAssert.AreEqual(new byte[] { 0x43, 0x43, 0x73 }, Controls(transport));
        Assert.AreEqual("68 04 04 68 43 FF 51 01 94 16", transport.Sent[0].ToHex());
    }

    [TestMethod]
    public async Task ScanAsync_RequestAfterSndNkeCarriesFcbOne()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(5)).ReplyAck().Reply(RspUd(5));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(5);
        var found = await ScanAll(master, 5);

        Assert.AreEqual(ScanStatus.Found, found[0].Status);
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x40, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task SelectSlaveAsync_RestartsFcbOfAddress253()
    {
        var transport = new FakeMBusTransport().Reply(RspUd(7)).ReplyAck().Reply(RspUd(7));
        var master = CreateMaster(transport);

        await master.RequestDataAsync(MBusConstants.ADDRESS_NETWORK_LAYER);
        await master.SelectSlaveAsync(new SecondaryAddress(12345678, 0x1234, 1, DeviceType.Water));
        await master.RequestDataAsync(MBusConstants.ADDRESS_NETWORK_LAYER);

        // Without the restart the second request would carry FCB = 0 (5B).
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x53, 0x7B }, Controls(transport));
    }

    // ---- Multi-telegram ----

    [TestMethod]
    public async Task RequestAllTelegramsAsync_ReadsWhileMoreRecordsFollow()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: true)).Reply(Telegram(5, more: true)).Reply(Telegram(5, more: false));
        var master = CreateMaster(transport);

        var telegrams = await master.RequestAllTelegramsAsync(5);

        Assert.HasCount(3, telegrams);
        var packets = telegrams.Cast<VariableDataPacket>().ToList();
        CollectionAssert.AreEqual(new[] { true, true, false }, packets.Select(p => p.MoreRecordsFollow).ToArray());
        Assert.IsTrue(packets.All(p => p.Records.Length == 6));
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x5B, 0x7B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_SingleTelegram_SendsOneRequest()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: false));
        var master = CreateMaster(transport);

        var telegrams = await master.RequestAllTelegramsAsync(5);

        Assert.HasCount(1, telegrams);
        Assert.HasCount(1, transport.Sent);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_AckToFollowUp_EndsSequence()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: true)).ReplyAck();
        var master = CreateMaster(transport);

        var telegrams = await master.RequestAllTelegramsAsync(5);

        Assert.HasCount(1, telegrams);
        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_AckToFirstRequest_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestAllTelegramsAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_UNEXPECTED_FRAME, ex.Error.Code);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_LostTelegram_IsRequestedAgainWithSameFcb()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: true)).NoReply().Reply(Telegram(5, more: false));
        var master = CreateMaster(transport, retries: 1);

        var telegrams = await master.RequestAllTelegramsAsync(5);

        Assert.HasCount(2, telegrams);
        CollectionAssert.AreEqual(new byte[] { 0x7B, 0x5B, 0x5B }, Controls(transport));
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_StillMoreAtLimit_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: true)).Reply(Telegram(5, more: true)).Reply(Telegram(5, more: false));
        var master = new MBusMaster(transport, Parser, Serializer, new PacketMapper(new VifLookupService()),
            new MBusMasterOptions { Retries = 0, MaxTelegrams = 2 });

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestAllTelegramsAsync(5));
        Assert.AreEqual(MBusConstants.ERROR_TELEGRAM_LIMIT, ex.Error.Code);
        Assert.HasCount(2, transport.Sent);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_TelegramFromOtherMeter_ThrowsMBusException()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(7, more: true)).Reply(Telegram(8, more: false, idLow: 0x46));
        var master = CreateMaster(transport);

        var ex = await Assert.ThrowsExactlyAsync<MBusException>(() => master.RequestAllTelegramsAsync(MBusConstants.ADDRESS_BROADCAST_REPLY));
        Assert.AreEqual(MBusConstants.ERROR_TELEGRAM_MISMATCH, ex.Error.Code);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_ApplicationErrorMidSequence_EndsWithIt()
    {
        var transport = new FakeMBusTransport().Reply(Telegram(5, more: true)).Reply(Reply(5, ControlInformation.ERROR_GENERAL, 0x08));
        var master = CreateMaster(transport);

        var telegrams = await master.RequestAllTelegramsAsync(5);

        Assert.HasCount(2, telegrams);
        Assert.AreEqual(new ApplicationErrorPacket(5, ApplicationErrorCode.Busy), telegrams[1]);
    }

    [TestMethod]
    public async Task RequestAllTelegramsAsync_HoldsBusForWholeSequence()
    {
        var firstTelegram = new TaskCompletionSource();
        var transport = new FakeMBusTransport()
            .ReplyAfter(firstTelegram.Task, Telegram(1, more: true)).Reply(Telegram(1, more: false))
            .Reply(RspUd(2));
        var master = CreateMaster(transport);
        var firstWaiting = new TaskCompletionSource();
        transport.Receiving += () => firstWaiting.TrySetResult();

        var all = master.RequestAllTelegramsAsync(1);
        await firstWaiting.Task;
        var other = master.RequestDataAsync(2);
        firstTelegram.SetResult();

        Assert.HasCount(2, await all);
        Assert.AreEqual((byte)2, (await other).Address);
        CollectionAssert.AreEqual(new byte[] { 1, 1, 2 }, transport.Sent.Select(f => f[2]).ToArray());
    }

    [TestMethod]
    public void Constructor_MaxTelegramsBelowOne_Throws()
    {
        var transport = new FakeMBusTransport();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new MBusMaster(transport, Parser, Serializer,
            new PacketMapper(new VifLookupService()), new MBusMasterOptions { MaxTelegrams = 0 }));
    }

    // ---- Secondary addressing ----

    [TestMethod]
    public async Task SelectSlaveAsync_SendsSelectionTo253()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        await master.SelectSlaveAsync(new SecondaryAddress(12345678, 0x2C2D, 0x01, DeviceType.Water));

        // MBDOC48 Fig. 29: 53 FD 52, ID least significant BCD byte first, manufacturer little-endian, version, medium.
        Assert.AreEqual("68 0B 0B 68 53 FD 52 78 56 34 12 2D 2C 01 07 17 16", transport.Sent[0].ToHex());
    }

    [TestMethod]
    public async Task SelectSlaveAsync_SmallId_IsZeroPadded()
    {
        var transport = new FakeMBusTransport().ReplyAck();
        var master = CreateMaster(transport);

        await master.SelectSlaveAsync(new SecondaryAddress(42, 0x2C2D, 0x01, DeviceType.Water));

        CollectionAssert.AreEqual(new byte[] { 0x42, 0x00, 0x00, 0x00 }, transport.Sent[0][7..11]);
    }

    [TestMethod]
    public async Task SelectSlaveAsync_IdOverEightDigits_ThrowsBeforeSending()
    {
        var transport = new FakeMBusTransport();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => master.SelectSlaveAsync(new SecondaryAddress(100_000_000, 0x2C2D, 0x01, DeviceType.Water)));
        Assert.IsEmpty(transport.Sent);
    }

    [TestMethod]
    public async Task SelectSlaveAsync_RawId_SendsWildcardsAndHexNibblesAsGiven()
    {
        var transport = new FakeMBusTransport().ReplyAck().ReplyAck();
        var master = CreateMaster(transport);

        await master.SelectSlaveAsync(0x1234FFFF, 0xFFFF, 0xFF, (DeviceType)0xFF);
        await master.SelectSlaveAsync(0x00AB1234, 0x2C2D, 0x01, DeviceType.Water);

        Assert.AreEqual("68 0B 0B 68 53 FD 52 FF FF 34 12 FF FF FF FF", transport.Sent[0][..15].ToHex());
        CollectionAssert.AreEqual(new byte[] { 0x34, 0x12, 0xAB, 0x00 }, transport.Sent[1][7..11]);
    }

    [TestMethod]
    public async Task SelectSlaveAsync_NoMatch_ThrowsTimeout()
    {
        var transport = new FakeMBusTransport().NoReply();
        var master = CreateMaster(transport);

        await Assert.ThrowsExactlyAsync<TimeoutException>(
            () => master.SelectSlaveAsync(new SecondaryAddress(12345678, 0x2C2D, 0x01, DeviceType.Water)));
    }

    [TestMethod]
    public async Task SelectSlaveAsync_ThenRequestData_ReadsSelectedSlave()
    {
        var transport = new FakeMBusTransport().ReplyAck().Reply(RspUd(0));
        var master = CreateMaster(transport);

        await master.SelectSlaveAsync(new SecondaryAddress(3575845, 0x05B4, 0x34, DeviceType.Water));
        var packet = await master.RequestDataAsync(MBusConstants.ADDRESS_NETWORK_LAYER);

        Assert.IsInstanceOfType<VariableDataPacket>(packet);
        Assert.AreEqual("10 7B FD 78 16", transport.Sent[1].ToHex());
    }

    // ---- Tracing ----

    private sealed class ListLogger(LogLevel minimum = LogLevel.Trace) : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= minimum;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                Entries.Add((logLevel, formatter(state, exception)));
        }
    }

    [TestMethod]
    public async Task TracingTransport_LogsSentAndReceivedFramesAsHex()
    {
        var inner = new FakeMBusTransport().ReplyAck();
        var logger = new ListLogger();
        var master = CreateMaster(new TracingMBusTransport(inner, logger));

        Assert.IsTrue(await master.PingAsync(5));

        var debug = logger.Entries.Where(e => e.Level == LogLevel.Debug).Select(e => e.Message).ToList();
        Assert.HasCount(2, debug);
        Assert.AreEqual("TX 5 bytes: 10 40 05 45 16", debug[0]);
        StringAssert.StartsWith(debug[1], "RX 1 bytes after ");
        StringAssert.EndsWith(debug[1], " ms: E5");
        Assert.AreEqual(1, inner.ReceiveCount);
    }

    [TestMethod]
    public async Task TracingTransport_LogsTimeoutAndRethrows()
    {
        var logger = new ListLogger();
        var tracing = new TracingMBusTransport(new FakeMBusTransport(), logger);

        await Assert.ThrowsExactlyAsync<TimeoutException>(async () => await tracing.ReceiveFrameAsync());

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Debug && e.Message.StartsWith("RX timeout after ")));
    }

    [TestMethod]
    public async Task TracingTransport_TimeoutByCancelling_IsLoggedAndRethrown()
    {
        var logger = new ListLogger();
        var tracing = new TracingMBusTransport(new FakeMBusTransport(cancelOnTimeout: true), logger);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await tracing.ReceiveFrameAsync());

        Assert.IsTrue(logger.Entries.Any(e => e.Level == LogLevel.Debug && e.Message.StartsWith("RX timeout after ")));
    }

    [TestMethod]
    public async Task TracingTransport_CallerCancels_IsNotLoggedAsTimeout()
    {
        var logger = new ListLogger();
        using var cts = new CancellationTokenSource();
        var inner = new FakeMBusTransport(TimeSpan.FromSeconds(30));
        inner.Receiving += cts.Cancel;
        var tracing = new TracingMBusTransport(inner, logger);

        await Assert.ThrowsAsync<OperationCanceledException>(async () => await tracing.ReceiveFrameAsync(cts.Token));

        Assert.IsFalse(logger.Entries.Any(e => e.Message.StartsWith("RX timeout")));
    }

    [TestMethod]
    public async Task TracingTransport_DebugDisabled_LogsNoFrames()
    {
        var inner = new FakeMBusTransport().ReplyAck();
        var logger = new ListLogger(LogLevel.Information);
        var master = CreateMaster(new TracingMBusTransport(inner, logger));

        Assert.IsTrue(await master.PingAsync(5));

        Assert.IsEmpty(logger.Entries);
    }

    [TestMethod]
    public async Task TracingTransport_DisposesInnerOnce()
    {
        var inner = new FakeMBusTransport();
        var tracing = new TracingMBusTransport(inner, new ListLogger());

        tracing.Dispose();
        await tracing.DisposeAsync();
        tracing.Dispose();

        Assert.AreEqual(1, inner.DisposeCount);
    }
}
