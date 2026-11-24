using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>
/// A Fleet Telemetry configuration: where the vehicle streams to and which fields it sends
/// (<c>config</c> of <c>fleet_telemetry_config</c>). Field names are listed in Tesla's
/// <c>fleet-telemetry/protos/vehicle_data.proto</c>.
/// </summary>
public sealed record FleetTelemetryConfig
{
    /// <summary>Host of your telemetry server; must share the root domain of the application.</summary>
    [JsonPropertyName("hostname")] public required string Hostname { get; init; }

    /// <summary>PEM certificate chain the vehicle uses to verify the server.</summary>
    [JsonPropertyName("ca")] public required string Ca { get; init; }

    /// <summary>Port of the telemetry server.</summary>
    [JsonPropertyName("port")] public required int Port { get; init; }

    /// <summary>Streamed fields by name, e.g. <c>"BatteryLevel"</c>.</summary>
    [JsonPropertyName("fields")] public IDictionary<string, FleetTelemetryFieldConfig> Fields { get; init; } = new Dictionary<string, FleetTelemetryFieldConfig>();

    /// <summary>Alert types to stream: <c>service</c>, <c>customer</c>, <c>service-fix</c>.</summary>
    [JsonPropertyName("alert_types")] public IReadOnlyList<string>? AlertTypes { get; init; }

    /// <summary>Expiry as Unix seconds; null = no expiry set by the client.</summary>
    [JsonPropertyName("exp")] public long? Expiry { get; init; }

    /// <summary>Delivery policy, e.g. <c>latest</c>.</summary>
    [JsonPropertyName("delivery_policy")] public string? DeliveryPolicy { get; init; }

    /// <summary>Fields not modeled above, preserved for forward compatibility.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Streaming settings of one Fleet Telemetry field.</summary>
public sealed record FleetTelemetryFieldConfig
{
    /// <summary>Send at most every this many seconds.</summary>
    [JsonPropertyName("interval_seconds")] public required int IntervalSeconds { get; init; }

    /// <summary>Only send when the value changed by at least this much.</summary>
    [JsonPropertyName("minimum_delta")] public double? MinimumDelta { get; init; }

    /// <summary>Resend the unchanged value after this many seconds.</summary>
    [JsonPropertyName("resend_interval_seconds")] public int? ResendIntervalSeconds { get; init; }

    /// <summary>Additional fields to include with this one.</summary>
    [JsonPropertyName("include_fields")] public IReadOnlyList<string>? IncludeFields { get; init; }
}
