namespace CoilPro;

/// <summary>
/// Client for the Tesla Fleet API REST endpoints that take a user (or business) token: vehicles, users,
/// charging, vehicle management and the unsigned REST commands. All calls attach a bearer token from the
/// configured <see cref="ITeslaTokenProvider"/> and throw <see cref="TeslaApiException"/> on API errors.
/// </summary>
/// <remarks>
/// Signed vehicle commands live in <c>CoilPro.Commands</c>; energy products (Powerwall, solar) in
/// <see cref="ICoilProEnergyClient"/>; partner-token endpoints in <see cref="ITeslaPartnerClient"/>.
/// Methods marked "billed" count against the Fleet API pay-per-use categories (device data, commands,
/// wakes); all others are free. Vehicle paths take the 17-character VIN.
/// </remarks>
public interface ICoilProClient
{
    /// <summary>The rate-limit snapshot observed on the most recent response, if any.</summary>
    RateLimitInfo? LastRateLimit { get; }

    // ---- Vehicles -------------------------------------------------------------------------------

    /// <summary>Lists the vehicles on the authenticated account (<c>GET /api/1/vehicles</c>, first page). Free.</summary>
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default);

    /// <summary>One page of <c>GET /api/1/vehicles</c> with paging metadata. Free.</summary>
    Task<ResultPage<Vehicle>> GetVehiclesPageAsync(int? page = null, int? perPage = null, CancellationToken ct = default);

    /// <summary>One vehicle's summary (<c>GET /api/1/vehicles/{vin}</c>); does not wake it. Free.</summary>
    Task<Vehicle> GetVehicleAsync(string vin, CancellationToken ct = default);

    /// <summary>
    /// Live vehicle data (<c>GET /api/1/vehicles/{vin}/vehicle_data</c>). The vehicle must be online. Billed (device data).
    /// Pass <paramref name="endpoints"/> to request only the segments you need.
    /// </summary>
    /// <param name="vehicleTag">The VIN (the numeric id also works on this endpoint).</param>
    /// <param name="endpoints">Data segments to request; <see cref="VehicleDataEndpoint.None"/> for the default set.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default);

    /// <summary>
    /// Last data the vehicle reported (<c>GET /api/1/vehicles/{vin}/vehicle_data_cached</c>). Works while the
    /// vehicle sleeps, never wakes it, no location or drive state. Check each block's timestamp. Free.
    /// </summary>
    Task<VehicleData> GetVehicleDataCachedAsync(string vin, CancellationToken ct = default);

    /// <summary>Wakes a sleeping vehicle (<c>POST /api/1/vehicles/{vin}/wake_up</c>). Billed (wakes).</summary>
    Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default);

    /// <summary>Superchargers and destination chargers near the vehicle (<c>GET …/nearby_charging_sites</c>). Billed (device data).</summary>
    Task<NearbyChargingSites> GetNearbyChargingSitesAsync(string vin, NearbyChargingSitesQuery? query = null, CancellationToken ct = default);

    /// <summary>Recent vehicle alerts (<c>GET …/recent_alerts</c>). Free.</summary>
    Task<IReadOnlyList<VehicleAlert>> GetRecentAlertsAsync(string vin, CancellationToken ct = default);

    /// <summary>Release notes of the installed (or, with <paramref name="staged"/>, the upcoming) firmware (<c>GET …/release_notes</c>). Free.</summary>
    Task<IReadOnlyList<ReleaseNote>> GetReleaseNotesAsync(string vin, bool staged = false, string? language = null, CancellationToken ct = default);

    /// <summary>Current service status (<c>GET …/service_data</c>). Free.</summary>
    Task<ServiceData> GetServiceDataAsync(string vin, CancellationToken ct = default);

    /// <summary>One page of past service visits (<c>GET …/service_history</c>, page size 1–50). Free.</summary>
    Task<ResultPage<ServiceVisit>> GetServiceHistoryAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default);

    /// <summary>One service visit with work items and cost (<c>GET …/service_history/{id}</c>). Free.</summary>
    Task<ServiceVisitDetail> GetServiceVisitAsync(string vin, long serviceVisitId, CancellationToken ct = default);

    /// <summary>Drivers with access to the vehicle (<c>GET …/drivers</c>, owner only). Free.</summary>
    Task<IReadOnlyList<VehicleDriver>> GetDriversAsync(string vin, CancellationToken ct = default);

    /// <summary>Removes a driver's access, or the caller's own share when <paramref name="shareUserId"/> is null (<c>DELETE …/drivers</c>). Free.</summary>
    Task RemoveDriverAsync(string vin, long? shareUserId = null, CancellationToken ct = default);

    /// <summary>Whether mobile access is enabled on the vehicle (<c>GET …/mobile_enabled</c>). Free.</summary>
    Task<MobileAccessStatus> GetMobileEnabledAsync(string vin, CancellationToken ct = default);

    /// <summary>One page of share invitations (<c>GET …/invitations</c>, page size up to 25). Free.</summary>
    Task<ResultPage<ShareInvite>> GetShareInvitesAsync(string vin, int? page = null, int? perPage = null, CancellationToken ct = default);

    /// <summary>Creates a single-use, 24-hour driver invitation (<c>POST …/invitations</c>). Free.</summary>
    Task<ShareInvite> CreateShareInviteAsync(string vin, CancellationToken ct = default);

    /// <summary>Redeems an invitation code for the authenticated user (<c>POST /api/1/invitations/redeem</c>). Free.</summary>
    Task<RedeemedShareInvite> RedeemShareInviteAsync(string code, CancellationToken ct = default);

    /// <summary>Revokes a share invitation (<c>POST …/invitations/{id}/revoke</c>). Free.</summary>
    Task RevokeShareInviteAsync(string vin, string inviteId, CancellationToken ct = default);

    /// <summary>
    /// Key pairing and firmware info (<c>POST /api/1/vehicles/fleet_status</c>). Tesla recommends one VIN per call. Free.
    /// </summary>
    Task<FleetStatus> GetFleetStatusAsync(IEnumerable<string> vins, CancellationToken ct = default);

    // ---- Fleet Telemetry configuration (creating a config needs signing: see CoilPro.Commands) ---

    /// <summary>The vehicle's Fleet Telemetry configuration and sync state (<c>GET …/fleet_telemetry_config</c>). Free.</summary>
    Task<FleetTelemetryConfigStatus> GetFleetTelemetryConfigAsync(string vin, CancellationToken ct = default);

    /// <summary>Removes the vehicle's Fleet Telemetry configuration (<c>DELETE …/fleet_telemetry_config</c>). Returns the number of updated vehicles. Free.</summary>
    Task<int> DeleteFleetTelemetryConfigAsync(string vin, CancellationToken ct = default);

    /// <summary>Recent Fleet Telemetry errors of the vehicle (<c>GET …/fleet_telemetry_errors</c>). Free.</summary>
    Task<IReadOnlyList<FleetTelemetryError>> GetFleetTelemetryErrorsAsync(string vin, CancellationToken ct = default);

    // ---- Unsigned REST commands (POST /api/1/vehicles/{vin}/command/...) -------------------------

    /// <summary>
    /// Sends a destination (address or place text) to the vehicle's navigation (<c>command/navigation_request</c>).
    /// Unsigned by design (Tesla processes shares server-side). Billed (commands).
    /// </summary>
    Task<RestCommandResult> ShareNavigationDestinationAsync(string vin, string destination, string? locale = null, CancellationToken ct = default);

    /// <summary>Navigates to coordinates (<c>command/navigation_gps_request</c>). Unverified on protocol-only vehicles. Billed (commands).</summary>
    Task<RestCommandResult> NavigateToCoordinatesAsync(string vin, double latitude, double longitude, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default);

    /// <summary>Navigates to a Supercharger by id (<c>command/navigation_sc_request</c>). Unverified on protocol-only vehicles. Billed (commands).</summary>
    Task<RestCommandResult> NavigateToSuperchargerAsync(string vin, long superchargerId, NavigationTripOrder order = NavigationTripOrder.Replace, CancellationToken ct = default);

    /// <summary>Sets a route of Google place ids (<c>command/navigation_waypoints_request</c>, <c>refId:&lt;placeId&gt;,…</c>). Unverified on protocol-only vehicles. Billed (commands).</summary>
    Task<RestCommandResult> SetNavigationWaypointsAsync(string vin, IEnumerable<string> placeIds, CancellationToken ct = default);

    // ---- Users ----------------------------------------------------------------------------------

    /// <summary>The authenticated user's profile (<c>GET /api/1/users/me</c>, scope <c>user_data</c>). Free.</summary>
    Task<UserProfile> GetMeAsync(CancellationToken ct = default);

    /// <summary>The user's vehicle orders (<c>GET /api/1/users/orders</c>). Free.</summary>
    Task<IReadOnlyList<VehicleOrder>> GetOrdersAsync(CancellationToken ct = default);

    /// <summary>The user's Fleet API region and base URL (<c>GET /api/1/users/region</c>). Free.</summary>
    Task<UserRegion> GetRegionAsync(CancellationToken ct = default);

    // ---- Charging -------------------------------------------------------------------------------

    /// <summary>Supercharger charging history (<c>GET /api/1/dx/charging/history</c>), optionally filtered and paged. Free.</summary>
    Task<ChargingHistoryPage> GetChargingHistoryAsync(ChargingHistoryQuery? query = null, CancellationToken ct = default);

    /// <summary>Downloads a charging invoice PDF (<c>GET /api/1/dx/charging/invoice/{contentId}</c>; id from <see cref="ChargingInvoice.ContentId"/>). Free.</summary>
    Task<byte[]> GetChargingInvoiceAsync(string contentId, CancellationToken ct = default);

    /// <summary>Charging sessions of a business fleet (<c>GET /api/1/dx/charging/sessions</c>; business accounts only). Free.</summary>
    Task<ChargingSessionsPage> GetChargingSessionsAsync(ChargingSessionsQuery? query = null, CancellationToken ct = default);

    // ---- Vehicle management ---------------------------------------------------------------------

    /// <summary>Option codes of the vehicle (<c>GET /api/1/dx/vehicles/options</c>). Free.</summary>
    Task<IReadOnlyList<VehicleOption>> GetVehicleOptionsAsync(string vin, CancellationToken ct = default);

    /// <summary>Active, upcoming and expired warranties (<c>GET /api/1/dx/warranty/details</c>). Free.</summary>
    Task<WarrantyDetails> GetWarrantyDetailsAsync(string vin, CancellationToken ct = default);

    /// <summary>Subscriptions the vehicle is eligible for (<c>GET /api/1/dx/vehicles/subscriptions/eligibility</c>). Free.</summary>
    Task<SubscriptionEligibility> GetSubscriptionEligibilityAsync(string vin, CancellationToken ct = default);

    /// <summary>Upgrades the vehicle is eligible for (<c>GET /api/1/dx/vehicles/upgrades/eligibility</c>). Free.</summary>
    Task<UpgradeEligibility> GetUpgradeEligibilityAsync(string vin, CancellationToken ct = default);

    /// <summary>Owner, drivers and payers of a business vehicle (<c>GET /api/1/dx/enterprise/v1/{vin}/roles</c>; scope <c>enterprise_management</c>). Free.</summary>
    Task<EnterpriseRoles> GetEnterpriseRolesAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets who pays for charging or subscriptions of a business vehicle (<c>POST …/payer</c>). Free.</summary>
    Task SetEnterprisePayerAsync(string vin, EnterprisePayerRequest request, CancellationToken ct = default);

    /// <summary>True when the Fleet API health endpoint (<c>GET /status</c>) answers <c>ok</c>. Free, unauthenticated.</summary>
    Task<bool> GetApiStatusAsync(CancellationToken ct = default);
}
