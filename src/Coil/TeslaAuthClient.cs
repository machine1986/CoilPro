using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Coil.Internal;

namespace Coil;

/// <summary>Default <see cref="ITeslaAuthClient"/> backed by an <see cref="HttpClient"/>.</summary>
public sealed class TeslaAuthClient : ITeslaAuthClient
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly HttpClient _http;
    private readonly TeslaAuthOptions _options;
    private readonly TeslaEndpoints _endpoints;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Creates the client. The <paramref name="http"/> instance is typically supplied by <c>IHttpClientFactory</c>.
    /// </summary>
    /// <param name="http">HTTP client used for authorize/token requests.</param>
    /// <param name="options">OAuth configuration (client id/secret, redirect URI, region, scopes).</param>
    /// <param name="timeProvider">
    /// Optional clock used to compute <see cref="TeslaTokens.ExpiresAt"/>; defaults to <see cref="TimeProvider.System"/>.
    /// Inject a fake for deterministic expiry testing.
    /// </param>
    public TeslaAuthClient(HttpClient http, TeslaAuthOptions options, TimeProvider? timeProvider = null)
    {
        _http = http;
        _options = options;
        _endpoints = options.Endpoints ?? TeslaEndpoints.For(options.Region);
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public AuthorizationRequest CreateAuthorizationRequest(string? state = null)
    {
        var redirectUri = _options.RedirectUri
            ?? throw new InvalidOperationException(
                $"{nameof(TeslaAuthOptions)}.{nameof(TeslaAuthOptions.RedirectUri)} must be set to build an authorization request.");

        var pkce = PkceCodes.Generate();
        var resolvedState = state ?? Base64Url.Encode(RandomNumberGenerator.GetBytes(16));

        var query = BuildQueryString(
        [
            new("response_type", "code"),
            new("client_id", _options.ClientId),
            new("redirect_uri", redirectUri.ToString()),
            new("scope", _options.Scopes.ToScopeString()),
            new("state", resolvedState),
            new("code_challenge", pkce.Challenge),
            new("code_challenge_method", "S256"),
        ]);

        var authorizeUrl = new Uri(_endpoints.AuthBaseUrl, "/oauth2/v3/authorize");
        var builder = new UriBuilder(authorizeUrl) { Query = query };

        return new AuthorizationRequest
        {
            AuthorizationUrl = builder.Uri,
            State = resolvedState,
            Pkce = pkce,
        };
    }

    /// <inheritdoc />
    public Task<TeslaTokens> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default)
    {
        var redirectUri = _options.RedirectUri
            ?? throw new InvalidOperationException(
                $"{nameof(TeslaAuthOptions)}.{nameof(TeslaAuthOptions.RedirectUri)} must be set to exchange an authorization code.");

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "authorization_code"),
            new("client_id", _options.ClientId),
        };
        if (!string.IsNullOrEmpty(_options.ClientSecret))
        {
            form.Add(new("client_secret", _options.ClientSecret));
        }
        form.Add(new("code", code));
        form.Add(new("redirect_uri", redirectUri.ToString()));
        form.Add(new("code_verifier", codeVerifier));

        return SendTokenRequestAsync(form, ct);
    }

    /// <inheritdoc />
    public Task<TeslaTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "refresh_token"),
            new("client_id", _options.ClientId),
            new("refresh_token", refreshToken),
        };

        return SendTokenRequestAsync(form, ct);
    }

    /// <inheritdoc />
    public Task<TeslaTokens> GetPartnerTokenAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_options.ClientSecret))
        {
            throw new InvalidOperationException(
                $"{nameof(TeslaAuthOptions)}.{nameof(TeslaAuthOptions.ClientSecret)} must be set to use the client-credentials flow.");
        }

        var form = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "client_credentials"),
            new("client_id", _options.ClientId),
            new("client_secret", _options.ClientSecret),
            new("scope", _options.Scopes.ToScopeString()),
            // Bare origin, no trailing slash: Uri.ToString() would append "/" and Tesla rejects that
            // with 400 invalid_audience.
            new("audience", _endpoints.FleetApiBaseUrl.GetLeftPart(UriPartial.Authority)),
        };

        return SendTokenRequestAsync(form, ct);
    }

    private async Task<TeslaTokens> SendTokenRequestAsync(IEnumerable<KeyValuePair<string, string>> form, CancellationToken ct)
    {
        var tokenUrl = new Uri(_endpoints.AuthBaseUrl, "/oauth2/v3/token");
        using var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new FormUrlEncodedContent(form),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Tesla token endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}",
                inner: null,
                statusCode: response.StatusCode);
        }

        TokenResponse? token;
        try
        {
            token = JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new HttpRequestException($"Tesla token endpoint returned an unparseable response body: {body}", ex);
        }

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new HttpRequestException($"Tesla token endpoint response did not include an access_token: {body}");
        }

        var now = _timeProvider.GetUtcNow();

        return new TeslaTokens
        {
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            IdToken = token.IdToken,
            TokenType = string.IsNullOrEmpty(token.TokenType) ? "Bearer" : token.TokenType,
            ExpiresAt = now + TimeSpan.FromSeconds(token.ExpiresIn),
        };
    }

    private static string BuildQueryString(IReadOnlyList<KeyValuePair<string, string>> parameters) =>
        string.Join('&', parameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
}
