namespace CoilPro.Commands;

/// <summary>
/// Signed vehicle commands over the Fleet API (Tesla Vehicle Command Protocol). Session handshake, signing,
/// response decryption and retries are handled internally.
/// </summary>
/// <remarks>
/// Every method takes the vehicle's 17-character VIN (not the numeric Fleet API id). A vehicle refusal is
/// returned as <see cref="CommandResult"/> with <c>Success = false</c>; delivery/authentication failures throw
/// <see cref="TeslaCommandException"/> (or its subtypes <see cref="TeslaKeyNotPairedException"/> and
/// <see cref="TeslaVehicleAsleepException"/>). Commands to the same VIN are serialized.
/// </remarks>
public interface ICoilProCommands
{
    /// <summary>Locks the vehicle (security domain).</summary>
    Task<CommandResult> LockAsync(string vin, CancellationToken ct = default);

    /// <summary>Unlocks the vehicle (security domain).</summary>
    Task<CommandResult> UnlockAsync(string vin, CancellationToken ct = default);

    /// <summary>Honks the horn.</summary>
    Task<CommandResult> HonkHornAsync(string vin, CancellationToken ct = default);

    /// <summary>Flashes the lights.</summary>
    Task<CommandResult> FlashLightsAsync(string vin, CancellationToken ct = default);

    /// <summary>Turns climate control (HVAC auto) on.</summary>
    Task<CommandResult> StartClimateAsync(string vin, CancellationToken ct = default);

    /// <summary>Turns climate control (HVAC auto) off.</summary>
    Task<CommandResult> StopClimateAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the driver and passenger cabin temperature in °C.</summary>
    Task<CommandResult> SetTemperaturesAsync(string vin, float driverCelsius, float passengerCelsius, CancellationToken ct = default);

    /// <summary>Starts charging.</summary>
    Task<CommandResult> StartChargingAsync(string vin, CancellationToken ct = default);

    /// <summary>Stops charging.</summary>
    Task<CommandResult> StopChargingAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the charge limit in percent (50–100).</summary>
    Task<CommandResult> SetChargeLimitAsync(string vin, int percent, CancellationToken ct = default);

    /// <summary>Opens the charge port door.</summary>
    Task<CommandResult> OpenChargePortAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the charging current in amperes.</summary>
    Task<CommandResult> SetChargingAmpsAsync(string vin, int amps, CancellationToken ct = default);

    /// <summary>Enables keyless driving (<c>remote_start_drive</c>, security domain).</summary>
    Task<CommandResult> RemoteStartDriveAsync(string vin, CancellationToken ct = default);

    /// <summary>Opens the front trunk (<c>actuate_trunk</c> front).</summary>
    Task<CommandResult> OpenFrunkAsync(string vin, CancellationToken ct = default);

    /// <summary>Opens or closes the rear trunk — a toggle (<c>actuate_trunk</c> rear).</summary>
    Task<CommandResult> ActuateRearTrunkAsync(string vin, CancellationToken ct = default);

    /// <summary>Closes a powered rear trunk (vehicles with powered liftgate only).</summary>
    Task<CommandResult> CloseRearTrunkAsync(string vin, CancellationToken ct = default);

    /// <summary>Model X: closes the falcon-wing doors and locks.</summary>
    Task<CommandResult> AutoSecureModelXAsync(string vin, CancellationToken ct = default);

    /// <summary>Cybertruck: opens the tonneau cover.</summary>
    Task<CommandResult> OpenTonneauAsync(string vin, CancellationToken ct = default);

    /// <summary>Cybertruck: closes the tonneau cover.</summary>
    Task<CommandResult> CloseTonneauAsync(string vin, CancellationToken ct = default);

    /// <summary>Cybertruck: stops the tonneau cover.</summary>
    Task<CommandResult> StopTonneauAsync(string vin, CancellationToken ct = default);

    /// <summary>Closes the charge port door.</summary>
    Task<CommandResult> CloseChargePortAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the charge limit to the standard range (<c>charge_standard</c>).</summary>
    Task<CommandResult> ChargeStandardRangeAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the charge limit to maximum range (<c>charge_max_range</c>).</summary>
    Task<CommandResult> ChargeMaxRangeAsync(string vin, CancellationToken ct = default);

    /// <summary>Legacy scheduled charging start (<c>set_scheduled_charging</c>; prefer <see cref="AddChargeScheduleAsync"/> from firmware 2024.26).</summary>
    Task<CommandResult> SetScheduledChargingAsync(string vin, bool enabled, TimeOnly time, CancellationToken ct = default);

    /// <summary>Legacy scheduled departure (<c>set_scheduled_departure</c>).</summary>
    Task<CommandResult> SetScheduledDepartureAsync(string vin, ScheduledDeparture departure, CancellationToken ct = default);

    /// <summary>Disables the legacy scheduled departure.</summary>
    Task<CommandResult> ClearScheduledDepartureAsync(string vin, CancellationToken ct = default);

    /// <summary>Adds or replaces a charge schedule (<c>add_charge_schedule</c>).</summary>
    Task<CommandResult> AddChargeScheduleAsync(string vin, ChargeSchedule schedule, CancellationToken ct = default);

    /// <summary>Removes a charge schedule by id.</summary>
    Task<CommandResult> RemoveChargeScheduleAsync(string vin, ulong scheduleId, CancellationToken ct = default);

    /// <summary>Removes all charge schedules at the given kinds of locations.</summary>
    Task<CommandResult> RemoveChargeSchedulesAsync(string vin, ScheduleLocations locations, CancellationToken ct = default);

    /// <summary>Adds or replaces a preconditioning schedule (<c>add_precondition_schedule</c>).</summary>
    Task<CommandResult> AddPreconditionScheduleAsync(string vin, PreconditionSchedule schedule, CancellationToken ct = default);

    /// <summary>Removes a preconditioning schedule by id.</summary>
    Task<CommandResult> RemovePreconditionScheduleAsync(string vin, ulong scheduleId, CancellationToken ct = default);

    /// <summary>Removes all preconditioning schedules at the given kinds of locations.</summary>
    Task<CommandResult> RemovePreconditionSchedulesAsync(string vin, ScheduleLocations locations, CancellationToken ct = default);

    /// <summary>Turns low power mode on or off.</summary>
    Task<CommandResult> SetLowPowerModeAsync(string vin, bool enabled, CancellationToken ct = default);

    /// <summary>Keeps accessory power on (or off) after exit.</summary>
    Task<CommandResult> SetKeepAccessoryPowerModeAsync(string vin, bool enabled, CancellationToken ct = default);

    /// <summary>Turns maximum defrost preconditioning on or off (<c>set_preconditioning_max</c>).</summary>
    Task<CommandResult> SetPreconditioningMaxAsync(string vin, bool on, bool manualOverride = false, CancellationToken ct = default);

    /// <summary>Turns Bioweapon Defense Mode on or off.</summary>
    Task<CommandResult> SetBioweaponModeAsync(string vin, bool on, bool manualOverride = false, CancellationToken ct = default);

    /// <summary>Turns cabin overheat protection on or off.</summary>
    Task<CommandResult> SetCabinOverheatProtectionAsync(string vin, bool on, bool fanOnly = false, CancellationToken ct = default);

    /// <summary>Sets the cabin overheat protection activation temperature (does not turn it on).</summary>
    Task<CommandResult> SetCabinOverheatProtectionTemperatureAsync(string vin, CabinOverheatTemperature temperature, CancellationToken ct = default);

    /// <summary>Sets climate keeper (keep, dog, camp) or turns it off.</summary>
    Task<CommandResult> SetClimateKeeperModeAsync(string vin, ClimateKeeperMode mode, bool manualOverride = false, CancellationToken ct = default);

    /// <summary>Sets a seat heater level (requires climate on).</summary>
    Task<CommandResult> SetSeatHeaterAsync(string vin, SeatPosition seat, SeatHeaterLevel level, CancellationToken ct = default);

    /// <summary>Sets a front seat cooler (ventilation) level.</summary>
    Task<CommandResult> SetSeatCoolerAsync(string vin, FrontSeat seat, SeatCoolerLevel level, CancellationToken ct = default);

    /// <summary>Turns automatic seat climate on or off for a front seat.</summary>
    Task<CommandResult> SetAutoSeatClimateAsync(string vin, FrontSeat seat, bool on, CancellationToken ct = default);

    /// <summary>Turns the steering wheel heater on or off.</summary>
    Task<CommandResult> SetSteeringWheelHeaterAsync(string vin, bool on, CancellationToken ct = default);

    /// <summary>Vents all windows (vehicle must be parked).</summary>
    Task<CommandResult> VentWindowsAsync(string vin, CancellationToken ct = default);

    /// <summary>Closes all windows.</summary>
    Task<CommandResult> CloseWindowsAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the media volume (0–10).</summary>
    Task<CommandResult> SetVolumeAsync(string vin, float volume, CancellationToken ct = default);

    /// <summary>Raises the media volume one step.</summary>
    Task<CommandResult> VolumeUpAsync(string vin, CancellationToken ct = default);

    /// <summary>Lowers the media volume one step.</summary>
    Task<CommandResult> VolumeDownAsync(string vin, CancellationToken ct = default);

    /// <summary>Skips to the next track.</summary>
    Task<CommandResult> NextTrackAsync(string vin, CancellationToken ct = default);

    /// <summary>Goes to the previous track.</summary>
    Task<CommandResult> PreviousTrackAsync(string vin, CancellationToken ct = default);

    /// <summary>Skips to the next favorite.</summary>
    Task<CommandResult> NextFavoriteAsync(string vin, CancellationToken ct = default);

    /// <summary>Goes to the previous favorite.</summary>
    Task<CommandResult> PreviousFavoriteAsync(string vin, CancellationToken ct = default);

    /// <summary>Toggles media play/pause.</summary>
    Task<CommandResult> TogglePlaybackAsync(string vin, CancellationToken ct = default);

    /// <summary>Turns Sentry Mode on or off.</summary>
    Task<CommandResult> SetSentryModeAsync(string vin, bool on, CancellationToken ct = default);

    /// <summary>Turns valet mode on with a 4-digit PIN.</summary>
    Task<CommandResult> EnableValetModeAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Turns valet mode off.</summary>
    Task<CommandResult> DisableValetModeAsync(string vin, CancellationToken ct = default);

    /// <summary>Clears the valet PIN (valet mode must be off).</summary>
    Task<CommandResult> ResetValetPinAsync(string vin, CancellationToken ct = default);

    /// <summary>Turns PIN to Drive on (with a 4-digit PIN) or off.</summary>
    Task<CommandResult> SetPinToDriveAsync(string vin, bool on, string? pin = null, CancellationToken ct = default);

    /// <summary>Owner/fleet manager: clears PIN to Drive (firmware 2023.44+).</summary>
    Task<CommandResult> ClearPinToDriveAdminAsync(string vin, CancellationToken ct = default);

    /// <summary>Resets the PIN to Drive PIN.</summary>
    Task<CommandResult> ResetPinToDriveAsync(string vin, CancellationToken ct = default);

    /// <summary>Activates Speed Limit Mode with its 4-digit PIN.</summary>
    Task<CommandResult> ActivateSpeedLimitAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Deactivates Speed Limit Mode with its PIN.</summary>
    Task<CommandResult> DeactivateSpeedLimitAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Clears the Speed Limit Mode PIN.</summary>
    Task<CommandResult> ClearSpeedLimitPinAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Owner/fleet manager: clears the Speed Limit Mode PIN (firmware 2023.38+).</summary>
    Task<CommandResult> ClearSpeedLimitPinAdminAsync(string vin, CancellationToken ct = default);

    /// <summary>Sets the Speed Limit Mode limit in mph.</summary>
    Task<CommandResult> SetSpeedLimitAsync(string vin, double limitMph, CancellationToken ct = default);

    /// <summary>Activates parental controls with a 4-digit PIN.</summary>
    Task<CommandResult> ActivateParentalControlsAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Deactivates parental controls with its PIN.</summary>
    Task<CommandResult> DeactivateParentalControlsAsync(string vin, string pin, CancellationToken ct = default);

    /// <summary>Enables or disables a parental controls policy (while controls are inactive).</summary>
    Task<CommandResult> SetParentalControlsSettingAsync(string vin, ParentalControlsSetting setting, bool enable, CancellationToken ct = default);

    /// <summary>Sets the parental controls speed limit in mph.</summary>
    Task<CommandResult> SetParentalControlsSpeedLimitAsync(string vin, double limitMph, CancellationToken ct = default);

    /// <summary>Owner/fleet manager: clears the parental controls PIN.</summary>
    Task<CommandResult> ClearParentalControlsPinAdminAsync(string vin, CancellationToken ct = default);

    /// <summary>Turns guest mode on or off.</summary>
    Task<CommandResult> SetGuestModeAsync(string vin, bool on, CancellationToken ct = default);

    /// <summary>Erases guest user data (parked, in guest mode).</summary>
    Task<CommandResult> EraseUserDataAsync(string vin, CancellationToken ct = default);

    /// <summary>Schedules an available software update to install after <paramref name="delay"/>.</summary>
    Task<CommandResult> ScheduleSoftwareUpdateAsync(string vin, TimeSpan delay, CancellationToken ct = default);

    /// <summary>Cancels a scheduled software update.</summary>
    Task<CommandResult> CancelSoftwareUpdateAsync(string vin, CancellationToken ct = default);

    /// <summary>Triggers HomeLink at the vehicle's location.</summary>
    Task<CommandResult> TriggerHomelinkAsync(string vin, double latitude, double longitude, CancellationToken ct = default);

    /// <summary>Renames the vehicle.</summary>
    Task<CommandResult> SetVehicleNameAsync(string vin, string name, CancellationToken ct = default);

    /// <summary>Signed no-op: checks that the key is paired and infotainment answers.</summary>
    Task<CommandResult> PingAsync(string vin, CancellationToken ct = default);
}
