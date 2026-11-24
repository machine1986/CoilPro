using Coil.Commands.Internal.Transport;
using Coil.Commands.Tests.Protocol.TestSupport;
using Google.Protobuf;
using UniversalMessage;
using Cs = global::CarServer;

namespace Coil.Commands.Tests.EndToEnd;

/// <summary>
/// Full path through the real components — <see cref="CoilCommands"/> orchestration, <c>CommandPayloads</c>,
/// <c>VehicleProtocol</c>, <c>ReplyInterpreter</c> — against <see cref="FakeVehicle"/>, with only the HTTP transport
/// replaced by an in-memory one.
/// </summary>
public sealed class CoilCommandsEndToEndTests : IDisposable
{
    private readonly ManualTimeProvider _time = new();
    private readonly TeslaKeyPair _keyPair = TestKeys.ClientKeyPair();
    private readonly List<FakeVehicle> _vehicles = [];

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FlashLights_IsSignedVerifiedByTheVehicle_AndSucceeds(bool detached)
    {
        var infotainment = Vehicle(Domain.Infotainment, detached);
        var transport = new VehicleTransport(infotainment);
        var commands = Create(transport);

        var result = await commands.FlashLightsAsync(TestKeys.Vin);

        Assert.True(result.Success);
        Assert.Equal(1, transport.Handshakes);
        var executed = Assert.Single(transport.ExecutedPayloads);
        Assert.Equal("1203D20100", Convert.ToHexString(executed));
    }

    [Fact]
    public async Task SecondCommand_ReusesTheSession()
    {
        var transport = new VehicleTransport(Vehicle(Domain.Infotainment, detached: true));
        var commands = Create(transport);

        await commands.FlashLightsAsync(TestKeys.Vin);
        await commands.HonkHornAsync(TestKeys.Vin);

        Assert.Equal(1, transport.Handshakes);
        Assert.Equal(2, transport.ExecutedPayloads.Count);
    }

    [Fact]
    public async Task VehicleRefusal_IsReturnedAsResult()
    {
        var transport = new VehicleTransport(Vehicle(Domain.Infotainment, detached: true))
        {
            InfotainmentReply = new Cs.Response
            {
                ActionStatus = new Cs.ActionStatus
                {
                    Result = Cs.OperationStatus_E.Rror,
                    ResultReason = new Cs.ResultReason { PlainText = "already_set" },
                },
            },
        };
        var commands = Create(transport);

        var result = await commands.SetChargeLimitAsync(TestKeys.Vin, 80);

        Assert.False(result.Success);
        Assert.Equal("already_set", result.Reason);
        Assert.Equal("12042A020850", Convert.ToHexString(Assert.Single(transport.ExecutedPayloads)));
    }

    [Fact]
    public async Task Lock_GoesToVcsec_AndSucceeds()
    {
        var transport = new VehicleTransport(Vehicle(Domain.Infotainment, detached: true), Vehicle(Domain.VehicleSecurity, detached: false));
        var commands = Create(transport);

        var result = await commands.LockAsync(TestKeys.Vin);

        Assert.True(result.Success);
        Assert.Equal(1, transport.Handshakes);
        Assert.Equal("1001", Convert.ToHexString(Assert.Single(transport.ExecutedPayloads)));
    }

    [Fact]
    public async Task EpochChange_IsRecoveredFromTheFaultReply_WithoutNewHandshake()
    {
        var vehicle = Vehicle(Domain.Infotainment, detached: true);
        var transport = new VehicleTransport(vehicle);
        var commands = Create(transport);
        await commands.FlashLightsAsync(TestKeys.Vin);

        vehicle.RotateEpoch(); // vehicle rebooted its domain: our cached session is stale

        var result = await commands.HonkHornAsync(TestKeys.Vin);

        Assert.True(result.Success);
        Assert.Equal(1, transport.Handshakes);
        Assert.Equal(1, transport.Faults);
        Assert.Equal(2, transport.ExecutedPayloads.Count);
    }

    [Fact]
    public async Task UnknownKey_ThrowsKeyNotPaired()
    {
        var vehicle = Vehicle(Domain.Infotainment, detached: true);
        vehicle.Status = Signatures.Session_Info_Status.KeyNotOnWhitelist;
        var commands = Create(new VehicleTransport(vehicle));

        await Assert.ThrowsAsync<TeslaKeyNotPairedException>(() => commands.FlashLightsAsync(TestKeys.Vin));
    }

    public void Dispose()
    {
        foreach (var vehicle in _vehicles)
        {
            vehicle.Dispose();
        }

        _keyPair.Dispose();
    }

    private FakeVehicle Vehicle(Domain domain, bool detached)
    {
        var vehicle = FakeVehicle.FromProtocolDoc(domain, _time);
        vehicle.SupportsDetached = detached;
        _vehicles.Add(vehicle);
        return vehicle;
    }

    private CoilCommands Create(ISignedCommandTransport transport)
    {
        var options = new CoilCommandsOptions { RetryDelay = TimeSpan.Zero };
        var protocol = new Internal.Protocol.VehicleProtocol(_keyPair, _time, new QueuedRandomSource(), options.MessageLifetime);
        return new CoilCommands(transport, protocol, coilClient: null, options, _time);
    }

    /// <summary>In-memory <see cref="ISignedCommandTransport"/> that hands each message to the addressed fake domain.</summary>
    private sealed class VehicleTransport(params FakeVehicle[] vehicles) : ISignedCommandTransport
    {
        public int Handshakes { get; private set; }

        public int Faults { get; private set; }

        public List<byte[]> ExecutedPayloads { get; } = [];

        public Cs.Response InfotainmentReply { get; init; } = new() { ActionStatus = new Cs.ActionStatus { Result = Cs.OperationStatus_E.OperationstatusOk } };

        public Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct)
        {
            // Round-trip through bytes, exactly as the HTTP transport would.
            message = RoutableMessage.Parser.ParseFrom(message.ToByteArray());
            var vehicle = vehicles.Single(v => v.Domain == message.ToDestination.Domain);

            if (message.PayloadCase == RoutableMessage.PayloadOneofCase.SessionInfoRequest)
            {
                Handshakes++;
                return Task.FromResult(vehicle.AnswerSessionInfoRequest(message));
            }

            byte[] executed;
            try
            {
                executed = vehicle.VerifyCommand(message);
            }
            catch (FakeVehicleFaultException ex)
            {
                Faults++;
                return Task.FromResult(vehicle.CreateFaultReply(message, ex.Fault));
            }

            ExecutedPayloads.Add(executed);
            var reply = vehicle.Domain == Domain.Infotainment ? InfotainmentReply.ToByteArray() : [];
            return Task.FromResult(vehicle.CreateResponse(message, reply, responseCounter: (uint)ExecutedPayloads.Count));
        }
    }
}
