using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CoilPro;

/// <summary>
/// Snapshot of Tesla Fleet API rate-limit state, read from the response headers. Because the Fleet API
/// is pay-per-use, callers should surface remaining budget to end users.
/// </summary>
/// <remarks>
/// Header format confirmed against the live API — see <see cref="FromHeaders"/> and
/// <c>docs/api-deviations.md</c>.
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
    /// The rate-limit bucket the numbers belong to, taken from the header name — e.g.
    /// <c>device-data-per-vehicle</c> for <c>ratelimit-device-data-per-vehicle-remaining</c>. Null for
    /// un-categorized headers.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Parses a <see cref="RateLimitInfo"/> from response headers. Returns null when no relevant headers exist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Confirmed live (EU, 2026-11-24): Tesla sends one header triple per bucket, named
    /// <c>ratelimit-&lt;bucket&gt;-limit</c> / <c>-remaining</c> / <c>-reset</c>, e.g.
    /// <c>ratelimit-device-data-per-vehicle-remaining: 58</c>. <c>reset</c> is a small number of
    /// <b>seconds until</b> the window resets, not a Unix timestamp. The generic
    /// <c>x-ratelimit-*</c> / <c>ratelimit-*</c> forms are accepted too.
    /// </para>
    /// <para>
    /// If several buckets are present, the one with the fewest remaining requests wins — that is the one
    /// that will throttle first. A <c>reset</c> value of at least 1,000,000,000 is read as a Unix timestamp,
    /// anything smaller as seconds from <paramref name="now"/>. <c>Retry-After</c> (RFC 9110) is read
    /// regardless.
    /// </para>
    /// </remarks>
    /// <param name="headers">Response headers.</param>
    /// <param name="now">Reference time for relative <c>reset</c> values. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public static RateLimitInfo? FromHeaders(HttpResponseHeaders headers, DateTimeOffset? now = null)
    {
        var buckets = new Dictionary<string, (int? Limit, int? Remaining, long? Reset)>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            var match = HeaderPattern.Match(header.Key);
            if (!match.Success
                || !long.TryParse(header.Value.FirstOrDefault(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            var category = match.Groups["category"].Success ? match.Groups["category"].Value : string.Empty;
            buckets.TryGetValue(category, out var bucket);
            bucket = match.Groups["field"].Value.ToLowerInvariant() switch
            {
                "limit" => bucket with { Limit = (int)Math.Clamp(value, int.MinValue, int.MaxValue) },
                "remaining" => bucket with { Remaining = (int)Math.Clamp(value, int.MinValue, int.MaxValue) },
                _ => bucket with { Reset = value },
            };
            buckets[category] = bucket;
        }

        var retryAfter = ParseRetryAfter(headers);

        if (buckets.Count == 0)
        {
            return retryAfter is null ? null : new RateLimitInfo { RetryAfter = retryAfter };
        }

        var (name, chosen) = buckets
            .OrderBy(b => b.Value.Remaining ?? int.MaxValue)
            .ThenBy(b => b.Key, StringComparer.Ordinal)
            .First();

        return new RateLimitInfo
        {
            Category = name.Length == 0 ? null : name,
            Limit = chosen.Limit,
            Remaining = chosen.Remaining,
            ResetsAt = chosen.Reset switch
            {
                null => null,
                >= UnixTimestampThreshold => DateTimeOffset.FromUnixTimeSeconds(chosen.Reset.Value),
                _ => (now ?? DateTimeOffset.UtcNow).AddSeconds(chosen.Reset.Value),
            },
            RetryAfter = retryAfter,
        };
    }

    private const long UnixTimestampThreshold = 1_000_000_000;

    private static readonly Regex HeaderPattern = new(
        "^(?:x-)?ratelimit-(?:(?<category>.+)-)?(?<field>limit|remaining|reset)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
