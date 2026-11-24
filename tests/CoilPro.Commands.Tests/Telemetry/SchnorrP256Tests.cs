using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using CoilPro.Commands.Internal.Telemetry;
using static CoilPro.Commands.Tests.Telemetry.ReferenceSchnorrVerifier;

namespace CoilPro.Commands.Tests.Telemetry;

/// <summary>Vectors from teslamotors/vehicle-command <c>internal/schnorr/sign_test.go</c> and <c>schnorr_test.go</c>.</summary>
public sealed class SchnorrP256Tests
{
    /// <summary>Go <c>testKey()</c>: scalar 0x03 followed by 31 zero bytes.</summary>
    private static byte[] TestScalar()
    {
        var scalar = new byte[32];
        scalar[0] = 3;
        return scalar;
    }

    /// <summary>Go <c>goodSig()</c> signature over "hello world" with <see cref="TestScalar"/>.</summary>
    private static readonly byte[] GoodSignature = Convert.FromHexString(
        "7cfdbeb5baa730540401550bdefa20976453e8539ae4b2f26ce33125801a08f9" +
        "0ed20c3d846497ff82cc9772e3db4703982f47bd0b0b89dfb9a49cd2e5240546" +
        "02b1e05fbf95f5686faea7a5809eb92f5ecc22eae74ceccc5e2a65dd67ff20fc");

    private static readonly byte[] HelloWorld = Encoding.ASCII.GetBytes("hello world");

    [Fact]
    public void DeterministicNonce_MatchesRfc6979_A_2_5()
    {
        // RFC 6979 A.2.5, P-256 / SHA-256, message "sample" (Go TestDeterministicNonce).
        var scalar = Convert.FromHexString("C9AFA9D845BA75166B5C215767B1D6934E50C3DB36E89B127B8A622B120F6721");
        var nonce = SchnorrP256.DeterministicNonce(scalar, SHA256.HashData("sample"u8));
        Assert.Equal("A6E3C57DD01ABE90086538398355DD4C3B17AA873382B0F24D6129493D8AAD60", Convert.ToHexString(nonce));
    }

    [Fact]
    public void DeterministicNonce_ResamplesWhenFirstCandidateExceedsOrder()
    {
        // Go TestRejectionSampling: this digest's first candidate is >= n.
        var digest = Convert.FromHexString("0080c36864c5f2f460e3767983c65677b65cef901bcedcb223f9b365c68f52f6");
        var nonce = SchnorrP256.DeterministicNonce(TestScalar(), digest);
        Assert.True(new BigInteger(nonce, isUnsigned: true, isBigEndian: true) < SchnorrP256.Order);
        Assert.Equal("264FC6592FBEA24FD0954E0B86B886E8743161758DDAD2F7E9FED75A0019E005", Convert.ToHexString(nonce));
    }

    [Fact]
    public void Sign_MatchesGoVectorByteForByte()
    {
        var scalar = TestScalar();
        var signature = SchnorrP256.Sign(scalar, PublicKeyFor(scalar), HelloWorld);
        Assert.Equal(Convert.ToHexString(GoodSignature), Convert.ToHexString(signature));
    }

    [Fact]
    public void ReferenceVerifier_AcceptsGoVector() =>
        Assert.Equal(Result.Valid, Verify(PublicKeyFor(TestScalar()), HelloWorld, GoodSignature));

    [Fact]
    public void ReferenceVerifier_RejectsWrongMessage()
    {
        var message = (byte[])HelloWorld.Clone();
        message[0] ^= 1;
        Assert.Equal(Result.InvalidSignature, Verify(PublicKeyFor(TestScalar()), message, GoodSignature));
    }

    [Fact]
    public void ReferenceVerifier_RejectsWrongPublicKey()
    {
        var other = new byte[32];
        other[0] = 4;
        Assert.Equal(Result.InvalidSignature, Verify(PublicKeyFor(other), HelloWorld, GoodSignature));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    [InlineData(95)]
    public void ReferenceVerifier_RejectsTamperedSignature(int index)
    {
        var signature = (byte[])GoodSignature.Clone();
        signature[index] ^= 1;
        Assert.Equal(Result.InvalidSignature, Verify(PublicKeyFor(TestScalar()), HelloWorld, signature));
    }

    [Fact]
    public void ReferenceVerifier_RejectsPublicKeyNotOnCurve()
    {
        var key = PublicKeyFor(TestScalar());
        key[0] ^= 1;
        Assert.Equal(Result.InvalidPublicKey, Verify(key, HelloWorld, GoodSignature));

        var zero = new byte[65];
        zero[0] = 0x04;
        Assert.Equal(Result.InvalidPublicKey, Verify(zero, HelloWorld, GoodSignature));
    }

    [Fact]
    public void ReferenceVerifier_RejectsShortSignature() =>
        Assert.Equal(Result.InvalidSignature, Verify(PublicKeyFor(TestScalar()), HelloWorld, GoodSignature[..^1]));

    [Fact]
    public void Sign_RoundTripsWithRandomKeys()
    {
        for (var i = 0; i < 8; i++)
        {
            using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var parameters = key.ExportParameters(includePrivateParameters: true);
            var publicKey = new byte[65];
            publicKey[0] = 0x04;
            parameters.Q.X!.CopyTo(publicKey, 1);
            parameters.Q.Y!.CopyTo(publicKey, 33);
            var message = RandomNumberGenerator.GetBytes(17 * i);

            var signature = SchnorrP256.Sign(parameters.D, publicKey, message);

            Assert.Equal(Result.Valid, Verify(publicKey, message, signature));
            Assert.Equal(signature, SchnorrP256.Sign(parameters.D, publicKey, message)); // deterministic
        }
    }

    [Fact]
    public void Sign_RejectsOutOfRangeScalar()
    {
        var publicKey = PublicKeyFor(TestScalar());
        Assert.Throws<ArgumentException>(() => SchnorrP256.Sign(new byte[32], publicKey, HelloWorld));
        Assert.Throws<ArgumentException>(() => SchnorrP256.Sign(SchnorrP256.Order.ToByteArray(isUnsigned: true, isBigEndian: true), publicKey, HelloWorld));
    }
}
