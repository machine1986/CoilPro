using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil;

/// <summary>
/// Aggregate vehicle data from <c>GET /api/1/vehicles/{tag}/vehicle_data</c>. Sub-states are null when
/// not requested via the <c>endpoints</c> filter. Unmodeled fields are preserved in <see cref="AdditionalData"/>.
/// </summary>
public sealed record VehicleData
{
    /// <summary>Numeric vehicle id (<c>id</c>).</summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>Vehicle identification number (<c>vin</c>).</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Connectivity state such as <c>online</c> or <c>asleep</c> (<c>state</c>).</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>Charging and battery state; null unless requested.</summary>
    [JsonPropertyName("charge_state")]
    public ChargeState? ChargeState { get; init; }

    /// <summary>Drive and location state; null unless requested.</summary>
    [JsonPropertyName("drive_state")]
    public DriveState? DriveState { get; init; }

    /// <summary>Climate state; null unless requested.</summary>
    [JsonPropertyName("climate_state")]
    public ClimateState? ClimateState { get; init; }

    /// <summary>General vehicle state; null unless requested.</summary>
    [JsonPropertyName("vehicle_state")]
    public VehicleState? VehicleState { get; init; }

    /// <summary>Display and unit preferences; null unless requested.</summary>
    [JsonPropertyName("gui_settings")]
    public GuiSettings? GuiSettings { get; init; }

    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Charging and battery state. Only the commonly-used fields are modeled.</summary>
public sealed record ChargeState
{
    /// <summary>Battery charge in percent (<c>battery_level</c>).</summary>
    [JsonPropertyName("battery_level")] public int? BatteryLevel { get; init; }
    /// <summary>Usable battery charge in percent (<c>usable_battery_level</c>).</summary>
    [JsonPropertyName("usable_battery_level")] public int? UsableBatteryLevel { get; init; }
    /// <summary>Estimated range as reported by the Fleet API field <c>battery_range</c> (miles).</summary>
    [JsonPropertyName("battery_range")] public double? BatteryRange { get; init; }
    /// <summary>Charging status string such as <c>Charging</c>, <c>Stopped</c> or <c>Disconnected</c> (<c>charging_state</c>).</summary>
    [JsonPropertyName("charging_state")] public string? ChargingState { get; init; }
    /// <summary>Charge limit in percent (<c>charge_limit_soc</c>).</summary>
    [JsonPropertyName("charge_limit_soc")] public int? ChargeLimitSoc { get; init; }
    /// <summary>Energy added in the current or last session, kWh (<c>charge_energy_added</c>).</summary>
    [JsonPropertyName("charge_energy_added")] public double? ChargeEnergyAdded { get; init; }
    /// <summary>Charging power in kW (<c>charger_power</c>).</summary>
    [JsonPropertyName("charger_power")] public int? ChargerPower { get; init; }
    /// <summary>Charger voltage in volts (<c>charger_voltage</c>).</summary>
    [JsonPropertyName("charger_voltage")] public int? ChargerVoltage { get; init; }
    /// <summary>Actual charging current in amperes (<c>charger_actual_current</c>).</summary>
    [JsonPropertyName("charger_actual_current")] public int? ChargerActualCurrent { get; init; }
    /// <summary>Minutes until the charge limit is reached (<c>minutes_to_full_charge</c>).</summary>
    [JsonPropertyName("minutes_to_full_charge")] public int? MinutesToFullCharge { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Drive/location state. Requires the <c>location_data</c>/<c>drive_state</c> scope+endpoint.</summary>
public sealed record DriveState
{
    /// <summary>Latitude in degrees (<c>latitude</c>); requires the location scope.</summary>
    [JsonPropertyName("latitude")] public double? Latitude { get; init; }
    /// <summary>Longitude in degrees (<c>longitude</c>); requires the location scope.</summary>
    [JsonPropertyName("longitude")] public double? Longitude { get; init; }
    /// <summary>Compass heading in degrees (<c>heading</c>).</summary>
    [JsonPropertyName("heading")] public int? Heading { get; init; }
    /// <summary>Speed as reported by the Fleet API field <c>speed</c> (mph); null when parked.</summary>
    [JsonPropertyName("speed")] public double? Speed { get; init; }
    /// <summary>Gear selector state such as <c>P</c>, <c>D</c>, <c>R</c> (<c>shift_state</c>); null when parked.</summary>
    [JsonPropertyName("shift_state")] public string? ShiftState { get; init; }
    /// <summary>Time of the reading as reported by <c>timestamp</c> (Unix epoch, milliseconds).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Climate state.</summary>
public sealed record ClimateState
{
    /// <summary>Cabin temperature in degrees Celsius (<c>inside_temp</c>).</summary>
    [JsonPropertyName("inside_temp")] public double? InsideTemp { get; init; }
    /// <summary>Outside temperature in degrees Celsius (<c>outside_temp</c>).</summary>
    [JsonPropertyName("outside_temp")] public double? OutsideTemp { get; init; }
    /// <summary>Whether climate control is running (<c>is_climate_on</c>).</summary>
    [JsonPropertyName("is_climate_on")] public bool? IsClimateOn { get; init; }
    /// <summary>Driver-side target temperature in degrees Celsius (<c>driver_temp_setting</c>).</summary>
    [JsonPropertyName("driver_temp_setting")] public double? DriverTempSetting { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>General vehicle state (odometer, locks, firmware, …).</summary>
public sealed record VehicleState
{
    /// <summary>Odometer reading as reported by the Fleet API field <c>odometer</c> (miles).</summary>
    [JsonPropertyName("odometer")] public double? Odometer { get; init; }
    /// <summary>Whether the vehicle is locked (<c>locked</c>).</summary>
    [JsonPropertyName("locked")] public bool? Locked { get; init; }
    /// <summary>Installed software version string (<c>car_version</c>).</summary>
    [JsonPropertyName("car_version")] public string? CarVersion { get; init; }
    /// <summary>Whether Sentry Mode is active (<c>sentry_mode</c>).</summary>
    [JsonPropertyName("sentry_mode")] public bool? SentryMode { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>UI/unit preferences.</summary>
public sealed record GuiSettings
{
    /// <summary>Distance unit shown in the vehicle UI, e.g. <c>km/hr</c> (<c>gui_distance_units</c>).</summary>
    [JsonPropertyName("gui_distance_units")] public string? GuiDistanceUnits { get; init; }
    /// <summary>Temperature unit shown in the vehicle UI, <c>C</c> or <c>F</c> (<c>gui_temperature_units</c>).</summary>
    [JsonPropertyName("gui_temperature_units")] public string? GuiTemperatureUnits { get; init; }
    /// <summary>Charge rate unit shown in the vehicle UI (<c>gui_charge_rate_units</c>).</summary>
    [JsonPropertyName("gui_charge_rate_units")] public string? GuiChargeRateUnits { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
