using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

// Owned by the v0.2 'vehicles' work package: replace the stub bodies with modeled fields.

/// <summary>Superchargers and destination chargers near a vehicle.</summary>
public sealed record NearbyChargingSites
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A recent vehicle alert.</summary>
public sealed record VehicleAlert
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One firmware release note.</summary>
public sealed record ReleaseNote
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Current service status of a vehicle.</summary>
public sealed record ServiceData
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A past service visit.</summary>
public sealed record ServiceVisit
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A service visit with work items and cost.</summary>
public sealed record ServiceVisitDetail
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A driver with access to a vehicle.</summary>
public sealed record VehicleDriver
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Whether mobile access is enabled on a vehicle.</summary>
public sealed record MobileAccessStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle share invitation.</summary>
public sealed record ShareInvite
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Result of redeeming a share invitation.</summary>
public sealed record RedeemedShareInvite
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Key pairing and firmware info of vehicles (fleet_status).</summary>
public sealed record FleetStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle's Fleet Telemetry configuration and sync state.</summary>
public sealed record FleetTelemetryConfigStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Result of creating or updating Fleet Telemetry configurations.</summary>
public sealed record FleetTelemetryConfigResult
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A Fleet Telemetry error reported for a vehicle.</summary>
public sealed record FleetTelemetryError
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
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
