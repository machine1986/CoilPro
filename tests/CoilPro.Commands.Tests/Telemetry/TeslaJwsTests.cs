using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CoilPro.Commands.Internal.Telemetry;
using static CoilPro.Commands.Tests.Telemetry.ReferenceSchnorrVerifier;

namespace CoilPro.Commands.Tests.Telemetry;

public sealed class TeslaJwsTests
{
    /// <summary>Public key from Go <c>internal/authentication/jwt_test.go</c> (TestVerify).</summary>
    private static readonly byte[] GoPublicKey = Convert.FromHexString(
        "04775e2ef570d292df424c09f70e7d95678d5ce78124acb3f9375b198a4da9f9e9f0cc6d880e2e606e9b01b0fa62c6150e371aa5d8f7f09cc9e1bd3a1b449893ec");

    /// <summary>Token from Go <c>internal/authentication/jwt_test.go</c> (TestVerify), produced by <c>authentication.SignMessage</c>.</summary>
    private const string GoToken =
        "eyJhbGciOiJUZXNsYS5TUzI1NiIsInR5cCI6IkpXVCJ9.eyJhdWQiOiJjb20udGVzbGEudmVoaWNsZS5NM1RFUkFTSElNQUJVQ0swMSIsImZvbyI6ImJhciIsImlzcyI6IkJIZGVMdlZ3MHBMZlFrd0o5dzU5bFdlTlhPZUJKS3l6K1RkYkdZcE5xZm5wOE14dGlBNHVZRzZiQWJENllzWVZEamNhcGRqMzhKeko0YjA2RzBTWWsrdz0ifQ.4Xq2tsynDYOhLBWpnqLJfsAzdOqTOwHkVowI47A-yzVXIQHaawObJvAYRoRs61oVwWiEQ7XYXG8WE_Vz_49eVirAV9NGKymj3HBTTjN5DmViWmFzfaIOXRWKJlE--vzU";

    [Fact]
    public void SigningInput_IsByteIdenticalToGoToken()
    {
        var claims = new JsonObject { ["foo"] = "bar", ["iss"] = "overwritten", ["aud"] = "overwritten" };
        var signingInput = TeslaJws.BuildSigningInput(GoPublicKey, "com.tesla.vehicle.M3TERASHIMABUCK01", claims);
        Assert.Equal(GoToken[..GoToken.LastIndexOf('.')], signingInput);
    }

    [Fact]
    public void ReferenceVerifier_AcceptsGoToken()
    {
        var lastDot = GoToken.LastIndexOf('.');
        var signature = Base64Url.DecodeFromChars(GoToken.AsSpan(lastDot + 1));
        Assert.Equal(Result.Valid, Verify(GoPublicKey, Encoding.ASCII.GetBytes(GoToken[..lastDot]), signature));
    }

    [Fact]
    public void SignForFleet_ProducesVerifiableTokenWithFleetClaims()
    {
        using var keyPair = TeslaKeyPair.Generate();
        var claims = new JsonObject { ["hostname"] = "telemetry.example.com", ["port"] = 443, ["exp"] = 1_900_000_000 };

        var token = TeslaJws.SignForFleet(keyPair, "TelemetryClient", claims);

        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.DoesNotContain('=', token);
        Assert.Equal("""{"alg":"Tesla.SS256","typ":"JWT"}""", Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[0])));

        var publicKey = keyPair.ExportPublicKeyUncompressedPoint();
        var payload = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1]));
        Assert.Equal(
            $$"""{"aud":"com.tesla.fleet.TelemetryClient","exp":1900000000,"hostname":"telemetry.example.com","iss":"{{Convert.ToBase64String(publicKey)}}","port":443}""",
            payload);

        var signature = Base64Url.DecodeFromChars(parts[2]);
        Assert.Equal(96, signature.Length);
        Assert.Equal(Result.Valid, Verify(publicKey, Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]), signature));

        // Deterministic (RFC 6979 nonce) and the caller's claims are untouched.
        Assert.Equal(token, TeslaJws.SignForFleet(keyPair, "TelemetryClient", claims));
        Assert.False(claims.ContainsKey("iss"));
    }

    [Fact]
    public void GoJson_SortsKeysAndEscapesLikeGo()
    {
        // JSON escapes are assembled from parts so the source file stays ASCII-only.
        static string U(string hex) => "\\" + "u" + hex;
        var input = "{\"b\":1,\"a\":{\"z\":[true,null,1.5],\"y\":\"x\"},\"A\":\"<&>\\b\\f\\n\\r\\t" + U("0001") + U("2028") + U("2029") + "+/=" + U("00e9") + "\\\"\\\\\"}";
        var expected = "{\"A\":\"" + U("003c") + U("0026") + U("003e") + "\\b\\f\\n\\r\\t" + U("0001") + U("2028") + U("2029") + "+/=" + (char)0xE9 + "\\\"\\\\\",\"a\":{\"y\":\"x\",\"z\":[true,null,1.5]},\"b\":1}";

        using var doc = JsonDocument.Parse(input);
        Assert.Equal(expected, GoJson.Serialize(doc.RootElement));
    }
}
