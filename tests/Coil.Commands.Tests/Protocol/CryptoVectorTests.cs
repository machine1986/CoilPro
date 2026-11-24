using System.Security.Cryptography;
using System.Text;
using Coil.Commands.Internal.Crypto;
using Coil.Commands.Internal.Protocol;
using Coil.Commands.Tests.Protocol.TestSupport;
using Signatures;
using UniversalMessage;
using static Coil.Commands.Tests.Protocol.TestSupport.TestKeys;

namespace Coil.Commands.Tests.Protocol;

/// <summary>
/// Byte-exact vectors from teslamotors/vehicle-command: <c>pkg/protocol/protocol.md</c> (pinned upstream by
/// <c>protocol_doc_test.go</c>), <c>ecdh_test.go</c>, <c>metadata_test.go</c>, <c>verifier_test.go</c>.
/// </summary>
public class CryptoVectorTests
{
    private static readonly byte[] VinBytes = Encoding.ASCII.GetBytes(Vin);

    [Fact]
    public void ClientKeyPem_HasProtocolDocPublicKey()
    {
        using var keyPair = ClientKeyPair();
        Assert.Equal(ClientPublic, X(keyPair.ExportPublicKeyUncompressedPoint()));
    }

    [Fact]
    public void SharedSecret_And_K_MatchProtocolDoc()
    {
        using var client = EcdhFromScalar(ClientScalar);

        Assert.Equal(Sx, X(SessionKeys.DeriveSharedSecret(client, H(VehiclePublic))));
        Assert.Equal(K, X(SessionKeys.Derive(client, H(VehiclePublic)).SharedKey));
    }

    [Fact]
    public void SharedSecret_IsSymmetric()
    {
        using var vehicle = EcdhFromScalar(VehicleScalar);
        Assert.Equal(K, X(SessionKeys.Derive(vehicle, H(ClientPublic)).SharedKey));
    }

    [Fact]
    public void SharedSecret_KeepsLeadingZero()
    {
        // ecdh_test.go TestSharedKey: values chosen so that Sx starts with 0x00.
        using var vehicle = EcdhFromScalar("5260f8d6113875d86f8ee8fea340df1ffb40c658b5455e8c33d797c53a41afd3");
        var phone = H("0407fb60b65b94e0de4a954c53be10003d9e69918dedfda5f4e9efb9ebd8c5bd672a53991c4068865d5fb44f97f6cecf8398f261dd1d7bc69be676afdc8ffacbcc");

        var sx = SessionKeys.DeriveSharedSecret(vehicle, phone);

        Assert.Equal("0072d5b815207a04f0c795fba0ba9e8add3f1f57148c51fface22ca15e6fd845", X(sx));
        Assert.Equal("0db468f9045e01c1798c54e52d7f408f", X(SessionKeys.Derive(vehicle, phone).SharedKey));
    }

    [Fact]
    public void Derive_RejectsInvalidPoints()
    {
        using var vehicle = EcdhFromScalar("5260f8d6113875d86f8ee8fea340df1ffb40c658b5455e8c33d797c53a41afd3");
        var phone = H("0407fb60b65b94e0de4a954c53be10003d9e69918dedfda5f4e9efb9ebd8c5bd672a53991c4068865d5fb44f97f6cecf8398f261dd1d7bc69be676afdc8ffacbcc");
        phone[1] ^= 1; // no longer on the curve
        var infinity = new byte[65];
        infinity[0] = 0x04;

        Assert.ThrowsAny<CryptographicException>(() => SessionKeys.Derive(vehicle, phone));
        Assert.ThrowsAny<CryptographicException>(() => SessionKeys.Derive(vehicle, infinity));
        Assert.ThrowsAny<CryptographicException>(() => SessionKeys.Derive(vehicle, phone.AsSpan(0, 33)));
    }

    [Fact]
    public void SubKeys_MatchProtocolDoc()
    {
        var keys = SessionKeys.FromSharedKey(H(K));

        Assert.Equal(SessionInfoKey, X(keys.SessionInfoKey));
        Assert.Equal(CommandKey, X(keys.CommandKey));
    }

    [Fact]
    public void SessionInfoMetadata_And_Tag_MatchProtocolDoc()
    {
        var meta = new Metadata()
            .AddByte(Tag.SignatureType, (byte)SignatureType.Hmac)
            .Add(Tag.Personalization, VinBytes)
            .Add(Tag.Challenge, H(Challenge));
        Assert.Equal("000106021135594a333031323334353637383941424306101588d5a30eabc6f8fc9a951b11f6fd11ff", X(meta.Serialize()));

        var tag = SessionKeys.FromSharedKey(H(K)).SessionInfoTag(VinBytes, H(Challenge), H(EncodedSessionInfo));
        Assert.Equal(SessionInfoTag, X(tag));
    }

    [Fact]
    public void AesGcmPersonalized_HvacExample_MatchesProtocolDoc()
    {
        // protocol.md "Authorizing commands" / protocol_doc_test.go TestProtocolDocAESGCMExample.
        var meta = MessageAuthentication.CommandMetadata(
            SignatureType.AesGcmPersonalized, Domain.Infotainment, VinBytes, H(Epoch), expiresAt: 2655, counter: 7, flags: 2);
        Assert.Equal(
            "000105010103021135594a333031323334353637383941424303104c463f9cc0d3d26906e982ed224adde6040400000a5f050400000007070400000002ff",
            X(meta.Serialize()));

        var aad = meta.Sha256Checksum();
        Assert.Equal("54785b6878a51a70acaf240199f92f482962a73a8c3170bfd2a94b1715d127b9", X(aad));

        var (ciphertext, tag) = SessionKeys.FromSharedKey(H(K)).Encrypt(H("dbf79447fa156674dae1caed"), H("120452020801"), aad);
        Assert.Equal("38038e8c0f2e", X(ciphertext));
        Assert.Equal("c228e0ff64991481db3a7bbc133696c5", X(tag));

        var plaintext = SessionKeys.FromSharedKey(H(K)).Decrypt(H("dbf79447fa156674dae1caed"), ciphertext, tag, aad);
        Assert.Equal("120452020801", X(plaintext));
    }

    [Fact]
    public void HmacPersonalized_Tag_MatchesDerivedVector()
    {
        // Same inputs as the protocol.md AES-GCM example, signature type 8. Derived (not an upstream vector),
        // cross-checked with the openssl CLI during research.
        var tag = MessageAuthentication
            .CommandMetadata(SignatureType.HmacPersonalized, Domain.Infotainment, VinBytes, H(Epoch), 2655, 7, 2)
            .HmacChecksum(H(CommandKey), H("120452020801"));

        Assert.Equal("f3c24554a00e42160e6acef6dd4f1c8ef9c30ed6391ec818d75663bee00a47f0", X(tag));
    }

    [Fact]
    public void CommandMetadata_OmitsZeroFlags()
    {
        var meta = MessageAuthentication.CommandMetadata(SignatureType.HmacPersonalized, Domain.VehicleSecurity, VinBytes, H(Epoch), 1, 2, flags: 0);
        Assert.EndsWith("0504" + "00000002" + "ff", X(meta.Serialize()));
    }

    [Fact]
    public void Metadata_Checksum_MatchesGoTestCheckSum()
    {
        // metadata_test.go TestCheckSum.
        var checksum = new Metadata()
            .Add(Tag.SignatureType, [0x05])
            .Add(Tag.Domain, [0x02])
            .Add(Tag.Personalization, "testVIN"u8)
            .Add(Tag.Epoch, H("aada928a4f215f55f9e6e45e66b6521e"))
            .Add(Tag.ExpiresAt, H("00000e74"))
            .Add(Tag.Counter, H("0000053a"))
            .Sha256Checksum();

        Assert.Equal("abab04d804499813382efd74a06791ce2de777439603246dfbaa8392ca05868e", X(checksum));
    }

    [Fact]
    public void Metadata_RejectsOutOfOrderTags_And_LongValues()
    {
        var meta = new Metadata().AddByte(Tag.Domain, 3);

        Assert.Throws<InvalidOperationException>(() => meta.AddByte(Tag.SignatureType, 8));
        Assert.Throws<ArgumentException>(() => new Metadata().Add(Tag.Personalization, new byte[256]));
    }

    [Fact]
    public void GcmKnown_VcsecMessage_Decrypts()
    {
        // verifier_test.go TestGCMKnown: AES_GCM_PERSONALIZED message to VCSEC, decrypted with the vehicle key.
        var message = RoutableMessage.Parser.ParseFrom(H(
            "320208023a020800525929 6d587d130d34d74d6d5c628c73c1f8ef99cc4ae1c95b97671474980ccc7946a90a2f7971a5c837b96c9cc1e8075d1aab92bc8557b7fdf2dcf9ddbb90e236246db699788e585e8b0ea84752e0090cc80c4384d27ca6fcdd21" +
            "6a470a061204bb0ca3712a3d0a10eaabe301b4b4a1243118a4082522011512 0c45d29af664e2ff8fd49218b718ffffffff0f2550c300002a106d846705ba5c143f94257275a2ca701f"));
        using var vehicle = EcdhFromScalar("9d9c5e86d5a83057965ca94b5faeff116c275d08ca91e839bd2dea59c2229b81");
        var signerPublicKey = H("0450444fabe062f6ffc59de654373e1aa84aa4f053f365f3746fbaaa8ad5d5875e8279ba379b4788e9148f5000bd1fe085d18925e8d047239cfa0d9cd23417b714");
        var keys = SessionKeys.Derive(vehicle, signerPublicKey);
        var gcm = message.SignatureData.AESGCMPersonalizedData;

        var aad = MessageAuthentication.CommandMetadata(
            SignatureType.AesGcmPersonalized, message.ToDestination.Domain, "testvin"u8.ToArray(), gcm.Epoch.Span, gcm.ExpiresAt, gcm.Counter, message.Flags)
            .Sha256Checksum();
        var plaintext = keys.Decrypt(gcm.Nonce.Span, message.ProtobufMessageAsBytes.Span, gcm.Tag.Span, aad);

        Assert.Equal(
            "82014a2a480a430a41044b96809e66821b4553ff2b427a52d50843a2f718fd56f2ad93d46a8b1db74dd6190f20b6b82e85a8eac40e867b9a0cf74f364f03e9946e25ceee9934b19cb603120102ca01090a077465737476696e",
            X(plaintext));
    }

    [Fact]
    public void Decrypt_RejectsWrongNonceLength_And_TamperedTag()
    {
        var keys = SessionKeys.FromSharedKey(H(K));
        var aad = new byte[32];
        var (ciphertext, tag) = keys.Encrypt(new byte[12], H("0801"), aad);

        Assert.ThrowsAny<CryptographicException>(() => keys.Decrypt(new byte[11], ciphertext, tag, aad));
        tag[0] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => keys.Decrypt(new byte[12], ciphertext, tag, aad));
    }

    [Fact]
    public void ResponseAssociatedData_MatchesDerivedExample()
    {
        // Structure per peer.go responseMetadata; values derived from the protocol.md session (not an upstream vector).
        var requestHash = H("08" + "f3c24554a00e42160e6acef6dd4f1c8ef9c30ed6391ec818d75663bee00a47f0");

        var aad = MessageAuthentication.ResponseAssociatedData(Domain.Infotainment, VinBytes, 1, 0, requestHash, MessageFault_E.RrorNone);

        // = SHA256(000109 010103 0211<VIN> 050400000001 070400000000 0821<hash> 090400000000 ff)
        Assert.Equal("3ac5c913aa39e5e0895eeae0cbc3c1fc28970bee28732ed8e81be9d0cda37501", X(aad));
    }
}
