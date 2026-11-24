using CoilPro.Commands.Internal.Crypto;
using CoilPro.Commands.Internal.Protocol;
using CoilPro.Commands.Tests.Protocol.TestSupport;
using Google.Protobuf;
using Signatures;
using UniversalMessage;
using static CoilPro.Commands.Tests.Protocol.TestSupport.TestKeys;

namespace CoilPro.Commands.Tests.Protocol;

/// <summary>Round trips against <see cref="FakeVehicle"/> and the protocol's validation/failure paths.</summary>
public class VehicleProtocolTests
{
    private static readonly byte[] HvacOn = H("120452020801");
    private static readonly byte[] Lock = H("1001"); // VCSEC UnsignedMessage{RKEAction: LOCK} (varint, not nestable)
    private static readonly byte[] ResponsePayload = H("0a020800"); // arbitrary response bytes

    // ---------- round trips ----------

    [Theory]
    [InlineData((int)Domain.Infotainment, false)]
    [InlineData((int)Domain.Infotainment, true)]
    [InlineData((int)Domain.VehicleSecurity, false)]
    public void RoundTrip_EstablishSignVerifyOpen(int domainValue, bool detached)
    {
        var domain = (Domain)domainValue;
        using var harness = new ProtocolHarness(domain, detached);
        var session = harness.Establish();
        var payload = domain == Domain.Infotainment ? HvacOn : Lock;

        for (var i = 1u; i <= 3; i++)
        {
            var command = harness.Protocol.Sign(session, Vin, payload);
            Assert.Equal(
                detached ? SignatureData.SigTypeOneofCase.AESGCMDetachedData : SignatureData.SigTypeOneofCase.HMACPersonalizedData,
                command.Message.SignatureData.SigTypeCase);
            Assert.Equal(SessionCounter + i, command.Counter);

            Assert.Equal(payload, harness.Vehicle.VerifyCommand(command.Message));

            var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);
            Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, command, reply).ToArray());
            harness.Time.Advance(TimeSpan.FromSeconds(1));
        }
    }

    [Fact]
    public void Vcsec_HmacRequestHash_IsTruncatedTo17Bytes()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();

        var command = harness.Protocol.Sign(session, Vin, Lock);

        Assert.Equal(17, command.RequestHash.Length);
        Assert.Equal((byte)SignatureType.HmacPersonalized, command.RequestHash[0]);
        Assert.Equal(command.Message.SignatureData.HMACPersonalizedData.Tag.Span[..16].ToArray(), command.RequestHash[1..]);
    }

    [Fact]
    public void Vcsec_DetachedAdvertised_NestedPayloadUsesDepthOne()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity, detached: true);
        var session = harness.Establish();
        var nested = H("2a020801"); // UnsignedMessage{field 5: {08 01}}

        var command = harness.Protocol.Sign(session, Vin, nested);

        Assert.Equal("2a00", X(command.Message.SignatureData.AESGCMDetachedData.Prefix.Span));
        Assert.Equal(nested, harness.Vehicle.VerifyCommand(command.Message));
    }

    [Fact]
    public void DetachedAdvertised_NonNestedPayload_FallsBackToHmac()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity, detached: true);
        var session = harness.Establish();

        var command = harness.Protocol.Sign(session, Vin, Lock);

        Assert.Equal(SignatureData.SigTypeOneofCase.HMACPersonalizedData, command.Message.SignatureData.SigTypeCase);
        Assert.Equal(Lock, harness.Vehicle.VerifyCommand(command.Message));
    }

    [Fact]
    public void Sign_ExpiryFollowsVehicleClock()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();

        harness.Time.Advance(TimeSpan.FromSeconds(100.7));
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        Assert.Equal(ClockTime + 105, command.Message.SignatureData.HMACPersonalizedData.ExpiresAt);
        Assert.Equal(harness.Time.GetUtcNow(), command.SignedAt);
    }

    [Fact]
    public void Sign_ExpiredCommand_IsRejectedByVehicle()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        harness.Time.Advance(TimeSpan.FromSeconds(6));

        var fault = Assert.Throws<FakeVehicleFaultException>(() => harness.Vehicle.VerifyCommand(command.Message));
        Assert.Equal(MessageFault_E.RrorTimeExpired, fault.Fault);
    }

    [Fact]
    public void Sign_UsesFreshRandomAddressingPerMessage()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();

        var first = harness.Protocol.Sign(session, Vin, HvacOn).Message;
        var second = harness.Protocol.Sign(session, Vin, HvacOn).Message;

        Assert.Equal(16, first.Uuid.Length);
        Assert.Equal(16, first.FromDestination.RoutingAddress.Length);
        Assert.NotEqual(first.Uuid, second.Uuid);
        Assert.NotEqual(first.FromDestination.RoutingAddress, second.FromDestination.RoutingAddress);
    }

    [Fact]
    public void Sign_ExhaustedCounter_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        session.Counter = uint.MaxValue;

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.Sign(session, Vin, HvacOn));
    }

    // ---------- handshake ----------

    [Fact]
    public void Establish_ReadsDetachedFlagAndHandle()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment, detached: true);
        harness.Vehicle.Handle = 42;

        var session = harness.Establish();

        Assert.True(session.SupportsDetached);
        Assert.Equal(42u, session.Handle);
    }

    [Fact]
    public void Establish_TamperedTag_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        var tag = reply.SignatureData.SessionInfoTag.Tag.ToByteArray();
        tag[0] ^= 1;
        reply.SignatureData.SessionInfoTag.Tag = ByteString.CopyFrom(tag);

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_TamperedSessionInfo_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        var info = SessionInfo.Parser.ParseFrom(reply.SessionInfo);
        info.Counter = 1_000; // e.g. a MITM trying to burn counters
        reply.SessionInfo = info.ToByteString();

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_WrongVin_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession("5YJ3000000000000X", request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_MissingTag_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        reply.ClearSubSigData();

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_ReplyLaterThan10Seconds_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var sentAt = harness.Time.GetUtcNow();
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);

        harness.Time.Advance(TimeSpan.FromSeconds(10));
        harness.Protocol.EstablishSession(Vin, request, reply, sentAt); // exactly 10 s is still fine

        harness.Time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, sentAt));
    }

    [Fact]
    public void Establish_KeyNotOnWhitelist_ThrowsKeyNotPaired()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        harness.Vehicle.Status = Session_Info_Status.KeyNotOnWhitelist;

        var ex = Assert.Throws<TeslaKeyNotPairedException>(() => harness.Establish());
        Assert.Equal(Vin, ex.Vin);
    }

    [Fact]
    public void Establish_UnknownKeyIdFault_ThrowsKeyNotPaired()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = new RoutableMessage
        {
            ToDestination = new Destination { RoutingAddress = request.FromDestination.RoutingAddress },
            FromDestination = new Destination { Domain = Domain.Infotainment },
            RequestUuid = request.Uuid,
            SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.Rror, SignedMessageFault = MessageFault_E.RrorUnknownKeyId },
        };

        var ex = Assert.Throws<TeslaKeyNotPairedException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
        Assert.Equal(TeslaMessageFault.UnknownKeyId, ex.Fault);
    }

    [Fact]
    public void Establish_OtherFault_ThrowsWithFault()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        reply.SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.Rror, SignedMessageFault = MessageFault_E.RrorBusy };

        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
        Assert.Equal(TeslaMessageFault.Busy, ex.Fault);
    }

    [Fact]
    public void Establish_WrongRoutingAddressOrDomain_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);

        var wrongAddress = harness.Vehicle.AnswerSessionInfoRequest(request);
        wrongAddress.ToDestination = new Destination { RoutingAddress = ByteString.CopyFrom(new byte[16]) };
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, wrongAddress, harness.Time.GetUtcNow()));

        var wrongDomain = harness.Vehicle.AnswerSessionInfoRequest(request);
        wrongDomain.FromDestination = new Destination { Domain = Domain.VehicleSecurity };
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, wrongDomain, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_Infotainment_RequiresRequestUuidEcho()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        harness.Vehicle.EchoRequestUuid = false;

        Assert.Throws<TeslaCommandException>(() => harness.Establish());
    }

    [Fact]
    public void Establish_Vcsec_WithoutRequestUuid_UsesNoChallenge()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        harness.Vehicle.EchoRequestUuid = false;

        var session = harness.Establish();

        var command = harness.Protocol.Sign(session, Vin, Lock);
        Assert.Equal(Lock, harness.Vehicle.VerifyCommand(command.Message));
        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);
        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, command, reply).ToArray());
    }

    [Fact]
    public void Establish_Vcsec_WrongRequestUuid_Throws()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.VehicleSecurity);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        reply.RequestUuid = ByteString.CopyFrom(new byte[16]);

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void Establish_InvalidVehiclePublicKey_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var request = harness.Protocol.CreateSessionInfoRequest(Domain.Infotainment);
        var reply = harness.Vehicle.AnswerSessionInfoRequest(request);
        var info = SessionInfo.Parser.ParseFrom(reply.SessionInfo);
        info.PublicKey = ByteString.CopyFrom(new byte[65]);
        reply.SessionInfo = info.ToByteString();

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.EstablishSession(Vin, request, reply, harness.Time.GetUtcNow()));
    }

    [Fact]
    public void CreateSessionInfoRequest_RejectsBroadcast()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        Assert.Throws<ArgumentOutOfRangeException>(() => harness.Protocol.CreateSessionInfoRequest(Domain.Broadcast));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveLifetime()
    {
        using var keyPair = ClientKeyPair();
        Assert.Throws<ArgumentOutOfRangeException>(() => new VehicleProtocol(keyPair, TimeProvider.System, new QueuedRandomSource(), TimeSpan.Zero));
    }

    // ---------- replies ----------

    [Fact]
    public void OpenReply_RepeatedResponseCounter_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var first = harness.Vehicle.CreateResponse(command.Message, ResponsePayload, responseCounter: 1);
        var second = harness.Vehicle.CreateResponse(command.Message, ResponsePayload, responseCounter: 2);

        harness.Protocol.OpenReply(session, Vin, command, first);
        harness.Protocol.OpenReply(session, Vin, command, second);

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, first));
    }

    [Fact]
    public void OpenReply_TamperedCiphertextOrMetadata_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var tampered = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);
        var bytes = tampered.ProtobufMessageAsBytes.ToByteArray();
        bytes[0] ^= 1;
        tampered.ProtobufMessageAsBytes = ByteString.CopyFrom(bytes);
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, tampered));

        var flagsChanged = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);
        flagsChanged.Flags = 1; // authenticated in the response metadata
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, flagsChanged));
    }

    [Fact]
    public void OpenReply_ResponseFlagsAreAuthenticated()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        harness.Vehicle.ResponseFlags = 2;
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);

        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, command, reply).ToArray());
    }

    [Fact]
    public void OpenReply_ResponseForAnotherCommand_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var first = harness.Protocol.Sign(session, Vin, HvacOn);
        var second = harness.Protocol.Sign(session, Vin, HvacOn);

        var replyToFirst = harness.Vehicle.CreateResponse(first.Message, ResponsePayload);
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, second, replyToFirst));

        // Even with matching addressing, the request hash binds the response to its request.
        replyToFirst.ToDestination = second.Message.FromDestination;
        replyToFirst.RequestUuid = second.Message.Uuid;
        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, second, replyToFirst));
    }

    [Fact]
    public void OpenReply_Infotainment_MissingRequestUuid_Throws()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload);
        reply.RequestUuid = ByteString.Empty;

        Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, reply));
    }

    [Fact]
    public void OpenReply_UnencryptedReply_ReturnsPayloadAsIs()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, Lock);

        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload, encrypt: false);

        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, command, reply).ToArray());
    }

    [Fact]
    public void OpenReply_EmptyEncryptedPayload_ReturnsEmpty()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, Lock);

        var reply = harness.Vehicle.CreateResponse(command.Message, []);

        Assert.True(harness.Protocol.OpenReply(session, Vin, command, reply).IsEmpty);
    }

    [Theory]
    [InlineData((int)MessageFault_E.RrorBusy, TeslaMessageFault.Busy)]
    [InlineData((int)MessageFault_E.RrorInvalidSignature, TeslaMessageFault.InvalidSignature)]
    [InlineData((int)MessageFault_E.RrorInsufficientPrivileges, TeslaMessageFault.InsufficientPrivileges)]
    [InlineData((int)MessageFault_E.RrorRequiresResponseEncryption, TeslaMessageFault.RequiresResponseEncryption)]
    public void OpenReply_Fault_ThrowsWithMappedFault(int faultValue, TeslaMessageFault expected)
    {
        var fault = (MessageFault_E)faultValue;
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var reply = harness.Vehicle.CreateFaultReply(command.Message, fault);

        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, reply));
        Assert.Equal(expected, ex.Fault);
        Assert.Equal(Vin, ex.Vin);
    }

    [Fact]
    public void OpenReply_AllFaultValuesMapByNumber()
    {
        foreach (var fault in Enum.GetValues<MessageFault_E>())
        {
            Assert.Equal((int)fault, (int)(TeslaMessageFault)(int)fault);
            Assert.True(Enum.IsDefined((TeslaMessageFault)(int)fault));
        }
    }

    [Fact]
    public void OpenReply_UnknownKeyId_ThrowsKeyNotPaired()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, Lock);

        var reply = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorUnknownKeyId, attachSessionInfo: false);

        var ex = Assert.Throws<TeslaKeyNotPairedException>(() => harness.Protocol.OpenReply(session, Vin, command, reply));
        Assert.Equal(TeslaMessageFault.UnknownKeyId, ex.Fault);
    }

    [Fact]
    public void OpenReply_EncryptedFault_IsAuthenticatedThenRaised()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var reply = harness.Vehicle.CreateResponse(command.Message, [], fault: MessageFault_E.RrorTimeExpired);
        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, reply));
        Assert.Equal(TeslaMessageFault.TimeExpired, ex.Fault);

        // The fault code is part of the associated data: stripping it breaks authentication.
        var stripped = harness.Vehicle.CreateResponse(command.Message, [], responseCounter: 2, fault: MessageFault_E.RrorTimeExpired);
        stripped.SignedMessageStatus = null;
        var strippedEx = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, stripped));
        Assert.Null(strippedEx.Fault);
    }

    [Fact]
    public void OpenReply_OperationWait_ThrowsBusy()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload, encrypt: false);
        reply.SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.OperationstatusWait };

        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, reply));
        Assert.Equal(TeslaMessageFault.Busy, ex.Fault);
    }

    [Fact]
    public void OpenReply_OperationErrorWithoutFault_ReturnsPayload()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var reply = harness.Vehicle.CreateResponse(command.Message, ResponsePayload, encrypt: false);
        reply.SignedMessageStatus = new MessageStatus { OperationStatus = OperationStatus_E.Rror };

        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, command, reply).ToArray());
    }

    // ---------- resync after faults ----------

    [Fact]
    public void TryUpdateSession_AfterCounterFault_AdoptsHigherCounter_AndRetrySucceeds()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        harness.Vehicle.Counter = 100; // e.g. another client of the same key advanced the counter

        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var faultReply = harness.Vehicle.Process(command.Message, ResponsePayload);
        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, faultReply));
        Assert.Equal(TeslaMessageFault.InvalidTokenOrCounter, ex.Fault);

        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));
        Assert.Equal(100u, session.Counter);

        var retry = harness.Protocol.Sign(session, Vin, HvacOn);
        Assert.Equal(101u, retry.Counter);
        var reply = harness.Vehicle.Process(retry.Message, ResponsePayload);
        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, retry, reply).ToArray());
    }

    [Fact]
    public void TryUpdateSession_AfterEpochChange_AdoptsNewEpoch_AndRetrySucceeds()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment, detached: true);
        var session = harness.Establish();
        harness.Time.Advance(TimeSpan.FromMinutes(5));
        harness.Vehicle.RotateEpoch(); // vehicle rebooted

        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var faultReply = harness.Vehicle.Process(command.Message, ResponsePayload);
        Assert.Equal(MessageFault_E.RrorInvalidSignature, faultReply.SignedMessageStatus.SignedMessageFault); // detached: epoch only in M

        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));
        Assert.Equal(harness.Vehicle.Epoch, session.Epoch);
        Assert.Equal(SessionCounter + 1, session.Counter); // never lowered to the new epoch's 0
        Assert.Equal(0u, session.ClockTime);
        Assert.Equal(harness.Time.GetUtcNow(), session.TimeZero);

        var retry = harness.Protocol.Sign(session, Vin, HvacOn);
        Assert.Equal(5u, retry.Message.SignatureData.AESGCMDetachedData.ExpiresAt);
        var reply = harness.Vehicle.Process(retry.Message, ResponsePayload);
        Assert.Equal(ResponsePayload, harness.Protocol.OpenReply(session, Vin, retry, reply).ToArray());
    }

    [Fact]
    public void TryUpdateSession_AfterHmacEpochFault_ReportsIncorrectEpoch()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();
        harness.Vehicle.RotateEpoch();

        var command = harness.Protocol.Sign(session, Vin, Lock);
        var faultReply = harness.Vehicle.Process(command.Message, []);

        var ex = Assert.Throws<TeslaCommandException>(() => harness.Protocol.OpenReply(session, Vin, command, faultReply));
        Assert.Equal(TeslaMessageFault.IncorrectEpoch, ex.Fault);
        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));

        var retry = harness.Protocol.Sign(session, Vin, Lock);
        Assert.Equal(Lock, harness.Vehicle.VerifyCommand(retry.Message));
    }

    [Fact]
    public void TryUpdateSession_ClockSyncAfterExpiry()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        harness.Vehicle.ClockTime += 60; // vehicle clock ran ahead of ours

        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var faultReply = harness.Vehicle.Process(command.Message, ResponsePayload);
        Assert.Equal(MessageFault_E.RrorTimeExpired, faultReply.SignedMessageStatus.SignedMessageFault);

        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));
        Assert.Equal(ClockTime + 60, session.ClockTime);

        var retry = harness.Protocol.Sign(session, Vin, HvacOn);
        Assert.Equal(HvacOn, harness.Vehicle.VerifyCommand(retry.Message));
    }

    [Fact]
    public void TryUpdateSession_NeverLowersCounter()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        harness.Vehicle.Counter = 2;
        harness.Vehicle.ClockTime += 1;
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var faultReply = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy);

        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));
        Assert.Equal(SessionCounter + 1, session.Counter);
        Assert.Equal(ClockTime + 1, session.ClockTime);
    }

    [Fact]
    public void TryUpdateSession_SameEpochClockBackwards_IsIgnored()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        harness.Vehicle.Counter = 50;
        harness.Vehicle.ClockTime -= 10;
        var command = harness.Protocol.Sign(session, Vin, HvacOn);
        var faultReply = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy);

        Assert.True(harness.Protocol.TryUpdateSession(session, Vin, command, faultReply));

        Assert.Equal(SessionCounter + 1, session.Counter);
        Assert.Equal(ClockTime, session.ClockTime);
    }

    [Fact]
    public void TryUpdateSession_InvalidOrMissingSessionInfo_ReturnsFalse()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        harness.Vehicle.Counter = 100;
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        var noInfo = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy, attachSessionInfo: false);
        Assert.False(harness.Protocol.TryUpdateSession(session, Vin, command, noInfo));

        var badTag = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy);
        var tag = badTag.SignatureData.SessionInfoTag.Tag.ToByteArray();
        tag[31] ^= 1;
        badTag.SignatureData.SessionInfoTag.Tag = ByteString.CopyFrom(tag);
        Assert.False(harness.Protocol.TryUpdateSession(session, Vin, command, badTag));

        var wrongUuid = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy);
        wrongUuid.RequestUuid = ByteString.CopyFrom(new byte[16]);
        Assert.False(harness.Protocol.TryUpdateSession(session, Vin, command, wrongUuid));

        var late = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorBusy);
        harness.Time.Advance(TimeSpan.FromSeconds(11));
        Assert.False(harness.Protocol.TryUpdateSession(session, Vin, command, late));

        Assert.Equal(SessionCounter + 1, session.Counter);
    }

    [Fact]
    public void TryUpdateSession_ChangedVehiclePublicKey_ReturnsFalse()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, HvacOn);

        // Correctly tagged (same shared key), but announcing a different vehicle key.
        using var other = new FakeVehicle(Domain.Infotainment, harness.Time);
        var info = SessionInfo.Parser.ParseFrom(harness.Vehicle.EncodeSessionInfo());
        info.PublicKey = ByteString.CopyFrom(other.PublicKey);
        info.Counter = 100;
        var encoded = info.ToByteArray();
        var keys = harness.Vehicle.KeysFor(H(ClientPublic));
        var reply = harness.Vehicle.CreateFaultReply(command.Message, MessageFault_E.RrorInvalidSignature, attachSessionInfo: false);
        reply.SessionInfo = ByteString.CopyFrom(encoded);
        reply.SignatureData = new SignatureData
        {
            SessionInfoTag = new HMAC_Signature_Data
            {
                Tag = ByteString.CopyFrom(keys.SessionInfoTag(System.Text.Encoding.ASCII.GetBytes(Vin), command.Message.Uuid.Span, encoded)),
            },
        };

        Assert.False(harness.Protocol.TryUpdateSession(session, Vin, command, reply));
        Assert.Equal(SessionCounter + 1, session.Counter);
    }

    [Fact]
    public void ReplayedCommand_IsRejectedByVehicle()
    {
        using var harness = new ProtocolHarness(Domain.VehicleSecurity);
        var session = harness.Establish();
        var command = harness.Protocol.Sign(session, Vin, Lock);

        harness.Vehicle.VerifyCommand(command.Message);

        var fault = Assert.Throws<FakeVehicleFaultException>(() => harness.Vehicle.VerifyCommand(command.Message));
        Assert.Equal(MessageFault_E.RrorInvalidTokenOrCounter, fault.Fault);
    }

    [Fact]
    public void SessionKeys_AreBoundToTheClientKey()
    {
        using var harness = new ProtocolHarness(Domain.Infotainment);
        var session = harness.Establish();

        Assert.Equal(K, X(session.Keys.SharedKey));
        Assert.Equal(K, X(SessionKeys.Derive(EcdhFromScalar(VehicleScalar), H(ClientPublic)).SharedKey));
    }
}
