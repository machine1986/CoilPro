namespace CoilPro.Commands.Internal.Commands;

internal static partial class CommandPayloads
{
    public static VehicleCommand SetVolume(float volume) => throw new NotImplementedException("TODO(v0.2 commands-b): SetVolume");

    public static VehicleCommand VolumeUp() => throw new NotImplementedException("TODO(v0.2 commands-b): VolumeUp");

    public static VehicleCommand VolumeDown() => throw new NotImplementedException("TODO(v0.2 commands-b): VolumeDown");

    public static VehicleCommand NextTrack() => throw new NotImplementedException("TODO(v0.2 commands-b): NextTrack");

    public static VehicleCommand PreviousTrack() => throw new NotImplementedException("TODO(v0.2 commands-b): PreviousTrack");

    public static VehicleCommand NextFavorite() => throw new NotImplementedException("TODO(v0.2 commands-b): NextFavorite");

    public static VehicleCommand PreviousFavorite() => throw new NotImplementedException("TODO(v0.2 commands-b): PreviousFavorite");

    public static VehicleCommand TogglePlayback() => throw new NotImplementedException("TODO(v0.2 commands-b): TogglePlayback");

    public static VehicleCommand SetSentryMode(bool on) => throw new NotImplementedException("TODO(v0.2 commands-b): SetSentryMode");

    public static VehicleCommand EnableValetMode(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): EnableValetMode");

    public static VehicleCommand DisableValetMode() => throw new NotImplementedException("TODO(v0.2 commands-b): DisableValetMode");

    public static VehicleCommand ResetValetPin() => throw new NotImplementedException("TODO(v0.2 commands-b): ResetValetPin");

    public static VehicleCommand SetPinToDrive(bool on, string? pin) => throw new NotImplementedException("TODO(v0.2 commands-b): SetPinToDrive");

    public static VehicleCommand ClearPinToDriveAdmin() => throw new NotImplementedException("TODO(v0.2 commands-b): ClearPinToDriveAdmin");

    public static VehicleCommand ResetPinToDrive() => throw new NotImplementedException("TODO(v0.2 commands-b): ResetPinToDrive");

    public static VehicleCommand ActivateSpeedLimit(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): ActivateSpeedLimit");

    public static VehicleCommand DeactivateSpeedLimit(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): DeactivateSpeedLimit");

    public static VehicleCommand ClearSpeedLimitPin(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): ClearSpeedLimitPin");

    public static VehicleCommand ClearSpeedLimitPinAdmin() => throw new NotImplementedException("TODO(v0.2 commands-b): ClearSpeedLimitPinAdmin");

    public static VehicleCommand SetSpeedLimit(double limitMph) => throw new NotImplementedException("TODO(v0.2 commands-b): SetSpeedLimit");

    public static VehicleCommand ActivateParentalControls(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): ActivateParentalControls");

    public static VehicleCommand DeactivateParentalControls(string pin) => throw new NotImplementedException("TODO(v0.2 commands-b): DeactivateParentalControls");

    public static VehicleCommand SetParentalControlsSetting(ParentalControlsSetting setting, bool enable) => throw new NotImplementedException("TODO(v0.2 commands-b): SetParentalControlsSetting");

    public static VehicleCommand SetParentalControlsSpeedLimit(double limitMph) => throw new NotImplementedException("TODO(v0.2 commands-b): SetParentalControlsSpeedLimit");

    public static VehicleCommand ClearParentalControlsPinAdmin() => throw new NotImplementedException("TODO(v0.2 commands-b): ClearParentalControlsPinAdmin");

    public static VehicleCommand SetGuestMode(bool on) => throw new NotImplementedException("TODO(v0.2 commands-b): SetGuestMode");

    public static VehicleCommand EraseUserData() => throw new NotImplementedException("TODO(v0.2 commands-b): EraseUserData");

    public static VehicleCommand ScheduleSoftwareUpdate(TimeSpan delay) => throw new NotImplementedException("TODO(v0.2 commands-b): ScheduleSoftwareUpdate");

    public static VehicleCommand CancelSoftwareUpdate() => throw new NotImplementedException("TODO(v0.2 commands-b): CancelSoftwareUpdate");

    public static VehicleCommand TriggerHomelink(double latitude, double longitude) => throw new NotImplementedException("TODO(v0.2 commands-b): TriggerHomelink");

    public static VehicleCommand SetVehicleName(string name) => throw new NotImplementedException("TODO(v0.2 commands-b): SetVehicleName");

    public static VehicleCommand Ping() => throw new NotImplementedException("TODO(v0.2 commands-b): Ping");
}
