// Port of teslamotors/vehicle-command pkg/vehicle/{infotainment,security}.go (Apache-2.0): media, sentry, valet,
// PIN to Drive, speed limit, parental controls, guest mode, software update, HomeLink, vehicle name, ping.
using Cs = global::CarServer;

namespace CoilPro.Commands.Internal.Commands;

internal static partial class CommandPayloads
{
    // ---- Media -------------------------------------------------------------------------------------------------

    /// <summary>Sets the absolute media volume (0–10, like Go).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Not a finite number in [0, 10].</exception>
    public static VehicleCommand SetVolume(float volume)
    {
        if (!float.IsFinite(volume) || volume < 0f || volume > 10f)
            throw new ArgumentOutOfRangeException(nameof(volume), volume, "Volume must be between 0 and 10.");
        return Infotainment("set_volume", new Cs.VehicleAction { MediaUpdateVolume = new Cs.MediaUpdateVolume { VolumeAbsoluteFloat = volume } });
    }

    /// <summary>Raises the volume one step.</summary>
    public static VehicleCommand VolumeUp() =>
        Infotainment("volume_up", new Cs.VehicleAction { MediaUpdateVolume = new Cs.MediaUpdateVolume { VolumeDelta = 1 } });

    /// <summary>Lowers the volume one step.</summary>
    public static VehicleCommand VolumeDown() =>
        Infotainment("volume_down", new Cs.VehicleAction { MediaUpdateVolume = new Cs.MediaUpdateVolume { VolumeDelta = -1 } });

    /// <summary>Skips to the next track.</summary>
    public static VehicleCommand NextTrack() =>
        Infotainment("media_next_track", new Cs.VehicleAction { MediaNextTrack = new Cs.MediaNextTrack() });

    /// <summary>Returns to the previous track.</summary>
    public static VehicleCommand PreviousTrack() =>
        Infotainment("media_previous_track", new Cs.VehicleAction { MediaPreviousTrack = new Cs.MediaPreviousTrack() });

    /// <summary>Skips to the next favorite.</summary>
    public static VehicleCommand NextFavorite() =>
        Infotainment("media_next_favorite", new Cs.VehicleAction { MediaNextFavorite = new Cs.MediaNextFavorite() });

    /// <summary>Returns to the previous favorite.</summary>
    public static VehicleCommand PreviousFavorite() =>
        Infotainment("media_previous_favorite", new Cs.VehicleAction { MediaPreviousFavorite = new Cs.MediaPreviousFavorite() });

    /// <summary>Toggles media playback.</summary>
    public static VehicleCommand TogglePlayback() =>
        Infotainment("media_toggle_playback", new Cs.VehicleAction { MediaPlayAction = new Cs.MediaPlayAction() });

    // ---- Sentry, valet, PIN to Drive ---------------------------------------------------------------------------

    /// <summary>Turns Sentry Mode on or off.</summary>
    public static VehicleCommand SetSentryMode(bool on) =>
        Infotainment("set_sentry_mode", new Cs.VehicleAction { VehicleControlSetSentryModeAction = new Cs.VehicleControlSetSentryModeAction { On = on } });

    /// <summary>Turns Valet Mode on with a 4-digit PIN.</summary>
    /// <exception cref="ArgumentException"><paramref name="pin"/> is not 4 ASCII digits.</exception>
    public static VehicleCommand EnableValetMode(string pin)
    {
        RequirePin(pin, nameof(pin));
        return Infotainment("enable_valet_mode", new Cs.VehicleAction
        {
            VehicleControlSetValetModeAction = new Cs.VehicleControlSetValetModeAction { On = true, Password = pin },
        });
    }

    /// <summary>Turns Valet Mode off (Go sends an empty password).</summary>
    public static VehicleCommand DisableValetMode() =>
        Infotainment("disable_valet_mode", new Cs.VehicleAction
        {
            VehicleControlSetValetModeAction = new Cs.VehicleControlSetValetModeAction { On = false, Password = "" },
        });

    /// <summary>Resets the valet PIN (valet mode must be off).</summary>
    public static VehicleCommand ResetValetPin() =>
        Infotainment("reset_valet_pin", new Cs.VehicleAction { VehicleControlResetValetPinAction = new Cs.VehicleControlResetValetPinAction() });

    /// <summary>Turns PIN to Drive on (4-digit PIN required) or off (PIN optional).</summary>
    /// <exception cref="ArgumentException">Turning on without a 4-digit PIN, or a PIN that is not 4 digits when given.</exception>
    public static VehicleCommand SetPinToDrive(bool on, string? pin)
    {
        if (on || pin is not null) RequirePin(pin, nameof(pin));
        return Infotainment("set_pin_to_drive", new Cs.VehicleAction
        {
            VehicleControlSetPinToDriveAction = new Cs.VehicleControlSetPinToDriveAction { On = on, Password = pin ?? "" },
        });
    }

    /// <summary>Clears the PIN to Drive as owner/fleet manager (firmware 2023.44+).</summary>
    public static VehicleCommand ClearPinToDriveAdmin() =>
        Infotainment("clear_pin_to_drive_admin", new Cs.VehicleAction { VehicleControlResetPinToDriveAdminAction = new Cs.VehicleControlResetPinToDriveAdminAction() });

    /// <summary>Resets the PIN to Drive (deprecated by Tesla).</summary>
    public static VehicleCommand ResetPinToDrive() =>
        Infotainment("reset_pin_to_drive", new Cs.VehicleAction { VehicleControlResetPinToDriveAction = new Cs.VehicleControlResetPinToDriveAction() });

    // ---- Speed limit -------------------------------------------------------------------------------------------

    /// <summary>Activates the speed limit with a 4-digit PIN.</summary>
    public static VehicleCommand ActivateSpeedLimit(string pin) => SpeedLimitActivation("activate_speed_limit", pin, true);

    /// <summary>Deactivates the speed limit with a 4-digit PIN.</summary>
    public static VehicleCommand DeactivateSpeedLimit(string pin) => SpeedLimitActivation("deactivate_speed_limit", pin, false);

    /// <summary>Clears the speed limit PIN using the current PIN.</summary>
    public static VehicleCommand ClearSpeedLimitPin(string pin)
    {
        RequirePin(pin, nameof(pin));
        return Infotainment("clear_speed_limit_pin", new Cs.VehicleAction
        {
            DrivingClearSpeedLimitPinAction = new Cs.DrivingClearSpeedLimitPinAction { Pin = pin },
        });
    }

    /// <summary>Clears the speed limit PIN as owner/fleet manager (firmware 2023.38+).</summary>
    public static VehicleCommand ClearSpeedLimitPinAdmin() =>
        Infotainment("clear_speed_limit_pin_admin", new Cs.VehicleAction { DrivingClearSpeedLimitPinAdminAction = new Cs.DrivingClearSpeedLimitPinAdminAction() });

    /// <summary>Sets the speed limit in mph. Like Go, no range check beyond finiteness (the vehicle enforces its range).</summary>
    public static VehicleCommand SetSpeedLimit(double limitMph)
    {
        RequireFinite(limitMph, nameof(limitMph));
        return Infotainment("set_speed_limit", new Cs.VehicleAction
        {
            DrivingSetSpeedLimitAction = new Cs.DrivingSetSpeedLimitAction { LimitMph = limitMph },
        });
    }

    // ---- Parental controls -------------------------------------------------------------------------------------

    /// <summary>Activates parental controls with a 4-digit PIN.</summary>
    public static VehicleCommand ActivateParentalControls(string pin) => ParentalActivation("activate_parental_controls", pin, true);

    /// <summary>Deactivates parental controls with a 4-digit PIN.</summary>
    public static VehicleCommand DeactivateParentalControls(string pin) => ParentalActivation("deactivate_parental_controls", pin, false);

    /// <summary>Enables or disables one parental controls policy (only while parental controls are inactive).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Unknown <paramref name="setting"/>.</exception>
    public static VehicleCommand SetParentalControlsSetting(ParentalControlsSetting setting, bool enable)
    {
        if (!Enum.IsDefined(setting))
            throw new ArgumentOutOfRangeException(nameof(setting), setting, "Unknown parental controls setting.");
        return Infotainment("set_parental_controls_setting", new Cs.VehicleAction
        {
            ParentalControlsEnableSettingsAction = new Cs.ParentalControlsEnableSettingsAction
            {
                Setting = (Cs.ParentalControlsEnableSettingsAction.Types.ParentalControlsSetting_E)(int)setting,
                Enable = enable,
            },
        });
    }

    /// <summary>Sets the parental controls speed limit in mph (only while parental controls are inactive).</summary>
    public static VehicleCommand SetParentalControlsSpeedLimit(double limitMph)
    {
        RequireFinite(limitMph, nameof(limitMph));
        return Infotainment("set_parental_controls_speed_limit", new Cs.VehicleAction
        {
            ParentalControlsSetSpeedLimitAction = new Cs.ParentalControlsSetSpeedLimitAction { LimitMph = limitMph },
        });
    }

    /// <summary>Clears the parental controls PIN as owner/fleet manager.</summary>
    public static VehicleCommand ClearParentalControlsPinAdmin() =>
        Infotainment("clear_parental_controls_pin_admin", new Cs.VehicleAction { ParentalControlsClearPinAdminAction = new Cs.ParentalControlsClearPinAdminAction() });

    // ---- Guest mode --------------------------------------------------------------------------------------------

    /// <summary>Turns Guest Mode on or off (<c>VehicleState.GuestMode</c> from vehicle.proto).</summary>
    public static VehicleCommand SetGuestMode(bool on) =>
        Infotainment("set_guest_mode", new Cs.VehicleAction
        {
            GuestModeAction = new Cs.VehicleState.Types.GuestMode { GuestModeActive = on },
        });

    /// <summary>Erases guest user data (empty message in the oneof; the <c>reason</c> field is never set, like Go).</summary>
    public static VehicleCommand EraseUserData() =>
        Infotainment("erase_user_data", new Cs.VehicleAction { EraseUserDataAction = new Cs.EraseUserDataAction() });

    // ---- Software update ---------------------------------------------------------------------------------------

    /// <summary>Schedules the pending software update to start after <paramref name="delay"/> (whole seconds).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Negative or larger than <see cref="int.MaxValue"/> seconds.</exception>
    public static VehicleCommand ScheduleSoftwareUpdate(TimeSpan delay)
    {
        double seconds = delay.TotalSeconds;
        if (seconds < 0 || seconds > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Delay must be between zero and int.MaxValue seconds.");
        return Infotainment("schedule_software_update", new Cs.VehicleAction
        {
            VehicleControlScheduleSoftwareUpdateAction = new Cs.VehicleControlScheduleSoftwareUpdateAction { OffsetSec = (int)seconds },
        });
    }

    /// <summary>Cancels a scheduled software update (fails once installation has started).</summary>
    public static VehicleCommand CancelSoftwareUpdate() =>
        Infotainment("cancel_software_update", new Cs.VehicleAction { VehicleControlCancelSoftwareUpdateAction = new Cs.VehicleControlCancelSoftwareUpdateAction() });

    // ---- Misc --------------------------------------------------------------------------------------------------

    /// <summary>Triggers HomeLink at the given location. The <c>token</c> field is never set (like Go).</summary>
    /// <exception cref="ArgumentOutOfRangeException">Latitude outside ±90, longitude outside ±180, or not finite.</exception>
    public static VehicleCommand TriggerHomelink(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90.");
        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180.");
        return Infotainment("trigger_homelink", new Cs.VehicleAction
        {
            VehicleControlTriggerHomelinkAction = new Cs.VehicleControlTriggerHomelinkAction
            {
                Location = new Cs.LatLong { Latitude = (float)latitude, Longitude = (float)longitude },
            },
        });
    }

    /// <summary>Renames the vehicle.</summary>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null, empty or whitespace.</exception>
    public static VehicleCommand SetVehicleName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vehicle name must not be empty.", nameof(name));
        return Infotainment("set_vehicle_name", new Cs.VehicleAction { SetVehicleNameAction = new Cs.SetVehicleNameAction { VehicleName = name } });
    }

    /// <summary>Signed no-op (<c>ping_id = 1</c>, like Go); success means the key is paired and infotainment is awake.</summary>
    public static VehicleCommand Ping() =>
        Infotainment("ping", new Cs.VehicleAction { Ping = new Cs.Ping { PingId = 1 } });

    // ---- helpers -----------------------------------------------------------------------------------------------

    private static VehicleCommand SpeedLimitActivation(string name, string pin, bool activate)
    {
        RequirePin(pin, nameof(pin));
        return Infotainment(name, new Cs.VehicleAction
        {
            DrivingSpeedLimitAction = new Cs.DrivingSpeedLimitAction { Activate = activate, Pin = pin },
        });
    }

    private static VehicleCommand ParentalActivation(string name, string pin, bool activate)
    {
        RequirePin(pin, nameof(pin));
        return Infotainment(name, new Cs.VehicleAction
        {
            ParentalControlsAction = new Cs.ParentalControlsAction { Activate = activate, Pin = pin },
        });
    }

    private static void RequirePin(string? pin, string paramName)
    {
        if (pin is not { Length: 4 } || !pin.All(static c => c is >= '0' and <= '9'))
            throw new ArgumentException("PIN must be exactly 4 ASCII digits.", paramName);
    }

    private static void RequireFinite(double value, string paramName)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be a finite number.");
    }
}
