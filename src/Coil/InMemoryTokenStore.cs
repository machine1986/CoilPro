namespace Coil;

/// <summary>Non-persistent, thread-safe-enough default <see cref="ITokenStore"/> for a single account.</summary>
public sealed class InMemoryTokenStore : ITokenStore
{
    private TeslaTokens? _tokens;

    /// <inheritdoc />
    public ValueTask<TeslaTokens?> GetAsync(CancellationToken ct = default) => new(_tokens);

    /// <inheritdoc />
    public ValueTask SaveAsync(TeslaTokens tokens, CancellationToken ct = default)
    {
        _tokens = tokens;
        return default;
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken ct = default)
    {
        _tokens = null;
        return default;
    }
}
