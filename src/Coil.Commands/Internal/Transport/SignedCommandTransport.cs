using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Google.Protobuf;
using UniversalMessage;

namespace Coil.Commands.Internal.Transport;

/// <summary>
/// Sends a <see cref="RoutableMessage"/> to <c>POST api/1/vehicles/{vin}/signed_command</c> and returns the
/// vehicle's reply. FROZEN CONTRACT (M3 stage 0).
/// </summary>
internal interface ISignedCommandTransport
{
    /// <summary>
    /// Body <c>{"routable_message":"&lt;std base64&gt;"}</c>; reply <c>{"response":"&lt;base64 RoutableMessage&gt;"}</c>.
    /// Resends the same bytes only where that is safe (408 not "offline", 421 with host switch, network errors
    /// before the request was sent); never after other 5xx, because the command may have executed.
    /// </summary>
    /// <exception cref="TeslaVehicleAsleepException">503, or 408 with "vehicle is offline" in the body.</exception>
    /// <exception cref="TeslaCommandException">Any other non-success status (with <see cref="TeslaCommandException.StatusCode"/>), or an undecodable reply.</exception>
    Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct);
}

/// <summary>HTTP implementation of <see cref="ISignedCommandTransport"/> against the Fleet API.</summary>
/// <remarks>
/// Deliberately does not use the Polly pipeline of <c>FleetApiExecutor</c>: a generic retry of a 5xx could
/// execute a command twice. Only 408 (not "offline") and 421 (after switching host) are resent, with the
/// same bytes; network errors are surfaced, not retried, because the request may have reached the vehicle.
/// </remarks>
internal sealed class FleetSignedCommandTransport : ISignedCommandTransport
{
    /// <summary>Maximum accepted response size, as in Tesla's reference client.</summary>
    internal const int MaxResponseBytes = 100_000;

    private const int Max408Attempts = 3;
    private static readonly TimeSpan Retry408Delay = TimeSpan.FromSeconds(1);
    private static readonly Regex BaseUrlRegex = new(@"use base URL: https://([-a-z0-9.]*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly HttpClient _http;
    private readonly ITeslaTokenProvider _tokenProvider;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _retry408Delay;
    private volatile string? _hostOverride;

    /// <param name="http">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host.</param>
    /// <param name="tokenProvider">Supplies the user's bearer token.</param>
    public FleetSignedCommandTransport(HttpClient http, ITeslaTokenProvider tokenProvider)
        : this(http, tokenProvider, TimeProvider.System, Retry408Delay)
    {
    }

    internal FleetSignedCommandTransport(HttpClient http, ITeslaTokenProvider tokenProvider, TimeProvider timeProvider, TimeSpan retry408Delay)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _http = http;
        _tokenProvider = tokenProvider;
        _timeProvider = timeProvider;
        _retry408Delay = retry408Delay;
    }

    public async Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct)
    {
        if (vin is null || vin.Length != 17)
        {
            throw new ArgumentException("A VIN must be exactly 17 characters.", nameof(vin));
        }

        ArgumentNullException.ThrowIfNull(message);

        var json = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["routable_message"] = Convert.ToBase64String(message.ToByteArray()),
        });

        var attempts408 = 0;
        var switchedHost = false;
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var (status, body, truncated, retryAfter) = await PostOnceAsync(vin, json, ct).ConfigureAwait(false);

            switch (status)
            {
                case HttpStatusCode.OK:
                    if (truncated)
                    {
                        throw new TeslaCommandException("Response from signed_command exceeds the maximum length.", vin, statusCode: status);
                    }

                    return ParseReply(vin, body);

                case HttpStatusCode.UnprocessableEntity:
                    throw new TeslaCommandException("The vehicle does not support the Vehicle Command Protocol.", vin, statusCode: status);

                case HttpStatusCode.ServiceUnavailable:
                    throw new TeslaVehicleAsleepException(vin, status);

                case HttpStatusCode.RequestTimeout:
                    if (body.Contains("vehicle is offline", StringComparison.Ordinal))
                    {
                        throw new TeslaVehicleAsleepException(vin, status, "vehicle is offline");
                    }

                    if (++attempts408 < Max408Attempts)
                    {
                        await Task.Delay(_retry408Delay, _timeProvider, ct).ConfigureAwait(false);
                        continue;
                    }

                    throw new TeslaCommandException($"signed_command timed out (HTTP 408) after {attempts408} attempts: {Trim(body)}", vin, statusCode: status);

                case HttpStatusCode.MisdirectedRequest:
                    var match = BaseUrlRegex.Match(body);
                    if (!switchedHost && match.Success && IsValidTeslaHost(match.Groups[1].Value))
                    {
                        switchedHost = true;
                        _hostOverride = match.Groups[1].Value;
                        continue;
                    }

                    throw new TeslaCommandException($"signed_command was sent to the wrong region (HTTP 421): {Trim(body)}", vin, statusCode: status);

                case HttpStatusCode.TooManyRequests:
                    throw new TeslaCommandException(
                        $"signed_command was rate limited (HTTP 429){(retryAfter is null ? string.Empty : $", Retry-After: {retryAfter}")}: {Trim(body)}",
                        vin,
                        statusCode: status);

                default:
                    throw new TeslaCommandException($"signed_command failed with HTTP {(int)status}: {Trim(body)}", vin, statusCode: status);
            }
        }
    }

    internal static bool IsValidTeslaHost(string host) =>
        host.EndsWith(".tesla.com", StringComparison.Ordinal) ||
        host.EndsWith(".tesla.cn", StringComparison.Ordinal) ||
        host.EndsWith(".teslamotors.com", StringComparison.Ordinal);

    private async Task<(HttpStatusCode Status, string Body, bool Truncated, string? RetryAfter)> PostOnceAsync(string vin, string json, CancellationToken ct)
    {
        var path = $"api/1/vehicles/{vin}/signed_command";
        string token;
        try
        {
            token = await _tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            throw new TeslaCommandException("No usable access token for signed_command.", vin, innerException: ex);
        }

        var host = _hostOverride;
        using var request = new HttpRequestMessage(HttpMethod.Post, host is null ? new Uri(path, UriKind.Relative) : new Uri($"https://{host}/{path}"))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            var retryAfter = response.Headers.RetryAfter?.ToString();
            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            var buffer = new byte[MaxResponseBytes + 1];
            var total = 0;
            while (total < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(total), ct).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                total += read;
            }

            var truncated = total > MaxResponseBytes;
            var body = Encoding.UTF8.GetString(buffer, 0, Math.Min(total, MaxResponseBytes));
            return (response.StatusCode, body, truncated, retryAfter);
        }
        catch (HttpRequestException ex)
        {
            throw new TeslaCommandException($"signed_command request failed: {ex.Message}", vin, innerException: ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new TeslaCommandException("signed_command request timed out.", vin, innerException: ex);
        }
    }

    private static RoutableMessage ParseReply(string vin, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var encoded = doc.RootElement.GetProperty("response").GetString()
                ?? throw new FormatException("response is null");
            return RoutableMessage.Parser.ParseFrom(Convert.FromBase64String(encoded));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or InvalidProtocolBufferException)
        {
            throw new TeslaCommandException("signed_command returned an undecodable reply.", vin, innerException: ex);
        }
    }

    private static string Trim(string body) => body.Length <= 300 ? body : body[..300] + "...";
}
