namespace CoilPro.Tests.TestSupport;

/// <summary>
/// Hand-rolled <see cref="ITeslaAuthClient"/> fake used by <c>TeslaTokenProviderTests</c> to control
/// (and assert on) refresh behavior without performing HTTP. Only <see cref="RefreshAsync"/> is
/// expected to be exercised by <see cref="TeslaTokenProvider"/>; the other members throw if called,
/// since they're outside the token-provider's contract.
/// </summary>
internal sealed class FakeTeslaAuthClient : ITeslaAuthClient
{
    private readonly Func<string, TeslaTokens> _refresh;

    public FakeTeslaAuthClient(Func<string, TeslaTokens> refresh)
    {
        _refresh = refresh;
    }

    /// <summary>Number of times <see cref="RefreshAsync"/> was called.</summary>
    public int RefreshCallCount { get; private set; }

    /// <summary>The refresh token passed to the most recent <see cref="RefreshAsync"/> call.</summary>
    public string? LastRefreshToken { get; private set; }

    public Task<TeslaTokens> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        RefreshCallCount++;
        LastRefreshToken = refreshToken;
        return Task.FromResult(_refresh(refreshToken));
    }

    public AuthorizationRequest CreateAuthorizationRequest(string? state = null) =>
        throw new NotSupportedException("Not exercised by TeslaTokenProvider.");

    public Task<TeslaTokens> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default) =>
        throw new NotSupportedException("Not exercised by TeslaTokenProvider.");

    public Task<TeslaTokens> GetPartnerTokenAsync(CancellationToken ct = default) =>
        throw new NotSupportedException("Not exercised by TeslaTokenProvider.");
}
