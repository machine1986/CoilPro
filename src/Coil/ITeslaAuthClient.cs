namespace Coil;

/// <summary>
/// Handles the Tesla Fleet API OAuth 2.0 flows: authorization code (with PKCE),
/// client credentials (partner token), and refresh.
/// </summary>
public interface ITeslaAuthClient
{
    /// <summary>
    /// Builds the authorization-code URL with PKCE and state. Persist the returned
    /// <see cref="AuthorizationRequest.Pkce"/> and <see cref="AuthorizationRequest.State"/> to
    /// complete the flow in <see cref="ExchangeCodeAsync"/>.
    /// </summary>
    /// <param name="state">Optional explicit state; a random value is generated when null.</param>
    AuthorizationRequest CreateAuthorizationRequest(string? state = null);

    /// <summary>Exchanges an authorization code for tokens (end-user flow).</summary>
    Task<TeslaTokens> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default);

    /// <summary>Refreshes an access token using a refresh token.</summary>
    Task<TeslaTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Obtains a partner (application) token via the client-credentials flow.</summary>
    Task<TeslaTokens> GetPartnerTokenAsync(CancellationToken ct = default);
}
