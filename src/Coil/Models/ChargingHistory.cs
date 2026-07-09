using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil;

/// <summary>
/// A page of charging history. The precise field set of the Fleet API charging-history endpoint is
/// PROVISIONAL and must be verified against the live API (record deviations in <c>docs/api-deviations.md</c>);
/// unmodeled fields are preserved in <see cref="AdditionalData"/>.
/// </summary>
public sealed record ChargingHistoryPage
{
    /// <summary>The charging records in this page.</summary>
    [JsonPropertyName("data")]
    public IReadOnlyList<ChargingRecord> Data { get; init; } = [];

    /// <summary>Total number of records across all pages, if reported.</summary>
    [JsonPropertyName("totalResults")]
    public int? TotalResults { get; init; }

    /// <summary>Whether more pages are available, if reported.</summary>
    [JsonPropertyName("hasMoreData")]
    public bool? HasMoreData { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A single charging session record (provisional shape — verify against the live API).</summary>
public sealed record ChargingRecord
{
    [JsonPropertyName("vin")] public string? Vin { get; init; }
    [JsonPropertyName("chargeStartDateTime")] public string? ChargeStartDateTime { get; init; }
    [JsonPropertyName("chargeStopDateTime")] public string? ChargeStopDateTime { get; init; }
    [JsonPropertyName("siteLocationName")] public string? SiteLocationName { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
