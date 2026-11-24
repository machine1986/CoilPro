namespace CoilPro;

/// <summary>
/// Read-access client for the Tesla Fleet API vehicle endpoints. All calls attach a bearer token from
/// the configured <see cref="ITeslaTokenProvider"/> and throw <see cref="TeslaApiException"/> on API errors.
/// </summary>
public interface ICoilProClient
{
    /// <summary>Lists the vehicles on the authenticated account (<c>GET /api/1/vehicles</c>).</summary>
    Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default);

    /// <summary>
    /// Returns a vehicle's data (<c>GET /api/1/vehicles/{tag}/vehicle_data</c>). Pass
    /// <paramref name="endpoints"/> to request only the segments you need (lower cost). The vehicle must be awake.
    /// </summary>
    /// <param name="vehicleTag">The vehicle id or VIN.</param>
    /// <param name="endpoints">Which data segments to request; <see cref="VehicleDataEndpoint.None"/> for the default set.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default);

    /// <summary>Wakes a sleeping vehicle (<c>POST /api/1/vehicles/{tag}/wake_up</c>). Returns its latest summary.</summary>
    Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default);

    /// <summary>Returns charging history for the account. Shape is provisional — verify against the live API.</summary>
    Task<ChargingHistoryPage> GetChargingHistoryAsync(CancellationToken ct = default);

    /// <summary>The rate-limit snapshot observed on the most recent response, if any.</summary>
    RateLimitInfo? LastRateLimit { get; }
}
