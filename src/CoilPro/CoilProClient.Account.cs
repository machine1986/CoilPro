namespace CoilPro;

public sealed partial class CoilProClient
{
    /// <inheritdoc />
    public Task<RestCommandResult> ShareNavigationDestinationAsync(string vin, string destination, string? locale = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): ShareNavigationDestinationAsync");

    /// <inheritdoc />
    public Task<RestCommandResult> NavigateToCoordinatesAsync(string vin, double latitude, double longitude, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): NavigateToCoordinatesAsync");

    /// <inheritdoc />
    public Task<RestCommandResult> NavigateToSuperchargerAsync(string vin, long superchargerId, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): NavigateToSuperchargerAsync");

    /// <inheritdoc />
    public Task<RestCommandResult> SetNavigationWaypointsAsync(string vin, IEnumerable<string> placeIds, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): SetNavigationWaypointsAsync");

    /// <inheritdoc />
    public Task<UserProfile> GetMeAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetMeAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<VehicleOrder>> GetOrdersAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetOrdersAsync");

    /// <inheritdoc />
    public Task<UserRegion> GetRegionAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetRegionAsync");

    /// <inheritdoc />
    public Task<byte[]> GetChargingInvoiceAsync(string contentId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetChargingInvoiceAsync");

    /// <inheritdoc />
    public Task<ChargingSessionsPage> GetChargingSessionsAsync(ChargingSessionsQuery? query = null, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetChargingSessionsAsync");

    /// <inheritdoc />
    public Task<IReadOnlyList<VehicleOption>> GetVehicleOptionsAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetVehicleOptionsAsync");

    /// <inheritdoc />
    public Task<WarrantyDetails> GetWarrantyDetailsAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetWarrantyDetailsAsync");

    /// <inheritdoc />
    public Task<SubscriptionEligibility> GetSubscriptionEligibilityAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetSubscriptionEligibilityAsync");

    /// <inheritdoc />
    public Task<UpgradeEligibility> GetUpgradeEligibilityAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetUpgradeEligibilityAsync");

    /// <inheritdoc />
    public Task<EnterpriseRoles> GetEnterpriseRolesAsync(string vin, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetEnterpriseRolesAsync");

    /// <inheritdoc />
    public Task SetEnterprisePayerAsync(string vin, EnterprisePayerRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): SetEnterprisePayerAsync");

    /// <inheritdoc />
    public Task<bool> GetApiStatusAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 account): GetApiStatusAsync");
}
