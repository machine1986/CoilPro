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

    /// <summary>Returns the well-known endpoints for <paramref name="region"/>.</summary>
    public static TeslaEndpoints For(TeslaRegion region) =>
        throw new NotImplementedException("TODO(M1 core-impl): map region -> hosts; verify against developer.tesla.com");
}
