using CoilPro.Commands.Internal.Crypto;
using CoilPro.Commands.Internal.Protocol;
using Google.Protobuf;
using Signatures;
using UniversalMessage;
using static CoilPro.Commands.Tests.Protocol.TestSupport.TestKeys;

namespace CoilPro.Commands.Tests.Protocol;

/// <summary>Ports of <c>window_test.go</c>, <c>peer_test.go</c> and the detached-prefix walk.</summary>
public class ProtocolHelperTests
{
    [Theory]
    // window_test.go TestSlidingWindow
    [InlineData(100u, (1ul << 0) | (1ul << 5), 101u, 101u, 1ul | (1ul << 1) | (1ul << 6), true)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 103u, 103u, (1ul << 2) | (1ul << 3) | (1ul << 8), true)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 500u, 500u, 0ul, true)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 98u, 100u, (1ul << 0) | (1ul << 1) | (1ul << 5), true)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 99u, 100u, (1ul << 0) | (1ul << 5), false)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 3u, 100u, (1ul << 0) | (1ul << 5), false)]
    [InlineData(100u, (1ul << 0) | (1ul << 5), 100u, 100u, (1ul << 0) | (1ul << 5), false)]
    // Shift by exactly 64: the old window falls out completely, bit 63 marks the old counter.
    [InlineData(100u, ulong.MaxValue, 164u, 164u, 1ul << 63, true)]
    public void SlidingWindow_MatchesGo(uint counter, ulong window, uint newCounter, uint expectedCounter, ulong expectedWindow, bool expectedOk)
    {
        var ok = SlidingWindow.TryUpdate(ref counter, ref window, newCounter);

        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedCounter, counter);
        Assert.Equal(expectedWindow, window);
    }

    [Fact]
    public void SlidingWindow_AcceptsFirstValue_ThenRejectsRepeats()
    {
        var window = new SlidingWindow();

        Assert.True(window.Update(5));
        Assert.False(window.Update(5));
        Assert.True(window.Update(3));
        Assert.False(window.Update(3));
        Assert.True(window.Update(6));
    }

    [Fact]
    public void MarshalNested_MatchesGoTestMarshalNestedMessages()
    {
        // peer_test.go TestMarshalNestedMessages: '1: { 2: { 3000: {"hello"}}}'
        var nested = NestedMessage.Marshal([1, 2, 3000], "hello"u8);
        Assert.Equal("0a0b1209c2bb010568656c6c6f", X(nested));
    }

    [Fact]
    public void TrySplit_HvacOn_ProducesPrefixAndInnermost()
    {
        // Action{vehicleAction(2){hvacAutoAction(10){power_on: true}}}
        Assert.True(NestedMessage.TrySplit(H("120452020801"), 2, out var prefix, out var innermost));
        Assert.Equal("12025200", X(prefix));
        Assert.Equal("0801", X(innermost));

        Assert.True(NestedMessage.TryParsePrefix(prefix, out var fields));
        Assert.Equal([2u, 10u], fields);
        Assert.Equal("120452020801", X(NestedMessage.Marshal(fields, innermost)));
    }

    [Fact]
    public void TrySplit_EmptyInnermostCommand()
    {
        // Action{vehicleAction{vehicleControlHonkHornAction(27){}}}: field 27 needs a two-byte key.
        Assert.True(NestedMessage.TrySplit(H("1203da0100"), 2, out var prefix, out var innermost));
        Assert.Equal("1203da0100", X(prefix));
        Assert.Empty(innermost);
    }

    [Theory]
    [InlineData("1000", 1)] // VCSEC RKEAction is a varint, not a nested message
    [InlineData("120452020801" + "1800", 2)] // data outside of the envelope
    [InlineData("1205520208", 2)] // declared length exceeds the data
    [InlineData("", 1)]
    public void TrySplit_RejectsNonNestedPayloads(string hex, int depth)
    {
        Assert.False(NestedMessage.TrySplit(H(hex), depth, out _, out _));
    }

    [Fact]
    public void TryParsePrefix_RejectsEmptyAndMultipleFields()
    {
        Assert.False(NestedMessage.TryParsePrefix([], out _));
        Assert.False(NestedMessage.TryParsePrefix(H("12005200"), out _));
        Assert.False(NestedMessage.TryParsePrefix(H("0800"), out _));
    }

    [Fact]
    public void RequestHash_MatchesGoTestRequestID()
    {
        // peer_test.go TestRequestID
        var tag = Enumerable.Range(0, 19).Select(i => (byte)i).ToArray();
        var message = new RoutableMessage
        {
            ToDestination = new Destination { Domain = Domain.VehicleSecurity },
            SignatureData = new SignatureData
            {
                HMACPersonalizedData = new HMAC_Personalized_Signature_Data { Tag = ByteString.CopyFrom(tag) },
            },
        };

        var vcsec = MessageAuthentication.RequestHash(message);
        Assert.Equal(17, vcsec.Length);
        Assert.Equal((byte)SignatureType.HmacPersonalized, vcsec[0]);
        Assert.Equal(tag[..16], vcsec[1..]);

        message.ToDestination = new Destination { Domain = Domain.Infotainment };
        var infotainment = MessageAuthentication.RequestHash(message);
        Assert.Equal((byte)SignatureType.HmacPersonalized, infotainment[0]);
        Assert.Equal(tag, infotainment[1..]);
    }

    [Fact]
    public void RequestHash_Detached_IsTypeAndFullTag()
    {
        var message = new RoutableMessage
        {
            ToDestination = new Destination { Domain = Domain.VehicleSecurity },
            SignatureData = new SignatureData
            {
                AESGCMDetachedData = new AES_GCM_Detached_Signature_Data { Tag = ByteString.CopyFrom(new byte[16]) },
            },
        };

        var hash = MessageAuthentication.RequestHash(message);
        Assert.Equal(17, hash.Length);
        Assert.Equal((byte)SignatureType.AesGcmDetached, hash[0]);
        Assert.Empty(MessageAuthentication.RequestHash(new RoutableMessage()));
    }
}
