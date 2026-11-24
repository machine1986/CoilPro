using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CoilPro.Commands.Internal.Telemetry;

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
/// <remarks>
/// Mirrors the Go proxy's <c>handleFleetTelemetryConfig</c>: the configuration's JSON becomes the JWT claims,
/// <c>iss</c> (base64 public key) and <c>aud</c> (<c>com.tesla.fleet.TelemetryClient</c>) are overwritten, the
/// claims are signed with <c>Tesla.SS256</c> (Schnorr/P-256) and posted as <c>{"vins": [...], "token": "&lt;jws&gt;"}</c>.
/// The request is sent once and never retried.
/// </remarks>
public sealed class FleetTelemetryConfigurator : IFleetTelemetryConfigurator
{
    /// <summary>Maximum number of VINs per request.</summary>
    private const int MaxVins = 100;

    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly TelemetryConfigSender _sender;
    private readonly TeslaKeyPair _keyPair;

    /// <summary>Creates the configurator.</summary>
    /// <param name="fleetApiHttp">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host.</param>
    /// <param name="tokenProvider">Supplies the user's bearer token (scopes <c>vehicle_device_data</c> + <c>vehicle_location</c>).</param>
    /// <param name="keyPair">The application's P-256 key pair (registered with Tesla, installed on the vehicles).</param>
    public FleetTelemetryConfigurator(HttpClient fleetApiHttp, ITeslaTokenProvider tokenProvider, TeslaKeyPair keyPair)
    {
        ArgumentNullException.ThrowIfNull(fleetApiHttp);
        ArgumentNullException.ThrowIfNull(tokenProvider);
        ArgumentNullException.ThrowIfNull(keyPair);
        _sender = new TelemetryConfigSender(fleetApiHttp, tokenProvider);
        _keyPair = keyPair;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">No VINs, more than 100, or an empty VIN.</exception>
    /// <exception cref="TeslaApiException">The Fleet API rejected the request or returned an unreadable body.</exception>
    public async Task<FleetTelemetryConfigResult> ConfigureAsync(IEnumerable<string> vins, FleetTelemetryConfig config, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vins);
        ArgumentNullException.ThrowIfNull(config);

        var vinList = vins.ToList();
        if (vinList.Count == 0)
        {
            throw new ArgumentException("At least one VIN is required.", nameof(vins));
        }

        if (vinList.Count > MaxVins)
        {
            throw new ArgumentException($"At most {MaxVins} VINs per request (Tesla recommends one at a time).", nameof(vins));
        }

        if (vinList.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("VINs must not be empty.", nameof(vins));
        }

        var token = CreateToken(config, _keyPair);
        return await _sender.SendAsync(vinList, token, ct).ConfigureAwait(false);
    }

    /// <summary>Signs <paramref name="config"/> as the Go proxy does (<c>SignMessageForFleet(key, "TelemetryClient", config)</c>).</summary>
    internal static string CreateToken(FleetTelemetryConfig config, TeslaKeyPair keyPair)
    {
        var claims = JsonSerializer.SerializeToNode(config, ConfigJsonOptions)!.AsObject();
        return TeslaJws.SignForFleet(keyPair, TeslaJws.TelemetryApplication, claims);
    }
}
