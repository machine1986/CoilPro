namespace CoilPro.Commands.Internal.Commands;

internal static partial class CommandPayloads
{
    public static VehicleCommand RemoteStartDrive() => throw new NotImplementedException("TODO(v0.2 commands-a): RemoteStartDrive");

    public static VehicleCommand OpenFrunk() => throw new NotImplementedException("TODO(v0.2 commands-a): OpenFrunk");

    public static VehicleCommand ActuateRearTrunk() => throw new NotImplementedException("TODO(v0.2 commands-a): ActuateRearTrunk");

    public static VehicleCommand CloseRearTrunk() => throw new NotImplementedException("TODO(v0.2 commands-a): CloseRearTrunk");

    public static VehicleCommand AutoSecureModelX() => throw new NotImplementedException("TODO(v0.2 commands-a): AutoSecureModelX");

    public static VehicleCommand OpenTonneau() => throw new NotImplementedException("TODO(v0.2 commands-a): OpenTonneau");

    public static VehicleCommand CloseTonneau() => throw new NotImplementedException("TODO(v0.2 commands-a): CloseTonneau");

    public static VehicleCommand StopTonneau() => throw new NotImplementedException("TODO(v0.2 commands-a): StopTonneau");

    public static VehicleCommand CloseChargePort() => throw new NotImplementedException("TODO(v0.2 commands-a): CloseChargePort");

    public static VehicleCommand ChargeStandardRange() => throw new NotImplementedException("TODO(v0.2 commands-a): ChargeStandardRange");

    public static VehicleCommand ChargeMaxRange() => throw new NotImplementedException("TODO(v0.2 commands-a): ChargeMaxRange");

    public static VehicleCommand SetScheduledCharging(bool enabled, TimeOnly time) => throw new NotImplementedException("TODO(v0.2 commands-a): SetScheduledCharging");

    public static VehicleCommand SetScheduledDeparture(ScheduledDeparture departure) => throw new NotImplementedException("TODO(v0.2 commands-a): SetScheduledDeparture");

    public static VehicleCommand ClearScheduledDeparture() => throw new NotImplementedException("TODO(v0.2 commands-a): ClearScheduledDeparture");

    public static VehicleCommand AddChargeSchedule(ChargeSchedule schedule) => throw new NotImplementedException("TODO(v0.2 commands-a): AddChargeSchedule");

    public static VehicleCommand RemoveChargeSchedule(ulong scheduleId) => throw new NotImplementedException("TODO(v0.2 commands-a): RemoveChargeSchedule");

    public static VehicleCommand RemoveChargeSchedules(ScheduleLocations locations) => throw new NotImplementedException("TODO(v0.2 commands-a): RemoveChargeSchedules");

    public static VehicleCommand AddPreconditionSchedule(PreconditionSchedule schedule) => throw new NotImplementedException("TODO(v0.2 commands-a): AddPreconditionSchedule");

    public static VehicleCommand RemovePreconditionSchedule(ulong scheduleId) => throw new NotImplementedException("TODO(v0.2 commands-a): RemovePreconditionSchedule");

    public static VehicleCommand RemovePreconditionSchedules(ScheduleLocations locations) => throw new NotImplementedException("TODO(v0.2 commands-a): RemovePreconditionSchedules");

    public static VehicleCommand SetLowPowerMode(bool enabled) => throw new NotImplementedException("TODO(v0.2 commands-a): SetLowPowerMode");

    public static VehicleCommand SetKeepAccessoryPowerMode(bool enabled) => throw new NotImplementedException("TODO(v0.2 commands-a): SetKeepAccessoryPowerMode");

    public static VehicleCommand SetPreconditioningMax(bool on, bool manualOverride) => throw new NotImplementedException("TODO(v0.2 commands-a): SetPreconditioningMax");

    public static VehicleCommand SetBioweaponMode(bool on, bool manualOverride) => throw new NotImplementedException("TODO(v0.2 commands-a): SetBioweaponMode");

    public static VehicleCommand SetCabinOverheatProtection(bool on, bool fanOnly) => throw new NotImplementedException("TODO(v0.2 commands-a): SetCabinOverheatProtection");

    public static VehicleCommand SetCabinOverheatProtectionTemperature(CabinOverheatTemperature temperature) => throw new NotImplementedException("TODO(v0.2 commands-a): SetCabinOverheatProtectionTemperature");

    public static VehicleCommand SetClimateKeeperMode(ClimateKeeperMode mode, bool manualOverride) => throw new NotImplementedException("TODO(v0.2 commands-a): SetClimateKeeperMode");

    public static VehicleCommand SetSeatHeater(SeatPosition seat, SeatHeaterLevel level) => throw new NotImplementedException("TODO(v0.2 commands-a): SetSeatHeater");

    public static VehicleCommand SetSeatCooler(FrontSeat seat, SeatCoolerLevel level) => throw new NotImplementedException("TODO(v0.2 commands-a): SetSeatCooler");

    public static VehicleCommand SetAutoSeatClimate(FrontSeat seat, bool on) => throw new NotImplementedException("TODO(v0.2 commands-a): SetAutoSeatClimate");

    public static VehicleCommand SetSteeringWheelHeater(bool on) => throw new NotImplementedException("TODO(v0.2 commands-a): SetSteeringWheelHeater");

    public static VehicleCommand VentWindows() => throw new NotImplementedException("TODO(v0.2 commands-a): VentWindows");

    public static VehicleCommand CloseWindows() => throw new NotImplementedException("TODO(v0.2 commands-a): CloseWindows");
}
