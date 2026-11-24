using CoilPro.Commands.Internal.Commands;
using UniversalMessage;

namespace CoilPro.Commands.Tests.CommandsSettings;

/// <summary>
/// Golden bytes hand-encoded from the .proto field numbers (tag = field &lt;&lt; 3 | wire type, varint-encoded).
/// Prefix <c>12 len</c> = Action.vehicleAction(2). PIN "1234" = 31 32 33 34.
/// </summary>
public class CommandPayloadsSettingsTests
{
    private const string Pin = "31323334";

    private static void Check(string expectedHex, string name, VehicleCommand command)
    {
        Assert.Equal(name, command.Name);
        Assert.Equal(Domain.Infotainment, command.Domain);
        Assert.Equal(expectedHex, Convert.ToHexString(command.Payload));
    }

    // ---- media ----

    // mediaUpdateVolume = 16 -> 82 01; volume_absolute_float(3) = 1D + 5.0f (00 00 A0 40)
    [Fact] public void SetVolume() => Check("1208820105" + "1D0000A040", "set_volume", CommandPayloads.SetVolume(5f));

    // volume_delta(1) sint32 zigzag: +1 -> 02, -1 -> 01
    [Fact] public void VolumeUp() => Check("12058201020802", "volume_up", CommandPayloads.VolumeUp());
    [Fact] public void VolumeDown() => Check("12058201020801", "volume_down", CommandPayloads.VolumeDown());

    // mediaNextTrack 19 -> 9A 01, mediaPreviousTrack 20 -> A2 01, mediaNextFavorite 17 -> 8A 01,
    // mediaPreviousFavorite 18 -> 92 01, mediaPlayAction 15 -> 7A
    [Fact] public void NextTrack() => Check("12039A0100", "media_next_track", CommandPayloads.NextTrack());
    [Fact] public void PreviousTrack() => Check("1203A20100", "media_previous_track", CommandPayloads.PreviousTrack());
    [Fact] public void NextFavorite() => Check("12038A0100", "media_next_favorite", CommandPayloads.NextFavorite());
    [Fact] public void PreviousFavorite() => Check("1203920100", "media_previous_favorite", CommandPayloads.PreviousFavorite());
    [Fact] public void TogglePlayback() => Check("12027A00", "media_toggle_playback", CommandPayloads.TogglePlayback());

    // ---- sentry / valet / PIN to Drive ----

    // vehicleControlSetSentryModeAction = 30 -> F2 01
    [Fact] public void SetSentryModeOn() => Check("1205F201020801", "set_sentry_mode", CommandPayloads.SetSentryMode(true));
    [Fact] public void SetSentryModeOff() => Check("1203F20100","set_sentry_mode", CommandPayloads.SetSentryMode(false));

    // vehicleControlSetValetModeAction = 31 -> FA 01; on(1), password(2)
    [Fact] public void EnableValetMode() => Check("120BFA01" + "08" + "0801" + "1204" + Pin, "enable_valet_mode", CommandPayloads.EnableValetMode("1234"));
    [Fact] public void DisableValetMode() => Check("1203FA0100", "disable_valet_mode", CommandPayloads.DisableValetMode());

    // vehicleControlResetValetPinAction = 28 -> E2 01
    [Fact] public void ResetValetPin() => Check("1203E20100", "reset_valet_pin", CommandPayloads.ResetValetPin());

    // vehicleControlSetPinToDriveAction = 77 -> EA 04
    [Fact] public void SetPinToDriveOn() => Check("120BEA04" + "08" + "0801" + "1204" + Pin, "set_pin_to_drive", CommandPayloads.SetPinToDrive(true, "1234"));
    [Fact] public void SetPinToDriveOffNoPin() => Check("1203EA0400", "set_pin_to_drive", CommandPayloads.SetPinToDrive(false, null));
    [Fact] public void SetPinToDriveOffWithPin() => Check("1209EA04" + "06" + "1204" + Pin, "set_pin_to_drive", CommandPayloads.SetPinToDrive(false, "1234"));

    // vehicleControlResetPinToDriveAdminAction = 89 -> CA 05; vehicleControlResetPinToDriveAction = 78 -> F2 04
    [Fact] public void ClearPinToDriveAdmin() => Check("1203CA0500", "clear_pin_to_drive_admin", CommandPayloads.ClearPinToDriveAdmin());
    [Fact] public void ResetPinToDrive() => Check("1203F20400", "reset_pin_to_drive", CommandPayloads.ResetPinToDrive());

    // ---- speed limit ----

    // drivingSpeedLimitAction = 9 -> 4A; activate(1), pin(2)
    [Fact] public void ActivateSpeedLimit() => Check("120A4A" + "08" + "0801" + "1204" + Pin, "activate_speed_limit", CommandPayloads.ActivateSpeedLimit("1234"));
    [Fact] public void DeactivateSpeedLimit() => Check("12084A" + "06" + "1204" + Pin, "deactivate_speed_limit", CommandPayloads.DeactivateSpeedLimit("1234"));

    // drivingClearSpeedLimitPinAction = 7 -> 3A; pin(1) = 0A
    [Fact] public void ClearSpeedLimitPin() => Check("12083A" + "06" + "0A04" + Pin, "clear_speed_limit_pin", CommandPayloads.ClearSpeedLimitPin("1234"));

    // drivingClearSpeedLimitPinAdminAction = 79 -> FA 04
    [Fact] public void ClearSpeedLimitPinAdmin() => Check("1203FA0400", "clear_speed_limit_pin_admin", CommandPayloads.ClearSpeedLimitPinAdmin());

    // drivingSetSpeedLimitAction = 8 -> 42; limit_mph(1) double = 09 + 50.0 (00 00 00 00 00 00 49 40)
    [Fact] public void SetSpeedLimit() => Check("120B42" + "09" + "09" + "0000000000004940", "set_speed_limit", CommandPayloads.SetSpeedLimit(50));

    // ---- parental controls ----

    // parentalControlsAction = 111 -> FA 06
    [Fact] public void ActivateParentalControls() => Check("120BFA06" + "08" + "0801" + "1204" + Pin, "activate_parental_controls", CommandPayloads.ActivateParentalControls("1234"));
    [Fact] public void DeactivateParentalControls() => Check("1209FA06" + "06" + "1204" + Pin, "deactivate_parental_controls", CommandPayloads.DeactivateParentalControls("1234"));

    // parentalControlsEnableSettingsAction = 112 -> 82 07; setting(1) enum, enable(2)
    [Fact] public void SetParentalControlsSettingCurfewOn() => Check("12078207" + "04" + "0803" + "1001", "set_parental_controls_setting", CommandPayloads.SetParentalControlsSetting(ParentalControlsSetting.Curfew, true));
    [Fact] public void SetParentalControlsSettingSpeedLimitOff() => Check("1203820700", "set_parental_controls_setting", CommandPayloads.SetParentalControlsSetting(ParentalControlsSetting.SpeedLimit, false));

    [Theory]
    [InlineData(ParentalControlsSetting.Acceleration, 1)]
    [InlineData(ParentalControlsSetting.SafetyFeatures, 2)]
    [InlineData(ParentalControlsSetting.BrowserBlocked, 4)]
    [InlineData(ParentalControlsSetting.TheaterBlocked, 5)]
    [InlineData(ParentalControlsSetting.ArcadeBlocked, 6)]
    public void ParentalControlsSettingEnumValues(ParentalControlsSetting setting, int wire) =>
        Check("12078207" + "04" + "08" + wire.ToString("X2") + "1001", "set_parental_controls_setting", CommandPayloads.SetParentalControlsSetting(setting, true));

    // parentalControlsSetSpeedLimitAction = 113 -> 8A 07; limit_mph(1) double 50.0
    [Fact] public void SetParentalControlsSpeedLimit() => Check("120C8A07" + "09" + "09" + "0000000000004940", "set_parental_controls_speed_limit", CommandPayloads.SetParentalControlsSpeedLimit(50));

    // parentalControlsClearPinAdminAction = 110 -> F2 06
    [Fact] public void ClearParentalControlsPinAdmin() => Check("1203F20600", "clear_parental_controls_pin_admin", CommandPayloads.ClearParentalControlsPinAdmin());

    // ---- guest mode ----

    // guestModeAction = 65 -> 8A 04; VehicleState.GuestMode{GuestModeActive(1)}
    [Fact] public void SetGuestModeOn() => Check("12058A04020801", "set_guest_mode", CommandPayloads.SetGuestMode(true));
    [Fact] public void SetGuestModeOff() => Check("12038A0400", "set_guest_mode", CommandPayloads.SetGuestMode(false));

    // eraseUserDataAction = 72 -> C2 04, empty message (oneof member is still emitted)
    [Fact] public void EraseUserData() => Check("1203C20400", "erase_user_data", CommandPayloads.EraseUserData());

    // ---- software update ----

    // vehicleControlScheduleSoftwareUpdateAction = 29 -> EA 01; offset_sec(1) int32 7200 = A0 38
    [Fact] public void ScheduleSoftwareUpdate() => Check("1206EA01" + "03" + "08A038", "schedule_software_update", CommandPayloads.ScheduleSoftwareUpdate(TimeSpan.FromHours(2)));
    [Fact] public void ScheduleSoftwareUpdateImmediate() => Check("1203EA0100", "schedule_software_update", CommandPayloads.ScheduleSoftwareUpdate(TimeSpan.Zero));

    // vehicleControlCancelSoftwareUpdateAction = 25 -> CA 01
    [Fact] public void CancelSoftwareUpdate() => Check("1203CA0100", "cancel_software_update", CommandPayloads.CancelSoftwareUpdate());

    // ---- misc ----

    // vehicleControlTriggerHomelinkAction = 33 -> 8A 02; location(1) = 0A { lat(1) 0D 1.0f, lon(2) 15 2.0f }
    [Fact]
    public void TriggerHomelink() => Check(
        "120F8A02" + "0C" + "0A0A" + "0D0000803F" + "1500000040", "trigger_homelink", CommandPayloads.TriggerHomelink(1.0, 2.0));

    // setVehicleNameAction = 54 -> B2 03; vehicleName(1)
    [Fact] public void SetVehicleName() => Check("1208B203" + "05" + "0A03" + "416263", "set_vehicle_name", CommandPayloads.SetVehicleName("Abc"));

    // ping = 46 -> F2 02; ping_id(1) = 1
    [Fact] public void Ping() => Check("1205F202020801", "ping", CommandPayloads.Ping());

    // ---- validation ----

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(10.1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void SetVolumeRejectsOutOfRange(float v) => Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetVolume(v));

    [Theory]
    [InlineData(0f)]
    [InlineData(10f)]
    public void SetVolumeAcceptsBounds(float v) => Assert.Equal(Domain.Infotainment, CommandPayloads.SetVolume(v).Domain);

    public static TheoryData<string?> BadPins => new() { null, "", "123", "12345", "12a4", "١٢٣٤", " 123", "1234 " };

    [Theory, MemberData(nameof(BadPins))]
    public void PinsRejected(string? pin)
    {
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.EnableValetMode(pin!));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.SetPinToDrive(true, pin));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.ActivateSpeedLimit(pin!));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.DeactivateSpeedLimit(pin!));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.ClearSpeedLimitPin(pin!));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.ActivateParentalControls(pin!));
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.DeactivateParentalControls(pin!));
    }

    [Fact]
    public void SetPinToDriveOffRejectsMalformedPinButAllowsNull()
    {
        Assert.Throws<ArgumentException>(() => CommandPayloads.SetPinToDrive(false, "12"));
        CommandPayloads.SetPinToDrive(false, null);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void SpeedLimitsRejectNonFinite(double v)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetSpeedLimit(v));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetParentalControlsSpeedLimit(v));
    }

    [Fact]
    public void UnknownParentalSettingRejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetParentalControlsSetting((ParentalControlsSetting)99, true));

    [Theory]
    [InlineData(-1)]
    [InlineData(2147483648.0)]
    public void ScheduleSoftwareUpdateRejectsOutOfRange(double seconds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.ScheduleSoftwareUpdate(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void ScheduleSoftwareUpdateAcceptsMaxInt() =>
        Assert.Equal(Domain.Infotainment, CommandPayloads.ScheduleSoftwareUpdate(TimeSpan.FromSeconds(int.MaxValue)).Domain);

    [Theory]
    [InlineData(90.1, 0)]
    [InlineData(-90.1, 0)]
    [InlineData(0, 180.1)]
    [InlineData(0, -180.1)]
    [InlineData(double.NaN, 0)]
    [InlineData(0, double.PositiveInfinity)]
    public void HomelinkRejectsInvalidCoordinates(double lat, double lon) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.TriggerHomelink(lat, lon));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VehicleNameRejectsEmpty(string? name) =>
        Assert.ThrowsAny<ArgumentException>(() => CommandPayloads.SetVehicleName(name!));
}
