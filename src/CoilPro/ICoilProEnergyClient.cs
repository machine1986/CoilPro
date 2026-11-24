namespace CoilPro;

/// <summary>
/// Client for Tesla energy products (Powerwall, solar, Wall Connector) on the Fleet API. Site ids come from
/// <see cref="GetProductsAsync"/>. Reads need scope <c>energy_device_data</c>, settings need <c>energy_cmds</c>.
/// Throws <see cref="TeslaApiException"/> on API errors.
/// </summary>
public interface ICoilProEnergyClient
{
    /// <summary>The rate-limit snapshot observed on the most recent response, if any.</summary>
    RateLimitInfo? LastRateLimit { get; }

    /// <summary>All products of the account — vehicles and energy sites (<c>GET /api/1/products</c>).</summary>
    Task<EnergyProducts> GetProductsAsync(CancellationToken ct = default);

    /// <summary>Live power flows and battery level (<c>GET /api/1/energy_sites/{id}/live_status</c>). Power in W, energy in Wh.</summary>
    Task<EnergyLiveStatus> GetLiveStatusAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>Site configuration and components (<c>GET …/site_info</c>).</summary>
    Task<EnergySiteInfo> GetSiteInfoAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>Summary status of the site (<c>GET …/site_status</c>).</summary>
    Task<EnergySiteStatus> GetSiteStatusAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>Estimated backup time left in hours (<c>GET …/backup_time_remaining</c>).</summary>
    Task<double> GetBackupTimeRemainingAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>Live state of the site's Wall Connectors (<c>GET …/charger_live_status</c>).</summary>
    Task<WallConnectorLiveStatus> GetWallConnectorLiveStatusAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>The site's utility tariff (<c>GET …/tariff_rate</c>). Null when the site has none (e.g. Tesla Electric).</summary>
    Task<EnergyTariff?> GetTariffRateAsync(long energySiteId, CancellationToken ct = default);

    /// <summary>Backup (grid outage) events (<c>GET …/calendar_history?kind=backup</c>).</summary>
    Task<EnergyBackupHistory> GetBackupHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default);

    /// <summary>Energy flows over time in Wh (<c>GET …/calendar_history?kind=energy</c>).</summary>
    Task<EnergyHistory> GetEnergyHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default);

    /// <summary>Wall Connector charge sessions (<c>GET …/telemetry_history?kind=charge</c>).</summary>
    Task<EnergyChargeHistory> GetChargeHistoryAsync(long energySiteId, DateTimeOffset start, DateTimeOffset end, string timeZone, CancellationToken ct = default);

    /// <summary>Sets the battery percentage reserved for backup (<c>POST …/backup</c>).</summary>
    Task SetBackupReserveAsync(long energySiteId, int percent, CancellationToken ct = default);

    /// <summary>Sets the battery percentage reserved for charging vehicles while off grid (<c>POST …/off_grid_vehicle_charging_reserve</c>).</summary>
    Task SetOffGridVehicleChargingReserveAsync(long energySiteId, int percent, CancellationToken ct = default);

    /// <summary>Sets the operation mode (<c>POST …/operation</c>).</summary>
    Task SetOperationModeAsync(long energySiteId, EnergyOperationMode mode, CancellationToken ct = default);

    /// <summary>Enables or disables Storm Watch (<c>POST …/storm_mode</c>).</summary>
    Task SetStormModeAsync(long energySiteId, bool enabled, CancellationToken ct = default);

    /// <summary>Grid import/export rules (<c>POST …/grid_import_export</c>); null leaves a setting unchanged.</summary>
    Task SetGridImportExportAsync(long energySiteId, bool? disallowChargeFromGridWithSolar, GridExportRule? exportRule, CancellationToken ct = default);

    /// <summary>
    /// Replaces the site's time-of-use tariff (<c>POST …/time_of_use_settings</c>). <paramref name="tariffContentV2"/> is
    /// Tesla's tariff JSON (same structure as <see cref="GetTariffRateAsync"/>).
    /// </summary>
    Task SetTimeOfUseSettingsAsync(long energySiteId, System.Text.Json.JsonElement tariffContentV2, CancellationToken ct = default);
}
