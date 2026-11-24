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

    /// <summary>Exterior color code, if reported (<c>color</c>).</summary>
    [JsonPropertyName("color")]
    public string? Color { get; init; }

    /// <summary>Access level of the caller, e.g. <c>OWNER</c> or <c>DRIVER</c> (<c>access_type</c>).</summary>
    [JsonPropertyName("access_type")]
    public string? AccessType { get; init; }

    /// <summary>Comma-separated option codes (<c>option_codes</c>).</summary>
    [JsonPropertyName("option_codes")]
    public string? OptionCodes { get; init; }

    /// <summary>Granular access restrictions; <c>hide_private</c> means location is not shared (<c>granular_access</c>).</summary>
    [JsonPropertyName("granular_access")]
    public GranularAccess? GranularAccess { get; init; }

    /// <summary>Legacy streaming tokens (<c>tokens</c>).</summary>
    [JsonPropertyName("tokens")]
    public IReadOnlyList<string>? Tokens { get; init; }

    /// <summary>Whether the vehicle is in service (<c>in_service</c>).</summary>
    [JsonPropertyName("in_service")]
    public bool? InService { get; init; }

    /// <summary><see cref="Id"/> as a string (<c>id_s</c>).</summary>
    [JsonPropertyName("id_s")]
    public string? IdString { get; init; }

    /// <summary>Whether calendar sync is enabled (<c>calendar_enabled</c>).</summary>
    [JsonPropertyName("calendar_enabled")]
    public bool? CalendarEnabled { get; init; }

    /// <summary>Vehicle API version, if reported (<c>api_version</c>).</summary>
    [JsonPropertyName("api_version")]
    public int? ApiVersion { get; init; }

    /// <summary>Back-seat token, if any (<c>backseat_token</c>).</summary>
    [JsonPropertyName("backseat_token")]
    public string? BackseatToken { get; init; }

    /// <summary>When the back-seat token was updated, if reported (<c>backseat_token_updated_at</c>).</summary>
    [JsonPropertyName("backseat_token_updated_at")]
    public JsonElement? BackseatTokenUpdatedAt { get; init; }

    /// <summary>Owning user id; present on wake_up and vehicle_data responses (<c>user_id</c>).</summary>
    [JsonPropertyName("user_id")]
    public long? UserId { get; init; }

    /// <summary>Any fields not modeled above, preserved for forward-compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Granular access restrictions of a vehicle share.</summary>
public sealed record GranularAccess
{
    /// <summary>When true the vehicle must not share its location (<c>hide_private</c>).</summary>
    [JsonPropertyName("hide_private")]
    public bool? HidePrivate { get; init; }

    /// <summary>Fields not modeled above, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
