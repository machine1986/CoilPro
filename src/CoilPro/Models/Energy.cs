using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

// Energy values are in Wh, power values in W unless stated otherwise.

/// <summary>All products of an account: vehicles and energy sites (<c>/api/1/products</c>).</summary>
public sealed record EnergyProducts
{
    /// <summary>Vehicle products (<c>device_type</c> = <c>vehicle</c>).</summary>
    public IReadOnlyList<Vehicle> Vehicles { get; init; } = [];

    /// <summary>Energy-site products (Powerwall, solar, Wall Connector).</summary>
    public IReadOnlyList<EnergySiteSummary> EnergySites { get; init; } = [];

    /// <summary>Total number of products as reported by Tesla (falls back to the parsed item count).</summary>
    public int Count { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>An energy site as listed by <c>/api/1/products</c>.</summary>
public sealed record EnergySiteSummary
{
    /// <summary>The site id used in all <c>energy_sites/{id}</c> paths.</summary>
    [JsonPropertyName("energy_site_id")]
    public long EnergySiteId { get; init; }

    /// <summary>Device type, <c>energy</c>.</summary>
    [JsonPropertyName("device_type")]
    public string? DeviceType { get; init; }

    /// <summary>Resource type, e.g. <c>battery</c> or <c>solar</c>.</summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; init; }

    /// <summary>User-assigned site name.</summary>
    [JsonPropertyName("site_name")]
    public string? SiteName { get; init; }

    /// <summary>Device id of the site (a string on energy sites, unlike vehicles).</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>Gateway id (DIN).</summary>
    [JsonPropertyName("gateway_id")]
    public string? GatewayId { get; init; }

    /// <summary>Energy currently stored in the battery, in Wh.</summary>
    [JsonPropertyName("energy_left")]
    public double? EnergyLeft { get; init; }

    /// <summary>Total battery pack capacity, in Wh.</summary>
    [JsonPropertyName("total_pack_energy")]
    public double? TotalPackEnergy { get; init; }

    /// <summary>Battery state of charge in percent.</summary>
    [JsonPropertyName("percentage_charged")]
    public double? PercentageCharged { get; init; }

    /// <summary>Battery power in W (negative = charging).</summary>
    [JsonPropertyName("battery_power")]
    public double? BatteryPower { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Live power flows of an energy site. Power in W, energy in Wh.</summary>
public sealed record EnergyLiveStatus
{
    /// <summary>Solar production in W.</summary>
    [JsonPropertyName("solar_power")]
    public double? SolarPower { get; init; }

    /// <summary>Energy stored in the battery, in Wh.</summary>
    [JsonPropertyName("energy_left")]
    public double? EnergyLeft { get; init; }

    /// <summary>Total battery capacity, in Wh.</summary>
    [JsonPropertyName("total_pack_energy")]
    public double? TotalPackEnergy { get; init; }

    /// <summary>Battery state of charge in percent.</summary>
    [JsonPropertyName("percentage_charged")]
    public double? PercentageCharged { get; init; }

    /// <summary>Whether the site can provide backup power.</summary>
    [JsonPropertyName("backup_capable")]
    public bool? BackupCapable { get; init; }

    /// <summary>Battery power in W (negative = charging).</summary>
    [JsonPropertyName("battery_power")]
    public double? BatteryPower { get; init; }

    /// <summary>Home load in W.</summary>
    [JsonPropertyName("load_power")]
    public double? LoadPower { get; init; }

    /// <summary>Grid status, e.g. <c>Active</c>.</summary>
    [JsonPropertyName("grid_status")]
    public string? GridStatus { get; init; }

    /// <summary>Grid power in W (positive = importing).</summary>
    [JsonPropertyName("grid_power")]
    public double? GridPower { get; init; }

    /// <summary>Islanding state, e.g. <c>on_grid</c>.</summary>
    [JsonPropertyName("island_status")]
    public string? IslandStatus { get; init; }

    /// <summary>Whether Storm Watch is currently active.</summary>
    [JsonPropertyName("storm_mode_active")]
    public bool? StormModeActive { get; init; }

    /// <summary>Time of the measurement.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Live state of the site's Wall Connectors (<c>wall_connectors</c>).</summary>
    [JsonPropertyName("wall_connectors")]
    public IReadOnlyList<WallConnectorState>? WallConnectors { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Configuration and components of an energy site.</summary>
public sealed record EnergySiteInfo
{
    /// <summary>Site id (DIN string).</summary>
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    /// <summary>User-assigned site name.</summary>
    [JsonPropertyName("site_name")]
    public string? SiteName { get; init; }

    /// <summary>Backup reserve in percent.</summary>
    [JsonPropertyName("backup_reserve_percent")]
    public int? BackupReservePercent { get; init; }

    /// <summary>Operation mode as reported by Tesla (<c>autonomous</c> or <c>self_consumption</c>).</summary>
    [JsonPropertyName("default_real_mode")]
    public string? DefaultRealMode { get; init; }

    /// <summary>Installation date.</summary>
    [JsonPropertyName("installation_date")]
    public DateTimeOffset? InstallationDate { get; init; }

    /// <summary>User settings.</summary>
    [JsonPropertyName("user_settings")]
    public EnergySiteUserSettings? UserSettings { get; init; }

    /// <summary>Installed components and capabilities.</summary>
    [JsonPropertyName("components")]
    public EnergySiteComponents? Components { get; init; }

    /// <summary>Firmware version.</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>Number of batteries.</summary>
    [JsonPropertyName("battery_count")]
    public int? BatteryCount { get; init; }

    /// <summary>Nameplate power in W.</summary>
    [JsonPropertyName("nameplate_power")]
    public double? NameplatePower { get; init; }

    /// <summary>Nameplate energy in Wh.</summary>
    [JsonPropertyName("nameplate_energy")]
    public double? NameplateEnergy { get; init; }

    /// <summary>IANA time zone of the installation.</summary>
    [JsonPropertyName("installation_time_zone")]
    public string? InstallationTimeZone { get; init; }

    /// <summary>Maximum site meter power (AC) in W.</summary>
    [JsonPropertyName("max_site_meter_power_ac")]
    public double? MaxSiteMeterPowerAc { get; init; }

    /// <summary>Minimum site meter power (AC) in W.</summary>
    [JsonPropertyName("min_site_meter_power_ac")]
    public double? MinSiteMeterPowerAc { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>User settings of an energy site.</summary>
public sealed record EnergySiteUserSettings
{
    /// <summary>Whether Storm Watch is enabled.</summary>
    [JsonPropertyName("storm_mode_enabled")]
    public bool? StormModeEnabled { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Installed components and capabilities of an energy site.</summary>
public sealed record EnergySiteComponents
{
    /// <summary>Solar installed.</summary>
    [JsonPropertyName("solar")]
    public bool? Solar { get; init; }

    /// <summary>Solar type, e.g. <c>pv_panel</c>.</summary>
    [JsonPropertyName("solar_type")]
    public string? SolarType { get; init; }

    /// <summary>Battery installed.</summary>
    [JsonPropertyName("battery")]
    public bool? Battery { get; init; }

    /// <summary>Grid connection present.</summary>
    [JsonPropertyName("grid")]
    public bool? Grid { get; init; }

    /// <summary>Backup capable.</summary>
    [JsonPropertyName("backup")]
    public bool? Backup { get; init; }

    /// <summary>Load meter present.</summary>
    [JsonPropertyName("load_meter")]
    public bool? LoadMeter { get; init; }

    /// <summary>Storm Watch supported.</summary>
    [JsonPropertyName("storm_mode_capable")]
    public bool? StormModeCapable { get; init; }

    /// <summary>Off-grid vehicle charging reserve supported.</summary>
    [JsonPropertyName("off_grid_vehicle_charging_reserve_supported")]
    public bool? OffGridVehicleChargingReserveSupported { get; init; }

    /// <summary>Solar value enabled.</summary>
    [JsonPropertyName("solar_value_enabled")]
    public bool? SolarValueEnabled { get; init; }

    /// <summary>Islanding mode can be set.</summary>
    [JsonPropertyName("set_islanding_mode_enabled")]
    public bool? SetIslandingModeEnabled { get; init; }

    /// <summary>Battery type, e.g. <c>ac_powerwall</c>.</summary>
    [JsonPropertyName("battery_type")]
    public string? BatteryType { get; init; }

    /// <summary>Whether the site is configurable.</summary>
    [JsonPropertyName("configurable")]
    public bool? Configurable { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Summary status of an energy site.</summary>
public sealed record EnergySiteStatus
{
    /// <summary>Resource type, e.g. <c>gateway</c>.</summary>
    [JsonPropertyName("resource_type")]
    public string? ResourceType { get; init; }

    /// <summary>User-assigned site name.</summary>
    [JsonPropertyName("site_name")]
    public string? SiteName { get; init; }

    /// <summary>Gateway id (DIN).</summary>
    [JsonPropertyName("gateway_id")]
    public string? GatewayId { get; init; }

    /// <summary>Battery state of charge in percent.</summary>
    [JsonPropertyName("percentage_charged")]
    public double? PercentageCharged { get; init; }

    /// <summary>Battery power in W.</summary>
    [JsonPropertyName("battery_power")]
    public double? BatteryPower { get; init; }

    /// <summary>Whether Storm Watch is enabled.</summary>
    [JsonPropertyName("storm_mode_enabled")]
    public bool? StormModeEnabled { get; init; }

    /// <summary>Whether the Powerwall onboarding settings are set.</summary>
    [JsonPropertyName("powerwall_onboarding_settings_set")]
    public bool? PowerwallOnboardingSettingsSet { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Live state of an energy site's Wall Connectors.</summary>
public sealed record WallConnectorLiveStatus
{
    /// <summary>The site's Wall Connectors.</summary>
    [JsonPropertyName("wall_connectors")]
    public IReadOnlyList<WallConnectorState>? WallConnectors { get; init; }

    /// <summary>Time of the measurement.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Fields not modeled yet (e.g. <c>wall_boxes</c>), preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Live state of one Wall Connector.</summary>
public sealed record WallConnectorState
{
    /// <summary>Device id (DIN).</summary>
    [JsonPropertyName("din")]
    public string? Din { get; init; }

    /// <summary>Numeric state code as reported by Tesla (meaning not documented).</summary>
    [JsonPropertyName("wall_connector_state")]
    public int? State { get; init; }

    /// <summary>Charging power in W.</summary>
    [JsonPropertyName("wall_connector_power")]
    public double? Power { get; init; }

    /// <summary>Numeric fault state code (<c>wall_connector_fault_state</c>; meaning not documented, 2 observed when idle).</summary>
    [JsonPropertyName("wall_connector_fault_state")]
    public int? FaultState { get; init; }

    /// <summary>OCPP connection status code (<c>ocpp_status</c>).</summary>
    [JsonPropertyName("ocpp_status")]
    public int? OcppStatus { get; init; }

    /// <summary>Powershare session state code (<c>powershare_session_state</c>).</summary>
    [JsonPropertyName("powershare_session_state")]
    public int? PowershareSessionState { get; init; }

    /// <summary>Dry-contact control state code (<c>dry_contact_control_state</c>).</summary>
    [JsonPropertyName("dry_contact_control_state")]
    public int? DryContactControlState { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A utility tariff of an energy site.</summary>
public sealed record EnergyTariff
{
    /// <summary>Tariff code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Plan name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Utility name.</summary>
    [JsonPropertyName("utility")]
    public string? Utility { get; init; }

    /// <summary>ISO currency code.</summary>
    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    /// <summary>Energy prices per kWh: season (or <c>ALL</c>) to period label to price.</summary>
    [JsonPropertyName("energy_charges")]
    public IDictionary<string, IDictionary<string, double>>? EnergyCharges { get; init; }

    /// <summary>Demand charges: season (or <c>ALL</c>) to period label to price.</summary>
    [JsonPropertyName("demand_charges")]
    public IDictionary<string, IDictionary<string, double>>? DemandCharges { get; init; }

    /// <summary>Seasons by name.</summary>
    [JsonPropertyName("seasons")]
    public IDictionary<string, EnergyTariffSeason>? Seasons { get; init; }

    /// <summary>Feed-in (sell) tariff.</summary>
    [JsonPropertyName("sell_tariff")]
    public EnergySellTariff? SellTariff { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A tariff season with its time-of-use periods.</summary>
public sealed record EnergyTariffSeason
{
    /// <summary>Start month (1-12).</summary>
    [JsonPropertyName("fromMonth")]
    public int? FromMonth { get; init; }

    /// <summary>Start day of month.</summary>
    [JsonPropertyName("fromDay")]
    public int? FromDay { get; init; }

    /// <summary>End month (1-12).</summary>
    [JsonPropertyName("toMonth")]
    public int? ToMonth { get; init; }

    /// <summary>End day of month.</summary>
    [JsonPropertyName("toDay")]
    public int? ToDay { get; init; }

    /// <summary>Time-of-use periods by label (e.g. <c>ON_PEAK</c>).</summary>
    [JsonPropertyName("tou_periods")]
    public IDictionary<string, IReadOnlyList<EnergyTouPeriod>>? TouPeriods { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A time-of-use period (days of week and hours).</summary>
public sealed record EnergyTouPeriod
{
    /// <summary>First day of week.</summary>
    [JsonPropertyName("fromDayOfWeek")]
    public int? FromDayOfWeek { get; init; }

    /// <summary>Last day of week.</summary>
    [JsonPropertyName("toDayOfWeek")]
    public int? ToDayOfWeek { get; init; }

    /// <summary>Start hour.</summary>
    [JsonPropertyName("fromHour")]
    public int? FromHour { get; init; }

    /// <summary>Start minute.</summary>
    [JsonPropertyName("fromMinute")]
    public int? FromMinute { get; init; }

    /// <summary>End hour.</summary>
    [JsonPropertyName("toHour")]
    public int? ToHour { get; init; }

    /// <summary>End minute.</summary>
    [JsonPropertyName("toMinute")]
    public int? ToMinute { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Feed-in (sell) tariff of a site.</summary>
public sealed record EnergySellTariff
{
    /// <summary>Tariff code.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Utility name.</summary>
    [JsonPropertyName("utility")]
    public string? Utility { get; init; }

    /// <summary>Sell prices: season (or <c>ALL</c>) to period label to price.</summary>
    [JsonPropertyName("energy_charges")]
    public IDictionary<string, IDictionary<string, double>>? EnergyCharges { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Grid outage (backup) events of an energy site.</summary>
public sealed record EnergyBackupHistory
{
    /// <summary>The outage events.</summary>
    [JsonPropertyName("events")]
    public IReadOnlyList<EnergyBackupEvent>? Events { get; init; }

    /// <summary>Total number of events.</summary>
    [JsonPropertyName("total_events")]
    public int? TotalEvents { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One grid outage event.</summary>
public sealed record EnergyBackupEvent
{
    /// <summary>Start of the outage.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Duration in seconds.</summary>
    [JsonPropertyName("duration")]
    public long? DurationSeconds { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Energy flows of an energy site over time (Wh).</summary>
public sealed record EnergyHistory
{
    /// <summary>The aggregation period echoed by Tesla.</summary>
    [JsonPropertyName("period")]
    public string? Period { get; init; }

    /// <summary>Time series entries.</summary>
    [JsonPropertyName("time_series")]
    public IReadOnlyList<EnergyHistoryEntry>? TimeSeries { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One time-series entry of <see cref="EnergyHistory"/>. All energy values are in Wh.</summary>
public sealed record EnergyHistoryEntry
{
    /// <summary>Start of the bucket.</summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>Solar energy produced, Wh.</summary>
    [JsonPropertyName("solar_energy_exported")]
    public double? SolarEnergyExported { get; init; }

    /// <summary>Generator energy produced, Wh.</summary>
    [JsonPropertyName("generator_energy_exported")]
    public double? GeneratorEnergyExported { get; init; }

    /// <summary>Energy imported from the grid, Wh.</summary>
    [JsonPropertyName("grid_energy_imported")]
    public double? GridEnergyImported { get; init; }

    /// <summary>Grid-services energy imported, Wh.</summary>
    [JsonPropertyName("grid_services_energy_imported")]
    public double? GridServicesEnergyImported { get; init; }

    /// <summary>Grid-services energy exported, Wh.</summary>
    [JsonPropertyName("grid_services_energy_exported")]
    public double? GridServicesEnergyExported { get; init; }

    /// <summary>Energy exported to the grid from solar, Wh.</summary>
    [JsonPropertyName("grid_energy_exported_from_solar")]
    public double? GridEnergyExportedFromSolar { get; init; }

    /// <summary>Energy exported to the grid from the generator, Wh.</summary>
    [JsonPropertyName("grid_energy_exported_from_generator")]
    public double? GridEnergyExportedFromGenerator { get; init; }

    /// <summary>Energy exported to the grid from the battery, Wh.</summary>
    [JsonPropertyName("grid_energy_exported_from_battery")]
    public double? GridEnergyExportedFromBattery { get; init; }

    /// <summary>Energy discharged from the battery, Wh.</summary>
    [JsonPropertyName("battery_energy_exported")]
    public double? BatteryEnergyExported { get; init; }

    /// <summary>Battery energy charged from the grid, Wh.</summary>
    [JsonPropertyName("battery_energy_imported_from_grid")]
    public double? BatteryEnergyImportedFromGrid { get; init; }

    /// <summary>Battery energy charged from solar, Wh.</summary>
    [JsonPropertyName("battery_energy_imported_from_solar")]
    public double? BatteryEnergyImportedFromSolar { get; init; }

    /// <summary>Battery energy charged from the generator, Wh.</summary>
    [JsonPropertyName("battery_energy_imported_from_generator")]
    public double? BatteryEnergyImportedFromGenerator { get; init; }

    /// <summary>Home consumption supplied by the grid, Wh.</summary>
    [JsonPropertyName("consumer_energy_imported_from_grid")]
    public double? ConsumerEnergyImportedFromGrid { get; init; }

    /// <summary>Home consumption supplied by solar, Wh.</summary>
    [JsonPropertyName("consumer_energy_imported_from_solar")]
    public double? ConsumerEnergyImportedFromSolar { get; init; }

    /// <summary>Home consumption supplied by the battery, Wh.</summary>
    [JsonPropertyName("consumer_energy_imported_from_battery")]
    public double? ConsumerEnergyImportedFromBattery { get; init; }

    /// <summary>Home consumption supplied by the generator, Wh.</summary>
    [JsonPropertyName("consumer_energy_imported_from_generator")]
    public double? ConsumerEnergyImportedFromGenerator { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Wall Connector charge sessions of an energy site.</summary>
public sealed record EnergyChargeHistory
{
    /// <summary>The charge sessions.</summary>
    [JsonPropertyName("charge_history")]
    public IReadOnlyList<EnergyChargeSession>? ChargeHistory { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>One Wall Connector charge session.</summary>
public sealed record EnergyChargeSession
{
    /// <summary>Start time as Unix seconds.</summary>
    [JsonPropertyName("charge_start_time")]
    public EnergySeconds? ChargeStartTime { get; init; }

    /// <summary>Duration in seconds.</summary>
    [JsonPropertyName("charge_duration")]
    public EnergySeconds? ChargeDuration { get; init; }

    /// <summary>Energy added, Wh.</summary>
    [JsonPropertyName("energy_added_wh")]
    public double? EnergyAddedWh { get; init; }

    /// <summary>Wall Connector device id that delivered the session (<c>target_id.text</c>).</summary>
    [JsonPropertyName("target_id")]
    public EnergyTargetId? TargetId { get; init; }

    /// <summary>Wall Connector DIN (<c>din</c>).</summary>
    [JsonPropertyName("din")]
    public string? Din { get; init; }

    /// <summary>Start time as a <see cref="DateTimeOffset"/> (UTC), when present.</summary>
    [JsonIgnore]
    public DateTimeOffset? StartTime => ChargeStartTime?.Seconds is { } s ? DateTimeOffset.FromUnixTimeSeconds(s) : null;

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>A <c>{ "seconds": n }</c> value (Unix time or duration).</summary>
public sealed record EnergySeconds
{
    /// <summary>The number of seconds.</summary>
    [JsonPropertyName("seconds")]
    public long? Seconds { get; init; }

    /// <summary>Fields not modeled yet, preserved for forward compatibility.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Range and resolution for energy-site history queries.</summary>
public sealed record EnergyHistoryQuery
{
    /// <summary>Start of the range.</summary>
    public required DateTimeOffset Start { get; init; }

    /// <summary>End of the range.</summary>
    public required DateTimeOffset End { get; init; }

    /// <summary>Aggregation period.</summary>
    public EnergyHistoryPeriod Period { get; init; } = EnergyHistoryPeriod.Day;

    /// <summary>IANA time zone of the site, e.g. <c>Europe/Berlin</c>.</summary>
    public required string TimeZone { get; init; }
}

/// <summary>Aggregation period of energy history.</summary>
public enum EnergyHistoryPeriod
{
    /// <summary>One day.</summary>
    Day,
    /// <summary>One week.</summary>
    Week,
    /// <summary>One month.</summary>
    Month,
    /// <summary>One year.</summary>
    Year,
    /// <summary>Since installation.</summary>
    Lifetime,
}

/// <summary>Powerwall operation mode (<c>default_real_mode</c>).</summary>
public enum EnergyOperationMode
{
    /// <summary><c>self_consumption</c>: maximise self-powered use.</summary>
    SelfConsumption,
    /// <summary><c>autonomous</c>: time-based control.</summary>
    Autonomous,
}

/// <summary>What the site may export to the grid (<c>customer_preferred_export_rule</c>).</summary>
public enum GridExportRule
{
    /// <summary><c>battery_ok</c>: solar and battery may export.</summary>
    BatteryOk,
    /// <summary><c>pv_only</c>: only solar exports.</summary>
    PvOnly,
    /// <summary><c>never</c>: no export.</summary>
    Never,
}

/// <summary>Identifier wrapper <c>{ "text": "…" }</c> used by energy telemetry history.</summary>
public sealed record EnergyTargetId
{
    /// <summary>The id (<c>text</c>).</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
