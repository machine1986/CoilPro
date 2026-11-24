namespace CoilPro.Tests.TestSupport;

/// <summary>
/// Hand-rolled <see cref="ITeslaAuthClient"/> fake for <see cref="TeslaPartnerClient"/> tests: serves
/// client-credentials (partner) tokens and counts how often one was requested, so caching can be
/// asserted. The other members throw, since the partner client never uses them.
/// </summary>
internal sealed class FakePartnerAuthClient : ITeslaAuthClient
{
    private readonly Func<int, TeslaTokens> _partnerToken;

    /// <summary>Issues a non-expiring token whose value is <c>partner-token-N</c> for the Nth call.</summary>
    public FakePartnerAuthClient()
        : this(call => new TeslaTokens
        {
            AccessToken = $"partner-token-{call}",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
        })
    {
    }

    /// <summary>Issues a token computed from the 1-based call number.</summary>
    public FakePartnerAuthClient(Func<int, TeslaTokens> partnerToken)
    {
        _partnerToken = partnerToken;
    }

    /// <summary>Number of times <see cref="GetPartnerTokenAsync"/> was called.</summary>
    public int PartnerTokenCallCount { get; private set; }

    public Task<TeslaTokens> GetPartnerTokenAsync(CancellationToken ct = default)
    {
        PartnerTokenCallCount++;
        return Task.FromResult(_partnerToken(PartnerTokenCallCount));
    }

    public AuthorizationRequest CreateAuthorizationRequest(string? state = null) =>
        throw new NotSupportedException("Not exercised by TeslaPartnerClient.");

    public Task<TeslaTokens> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken ct = default) =>
        throw new NotSupportedException("Not exercised by TeslaPartnerClient.");

    public Task<TeslaTokens> RefreshAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotSupportedException("Not exercised by TeslaPartnerClient.");
}
