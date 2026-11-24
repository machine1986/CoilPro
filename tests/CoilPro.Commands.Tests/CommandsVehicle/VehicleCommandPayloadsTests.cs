using CoilPro.Commands.Internal.Commands;
using UniversalMessage;

namespace CoilPro.Commands.Tests.CommandsVehicle;

/// <summary>
/// Golden bytes are hand-encoded from the .proto field numbers (tag = field &lt;&lt; 3 | wire type; fields above 15
/// take a two-byte varint tag). Infotainment payloads are <c>Action.vehicleAction(2)</c> = <c>12 LL</c> + VehicleAction;
/// VCSEC payloads are a bare <c>UnsignedMessage</c>. Void oneof members serialize as <c>tag 00</c>.
/// </summary>
public class VehicleCommandPayloadsTests
{
    private static void Check(string expectedHex, Domain domain, string name, VehicleCommand command)
    {
        Assert.Equal(name, command.Name);
        Assert.Equal(domain, command.Domain);
        Assert.Equal(expectedHex.Replace(" ", ""), Convert.ToHexString(command.Payload));
    }

    private static void Info(string hex, string name, VehicleCommand command) => Check(hex, Domain.Infotainment, name, command);

    private static void Vcsec(string hex, string name, VehicleCommand command) => Check(hex, Domain.VehicleSecurity, name, command);

    // ---- VCSEC: RKEAction(2) varint; closureMoveRequest(4)=0x22 { rearTrunk(5)=0x28, frontTrunk(6)=0x30, tonneau(8)=0x40 }

    [Fact] public void RemoteStartDrive() => Vcsec("10 14", "remote_start_drive", CommandPayloads.RemoteStartDrive());

    [Fact] public void AutoSecureModelX() => Vcsec("10 1D", "auto_secure_model_x", CommandPayloads.AutoSecureModelX());

    [Fact] public void OpenFrunk() => Vcsec("22 02 30 01", "open_frunk", CommandPayloads.OpenFrunk());

    [Fact] public void ActuateRearTrunk() => Vcsec("22 02 28 01", "actuate_rear_trunk", CommandPayloads.ActuateRearTrunk());

    [Fact] public void CloseRearTrunk() => Vcsec("22 02 28 04", "close_rear_trunk", CommandPayloads.CloseRearTrunk());

    [Fact] public void OpenTonneau() => Vcsec("22 02 40 03", "open_tonneau", CommandPayloads.OpenTonneau());

    [Fact] public void CloseTonneau() => Vcsec("22 02 40 04", "close_tonneau", CommandPayloads.CloseTonneau());

    [Fact] public void StopTonneau() => Vcsec("22 02 40 02", "stop_tonneau", CommandPayloads.StopTonneau());

    // ---- Charging

    // chargePortDoorClose = 61 -> tag EA 03
    [Fact] public void CloseChargePort() => Info("12 03 EA 03 00", "close_charge_port", CommandPayloads.CloseChargePort());

    // chargingStartStopAction = 6 (0x32): start_standard = 3 (0x1A) / start_max_range = 4 (0x22)
    [Fact] public void ChargeStandardRange() => Info("12 04 32 02 1A 00", "charge_standard", CommandPayloads.ChargeStandardRange());

    [Fact] public void ChargeMaxRange() => Info("12 04 32 02 22 00", "charge_max_range", CommandPayloads.ChargeMaxRange());

    // scheduledChargingAction = 41 -> tag CA 02; enabled(1)=08 01, charging_time(2)=10 96 01 (02:30 = 150 min)
    [Fact]
    public void SetScheduledCharging() =>
        Info("12 08 CA 02 05 08 01 10 96 01", "set_scheduled_charging", CommandPayloads.SetScheduledCharging(true, new TimeOnly(2, 30)));

    [Fact]
    public void SetScheduledChargingDisabledMidnightIsEmptyMessage() =>
        Info("12 03 CA 02 00", "set_scheduled_charging", CommandPayloads.SetScheduledCharging(false, TimeOnly.MinValue));

    // scheduledDepartureAction = 42 -> tag D2 02; enabled 08 01, departure_time(2) 10 C2 03 (450 = 07:30),
    // preconditioning_times(3) 1A {weekdays(2)=12 00}, off_peak_charging_times(4) 22 {all_week(1)=0A 00},
    // off_peak_hours_end_time(5) 28 E8 02 (360 = 06:00)
    [Fact]
    public void SetScheduledDepartureFull() => Info(
        "12 13 D2 02 10 08 01 10 C2 03 1A 02 12 00 22 02 0A 00 28 E8 02",
        "set_scheduled_departure",
        CommandPayloads.SetScheduledDeparture(new ScheduledDeparture
        {
            DepartureTime = new TimeOnly(7, 30),
            Preconditioning = DeparturePolicy.WeekdaysOnly,
            OffPeakCharging = DeparturePolicy.AllWeek,
            OffPeakHoursEndTime = new TimeOnly(6, 0),
        }));

    // Off -> field unset; no end time -> 0 (omitted); enabled is always true
    [Fact]
    public void SetScheduledDepartureOffPoliciesLeaveFieldsUnset() => Info(
        "12 08 D2 02 05 08 01 10 C2 03",
        "set_scheduled_departure",
        CommandPayloads.SetScheduledDeparture(new ScheduledDeparture { DepartureTime = new TimeOnly(7, 30) }));

    // preconditioning all_week(1) = 0A 00, off-peak weekdays(2) = 12 00
    [Fact]
    public void SetScheduledDepartureAllWeekPreconditioning() => Info(
        "12 10 D2 02 0D 08 01 10 C2 03 1A 02 0A 00 22 02 12 00",
        "set_scheduled_departure",
        CommandPayloads.SetScheduledDeparture(new ScheduledDeparture
        {
            DepartureTime = new TimeOnly(7, 30),
            Preconditioning = DeparturePolicy.AllWeek,
            OffPeakCharging = DeparturePolicy.WeekdaysOnly,
        }));

    // enabled=false only
    [Fact] public void ClearScheduledDeparture() => Info("12 03 D2 02 00", "clear_scheduled_departure", CommandPayloads.ClearScheduledDeparture());

    // addChargeScheduleAction = 97 -> tag 8A 06. id(1)=300 (08 AC 02), days_of_week(3)=62 (18 3E), start_enabled(4) 20 01,
    // start_time(5) 28 A8 0A (1320 = 22:00), end_enabled(6) 30 01, end_time(7) 38 E8 02 (360 = 06:00), enabled(9) 48 01,
    // latitude(10) 55 + 48.5f (00 00 42 42), longitude(11) 5D + 9.0f (00 00 10 41)
    [Fact]
    public void AddChargeSchedule() => Info(
        "12 1E 8A 06 1B 08 AC 02 18 3E 20 01 28 A8 0A 30 01 38 E8 02 48 01 55 00004242 5D 00001041",
        "add_charge_schedule",
        CommandPayloads.AddChargeSchedule(new ChargeSchedule
        {
            Id = 300,
            Days = ScheduleDays.Weekdays,
            StartTime = new TimeOnly(22, 0),
            EndTime = new TimeOnly(6, 0),
            Latitude = 48.5f,
            Longitude = 9.0f,
        }));

    // no times -> start/end disabled, zeros omitted; one_time(8) 40 01; enabled=false omitted; days All = 127 (18 7F)
    [Fact]
    public void AddChargeScheduleWithoutTimes() => Info(
        "12 09 8A 06 06 08 01 18 7F 40 01",
        "add_charge_schedule",
        CommandPayloads.AddChargeSchedule(new ChargeSchedule
        {
            Id = 1,
            Days = ScheduleDays.All,
            OneTime = true,
            Enabled = false,
            Latitude = 0f,
            Longitude = 0f,
        }));

    // only a start time: start_enabled + start_time, end_enabled omitted
    [Fact]
    public void AddChargeScheduleStartOnly() => Info(
        "12 0D 8A 06 0A 08 01 18 01 20 01 28 3C 48 01",
        "add_charge_schedule",
        CommandPayloads.AddChargeSchedule(new ChargeSchedule
        {
            Id = 1,
            Days = ScheduleDays.Sunday,
            StartTime = new TimeOnly(1, 0),
            Latitude = 0f,
            Longitude = 0f,
        }));

    // removeChargeScheduleAction = 98 -> tag 92 06; id(1) = 300
    [Fact] public void RemoveChargeSchedule() => Info("12 06 92 06 03 08 AC 02", "remove_charge_schedule", CommandPayloads.RemoveChargeSchedule(300));

    // batchRemoveChargeSchedulesAction = 108 -> tag E2 06; home(1) 08, work(2) 10, other(3) 18
    [Theory]
    [InlineData(ScheduleLocations.Home | ScheduleLocations.Other, "12 07 E2 06 04 08 01 18 01")]
    [InlineData(ScheduleLocations.Work, "12 05 E2 06 02 10 01")]
    [InlineData(ScheduleLocations.Home | ScheduleLocations.Work | ScheduleLocations.Other, "12 09 E2 06 06 08 01 10 01 18 01")]
    [InlineData(ScheduleLocations.None, "12 03 E2 06 00")]
    public void RemoveChargeSchedules(ScheduleLocations locations, string hex) =>
        Info(hex, "remove_charge_schedules", CommandPayloads.RemoveChargeSchedules(locations));

    // addPreconditionScheduleAction = 99 -> tag 9A 06. id 08 AC 02, days(3) 18 2A (Mon|Wed|Fri = 42),
    // precondition_time(4) 20 B3 03 (435 = 07:15), one_time(5) 28 01, enabled(6) 30 01,
    // latitude(7) 3D 00 00 42 42, longitude(8) 45 00 00 10 41
    [Fact]
    public void AddPreconditionSchedule() => Info(
        "12 19 9A 06 16 08 AC 02 18 2A 20 B3 03 28 01 30 01 3D 00004242 45 00001041",
        "add_precondition_schedule",
        CommandPayloads.AddPreconditionSchedule(new PreconditionSchedule
        {
            Id = 300,
            Days = ScheduleDays.Monday | ScheduleDays.Wednesday | ScheduleDays.Friday,
            PreconditionTime = new TimeOnly(7, 15),
            OneTime = true,
            Latitude = 48.5f,
            Longitude = 9.0f,
        }));

    // disabled, midnight, negative coordinates: -1.0f = 0xBF800000 (00 00 80 BF)
    [Fact]
    public void AddPreconditionScheduleMinimal() => Info(
        "12 11 9A 06 0E 08 01 18 40 3D 000080BF 45 000080BF",
        "add_precondition_schedule",
        CommandPayloads.AddPreconditionSchedule(new PreconditionSchedule
        {
            Id = 1,
            Days = ScheduleDays.Saturday,
            PreconditionTime = TimeOnly.MinValue,
            Enabled = false,
            Latitude = -1f,
            Longitude = -1f,
        }));

    // removePreconditionScheduleAction = 100 -> tag A2 06
    [Fact] public void RemovePreconditionSchedule() => Info("12 06 A2 06 03 08 AC 02", "remove_precondition_schedule", CommandPayloads.RemovePreconditionSchedule(300));

    // batchRemovePreconditionSchedulesAction = 107 -> tag DA 06
    [Theory]
    [InlineData(ScheduleLocations.Work | ScheduleLocations.Other, "12 07 DA 06 04 10 01 18 01")]
    [InlineData(ScheduleLocations.Home, "12 05 DA 06 02 08 01")]
    [InlineData(ScheduleLocations.None, "12 03 DA 06 00")]
    public void RemovePreconditionSchedules(ScheduleLocations locations, string hex) =>
        Info(hex, "remove_precondition_schedules", CommandPayloads.RemovePreconditionSchedules(locations));

    // setLowPowerModeAction = 130 -> tag 92 08; setKeepAccessoryPowerModeAction = 138 -> tag D2 08; field 1 bool
    [Fact] public void SetLowPowerModeOn() => Info("12 05 92 08 02 08 01", "set_low_power_mode", CommandPayloads.SetLowPowerMode(true));

    [Fact] public void SetLowPowerModeOff() => Info("12 03 92 08 00", "set_low_power_mode", CommandPayloads.SetLowPowerMode(false));

    [Fact] public void SetKeepAccessoryPowerModeOn() => Info("12 05 D2 08 02 08 01", "set_keep_accessory_power_mode", CommandPayloads.SetKeepAccessoryPowerMode(true));

    [Fact] public void SetKeepAccessoryPowerModeOff() => Info("12 03 D2 08 00", "set_keep_accessory_power_mode", CommandPayloads.SetKeepAccessoryPowerMode(false));

    // ---- Climate and seats

    // hvacSetPreconditioningMaxAction = 12 -> tag 62; on(1) 08, manual_override(2) 10
    [Fact] public void SetPreconditioningMaxWithOverride() => Info("12 06 62 04 08 01 10 01", "set_preconditioning_max", CommandPayloads.SetPreconditioningMax(true, true));

    [Fact] public void SetPreconditioningMaxNoOverride() => Info("12 04 62 02 08 01", "set_preconditioning_max", CommandPayloads.SetPreconditioningMax(true, false));

    [Fact] public void SetPreconditioningMaxOff() => Info("12 02 62 00", "set_preconditioning_max", CommandPayloads.SetPreconditioningMax(false, false));

    // hvacBioweaponModeAction = 35 -> tag 9A 02
    [Fact] public void SetBioweaponMode() => Info("12 07 9A 02 04 08 01 10 01", "set_bioweapon_mode", CommandPayloads.SetBioweaponMode(true, true));

    [Fact] public void SetBioweaponModeOff() => Info("12 05 9A 02 02 10 01", "set_bioweapon_mode", CommandPayloads.SetBioweaponMode(false, true));

    // setCabinOverheatProtectionAction = 50 -> tag 92 03; on(1) 08, fan_only(2) 10
    [Fact] public void SetCabinOverheatProtection() => Info("12 07 92 03 04 08 01 10 01", "set_cabin_overheat_protection", CommandPayloads.SetCabinOverheatProtection(true, true));

    [Fact] public void SetCabinOverheatProtectionFanOnlyWhileOff() => Info("12 05 92 03 02 10 01", "set_cabin_overheat_protection", CommandPayloads.SetCabinOverheatProtection(false, true));

    // setCopTempAction = 66 -> tag 92 04; copActivationTemp(1) enum: Low 1, Medium 2, High 3
    [Theory]
    [InlineData(CabinOverheatTemperature.Low, "12 05 92 04 02 08 01")]
    [InlineData(CabinOverheatTemperature.Medium, "12 05 92 04 02 08 02")]
    [InlineData(CabinOverheatTemperature.High, "12 05 92 04 02 08 03")]
    public void SetCabinOverheatProtectionTemperature(CabinOverheatTemperature temperature, string hex) =>
        Info(hex, "set_cop_temp", CommandPayloads.SetCabinOverheatProtectionTemperature(temperature));

    // hvacClimateKeeperAction = 44 -> tag E2 02; ClimateKeeperAction(1) 08 (Off 0 omitted, On 1, Dog 2, Camp 3), manual_override(2) 10
    [Theory]
    [InlineData(ClimateKeeperMode.Off, false, "12 03 E2 02 00")]
    [InlineData(ClimateKeeperMode.Keep, false, "12 05 E2 02 02 08 01")]
    [InlineData(ClimateKeeperMode.Dog, true, "12 07 E2 02 04 08 02 10 01")]
    [InlineData(ClimateKeeperMode.Camp, false, "12 05 E2 02 02 08 03")]
    [InlineData(ClimateKeeperMode.Off, true, "12 05 E2 02 02 10 01")]
    public void SetClimateKeeperMode(ClimateKeeperMode mode, bool manualOverride, string hex) =>
        Info(hex, "set_climate_keeper_mode", CommandPayloads.SetClimateKeeperMode(mode, manualOverride));

    // hvacSeatHeaterActions = 36 -> tag A2 02 { hvacSeatHeaterAction(1) 0A 04 { level Void, position Void } }
    // level Void tags: OFF(2) 12, LOW(3) 1A, MED(4) 22, HIGH(5) 2A
    [Theory]
    [InlineData(SeatHeaterLevel.Off, "12")]
    [InlineData(SeatHeaterLevel.Low, "1A")]
    [InlineData(SeatHeaterLevel.Medium, "22")]
    [InlineData(SeatHeaterLevel.High, "2A")]
    public void SetSeatHeaterLevels(SeatHeaterLevel level, string levelTag) =>
        Info($"12 09 A2 02 06 0A 04 {levelTag} 00 3A 00", "set_seat_heater", CommandPayloads.SetSeatHeater(SeatPosition.FrontLeft, level));

    // position Void tags: FL(7) 3A, FR(8) 42, RL(9) 4A, RL-back(10) 52, RC(11) 5A, RR(12) 62, RR-back(13) 6A, 3rd L(14) 72, 3rd R(15) 7A
    [Theory]
    [InlineData(SeatPosition.FrontLeft, "3A")]
    [InlineData(SeatPosition.FrontRight, "42")]
    [InlineData(SeatPosition.RearLeft, "4A")]
    [InlineData(SeatPosition.RearLeftBack, "52")]
    [InlineData(SeatPosition.RearCenter, "5A")]
    [InlineData(SeatPosition.RearRight, "62")]
    [InlineData(SeatPosition.RearRightBack, "6A")]
    [InlineData(SeatPosition.ThirdRowLeft, "72")]
    [InlineData(SeatPosition.ThirdRowRight, "7A")]
    public void SetSeatHeaterPositions(SeatPosition seat, string positionTag) =>
        Info($"12 09 A2 02 06 0A 04 2A 00 {positionTag} 00", "set_seat_heater", CommandPayloads.SetSeatHeater(seat, SeatHeaterLevel.High));

    // hvacSeatCoolerActions = 49 -> tag 8A 03 { hvacSeatCoolerAction(1) 0A 04 { seat_cooler_level(1) 08 v, seat_position(2) 10 v } }
    // level: Off 1, Low 2, Med 3, High 4; position: FL 1, FR 2
    [Theory]
    [InlineData(SeatCoolerLevel.Off, "01")]
    [InlineData(SeatCoolerLevel.Low, "02")]
    [InlineData(SeatCoolerLevel.Medium, "03")]
    [InlineData(SeatCoolerLevel.High, "04")]
    public void SetSeatCoolerLevels(SeatCoolerLevel level, string protoLevel) =>
        Info($"12 09 8A 03 06 0A 04 08 {protoLevel} 10 01", "set_seat_cooler", CommandPayloads.SetSeatCooler(FrontSeat.FrontLeft, level));

    [Fact]
    public void SetSeatCoolerFrontRight() =>
        Info("12 09 8A 03 06 0A 04 08 04 10 02", "set_seat_cooler", CommandPayloads.SetSeatCooler(FrontSeat.FrontRight, SeatCoolerLevel.High));

    // autoSeatClimateAction = 48 -> tag 82 03 { carseat(1) 0A { on(1) 08, seat_position(2) 10 v } }
    [Fact]
    public void SetAutoSeatClimateRightOn() =>
        Info("12 09 82 03 06 0A 04 08 01 10 02", "set_auto_seat_climate", CommandPayloads.SetAutoSeatClimate(FrontSeat.FrontRight, true));

    [Fact]
    public void SetAutoSeatClimateLeftOff() =>
        Info("12 07 82 03 04 0A 02 10 01", "set_auto_seat_climate", CommandPayloads.SetAutoSeatClimate(FrontSeat.FrontLeft, false));

    // hvacSteeringWheelHeaterAction = 13 -> tag 6A; power_on(1) 08
    [Fact] public void SetSteeringWheelHeaterOn() => Info("12 04 6A 02 08 01", "set_steering_wheel_heater", CommandPayloads.SetSteeringWheelHeater(true));

    [Fact] public void SetSteeringWheelHeaterOff() => Info("12 02 6A 00", "set_steering_wheel_heater", CommandPayloads.SetSteeringWheelHeater(false));

    // ---- Windows: vehicleControlWindowAction = 34 -> tag 92 02; vent(3) 1A 00, close(4) 22 00

    [Fact] public void VentWindows() => Info("12 05 92 02 02 1A 00", "vent_windows", CommandPayloads.VentWindows());

    [Fact] public void CloseWindows() => Info("12 05 92 02 02 22 00", "close_windows", CommandPayloads.CloseWindows());

    // ---- Validation

    [Fact]
    public void UndefinedEnumsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetCabinOverheatProtectionTemperature((CabinOverheatTemperature)7));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetClimateKeeperMode((ClimateKeeperMode)(-1), false));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetSeatHeater((SeatPosition)9, SeatHeaterLevel.Low));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetSeatHeater(SeatPosition.FrontLeft, (SeatHeaterLevel)4));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetSeatCooler((FrontSeat)2, SeatCoolerLevel.Low));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetSeatCooler(FrontSeat.FrontLeft, (SeatCoolerLevel)4));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetAutoSeatClimate((FrontSeat)5, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetScheduledDeparture(
            new ScheduledDeparture { DepartureTime = TimeOnly.MinValue, Preconditioning = (DeparturePolicy)3 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.SetScheduledDeparture(
            new ScheduledDeparture { DepartureTime = TimeOnly.MinValue, OffPeakCharging = (DeparturePolicy)(-1) }));
    }

    [Fact]
    public void UndefinedLocationBitsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.RemoveChargeSchedules((ScheduleLocations)8));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.RemovePreconditionSchedules((ScheduleLocations)16));
    }

    [Fact]
    public void UndefinedDayBitsThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.AddChargeSchedule(Charge(days: (ScheduleDays)128)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.AddPreconditionSchedule(Precondition(days: (ScheduleDays)256)));
    }

    [Fact]
    public void NullArgumentsThrow()
    {
        Assert.Throws<ArgumentNullException>(() => CommandPayloads.SetScheduledDeparture(null!));
        Assert.Throws<ArgumentNullException>(() => CommandPayloads.AddChargeSchedule(null!));
        Assert.Throws<ArgumentNullException>(() => CommandPayloads.AddPreconditionSchedule(null!));
    }

    [Theory]
    [InlineData(float.NaN, 0f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(90.5f, 0f)]
    [InlineData(-91f, 0f)]
    [InlineData(0f, float.NaN)]
    [InlineData(0f, float.NegativeInfinity)]
    [InlineData(0f, 180.5f)]
    [InlineData(0f, -181f)]
    public void InvalidCoordinatesThrow(float latitude, float longitude)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.AddChargeSchedule(Charge(latitude: latitude, longitude: longitude)));
        Assert.Throws<ArgumentOutOfRangeException>(() => CommandPayloads.AddPreconditionSchedule(Precondition(latitude: latitude, longitude: longitude)));
    }

    // -90 / 90 and -180 / 180 are accepted; lat 90.0f = 0x42B40000, lon 180.0f = 0x43340000
    [Fact]
    public void CoordinateBoundsAccepted() => Info(
        "12 11 9A 06 0E 08 01 30 01 3D 0000B442 45 00003443",
        "add_precondition_schedule",
        CommandPayloads.AddPreconditionSchedule(Precondition(days: ScheduleDays.None, latitude: 90f, longitude: 180f)));

    private static ChargeSchedule Charge(ScheduleDays days = ScheduleDays.All, float latitude = 0f, float longitude = 0f) =>
        new() { Id = 1, Days = days, Latitude = latitude, Longitude = longitude };

    private static PreconditionSchedule Precondition(ScheduleDays days = ScheduleDays.All, float latitude = 0f, float longitude = 0f) =>
        new() { Id = 1, Days = days, PreconditionTime = TimeOnly.MinValue, Latitude = latitude, Longitude = longitude };
}
