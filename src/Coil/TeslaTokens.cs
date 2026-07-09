namespace Coil;

/// <summary>OAuth tokens returned by the Tesla auth server.</summary>
public sealed record TeslaTokens
{
    /// <summary>Bearer access token for the Fleet API.</summary>
    public required string AccessToken { get; init; }

    /// <summary>Refresh token (present when <c>offline_access</c> was requested).</summary>
    public string? RefreshToken { get; init; }

    /// <summary>OpenID Connect id token, if issued.</summary>
    public string? IdToken { get; init; }

    /// <summary>Token type, normally <c>Bearer</c>.</summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>Absolute UTC expiry of the access token.</summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>True when the access token is at or past its expiry at <paramref name="now"/>.</summary>
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
