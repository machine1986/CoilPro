using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoilPro;

/// <summary>
/// Aggregate vehicle data from <c>GET /api/1/vehicles/{tag}/vehicle_data</c> (and <c>vehicle_data_cached</c>).
/// Sub-states are null when not requested via the <c>endpoints</c> filter. Unmodeled fields are preserved in <see cref="AdditionalData"/>.
/// The Fleet API reports imperial units (miles, mph) and Celsius regardless of the vehicle UI; pressures are bar.
/// </summary>
public sealed record VehicleData
{
    /// <summary>Numeric vehicle id (<c>id</c>).</summary>
    [JsonPropertyName("id")]
    public long Id { get; init; }

    /// <summary>Numeric user id (<c>user_id</c>).</summary>
    [JsonPropertyName("user_id")]
    public long? UserId { get; init; }

    /// <summary>Vehicle identification number (<c>vin</c>).</summary>
    [JsonPropertyName("vin")]
    public string? Vin { get; init; }

    /// <summary>Connectivity state such as <c>online</c> or <c>asleep</c> (<c>state</c>).</summary>
    [JsonPropertyName("state")]
    public string? State { get; init; }

    /// <summary>Charging and battery state; null unless requested.</summary>
    [JsonPropertyName("charge_state")]
    public ChargeState? ChargeState { get; init; }

    /// <summary>Drive state; null unless requested.</summary>
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

    /// <summary>Vehicle configuration; null unless requested.</summary>
    [JsonPropertyName("vehicle_config")]
    public VehicleConfig? VehicleConfig { get; init; }

    /// <summary>Location block (<c>location_data</c>); null unless requested. UNVERIFIED shape.</summary>
    [JsonPropertyName("location_data")]
    public LocationData? LocationData { get; init; }

    /// <summary>Closures block (<c>closures_state</c>); null unless requested. UNVERIFIED shape.</summary>
    [JsonPropertyName("closures_state")]
    public ClosuresState? ClosuresState { get; init; }

    /// <summary>Charge schedule block (<c>charge_schedule_data</c>); null unless requested. UNVERIFIED shape.</summary>
    [JsonPropertyName("charge_schedule_data")]
    public ChargeScheduleData? ChargeScheduleData { get; init; }

    /// <summary>Preconditioning schedule block (<c>preconditioning_schedule_data</c>); null unless requested. UNVERIFIED shape.</summary>
    [JsonPropertyName("preconditioning_schedule_data")]
    public PreconditioningScheduleData? PreconditioningScheduleData { get; init; }

    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Charging and battery state. Distances are miles (the Fleet API reports imperial units regardless of the vehicle UI).</summary>
public sealed record ChargeState
{
    /// <summary>Battery charge in percent (<c>battery_level</c>).</summary>
    [JsonPropertyName("battery_level")] public int? BatteryLevel { get; init; }
    /// <summary>Usable battery charge in percent (<c>usable_battery_level</c>).</summary>
    [JsonPropertyName("usable_battery_level")] public int? UsableBatteryLevel { get; init; }
    /// <summary>Estimated range, miles (<c>battery_range</c>).</summary>
    [JsonPropertyName("battery_range")] public double? BatteryRange { get; init; }
    /// <summary>Estimated range based on recent driving, miles (<c>est_battery_range</c>).</summary>
    [JsonPropertyName("est_battery_range")] public double? EstBatteryRange { get; init; }
    /// <summary>Ideal range, miles (<c>ideal_battery_range</c>).</summary>
    [JsonPropertyName("ideal_battery_range")] public double? IdealBatteryRange { get; init; }
    /// <summary>Charging status such as Charging, Stopped, Disconnected, Complete (<c>charging_state</c>).</summary>
    [JsonPropertyName("charging_state")] public string? ChargingState { get; init; }
    /// <summary>Charge limit in percent (<c>charge_limit_soc</c>).</summary>
    [JsonPropertyName("charge_limit_soc")] public int? ChargeLimitSoc { get; init; }
    /// <summary>Maximum selectable charge limit, percent (<c>charge_limit_soc_max</c>).</summary>
    [JsonPropertyName("charge_limit_soc_max")] public int? ChargeLimitSocMax { get; init; }
    /// <summary>Minimum selectable charge limit, percent (<c>charge_limit_soc_min</c>).</summary>
    [JsonPropertyName("charge_limit_soc_min")] public int? ChargeLimitSocMin { get; init; }
    /// <summary>Standard charge limit, percent (<c>charge_limit_soc_std</c>).</summary>
    [JsonPropertyName("charge_limit_soc_std")] public int? ChargeLimitSocStd { get; init; }
    /// <summary>Energy added in the current or last session, kWh (<c>charge_energy_added</c>).</summary>
    [JsonPropertyName("charge_energy_added")] public double? ChargeEnergyAdded { get; init; }
    /// <summary>Ideal range added, miles (<c>charge_miles_added_ideal</c>).</summary>
    [JsonPropertyName("charge_miles_added_ideal")] public double? ChargeMilesAddedIdeal { get; init; }
    /// <summary>Rated range added, miles (<c>charge_miles_added_rated</c>).</summary>
    [JsonPropertyName("charge_miles_added_rated")] public double? ChargeMilesAddedRated { get; init; }
    /// <summary>Configured charging current, A (<c>charge_amps</c>).</summary>
    [JsonPropertyName("charge_amps")] public int? ChargeAmps { get; init; }
    /// <summary>Requested charging current, A (<c>charge_current_request</c>).</summary>
    [JsonPropertyName("charge_current_request")] public int? ChargeCurrentRequest { get; init; }
    /// <summary>Maximum requestable charging current, A (<c>charge_current_request_max</c>).</summary>
    [JsonPropertyName("charge_current_request_max")] public int? ChargeCurrentRequestMax { get; init; }
    /// <summary>Whether charging is enabled (<c>charge_enable_request</c>).</summary>
    [JsonPropertyName("charge_enable_request")] public bool? ChargeEnableRequest { get; init; }
    /// <summary>User-set charge enable request (<c>user_charge_enable_request</c>).</summary>
    [JsonPropertyName("user_charge_enable_request")] public bool? UserChargeEnableRequest { get; init; }
    /// <summary>Whether the charge port door is open (<c>charge_port_door_open</c>).</summary>
    [JsonPropertyName("charge_port_door_open")] public bool? ChargePortDoorOpen { get; init; }
    /// <summary>Charge port latch state such as Engaged (<c>charge_port_latch</c>).</summary>
    [JsonPropertyName("charge_port_latch")] public string? ChargePortLatch { get; init; }
    /// <summary>Charge port LED color (<c>charge_port_color</c>).</summary>
    [JsonPropertyName("charge_port_color")] public string? ChargePortColor { get; init; }
    /// <summary>Whether cold-weather mode of the charge port is on (<c>charge_port_cold_weather_mode</c>).</summary>
    [JsonPropertyName("charge_port_cold_weather_mode")] public bool? ChargePortColdWeatherMode { get; init; }
    /// <summary>Charge rate, mph of range per hour (<c>charge_rate</c>).</summary>
    [JsonPropertyName("charge_rate")] public double? ChargeRate { get; init; }
    /// <summary>Charging power, kW (<c>charger_power</c>).</summary>
    [JsonPropertyName("charger_power")] public int? ChargerPower { get; init; }
    /// <summary>Charger voltage, V (<c>charger_voltage</c>).</summary>
    [JsonPropertyName("charger_voltage")] public int? ChargerVoltage { get; init; }
    /// <summary>Actual charging current, A (<c>charger_actual_current</c>).</summary>
    [JsonPropertyName("charger_actual_current")] public int? ChargerActualCurrent { get; init; }
    /// <summary>Pilot current of the charger, A (<c>charger_pilot_current</c>).</summary>
    [JsonPropertyName("charger_pilot_current")] public int? ChargerPilotCurrent { get; init; }
    /// <summary>Number of charging phases (<c>charger_phases</c>).</summary>
    [JsonPropertyName("charger_phases")] public int? ChargerPhases { get; init; }
    /// <summary>Connected cable type (<c>conn_charge_cable</c>).</summary>
    [JsonPropertyName("conn_charge_cable")] public string? ConnChargeCable { get; init; }
    /// <summary>Fast charger brand (<c>fast_charger_brand</c>).</summary>
    [JsonPropertyName("fast_charger_brand")] public string? FastChargerBrand { get; init; }
    /// <summary>Whether a DC fast charger is connected (<c>fast_charger_present</c>).</summary>
    [JsonPropertyName("fast_charger_present")] public bool? FastChargerPresent { get; init; }
    /// <summary>Fast charger type (<c>fast_charger_type</c>).</summary>
    [JsonPropertyName("fast_charger_type")] public string? FastChargerType { get; init; }
    /// <summary>Whether the battery heater is on (<c>battery_heater_on</c>).</summary>
    [JsonPropertyName("battery_heater_on")] public bool? BatteryHeaterOn { get; init; }
    /// <summary>Whether there is not enough power to heat the battery (<c>not_enough_power_to_heat</c>).</summary>
    [JsonPropertyName("not_enough_power_to_heat")] public bool? NotEnoughPowerToHeat { get; init; }
    /// <summary>Whether managed charging is active (<c>managed_charging_active</c>).</summary>
    [JsonPropertyName("managed_charging_active")] public bool? ManagedChargingActive { get; init; }
    /// <summary>Whether the user canceled managed charging (<c>managed_charging_user_canceled</c>).</summary>
    [JsonPropertyName("managed_charging_user_canceled")] public bool? ManagedChargingUserCanceled { get; init; }
    /// <summary>Managed charging start, Unix epoch (<c>managed_charging_start_time</c>).</summary>
    [JsonPropertyName("managed_charging_start_time")] public long? ManagedChargingStartTime { get; init; }
    /// <summary>Max-range charge counter (<c>max_range_charge_counter</c>).</summary>
    [JsonPropertyName("max_range_charge_counter")] public int? MaxRangeChargeCounter { get; init; }
    /// <summary>Minutes until the charge limit is reached (<c>minutes_to_full_charge</c>).</summary>
    [JsonPropertyName("minutes_to_full_charge")] public int? MinutesToFullCharge { get; init; }
    /// <summary>Hours until the charge limit is reached (<c>time_to_full_charge</c>).</summary>
    [JsonPropertyName("time_to_full_charge")] public double? TimeToFullCharge { get; init; }
    /// <summary>Whether off-peak charging is enabled (<c>off_peak_charging_enabled</c>).</summary>
    [JsonPropertyName("off_peak_charging_enabled")] public bool? OffPeakChargingEnabled { get; init; }
    /// <summary>Off-peak charging times setting (<c>off_peak_charging_times</c>).</summary>
    [JsonPropertyName("off_peak_charging_times")] public string? OffPeakChargingTimes { get; init; }
    /// <summary>Off-peak end, minutes after midnight (<c>off_peak_hours_end_time</c>).</summary>
    [JsonPropertyName("off_peak_hours_end_time")] public int? OffPeakHoursEndTime { get; init; }
    /// <summary>Whether preconditioning is enabled (<c>preconditioning_enabled</c>).</summary>
    [JsonPropertyName("preconditioning_enabled")] public bool? PreconditioningEnabled { get; init; }
    /// <summary>Preconditioning times setting (<c>preconditioning_times</c>).</summary>
    [JsonPropertyName("preconditioning_times")] public string? PreconditioningTimes { get; init; }
    /// <summary>Scheduled charging mode such as Off (<c>scheduled_charging_mode</c>).</summary>
    [JsonPropertyName("scheduled_charging_mode")] public string? ScheduledChargingMode { get; init; }
    /// <summary>Whether scheduled charging is pending (<c>scheduled_charging_pending</c>).</summary>
    [JsonPropertyName("scheduled_charging_pending")] public bool? ScheduledChargingPending { get; init; }
    /// <summary>Scheduled charging start, Unix epoch seconds (<c>scheduled_charging_start_time</c>).</summary>
    [JsonPropertyName("scheduled_charging_start_time")] public long? ScheduledChargingStartTime { get; init; }
    /// <summary>Scheduled departure, Unix epoch seconds (<c>scheduled_departure_time</c>).</summary>
    [JsonPropertyName("scheduled_departure_time")] public long? ScheduledDepartureTime { get; init; }
    /// <summary>Scheduled departure, minutes after midnight (<c>scheduled_departure_time_minutes</c>).</summary>
    [JsonPropertyName("scheduled_departure_time_minutes")] public int? ScheduledDepartureTimeMinutes { get; init; }
    /// <summary>Whether the Supercharger session came from the trip planner (<c>supercharger_session_trip_planner</c>).</summary>
    [JsonPropertyName("supercharger_session_trip_planner")] public bool? SuperchargerSessionTripPlanner { get; init; }
    /// <summary>Whether trip charging is active (<c>trip_charging</c>).</summary>
    [JsonPropertyName("trip_charging")] public bool? TripCharging { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Climate state. Temperatures are degrees Celsius.</summary>
public sealed record ClimateState
{
    /// <summary>Cabin temperature, degrees Celsius (<c>inside_temp</c>).</summary>
    [JsonPropertyName("inside_temp")] public double? InsideTemp { get; init; }
    /// <summary>Outside temperature, degrees Celsius (<c>outside_temp</c>).</summary>
    [JsonPropertyName("outside_temp")] public double? OutsideTemp { get; init; }
    /// <summary>Whether climate control is running (<c>is_climate_on</c>).</summary>
    [JsonPropertyName("is_climate_on")] public bool? IsClimateOn { get; init; }
    /// <summary>Driver-side target temperature, degrees Celsius (<c>driver_temp_setting</c>).</summary>
    [JsonPropertyName("driver_temp_setting")] public double? DriverTempSetting { get; init; }
    /// <summary>Passenger-side target temperature, degrees Celsius (<c>passenger_temp_setting</c>).</summary>
    [JsonPropertyName("passenger_temp_setting")] public double? PassengerTempSetting { get; init; }
    /// <summary>Maximum selectable temperature, degrees Celsius (<c>max_avail_temp</c>).</summary>
    [JsonPropertyName("max_avail_temp")] public double? MaxAvailTemp { get; init; }
    /// <summary>Minimum selectable temperature, degrees Celsius (<c>min_avail_temp</c>).</summary>
    [JsonPropertyName("min_avail_temp")] public double? MinAvailTemp { get; init; }
    /// <summary>Whether cabin overheat protection is allowed (<c>allow_cabin_overheat_protection</c>).</summary>
    [JsonPropertyName("allow_cabin_overheat_protection")] public bool? AllowCabinOverheatProtection { get; init; }
    /// <summary>Cabin overheat protection mode such as On, Off, FanOnly (<c>cabin_overheat_protection</c>).</summary>
    [JsonPropertyName("cabin_overheat_protection")] public string? CabinOverheatProtection { get; init; }
    /// <summary>Whether overheat protection is cooling (<c>cabin_overheat_protection_actively_cooling</c>).</summary>
    [JsonPropertyName("cabin_overheat_protection_actively_cooling")] public bool? CabinOverheatProtectionActivelyCooling { get; init; }
    /// <summary>Overheat protection activation temperature setting (<c>cop_activation_temperature</c>).</summary>
    [JsonPropertyName("cop_activation_temperature")] public string? CopActivationTemperature { get; init; }
    /// <summary>Whether fan-only overheat protection is supported (<c>supports_fan_only_cabin_overheat_protection</c>).</summary>
    [JsonPropertyName("supports_fan_only_cabin_overheat_protection")] public bool? SupportsFanOnlyCabinOverheatProtection { get; init; }
    /// <summary>Auto seat climate, left (<c>auto_seat_climate_left</c>).</summary>
    [JsonPropertyName("auto_seat_climate_left")] public bool? AutoSeatClimateLeft { get; init; }
    /// <summary>Auto seat climate, right (<c>auto_seat_climate_right</c>).</summary>
    [JsonPropertyName("auto_seat_climate_right")] public bool? AutoSeatClimateRight { get; init; }
    /// <summary>Auto steering wheel heat (<c>auto_steering_wheel_heat</c>).</summary>
    [JsonPropertyName("auto_steering_wheel_heat")] public bool? AutoSteeringWheelHeat { get; init; }
    /// <summary>Whether the battery heater is on (<c>battery_heater</c>).</summary>
    [JsonPropertyName("battery_heater")] public bool? BatteryHeater { get; init; }
    /// <summary>Whether the battery heater lacks power (<c>battery_heater_no_power</c>).</summary>
    [JsonPropertyName("battery_heater_no_power")] public bool? BatteryHeaterNoPower { get; init; }
    /// <summary>Whether Bioweapon Defense Mode is on (<c>bioweapon_mode</c>).</summary>
    [JsonPropertyName("bioweapon_mode")] public bool? BioweaponMode { get; init; }
    /// <summary>Climate keeper mode such as off, dog, camp (<c>climate_keeper_mode</c>).</summary>
    [JsonPropertyName("climate_keeper_mode")] public string? ClimateKeeperMode { get; init; }
    /// <summary>Defrost mode (<c>defrost_mode</c>).</summary>
    [JsonPropertyName("defrost_mode")] public int? DefrostMode { get; init; }
    /// <summary>Fan speed (<c>fan_status</c>).</summary>
    [JsonPropertyName("fan_status")] public int? FanStatus { get; init; }
    /// <summary>HVAC auto request (<c>hvac_auto_request</c>).</summary>
    [JsonPropertyName("hvac_auto_request")] public string? HvacAutoRequest { get; init; }
    /// <summary>Whether auto conditioning is on (<c>is_auto_conditioning_on</c>).</summary>
    [JsonPropertyName("is_auto_conditioning_on")] public bool? IsAutoConditioningOn { get; init; }
    /// <summary>Whether the front defroster is on (<c>is_front_defroster_on</c>).</summary>
    [JsonPropertyName("is_front_defroster_on")] public bool? IsFrontDefrosterOn { get; init; }
    /// <summary>Whether the vehicle is preconditioning (<c>is_preconditioning</c>).</summary>
    [JsonPropertyName("is_preconditioning")] public bool? IsPreconditioning { get; init; }
    /// <summary>Whether the rear defroster is on (<c>is_rear_defroster_on</c>).</summary>
    [JsonPropertyName("is_rear_defroster_on")] public bool? IsRearDefrosterOn { get; init; }
    /// <summary>Left temperature direction (<c>left_temp_direction</c>).</summary>
    [JsonPropertyName("left_temp_direction")] public int? LeftTempDirection { get; init; }
    /// <summary>Right temperature direction (<c>right_temp_direction</c>).</summary>
    [JsonPropertyName("right_temp_direction")] public int? RightTempDirection { get; init; }
    /// <summary>Whether remote heater control is enabled (<c>remote_heater_control_enabled</c>).</summary>
    [JsonPropertyName("remote_heater_control_enabled")] public bool? RemoteHeaterControlEnabled { get; init; }
    /// <summary>Left seat heater level (<c>seat_heater_left</c>).</summary>
    [JsonPropertyName("seat_heater_left")] public int? SeatHeaterLeft { get; init; }
    /// <summary>Right seat heater level (<c>seat_heater_right</c>).</summary>
    [JsonPropertyName("seat_heater_right")] public int? SeatHeaterRight { get; init; }
    /// <summary>Rear-left seat heater level (<c>seat_heater_rear_left</c>).</summary>
    [JsonPropertyName("seat_heater_rear_left")] public int? SeatHeaterRearLeft { get; init; }
    /// <summary>Rear-center seat heater level (<c>seat_heater_rear_center</c>).</summary>
    [JsonPropertyName("seat_heater_rear_center")] public int? SeatHeaterRearCenter { get; init; }
    /// <summary>Rear-right seat heater level (<c>seat_heater_rear_right</c>).</summary>
    [JsonPropertyName("seat_heater_rear_right")] public int? SeatHeaterRearRight { get; init; }
    /// <summary>Whether the side mirror heaters are on (<c>side_mirror_heaters</c>).</summary>
    [JsonPropertyName("side_mirror_heaters")] public bool? SideMirrorHeaters { get; init; }
    /// <summary>Steering wheel heater level (<c>steering_wheel_heat_level</c>).</summary>
    [JsonPropertyName("steering_wheel_heat_level")] public int? SteeringWheelHeatLevel { get; init; }
    /// <summary>Whether the steering wheel heater is on (<c>steering_wheel_heater</c>).</summary>
    [JsonPropertyName("steering_wheel_heater")] public bool? SteeringWheelHeater { get; init; }
    /// <summary>Whether the wiper blade heater is on (<c>wiper_blade_heater</c>).</summary>
    [JsonPropertyName("wiper_blade_heater")] public bool? WiperBladeHeater { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Drive/location state. Location needs the <c>vehicle_location</c> scope and the <c>location_data</c> endpoint.</summary>
public sealed record DriveState
{
    /// <summary>Latitude, degrees; requires the location scope and location_data (<c>latitude</c>).</summary>
    [JsonPropertyName("latitude")] public double? Latitude { get; init; }
    /// <summary>Longitude, degrees; requires the location scope and location_data (<c>longitude</c>).</summary>
    [JsonPropertyName("longitude")] public double? Longitude { get; init; }
    /// <summary>Compass heading, degrees (<c>heading</c>).</summary>
    [JsonPropertyName("heading")] public int? Heading { get; init; }
    /// <summary>Speed, mph; null when parked (<c>speed</c>).</summary>
    [JsonPropertyName("speed")] public double? Speed { get; init; }
    /// <summary>Gear selector state such as P, D, R; null when parked (<c>shift_state</c>).</summary>
    [JsonPropertyName("shift_state")] public string? ShiftState { get; init; }
    /// <summary>Power, kW (negative while regenerating) (<c>power</c>).</summary>
    [JsonPropertyName("power")] public int? Power { get; init; }
    /// <summary>GPS fix time, Unix epoch seconds (<c>gps_as_of</c>).</summary>
    [JsonPropertyName("gps_as_of")] public long? GpsAsOf { get; init; }
    /// <summary>Native latitude, degrees (<c>native_latitude</c>).</summary>
    [JsonPropertyName("native_latitude")] public double? NativeLatitude { get; init; }
    /// <summary>Native longitude, degrees (<c>native_longitude</c>).</summary>
    [JsonPropertyName("native_longitude")] public double? NativeLongitude { get; init; }
    /// <summary>Whether native location is supported (0 or 1) (<c>native_location_supported</c>).</summary>
    [JsonPropertyName("native_location_supported")] public int? NativeLocationSupported { get; init; }
    /// <summary>Native location type such as wgs (<c>native_type</c>).</summary>
    [JsonPropertyName("native_type")] public string? NativeType { get; init; }
    /// <summary>Active route destination latitude, degrees (<c>active_route_latitude</c>).</summary>
    [JsonPropertyName("active_route_latitude")] public double? ActiveRouteLatitude { get; init; }
    /// <summary>Active route destination longitude, degrees (<c>active_route_longitude</c>).</summary>
    [JsonPropertyName("active_route_longitude")] public double? ActiveRouteLongitude { get; init; }
    /// <summary>Traffic delay on the active route, minutes (<c>active_route_traffic_minutes_delay</c>).</summary>
    [JsonPropertyName("active_route_traffic_minutes_delay")] public double? ActiveRouteTrafficMinutesDelay { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>General vehicle state (odometer, locks, doors, tires, software, media). Door/window values: 0 = closed.</summary>
public sealed record VehicleState
{
    /// <summary>Odometer, miles (<c>odometer</c>).</summary>
    [JsonPropertyName("odometer")] public double? Odometer { get; init; }
    /// <summary>Whether the vehicle is locked (<c>locked</c>).</summary>
    [JsonPropertyName("locked")] public bool? Locked { get; init; }
    /// <summary>Installed software version (<c>car_version</c>).</summary>
    [JsonPropertyName("car_version")] public string? CarVersion { get; init; }
    /// <summary>Whether Sentry Mode is active (<c>sentry_mode</c>).</summary>
    [JsonPropertyName("sentry_mode")] public bool? SentryMode { get; init; }
    /// <summary>Vehicle name (<c>vehicle_name</c>).</summary>
    [JsonPropertyName("vehicle_name")] public string? VehicleName { get; init; }
    /// <summary>Vehicle API version (<c>api_version</c>).</summary>
    [JsonPropertyName("api_version")] public int? ApiVersion { get; init; }
    /// <summary>Autopark state (<c>autopark_state_v3</c>).</summary>
    [JsonPropertyName("autopark_state_v3")] public string? AutoparkStateV3 { get; init; }
    /// <summary>Autopark style (<c>autopark_style</c>).</summary>
    [JsonPropertyName("autopark_style")] public string? AutoparkStyle { get; init; }
    /// <summary>Last autopark error (<c>last_autopark_error</c>).</summary>
    [JsonPropertyName("last_autopark_error")] public string? LastAutoparkError { get; init; }
    /// <summary>Whether calendar is supported (<c>calendar_supported</c>).</summary>
    [JsonPropertyName("calendar_supported")] public bool? CalendarSupported { get; init; }
    /// <summary>Whether parsed calendar is supported (<c>parsed_calendar_supported</c>).</summary>
    [JsonPropertyName("parsed_calendar_supported")] public bool? ParsedCalendarSupported { get; init; }
    /// <summary>Center display state (<c>center_display_state</c>).</summary>
    [JsonPropertyName("center_display_state")] public int? CenterDisplayState { get; init; }
    /// <summary>Whether a dashcam clip can be saved (<c>dashcam_clip_save_available</c>).</summary>
    [JsonPropertyName("dashcam_clip_save_available")] public bool? DashcamClipSaveAvailable { get; init; }
    /// <summary>Dashcam state (<c>dashcam_state</c>).</summary>
    [JsonPropertyName("dashcam_state")] public string? DashcamState { get; init; }
    /// <summary>Driver front door (0 closed) (<c>df</c>).</summary>
    [JsonPropertyName("df")] public int? Df { get; init; }
    /// <summary>Driver rear door (0 closed) (<c>dr</c>).</summary>
    [JsonPropertyName("dr")] public int? Dr { get; init; }
    /// <summary>Passenger front door (0 closed) (<c>pf</c>).</summary>
    [JsonPropertyName("pf")] public int? Pf { get; init; }
    /// <summary>Passenger rear door (0 closed) (<c>pr</c>).</summary>
    [JsonPropertyName("pr")] public int? Pr { get; init; }
    /// <summary>Front trunk (0 closed) (<c>ft</c>).</summary>
    [JsonPropertyName("ft")] public int? Ft { get; init; }
    /// <summary>Rear trunk (0 closed) (<c>rt</c>).</summary>
    [JsonPropertyName("rt")] public int? Rt { get; init; }
    /// <summary>Front driver window (0 closed) (<c>fd_window</c>).</summary>
    [JsonPropertyName("fd_window")] public int? FdWindow { get; init; }
    /// <summary>Front passenger window (0 closed) (<c>fp_window</c>).</summary>
    [JsonPropertyName("fp_window")] public int? FpWindow { get; init; }
    /// <summary>Rear driver window (0 closed) (<c>rd_window</c>).</summary>
    [JsonPropertyName("rd_window")] public int? RdWindow { get; init; }
    /// <summary>Rear passenger window (0 closed) (<c>rp_window</c>).</summary>
    [JsonPropertyName("rp_window")] public int? RpWindow { get; init; }
    /// <summary>Feature bitmask (<c>feature_bitmask</c>).</summary>
    [JsonPropertyName("feature_bitmask")] public string? FeatureBitmask { get; init; }
    /// <summary>HomeLink device count (<c>homelink_device_count</c>).</summary>
    [JsonPropertyName("homelink_device_count")] public int? HomelinkDeviceCount { get; init; }
    /// <summary>Whether a HomeLink device is nearby (<c>homelink_nearby</c>).</summary>
    [JsonPropertyName("homelink_nearby")] public bool? HomelinkNearby { get; init; }
    /// <summary>Whether a user is present in the vehicle (<c>is_user_present</c>).</summary>
    [JsonPropertyName("is_user_present")] public bool? IsUserPresent { get; init; }
    /// <summary>Media player state (<c>media_info</c>).</summary>
    [JsonPropertyName("media_info")] public MediaInfo? MediaInfo { get; init; }
    /// <summary>Media remote-control state (<c>media_state</c>).</summary>
    [JsonPropertyName("media_state")] public MediaState? MediaState { get; init; }
    /// <summary>Whether notifications are supported (<c>notifications_supported</c>).</summary>
    [JsonPropertyName("notifications_supported")] public bool? NotificationsSupported { get; init; }
    /// <summary>Whether remote start is active (<c>remote_start</c>).</summary>
    [JsonPropertyName("remote_start")] public bool? RemoteStart { get; init; }
    /// <summary>Whether remote start is enabled (<c>remote_start_enabled</c>).</summary>
    [JsonPropertyName("remote_start_enabled")] public bool? RemoteStartEnabled { get; init; }
    /// <summary>Whether remote start is supported (<c>remote_start_supported</c>).</summary>
    [JsonPropertyName("remote_start_supported")] public bool? RemoteStartSupported { get; init; }
    /// <summary>Santa mode (<c>santa_mode</c>).</summary>
    [JsonPropertyName("santa_mode")] public int? SantaMode { get; init; }
    /// <summary>Whether Sentry Mode is available (<c>sentry_mode_available</c>).</summary>
    [JsonPropertyName("sentry_mode_available")] public bool? SentryModeAvailable { get; init; }
    /// <summary>Whether service mode is on (<c>service_mode</c>).</summary>
    [JsonPropertyName("service_mode")] public bool? ServiceMode { get; init; }
    /// <summary>Whether service mode plus is on (<c>service_mode_plus</c>).</summary>
    [JsonPropertyName("service_mode_plus")] public bool? ServiceModePlus { get; init; }
    /// <summary>Whether Smart Summon is available (<c>smart_summon_available</c>).</summary>
    [JsonPropertyName("smart_summon_available")] public bool? SmartSummonAvailable { get; init; }
    /// <summary>Pending software update (<c>software_update</c>).</summary>
    [JsonPropertyName("software_update")] public SoftwareUpdate? SoftwareUpdate { get; init; }
    /// <summary>Speed limit mode (<c>speed_limit_mode</c>).</summary>
    [JsonPropertyName("speed_limit_mode")] public SpeedLimitMode? SpeedLimitMode { get; init; }
    /// <summary>Whether Summon standby is enabled (<c>summon_standby_mode_enabled</c>).</summary>
    [JsonPropertyName("summon_standby_mode_enabled")] public bool? SummonStandbyModeEnabled { get; init; }
    /// <summary>Hard tire-pressure warning, front left (<c>tpms_hard_warning_fl</c>).</summary>
    [JsonPropertyName("tpms_hard_warning_fl")] public bool? TpmsHardWarningFl { get; init; }
    /// <summary>Hard tire-pressure warning, front right (<c>tpms_hard_warning_fr</c>).</summary>
    [JsonPropertyName("tpms_hard_warning_fr")] public bool? TpmsHardWarningFr { get; init; }
    /// <summary>Hard tire-pressure warning, rear left (<c>tpms_hard_warning_rl</c>).</summary>
    [JsonPropertyName("tpms_hard_warning_rl")] public bool? TpmsHardWarningRl { get; init; }
    /// <summary>Hard tire-pressure warning, rear right (<c>tpms_hard_warning_rr</c>).</summary>
    [JsonPropertyName("tpms_hard_warning_rr")] public bool? TpmsHardWarningRr { get; init; }
    /// <summary>Soft tire-pressure warning, front left (<c>tpms_soft_warning_fl</c>).</summary>
    [JsonPropertyName("tpms_soft_warning_fl")] public bool? TpmsSoftWarningFl { get; init; }
    /// <summary>Soft tire-pressure warning, front right (<c>tpms_soft_warning_fr</c>).</summary>
    [JsonPropertyName("tpms_soft_warning_fr")] public bool? TpmsSoftWarningFr { get; init; }
    /// <summary>Soft tire-pressure warning, rear left (<c>tpms_soft_warning_rl</c>).</summary>
    [JsonPropertyName("tpms_soft_warning_rl")] public bool? TpmsSoftWarningRl { get; init; }
    /// <summary>Soft tire-pressure warning, rear right (<c>tpms_soft_warning_rr</c>).</summary>
    [JsonPropertyName("tpms_soft_warning_rr")] public bool? TpmsSoftWarningRr { get; init; }
    /// <summary>Tire pressure front left, bar (<c>tpms_pressure_fl</c>).</summary>
    [JsonPropertyName("tpms_pressure_fl")] public double? TpmsPressureFl { get; init; }
    /// <summary>Tire pressure front right, bar (<c>tpms_pressure_fr</c>).</summary>
    [JsonPropertyName("tpms_pressure_fr")] public double? TpmsPressureFr { get; init; }
    /// <summary>Tire pressure rear left, bar (<c>tpms_pressure_rl</c>).</summary>
    [JsonPropertyName("tpms_pressure_rl")] public double? TpmsPressureRl { get; init; }
    /// <summary>Tire pressure rear right, bar (<c>tpms_pressure_rr</c>).</summary>
    [JsonPropertyName("tpms_pressure_rr")] public double? TpmsPressureRr { get; init; }
    /// <summary>Last seen pressure time front left, Unix epoch seconds (<c>tpms_last_seen_pressure_time_fl</c>).</summary>
    [JsonPropertyName("tpms_last_seen_pressure_time_fl")] public long? TpmsLastSeenPressureTimeFl { get; init; }
    /// <summary>Last seen pressure time front right, Unix epoch seconds (<c>tpms_last_seen_pressure_time_fr</c>).</summary>
    [JsonPropertyName("tpms_last_seen_pressure_time_fr")] public long? TpmsLastSeenPressureTimeFr { get; init; }
    /// <summary>Last seen pressure time rear left, Unix epoch seconds (<c>tpms_last_seen_pressure_time_rl</c>).</summary>
    [JsonPropertyName("tpms_last_seen_pressure_time_rl")] public long? TpmsLastSeenPressureTimeRl { get; init; }
    /// <summary>Last seen pressure time rear right, Unix epoch seconds (<c>tpms_last_seen_pressure_time_rr</c>).</summary>
    [JsonPropertyName("tpms_last_seen_pressure_time_rr")] public long? TpmsLastSeenPressureTimeRr { get; init; }
    /// <summary>Recommended cold pressure front, bar (<c>tpms_rcp_front_value</c>).</summary>
    [JsonPropertyName("tpms_rcp_front_value")] public double? TpmsRcpFrontValue { get; init; }
    /// <summary>Recommended cold pressure rear, bar (<c>tpms_rcp_rear_value</c>).</summary>
    [JsonPropertyName("tpms_rcp_rear_value")] public double? TpmsRcpRearValue { get; init; }
    /// <summary>Whether Valet Mode is on (<c>valet_mode</c>).</summary>
    [JsonPropertyName("valet_mode")] public bool? ValetMode { get; init; }
    /// <summary>Whether a valet PIN is needed (<c>valet_pin_needed</c>).</summary>
    [JsonPropertyName("valet_pin_needed")] public bool? ValetPinNeeded { get; init; }
    /// <summary>Self-test progress (<c>vehicle_self_test_progress</c>).</summary>
    [JsonPropertyName("vehicle_self_test_progress")] public int? VehicleSelfTestProgress { get; init; }
    /// <summary>Whether a self-test was requested (<c>vehicle_self_test_requested</c>).</summary>
    [JsonPropertyName("vehicle_self_test_requested")] public bool? VehicleSelfTestRequested { get; init; }
    /// <summary>Whether a webcam is available (<c>webcam_available</c>).</summary>
    [JsonPropertyName("webcam_available")] public bool? WebcamAvailable { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>UI/unit preferences of the vehicle display.</summary>
public sealed record GuiSettings
{
    /// <summary>Distance unit shown in the vehicle UI, e.g. <c>mi/hr</c> (<c>gui_distance_units</c>).</summary>
    [JsonPropertyName("gui_distance_units")] public string? GuiDistanceUnits { get; init; }
    /// <summary>Temperature unit shown in the vehicle UI, C or F (<c>gui_temperature_units</c>).</summary>
    [JsonPropertyName("gui_temperature_units")] public string? GuiTemperatureUnits { get; init; }
    /// <summary>Charge rate unit (<c>gui_charge_rate_units</c>).</summary>
    [JsonPropertyName("gui_charge_rate_units")] public string? GuiChargeRateUnits { get; init; }
    /// <summary>Whether 24-hour time is shown (<c>gui_24_hour_time</c>).</summary>
    [JsonPropertyName("gui_24_hour_time")] public bool? Gui24HourTime { get; init; }
    /// <summary>Range display mode such as Rated (<c>gui_range_display</c>).</summary>
    [JsonPropertyName("gui_range_display")] public string? GuiRangeDisplay { get; init; }
    /// <summary>Tire pressure unit such as Psi (<c>gui_tirepressure_units</c>).</summary>
    [JsonPropertyName("gui_tirepressure_units")] public string? GuiTirepressureUnits { get; init; }
    /// <summary>Whether range units are shown (<c>show_range_units</c>).</summary>
    [JsonPropertyName("show_range_units")] public bool? ShowRangeUnits { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Vehicle configuration (type, colors, hardware options).</summary>
public sealed record VehicleConfig
{
    /// <summary>Car type such as model3 (<c>car_type</c>).</summary>
    [JsonPropertyName("car_type")] public string? CarType { get; init; }
    /// <summary>Special car type (<c>car_special_type</c>).</summary>
    [JsonPropertyName("car_special_type")] public string? CarSpecialType { get; init; }
    /// <summary>Trim badging (<c>trim_badging</c>).</summary>
    [JsonPropertyName("trim_badging")] public string? TrimBadging { get; init; }
    /// <summary>Exterior color (<c>exterior_color</c>).</summary>
    [JsonPropertyName("exterior_color")] public string? ExteriorColor { get; init; }
    /// <summary>Exterior trim (<c>exterior_trim</c>).</summary>
    [JsonPropertyName("exterior_trim")] public string? ExteriorTrim { get; init; }
    /// <summary>Exterior trim override (<c>exterior_trim_override</c>).</summary>
    [JsonPropertyName("exterior_trim_override")] public string? ExteriorTrimOverride { get; init; }
    /// <summary>Paint color override (<c>paint_color_override</c>).</summary>
    [JsonPropertyName("paint_color_override")] public string? PaintColorOverride { get; init; }
    /// <summary>Roof color (<c>roof_color</c>).</summary>
    [JsonPropertyName("roof_color")] public string? RoofColor { get; init; }
    /// <summary>Interior trim type (<c>interior_trim_type</c>).</summary>
    [JsonPropertyName("interior_trim_type")] public string? InteriorTrimType { get; init; }
    /// <summary>Wheel type (<c>wheel_type</c>).</summary>
    [JsonPropertyName("wheel_type")] public string? WheelType { get; init; }
    /// <summary>Spoiler type (<c>spoiler_type</c>).</summary>
    [JsonPropertyName("spoiler_type")] public string? SpoilerType { get; init; }
    /// <summary>Headlamp type (<c>headlamp_type</c>).</summary>
    [JsonPropertyName("headlamp_type")] public string? HeadlampType { get; init; }
    /// <summary>Charge port type such as CCS, NACS (<c>charge_port_type</c>).</summary>
    [JsonPropertyName("charge_port_type")] public string? ChargePortType { get; init; }
    /// <summary>Driver assist hardware (<c>driver_assist</c>).</summary>
    [JsonPropertyName("driver_assist")] public string? DriverAssist { get; init; }
    /// <summary>Efficiency package (<c>efficiency_package</c>).</summary>
    [JsonPropertyName("efficiency_package")] public string? EfficiencyPackage { get; init; }
    /// <summary>Performance package (<c>performance_package</c>).</summary>
    [JsonPropertyName("performance_package")] public string? PerformancePackage { get; init; }
    /// <summary>Rear drive unit (<c>rear_drive_unit</c>).</summary>
    [JsonPropertyName("rear_drive_unit")] public string? RearDriveUnit { get; init; }
    /// <summary>Third row seats (<c>third_row_seats</c>).</summary>
    [JsonPropertyName("third_row_seats")] public string? ThirdRowSeats { get; init; }
    /// <summary>Aux park lamps (<c>aux_park_lamps</c>).</summary>
    [JsonPropertyName("aux_park_lamps")] public string? AuxParkLamps { get; init; }
    /// <summary>Badge version (<c>badge_version</c>).</summary>
    [JsonPropertyName("badge_version")] public int? BadgeVersion { get; init; }
    /// <summary>Key version (<c>key_version</c>).</summary>
    [JsonPropertyName("key_version")] public int? KeyVersion { get; init; }
    /// <summary>Rear seat heaters (<c>rear_seat_heaters</c>).</summary>
    [JsonPropertyName("rear_seat_heaters")] public int? RearSeatHeaters { get; init; }
    /// <summary>Rear seat type (<c>rear_seat_type</c>).</summary>
    [JsonPropertyName("rear_seat_type")] public int? RearSeatType { get; init; }
    /// <summary>Seat type (JSON type varies) (<c>seat_type</c>).</summary>
    [JsonPropertyName("seat_type")] public JsonElement? SeatType { get; init; }
    /// <summary>Sunroof (JSON type varies) (<c>sun_roof_installed</c>).</summary>
    [JsonPropertyName("sun_roof_installed")] public JsonElement? SunRoofInstalled { get; init; }
    /// <summary>Whether navigation requests are accepted (<c>can_accept_navigation_requests</c>).</summary>
    [JsonPropertyName("can_accept_navigation_requests")] public bool? CanAcceptNavigationRequests { get; init; }
    /// <summary>Whether trunks can be actuated (<c>can_actuate_trunks</c>).</summary>
    [JsonPropertyName("can_actuate_trunks")] public bool? CanActuateTrunks { get; init; }
    /// <summary>Whether user-set overheat protection temperature is supported (<c>cop_user_set_temp_supported</c>).</summary>
    [JsonPropertyName("cop_user_set_temp_supported")] public bool? CopUserSetTempSupported { get; init; }
    /// <summary>Whether saving dashcam clips is supported (<c>dashcam_clip_save_supported</c>).</summary>
    [JsonPropertyName("dashcam_clip_save_supported")] public bool? DashcamClipSaveSupported { get; init; }
    /// <summary>Whether default charge is max (<c>default_charge_to_max</c>).</summary>
    [JsonPropertyName("default_charge_to_max")] public bool? DefaultChargeToMax { get; init; }
    /// <summary>Whether ECE restrictions apply (<c>ece_restrictions</c>).</summary>
    [JsonPropertyName("ece_restrictions")] public bool? EceRestrictions { get; init; }
    /// <summary>Whether this is an EU vehicle (<c>eu_vehicle</c>).</summary>
    [JsonPropertyName("eu_vehicle")] public bool? EuVehicle { get; init; }
    /// <summary>Has air suspension (<c>has_air_suspension</c>).</summary>
    [JsonPropertyName("has_air_suspension")] public bool? HasAirSuspension { get; init; }
    /// <summary>Has Ludicrous mode (<c>has_ludicrous_mode</c>).</summary>
    [JsonPropertyName("has_ludicrous_mode")] public bool? HasLudicrousMode { get; init; }
    /// <summary>Has seat cooling (<c>has_seat_cooling</c>).</summary>
    [JsonPropertyName("has_seat_cooling")] public bool? HasSeatCooling { get; init; }
    /// <summary>Has a motorized charge port (<c>motorized_charge_port</c>).</summary>
    [JsonPropertyName("motorized_charge_port")] public bool? MotorizedChargePort { get; init; }
    /// <summary>Power liftgate (<c>plg</c>).</summary>
    [JsonPropertyName("plg")] public bool? Plg { get; init; }
    /// <summary>Pedestrian warning system (<c>pws</c>).</summary>
    [JsonPropertyName("pws")] public bool? Pws { get; init; }
    /// <summary>Right-hand drive (<c>rhd</c>).</summary>
    [JsonPropertyName("rhd")] public bool? Rhd { get; init; }
    /// <summary>Supports QR pairing (<c>supports_qr_pairing</c>).</summary>
    [JsonPropertyName("supports_qr_pairing")] public bool? SupportsQrPairing { get; init; }
    /// <summary>Uses range badging (<c>use_range_badging</c>).</summary>
    [JsonPropertyName("use_range_badging")] public bool? UseRangeBadging { get; init; }
    /// <summary>UTC offset, seconds (<c>utc_offset</c>).</summary>
    [JsonPropertyName("utc_offset")] public int? UtcOffset { get; init; }
    /// <summary>Webcam selfie supported (<c>webcam_selfie_supported</c>).</summary>
    [JsonPropertyName("webcam_selfie_supported")] public bool? WebcamSelfieSupported { get; init; }
    /// <summary>Webcam supported (<c>webcam_supported</c>).</summary>
    [JsonPropertyName("webcam_supported")] public bool? WebcamSupported { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Location block returned for the <c>location_data</c> endpoint. Shape is UNVERIFIED (not in Tesla's docs example); unmodeled fields land in <c>AdditionalData</c>.</summary>
public sealed record LocationData
{
    /// <summary>Latitude, degrees (<c>latitude</c>).</summary>
    [JsonPropertyName("latitude")] public double? Latitude { get; init; }
    /// <summary>Longitude, degrees (<c>longitude</c>).</summary>
    [JsonPropertyName("longitude")] public double? Longitude { get; init; }
    /// <summary>Compass heading, degrees (<c>heading</c>).</summary>
    [JsonPropertyName("heading")] public int? Heading { get; init; }
    /// <summary>Speed, mph (<c>speed</c>).</summary>
    [JsonPropertyName("speed")] public double? Speed { get; init; }
    /// <summary>GPS fix time, Unix epoch seconds (<c>gps_as_of</c>).</summary>
    [JsonPropertyName("gps_as_of")] public long? GpsAsOf { get; init; }
    /// <summary>Native latitude, degrees (<c>native_latitude</c>).</summary>
    [JsonPropertyName("native_latitude")] public double? NativeLatitude { get; init; }
    /// <summary>Native longitude, degrees (<c>native_longitude</c>).</summary>
    [JsonPropertyName("native_longitude")] public double? NativeLongitude { get; init; }
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Closures block returned for the <c>closures_state</c> endpoint. Shape is UNVERIFIED (not in Tesla's docs example); fields land in <c>AdditionalData</c>.</summary>
public sealed record ClosuresState
{
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Charge schedule block returned for the <c>charge_schedule_data</c> endpoint. Shape is UNVERIFIED; fields land in <c>AdditionalData</c>.</summary>
public sealed record ChargeScheduleData
{
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Preconditioning schedule block returned for the <c>preconditioning_schedule_data</c> endpoint. Shape is UNVERIFIED; fields land in <c>AdditionalData</c>.</summary>
public sealed record PreconditioningScheduleData
{
    /// <summary>Time of the reading, Unix epoch milliseconds (<c>timestamp</c>).</summary>
    [JsonPropertyName("timestamp")] public long? Timestamp { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Media player state.</summary>
public sealed record MediaInfo
{
    /// <summary>Bluetooth source name (<c>a2dp_source_name</c>).</summary>
    [JsonPropertyName("a2dp_source_name")] public string? A2dpSourceName { get; init; }
    /// <summary>Volume (<c>audio_volume</c>).</summary>
    [JsonPropertyName("audio_volume")] public double? AudioVolume { get; init; }
    /// <summary>Volume step (<c>audio_volume_increment</c>).</summary>
    [JsonPropertyName("audio_volume_increment")] public double? AudioVolumeIncrement { get; init; }
    /// <summary>Maximum volume (<c>audio_volume_max</c>).</summary>
    [JsonPropertyName("audio_volume_max")] public double? AudioVolumeMax { get; init; }
    /// <summary>Playback status (<c>media_playback_status</c>).</summary>
    [JsonPropertyName("media_playback_status")] public string? MediaPlaybackStatus { get; init; }
    /// <summary>Album (<c>now_playing_album</c>).</summary>
    [JsonPropertyName("now_playing_album")] public string? NowPlayingAlbum { get; init; }
    /// <summary>Artist (<c>now_playing_artist</c>).</summary>
    [JsonPropertyName("now_playing_artist")] public string? NowPlayingArtist { get; init; }
    /// <summary>Track duration, ms (<c>now_playing_duration</c>).</summary>
    [JsonPropertyName("now_playing_duration")] public int? NowPlayingDuration { get; init; }
    /// <summary>Elapsed time, ms (<c>now_playing_elapsed</c>).</summary>
    [JsonPropertyName("now_playing_elapsed")] public int? NowPlayingElapsed { get; init; }
    /// <summary>Source (<c>now_playing_source</c>).</summary>
    [JsonPropertyName("now_playing_source")] public string? NowPlayingSource { get; init; }
    /// <summary>Station (<c>now_playing_station</c>).</summary>
    [JsonPropertyName("now_playing_station")] public string? NowPlayingStation { get; init; }
    /// <summary>Title (<c>now_playing_title</c>).</summary>
    [JsonPropertyName("now_playing_title")] public string? NowPlayingTitle { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Media remote-control state.</summary>
public sealed record MediaState
{
    /// <summary>Whether remote control is enabled (<c>remote_control_enabled</c>).</summary>
    [JsonPropertyName("remote_control_enabled")] public bool? RemoteControlEnabled { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Pending software update.</summary>
public sealed record SoftwareUpdate
{
    /// <summary>Download progress, percent (<c>download_perc</c>).</summary>
    [JsonPropertyName("download_perc")] public int? DownloadPerc { get; init; }
    /// <summary>Expected install duration, seconds (<c>expected_duration_sec</c>).</summary>
    [JsonPropertyName("expected_duration_sec")] public int? ExpectedDurationSec { get; init; }
    /// <summary>Install progress, percent (<c>install_perc</c>).</summary>
    [JsonPropertyName("install_perc")] public int? InstallPerc { get; init; }
    /// <summary>Update status (<c>status</c>).</summary>
    [JsonPropertyName("status")] public string? Status { get; init; }
    /// <summary>Update version (<c>version</c>).</summary>
    [JsonPropertyName("version")] public string? Version { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}

/// <summary>Speed limit mode.</summary>
public sealed record SpeedLimitMode
{
    /// <summary>Whether active (<c>active</c>).</summary>
    [JsonPropertyName("active")] public bool? Active { get; init; }
    /// <summary>Current limit, mph (<c>current_limit_mph</c>).</summary>
    [JsonPropertyName("current_limit_mph")] public double? CurrentLimitMph { get; init; }
    /// <summary>Maximum limit, mph (<c>max_limit_mph</c>).</summary>
    [JsonPropertyName("max_limit_mph")] public double? MaxLimitMph { get; init; }
    /// <summary>Minimum limit, mph (<c>min_limit_mph</c>).</summary>
    [JsonPropertyName("min_limit_mph")] public double? MinLimitMph { get; init; }
    /// <summary>Whether a PIN is set (<c>pin_code_set</c>).</summary>
    [JsonPropertyName("pin_code_set")] public bool? PinCodeSet { get; init; }
    /// <summary>Fields in the response that are not modeled by this type, keyed by JSON property name.</summary>
    [JsonExtensionData] public IDictionary<string, JsonElement>? AdditionalData { get; init; }
}
