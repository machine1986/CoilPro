namespace Coil;

/// <summary>
/// The authorization URL to send the user to, plus the transient values required to complete the
/// authorization-code flow. Keep <see cref="Pkce"/> and <see cref="State"/> until the callback returns.
/// </summary>
public sealed record AuthorizationRequest
{
    /// <summary>The fully-built authorization URL (open this in the user's browser).</summary>
    public required Uri AuthorizationUrl { get; init; }

    /// <summary>Opaque state value; compare against the callback to prevent CSRF.</summary>
    public required string State { get; init; }

    /// <summary>PKCE codes; pass <see cref="PkceCodes.Verifier"/> to the token exchange.</summary>
    public required PkceCodes Pkce { get; init; }
}
