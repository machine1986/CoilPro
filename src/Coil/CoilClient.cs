namespace Coil;

/// <summary>
/// Default <see cref="ICoilClient"/> backed by an <see cref="HttpClient"/> whose
/// <see cref="HttpClient.BaseAddress"/> is the Fleet API host for the account's region.
/// </summary>
public sealed class CoilClient : ICoilClient
{
    private readonly HttpClient _http;
    private readonly ITeslaTokenProvider _tokenProvider;
    private readonly CoilClientOptions _options;

    /// <summary>Creates the client.</summary>
    /// <param name="http">HTTP client with <see cref="HttpClient.BaseAddress"/> set to the Fleet API base URL (see <see cref="TeslaEndpoints.FleetApiBaseUrl"/>).</param>
    /// <param name="tokenProvider">Supplies (and refreshes) the bearer token.</param>
    /// <param name="options">Resilience tunables; defaults are used when null.</param>
    public CoilClient(HttpClient http, ITeslaTokenProvider tokenProvider, CoilClientOptions? options = null)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _options = options ?? new CoilClientOptions();
    }

    /// <inheritdoc />
    public RateLimitInfo? LastRateLimit { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M2 core-client)");

    /// <inheritdoc />
    public Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M2 core-client)");

    /// <inheritdoc />
    public Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M2 core-client)");

    /// <inheritdoc />
    public Task<ChargingHistoryPage> GetChargingHistoryAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(M2 core-client)");
}
