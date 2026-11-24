using System.Security.Cryptography;

namespace CoilPro.Tests;

/// <summary>
/// Covers the P-256 key pair Tesla requires: curve, PEM shapes that must match what
/// <c>openssl</c>/<c>tesla-keygen</c> emit, and the uncompressed-point encoding used both on the
/// command wire and by the partner public-key endpoint.
/// </summary>
public class TeslaKeyPairTests
{
    [Fact]
    public void Generate_uses_the_nist_p256_curve()
    {
        using var keyPair = TeslaKeyPair.Generate();

        Assert.Equal(256, keyPair.Key.KeySize);

        var parameters = keyPair.Key.ExportParameters(includePrivateParameters: false);
        Assert.True(parameters.Curve.IsNamed);
        Assert.Equal(ECCurve.NamedCurves.nistP256.Oid.Value, parameters.Curve.Oid.Value);
    }

    [Fact]
    public void Generate_produces_a_distinct_key_each_time()
    {
        using var first = TeslaKeyPair.Generate();
        using var second = TeslaKeyPair.Generate();

        Assert.NotEqual(first.ExportPublicKeyHex(), second.ExportPublicKeyHex());
    }

    [Fact]
    public void ExportPublicKeyPem_is_a_subject_public_key_info_document()
    {
        using var keyPair = TeslaKeyPair.Generate();

        var pem = keyPair.ExportPublicKeyPem();

        // This exact header is what `openssl ec -pubout` and Tesla's tesla-keygen emit; the
        // well-known file must match, so the header is part of the contract, not cosmetics.
        Assert.StartsWith("-----BEGIN PUBLIC KEY-----", pem);
        Assert.Contains("-----END PUBLIC KEY-----", pem);
        Assert.DoesNotContain("PRIVATE", pem);

        // openssl and Go's encoding/pem both terminate the document with a newline; matching them
        // byte-for-byte keeps the published file identical to what Tesla's tooling produces.
        Assert.EndsWith("-----END PUBLIC KEY-----\n", pem);
        Assert.DoesNotContain("\r", pem);
    }

    [Fact]
    public void ExportPrivateKeyPem_is_an_unencrypted_pkcs8_document()
    {
        using var keyPair = TeslaKeyPair.Generate();

        var pem = keyPair.ExportPrivateKeyPem();

        Assert.StartsWith("-----BEGIN PRIVATE KEY-----", pem);
        Assert.Contains("-----END PRIVATE KEY-----", pem);
    }

    [Fact]
    public void ExportPublicKeyUncompressedPoint_is_65_bytes_starting_with_0x04()
    {
        using var keyPair = TeslaKeyPair.Generate();

        var point = keyPair.ExportPublicKeyUncompressedPoint();

        // 1 marker byte + two 32-byte P-256 coordinates.
        Assert.Equal(65, point.Length);
        Assert.Equal(0x04, point[0]);
    }

    [Fact]
    public void ExportPublicKeyHex_is_lowercase_hex_of_the_uncompressed_point()
    {
        using var keyPair = TeslaKeyPair.Generate();

        var hex = keyPair.ExportPublicKeyHex();

        Assert.Equal(130, hex.Length);
        Assert.StartsWith("04", hex);
        Assert.Equal(hex.ToLowerInvariant(), hex);
        Assert.Equal(Convert.ToHexString(keyPair.ExportPublicKeyUncompressedPoint()).ToLowerInvariant(), hex);
    }

    [Fact]
    public void FromPrivateKeyPem_round_trips_a_generated_pkcs8_key()
    {
        using var original = TeslaKeyPair.Generate();

        using var loaded = TeslaKeyPair.FromPrivateKeyPem(original.ExportPrivateKeyPem());

        Assert.Equal(original.ExportPublicKeyHex(), loaded.ExportPublicKeyHex());
        Assert.Equal(original.ExportPublicKeyPem(), loaded.ExportPublicKeyPem());
    }

    [Fact]
    public void FromPrivateKeyPem_accepts_a_sec1_ec_private_key()
    {
        // `openssl ecparam -name prime256v1 -genkey` emits SEC 1 ("EC PRIVATE KEY"), not PKCS#8 —
        // keys generated that way must load without conversion.
        using var source = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var sec1Pem = source.ExportECPrivateKeyPem();

        using var loaded = TeslaKeyPair.FromPrivateKeyPem(sec1Pem);

        Assert.Equal(
            Convert.ToHexString(source.ExportSubjectPublicKeyInfo()),
            Convert.ToHexString(loaded.Key.ExportSubjectPublicKeyInfo()));
    }

    [Fact]
    public void FromPrivateKeyPem_rejects_a_key_on_the_wrong_curve()
    {
        // secp384r1 is what Tesla's docs use for the proxy's TLS cert — a plausible mix-up that
        // would otherwise surface much later as an opaque signing failure.
        using var wrongCurve = ECDsa.Create(ECCurve.NamedCurves.nistP384);

        var ex = Assert.Throws<ArgumentException>(
            () => TeslaKeyPair.FromPrivateKeyPem(wrongCurve.ExportPkcs8PrivateKeyPem()));

        Assert.Contains("P-256", ex.Message);
    }

    [Fact]
    public void FromPrivateKeyPem_rejects_garbage()
    {
        Assert.ThrowsAny<ArgumentException>(() => TeslaKeyPair.FromPrivateKeyPem("not a pem at all"));
    }

    [Fact]
    public void WellKnownPublicKeyPath_matches_teslas_required_location()
    {
        Assert.Equal(".well-known/appspecific/com.tesla.3p.public-key.pem", TeslaKeyPair.WellKnownPublicKeyPath);
    }
}
