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
    /// <remarks>
    /// NOTE: header names <c>x-ratelimit-limit</c> / <c>x-ratelimit-remaining</c> / <c>x-ratelimit-reset</c>
    /// are NOT confirmed against current developer.tesla.com docs at time of writing — Tesla's Fleet API
    /// rate-limit header contract is not fully public. Verify before release and record any deviation in
    /// <c>docs/api-deviations.md</c>. <c>Retry-After</c> is a standard HTTP header (RFC 9110) and is read
    /// regardless of whether the rate-limit headers are present.
    /// </remarks>
    public static RateLimitInfo? FromHeaders(HttpResponseHeaders headers)
    {
        var hasLimit = TryGetInt(headers, "x-ratelimit-limit", out var limit);
        var hasRemaining = TryGetInt(headers, "x-ratelimit-remaining", out var remaining);
        var hasReset = TryGetInt(headers, "x-ratelimit-reset", out var resetEpochSeconds);
        var retryAfter = ParseRetryAfter(headers);

        if (!hasLimit && !hasRemaining && !hasReset && retryAfter is null)
        {
            return null;
        }

        return new RateLimitInfo
        {
            Limit = hasLimit ? limit : null,
            Remaining = hasRemaining ? remaining : null,
            ResetsAt = hasReset ? DateTimeOffset.FromUnixTimeSeconds(resetEpochSeconds) : null,
            RetryAfter = retryAfter,
        };
    }

    private static bool TryGetInt(HttpResponseHeaders headers, string name, out int value)
    {
        value = 0;
        if (!headers.TryGetValues(name, out var values))
        {
            return false;
        }

        var raw = values.FirstOrDefault();
        return raw is not null && int.TryParse(raw, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    private static TimeSpan? ParseRetryAfter(HttpResponseHeaders headers)
    {
        if (headers.RetryAfter is { } retryAfter)
        {
            if (retryAfter.Delta is { } delta)
            {
                return delta;
            }

            if (retryAfter.Date is { } date)
            {
                var delta2 = date - DateTimeOffset.UtcNow;
                return delta2 > TimeSpan.Zero ? delta2 : TimeSpan.Zero;
            }
        }

        return null;
    }
}
