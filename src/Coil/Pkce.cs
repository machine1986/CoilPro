namespace Coil;

/// <summary>A PKCE code verifier / challenge pair (RFC 7636, method <c>S256</c>).</summary>
public sealed record PkceCodes(string Verifier, string Challenge)
{
    /// <summary>
    /// Generates a cryptographically random verifier and its S256 challenge
    /// (base64url-encoded SHA-256 of the verifier, no padding).
    /// </summary>
    public static PkceCodes Generate() =>
        throw new NotImplementedException("TODO(M1 core-impl): random verifier + base64url(SHA256) challenge");
}
