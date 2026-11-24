using CoilPro.Internal;

namespace CoilPro;

/// <summary>Default <see cref="ICoilProEnergyClient"/> over an <see cref="HttpClient"/> whose base address is the Fleet API host.</summary>
public sealed class CoilProEnergyClient : ICoilProEnergyClient
{
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
    public Task<EnergyProducts> GetProductsAsync(CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetProductsAsync");

    /// <inheritdoc />
    public Task<EnergyLiveStatus> GetLiveStatusAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetLiveStatusAsync");

    /// <inheritdoc />
    public Task<EnergySiteInfo> GetSiteInfoAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetSiteInfoAsync");

    /// <inheritdoc />
    public Task<EnergySiteStatus> GetSiteStatusAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetSiteStatusAsync");

    /// <inheritdoc />
    public Task<double> GetBackupTimeRemainingAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetBackupTimeRemainingAsync");

    /// <inheritdoc />
    public Task<WallConnectorLiveStatus> GetWallConnectorLiveStatusAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetWallConnectorLiveStatusAsync");

    /// <inheritdoc />
    public Task<EnergyTariff?> GetTariffRateAsync(long energySiteId, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetTariffRateAsync");

    /// <inheritdoc />
    public Task<EnergyBackupHistory> GetBackupHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetBackupHistoryAsync");

    /// <inheritdoc />
    public Task<EnergyHistory> GetEnergyHistoryAsync(long energySiteId, EnergyHistoryQuery query, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetEnergyHistoryAsync");

    /// <inheritdoc />
    public Task<EnergyChargeHistory> GetChargeHistoryAsync(long energySiteId, DateTimeOffset start, DateTimeOffset end, string timeZone, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): GetChargeHistoryAsync");

    /// <inheritdoc />
    public Task SetBackupReserveAsync(long energySiteId, int percent, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetBackupReserveAsync");

    /// <inheritdoc />
    public Task SetOffGridVehicleChargingReserveAsync(long energySiteId, int percent, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetOffGridVehicleChargingReserveAsync");

    /// <inheritdoc />
    public Task SetOperationModeAsync(long energySiteId, EnergyOperationMode mode, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetOperationModeAsync");

    /// <inheritdoc />
    public Task SetStormModeAsync(long energySiteId, bool enabled, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetStormModeAsync");

    /// <inheritdoc />
    public Task SetGridImportExportAsync(long energySiteId, bool? disallowChargeFromGridWithSolar, GridExportRule? exportRule, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetGridImportExportAsync");

    /// <inheritdoc />
    public Task SetTimeOfUseSettingsAsync(long energySiteId, System.Text.Json.JsonElement tariffContentV2, CancellationToken ct = default) =>
        throw new NotImplementedException("TODO(v0.2 energy): SetTimeOfUseSettingsAsync");
}
