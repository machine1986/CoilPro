namespace CoilPro.Commands;

/// <summary>
/// Creates or updates Fleet Telemetry configurations. Tesla requires the configuration to be signed with the
/// application's key (a Schnorr/P-256 JWS posted to <c>POST /api/1/vehicles/fleet_telemetry_config_jws</c>) —
/// the job Tesla's Go proxy does; this class does it natively. Reading and deleting configurations is on
/// <c>ICoilProClient</c>.
/// </summary>
public interface IFleetTelemetryConfigurator
{
    /// <summary>
    /// Signs <paramref name="config"/> and applies it to <paramref name="vins"/> (up to 100; Tesla recommends one at a time).
    /// The vehicles must have the application's virtual key.
    /// </summary>
    Task<FleetTelemetryConfigResult> ConfigureAsync(IEnumerable<string> vins, FleetTelemetryConfig config, CancellationToken ct = default);
}

/// <summary>Default <see cref="IFleetTelemetryConfigurator"/>.</summary>
public sealed class FleetTelemetryConfigurator : IFleetTelemetryConfigurator
{
    /// <summary>Creates the configurator.</summary>
    /// <param name="fleetApiHttp">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host.</param>
    /// <param name="tokenProvider">Supplies the user's bearer token (scopes <c>vehicle_device_data</c> + <c>vehicle_location</c>).</param>
    /// <param name="keyPair">The application's P-256 key pair (registered with Tesla, installed on the vehicles).</param>
    public FleetTelemetryConfigurator(HttpClient fleetApiHttp, ITeslaTokenProvider tokenProvider, TeslaKeyPair keyPair)
    {
        ArgumentNullException.ThrowIfNull(fleetApiHttp);
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(keyPair);
    }

    /// <inheritdoc />
    public Task<FleetTelemetryConfigResult> ConfigureAsync(IEnumerable<string> vins, FleetTelemetryConfig config, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 telemetry): ConfigureAsync");
}
