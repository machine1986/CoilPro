using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoilPro.Commands.Internal.Telemetry;

/// <summary>
/// Builds Tesla's Schnorr-signed JWS — a port of Go <c>authentication.SignMessage</c> as used by
/// <c>sign.SignMessageForFleet</c> / <c>SignMessageForVehicle</c> (golang-jwt v5 with the custom
/// <c>Tesla.SS256</c> signing method).
/// </summary>
/// <remarks>
/// Token = <c>B64URL(header) "." B64URL(payload) "." B64URL(signature)</c>, base64url without padding, where
/// <list type="bullet">
///   <item><description>header is <c>{"alg":"Tesla.SS256","typ":"JWT"}</c> (golang-jwt's map, keys sorted);</description></item>
///   <item><description>payload is the claims with <c>iss</c> = standard base64 (with padding) of the 65-byte public key and <c>aud</c> = the audience, both overwriting caller values, marshalled like Go (<see cref="GoJson"/>);</description></item>
///   <item><description>signature is <see cref="SchnorrP256.Sign"/> over the ASCII signing input <c>header.payload</c>.</description></item>
/// </list>
/// </remarks>
internal static class TeslaJws
{
    /// <summary>The JWS <c>alg</c> value of Tesla's Schnorr/P-256 signing method (Go <c>TeslaSchnorrSHA256</c>).</summary>
    public const string Algorithm = "Tesla.SS256";

    /// <summary>The application context the Go proxy uses for telemetry configs.</summary>
    public const string TelemetryApplication = "TelemetryClient";

    /// <summary>Audience accepted by every vehicle that trusts the signing key (Go <c>SignMessageForFleet</c>).</summary>
    public static string FleetAudience(string application) => "com.tesla.fleet." + application;

    /// <summary>Audience accepted only by <paramref name="vin"/> (Go <c>SignMessageForVehicle</c>).</summary>
    public static string VehicleAudience(string vin, string application) => "com.tesla.vehicle." + vin + "." + application;

    private const string EncodedHeader = "eyJhbGciOiJUZXNsYS5TUzI1NiIsInR5cCI6IkpXVCJ9"; // {"alg":"Tesla.SS256","typ":"JWT"}

    /// <summary>
    /// Signs <paramref name="claims"/> for <paramref name="audience"/>. <c>iss</c> and <c>aud</c> are overwritten;
    /// every other claim (including <c>exp</c>) is passed through untouched.
    /// </summary>
    /// <param name="privateScalar">Private scalar, 32 bytes big-endian.</param>
    /// <param name="publicKey">Public key, 65-byte uncompressed point.</param>
    /// <param name="audience">The <c>aud</c> claim.</param>
    /// <param name="claims">The claims object; not modified.</param>
    public static string Sign(ReadOnlySpan<byte> privateScalar, ReadOnlySpan<byte> publicKey, string audience, JsonObject claims)
    {
        var signingInput = BuildSigningInput(publicKey, audience, claims);
        var signature = SchnorrP256.Sign(privateScalar, publicKey, Encoding.ASCII.GetBytes(signingInput));
        return signingInput + "." + Base64Url.EncodeToString(signature);
    }

    /// <summary>The JWS signing input <c>B64URL(header) "." B64URL(payload)</c> — the exact bytes the signature covers.</summary>
    internal static string BuildSigningInput(ReadOnlySpan<byte> publicKey, string audience, JsonObject claims)
    {
        var payload = (JsonObject)claims.DeepClone();
        payload["iss"] = Convert.ToBase64String(publicKey);
        payload["aud"] = audience;

        var payloadJson = GoJson.Serialize(JsonSerializer.SerializeToElement(payload));
        return EncodedHeader + "." + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payloadJson));
    }

    /// <summary>Signs <paramref name="claims"/> for the whole fleet with the key pair (Go <c>SignMessageForFleet</c>).</summary>
    public static string SignForFleet(TeslaKeyPair keyPair, string application, JsonObject claims)
    {
        var parameters = keyPair.Key.ExportParameters(includePrivateParameters: true);
        var scalar = new byte[SchnorrP256.ScalarLength];
        try
        {
            var d = parameters.D ?? throw new InvalidOperationException("The key pair has no private key.");
            if (d.Length > scalar.Length)
            {
                throw new InvalidOperationException("The key pair is not a P-256 key.");
            }

            d.CopyTo(scalar.AsSpan(scalar.Length - d.Length));
            return Sign(scalar, keyPair.ExportPublicKeyUncompressedPoint(), FleetAudience(application), claims);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(scalar);
            if (parameters.D is not null)
            {
                CryptographicOperations.ZeroMemory(parameters.D);
            }
        }
    }
}
