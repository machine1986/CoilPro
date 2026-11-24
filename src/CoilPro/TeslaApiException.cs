using System.Net;

namespace CoilPro;

/// <summary>
/// Thrown when the Tesla Fleet API returns a non-success response. Carries the HTTP status,
/// the Tesla error code (when present), the <c>Retry-After</c> hint, and the raw body.
/// </summary>
public sealed class TeslaApiException : Exception
{
    /// <summary>HTTP status code of the failing response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>Tesla-specific error code / short error string, if the body contained one.</summary>
    public string? ErrorCode { get; }

    /// <summary>Suggested wait before retrying, parsed from the <c>Retry-After</c> header, if present.</summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>Raw response body, for diagnostics. May be null/empty.</summary>
    public string? ResponseBody { get; }

    /// <summary>Rate-limit snapshot observed on the failing response, if any headers were present.</summary>
    public RateLimitInfo? RateLimit { get; }

    /// <summary>Creates a new <see cref="TeslaApiException"/>.</summary>
    public TeslaApiException(
        HttpStatusCode statusCode,
        string message,
        string? errorCode = null,
        TimeSpan? retryAfter = null,
        string? responseBody = null,
        RateLimitInfo? rateLimit = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        RetryAfter = retryAfter;
        ResponseBody = responseBody;
        RateLimit = rateLimit;
    }
}
