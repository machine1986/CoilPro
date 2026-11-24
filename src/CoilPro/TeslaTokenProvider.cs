namespace CoilPro;

/// <summary>
/// Default <see cref="ITeslaTokenProvider"/>: reads tokens from an <see cref="ITokenStore"/> and,
/// when the access token is expired (within a small leeway) and a refresh token is present, refreshes
/// via <see cref="ITeslaAuthClient"/> and writes the new tokens back to the store.
/// </summary>
public sealed class TeslaTokenProvider : ITeslaTokenProvider
{
    private readonly ITokenStore _store;
    private readonly ITeslaAuthClient _authClient;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Refresh a little before actual expiry to avoid racing the clock.</summary>
    private static readonly TimeSpan ExpiryLeeway = TimeSpan.FromSeconds(60);

    /// <summary>Creates the provider.</summary>
    public TeslaTokenProvider(ITokenStore store, ITeslaAuthClient authClient, TimeProvider? timeProvider = null)
    {
        _store = store;
        _authClient = authClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var tokens = await _store.GetAsync(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No stored tokens — run `coilpro login`.");

        var now = _timeProvider.GetUtcNow();
        if (!tokens.IsExpired(now + ExpiryLeeway))
        {
            return tokens.AccessToken;
        }

        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            // Expired and unrefreshable: throw rather than spend a pay-per-use API call that will 401.
            throw new InvalidOperationException(
                "Access token has expired and no refresh token is available — run `coilpro login` again.");
        }

        await _refreshLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            // Re-read: another caller may have refreshed while we waited for the lock.
            tokens = await _store.GetAsync(ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("No stored tokens — run `coilpro login`.");

            now = _timeProvider.GetUtcNow();
            if (!tokens.IsExpired(now + ExpiryLeeway))
            {
                return tokens.AccessToken;
            }

            if (string.IsNullOrEmpty(tokens.RefreshToken))
            {
                throw new InvalidOperationException(
                    "Access token has expired and no refresh token is available — run `coilpro login` again.");
            }

            var refreshed = await _authClient.RefreshAsync(tokens.RefreshToken, ct).ConfigureAwait(false);
            await _store.SaveAsync(refreshed, ct).ConfigureAwait(false);
            return refreshed.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
