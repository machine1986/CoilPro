using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using CoilPro.Internal;

namespace CoilPro;

public sealed partial class CoilProClient
{
    private static string VehiclePath(string vin, string suffix = "") =>
        $"api/1/vehicles/{Uri.EscapeDataString(vin)}{suffix}";

    private static string WithQuery(string path, params (string Name, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}")
            .ToList();
        return parts.Count == 0 ? path : $"{path}?{string.Join("&", parts)}";
    }

    private static string? Num(int? value) => value?.ToString(CultureInfo.InvariantCulture);

    private static string? Bool(bool? value) => value is null ? null : value.Value ? "true" : "false";

    private async Task<T> GetRequiredAsync<T>(string path, string what, CancellationToken ct) where T : class
    {
        var result = await _executor.SendAsync<T>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, what);
    }

    private async Task<ResultPage<T>> GetPageAsync<T>(string path, CancellationToken ct)
    {
        var result = await _executor.SendPagedAsync<List<T>>(HttpMethod.Get, path, ct).ConfigureAwait(false);
        return new ResultPage<T> { Items = result.Value ?? [], Pagination = result.Pagination, Count = result.Count };
    }

    /// <inheritdoc />
    public Task<ResultPage<Vehicle>> GetVehiclesPageAsync(int? page = null, int? perPage = null, CancellationToken ct = default) =>
        GetPageAsync<Vehicle>(WithQuery("api/1/vehicles", ("page", Num(page)), ("per_page", Num(perPage))), ct);

    /// <inheritdoc />
    public Task<Vehicle> GetVehicleAsync(string vin, CancellationToken ct = default) =>
        GetRequiredAsync<Vehicle>(VehiclePath(vin), "vehicle", ct);

    /// <inheritdoc />
    public Task<VehicleData> GetVehicleDataCachedAsync(string vin, CancellationToken ct = default) =>
        GetRequiredAsync<VehicleData>(VehiclePath(vin, "/vehicle_data_cached"), "vehicle_data_cached", ct);

    /// <inheritdoc />
    public Task<NearbyChargingSites> GetNearbyChargingSitesAsync(string vin, NearbyChargingSitesQuery? query = null, CancellationToken ct = default) =>
        GetRequiredAsync<NearbyChargingSites>(
            WithQuery(VehiclePath(vin, "/nearby_charging_sites"),
                ("count", Num(query?.Count)), ("radius", Num(query?.RadiusMiles)), ("detail", Bool(query?.Detail))),
            "nearby_charging_sites",
            ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleAlert>> GetRecentAlertsAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<RecentAlertsPayload>(HttpMethod.Get, VehiclePath(vin, "/recent_alerts"), null, ct).ConfigureAwait(false);
        return result.Value?.RecentAlerts ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync(string vin, bool staged = false, string? language = null, CancellationToken ct = default)
    {
        var path = WithQuery(VehiclePath(vin, "/release_notes"), ("staged", staged ? "true" : null), ("language", language));
        var result = await _executor.SendAsync<ReleaseNotesPayload>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);

        // Tesla's docs example nests the payload twice (response.response.release_notes); accept both.
        return result.Value?.ReleaseNotes ?? result.Value?.Nested?.ReleaseNotes ?? [];
    }

    /// <inheritdoc />
    public Task<ServiceData> GetServiceDataAsync(string vin, CancellationToken ct = default) =>
        GetRequiredAsync<ServiceData>(VehiclePath(vin, "/service_data"), "service_data", ct);

    /// <inheritdoc />
    public Task<ResultPage<ServiceVisit>> GetServiceHistoryAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) =>
        GetPageAsync<ServiceVisit>(WithQuery(VehiclePath(vin, "/service_history"), ("page", Num(page)), ("per_page", Num(perPage))), ct);

    /// <inheritdoc />
    public Task<ServiceVisitDetail> GetServiceVisitAsync(string vin, long serviceVisitId, CancellationToken ct = default) =>
        GetRequiredAsync<ServiceVisitDetail>(
            VehiclePath(vin, $"/service_history/{serviceVisitId.ToString(CultureInfo.InvariantCulture)}"),
            "service_history visit",
            ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<VehicleDriver>> GetDriversAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<List<VehicleDriver>>(HttpMethod.Get, VehiclePath(vin, "/drivers"), null, ct).ConfigureAwait(false);
        return result.Value ?? [];
    }

    /// <inheritdoc />
    public async Task RemoveDriverAsync(string vin, long? shareUserId = null, CancellationToken ct = default)
    {
        var path = WithQuery(VehiclePath(vin, "/drivers"), ("share_user_id", shareUserId?.ToString(CultureInfo.InvariantCulture)));

        // Response is the bare string "ok"; not inspected.
        await _executor.SendAsync<JsonElement?>(HttpMethod.Delete, path, null, ct, retryable: false).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<MobileAccessStatus> GetMobileEnabledAsync(string vin, CancellationToken ct = default) =>
        GetRequiredAsync<MobileAccessStatus>(VehiclePath(vin, "/mobile_enabled"), "mobile_enabled", ct);

    /// <inheritdoc />
    public Task<ResultPage<ShareInvite>> GetShareInvitesAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) =>
        GetPageAsync<ShareInvite>(WithQuery(VehiclePath(vin, "/invitations"), ("page", Num(page)), ("per_page", Num(perPage))), ct);

    /// <inheritdoc />
    public async Task<ShareInvite> CreateShareInviteAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<ShareInvite>(HttpMethod.Post, VehiclePath(vin, "/invitations"), null, ct, retryable: false).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "invitation");
    }

    /// <inheritdoc />
    public async Task<RedeemedShareInvite> RedeemShareInviteAsync(string code, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<RedeemedShareInvite>(
            HttpMethod.Post, "api/1/invitations/redeem", FleetApiExecutor.JsonBody(new RedeemRequest(code)), ct, retryable: false).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "invitation redeem");
    }

    /// <inheritdoc />
    public async Task RevokeShareInviteAsync(string vin, string inviteId, CancellationToken ct = default)
    {
        var path = VehiclePath(vin, $"/invitations/{Uri.EscapeDataString(inviteId)}/revoke");

        // Response is the bare boolean true; not inspected.
        await _executor.SendAsync<JsonElement?>(HttpMethod.Post, path, null, ct, retryable: false).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<FleetStatus> GetFleetStatusAsync(IEnumerable<string> vins, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vins);

        // A read despite being a POST, so transient failures may be retried.
        var result = await _executor.SendAsync<FleetStatus>(
            HttpMethod.Post, "api/1/vehicles/fleet_status", FleetApiExecutor.JsonBody(new FleetStatusRequest([.. vins])), ct).ConfigureAwait(false);
        return result.Value ?? throw EmptyResponse(result.StatusCode, "fleet_status");
    }

    /// <inheritdoc />
    public Task<FleetTelemetryConfigStatus> GetFleetTelemetryConfigAsync(string vin, CancellationToken ct = default) =>
        GetRequiredAsync<FleetTelemetryConfigStatus>(VehiclePath(vin, "/fleet_telemetry_config"), "fleet_telemetry_config", ct);

    /// <inheritdoc />
    public async Task<int> DeleteFleetTelemetryConfigAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<UpdatedVehiclesPayload>(
            HttpMethod.Delete, VehiclePath(vin, "/fleet_telemetry_config"), null, ct, retryable: false).ConfigureAwait(false);
        return result.Value?.UpdatedVehicles ?? 0;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(string vin, CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<TelemetryErrorsPayload>(HttpMethod.Get, VehiclePath(vin, "/fleet_telemetry_errors"), null, ct).ConfigureAwait(false);
        return result.Value?.Errors ?? [];
    }

    private sealed record RedeemRequest([property: JsonPropertyName("code")] string Code);

    private sealed record FleetStatusRequest([property: JsonPropertyName("vins")] string[] Vins);

    private sealed record RecentAlertsPayload
    {
        [JsonPropertyName("recent_alerts")] public List<VehicleAlert>? RecentAlerts { get; init; }
    }

    private sealed record ReleaseNotesPayload
    {
        [JsonPropertyName("release_notes")] public List<ReleaseNote>? ReleaseNotes { get; init; }

        [JsonPropertyName("response")] public ReleaseNotesPayload? Nested { get; init; }
    }

    private sealed record UpdatedVehiclesPayload
    {
        [JsonPropertyName("updated_vehicles")] public int? UpdatedVehicles { get; init; }
    }

    private sealed record TelemetryErrorsPayload
    {
        [JsonPropertyName("fleet_telemetry_errors")] public List<FleetTelemetryError>? Errors { get; init; }
    }
}
