using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>A charging site near a vehicle (<c>nearby_charging_sites</c>).</summary>
public sealed record ChargingSite
{
    /// <summary>Coordinates of the site (<c>location</c>).</summary>
    [JsonPropertyName("location")] public ChargingSiteLocation? Location { get; init; }

    /// <summary>Site name (<c>name</c>).</summary>
    [JsonPropertyName("name")] public string? Name { get; init; }

    /// <summary>Site kind such as <c>supercharger</c> or <c>destination</c> (<c>type</c>).</summary>
    [JsonPropertyName("type")] public string? Type { get; init; }

    /// <summary>Distance from the vehicle in miles (<c>distance_miles</c>).</summary>
    [JsonPropertyName("distance_miles")] public double? DistanceMiles { get; init; }

    /// <summary>Comma-separated amenities, e.g. <c>restrooms,wifi</c> (<c>amenities</c>).</summary>
    [JsonPropertyName("amenities")] public string? Amenities { get; init; }

    /// <summary>Free stalls (Superchargers only) (<c>available_stalls</c>).</summary>
    [JsonPropertyName("available_stalls")] public int? AvailableStalls { get; init; }

    /// <summary>Total stalls (Superchargers only) (<c>total_stalls</c>).</summary>
    [JsonPropertyName("total_stalls")] public int? TotalStalls { get; init; }

    /// <summary>Whether the site is closed (Superchargers only) (<c>site_closed</c>).</summary>
    [JsonPropertyName("site_closed")] public bool? SiteClosed { get; init; }

    /// <summary>Billing information, possibly empty (Superchargers only) (<c>billing_info</c>).</summary>
    [JsonPropertyName("billing_info")] public string? BillingInfo { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>WGS84 coordinates of a charging site.</summary>
public sealed record ChargingSiteLocation
{
    /// <summary>Latitude in degrees (<c>lat</c>).</summary>
    [JsonPropertyName("lat")] public double? Latitude { get; init; }

    /// <summary>Longitude in degrees (<c>long</c>).</summary>
    [JsonPropertyName("long")] public double? Longitude { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Superchargers and destination chargers near a vehicle.</summary>
public sealed record NearbyChargingSites
{
    /// <summary>Time of the congestion data, Unix epoch seconds (<c>congestion_sync_time_utc_secs</c>).</summary>
    [JsonPropertyName("congestion_sync_time_utc_secs")] public long? CongestionSyncTimeUtcSecs { get; init; }

    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }

    /// <summary>Destination chargers (<c>destination_charging</c>).</summary>
    [JsonPropertyName("destination_charging")] public IReadOnlyList<ChargingSite>? DestinationCharging { get; init; }

    /// <summary>Superchargers (<c>superchargers</c>).</summary>
    [JsonPropertyName("superchargers")] public IReadOnlyList<ChargingSite>? Superchargers { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A recent vehicle alert.</summary>
public sealed record VehicleAlert
{
    /// <summary>Alert name (<c>name</c>).</summary>
    [JsonPropertyName("name")] public string? Name { get; init; }

    /// <summary>
    /// When the alert was raised, as sent (<c>time</c>). Live responses use RFC 1123
    /// (<c>Tue, 24 Nov 2026 09:27:27 +0000</c>), not ISO 8601 — see <see cref="TimeUtc"/>.
    /// </summary>
    [JsonPropertyName("time")] public string? Time { get; init; }

    /// <summary><see cref="Time"/> parsed (RFC 1123 or ISO 8601); null when absent or unparseable.</summary>
    [JsonIgnore]
    public DateTimeOffset? TimeUtc =>
        DateTimeOffset.TryParse(Time, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var value)
            ? value
            : null;

    /// <summary>Intended audiences such as <c>customer</c>, <c>service</c> (<c>audiences</c>; the docs say <c>audience</c>).</summary>
    [JsonPropertyName("audiences")] public IReadOnlyList<string>? Audiences { get; init; }

    /// <summary>Additional description text (<c>user_text</c>).</summary>
    [JsonPropertyName("user_text")] public string? UserText { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One firmware release note.</summary>
public sealed record ReleaseNote
{
    /// <summary>Headline (<c>title</c>).</summary>
    [JsonPropertyName("title")] public string? Title { get; init; }

    /// <summary>Secondary headline (<c>subtitle</c>).</summary>
    [JsonPropertyName("subtitle")] public string? Subtitle { get; init; }

    /// <summary>Description text (<c>description</c>).</summary>
    [JsonPropertyName("description")] public string? Description { get; init; }

    /// <summary>Firmware version the note belongs to (<c>customer_version</c>).</summary>
    [JsonPropertyName("customer_version")] public string? CustomerVersion { get; init; }

    /// <summary>Icon name (<c>icon</c>).</summary>
    [JsonPropertyName("icon")] public string? Icon { get; init; }

    /// <summary>Image URL for dark backgrounds (<c>image_url</c>).</summary>
    [JsonPropertyName("image_url")] public string? ImageUrl { get; init; }

    /// <summary>Image URL for light backgrounds (<c>light_image_url</c>).</summary>
    [JsonPropertyName("light_image_url")] public string? LightImageUrl { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Current service status of a vehicle.</summary>
public sealed record ServiceData
{
    /// <summary>Service status such as <c>car_in_repair</c> (<c>service_status</c>).</summary>
    [JsonPropertyName("service_status")] public string? ServiceStatus { get; init; }

    /// <summary>Estimated completion time (<c>service_etc</c>, ISO 8601).</summary>
    [JsonPropertyName("service_etc")] public DateTimeOffset? ServiceEtc { get; init; }

    /// <summary>Service visit number such as <c>SV12345678</c> (<c>service_visit_number</c>).</summary>
    [JsonPropertyName("service_visit_number")] public string? ServiceVisitNumber { get; init; }

    /// <summary>Numeric status id (<c>status_id</c>).</summary>
    [JsonPropertyName("status_id")] public int? StatusId { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Location of a service visit.</summary>
public sealed record ServiceLocation
{
    /// <summary>Name of the service location (<c>name</c>).</summary>
    [JsonPropertyName("name")] public string? Name { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A past service visit.</summary>
public sealed record ServiceVisit
{
    /// <summary>Numeric visit id (<c>service_visit_id</c>).</summary>
    [JsonPropertyName("service_visit_id")] public long? ServiceVisitId { get; init; }

    /// <summary>Visit number such as <c>SV12345679</c> (<c>service_visit_number</c>).</summary>
    [JsonPropertyName("service_visit_number")] public string? ServiceVisitNumber { get; init; }

    /// <summary>Outcome: <c>delivered</c> or <c>diverted</c> (<c>status</c>).</summary>
    [JsonPropertyName("status")] public string? Status { get; init; }

    /// <summary>Kind: <c>service_center</c>, <c>mobile</c>, <c>body_shop</c> or <c>collision_partner</c> (<c>visit_type</c>).</summary>
    [JsonPropertyName("visit_type")] public string? VisitType { get; init; }

    /// <summary>Check-in time, UTC (<c>checked_in_at</c>).</summary>
    [JsonPropertyName("checked_in_at")] public DateTimeOffset? CheckedInAt { get; init; }

    /// <summary>Completion time, UTC (<c>completed_at</c>).</summary>
    [JsonPropertyName("completed_at")] public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Where the visit took place (<c>location</c>).</summary>
    [JsonPropertyName("location")] public ServiceLocation? Location { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One work item of a service visit.</summary>
public sealed record ServiceWorkItem
{
    /// <summary>Correction code (<c>correction_code</c>).</summary>
    [JsonPropertyName("correction_code")] public string? CorrectionCode { get; init; }

    /// <summary>Description of the work (<c>description</c>).</summary>
    [JsonPropertyName("description")] public string? Description { get; init; }

    /// <summary>Who paid: <c>customer_pay</c>, <c>warranty</c>, <c>goodwill</c>, <c>service_plan</c>, <c>adjustment_program</c> or <c>other</c> (<c>pay_type</c>).</summary>
    [JsonPropertyName("pay_type")] public string? PayType { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Cost of a service visit; amounts are in <see cref="Currency"/>.</summary>
public sealed record ServiceCost
{
    /// <summary>ISO currency code (<c>currency</c>).</summary>
    [JsonPropertyName("currency")] public string? Currency { get; init; }

    /// <summary>Subtotal (<c>subtotal</c>).</summary>
    [JsonPropertyName("subtotal")] public double? Subtotal { get; init; }

    /// <summary>Tax (<c>tax</c>).</summary>
    [JsonPropertyName("tax")] public double? Tax { get; init; }

    /// <summary>Total (<c>total</c>).</summary>
    [JsonPropertyName("total")] public double? Total { get; init; }

    /// <summary>Amount paid (<c>paid</c>).</summary>
    [JsonPropertyName("paid")] public double? Paid { get; init; }

    /// <summary>Amount still due (<c>due</c>).</summary>
    [JsonPropertyName("due")] public double? Due { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A service visit with work items and cost.</summary>
public sealed record ServiceVisitDetail
{
    /// <summary>Numeric visit id (<c>service_visit_id</c>).</summary>
    [JsonPropertyName("service_visit_id")] public long? ServiceVisitId { get; init; }

    /// <summary>Visit number (<c>service_visit_number</c>).</summary>
    [JsonPropertyName("service_visit_number")] public string? ServiceVisitNumber { get; init; }

    /// <summary>Outcome: <c>delivered</c> or <c>diverted</c> (<c>status</c>).</summary>
    [JsonPropertyName("status")] public string? Status { get; init; }

    /// <summary>Kind of visit (<c>visit_type</c>).</summary>
    [JsonPropertyName("visit_type")] public string? VisitType { get; init; }

    /// <summary>Check-in time, UTC (<c>checked_in_at</c>).</summary>
    [JsonPropertyName("checked_in_at")] public DateTimeOffset? CheckedInAt { get; init; }

    /// <summary>Completion time, UTC (<c>completed_at</c>).</summary>
    [JsonPropertyName("completed_at")] public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>Where the visit took place (<c>location</c>).</summary>
    [JsonPropertyName("location")] public ServiceLocation? Location { get; init; }

    /// <summary>Work performed (<c>work_items</c>).</summary>
    [JsonPropertyName("work_items")] public IReadOnlyList<ServiceWorkItem>? WorkItems { get; init; }

    /// <summary>Cost; null when the customer did not pay (<c>cost</c>).</summary>
    [JsonPropertyName("cost")] public ServiceCost? Cost { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A driver with access to a vehicle.</summary>
public sealed record VehicleDriver
{
    /// <summary>My Tesla unique id (<c>my_tesla_unique_id</c>).</summary>
    [JsonPropertyName("my_tesla_unique_id")] public long? MyTeslaUniqueId { get; init; }

    /// <summary>User id (<c>user_id</c>); pass to <see cref="ICoilProClient.RemoveDriverAsync"/>.</summary>
    [JsonPropertyName("user_id")] public long? UserId { get; init; }

    /// <summary><see cref="UserId"/> as string (<c>user_id_s</c>).</summary>
    [JsonPropertyName("user_id_s")] public string? UserIdString { get; init; }

    /// <summary>Vault UUID (<c>vault_uuid</c>).</summary>
    [JsonPropertyName("vault_uuid")] public string? VaultUuid { get; init; }

    /// <summary>First name (<c>driver_first_name</c>).</summary>
    [JsonPropertyName("driver_first_name")] public string? FirstName { get; init; }

    /// <summary>Last name (<c>driver_last_name</c>).</summary>
    [JsonPropertyName("driver_last_name")] public string? LastName { get; init; }

    /// <summary>Granular access; <c>hide_private</c> hides location (<c>granular_access</c>).</summary>
    [JsonPropertyName("granular_access")] public GranularAccess? GranularAccess { get; init; }

    /// <summary>Active public keys (<c>active_pubkeys</c>); entries may be null.</summary>
    [JsonPropertyName("active_pubkeys")] public IReadOnlyList<string?>? ActivePubkeys { get; init; }

    /// <summary>Public key, possibly empty (<c>public_key</c>).</summary>
    [JsonPropertyName("public_key")] public string? PublicKey { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Whether mobile access is enabled on a vehicle.</summary>
public sealed record MobileAccessStatus
{
    /// <summary>Reason text, empty when enabled (<c>reason</c>).</summary>
    [JsonPropertyName("reason")] public string? Reason { get; init; }

    /// <summary>True when mobile access is enabled (<c>result</c>).</summary>
    [JsonPropertyName("result")] public bool? Result { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle share invitation.</summary>
public sealed record ShareInvite
{
    /// <summary>Invitation id; use with <see cref="ICoilProClient.RevokeShareInviteAsync"/> (<c>id</c>).</summary>
    [JsonPropertyName("id")] public long? Id { get; init; }

    /// <summary>Owner user id (<c>owner_id</c>).</summary>
    [JsonPropertyName("owner_id")] public long? OwnerId { get; init; }

    /// <summary>User id of the invitee once redeemed (<c>share_user_id</c>).</summary>
    [JsonPropertyName("share_user_id")] public long? ShareUserId { get; init; }

    /// <summary>Product id, the VIN (<c>product_id</c>).</summary>
    [JsonPropertyName("product_id")] public string? ProductId { get; init; }

    /// <summary>State such as <c>pending</c> (<c>state</c>).</summary>
    [JsonPropertyName("state")] public string? State { get; init; }

    /// <summary>Invitation code to redeem (<c>code</c>).</summary>
    [JsonPropertyName("code")] public string? Code { get; init; }

    /// <summary>Expiry, UTC (<c>expires_at</c>).</summary>
    [JsonPropertyName("expires_at")] public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Revocation time, if revoked (<c>revoked_at</c>).</summary>
    [JsonPropertyName("revoked_at")] public DateTimeOffset? RevokedAt { get; init; }

    /// <summary>Product type such as <c>vehicle</c> (<c>product_type</c>).</summary>
    [JsonPropertyName("product_type")] public string? ProductType { get; init; }

    /// <summary>Share type such as <c>customer</c> (<c>share_type</c>).</summary>
    [JsonPropertyName("share_type")] public string? ShareType { get; init; }

    /// <summary>Active public keys (<c>active_pubkeys</c>); entries may be null.</summary>
    [JsonPropertyName("active_pubkeys")] public IReadOnlyList<string?>? ActivePubkeys { get; init; }

    /// <summary><see cref="Id"/> as string (<c>id_s</c>).</summary>
    [JsonPropertyName("id_s")] public string? IdString { get; init; }

    /// <summary>VIN (<c>vin</c>).</summary>
    [JsonPropertyName("vin")] public string? Vin { get; init; }

    /// <summary>Link to hand to the invitee (<c>share_link</c>).</summary>
    [JsonPropertyName("share_link")] public string? ShareLink { get; init; }

    /// <summary>Fields not modeled yet (e.g. <c>borrowing_device_id</c>, <c>key_id</c>), preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Result of redeeming a share invitation.</summary>
public sealed record RedeemedShareInvite
{
    /// <summary>Vehicle id as string (<c>vehicle_id_s</c>).</summary>
    [JsonPropertyName("vehicle_id_s")] public string? VehicleIdString { get; init; }

    /// <summary>VIN of the shared vehicle (<c>vin</c>).</summary>
    [JsonPropertyName("vin")] public string? Vin { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Per-vehicle info of <see cref="FleetStatus"/>.</summary>
public sealed record FleetVehicleInfo
{
    /// <summary>Installed firmware version (<c>firmware_version</c>).</summary>
    [JsonPropertyName("firmware_version")] public string? FirmwareVersion { get; init; }

    /// <summary>Whether the vehicle requires the Vehicle Command Protocol (<c>vehicle_command_protocol_required</c>).</summary>
    [JsonPropertyName("vehicle_command_protocol_required")] public bool? VehicleCommandProtocolRequired { get; init; }

    /// <summary>Whether discounted device-data pricing applies; only returned for single-VIN requests (<c>discounted_device_data</c>).</summary>
    [JsonPropertyName("discounted_device_data")] public bool? DiscountedDeviceData { get; init; }

    /// <summary>Fleet Telemetry version (<c>fleet_telemetry_version</c>).</summary>
    [JsonPropertyName("fleet_telemetry_version")] public string? FleetTelemetryVersion { get; init; }

    /// <summary>Number of keys paired to the vehicle (<c>total_number_of_keys</c>).</summary>
    [JsonPropertyName("total_number_of_keys")] public int? TotalNumberOfKeys { get; init; }

    /// <summary>Safety screen streaming toggle; only older Intel-based S/X (<c>safety_screen_streaming_toggle_enabled</c>).</summary>
    [JsonPropertyName("safety_screen_streaming_toggle_enabled")] public bool? SafetyScreenStreamingToggleEnabled { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Key pairing and firmware info of vehicles (fleet_status).</summary>
public sealed record FleetStatus
{
    /// <summary>VINs that have this application's key paired (<c>key_paired_vins</c>).</summary>
    [JsonPropertyName("key_paired_vins")] public IReadOnlyList<string>? KeyPairedVins { get; init; }

    /// <summary>VINs without a paired key (<c>unpaired_vins</c>).</summary>
    [JsonPropertyName("unpaired_vins")] public IReadOnlyList<string>? UnpairedVins { get; init; }

    /// <summary>Per-VIN details (<c>vehicle_info</c>).</summary>
    [JsonPropertyName("vehicle_info")] public IReadOnlyDictionary<string, FleetVehicleInfo>? VehicleInfo { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle's Fleet Telemetry configuration and sync state.</summary>
public sealed record FleetTelemetryConfigStatus
{
    /// <summary>True when the vehicle has adopted the target config (<c>synced</c>).</summary>
    [JsonPropertyName("synced")] public bool? Synced { get; init; }

    /// <summary>The target configuration; null when none is set (<c>config</c>).</summary>
    [JsonPropertyName("config")] public FleetTelemetryConfig? Config { get; init; }

    /// <summary>True when the vehicle has reached its maximum number of telemetry applications (<c>limit_reached</c>).</summary>
    [JsonPropertyName("limit_reached")] public bool? LimitReached { get; init; }

    /// <summary>True when this application's key is paired (<c>key_paired</c>).</summary>
    [JsonPropertyName("key_paired")] public bool? KeyPaired { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>VINs that were skipped when creating or updating Fleet Telemetry configurations, by reason.</summary>
public sealed record FleetTelemetrySkippedVehicles
{
    /// <summary>VINs without a paired key (<c>missing_key</c>).</summary>
    [JsonPropertyName("missing_key")] public IReadOnlyList<string>? MissingKey { get; init; }

    /// <summary>VINs with unsupported hardware (<c>unsupported_hardware</c>).</summary>
    [JsonPropertyName("unsupported_hardware")] public IReadOnlyList<string>? UnsupportedHardware { get; init; }

    /// <summary>VINs with unsupported firmware (<c>unsupported_firmware</c>).</summary>
    [JsonPropertyName("unsupported_firmware")] public IReadOnlyList<string>? UnsupportedFirmware { get; init; }

    /// <summary>VINs that reached the maximum number of configs (<c>max_configs</c>).</summary>
    [JsonPropertyName("max_configs")] public IReadOnlyList<string>? MaxConfigs { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Result of creating or updating Fleet Telemetry configurations.</summary>
public sealed record FleetTelemetryConfigResult
{
    /// <summary>Number of vehicles updated (<c>updated_vehicles</c>).</summary>
    [JsonPropertyName("updated_vehicles")] public int? UpdatedVehicles { get; init; }

    /// <summary>Skipped VINs by reason (<c>skipped_vehicles</c>).</summary>
    [JsonPropertyName("skipped_vehicles")] public FleetTelemetrySkippedVehicles? SkippedVehicles { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A Fleet Telemetry error reported for a vehicle.</summary>
public sealed record FleetTelemetryError
{
    /// <summary>Client (application) id the error belongs to (<c>name</c>).</summary>
    [JsonPropertyName("name")] public string? Name { get; init; }

    /// <summary>Error message (<c>error</c>).</summary>
    [JsonPropertyName("error")] public string? Error { get; init; }

    /// <summary>VIN (<c>vin</c>).</summary>
    [JsonPropertyName("vin")] public string? Vin { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Query options for <see cref="ICoilProClient.GetNearbyChargingSitesAsync"/>.</summary>
public sealed record NearbyChargingSitesQuery
{
    /// <summary>Maximum number of sites per kind.</summary>
    public int? Count { get; init; }

    /// <summary>Search radius in miles.</summary>
    public int? RadiusMiles { get; init; }

    /// <summary>Include details such as amenities and billing info.</summary>
    public bool? Detail { get; init; }
}
