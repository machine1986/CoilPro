using System.Text.Json.Serialization;
using CoilPro.Internal;

namespace CoilPro;

public sealed partial class TeslaPartnerClient
{
    /// <inheritdoc />
    public async Task<VehicleSpecs> GetVehicleSpecsAsync(string vin, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(vin);

        var result = await _executor.SendAsync<VehicleSpecs>(
            HttpMethod.Get, $"api/1/vehicles/{Uri.EscapeDataString(vin)}/specs", null, ct).ConfigureAwait(false);

        return result.Value ?? throw EmptyResponse(result.StatusCode, "vehicle specs");
    }

    /// <inheritdoc />
    public async Task<VehiclePricing> GetVehiclePricingAsync(string market, string model, string language, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(market);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        // POST, but a pure read: retrying is safe, so the default (retryable) applies.
        var result = await _executor.SendAsync<VehiclePricing>(
            HttpMethod.Post,
            "api/1/dx/vehicles/pricing",
            FleetApiExecutor.JsonBody(new Dictionary<string, string>
            {
                ["market"] = market,
                ["model"] = model,
                ["language"] = language,
            }),
            ct).ConfigureAwait(false);

        return result.Value ?? throw EmptyResponse(result.StatusCode, "vehicle pricing");
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<TelemetryErrorsPayload>(
            HttpMethod.Get, "api/1/partner_accounts/fleet_telemetry_errors", null, ct).ConfigureAwait(false);

        return result.Value?.Errors ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetFleetTelemetryErrorVinsAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<TelemetryErrorVinsPayload>(
            HttpMethod.Get, "api/1/partner_accounts/fleet_telemetry_error_vins", null, ct).ConfigureAwait(false);

        return result.Value?.Vins ?? [];
    }

    private TeslaApiException EmptyResponse(System.Net.HttpStatusCode status, string what) =>
        new(status, $"Fleet API returned a success status with an empty {what} response.", responseBody: null, rateLimit: LastRateLimit);

    private sealed record TelemetryErrorsPayload
    {
        [JsonPropertyName("fleet_telemetry_errors")]
        public List<FleetTelemetryError>? Errors { get; init; }
    }

    private sealed record TelemetryErrorVinsPayload
    {
        [JsonPropertyName("fleet_telemetry_error_vins")]
        public List<string>? Vins { get; init; }
    }
}
