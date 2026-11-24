using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Polly;
using Polly.Retry;

namespace CoilPro.Internal;

/// <summary>
/// Shared Fleet API request plumbing: the resilience pipeline, bearer authentication, rate-limit
/// capture, <c>{ "response": ... }</c> envelope unwrapping and error-to-<see cref="TeslaApiException"/>
/// mapping.
/// </summary>
/// <remarks>
/// Used by both <see cref="CoilProClient"/> (end-user tokens from an <see cref="ITeslaTokenProvider"/>)
/// and <see cref="TeslaPartnerClient"/> (partner tokens from the client-credentials flow). The only
/// difference between the two is where the bearer token comes from, which is why that is injected as
/// a delegate rather than a typed dependency.
/// </remarks>
internal sealed class FleetApiExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    /// <summary>HTTP statuses considered transient and worth retrying.</summary>
    private static readonly HashSet<HttpStatusCode> TransientStatusCodes =
    [
        HttpStatusCode.RequestTimeout,      // 408
        HttpStatusCode.TooManyRequests,     // 429
        HttpStatusCode.InternalServerError, // 500
        HttpStatusCode.BadGateway,          // 502
        HttpStatusCode.ServiceUnavailable,  // 503
        HttpStatusCode.GatewayTimeout,      // 504
    ];

    private readonly HttpClient _http;
    private readonly Func<CancellationToken, ValueTask<string>> _tokenFactory;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>Creates an executor over <paramref name="http"/>, taking bearer tokens from <paramref name="tokenFactory"/>.</summary>
    public FleetApiExecutor(
        HttpClient http,
        Func<CancellationToken, ValueTask<string>> tokenFactory,
        CoilProClientOptions options)
    {
        _http = http;
        _tokenFactory = tokenFactory;
        _pipeline = BuildPipeline(options);
    }

    /// <summary>Rate-limit snapshot from the most recent response, success or failure. Null until the first call.</summary>
    public RateLimitInfo? LastRateLimit { get; private set; }

    /// <summary>
    /// Sends a request through the resilience pipeline and unwraps the <c>{ "response": ... }</c> envelope.
    /// </summary>
    /// <param name="method">HTTP method.</param>
    /// <param name="path">Request path relative to the client's base address.</param>
    /// <param name="contentFactory">
    /// Optional factory for the request body. A factory rather than an instance because each retry
    /// attempt needs a fresh <see cref="HttpContent"/> — a consumed one cannot be re-sent.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="unwrapEnvelope">
    /// True (default) for the usual <c>{ "response": ... }</c> envelope. False for endpoints that return
    /// the payload bare — confirmed live for <c>api/1/dx/charging/history</c>, which answers
    /// <c>{ "data": [...], "totalResults": n }</c> at the top level.
    /// </param>
    /// <param name="retryable">
    /// False for non-idempotent mutations (POST commands, energy settings): the request is sent once and a
    /// transient failure surfaces instead of being retried, because the first attempt may have taken effect.
    /// </param>
    public async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method,
        string path,
        Func<HttpContent>? contentFactory,
        CancellationToken ct,
        bool unwrapEnvelope = true,
        bool retryable = true)
    {
        var (status, body) = await SendForBodyAsync(method, path, contentFactory, retryable, ct).ConfigureAwait(false);
        {
            var response = (StatusCode: status, Body: body);
            T? payload;
            try
            {
                payload = string.IsNullOrEmpty(body)
                    ? default
                    : unwrapEnvelope
                        ? JsonSerializer.Deserialize<TeslaResponse<T>>(body, JsonOptions) is { } envelope ? envelope.Response : default
                        : JsonSerializer.Deserialize<T>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new TeslaApiException(
                    response.StatusCode,
                    $"Failed to parse Fleet API response body: {ex.Message}",
                    responseBody: body,
                    rateLimit: LastRateLimit);
            }

            return new ApiResult<T>(payload, response.StatusCode);
        }
    }

    /// <summary>
    /// Like <see cref="SendAsync{T}"/> for list endpoints that put <c>pagination</c> and <c>count</c> next to
    /// <c>response</c> at the top level of the envelope.
    /// </summary>
    public async Task<ApiPageResult<T>> SendPagedAsync<T>(HttpMethod method, string path, CancellationToken ct)
    {
        var (status, body) = await SendForBodyAsync(method, path, contentFactory: null, retryable: true, ct).ConfigureAwait(false);
        try
        {
            var envelope = string.IsNullOrEmpty(body) ? null : JsonSerializer.Deserialize<TeslaResponse<T>>(body, JsonOptions);
            return new ApiPageResult<T>(envelope is null ? default : envelope.Response, envelope?.Pagination, envelope?.Count, status);
        }
        catch (JsonException ex)
        {
            throw new TeslaApiException(status, $"Failed to parse Fleet API response body: {ex.Message}", responseBody: body, rateLimit: LastRateLimit);
        }
    }

    /// <summary>Sends a GET and returns the raw response bytes (e.g. a PDF invoice). Errors map like <see cref="SendAsync{T}"/>.</summary>
    public async Task<byte[]> SendForBytesAsync(string path, CancellationToken ct)
    {
        var response = await _pipeline.ExecuteAsync(async token =>
        {
            using var request = await CreateRequestAsync(HttpMethod.Get, path, contentFactory: null, token).ConfigureAwait(false);
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            return await _http.SendAsync(request, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        using (response)
        {
            LastRateLimit = RateLimitInfo.FromHeaders(response.Headers);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                throw BuildException(response, errorBody);
            }

            return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
        }
    }

    /// <summary>Creates a JSON request-body factory (a fresh <see cref="HttpContent"/> per attempt).</summary>
    public static Func<HttpContent> JsonBody<TBody>(TBody body) =>
        () => new StringContent(JsonSerializer.Serialize(body, JsonOptions), System.Text.Encoding.UTF8, "application/json");

    private async Task<(HttpStatusCode Status, string Body)> SendForBodyAsync(
        HttpMethod method,
        string path,
        Func<HttpContent>? contentFactory,
        bool retryable,
        CancellationToken ct)
    {
        var pipeline = retryable ? _pipeline : ResiliencePipeline<HttpResponseMessage>.Empty;
        var response = await pipeline.ExecuteAsync(async token =>
        {
            using var request = await CreateRequestAsync(method, path, contentFactory, token).ConfigureAwait(false);
            return await _http.SendAsync(request, token).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

        using (response)
        {
            // Update from every response we observe — success or failure.
            LastRateLimit = RateLimitInfo.FromHeaders(response.Headers);

            var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw BuildException(response, body);
            }

            return (response.StatusCode, body);
        }
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, Func<HttpContent>? contentFactory, CancellationToken ct)
    {
        var request = new HttpRequestMessage(method, path);
        if (contentFactory is not null)
        {
            request.Content = contentFactory();
        }

        var accessToken = await _tokenFactory(ct).ConfigureAwait(false);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    /// <summary>Builds a <see cref="TeslaApiException"/> from a non-success response, parsing a best-effort error code.</summary>
    private TeslaApiException BuildException(HttpResponseMessage response, string body)
    {
        var errorCode = TryParseErrorCode(body);
        var retryAfter = LastRateLimit?.RetryAfter;

        var message = errorCode is not null
            ? $"Tesla Fleet API request failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorCode}"
            : $"Tesla Fleet API request failed with {(int)response.StatusCode} {response.ReasonPhrase}.";

        return new TeslaApiException(response.StatusCode, message, errorCode, retryAfter, body, LastRateLimit);
    }

    /// <summary>
    /// Best-effort extraction of a Tesla error string from the response body. Observed shape is
    /// <c>{ "response": null, "error": "...", "error_description": "...", "txid": "..." }</c>; the
    /// <c>error_description</c> is frequently empty, so <c>error</c> wins when both are present.
    /// </summary>
    private static string? TryParseErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
            {
                return error.GetString();
            }

            if (doc.RootElement.TryGetProperty("error_description", out var errorDescription) && errorDescription.ValueKind == JsonValueKind.String)
            {
                return errorDescription.GetString();
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(CoilProClientOptions options)
    {
        // MaxRetries <= 0 disables retries entirely (Polly requires MaxRetryAttempts >= 1).
        if (options.MaxRetries <= 0)
        {
            return ResiliencePipeline<HttpResponseMessage>.Empty;
        }

        var retryOptions = new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = options.MaxRetries,
            BackoffType = DelayBackoffType.Exponential,
            Delay = options.BaseRetryDelay,
            UseJitter = options.UseJitter,
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .HandleResult(r => TransientStatusCodes.Contains(r.StatusCode)),
            OnRetry = args =>
            {
                // Dispose the failed attempt's response before retrying — the pipeline does not do this for us.
                args.Outcome.Result?.Dispose();
                return default;
            },
        };

        if (options.RespectRetryAfter)
        {
            retryOptions.DelayGenerator = args =>
            {
                var retryAfter = args.Outcome.Result is { } response
                    ? RateLimitInfo.FromHeaders(response.Headers)?.RetryAfter
                    : null;
                return new ValueTask<TimeSpan?>(retryAfter);
            };
        }

        return new ResiliencePipelineBuilder<HttpResponseMessage>()
            .AddRetry(retryOptions)
            .Build();
    }
}

/// <summary>The unwrapped payload of a Fleet API response plus the status it arrived with.</summary>
internal readonly record struct ApiResult<T>(T? Value, HttpStatusCode StatusCode);

/// <summary>The unwrapped payload of a paged list response plus its top-level <c>pagination</c> and <c>count</c>.</summary>
internal readonly record struct ApiPageResult<T>(T? Value, TeslaPagination? Pagination, int? Count, HttpStatusCode StatusCode);
