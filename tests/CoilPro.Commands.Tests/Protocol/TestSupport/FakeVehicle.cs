// Port of teslamotors/vehicle-command internal/authentication/verifier.go (Apache-2.0), reduced to a test double.

using System.Security.Cryptography;
using System.Text;
using CoilPro.Commands.Internal.Crypto;
using CoilPro.Commands.Internal.Protocol;
using Google.Protobuf;
using Signatures;
using UniversalMessage;

namespace CoilPro.Commands.Tests.Protocol.TestSupport;

/// <summary>A fault the <see cref="FakeVehicle"/> raised while verifying a command.</summary>
internal sealed class FakeVehicleFaultException(MessageFault_E fault, string message) : Exception($"{fault}: {message}")
{
    public MessageFault_E Fault { get; } = fault;
}

/// <summary>
/// Vehicle-side test double for one domain (VCSEC or Infotainment): answers session-info requests with correctly
/// tagged <see cref="SessionInfo"/>, verifies signed commands (<c>HMAC_PERSONALIZED</c> and <c>AES_GCM_DETACHED</c>)
/// like the vehicle's verifier, and produces encrypted responses and fault replies.
/// </summary>
/// <remarks>
/// The vehicle clock is <see cref="ClockTime"/> = seconds since <see cref="TimeZero"/> on the shared
/// <see cref="TimeProvider"/>, so advancing a <see cref="ManualTimeProvider"/> ages both sides consistently.
/// </remarks>
internal sealed class FakeVehicle : IDisposable
{
    private const uint MaxSecondsWithoutCounter = 30;
    private const uint EpochLengthSeconds = 1u << 30;

    private readonly TimeProvider _time;
    private readonly ECDiffieHellman _key;
    private readonly byte[] _vin;
    private ulong _window;

    public FakeVehicle(Domain domain, TimeProvider time, ECDiffieHellman? key = null, string vin = TestKeys.Vin)
    {
        Domain = domain;
        _time = time;
        _key = key ?? ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        _vin = Encoding.UTF8.GetBytes(vin);
        Vin = vin;
        PublicKey = SessionKeys.ExportPublicKey(_key);
        Epoch = RandomNumberGenerator.GetBytes(16);
        TimeZero = time.GetUtcNow();
    }

    /// <summary>A vehicle using the published protocol.md vehicle key and the protocol.md session state.</summary>
    public static FakeVehicle FromProtocolDoc(Domain domain, TimeProvider time) =>
        new(domain, time, TestKeys.EcdhFromScalar(TestKeys.VehicleScalar))
        {
            Counter = TestKeys.SessionCounter,
            Epoch = TestKeys.H(TestKeys.Epoch),
            ClockTime = TestKeys.ClockTime,
        };

    public Domain Domain { get; }

    public string Vin { get; }

    /// <summary>The domain's 65-byte public key.</summary>
    public byte[] PublicKey { get; }

    /// <summary>Highest counter accepted so far; reported in session info.</summary>
    public uint Counter { get; set; }

    /// <summary>16-byte session epoch.</summary>
    public byte[] Epoch { get; set; }

    /// <summary>Local instant at which the vehicle epoch started.</summary>
    public DateTimeOffset TimeZero { get; set; }

    /// <summary>Vehicle clock: whole seconds since <see cref="TimeZero"/>. Setting it moves <see cref="TimeZero"/>.</summary>
    public uint ClockTime
    {
        get => (uint)((_time.GetUtcNow() - TimeZero).Ticks / TimeSpan.TicksPerSecond);
        set => TimeZero = _time.GetUtcNow() - TimeSpan.FromSeconds(value);
    }

    /// <summary><c>SessionInfo.flags</c>; bit 0 advertises detached encryption.</summary>
    public uint SessionFlags { get; set; }

    public bool SupportsDetached
    {
        get => (SessionFlags & 1) != 0;
        set => SessionFlags = value ? SessionFlags | 1 : SessionFlags & ~1u;
    }

    public Session_Info_Status Status { get; set; } = Session_Info_Status.Ok;

    public uint Handle { get; set; }

    /// <summary>Whether replies echo the request's uuid in <c>request_uuid</c> (and bind it into the session-info tag).</summary>
    public bool EchoRequestUuid { get; set; } = true;

    /// <summary><c>RoutableMessage.flags</c> set on responses (authenticated in the response metadata).</summary>
    public uint ResponseFlags { get; set; }

    /// <summary>Starts a new epoch (as after a vehicle reboot): random epoch, counter 0, clock 0.</summary>
    public void RotateEpoch()
    {
        Epoch = RandomNumberGenerator.GetBytes(16);
        Counter = 0;
        _window = 0;
        ClockTime = 0;
    }

    public SessionKeys KeysFor(ReadOnlySpan<byte> clientPublicKey) => SessionKeys.Derive(_key, clientPublicKey);

    public byte[] EncodeSessionInfo() => new SessionInfo
    {
        Counter = Counter,
        PublicKey = ByteString.CopyFrom(PublicKey),
        Epoch = ByteString.CopyFrom(Epoch),
        ClockTime = ClockTime,
        Status = Status,
        Handle = Handle,
        Flags = SessionFlags,
    }.ToByteArray();

    /// <summary>Answers a handshake request (<c>session_info_request</c>) with tagged session info.</summary>
    public RoutableMessage AnswerSessionInfoRequest(RoutableMessage request)
    {
        if (request.PayloadCase != RoutableMessage.PayloadOneofCase.SessionInfoRequest)
        {
            throw new ArgumentException("Not a session-info request.", nameof(request));
        }

        var reply = ReplyEnvelope(request);
        AttachSessionInfo(reply, KeysFor(request.SessionInfoRequest.PublicKey.Span), request.Uuid.Span);
        return reply;
    }

    /// <summary>
    /// Verifies a signed command like the vehicle does (domain, epoch, expiry, counter window, tag/decryption) and
    /// returns the full plaintext command. Accepted counters are recorded.
    /// </summary>
    /// <exception cref="FakeVehicleFaultException">The command fails verification.</exception>
    public byte[] VerifyCommand(RoutableMessage command)
    {
        var signature = command.SignatureData ?? throw Fault(MessageFault_E.RrorBadParameter, "signature data missing");
        if (signature.SignerIdentity?.IdentityTypeCase != KeyIdentity.IdentityTypeOneofCase.PublicKey)
        {
            throw Fault(MessageFault_E.RrorUnknownKeyId, "no signer public key");
        }

        var keys = KeysFor(signature.SignerIdentity.PublicKey.Span);
        if (command.ToDestination?.SubDestinationCase != Destination.SubDestinationOneofCase.Domain || command.ToDestination.Domain != Domain)
        {
            throw Fault(MessageFault_E.RrorInvalidDomains, "wrong domain");
        }

        uint counter;
        byte[] plaintext;
        switch (signature.SigTypeCase)
        {
            case SignatureData.SigTypeOneofCase.HMACPersonalizedData:
            {
                var data = signature.HMACPersonalizedData;
                counter = data.Counter;
                CheckSessionState(data.Epoch.Span, data.ExpiresAt, data.Counter);
                var expected = CommandMetadataFor(SignatureType.HmacPersonalized, command, data.ExpiresAt, data.Counter)
                    .HmacChecksum(keys.CommandKey, command.ProtobufMessageAsBytes.Span);
                if (!CryptographicOperations.FixedTimeEquals(expected, data.Tag.Span))
                {
                    throw Fault(MessageFault_E.RrorInvalidSignature, "HMAC tag mismatch");
                }

                plaintext = command.ProtobufMessageAsBytes.ToByteArray();
                break;
            }

            case SignatureData.SigTypeOneofCase.AESGCMDetachedData:
            {
                var data = signature.AESGCMDetachedData;
                counter = data.Counter;
                CheckSessionState(default, data.ExpiresAt, data.Counter);
                if (!NestedMessage.TryParsePrefix(data.Prefix.Span, out var fieldNumbers))
                {
                    throw Fault(MessageFault_E.RrorBadParameter, "invalid detached command prefix");
                }

                if (data.Nonce.Length != SessionKeys.NonceLength)
                {
                    throw Fault(MessageFault_E.RrorIvIncorrectLength, "nonce length");
                }

                var associatedData = CommandMetadataFor(SignatureType.AesGcmDetached, command, data.ExpiresAt, data.Counter)
                    .Add(Tag.CommandPrefix, data.Prefix.Span)
                    .Sha256Checksum();
                byte[] inner;
                try
                {
                    inner = keys.Decrypt(data.Nonce.Span, command.ProtobufMessageAsBytes.Span, data.Tag.Span, associatedData);
                }
                catch (CryptographicException)
                {
                    throw Fault(MessageFault_E.RrorInvalidSignature, "AES-GCM authentication failed");
                }

                plaintext = NestedMessage.Marshal(fieldNumbers, inner);
                break;
            }

            default:
                throw Fault(MessageFault_E.RrorBadParameter, $"unsupported signature type {signature.SigTypeCase}");
        }

        if (counter > 0)
        {
            var current = Counter;
            if (!SlidingWindow.TryUpdate(ref current, ref _window, counter))
            {
                throw Fault(MessageFault_E.RrorInvalidTokenOrCounter, $"counter {counter} already used");
            }

            Counter = current;
        }

        return plaintext;
    }

    /// <summary>
    /// Verifies <paramref name="command"/>; on success returns an encrypted response carrying
    /// <paramref name="responsePlaintext"/>, on failure a fault reply with attached session info (like the vehicle).
    /// </summary>
    public RoutableMessage Process(RoutableMessage command, ReadOnlySpan<byte> responsePlaintext, uint responseCounter = 1)
    {
        try
        {
            VerifyCommand(command);
        }
        catch (FakeVehicleFaultException ex)
        {
            return CreateFaultReply(command, ex.Fault);
        }

        return CreateResponse(command, responsePlaintext, responseCounter);
    }

    /// <summary>
    /// Builds a response to <paramref name="command"/>. Encrypted with <c>AES_GCM_Response_data</c> bound to the
    /// command's request hash unless <paramref name="encrypt"/> is false.
    /// </summary>
    public RoutableMessage CreateResponse(
        RoutableMessage command,
        ReadOnlySpan<byte> plaintext,
        uint responseCounter = 1,
        MessageFault_E fault = MessageFault_E.RrorNone,
        bool encrypt = true)
    {
        var reply = ReplyEnvelope(command);
        reply.Flags = ResponseFlags;
        if (fault != MessageFault_E.RrorNone)
        {
            reply.SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.Rror, SignedMessageFault = fault };
        }

        if (!encrypt)
        {
            reply.ProtobufMessageAsBytes = ByteString.CopyFrom(plaintext);
            return reply;
        }

        var keys = KeysFor(command.SignatureData.SignerIdentity.PublicKey.Span);
        var associatedData = MessageAuthentication.ResponseAssociatedData(
            Domain, _vin, responseCounter, reply.Flags, MessageAuthentication.RequestHash(command), fault);
        var nonce = RandomNumberGenerator.GetBytes(SessionKeys.NonceLength);
        var (ciphertext, tag) = keys.Encrypt(nonce, plaintext, associatedData);
        reply.ProtobufMessageAsBytes = ByteString.CopyFrom(ciphertext);
        reply.SignatureData = new SignatureData
        {
            AESGCMResponseData = new AES_GCM_Response_Signature_Data
            {
                Nonce = ByteString.CopyFrom(nonce),
                Counter = responseCounter,
                Tag = ByteString.CopyFrom(tag),
            },
        };
        return reply;
    }

    /// <summary>
    /// Builds an (unencrypted) fault reply, by default with tagged session info attached so the client can resync.
    /// </summary>
    public RoutableMessage CreateFaultReply(RoutableMessage command, MessageFault_E fault, bool attachSessionInfo = true)
    {
        var reply = ReplyEnvelope(command);
        reply.SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.Rror, SignedMessageFault = fault };
        if (attachSessionInfo)
        {
            AttachSessionInfo(reply, KeysFor(command.SignatureData.SignerIdentity.PublicKey.Span), command.Uuid.Span);
        }

        return reply;
    }

    public void Dispose() => _key.Dispose();

    private RoutableMessage ReplyEnvelope(RoutableMessage request)
    {
        var reply = new RoutableMessage
        {
            ToDestination = new Destination { RoutingAddress = request.FromDestination.RoutingAddress },
            FromDestination = new Destination { Domain = Domain },
        };
        if (EchoRequestUuid)
        {
            reply.RequestUuid = request.Uuid;
        }

        return reply;
    }

    private void AttachSessionInfo(RoutableMessage reply, SessionKeys keys, ReadOnlySpan<byte> challenge)
    {
        var encoded = EncodeSessionInfo();
        var tag = keys.SessionInfoTag(_vin, EchoRequestUuid ? challenge : default, encoded);
        reply.SessionInfo = ByteString.CopyFrom(encoded);
        reply.SignatureData = new SignatureData { SessionInfoTag = new HMAC_Signature_Data { Tag = ByteString.CopyFrom(tag) } };
    }

    private Metadata CommandMetadataFor(SignatureType type, RoutableMessage command, uint expiresAt, uint counter) =>
        MessageAuthentication.CommandMetadata(type, command.ToDestination.Domain, _vin, Epoch, expiresAt, counter, command.Flags);

    /// <summary>Port of the verifier's <c>verifySessionInfo</c> checks.</summary>
    private void CheckSessionState(ReadOnlySpan<byte> epoch, uint expiresAt, uint counter)
    {
        if (!epoch.IsEmpty && !epoch.SequenceEqual(Epoch))
        {
            throw Fault(MessageFault_E.RrorIncorrectEpoch, "epoch mismatch");
        }

        var now = ClockTime;
        if (expiresAt != 0 && expiresAt < now)
        {
            throw Fault(MessageFault_E.RrorTimeExpired, $"expired at {expiresAt}, now {now}");
        }

        if (expiresAt > EpochLengthSeconds)
        {
            throw Fault(MessageFault_E.RrorBadParameter, "expiry out of bounds");
        }

        if ((counter == 0 || counter < Counter) && (expiresAt == 0 || expiresAt - now > MaxSecondsWithoutCounter))
        {
            throw Fault(MessageFault_E.RrorTimeToLiveTooLong, "out-of-order message with long lifetime");
        }
    }

    private static FakeVehicleFaultException Fault(MessageFault_E fault, string message) => new(fault, message);
}
