namespace CoilPro;

/// <summary>
/// Base URLs for the Tesla Fleet API, resolved per <see cref="TeslaRegion"/>.
/// </summary>
/// <remarks>
/// The concrete hosts change over time — verify against https://developer.tesla.com before release
/// and record deviations in <c>docs/api-deviations.md</c>.
/// </remarks>
public sealed record TeslaEndpoints
{
    /// <summary>OAuth 2.0 token host (region-independent): code exchange, refresh, client credentials.</summary>
    public required Uri AuthBaseUrl { get; init; }

    /// <summary>
    /// Host of the interactive authorize page (region-independent), where the user signs in. This is
    /// <c>auth.tesla.com</c>, not the <see cref="AuthBaseUrl"/> token host — sending the user to
    /// <c>fleet-auth.prd.vn.cloud.tesla.com/oauth2/v3/authorize</c> fails with
    /// "The 'redirect_uri' supplied is not registered for this 'client_id'".
    /// </summary>
    public Uri AuthorizeBaseUrl { get; init; } = new("https://auth.tesla.com");

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
