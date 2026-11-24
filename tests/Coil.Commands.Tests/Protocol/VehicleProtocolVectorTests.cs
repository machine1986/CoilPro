using Coil.Commands.Internal.Protocol;
using Coil.Commands.Tests.Protocol.TestSupport;
using Google.Protobuf;
using Signatures;
using UniversalMessage;
using static Coil.Commands.Tests.Protocol.TestSupport.TestKeys;

namespace Coil.Commands.Tests.Protocol;

/// <summary>
/// End-to-end byte-exact checks of <see cref="VehicleProtocol"/> against the protocol.md handshake example
/// (published key pairs, routing address, challenge, session info and tag).
/// </summary>
public class VehicleProtocolVectorTests
{
    private const string HvacOn = "120452020801";

    [Fact]
    public void SessionInfoRequest_MatchesProtocolDocExample()
    {
        var time = new ManualTimeProvider();
        using var keyPair = ClientKeyPair();
        var protocol = new VehicleProtocol(keyPair, time, new QueuedRandomSource(RoutingAddress, Challenge), TimeSpan.FromSeconds(5));

        var request = protocol.CreateSessionInfoRequest(Domain.Infotainment);

        var expected = new RoutableMessage
        {
            ToDestination = new Destination { Domain = Domain.Infotainment },
            FromDestination = new Destination { RoutingAddress = ByteString.CopyFrom(H(RoutingAddress)) },
            SessionInfoRequest = new SessionInfoRequest { PublicKey = ByteString.CopyFrom(H(ClientPublic)) },
            Uuid = ByteString.CopyFrom(H(Challenge)),
        };
        Assert.Equal(expected, request);
        Assert.Equal(0u, request.Flags);
    }

    [Fact]
    public void EstablishSession_AcceptsProtocolDocReply_ThenSignsHmacByteExact()
    {
        var time = new ManualTimeProvider();
        var random = new QueuedRandomSource(RoutingAddress, Challenge);
        using var keyPair = ClientKeyPair();
        var protocol = new VehicleProtocol(keyPair, time, random, TimeSpan.FromSeconds(5));
        var request = protocol.CreateSessionInfoRequest(Domain.Infotainment);

        var session = protocol.EstablishSession(Vin, request, ProtocolDocReply(), time.GetUtcNow());

        Assert.Equal(Domain.Infotainment, session.Domain);
        Assert.Equal(SessionCounter, session.Counter);
        Assert.Equal(Epoch, X(session.Epoch));
        Assert.Equal(ClockTime, session.ClockTime);
        Assert.Equal(time.GetUtcNow() - TimeSpan.FromSeconds(ClockTime), session.TimeZero);
        Assert.Equal(VehiclePublic, X(session.VehiclePublicKey));
        Assert.Equal(K, X(session.Keys.SharedKey));
        Assert.False(session.SupportsDetached);

        // Signed at the handshake instant with a 5 s lifetime: expires_at = 2650 + 5, counter = 6 + 1,
        // i.e. exactly the protocol.md metadata example (flags = FLAG_ENCRYPT_RESPONSE).
        random.Enqueue("00112233445566778899aabbccddeeff").Enqueue("ffeeddccbbaa99887766554433221100");
        var command = protocol.Sign(session, Vin, H(HvacOn));

        var message = command.Message;
        Assert.Equal(Domain.Infotainment, message.ToDestination.Domain);
        Assert.Equal("00112233445566778899aabbccddeeff", X(message.FromDestination.RoutingAddress.Span));
        Assert.Equal("ffeeddccbbaa99887766554433221100", X(message.Uuid.Span));
        Assert.Equal(2u, message.Flags);
        Assert.Equal(HvacOn, X(message.ProtobufMessageAsBytes.Span));
        Assert.Equal(ClientPublic, X(message.SignatureData.SignerIdentity.PublicKey.Span));

        var hmac = message.SignatureData.HMACPersonalizedData;
        Assert.Equal(Epoch, X(hmac.Epoch.Span));
        Assert.Equal(7u, hmac.Counter);
        Assert.Equal(2655u, hmac.ExpiresAt);
        Assert.Equal("f3c24554a00e42160e6acef6dd4f1c8ef9c30ed6391ec818d75663bee00a47f0", X(hmac.Tag.Span));

        Assert.Equal("08f3c24554a00e42160e6acef6dd4f1c8ef9c30ed6391ec818d75663bee00a47f0", X(command.RequestHash));
        Assert.Equal(7u, command.Counter);
        Assert.Equal(7u, session.Counter);
    }

    [Fact]
    public void Sign_Detached_IsByteExact_AndVerifiedByVehicle()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment, detached: true);
        var session = harness.Establish();
        Assert.True(session.SupportsDetached);

        harness.Random
            .Enqueue("00112233445566778899aabbccddeeff")
            .Enqueue("ffeeddccbbaa99887766554433221100")
            .Enqueue("dbf79447fa156674dae1caed");
        var command = harness.Protocol.Sign(session, Vin, H(HvacOn));

        // Metadata 00010a 010103 0211<VIN> 0310<epoch> 040400000a5f 050400000007 070400000002 0a0412025200 ff.
        // Derived from the protocol.md session (not an upstream vector).
        var detached = command.Message.SignatureData.AESGCMDetachedData;
        Assert.Equal("dbf79447fa156674dae1caed", X(detached.Nonce.Span));
        Assert.Equal(7u, detached.Counter);
        Assert.Equal(2655u, detached.ExpiresAt);
        Assert.Equal("12025200", X(detached.Prefix.Span));
        Assert.Equal("2206", X(command.Message.ProtobufMessageAsBytes.Span));
        Assert.Equal("31eadeb53dcae49f1b46daff5eec1aa5", X(detached.Tag.Span));
        Assert.Equal("0a31eadeb53dcae49f1b46daff5eec1aa5", X(command.RequestHash));

        Assert.Equal(HvacOn, X(harness.Vehicle.VerifyCommand(command.Message)));
    }

    private static RoutableMessage ProtocolDocReply() => new()
    {
        ToDestination = new Destination { RoutingAddress = ByteString.CopyFrom(H(RoutingAddress)) },
        FromDestination = new Destination { Domain = Domain.Infotainment },
        SignatureData = new SignatureData
        {
            SessionInfoTag = new HMAC_Signature_Data { Tag = ByteString.CopyFrom(H(SessionInfoTag)) },
        },
        SessionInfo = ByteString.CopyFrom(H(EncodedSessionInfo)),
        RequestUuid = ByteString.CopyFrom(H(Challenge)),
    };
}
