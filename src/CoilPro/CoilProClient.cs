using CoilPro.Internal;

namespace CoilPro;

/// <summary>
/// Default <see cref="ICoilProClient"/> backed by an <see cref="HttpClient"/> whose
/// <see cref="HttpClient.BaseAddress"/> is the Fleet API host for the account's region.
/// </summary>
public sealed partial class CoilProClient : ICoilProClient
{
    private readonly FleetApiExecutor _executor;

    /// <summary>Creates the client.</summary>
    /// <param name="http">HTTP client with <see cref="HttpClient.BaseAddress"/> set to the Fleet API base URL (see <see cref="TeslaEndpoints.FleetApiBaseUrl"/>).</param>
    /// <param name="tokenProvider">Supplies (and refreshes) the bearer token.</param>
    /// <param name="options">Resilience tunables; defaults are used when null.</param>
    public CoilProClient(HttpClient http, ITeslaTokenProvider tokenProvider, CoilProClientOptions? options = null)
    {
        _executor = new FleetApiExecutor(
            http,
            async ct => await tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false),
            options ?? new CoilProClientOptions());
    }

    /// <inheritdoc />
    public RateLimitInfo? LastRateLimit => _executor.LastRateLimit;

    /// <inheritdoc />
    public async Task<IReadOnlyList<Vehicle>> GetVehiclesAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendAsync<List<Vehicle>>(HttpMethod.Get, "api/1/vehicles", null, ct).ConfigureAwait(false);
        return result.Value ?? [];
    }

    /// <inheritdoc />
    public async Task<VehicleData> GetVehicleDataAsync(string vehicleTag, VehicleDataEndpoint endpoints = VehicleDataEndpoint.None, CancellationToken ct = default)
    {
        var path = $"api/1/vehicles/{Uri.EscapeDataString(vehicleTag)}/vehicle_data";
        var queryValue = endpoints.ToQueryValue();
        if (queryValue is not null)
        {
            path = $"{path}?endpoints={Uri.EscapeDataString(queryValue)}";
        }

        var result = await _executor.SendAsync<VehicleData>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
        return result.Value ?? throw new TeslaApiException(
            result.StatusCode,
            "Fleet API returned a success status with an empty vehicle_data response.",
            responseBody: null,
            rateLimit: LastRateLimit);
    }

    /// <inheritdoc />
    public async Task<Vehicle> WakeUpAsync(string vehicleTag, CancellationToken ct = default)
    {
        var path = $"api/1/vehicles/{Uri.EscapeDataString(vehicleTag)}/wake_up";
        var result = await _executor.SendAsync<Vehicle>(HttpMethod.Post, path, null, ct).ConfigureAwait(false);
        return result.Value ?? throw new TeslaApiException(
            result.StatusCode,
            "Fleet API returned a success status with an empty wake_up response.",
            responseBody: null,
            rateLimit: LastRateLimit);
    }

    /// <inheritdoc />
    public async Task<ChargingHistoryPage> GetChargingHistoryAsync(ChargingHistoryQuery? query = null, CancellationToken ct = default)
    {
        var parameters = new List<string>();
        if (query is not null)
        {
            Add("vin", query.Vin);
            Add("startTime", query.StartTime?.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture));
            Add("endTime", query.EndTime?.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture));
            Add("pageNo", query.PageNumber?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Add("pageSize", query.PageSize?.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Add("sortBy", query.SortBy);
            Add("sortOrder", query.Ascending is null ? null : query.Ascending.Value ? "ASC" : "DESC");
        }

        var path = parameters.Count == 0 ? "api/1/dx/charging/history" : "api/1/dx/charging/history?" + string.Join("&", parameters);

        // No { "response": ... } envelope on this endpoint (confirmed live 2026-11-24).
        var result = await _executor.SendAsync<ChargingHistoryPage>(HttpMethod.Get, path, null, ct, unwrapEnvelope: false).ConfigureAwait(false);
        return result.Value ?? new ChargingHistoryPage();

        void Add(string name, string? value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                parameters.Add($"{name}={Uri.EscapeDataString(value)}");
            }
        }
    }
}
