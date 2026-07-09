namespace Coil;

/// <summary>Default <see cref="ITeslaAuthClient"/> backed by an <see cref="HttpClient"/>.</summary>
public sealed class TeslaAuthClient : ITeslaAuthClient
{
    private readonly HttpClient _http;
    private readonly TeslaAuthOptions _options;
    private readonly TeslaEndpoints _endpoints;

    /// <summary>Creates the client. The <paramref name="http"/> instance is typically supplied by <c>IHttpClientFactory</c>.</summary>
    public TeslaAuthClient(HttpClient http, TeslaAuthOptions options)
    {
        _http = http;
        _options = options;
        _endpoints = options.Endpoints ?? TeslaEndpoints.For(options.Region);
    }

    /// <inheritdoc />
    public AuthorizationRequest CreateAuthorizationRequest(string? state = null) =>
        throw new NotImplementedException("TODO(M1 core-impl)");

    /// <inheritdoc />
    public Task<TeslaTokens> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M1 core-impl)");

    /// <inheritdoc />
    public Task<TeslaTokens> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M1 core-impl)");

    /// <inheritdoc />
    public Task<TeslaTokens> GetPartnerTokenAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M1 core-impl)");
}
