namespace CoilPro;

public sealed partial class CoilProClient
{
    /// <inheritdoc />
    public Task<ResultPage<Vehicle>> GetVehiclesPageAsync(int? page = null, int? perPage = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetVehiclesPageAsync");

    /// <inheritdoc />
    public Task<Vehicle> GetVehicleAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetVehicleAsync");

    /// <inheritdoc />
    public Task<VehicleData> GetVehicleDataCachedAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetVehicleDataCachedAsync");

    /// <inheritdoc />
    public Task<NearbyChargingSites> GetNearbyChargingSitesAsync(string vin, NearbyChargingSitesQuery? query = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetNearbyChargingSitesAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<VehicleAlert>> GetRecentAlertsAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetRecentAlertsAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync(string vin, bool staged = false, string? language = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetReleaseNotesAsync");

    /// <inheritdoc />
    public Task<ServiceData> GetServiceDataAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetServiceDataAsync");

    /// <inheritdoc />
    public Task<ResultPage<ServiceVisit>> GetServiceHistoryAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetServiceHistoryAsync");

    /// <inheritdoc />
    public Task<ServiceVisitDetail> GetServiceVisitAsync(string vin, long serviceVisitId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetServiceVisitAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<VehicleDriver>> GetDriversAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetDriversAsync");

    /// <inheritdoc />
    public Task RemoveDriverAsync(string vin, long? shareUserId = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): RemoveDriverAsync");

    /// <inheritdoc />
    public Task<MobileAccessStatus> GetMobileEnabledAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetMobileEnabledAsync");

    /// <inheritdoc />
    public Task<ResultPage<ShareInvite>> GetShareInvitesAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetShareInvitesAsync");

    /// <inheritdoc />
    public Task<ShareInvite> CreateShareInviteAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): CreateShareInviteAsync");

    /// <inheritdoc />
    public Task<RedeemedShareInvite> RedeemShareInviteAsync(string code, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): RedeemShareInviteAsync");

    /// <inheritdoc />
    public Task RevokeShareInviteAsync(string vin, string inviteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): RevokeShareInviteAsync");

    /// <inheritdoc />
    public Task<FleetStatus> GetFleetStatusAsync(IEnumerable<string> vins, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetFleetStatusAsync");

    /// <inheritdoc />
    public Task<FleetTelemetryConfigStatus> GetFleetTelemetryConfigAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetFleetTelemetryConfigAsync");

    /// <inheritdoc />
    public Task<int> DeleteFleetTelemetryConfigAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): DeleteFleetTelemetryConfigAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 vehicles): GetFleetTelemetryErrorsAsync");
}
