namespace Coil;

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
    public ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M2 core-client): load from store, refresh when expired, persist, return access token");
}
