using System.Net.Http.Headers;

namespace Coil.Tests;

public class RateLimitInfoTests
{
    private static HttpResponseHeaders HeadersWith(params (string Name, string Value)[] pairs)
    {
        var response = new HttpResponseMessage();
        foreach (var (name, value) in pairs)
        {
            response.Headers.TryAddWithoutValidation(name, value);
        }

        return response.Headers;
    }

    [Fact]
    public void FromHeaders_WithLimitRemainingAndReset_PopulatesAllThree()
    {
        const long resetUnix = 1_800_000_000L;
        var headers = HeadersWith(
            ("x-ratelimit-limit", "100"),
            ("x-ratelimit-remaining", "42"),
            ("x-ratelimit-reset", resetUnix.ToString()));

        var info = RateLimitInfo.FromHeaders(headers);

        Assert.NotNull(info);
        Assert.Equal(100, info!.Limit);
        Assert.Equal(42, info.Remaining);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(resetUnix), info.ResetsAt);
    }

    [Fact]
    public void FromHeaders_WithRetryAfterAsDeltaSeconds_PopulatesRetryAfter()
    {
        var headers = HeadersWith(("Retry-After", "5"));

        var info = RateLimitInfo.FromHeaders(headers);

        Assert.NotNull(info);
        Assert.Equal(TimeSpan.FromSeconds(5), info!.RetryAfter);
    }

    [Fact]
    public void FromHeaders_WithRetryAfterOnly_LeavesLimitAndRemainingNull()
    {
        var headers = HeadersWith(("Retry-After", "5"));

        var info = RateLimitInfo.FromHeaders(headers);

        Assert.NotNull(info);
        Assert.Null(info!.Limit);
        Assert.Null(info.Remaining);
    }

    [Fact]
    public void FromHeaders_WithNoRelevantHeaders_ReturnsNull()
    {
        var headers = HeadersWith(("X-Some-Other-Header", "value"));

        var info = RateLimitInfo.FromHeaders(headers);

        Assert.Null(info);
    }

    [Fact]
    public void FromHeaders_WithOnlyRemaining_PopulatesRemainingAndLeavesRestNull()
    {
        var headers = HeadersWith(("x-ratelimit-remaining", "7"));

        var info = RateLimitInfo.FromHeaders(headers);

        Assert.NotNull(info);
        Assert.Equal(7, info!.Remaining);
        Assert.Null(info.Limit);
        Assert.Null(info.ResetsAt);
        Assert.Null(info.RetryAfter);
    }
}
