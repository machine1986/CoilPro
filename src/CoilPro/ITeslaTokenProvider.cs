namespace CoilPro;

/// <summary>
/// Supplies a valid Tesla Fleet API access token to <see cref="ICoilProClient"/>, refreshing it
/// transparently when it has expired.
/// </summary>
public interface ITeslaTokenProvider
{
    /// <summary>
    /// Returns a currently-valid access token, refreshing via the stored refresh token if needed.
    /// Throws <see cref="InvalidOperationException"/> when no usable tokens are available.
    /// </summary>
    ValueTask<string> GetAccessTokenAsync(CancellationToken ct = default);
}
