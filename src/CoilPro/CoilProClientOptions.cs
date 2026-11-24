namespace CoilPro;

/// <summary>
/// Tunables for <see cref="CoilProClient"/>'s resilience pipeline. Defaults are sensible for production;
/// tests set <see cref="BaseRetryDelay"/> to zero for fast, deterministic runs.
/// </summary>
public sealed class CoilProClientOptions
{
    /// <summary>Maximum retry attempts for transient failures (408, 429, 5xx). Default 3.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Base delay for exponential backoff. Default 500&#160;ms. Set to <see cref="TimeSpan.Zero"/> in tests.</summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Whether to add jitter to backoff delays. Default true.</summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>Honor the server's <c>Retry-After</c> header for 429/503 responses when present. Default true.</summary>
    public bool RespectRetryAfter { get; set; } = true;
}
