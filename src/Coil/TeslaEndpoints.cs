namespace Coil;

/// <summary>
/// Base URLs for the Tesla Fleet API, resolved per <see cref="TeslaRegion"/>.
/// </summary>
/// <remarks>
/// The concrete hosts change over time — verify against https://developer.tesla.com before release
/// and record deviations in <c>docs/api-deviations.md</c>.
/// </remarks>
public sealed record TeslaEndpoints
{
    /// <summary>OAuth 2.0 authorization / token host (region-independent).</summary>
    public required Uri AuthBaseUrl { get; init; }

    /// <summary>Fleet API host for the region. Also used as the token <c>audience</c>.</summary>
    public required Uri FleetApiBaseUrl { get; init; }

    /// <summary>
    /// Returns the well-known endpoints for <paramref name="region"/>.
    /// </summary>
    /// <remarks>
    /// NOTE: these hostnames were current at the time of writing but MUST be re-verified against
    /// https://developer.tesla.com before relying on them in production; Tesla can change hosts
    /// without notice. Record any deviation in <c>docs/api-deviations.md</c>.
    /// </remarks>
    public static TeslaEndpoints For(TeslaRegion region) => region switch
    {
        TeslaRegion.Europe => new TeslaEndpoints
        {
            AuthBaseUrl = new Uri("https://fleet-auth.prd.vn.cloud.tesla.com"),
            FleetApiBaseUrl = new Uri("https://fleet-api.prd.eu.vn.cloud.tesla.com"),
        },
        TeslaRegion.NorthAmerica => new TeslaEndpoints
        {
            AuthBaseUrl = new Uri("https://fleet-auth.prd.vn.cloud.tesla.com"),
            FleetApiBaseUrl = new Uri("https://fleet-api.prd.na.vn.cloud.tesla.com"),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(region), region, "Unknown Tesla region."),
    };
}
