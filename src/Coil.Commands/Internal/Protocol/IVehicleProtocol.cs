using UniversalMessage;

namespace Coil.Commands.Internal.Protocol;

/// <summary>
/// The cryptographic protocol layer: session handshake, signing, reply authentication and decryption.
/// Pure and synchronous — no I/O. The orchestrator (<see cref="CoilCommands"/>) drives it and owns transport,
/// retries and the session cache.
/// </summary>
/// <remarks>
/// FROZEN CONTRACT (M3 stage 0). Implementation: <see cref="VehicleProtocol"/>. Spec: <c>docs/m3-command-protocol.md</c>.
/// </remarks>
internal interface IVehicleProtocol
{
    /// <summary>
    /// Builds the unsigned session-info request for <paramref name="domain"/>: random 16-byte <c>uuid</c>, random
    /// 16-byte <c>from_destination.routing_address</c>, <c>session_info_request.public_key</c> = our 65-byte point.
    /// </summary>
    RoutableMessage CreateSessionInfoRequest(Domain domain);

    /// <summary>
    /// Authenticates the session info in <paramref name="reply"/> (HMAC with the "session info" sub-key over
    /// the metadata incl. VIN and the request uuid as challenge; constant-time compare; reject replies older than
    /// 10 s after <paramref name="requestSentAt"/>) and creates the session.
    /// </summary>
    /// <exception cref="TeslaKeyNotPairedException">Session status <c>KEY_NOT_ON_WHITELIST</c> or fault <c>UNKNOWN_KEY_ID</c>.</exception>
    /// <exception cref="TeslaCommandException">Missing/invalid tag, too late, malformed reply, or another fault.</exception>
    VehicleSession EstablishSession(string vin, RoutableMessage request, RoutableMessage reply, DateTimeOffset requestSentAt);

    /// <summary>
    /// Adopts the authenticated session info the vehicle attached to a reply (typically a fault reply) for
    /// <paramref name="request"/>: verify the tag, never lower the counter, reject a changed vehicle public key.
    /// Returns false when the reply carries no (valid) session info; the session is then unchanged.
    /// </summary>
    bool TryUpdateSession(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply);

    /// <summary>
    /// Wraps <paramref name="payload"/> (a serialized <c>CarServer.Action</c> or <c>VCSEC.UnsignedMessage</c>) in a
    /// <see cref="RoutableMessage"/> for <see cref="VehicleSession.Domain"/>, sets <c>FLAG_ENCRYPT_RESPONSE</c>, and
    /// signs it: <c>AES_GCM_DETACHED</c> if the session advertises it, else <c>HMAC_PERSONALIZED</c>. Every call
    /// consumes a fresh counter and computes a fresh expiry, so a retry must call this again.
    /// </summary>
    PreparedCommand Sign(VehicleSession session, string vin, ReadOnlyMemory<byte> payload);

    /// <summary>
    /// Validates that <paramref name="reply"/> answers <paramref name="request"/> (domain, routing address,
    /// request uuid where echoed), raises a vehicle fault from <c>signedMessageStatus</c>, and decrypts
    /// <c>AES_GCM_Response_data</c> if present (rejecting a repeated response counter).
    /// Returns the plaintext payload (<c>CarServer.Response</c> / <c>VCSEC.FromVCSECMessage</c> bytes; may be empty).
    /// </summary>
    /// <exception cref="TeslaKeyNotPairedException">Fault <c>UNKNOWN_KEY_ID</c>.</exception>
    /// <exception cref="TeslaCommandException">Any other fault (with <see cref="TeslaCommandException.Fault"/> set), or a reply that fails validation/decryption.</exception>
    ReadOnlyMemory<byte> OpenReply(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply);
}

/// <summary>
/// State of an authenticated session with one vehicle domain (epoch, counter, clock offset, derived keys,
/// detached-encryption support). Opaque to the orchestrator except for <see cref="Domain"/>.
/// </summary>
/// <remarks>Owned by the protocol layer; members beyond <see cref="Domain"/> may be added freely.</remarks>
internal sealed partial class VehicleSession
{
    /// <summary>The vehicle domain this session belongs to.</summary>
    public Domain Domain { get; }

    internal VehicleSession(Domain domain)
    {
        Domain = domain;
    }
}

/// <summary>
/// A signed command ready to send, plus what is needed to authenticate and decrypt its reply
/// (request hash, uuid, routing address, response-counter window).
/// </summary>
/// <remarks>Owned by the protocol layer; members beyond <see cref="Message"/> may be added freely.</remarks>
internal sealed partial class PreparedCommand
{
    /// <summary>The message to POST to <c>signed_command</c>.</summary>
    public RoutableMessage Message { get; }

    internal PreparedCommand(RoutableMessage message)
    {
        Message = message;
    }
}
