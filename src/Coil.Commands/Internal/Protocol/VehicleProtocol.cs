using UniversalMessage;

namespace Coil.Commands.Internal.Protocol;

/// <summary>
/// C# port of the Vehicle Command Protocol from teslamotors/vehicle-command (Apache-2.0),
/// <c>internal/authentication</c> and <c>internal/dispatcher</c>.
/// </summary>
internal sealed class VehicleProtocol : IVehicleProtocol
{
    /// <param name="keyPair">Our P-256 key pair (the one registered with Tesla and paired with the vehicle).</param>
    /// <param name="timeProvider">Clock for expiry and session-info latency checks.</param>
    /// <param name="random">Random source for uuids, routing addresses and AES-GCM nonces.</param>
    /// <param name="messageLifetime">Lifetime written into each signed message (expiry = now + lifetime on the vehicle clock).</param>
    public VehicleProtocol(TeslaKeyPair keyPair, TimeProvider timeProvider, IRandomSource random, TimeSpan messageLifetime)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(random);
        _ = messageLifetime;
    }

    public RoutableMessage CreateSessionInfoRequest(Domain domain) =>
        throw new NotImplementedException("TODO(M3 protocol): session-info request");

    public VehicleSession EstablishSession(string vin, RoutableMessage request, RoutableMessage reply, DateTimeOffset requestSentAt) =>
        throw new NotImplementedException("TODO(M3 protocol): establish session");

    public bool TryUpdateSession(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply) =>
        throw new NotImplementedException("TODO(M3 protocol): session resync");

    public PreparedCommand Sign(VehicleSession session, string vin, ReadOnlyMemory<byte> payload) =>
        throw new NotImplementedException("TODO(M3 protocol): sign");

    public ReadOnlyMemory<byte> OpenReply(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply) =>
        throw new NotImplementedException("TODO(M3 protocol): open reply");
}
