using System.Net.Http.Headers;

namespace Coil;

/// <summary>
/// Snapshot of Tesla Fleet API rate-limit state, read from the response headers. Because the Fleet API
/// is pay-per-use, callers should surface remaining budget to end users.
/// </summary>
/// <remarks>
/// The exact header names are provider-specific and MUST be verified against developer.tesla.com;
/// record any deviation in <c>docs/api-deviations.md</c>.
/// </remarks>
public sealed record RateLimitInfo
{
    /// <summary>Maximum number of requests permitted in the current window.</summary>
    public int? Limit { get; init; }

    /// <summary>Requests remaining in the current window.</summary>
    public int? Remaining { get; init; }

    /// <summary>When the current window resets.</summary>
    public DateTimeOffset? ResetsAt { get; init; }

    /// <summary>Server-suggested wait before retrying (from <c>Retry-After</c>), if present.</summary>
    public TimeSpan? RetryAfter { get; init; }

    /// <summary>
    /// Parses a <see cref="RateLimitInfo"/> from response headers. Returns null when no relevant headers exist.
    /// </summary>
    public static RateLimitInfo? FromHeaders(HttpResponseHeaders headers) =>
        throw new NotImplementedException("TODO(M2 core-client): parse rate-limit + Retry-After headers; verify names vs docs");
}
