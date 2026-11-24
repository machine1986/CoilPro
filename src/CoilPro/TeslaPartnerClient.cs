using System.Text;
using System.Text.Json;
using CoilPro.Internal;

namespace CoilPro;

/// <summary>
/// Default <see cref="ITeslaPartnerClient"/> backed by an <see cref="HttpClient"/> whose
/// <see cref="HttpClient.BaseAddress"/> is the Fleet API host for the region being registered.
/// </summary>
/// <remarks>
/// Partner endpoints authenticate with a <b>partner token</b> (OAuth client-credentials), not a
/// user token, so this client obtains its own token from <see cref="ITeslaAuthClient.GetPartnerTokenAsync"/>
/// and caches it in memory until shortly before it expires. That token's <c>audience</c> is derived
/// from the auth client's region, which must match this client's base address.
/// </remarks>
public sealed class TeslaPartnerClient : ITeslaPartnerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <summary>Refresh the cached partner token this long before it actually expires.</summary>
    private static readonly TimeSpan ExpiryMargin = TimeSpan.FromSeconds(30);

    private readonly ITeslaAuthClient _authClient;
    private readonly FleetApiExecutor _executor;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private TeslaTokens? _partnerTokens;

    /// <summary>Creates the client.</summary>
    /// <param name="http">
    /// HTTP client with <see cref="HttpClient.BaseAddress"/> set to the Fleet API base URL of the
    /// region to register (see <see cref="TeslaEndpoints.FleetApiBaseUrl"/>).
    /// </param>
    /// <param name="authClient">
    /// Auth client used for the client-credentials flow. Its <see cref="TeslaAuthOptions.ClientSecret"/>
    /// must be set, and its region must match <paramref name="http"/>'s base address.
    /// </param>
    /// <param name="options">Resilience tunables; defaults are used when null.</param>
    /// <param name="timeProvider">Optional clock for token-expiry checks; defaults to <see cref="TimeProvider.System"/>.</param>
    public TeslaPartnerClient(
        HttpClient http,
        ITeslaAuthClient authClient,
        CoilProClientOptions? options = null,
        TimeProvider? timeProvider = null)
    {
        _authClient = authClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _executor = new FleetApiExecutor(http, GetPartnerTokenAsync, options ?? new CoilProClientOptions());
    }

    /// <inheritdoc />
    public RateLimitInfo? LastRateLimit => _executor.LastRateLimit;

    /// <inheritdoc />
    public async Task<PartnerAccount> RegisterAsync(string domain, CancellationToken ct = default)
    {
        var normalized = NormalizeDomain(domain);

        var result = await _executor.SendAsync<PartnerAccount>(
            HttpMethod.Post,
            "api/1/partner_accounts",
            () => new StringContent(
                JsonSerializer.Serialize(new { domain = normalized }, JsonOptions),
                Encoding.UTF8,
                "application/json"),
            ct).ConfigureAwait(false);

        return result.Value ?? throw new TeslaApiException(
            result.StatusCode,
            "Fleet API returned a success status with an empty partner_accounts response.",
            responseBody: null,
            rateLimit: LastRateLimit);
    }

    /// <inheritdoc />
    public async Task<string?> GetPublicKeyAsync(string domain, CancellationToken ct = default)
    {
        var normalized = NormalizeDomain(domain);

        // NOTE: domain travels as a query parameter here, not in a body — a POST-style body yields a 404.
        var path = $"api/1/partner_accounts/public_key?domain={Uri.EscapeDataString(normalized)}";

        var result = await _executor.SendAsync<PartnerPublicKeyResponse>(HttpMethod.Get, path, null, ct)
            .ConfigureAwait(false);

        return string.IsNullOrEmpty(result.Value?.PublicKey) ? null : result.Value.PublicKey;
    }

    /// <summary>
    /// Returns a valid partner access token, fetching a new one when the cached token is missing or
    /// about to expire. Serialized so concurrent calls fetch at most one token.
    /// </summary>
    private async ValueTask<string> GetPartnerTokenAsync(CancellationToken ct)
    {
        if (IsUsable(_partnerTokens))
        {
            return _partnerTokens.AccessToken;
        }

        await _tokenLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Another caller may have refreshed it while we waited.
            if (IsUsable(_partnerTokens))
            {
                return _partnerTokens.AccessToken;
            }

            _partnerTokens = await _authClient.GetPartnerTokenAsync(ct).ConfigureAwait(false);
            return _partnerTokens.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private bool IsUsable([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] TeslaTokens? tokens) =>
        tokens is not null
        && !string.IsNullOrEmpty(tokens.AccessToken)
        && tokens.ExpiresAt - ExpiryMargin > _timeProvider.GetUtcNow();

    /// <summary>
    /// Validates and normalizes a domain: Tesla expects a bare host (<c>coil.example.com</c>), so a
    /// pasted URL is rejected with an actionable message rather than sent and refused server-side.
    /// Exposed so callers can validate user input before building a client.
    /// </summary>
    /// <param name="domain">The domain to validate.</param>
    /// <returns>The domain, trimmed and lower-cased.</returns>
    /// <exception cref="ArgumentException"><paramref name="domain"/> is empty or includes a scheme or path.</exception>
    public static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            throw new ArgumentException("Domain must not be empty.", nameof(domain));
        }

        var trimmed = domain.Trim().TrimEnd('/');

        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"Domain must be a bare host without a scheme (got '{domain}'). Use e.g. 'coil.example.com'.",
                nameof(domain));
        }

        if (trimmed.Contains('/'))
        {
            throw new ArgumentException(
                $"Domain must be a bare host without a path (got '{domain}'). Use e.g. 'coil.example.com'.",
                nameof(domain));
        }

        return trimmed.ToLowerInvariant();
    }
}
