using CoilPro.Commands.Internal.Protocol;
using UniversalMessage;

namespace CoilPro.Commands.Tests.Protocol.TestSupport;

/// <summary>
/// Wires a <see cref="VehicleProtocol"/> (protocol.md client key, manual clock, deterministic randomness) to a
/// <see cref="FakeVehicle"/> for round-trip tests.
/// </summary>
internal sealed class ProtocolHarness : IDisposable
{
    public ProtocolHarness(Domain domain, bool detached = false, FakeVehicle? vehicle = null)
    {
        Time = new ManualTimeProvider();
        Random = new QueuedRandomSource();
        KeyPair = TestKeys.ClientKeyPair();
        Protocol = new VehicleProtocol(KeyPair, Time, Random, TimeSpan.FromSeconds(5));
        Vehicle = vehicle ?? FakeVehicle.FromProtocolDoc(domain, Time);
        Vehicle.SupportsDetached = detached;
    }

    public ManualTimeProvider Time { get; }

    public QueuedRandomSource Random { get; }

    public TeslaKeyPair KeyPair { get; }

    public VehicleProtocol Protocol { get; }

    public FakeVehicle Vehicle { get; }

    public Domain Domain => Vehicle.Domain;

    /// <summary>Performs the handshake with <see cref="Vehicle"/>.</summary>
    public VehicleSession Establish()
    {
        var request = Protocol.CreateSessionInfoRequest(Domain);
        var reply = Vehicle.AnswerSessionInfoRequest(request);
        return Protocol.EstablishSession(Vehicle.Vin, request, reply, Time.GetUtcNow());
    }

    public void Dispose()
    {
        Vehicle.Dispose();
        KeyPair.Dispose();
    }
}
