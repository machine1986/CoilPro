using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil;

/// <summary>
/// Aggregate vehicle data from <c>GET /api/1/vehicles/{tag}/vehicle_data</c>. Sub-states are null when
/// not requested via the <c>endpoints</c> filter. Unmodeled fields are preserved in <see cref="AdditionalData"/>.
/// </summary>
public sealed record VehicleData
{
    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("charge_state")]
    public ChargeState? ChargeState { get; init; }

    [JsonPropertyName("drive_state")]
    public DriveState? DriveState { get; init; }

    [JsonPropertyName("climate_state")]
    public ClimateState? ClimateState { get; init; }

    [JsonPropertyName("vehicle_state")]
    public VehicleState? VehicleState { get; init; }

    [JsonPropertyName("gui_settings")]
    public GuiSettings? GuiSettings { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Charging and battery state. Only the commonly-used fields are modeled.</summary>
public sealed record ChargeState
{
    [JsonPropertyName("battery_level")] public int? BatteryLevel { get; init; }
    [JsonPropertyName("usable_battery_level")] public int? UsableBatteryLevel { get; init; }
    [JsonPropertyName("battery_range")] public double? BatteryRange { get; init; }
    [JsonPropertyName("charging_state")] public string? ChargingState { get; init; }
    [JsonPropertyName("charge_limit_soc")] public int? ChargeLimitSoc { get; init; }
    [JsonPropertyName("charge_energy_added")] public double? ChargeEnergyAdded { get; init; }
    [JsonPropertyName("charger_power")] public int? ChargerPower { get; init; }
    [JsonPropertyName("charger_voltage")] public int? ChargerVoltage { get; init; }
    [JsonPropertyName("charger_actual_current")] public int? ChargerActualCurrent { get; init; }
    [JsonPropertyName("minutes_to_full_charge")] public int? MinutesToFullCharge { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Drive/location state. Requires the <c>location_data</c>/<c>drive_state</c> scope+endpoint.</summary>
public sealed record DriveState
{
    [JsonPropertyName("latitude")] public double? Latitude { get; init; }
    [JsonPropertyName("longitude")] public double? Longitude { get; init; }
    [JsonPropertyName("heading")] public int? Heading { get; init; }
    [JsonPropertyName("speed")] public double? Speed { get; init; }
    [JsonPropertyName("shift_state")] public string? ShiftState { get; init; }
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Climate state.</summary>
public sealed record ClimateState
{
    [JsonPropertyName("inside_temp")] public double? InsideTemp { get; init; }
    [JsonPropertyName("outside_temp")] public double? OutsideTemp { get; init; }
    [JsonPropertyName("is_climate_on")] public bool? IsClimateOn { get; init; }
    [JsonPropertyName("driver_temp_setting")] public double? DriverTempSetting { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>General vehicle state (odometer, locks, firmware, …).</summary>
public sealed record VehicleState
{
    [JsonPropertyName("odometer")] public double? Odometer { get; init; }
    [JsonPropertyName("locked")] public bool? Locked { get; init; }
    [JsonPropertyName("car_version")] public string? CarVersion { get; init; }
    [JsonPropertyName("sentry_mode")] public bool? SentryMode { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>UI/unit preferences.</summary>
public sealed record GuiSettings
{
    [JsonPropertyName("gui_distance_units")] public string? GuiDistanceUnits { get; init; }
    [JsonPropertyName("gui_temperature_units")] public string? GuiTemperatureUnits { get; init; }
    [JsonPropertyName("gui_charge_rate_units")] public string? GuiChargeRateUnits { get; init; }
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
