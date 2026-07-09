using System.Security.Cryptography;
using Coil.Internal;

namespace Coil;

/// <summary>A PKCE code verifier / challenge pair (RFC 7636, method <c>S256</c>).</summary>
public sealed record PkceCodes(string Verifier, string Challenge)
{
    /// <summary>
    /// Generates a cryptographically random verifier and its S256 challenge
    /// (base64url-encoded SHA-256 of the verifier, no padding).
    /// </summary>
    public static PkceCodes Generate()
    {
        Span<byte> verifierBytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(verifierBytes);
        var verifier = Base64Url.Encode(verifierBytes);

        var challengeBytes = SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64Url.Encode(challengeBytes);

        return new PkceCodes(verifier, challenge);
    }
}
