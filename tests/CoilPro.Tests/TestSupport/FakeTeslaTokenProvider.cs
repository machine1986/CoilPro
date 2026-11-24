namespace CoilPro.Tests.TestSupport;

/// <summary>
/// Hand-rolled <see cref="ITeslaTokenProvider"/> fake that always returns a fixed token string,
/// used by <see cref="CoilProClient"/> tests that don't care about token refresh mechanics
/// (that's covered separately by <c>TeslaTokenProviderTests</c>).
/// </summary>
internal sealed class FakeTeslaTokenProvider : ITeslaTokenProvider
{
    private readonly string _token;

    public FakeTeslaTokenProvider(string token = "tok")
    {
        _token = token;
    }

    /// <summary>Number of times <see cref="GetAccessTokenAsync"/> was called.</summary>
    public int CallCount { get; private set; }

    public ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        CallCount++;
        return new ValueTask<string>(_token);
    }
}
