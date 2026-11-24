// Port of teslamotors/vehicle-command pkg/vehicle/{actions,vcsec,charge,climate}.go (Apache-2.0).
using Google.Protobuf;
using Cs = global::CarServer;
using Vc = global::VCSEC;
using UniversalMessage;

namespace CoilPro.Commands.Internal.Commands;

// Security/closures, charging and schedules, climate and seats, windows.
internal static partial class CommandPayloads
{
    // ---- Security and closures (VCSEC) ------------------------------------------------------------------------

    /// <summary>Starts keyless driving (<c>RKE_ACTION_REMOTE_DRIVE</c>).</summary>
    public static VehicleCommand RemoteStartDrive() => Rke("remote_start_drive", Vc.RKEAction_E.RkeActionRemoteDrive);

    /// <summary>Opens the frunk (<c>frontTrunk = MOVE</c>; there is no remote frunk close).</summary>
    public static VehicleCommand OpenFrunk() =>
        Closure("open_frunk", new Vc.ClosureMoveRequest { FrontTrunk = Vc.ClosureMoveType_E.ClosureMoveTypeMove });

    /// <summary>Toggles the rear trunk (<c>rearTrunk = MOVE</c>, like Go's <c>ActuateTrunk</c>/<c>OpenTrunk</c>).</summary>
    public static VehicleCommand ActuateRearTrunk() =>
        Closure("actuate_rear_trunk", new Vc.ClosureMoveRequest { RearTrunk = Vc.ClosureMoveType_E.ClosureMoveTypeMove });

    /// <summary>Closes the rear trunk (<c>rearTrunk = CLOSE</c>; powered liftgates only).</summary>
    public static VehicleCommand CloseRearTrunk() =>
        Closure("close_rear_trunk", new Vc.ClosureMoveRequest { RearTrunk = Vc.ClosureMoveType_E.ClosureMoveTypeClose });

    /// <summary>Model X: closes the falcon-wing doors and locks (<c>RKE_ACTION_AUTO_SECURE_VEHICLE</c>).</summary>
    public static VehicleCommand AutoSecureModelX() => Rke("auto_secure_model_x", Vc.RKEAction_E.RkeActionAutoSecureVehicle);

    /// <summary>Cybertruck: opens the tonneau cover.</summary>
    public static VehicleCommand OpenTonneau() => Tonneau("open_tonneau", Vc.ClosureMoveType_E.ClosureMoveTypeOpen);

    /// <summary>Cybertruck: closes the tonneau cover.</summary>
    public static VehicleCommand CloseTonneau() => Tonneau("close_tonneau", Vc.ClosureMoveType_E.ClosureMoveTypeClose);

    /// <summary>Cybertruck: stops the tonneau cover.</summary>
    public static VehicleCommand StopTonneau() => Tonneau("stop_tonneau", Vc.ClosureMoveType_E.ClosureMoveTypeStop);

    // ---- Charging ---------------------------------------------------------------------------------------------

    /// <summary>Closes the charge port door.</summary>
    public static VehicleCommand CloseChargePort() =>
        Infotainment("close_charge_port", new Cs.VehicleAction { ChargePortDoorClose = new Cs.ChargePortDoorClose() });

    /// <summary>Charges to the standard-range limit.</summary>
    public static VehicleCommand ChargeStandardRange() =>
        Infotainment("charge_standard", new Cs.VehicleAction { ChargingStartStopAction = new Cs.ChargingStartStopAction { StartStandard = new Cs.Void() } });

    /// <summary>Charges to the max-range limit.</summary>
    public static VehicleCommand ChargeMaxRange() =>
        Infotainment("charge_max_range", new Cs.VehicleAction { ChargingStartStopAction = new Cs.ChargingStartStopAction { StartMaxRange = new Cs.Void() } });

    /// <summary>Legacy scheduled charging (minutes after midnight).</summary>
    public static VehicleCommand SetScheduledCharging(bool enabled, TimeOnly time) =>
        Infotainment("set_scheduled_charging", new Cs.VehicleAction
        {
            ScheduledChargingAction = new Cs.ScheduledChargingAction { Enabled = enabled, ChargingTime = Minutes(time) },
        });

    /// <summary>
    /// Legacy scheduled departure. <c>enabled</c> is always true; <see cref="DeparturePolicy.Off"/> leaves the
    /// policy field unset (as Go does).
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="departure"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A policy is not a defined <see cref="DeparturePolicy"/>.</exception>
    public static VehicleCommand SetScheduledDeparture(ScheduledDeparture departure)
    {
        ArgumentNullException.ThrowIfNull(departure);

        var action = new Cs.ScheduledDepartureAction
        {
            Enabled = true,
            DepartureTime = Minutes(departure.DepartureTime),
            OffPeakHoursEndTime = departure.OffPeakHoursEndTime is { } end ? Minutes(end) : 0,
        };

        switch (RequireDefined(departure.Preconditioning, nameof(departure.Preconditioning)))
        {
            case DeparturePolicy.AllWeek:
                action.PreconditioningTimes = new Cs.PreconditioningTimes { AllWeek = new Cs.Void() };
                break;
            case DeparturePolicy.WeekdaysOnly:
                action.PreconditioningTimes = new Cs.PreconditioningTimes { Weekdays = new Cs.Void() };
                break;
        }

        switch (RequireDefined(departure.OffPeakCharging, nameof(departure.OffPeakCharging)))
        {
            case DeparturePolicy.AllWeek:
                action.OffPeakChargingTimes = new Cs.OffPeakChargingTimes { AllWeek = new Cs.Void() };
                break;
            case DeparturePolicy.WeekdaysOnly:
                action.OffPeakChargingTimes = new Cs.OffPeakChargingTimes { Weekdays = new Cs.Void() };
                break;
        }

        return Infotainment("set_scheduled_departure", new Cs.VehicleAction { ScheduledDepartureAction = action });
    }

    /// <summary>Disables legacy scheduled departure (<c>enabled = false</c> only, as in Go).</summary>
    public static VehicleCommand ClearScheduledDeparture() =>
        Infotainment("clear_scheduled_departure", new Cs.VehicleAction { ScheduledDepartureAction = new Cs.ScheduledDepartureAction { Enabled = false } });

    /// <summary>Adds or replaces a location-bound charge schedule.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="schedule"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid days, or latitude/longitude not finite or out of range.</exception>
    public static VehicleCommand AddChargeSchedule(ChargeSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ValidateDays(schedule.Days, nameof(schedule.Days));
        ValidateLocation(schedule.Latitude, schedule.Longitude);

        return Infotainment("add_charge_schedule", new Cs.VehicleAction
        {
            AddChargeScheduleAction = new Cs.ChargeSchedule
            {
                Id = schedule.Id,
                DaysOfWeek = (int)schedule.Days,
                StartEnabled = schedule.StartTime is not null,
                StartTime = schedule.StartTime is { } start ? Minutes(start) : 0,
                EndEnabled = schedule.EndTime is not null,
                EndTime = schedule.EndTime is { } end ? Minutes(end) : 0,
                OneTime = schedule.OneTime,
                Enabled = schedule.Enabled,
                Latitude = schedule.Latitude,
                Longitude = schedule.Longitude,
            },
        });
    }

    /// <summary>Removes one charge schedule by id.</summary>
    public static VehicleCommand RemoveChargeSchedule(ulong scheduleId) =>
        Infotainment("remove_charge_schedule", new Cs.VehicleAction
        {
            RemoveChargeScheduleAction = new Cs.RemoveChargeScheduleAction { Id = scheduleId },
        });

    /// <summary>Removes all charge schedules at the given locations.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="locations"/> contains undefined bits.</exception>
    public static VehicleCommand RemoveChargeSchedules(ScheduleLocations locations)
    {
        ValidateLocations(locations);
        return Infotainment("remove_charge_schedules", new Cs.VehicleAction
        {
            BatchRemoveChargeSchedulesAction = new Cs.BatchRemoveChargeSchedulesAction
            {
                Home = locations.HasFlag(ScheduleLocations.Home),
                Work = locations.HasFlag(ScheduleLocations.Work),
                Other = locations.HasFlag(ScheduleLocations.Other),
            },
        });
    }

    /// <summary>Adds or replaces a location-bound preconditioning schedule.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="schedule"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Invalid days, or latitude/longitude not finite or out of range.</exception>
    public static VehicleCommand AddPreconditionSchedule(PreconditionSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        ValidateDays(schedule.Days, nameof(schedule.Days));
        ValidateLocation(schedule.Latitude, schedule.Longitude);

        return Infotainment("add_precondition_schedule", new Cs.VehicleAction
        {
            AddPreconditionScheduleAction = new Cs.PreconditionSchedule
            {
                Id = schedule.Id,
                DaysOfWeek = (int)schedule.Days,
                PreconditionTime = Minutes(schedule.PreconditionTime),
                OneTime = schedule.OneTime,
                Enabled = schedule.Enabled,
                Latitude = schedule.Latitude,
                Longitude = schedule.Longitude,
            },
        });
    }

    /// <summary>Removes one preconditioning schedule by id.</summary>
    public static VehicleCommand RemovePreconditionSchedule(ulong scheduleId) =>
        Infotainment("remove_precondition_schedule", new Cs.VehicleAction
        {
            RemovePreconditionScheduleAction = new Cs.RemovePreconditionScheduleAction { Id = scheduleId },
        });

    /// <summary>Removes all preconditioning schedules at the given locations.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="locations"/> contains undefined bits.</exception>
    public static VehicleCommand RemovePreconditionSchedules(ScheduleLocations locations)
    {
        ValidateLocations(locations);
        return Infotainment("remove_precondition_schedules", new Cs.VehicleAction
        {
            BatchRemovePreconditionSchedulesAction = new Cs.BatchRemovePreconditionSchedulesAction
            {
                Home = locations.HasFlag(ScheduleLocations.Home),
                Work = locations.HasFlag(ScheduleLocations.Work),
                Other = locations.HasFlag(ScheduleLocations.Other),
            },
        });
    }

    /// <summary>Enables or disables low power mode.</summary>
    public static VehicleCommand SetLowPowerMode(bool enabled) =>
        Infotainment("set_low_power_mode", new Cs.VehicleAction { SetLowPowerModeAction = new Cs.SetLowPowerModeAction { LowPowerMode = enabled } });

    /// <summary>Enables or disables keep-accessory-power mode.</summary>
    public static VehicleCommand SetKeepAccessoryPowerMode(bool enabled) =>
        Infotainment("set_keep_accessory_power_mode", new Cs.VehicleAction
        {
            SetKeepAccessoryPowerModeAction = new Cs.SetKeepAccessoryPowerModeAction { KeepAccessoryPowerMode = enabled },
        });

    // ---- Climate and seats ------------------------------------------------------------------------------------

    /// <summary>Max preconditioning on/off (<c>manual_override_mode</c> is never set, like Go).</summary>
    public static VehicleCommand SetPreconditioningMax(bool on, bool manualOverride) =>
        Infotainment("set_preconditioning_max", new Cs.VehicleAction
        {
            HvacSetPreconditioningMaxAction = new Cs.HvacSetPreconditioningMaxAction { On = on, ManualOverride = manualOverride },
        });

    /// <summary>Bioweapon defense mode on/off.</summary>
    public static VehicleCommand SetBioweaponMode(bool on, bool manualOverride) =>
        Infotainment("set_bioweapon_mode", new Cs.VehicleAction
        {
            HvacBioweaponModeAction = new Cs.HvacBioweaponModeAction { On = on, ManualOverride = manualOverride },
        });

    /// <summary>Cabin overheat protection on/off, optionally fan only.</summary>
    public static VehicleCommand SetCabinOverheatProtection(bool on, bool fanOnly) =>
        Infotainment("set_cabin_overheat_protection", new Cs.VehicleAction
        {
            SetCabinOverheatProtectionAction = new Cs.SetCabinOverheatProtectionAction { On = on, FanOnly = fanOnly },
        });

    /// <summary>
    /// Cabin overheat protection activation temperature. Low/Medium/High map to proto 1/2/3
    /// (Go passes the REST number straight through, so REST 0 would become Unspecified).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="temperature"/> is not a defined value.</exception>
    public static VehicleCommand SetCabinOverheatProtectionTemperature(CabinOverheatTemperature temperature)
    {
        var proto = RequireDefined(temperature, nameof(temperature)) switch
        {
            CabinOverheatTemperature.Low => Cs.ClimateState.Types.CopActivationTemp.Low,
            CabinOverheatTemperature.Medium => Cs.ClimateState.Types.CopActivationTemp.Medium,
            _ => Cs.ClimateState.Types.CopActivationTemp.High,
        };

        return Infotainment("set_cop_temp", new Cs.VehicleAction
        {
            SetCopTempAction = new Cs.SetCopTempAction { CopActivationTemp = proto },
        });
    }

    /// <summary>Climate keeper mode (Off 0, Keep 1, Dog 2, Camp 3).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a defined value.</exception>
    public static VehicleCommand SetClimateKeeperMode(ClimateKeeperMode mode, bool manualOverride)
    {
        var proto = RequireDefined(mode, nameof(mode)) switch
        {
            ClimateKeeperMode.Off => Cs.HvacClimateKeeperAction.Types.ClimateKeeperAction_E.ClimateKeeperActionOff,
            ClimateKeeperMode.Keep => Cs.HvacClimateKeeperAction.Types.ClimateKeeperAction_E.ClimateKeeperActionOn,
            ClimateKeeperMode.Dog => Cs.HvacClimateKeeperAction.Types.ClimateKeeperAction_E.ClimateKeeperActionDog,
            _ => Cs.HvacClimateKeeperAction.Types.ClimateKeeperAction_E.ClimateKeeperActionCamp,
        };

        return Infotainment("set_climate_keeper_mode", new Cs.VehicleAction
        {
            HvacClimateKeeperAction = new Cs.HvacClimateKeeperAction { ClimateKeeperAction = proto, ManualOverride = manualOverride },
        });
    }

    /// <summary>Sets one seat's heater level (both oneofs hold empty <c>Void</c> members).</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seat"/> or <paramref name="level"/> is not a defined value.</exception>
    public static VehicleCommand SetSeatHeater(SeatPosition seat, SeatHeaterLevel level)
    {
        var action = new Cs.HvacSeatHeaterActions.Types.HvacSeatHeaterAction();

        switch (RequireDefined(level, nameof(level)))
        {
            case SeatHeaterLevel.Off: action.SEATHEATEROFF = new Cs.Void(); break;
            case SeatHeaterLevel.Low: action.SEATHEATERLOW = new Cs.Void(); break;
            case SeatHeaterLevel.Medium: action.SEATHEATERMED = new Cs.Void(); break;
            default: action.SEATHEATERHIGH = new Cs.Void(); break;
        }

        switch (RequireDefined(seat, nameof(seat)))
        {
            case SeatPosition.FrontLeft: action.CARSEATFRONTLEFT = new Cs.Void(); break;
            case SeatPosition.FrontRight: action.CARSEATFRONTRIGHT = new Cs.Void(); break;
            case SeatPosition.RearLeft: action.CARSEATREARLEFT = new Cs.Void(); break;
            case SeatPosition.RearLeftBack: action.CARSEATREARLEFTBACK = new Cs.Void(); break;
            case SeatPosition.RearCenter: action.CARSEATREARCENTER = new Cs.Void(); break;
            case SeatPosition.RearRight: action.CARSEATREARRIGHT = new Cs.Void(); break;
            case SeatPosition.RearRightBack: action.CARSEATREARRIGHTBACK = new Cs.Void(); break;
            case SeatPosition.ThirdRowLeft: action.CARSEATTHIRDROWLEFT = new Cs.Void(); break;
            default: action.CARSEATTHIRDROWRIGHT = new Cs.Void(); break;
        }

        var actions = new Cs.HvacSeatHeaterActions();
        actions.HvacSeatHeaterAction.Add(action);
        return Infotainment("set_seat_heater", new Cs.VehicleAction { HvacSeatHeaterActions = actions });
    }

    /// <summary>Sets a front seat's cooler (ventilation) level; Off/Low/Medium/High = proto 1/2/3/4.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seat"/> or <paramref name="level"/> is not a defined value.</exception>
    public static VehicleCommand SetSeatCooler(FrontSeat seat, SeatCoolerLevel level)
    {
        var protoLevel = RequireDefined(level, nameof(level)) switch
        {
            SeatCoolerLevel.Off => Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerLevel_E.HvacSeatCoolerLevelOff,
            SeatCoolerLevel.Low => Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerLevel_E.HvacSeatCoolerLevelLow,
            SeatCoolerLevel.Medium => Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerLevel_E.HvacSeatCoolerLevelMed,
            _ => Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerLevel_E.HvacSeatCoolerLevelHigh,
        };
        var protoSeat = RequireDefined(seat, nameof(seat)) == FrontSeat.FrontLeft
            ? Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerPosition_E.HvacSeatCoolerPositionFrontLeft
            : Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerPosition_E.HvacSeatCoolerPositionFrontRight;

        var actions = new Cs.HvacSeatCoolerActions();
        actions.HvacSeatCoolerAction.Add(new Cs.HvacSeatCoolerActions.Types.HvacSeatCoolerAction
        {
            SeatCoolerLevel = protoLevel,
            SeatPosition = protoSeat,
        });
        return Infotainment("set_seat_cooler", new Cs.VehicleAction { HvacSeatCoolerActions = actions });
    }

    /// <summary>Auto seat climate for a front seat.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seat"/> is not a defined value.</exception>
    public static VehicleCommand SetAutoSeatClimate(FrontSeat seat, bool on)
    {
        var protoSeat = RequireDefined(seat, nameof(seat)) == FrontSeat.FrontLeft
            ? Cs.AutoSeatClimateAction.Types.AutoSeatPosition_E.AutoSeatPositionFrontLeft
            : Cs.AutoSeatClimateAction.Types.AutoSeatPosition_E.AutoSeatPositionFrontRight;

        var action = new Cs.AutoSeatClimateAction();
        action.Carseat.Add(new Cs.AutoSeatClimateAction.Types.CarSeat { On = on, SeatPosition = protoSeat });
        return Infotainment("set_auto_seat_climate", new Cs.VehicleAction { AutoSeatClimateAction = action });
    }

    /// <summary>Steering wheel heater on/off.</summary>
    public static VehicleCommand SetSteeringWheelHeater(bool on) =>
        Infotainment("set_steering_wheel_heater", new Cs.VehicleAction
        {
            HvacSteeringWheelHeaterAction = new Cs.HvacSteeringWheelHeaterAction { PowerOn = on },
        });

    // ---- Windows ----------------------------------------------------------------------------------------------

    /// <summary>Vents all windows (<c>vent(3)</c>; the reserved location field is not sent).</summary>
    public static VehicleCommand VentWindows() =>
        Infotainment("vent_windows", new Cs.VehicleAction { VehicleControlWindowAction = new Cs.VehicleControlWindowAction { Vent = new Cs.Void() } });

    /// <summary>Closes all windows (<c>close(4)</c>).</summary>
    public static VehicleCommand CloseWindows() =>
        Infotainment("close_windows", new Cs.VehicleAction { VehicleControlWindowAction = new Cs.VehicleControlWindowAction { Close = new Cs.Void() } });

    // ---- Helpers ----------------------------------------------------------------------------------------------

    private static VehicleCommand Closure(string name, Vc.ClosureMoveRequest request) =>
        new(name, Domain.VehicleSecurity, new Vc.UnsignedMessage { ClosureMoveRequest = request }.ToByteArray());

    private static VehicleCommand Tonneau(string name, Vc.ClosureMoveType_E move) =>
        Closure(name, new Vc.ClosureMoveRequest { Tonneau = move });

    private static int Minutes(TimeOnly time) => time.Hour * 60 + time.Minute;

    private static T RequireDefined<T>(T value, string paramName) where T : struct, Enum =>
        Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(paramName, value, $"Not a defined {typeof(T).Name} value.");

    private static void ValidateDays(ScheduleDays days, string paramName)
    {
        if (((int)days & ~(int)ScheduleDays.All) != 0)
            throw new ArgumentOutOfRangeException(paramName, days, "Only the bits Sunday (1) through Saturday (64) may be set.");
    }

    private static void ValidateLocations(ScheduleLocations locations)
    {
        const int all = (int)(ScheduleLocations.Home | ScheduleLocations.Work | ScheduleLocations.Other);
        if (((int)locations & ~all) != 0)
            throw new ArgumentOutOfRangeException(nameof(locations), locations, "Only Home, Work and Other may be set.");
    }

    private static void ValidateLocation(float latitude, float longitude)
    {
        if (!float.IsFinite(latitude) || latitude is < -90f or > 90f)
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be a finite number between -90 and 90.");
        if (!float.IsFinite(longitude) || longitude is < -180f or > 180f)
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be a finite number between -180 and 180.");
    }
}
