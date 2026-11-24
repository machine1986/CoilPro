// Port of teslamotors/vehicle-command internal/authentication/signer.go, peer.go and
// internal/dispatcher/dispatcher.go, session.go (Apache-2.0).

using System.Security.Cryptography;
using System.Text;
using Coil.Commands.Internal.Crypto;
using Google.Protobuf;
using Signatures;
using UniversalMessage;

namespace Coil.Commands.Internal.Protocol;

/// <summary>
/// C# port of the Vehicle Command Protocol from teslamotors/vehicle-command (Apache-2.0),
/// <c>internal/authentication</c> and <c>internal/dispatcher</c>.
/// </summary>
/// <remarks>Not thread-safe per session: the orchestrator serializes commands per VIN.</remarks>
internal sealed class VehicleProtocol : IVehicleProtocol
{
    /// <summary>Length of routing addresses and message uuids.</summary>
    internal const int AddressLength = 16;

    /// <summary>Length of the vehicle's session epoch.</summary>
    internal const int EpochLength = 16;

    /// <summary><c>RoutableMessage.flags</c> of every signed command: <c>1 &lt;&lt; FLAG_ENCRYPT_RESPONSE</c>.</summary>
    internal const uint CommandFlags = 1u << (int)Flags.FlagEncryptResponse;

    /// <summary>Session info received later than this after the request is discarded.</summary>
    internal static readonly TimeSpan MaxSessionInfoLatency = TimeSpan.FromSeconds(10);

    private const uint SupportsDetachedFlag = 1u << (int)SessionInfoFlags.UpportsDetachedEncryption;

    private readonly ECDiffieHellman _key;
    private readonly byte[] _publicKey;
    private readonly TimeProvider _timeProvider;
    private readonly IRandomSource _random;
    private readonly TimeSpan _messageLifetime;

    /// <param name="keyPair">Our P-256 key pair (the one registered with Tesla and paired with the vehicle).</param>
    /// <param name="timeProvider">Clock for expiry and session-info latency checks.</param>
    /// <param name="random">Random source for uuids, routing addresses and AES-GCM nonces.</param>
    /// <param name="messageLifetime">Lifetime written into each signed message (expiry = now + lifetime on the vehicle clock).</param>
    public VehicleProtocol(TeslaKeyPair keyPair, TimeProvider timeProvider, IRandomSource random, TimeSpan messageLifetime)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(random);
        if (messageLifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(messageLifetime), messageLifetime, "Message lifetime must be positive.");
        }

        _key = ECDiffieHellman.Create(keyPair.Key.ExportParameters(includePrivateParameters: true));
        _publicKey = keyPair.ExportPublicKeyUncompressedPoint();
        _timeProvider = timeProvider;
        _random = random;
        _messageLifetime = messageLifetime;
    }

    /// <inheritdoc />
    public RoutableMessage CreateSessionInfoRequest(Domain domain)
    {
        ThrowIfUnsupportedDomain(domain);
        var routingAddress = RandomBytes(AddressLength);
        var uuid = RandomBytes(AddressLength);
        return new RoutableMessage
        {
            ToDestination = new Destination { Domain = domain },
            FromDestination = new Destination { RoutingAddress = ByteString.CopyFrom(routingAddress) },
            SessionInfoRequest = new SessionInfoRequest { PublicKey = ByteString.CopyFrom(_publicKey) },
            Uuid = ByteString.CopyFrom(uuid),
        };
    }

    /// <inheritdoc />
    public VehicleSession EstablishSession(string vin, RoutableMessage request, RoutableMessage reply, DateTimeOffset requestSentAt)
    {
        var vinBytes = EncodeVin(vin);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(reply);
        if (request.ToDestination?.SubDestinationCase != Destination.SubDestinationOneofCase.Domain ||
            request.FromDestination?.SubDestinationCase != Destination.SubDestinationOneofCase.RoutingAddress)
        {
            throw new ArgumentException("Not a session-info request created by this protocol.", nameof(request));
        }

        var domain = request.ToDestination.Domain;
        var now = _timeProvider.GetUtcNow();

        if (CheckAddressing(domain, request.FromDestination.RoutingAddress.Span, request.Uuid.Span, reply) is { } addressingError)
        {
            throw new TeslaCommandException($"Session-info reply from {domain} does not match the request: {addressingError}", vin);
        }

        ThrowIfVehicleError(vin, reply);

        if (reply.PayloadCase != RoutableMessage.PayloadOneofCase.SessionInfo)
        {
            throw new TeslaCommandException($"Session-info reply from {domain} carries no session info.", vin);
        }

        if (now - requestSentAt > MaxSessionInfoLatency)
        {
            throw new TeslaCommandException(
                $"Session info from {domain} arrived more than {MaxSessionInfoLatency.TotalSeconds:0} s after the request and was discarded.", vin);
        }

        if (reply.SignatureData?.SigTypeCase != SignatureData.SigTypeOneofCase.SessionInfoTag)
        {
            throw new TeslaCommandException($"Session info from {domain} is not authenticated (missing tag).", vin);
        }

        var info = ParseSessionInfo(vin, reply.SessionInfo);

        SessionKeys keys;
        try
        {
            keys = SessionKeys.Derive(_key, info.PublicKey.Span);
        }
        catch (CryptographicException ex)
        {
            throw new TeslaCommandException($"Session info from {domain} carries an invalid vehicle public key.", vin, innerException: ex);
        }

        var expectedTag = keys.SessionInfoTag(vinBytes, reply.RequestUuid.Span, reply.SessionInfo.Span);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, reply.SignatureData.SessionInfoTag.Tag.Span))
        {
            throw new TeslaCommandException($"Session info from {domain} failed authentication (invalid tag).", vin);
        }

        if (info.Epoch.Length != EpochLength)
        {
            throw new TeslaCommandException($"Session info from {domain} has an invalid epoch length {info.Epoch.Length}.", vin);
        }

        return new VehicleSession(
            domain,
            keys,
            info.PublicKey.ToByteArray(),
            info.Epoch.ToByteArray(),
            info.Counter,
            info.ClockTime,
            now - TimeSpan.FromSeconds(info.ClockTime))
        {
            SupportsDetached = (info.Flags & SupportsDetachedFlag) != 0,
            Handle = info.Handle,
        };
    }

    /// <inheritdoc />
    public bool TryUpdateSession(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply)
    {
        ArgumentNullException.ThrowIfNull(session);
        var vinBytes = EncodeVin(vin);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(reply);

        if (reply.PayloadCase != RoutableMessage.PayloadOneofCase.SessionInfo ||
            reply.SignatureData?.SigTypeCase != SignatureData.SigTypeOneofCase.SessionInfoTag ||
            request.Domain != session.Domain ||
            CheckAddressing(request.Domain, request.RoutingAddress, request.Uuid, reply) is not null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        if (now - request.SignedAt > MaxSessionInfoLatency)
        {
            return false;
        }

        var expectedTag = session.Keys.SessionInfoTag(vinBytes, reply.RequestUuid.Span, reply.SessionInfo.Span);
        if (!CryptographicOperations.FixedTimeEquals(expectedTag, reply.SignatureData.SessionInfoTag.Tag.Span))
        {
            return false;
        }

        SessionInfo info;
        try
        {
            info = SessionInfo.Parser.ParseFrom(reply.SessionInfo);
        }
        catch (InvalidProtocolBufferException)
        {
            return false;
        }

        if (info.Status != Session_Info_Status.Ok ||
            info.Epoch.Length != EpochLength ||
            !info.PublicKey.Span.SequenceEqual(session.VehiclePublicKey))
        {
            return false;
        }

        // Adopt only newer information (new epoch, or the vehicle clock did not move backwards), and never lower
        // the counter: re-using a counter value would make the vehicle reject the next command.
        if (!info.Epoch.Span.SequenceEqual(session.Epoch) || session.ClockTime <= info.ClockTime)
        {
            if (session.Counter < info.Counter)
            {
                session.Counter = info.Counter;
            }

            session.Epoch = info.Epoch.ToByteArray();
            session.ClockTime = info.ClockTime;
            session.TimeZero = now - TimeSpan.FromSeconds(info.ClockTime);
        }

        return true;
    }

    /// <inheritdoc />
    public PreparedCommand Sign(VehicleSession session, string vin, ReadOnlyMemory<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(session);
        var vinBytes = EncodeVin(vin);
        if (session.Counter == uint.MaxValue)
        {
            throw new TeslaCommandException("The session's anti-replay counter is exhausted; a new session handshake is required.", vin);
        }

        var domain = session.Domain;
        var now = _timeProvider.GetUtcNow();
        var expiresAt = ExpiresAt(session, now);
        var counter = ++session.Counter;

        var message = new RoutableMessage
        {
            ToDestination = new Destination { Domain = domain },
            FromDestination = new Destination { RoutingAddress = ByteString.CopyFrom(RandomBytes(AddressLength)) },
            Uuid = ByteString.CopyFrom(RandomBytes(AddressLength)),
            Flags = CommandFlags,
        };
        var signatureData = new SignatureData
        {
            SignerIdentity = new KeyIdentity { PublicKey = ByteString.CopyFrom(_publicKey) },
        };

        // Tesla's HTTP flow authorizes with HMAC and upgrades to detached encryption when the vehicle advertises
        // it: the command type stays readable for Fleet API scope checks, only its parameters are encrypted.
        if (session.SupportsDetached &&
            NestedMessage.TrySplit(payload.Span, DetachedDepth(domain), out var prefix, out var innermost))
        {
            var associatedData = MessageAuthentication
                .CommandMetadata(SignatureType.AesGcmDetached, domain, vinBytes, session.Epoch, expiresAt, counter, CommandFlags)
                .Add(Tag.CommandPrefix, prefix)
                .Sha256Checksum();
            var nonce = RandomBytes(SessionKeys.NonceLength);
            var (ciphertext, tag) = session.Keys.Encrypt(nonce, innermost, associatedData);

            message.ProtobufMessageAsBytes = ByteString.CopyFrom(ciphertext);
            signatureData.AESGCMDetachedData = new AES_GCM_Detached_Signature_Data
            {
                Nonce = ByteString.CopyFrom(nonce),
                Counter = counter,
                ExpiresAt = expiresAt,
                Tag = ByteString.CopyFrom(tag),
                Prefix = ByteString.CopyFrom(prefix),
            };
        }
        else
        {
            var tag = MessageAuthentication
                .CommandMetadata(SignatureType.HmacPersonalized, domain, vinBytes, session.Epoch, expiresAt, counter, CommandFlags)
                .HmacChecksum(session.Keys.CommandKey, payload.Span);

            message.ProtobufMessageAsBytes = ByteString.CopyFrom(payload.Span);
            signatureData.HMACPersonalizedData = new HMAC_Personalized_Signature_Data
            {
                Epoch = ByteString.CopyFrom(session.Epoch),
                Counter = counter,
                ExpiresAt = expiresAt,
                Tag = ByteString.CopyFrom(tag),
            };
        }

        message.SignatureData = signatureData;
        return new PreparedCommand(message, domain, MessageAuthentication.RequestHash(message), counter, now);
    }

    /// <inheritdoc />
    public ReadOnlyMemory<byte> OpenReply(VehicleSession session, string vin, PreparedCommand request, RoutableMessage reply)
    {
        ArgumentNullException.ThrowIfNull(session);
        var vinBytes = EncodeVin(vin);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(reply);
        if (request.Domain != session.Domain)
        {
            throw new ArgumentException($"The command was signed for {request.Domain}, not for the session's {session.Domain}.", nameof(request));
        }

        if (CheckAddressing(request.Domain, request.RoutingAddress, request.Uuid, reply) is { } addressingError)
        {
            throw new TeslaCommandException($"Reply from {request.Domain} does not match the command: {addressingError}", vin);
        }

        var payload = reply.PayloadCase == RoutableMessage.PayloadOneofCase.ProtobufMessageAsBytes
            ? reply.ProtobufMessageAsBytes.Memory
            : ReadOnlyMemory<byte>.Empty;

        if (reply.SignatureData?.SigTypeCase == SignatureData.SigTypeOneofCase.AESGCMResponseData)
        {
            var encryption = reply.SignatureData.AESGCMResponseData;
            var associatedData = MessageAuthentication.ResponseAssociatedData(
                reply.FromDestination.Domain,
                vinBytes,
                encryption.Counter,
                reply.Flags,
                request.RequestHash,
                reply.SignedMessageStatus?.SignedMessageFault ?? MessageFault_E.RrorNone);
            try
            {
                payload = session.Keys.Decrypt(encryption.Nonce.Span, payload.Span, encryption.Tag.Span, associatedData);
            }
            catch (CryptographicException ex)
            {
                throw new TeslaCommandException($"Reply from {request.Domain} failed authentication/decryption.", vin, innerException: ex);
            }

            if (!request.ResponseWindow.Update(encryption.Counter))
            {
                throw new TeslaCommandException($"Reply from {request.Domain} repeats response counter {encryption.Counter} (possible replay).", vin);
            }
        }

        ThrowIfVehicleError(vin, reply);
        return payload;
    }

    /// <summary>Vehicle-clock expiry: seconds since the epoch start at <c>now + lifetime</c>.</summary>
    private uint ExpiresAt(VehicleSession session, DateTimeOffset now)
    {
        var seconds = (now + _messageLifetime - session.TimeZero).Ticks / TimeSpan.TicksPerSecond;
        return seconds <= 0 ? 0 : seconds >= uint.MaxValue ? uint.MaxValue : (uint)seconds;
    }

    private static int DetachedDepth(Domain domain) => domain == Domain.Infotainment
        ? 2 // CarServer.Action -> <service> -> <command>
        : 1; // VCSEC.UnsignedMessage -> <command>

    /// <summary>
    /// Checks that <paramref name="reply"/> comes from <paramref name="domain"/>, is addressed to our routing address,
    /// and (Infotainment: required; VCSEC: if present) echoes our uuid. Returns an error description, or null.
    /// </summary>
    private static string? CheckAddressing(Domain domain, ReadOnlySpan<byte> routingAddress, ReadOnlySpan<byte> uuid, RoutableMessage reply)
    {
        if (reply.FromDestination?.SubDestinationCase != Destination.SubDestinationOneofCase.Domain ||
            reply.FromDestination.Domain != domain)
        {
            return "wrong source domain";
        }

        if (reply.ToDestination?.SubDestinationCase != Destination.SubDestinationOneofCase.RoutingAddress ||
            !reply.ToDestination.RoutingAddress.Span.SequenceEqual(routingAddress))
        {
            return "wrong routing address";
        }

        var requestUuid = reply.RequestUuid.Span;
        if (domain == Domain.Infotainment || !requestUuid.IsEmpty)
        {
            if (!requestUuid.SequenceEqual(uuid))
            {
                return "wrong request uuid";
            }
        }

        return null;
    }

    /// <summary>
    /// Raises what the vehicle reported in the envelope (port of Go's <c>protocol.GetError</c>): a message fault,
    /// a session-info status other than OK, or operation status WAIT (reported as <see cref="TeslaMessageFault.Busy"/>).
    /// </summary>
    private static void ThrowIfVehicleError(string vin, RoutableMessage reply)
    {
        var status = reply.SignedMessageStatus;
        var fault = status?.SignedMessageFault ?? MessageFault_E.RrorNone;
        if (fault != MessageFault_E.RrorNone)
        {
            var publicFault = (TeslaMessageFault)(int)fault;
            if (fault == MessageFault_E.RrorUnknownKeyId)
            {
                throw new TeslaKeyNotPairedException(vin, publicFault);
            }

            throw new TeslaCommandException($"Vehicle {vin} rejected the message: {publicFault}.", vin, publicFault);
        }

        if (reply.PayloadCase == RoutableMessage.PayloadOneofCase.SessionInfo)
        {
            var info = ParseSessionInfo(vin, reply.SessionInfo);
            if (info.Status == Session_Info_Status.KeyNotOnWhitelist)
            {
                throw new TeslaKeyNotPairedException(vin);
            }

            if (info.Status != Session_Info_Status.Ok)
            {
                throw new TeslaCommandException($"Vehicle {vin} reported unknown session status {(int)info.Status}.", vin);
            }
        }

        switch (status?.OperationStatus ?? OperationStatus_E.OperationstatusOk)
        {
            case OperationStatus_E.OperationstatusOk:
            case OperationStatus_E.Rror: // Go treats ERROR without a fault as no protocol error; the payload explains.
                return;
            case OperationStatus_E.OperationstatusWait:
                throw new TeslaCommandException($"Vehicle {vin} is busy or finishing wake-up.", vin, TeslaMessageFault.Busy);
            default:
                throw new TeslaCommandException($"Vehicle {vin} reported unknown operation status {(int)status!.OperationStatus}.", vin);
        }
    }

    private static SessionInfo ParseSessionInfo(string vin, ByteString encoded)
    {
        try
        {
            return SessionInfo.Parser.ParseFrom(encoded);
        }
        catch (InvalidProtocolBufferException ex)
        {
            throw new TeslaCommandException("Vehicle sent malformed session info.", vin, innerException: ex);
        }
    }

    private static byte[] EncodeVin(string vin)
    {
        ArgumentException.ThrowIfNullOrEmpty(vin);
        var bytes = Encoding.UTF8.GetBytes(vin);
        if (bytes.Length > 255)
        {
            throw new ArgumentException("VIN is too long.", nameof(vin));
        }

        return bytes;
    }

    private static void ThrowIfUnsupportedDomain(Domain domain)
    {
        if (domain is not (Domain.Infotainment or Domain.VehicleSecurity))
        {
            throw new ArgumentOutOfRangeException(nameof(domain), domain, "Only the Infotainment and VCSEC domains accept commands.");
        }
    }

    private byte[] RandomBytes(int length)
    {
        var buffer = new byte[length];
        _random.Fill(buffer);
        return buffer;
    }
}
