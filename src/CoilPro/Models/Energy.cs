using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

// Owned by the v0.2 'energy' work package: replace the stub bodies with modeled fields.

/// <summary>All products of an account: vehicles and energy sites (<c>/api/1/products</c>).</summary>
public sealed record EnergyProducts
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Live power flows of an energy site.</summary>
public sealed record EnergyLiveStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Configuration and components of an energy site.</summary>
public sealed record EnergySiteInfo
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Summary status of an energy site.</summary>
public sealed record EnergySiteStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Live state of an energy site's Wall Connectors.</summary>
public sealed record WallConnectorLiveStatus
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A utility tariff of an energy site.</summary>
public sealed record EnergyTariff
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Grid outage (backup) events of an energy site.</summary>
public sealed record EnergyBackupHistory
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Energy flows of an energy site over time (Wh).</summary>
public sealed record EnergyHistory
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Wall Connector charge sessions of an energy site.</summary>
public sealed record EnergyChargeHistory
{
    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Range and resolution for energy-site history queries.</summary>
public sealed record EnergyHistoryQuery
{
    /// <summary>Start of the range.</summary>
    public required DateTimeOffset Start { get; init; }

    /// <summary>End of the range.</summary>
    public required DateTimeOffset End { get; init; }

    /// <summary>Aggregation period.</summary>
    public EnergyHistoryPeriod Period { get; init; } = EnergyHistoryPeriod.Day;

    /// <summary>IANA time zone of the site, e.g. <c>Europe/Berlin</c>.</summary>
    public required string TimeZone { get; init; }
}

/// <summary>Aggregation period of energy history.</summary>
public enum EnergyHistoryPeriod
{
    /// <summary>One day.</summary>
    Day,
    /// <summary>One week.</summary>
    Week,
    /// <summary>One month.</summary>
    Month,
    /// <summary>One year.</summary>
    Year,
    /// <summary>Since installation.</summary>
    Lifetime,
}

/// <summary>Powerwall operation mode (<c>default_real_mode</c>).</summary>
public enum EnergyOperationMode
{
    /// <summary><c>self_consumption</c>: maximise self-powered use.</summary>
    SelfConsumption,
    /// <summary><c>autonomous</c>: time-based control.</summary>
    Autonomous,
}

/// <summary>What the site may export to the grid (<c>customer_preferred_export_rule</c>).</summary>
public enum GridExportRule
{
    /// <summary><c>battery_ok</c>: solar and battery may export.</summary>
    BatteryOk,
    /// <summary><c>pv_only</c>: only solar exports.</summary>
    PvOnly,
    /// <summary><c>never</c>: no export.</summary>
    Never,
}
