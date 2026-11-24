using System.Globalization;
using System.Net;
using System.Text.Json;
using CoilPro.Internal;

namespace CoilPro;

/// <summary>Default <see cref="ICoilProEnergyClient"/> over an <see cref="HttpClient"/> whose base address is the Fleet API host.</summary>
public sealed class CoilProEnergyClient : ICoilProEnergyClient
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly FleetApiExecutor _executor;

    /// <summary>Creates the client.</summary>
    /// <param name="http">HTTP client with <see cref="HttpClient.BaseAddress"/> set to the Fleet API base URL.</param>
    /// <param name="tokenProvider">Supplies (and refreshes) the bearer token.</param>
    /// <param name="options">Resilience tunables; defaults when null.</param>
    public CoilProEnergyClient(HttpClient http, ITeslaTokenProvider tokenProvider, CoilProClientOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(tokenProvider);
        _executor = new FleetApiExecutor(
            http,
            async ct => await tokenProvider.GetAccessTokenAsync(ct).ConfigureAwait(false),
            options ?? new CoilProClientOptions());
    }

    /// <inheritdoc />
    public RateLimitInfo? LastRateLimit => _executor.LastRateLimit;

    /// <inheritdoc />
    public async Task<EnergyProducts> GetProductsAsync(CancellationToken ct = default)
    {
        var result = await _executor.SendPagedAsync<JsonElement>(HttpMethod.Get, "api/1/products", ct).ConfigureAwait(false);
        var vehicles = new List<Vehicle>();
        var sites = new List<EnergySiteSummary>();
        if (result.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in result.Value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var deviceType = item.TryGetProperty("device_type", out var dt) && dt.ValueKind == JsonValueKind.String ? dt.GetString() : null;
                var isSite = string.Equals(deviceType, "energy", StringComparison.OrdinalIgnoreCase) || item.TryGetProperty("energy_site_id", out _);
                var isVehicle = !isSite && (string.Equals(deviceType, "vehicle", StringComparison.OrdinalIgnoreCase) || item.TryGetProperty("vin", out _));
                try
                {
                    if (isSite)
                    {
                        if (item.Deserialize<EnergySiteSummary>(JsonOptions) is { } site)
                        {
                            sites.Add(site);
                        }
                    }
                    else if (isVehicle && item.Deserialize<Vehicle>(JsonOptions) is { } vehicle)
                    {
                        vehicles.Add(vehicle);
                    }
                }
                catch (JsonException ex)
                {
                    throw new TeslaApiException(result.StatusCode, $"Failed to parse Fleet API response body: {ex.Message}", responseBody: item.GetRawText(), rateLimit: LastRateLimit);
                }
            }
        }

        return new EnergyProducts { Vehicles = vehicles, EnergySites = sites, Count = result.Count ?? vehicles.Count + sites.Count };
    }

    /// <inheritdoc />
    public async Task<EnergyLiveStatus> GetLiveStatusAsync(long energySiteId, CancellationToken ct = default) =>
        await GetAsync<EnergyLiveStatus>(SitePath(energySiteId, "live_status"), ct).ConfigureAwait(false)
        ?? throw Empty("live_status");

    /// <inheritdoc />
    public async Task<EnergySiteInfo> GetSiteInfoAsync(long energySiteId, CancellationToken ct = default) =>
        await GetAsync<EnergySiteInfo>(SitePath(energySiteId, "site_info"), ct).ConfigureAwait(false)
        ?? throw Empty("site_info");

    /// <inheritdoc />
    public async Task<EnergySiteStatus> GetSiteStatusAsync(long energySiteId, CancellationToken ct = default) =>
        await GetAsync<EnergySiteStatus>(SitePath(energySiteId, "site_status"), ct).ConfigureAwait(false)
        ?? throw Empty("site_status");

    /// <inheritdoc />
    public async Task<double> GetBackupTimeRemainingAsync(long energySiteId, CancellationToken ct = default)
    {
        var element = await GetElementAsync(SitePath(energySiteId, "backup_time_remaining"), ct).ConfigureAwait(false);
        if (element is { ValueKind: JsonValueKind.Object } e
            && e.TryGetProperty("time_remaining_hours", out var hours)
            && hours.ValueKind == JsonValueKind.Number)
        {
            return hours.GetDouble();
        }

        throw Empty("backup_time_remaining");
    }

    /// <inheritdoc />
    public async Task<WallConnectorLiveStatus> GetWallConnectorLiveStatusAsync(long energySiteId, CancellationToken ct = default) =>
        await GetAsync<WallConnectorLiveStatus>(SitePath(energySiteId, "charger_live_status"), ct).ConfigureAwait(false)
        ?? throw Empty("charger_live_status");

    /// <inheritdoc />
    public Task<EnergyTariff?> GetTariffRateAsync(long energySiteId, CancellationToken ct = default) =>
        GetAsync<EnergyTariff>(SitePath(energySiteId, "tariff_rate"), ct);

    /// <inheritdoc />
    public async Task<EnergyBackupHistory> GetBackupHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await GetAsync<EnergyBackupHistory>(HistoryPath(energySiteId, "calendar_history", "backup", query, includePeriod: true), ct).ConfigureAwait(false)
            ?? throw Empty("calendar_history");
    }

    /// <inheritdoc />
    public async Task<EnergyHistory> GetEnergyHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await GetAsync<EnergyHistory>(HistoryPath(energySiteId, "calendar_history", "energy", query, includePeriod: true), ct).ConfigureAwait(false)
            ?? throw Empty("calendar_history");
    }

    /// <inheritdoc />
    public async Task<EnergyChargeHistory> GetChargeHistoryAsync(long energySiteId, DateTimeOffset start, DateTimeOffset end, string timeZone, CancellationToken ct = default)
    {
        var query = new EnergyHistoryQuery { Start = start, End = end, TimeZone = timeZone };
        return await GetAsync<EnergyChargeHistory>(HistoryPath(energySiteId, "telemetry_history", "charge", query, includePeriod: false), ct).ConfigureAwait(false)
            ?? throw Empty("telemetry_history");
    }

    /// <inheritdoc />
    public Task SetBackupReserveAsync(long energySiteId, int percent, CancellationToken ct = default)
    {
        ValidatePercent(percent);
        return PostAsync(SitePath(energySiteId, "backup"), new { backup_reserve_percent = percent }, ct);
    }

    /// <inheritdoc />
    public Task SetOffGridVehicleChargingReserveAsync(long energySiteId, int percent, CancellationToken ct = default)
    {
        ValidatePercent(percent);
        return PostAsync(SitePath(energySiteId, "off_grid_vehicle_charging_reserve"), new { off_grid_vehicle_charging_reserve_percent = percent }, ct);
    }

    /// <inheritdoc />
    public Task SetOperationModeAsync(long energySiteId, EnergyOperationMode mode, CancellationToken ct = default)
    {
        var value = mode switch
        {
            EnergyOperationMode.SelfConsumption => "self_consumption",
            EnergyOperationMode.Autonomous => "autonomous",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown operation mode."),
        };
        return PostAsync(SitePath(energySiteId, "operation"), new { default_real_mode = value }, ct);
    }

    /// <inheritdoc />
    public Task SetStormModeAsync(long energySiteId, bool enabled, CancellationToken ct = default) =>
        PostAsync(SitePath(energySiteId, "storm_mode"), new { enabled }, ct);

    /// <inheritdoc />
    public Task SetGridImportExportAsync(long energySiteId, bool? disallowChargeFromGridWithSolar, GridExportRule? exportRule, CancellationToken ct = default)
    {
        if (disallowChargeFromGridWithSolar is null && exportRule is null)
        {
            throw new ArgumentException("At least one of disallowChargeFromGridWithSolar or exportRule must be specified.");
        }

        var body = new Dictionary<string, object>();
        if (disallowChargeFromGridWithSolar is { } disallow)
        {
            body["disallow_charge_from_grid_with_solar_installed"] = disallow;
        }

        if (exportRule is { } rule)
        {
            body["customer_preferred_export_rule"] = rule switch
            {
                GridExportRule.BatteryOk => "battery_ok",
                GridExportRule.PvOnly => "pv_only",
                GridExportRule.Never => "never",
                _ => throw new ArgumentOutOfRangeException(nameof(exportRule), rule, "Unknown export rule."),
            };
        }

        return PostAsync(SitePath(energySiteId, "grid_import_export"), body, ct);
    }

    /// <inheritdoc />
    public Task SetTimeOfUseSettingsAsync(long energySiteId, JsonElement tariffContentV2, CancellationToken ct = default)
    {
        if (tariffContentV2.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            throw new ArgumentException("The tariff content must be a JSON value.", nameof(tariffContentV2));
        }

        var body = new Dictionary<string, object> { ["tou_settings"] = new Dictionary<string, object> { ["tariff_content_v2"] = tariffContentV2 } };
        return PostAsync(SitePath(energySiteId, "time_of_use_settings"), body, ct);
    }

    private static string SitePath(long energySiteId, string segment) =>
        $"api/1/energy_sites/{energySiteId.ToString(CultureInfo.InvariantCulture)}/{segment}";

    private static string HistoryPath(long energySiteId, string segment, string kind, EnergyHistoryQuery query, bool includePeriod)
    {
        if (string.IsNullOrWhiteSpace(query.TimeZone))
        {
            throw new ArgumentException("A time zone (IANA name) is required.", nameof(query));
        }

        const string format = "yyyy-MM-ddTHH:mm:sszzz";
        var inv = CultureInfo.InvariantCulture;
        var path = $"{SitePath(energySiteId, segment)}?kind={kind}"
            + $"&start_date={Uri.EscapeDataString(query.Start.ToString(format, inv))}"
            + $"&end_date={Uri.EscapeDataString(query.End.ToString(format, inv))}";
        if (includePeriod)
        {
            path += $"&period={query.Period.ToString().ToLowerInvariant()}";
        }

        return path + $"&time_zone={Uri.EscapeDataString(query.TimeZone)}";
    }

    private static void ValidatePercent(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Percent must be between 0 and 100.");
        }
    }

    private TeslaApiException Empty(string endpoint) =>
        new(HttpStatusCode.OK, $"Fleet API returned a success status with an empty or unexpected {endpoint} response.", responseBody: null, rateLimit: LastRateLimit);

    // Settings are sent exactly once (never retried: the first attempt may have taken effect). Any 2xx is success;
    // non-2xx surfaces as TeslaApiException from the executor.
    private async Task PostAsync<TBody>(string path, TBody body, CancellationToken ct) =>
        await _executor.SendAsync<JsonElement>(HttpMethod.Post, path, FleetApiExecutor.JsonBody(body), ct, retryable: false).ConfigureAwait(false);

    private async Task<JsonElement?> GetElementAsync(string path, CancellationToken ct)
    {
        var result = await _executor.SendAsync<JsonElement>(HttpMethod.Get, path, null, ct).ConfigureAwait(false);
        var element = result.Value;

        // Some documented examples double-nest { "response": { "response": ... } }; unwrap tolerantly.
        while (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty("response", out var inner)
            && inner.ValueKind is JsonValueKind.Object or JsonValueKind.Null)
        {
            element = inner;
        }

        return element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null ? null : element;
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        var element = await GetElementAsync(path, ct).ConfigureAwait(false);
        if (element is not { } e)
        {
            return null;
        }

        try
        {
            return e.Deserialize<T>(JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new TeslaApiException(HttpStatusCode.OK, $"Failed to parse Fleet API response body: {ex.Message}", responseBody: e.GetRawText(), rateLimit: LastRateLimit);
        }
    }
}
