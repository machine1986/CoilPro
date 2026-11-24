namespace CoilPro;

/// <summary>
/// Abstraction for persisting <see cref="TeslaTokens"/>. Default implementation:
/// <see cref="InMemoryTokenStore"/>.
/// </summary>
public interface ITokenStore
{
    /// <summary>Returns the stored tokens, or null if none.</summary>
    ValueTask<TeslaTokens?> GetAsync(CancellationToken ct = default);

    /// <summary>Persists the tokens, replacing any existing ones.</summary>
    ValueTask SaveAsync(TeslaTokens tokens, CancellationToken ct = default);

    /// <summary>Clears the stored tokens.</summary>
    ValueTask ClearAsync(CancellationToken ct = default);
}
