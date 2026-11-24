using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CoilPro.Commands.Internal.Telemetry;

/// <summary>
/// Posts a signed telemetry configuration to <c>POST api/1/vehicles/fleet_telemetry_config_jws</c> with the
/// user's bearer token and unwraps the <c>{ "response": ... }</c> envelope. Sent exactly once — it is a mutation.
/// </summary>
internal sealed class TelemetryConfigSender
{
    /// <summary>Request path, relative to the regional Fleet API base address.</summary>
    public const string Path = "api/1/vehicles/fleet_telemetry_config_jws";

    private readonly HttpClient _http;
    private readonly ITeslaTokenProvider _tokenProvider;

    public TelemetryConfigSender(HttpClient http, ITeslaTokenProvider tokenProvider)
    {
        _http = http;
        _tokenProvider = tokenProvider;
    }

    /// <summary>Posts <c>{"vins": [...], "token": "&lt;jws&gt;"}</c> and returns the parsed <c>response</c> object.</summary>
    public async Task<FleetTelemetryConfigResult> SendAsync(IReadOnlyList<string> vins, string token, CancellationToken ct)
    {
        var body = JsonSerializer.Serialize(new Dictionary<string, object> { ["vins"] = vins, ["token"] = token });
        var accessToken = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Path, UriKind.Relative))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, ct).ConfigureAwait(false);
        var rateLimit = RateLimitInfo.FromHeaders(response.Headers);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorCode = TryParseErrorCode(responseBody);
            var message = errorCode is not null
                ? $"fleet_telemetry_config_jws failed with {(int)response.StatusCode} {response.ReasonPhrase}: {errorCode}"
                : $"fleet_telemetry_config_jws failed with {(int)response.StatusCode} {response.ReasonPhrase}.";
            throw new TeslaApiException(response.StatusCode, message, errorCode, rateLimit?.RetryAfter, responseBody, rateLimit);
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("response", out var payload)
                && payload.ValueKind == JsonValueKind.Object)
            {
                return payload.Deserialize<FleetTelemetryConfigResult>()
                    ?? throw new JsonException("response is null");
            }

            throw new JsonException("missing \"response\" object");
        }
        catch (JsonException ex)
        {
            throw new TeslaApiException(
                response.StatusCode,
                $"Failed to parse fleet_telemetry_config_jws response: {ex.Message}",
                responseBody: responseBody,
                rateLimit: rateLimit);
        }
    }

    /// <summary>Best-effort <c>error</c> / <c>error_description</c> from a Fleet API error body.</summary>
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

            foreach (var name in (ReadOnlySpan<string>)["error", "error_description"])
            {
                if (doc.RootElement.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(value.GetString()))
                {
                    return value.GetString();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
