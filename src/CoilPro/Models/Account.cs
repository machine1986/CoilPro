using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

// Owned by the v0.2 'account' work package: replace the stub bodies with modeled fields.

/// <summary>Result of an unsigned REST command: <c>{ result, reason, queued }</c>.</summary>
public sealed record RestCommandResult
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>The authenticated user's profile.</summary>
public sealed record UserProfile
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A vehicle order of the user.</summary>
public sealed record VehicleOrder
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>The user's Fleet API region and base URL.</summary>
public sealed record UserRegion
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A page of business-fleet charging sessions.</summary>
public sealed record ChargingSessionsPage
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An option code of a vehicle.</summary>
public sealed record VehicleOption
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Active, upcoming and expired warranties.</summary>
public sealed record WarrantyDetails
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Subscriptions a vehicle is eligible for.</summary>
public sealed record SubscriptionEligibility
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Upgrades a vehicle is eligible for.</summary>
public sealed record UpgradeEligibility
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Owner, drivers and payers of a business vehicle.</summary>
public sealed record EnterpriseRoles
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Vehicle specifications (partner token, billed per result).</summary>
public sealed record VehicleSpecs
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Model configuration prices for a market (partner token).</summary>
public sealed record VehiclePricing
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Where a navigation destination goes in the current trip.</summary>
public enum NavigationTripOrder
{
    /// <summary>Replace the current destination.</summary>
    Replace = 0,

    /// <summary>Insert before the current destination.</summary>
    Prepend = 1,

    /// <summary>Append after the current destination.</summary>
    Append = 2,
}

/// <summary>Filters and paging for <see cref="ICoilProClient.GetChargingHistoryAsync"/> (all optional).</summary>
public sealed record ChargingHistoryQuery
{
    /// <summary>Only sessions of this VIN.</summary>
    public string? Vin { get; init; }

    /// <summary>Sessions starting at or after this time.</summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>Sessions starting before this time.</summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>Page number (Tesla <c>pageNo</c>).</summary>
    public int? PageNumber { get; init; }

    /// <summary>Page size (Tesla <c>pageSize</c>).</summary>
    public int? PageSize { get; init; }

    /// <summary>Field to sort by (Tesla <c>sortBy</c>).</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort ascending (true) or descending (false).</summary>
    public bool? Ascending { get; init; }
}

/// <summary>Filters and paging for <see cref="ICoilProClient.GetChargingSessionsAsync"/> (all optional).</summary>
public sealed record ChargingSessionsQuery
{
    /// <summary>Only sessions of this VIN.</summary>
    public string? Vin { get; init; }

    /// <summary>Sessions from this date.</summary>
    public DateTimeOffset? DateFrom { get; init; }

    /// <summary>Sessions up to this date.</summary>
    public DateTimeOffset? DateTo { get; init; }

    /// <summary>Maximum number of sessions.</summary>
    public int? Limit { get; init; }

    /// <summary>Number of sessions to skip.</summary>
    public int? Offset { get; init; }
}

/// <summary>Which bill a business payer covers.</summary>
public enum EnterprisePayerRole
{
    /// <summary>Supercharging costs.</summary>
    Charging,

    /// <summary>Connectivity and other subscriptions.</summary>
    Subscription,
}

/// <summary>Request for <see cref="ICoilProClient.SetEnterprisePayerAsync"/>: set exactly one of <see cref="AccountId"/> or <see cref="FederationId"/>.</summary>
public sealed record EnterprisePayerRequest
{
    /// <summary>Which bill the payer covers.</summary>
    public required EnterprisePayerRole Role { get; init; }

    /// <summary>Business account id of the payer.</summary>
    public string? AccountId { get; init; }

    /// <summary>Federation id of a driver as payer.</summary>
    public string? FederationId { get; init; }
}
