using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Coil.Internal;
using Polly;
using Polly.Retry;

namespace Coil;

/// <summary>
/// Default <see cref="ICoilClient"/> backed by an <see cref="HttpClient"/> whose
/// <see cref="HttpClient.BaseAddress"/> is the Fleet API host for the account's region.
/// </summary>
public sealed class CoilClient : ICoilClient
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
    private readonly ITeslaTokenProvider _tokenProvider;
    private readonly CoilClientOptions _options;
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    /// <summary>Creates the client.</summary>
    /// <param name="http">HTTP client with <see cref="HttpClient.BaseAddress"/> set to the Fleet API base URL (see <see cref="TeslaEndpoints.FleetApiBaseUrl"/>).</param>
    /// <param name="tokenProvider">Supplies (and refreshes) the bearer token.</param>
    /// <param name="options">Resilience tunables; defaults are used when null.</param>
    public CoilClient(HttpClient http, ITeslaTokenProvider tokenProvider, CoilClientOptions? options = null)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _options = options ?? new CoilClientOptions();
        _pipeline = BuildPipeline(_options);
    }

    /// <inheritdoc />
    public RateLimitInfo? LastRateLimit { get; private set; }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default)
    {
        var result = await SendAsync<List<Vehicle>>(HttpMethod.Get, "api/1/vehicles", ct).ConfigureAwait(false);
        return result.Value ?? [];
    }

    /// <inheritdoc />
    public async Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default)
    {
        var path = $"api/1/vehicles/{Uri.EscapeDataString(vehicleTag)}/vehicle_data";
        var queryValue = endpoints.ToQueryValue();
        if (queryValue is not null)
        {
            path = $"{path}?endpoints={Uri.EscapeDataString(queryValue)}";
        }

        var result = await SendAsync<VehicleData>(HttpMethod.Get, path, ct).ConfigureAwait(false);
        return result.Value ?? throw new TeslaApiException(
            result.StatusCode,
            "Fleet API returned a success status with an empty vehicle_data response.",
            responseBody: null,
            rateLimit: LastRateLimit);
    }

    /// <inheritdoc />
    public async Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default)
    {
        var path = $"api/1/vehicles/{Uri.EscapeDataString(vehicleTag)}/wake_up";
        var result = await SendAsync<Vehicle>(HttpMethod.Post, path, ct).ConfigureAwait(false);
        return result.Value ?? throw new TeslaApiException(
            result.StatusCode,
            "Fleet API returned a success status with an empty wake_up response.",
            responseBody: null,
            rateLimit: LastRateLimit);
    }

    /// <inheritdoc />
    public async Task<ChargingHistoryPage> GetChargingHistoryAsync(CancellationToken ct = default)
    {
        // PROVISIONAL: path, query parameters and response envelope for the charging-history endpoint
        // are not confirmed against current developer.tesla.com docs. Verify before release and record
        // any deviation in docs/api-deviations.md.
        var result = await SendAsync<ChargingHistoryPage>(HttpMethod.Get, "api/1/dx/charging/history", ct).ConfigureAwait(false);
        return result.Value ?? new ChargingHistoryPage();
    }

    /// <summary>Sends a request through the resilience pipeline and unwraps the <c>{ "response": ... }</c> envelope.</summary>
    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string path, CancellationToken ct)
    {
        var response = await _pipeline.ExecuteAsync(async token =>
        {
            using var request = new HttpRequestMessage(method, path);
            var accessToken = await _tokenProvider.GetAccessTokenAsync(token).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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

            TeslaResponse<T>? envelope;
            try
            {
                envelope = string.IsNullOrEmpty(body)
                    ? null
                    : JsonSerializer.Deserialize<TeslaResponse<T>>(body, JsonOptions);
            }
            catch (JsonException ex)
            {
                throw new TeslaApiException(
                    response.StatusCode,
                    $"Failed to parse Fleet API response body: {ex.Message}",
                    responseBody: body,
                    rateLimit: LastRateLimit);
            }

            return new ApiResult<T>(envelope is null ? default : envelope.Response, response.StatusCode);
        }
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
    /// Best-effort extraction of a Tesla error string from the response body. The Fleet API error shape
    /// is NOT fully documented/confirmed — observed variants use top-level <c>error</c> and/or
    /// <c>error_description</c> string fields. Verify against developer.tesla.com and record deviations
    /// in <c>docs/api-deviations.md</c>.
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

    private static ResiliencePipeline<HttpResponseMessage> BuildPipeline(CoilClientOptions options)
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

    private readonly record struct ApiResult<T>(T? Value, HttpStatusCode StatusCode);
}
