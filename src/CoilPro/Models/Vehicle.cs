using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>A vehicle summary as returned by <c>GET /api/1/vehicles</c>.</summary>
public sealed record Vehicle
{
    /// <summary>Numeric vehicle id used in most Fleet API paths.</summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>Stable per-vehicle id (distinct from <see cref="Id"/>).</summary>
    [JsonPropertyName("vehicle_id")]
    public long VehicleId { get; init; }

    /// <summary>Vehicle identification number.</summary>
    [JsonPropertyName("vin")]
    public string Vin { get; init; } = string.Empty;

    /// <summary>User-assigned display name.</summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; init; }

    /// <summary>Online state, e.g. <c>online</c>, <c>asleep</c>, <c>offline</c>.</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>Any fields not modeled above, preserved for forward-compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
