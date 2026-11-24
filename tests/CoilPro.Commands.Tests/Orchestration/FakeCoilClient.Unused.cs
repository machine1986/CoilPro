namespace CoilPro.Commands.Tests.Orchestration;

// Members the orchestration tests never call.
internal sealed partial class FakeCoilClient
{
    public Task<ResultPage<Vehicle>> GetVehiclesPageAsync(int? page = null, int? perPage = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<Vehicle> GetVehicleAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<VehicleData> GetVehicleDataCachedAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<NearbyChargingSites> GetNearbyChargingSitesAsync(string vin, NearbyChargingSitesQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<VehicleAlert>> GetRecentAlertsAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync(string vin, bool staged = false, string? language = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ServiceData> GetServiceDataAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ResultPage<ServiceVisit>> GetServiceHistoryAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ServiceVisitDetail> GetServiceVisitAsync(string vin, long serviceVisitId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<VehicleDriver>> GetDriversAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task RemoveDriverAsync(string vin, long? shareUserId = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<MobileAccessStatus> GetMobileEnabledAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ResultPage<ShareInvite>> GetShareInvitesAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ShareInvite> CreateShareInviteAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RedeemedShareInvite> RedeemShareInviteAsync(string code, CancellationToken ct = default) => throw new NotSupportedException();

    public Task RevokeShareInviteAsync(string vin, string inviteId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<FleetStatus> GetFleetStatusAsync(IEnumerable<string> vins, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<FleetTelemetryConfigStatus> GetFleetTelemetryConfigAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<int> DeleteFleetTelemetryConfigAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RestCommandResult> ShareNavigationDestinationAsync(string vin, string destination, string? locale = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RestCommandResult> NavigateToCoordinatesAsync(string vin, double latitude, double longitude, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RestCommandResult> NavigateToSuperchargerAsync(string vin, long superchargerId, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<RestCommandResult> SetNavigationWaypointsAsync(string vin, IEnumerable<string> placeIds, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<UserProfile> GetMeAsync(CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<VehicleOrder>> GetOrdersAsync(CancellationToken ct = default) => throw new NotSupportedException();

    public Task<UserRegion> GetRegionAsync(CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ChargingHistoryPage> GetChargingHistoryAsync(ChargingHistoryQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<byte[]> GetChargingInvoiceAsync(string contentId, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<ChargingSessionsPage> GetChargingSessionsAsync(ChargingSessionsQuery? query = null, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<IReadOnlyList<VehicleOption>> GetVehicleOptionsAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<WarrantyDetails> GetWarrantyDetailsAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<SubscriptionEligibility> GetSubscriptionEligibilityAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<UpgradeEligibility> GetUpgradeEligibilityAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<EnterpriseRoles> GetEnterpriseRolesAsync(string vin, CancellationToken ct = default) => throw new NotSupportedException();

    public Task SetEnterprisePayerAsync(string vin, EnterprisePayerRequest request, CancellationToken ct = default) => throw new NotSupportedException();

    public Task<bool> GetApiStatusAsync(CancellationToken ct = default) => throw new NotSupportedException();
}
