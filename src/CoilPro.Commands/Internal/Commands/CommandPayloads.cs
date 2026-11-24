// Port of teslamotors/vehicle-command pkg/vehicle/{security,actions,climate,charge,infotainment,vcsec}.go (Apache-2.0).
using System.Globalization;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using UniversalMessage;
using Cs = global::CarServer;
using Vc = global::VCSEC;

namespace CoilPro.Commands.Internal.Commands;

/// <summary>A command payload and the vehicle domain it must be sent to.</summary>
/// <param name="Name">Short name for logs and errors, e.g. <c>set_charge_limit</c>.</param>
/// <param name="Domain"><see cref="Domain.VehicleSecurity"/> (VCSEC) or <see cref="Domain.Infotainment"/>.</param>
/// <param name="Payload">Serialized <c>VCSEC.UnsignedMessage</c> (VCSEC) or <c>CarServer.Action</c> (Infotainment).</param>
internal sealed record VehicleCommand(string Name, Domain Domain, byte[] Payload);

/// <summary>
/// Builds the protobuf payload for each supported command. Field-for-field equivalent to the Go reference
/// (message paths in <c>docs/m3-command-protocol.md</c> §2).
/// </summary>
internal static partial class CommandPayloads
{
    /// <summary>Locks the vehicle (<c>RKE_ACTION_LOCK</c>).</summary>
    public static VehicleCommand Lock() => Rke("lock", Vc.RKEAction_E.RkeActionLock);

    /// <summary>Unlocks the vehicle (<c>RKE_ACTION_UNLOCK</c>; serializes as <c>10 00</c> because it is a oneof member).</summary>
    public static VehicleCommand Unlock() => Rke("unlock", Vc.RKEAction_E.RkeActionUnlock);

    /// <summary>Honks the horn.</summary>
    public static VehicleCommand HonkHorn() =>
        Infotainment("honk_horn", new Cs.VehicleAction { VehicleControlHonkHornAction = new Cs.VehicleControlHonkHornAction() });

    /// <summary>Flashes the lights.</summary>
    public static VehicleCommand FlashLights() =>
        Infotainment("flash_lights", new Cs.VehicleAction { VehicleControlFlashLightsAction = new Cs.VehicleControlFlashLightsAction() });

    /// <summary>Turns climate control on.</summary>
    public static VehicleCommand StartClimate() => Climate("start_climate", true);

    /// <summary>Turns climate control off.</summary>
    public static VehicleCommand StopClimate() => Climate("stop_climate", false);

    /// <summary>
    /// Sets driver and passenger cabin temperature. Like the Go reference, no range is enforced client-side
    /// (the vehicle clamps or refuses out-of-range values); only non-finite values are rejected. Go always sets
    /// <c>level = TEMP_MAX</c>, which is mirrored here.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A temperature is NaN or infinite.</exception>
    public static VehicleCommand SetTemperatures(float driverCelsius, float passengerCelsius)
    {
        if (!float.IsFinite(driverCelsius))
            throw new ArgumentOutOfRangeException(nameof(driverCelsius), driverCelsius, "Temperature must be a finite number.");
        if (!float.IsFinite(passengerCelsius))
            throw new ArgumentOutOfRangeException(nameof(passengerCelsius), passengerCelsius, "Temperature must be a finite number.");

        return Infotainment("set_temperatures", new Cs.VehicleAction
        {
            HvacTemperatureAdjustmentAction = new Cs.HvacTemperatureAdjustmentAction
            {
                DriverTempCelsius = driverCelsius,
                PassengerTempCelsius = passengerCelsius,
                Level = new Cs.HvacTemperatureAdjustmentAction.Types.Temperature { TEMPMAX = new Cs.Void() },
            },
        });
    }

    /// <summary>Starts charging.</summary>
    public static VehicleCommand StartCharging() =>
        Infotainment("start_charging", new Cs.VehicleAction { ChargingStartStopAction = new Cs.ChargingStartStopAction { Start = new Cs.Void() } });

    /// <summary>Stops charging.</summary>
    public static VehicleCommand StopCharging() =>
        Infotainment("stop_charging", new Cs.VehicleAction { ChargingStartStopAction = new Cs.ChargingStartStopAction { Stop = new Cs.Void() } });

    /// <summary>Sets the charge limit in percent.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="percent"/> outside 50–100.</exception>
    public static VehicleCommand SetChargeLimit(int percent)
    {
        if (percent is < 50 or > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Charge limit must be between 50 and 100 percent.");

        return Infotainment("set_charge_limit", new Cs.VehicleAction { ChargingSetLimitAction = new Cs.ChargingSetLimitAction { Percent = percent } });
    }

    /// <summary>Opens the charge port door.</summary>
    public static VehicleCommand OpenChargePort() =>
        Infotainment("open_charge_port", new Cs.VehicleAction { ChargePortDoorOpen = new Cs.ChargePortDoorOpen() });

    /// <summary>Sets the charging current. Go imposes no upper bound; neither does this method.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amps"/> not positive.</exception>
    public static VehicleCommand SetChargingAmps(int amps)
    {
        if (amps <= 0)
            throw new ArgumentOutOfRangeException(nameof(amps), amps, "Charging amps must be positive.");

        return Infotainment("set_charging_amps", new Cs.VehicleAction { SetChargingAmpsAction = new Cs.SetChargingAmpsAction { ChargingAmps = amps } });
    }

    private static VehicleCommand Climate(string name, bool on) =>
        Infotainment(name, new Cs.VehicleAction { HvacAutoAction = new Cs.HvacAutoAction { PowerOn = on } });

    private static VehicleCommand Rke(string name, Vc.RKEAction_E action) =>
        new(name, Domain.VehicleSecurity, new Vc.UnsignedMessage { RKEAction = action }.ToByteArray());

    private static VehicleCommand Infotainment(string name, Cs.VehicleAction action) =>
        new(name, Domain.Infotainment, new Cs.Action { VehicleAction = action }.ToByteArray());
}

/// <summary>
/// Turns the decrypted reply payload into a <see cref="CommandResult"/>.
/// </summary>
internal static class ReplyInterpreter
{
    /// <summary>
    /// Infotainment: parse <c>CarServer.Response</c>; <c>actionStatus.result == ERROR</c> → refused with
    /// <c>result_reason.plain_text</c> ("unspecified error" when absent, as in Go). An empty payload parses as
    /// an empty response and counts as success (Go does the same). VCSEC: parse <c>VCSEC.FromVCSECMessage</c>;
    /// empty → success; <c>nominalError</c> → refused with the proto enum name (e.g. <c>VEHICLE_NOT_IN_PARK</c>).
    /// </summary>
    /// <exception cref="TeslaCommandException">
    /// VCSEC <c>commandStatus</c> WAIT → thrown with <see cref="TeslaMessageFault.Busy"/> so the orchestrator retries;
    /// VCSEC <c>commandStatus</c> ERROR → thrown with the fault detail; unparseable payload → thrown without fault.
    /// </exception>
    public static CommandResult Interpret(Domain domain, ReadOnlySpan<byte> plaintext) => domain switch
    {
        Domain.Infotainment => InterpretInfotainment(plaintext),
        Domain.VehicleSecurity => InterpretVcsec(plaintext),
        _ => throw new TeslaCommandException($"Cannot interpret a reply from domain {domain}."),
    };

    private static CommandResult InterpretInfotainment(ReadOnlySpan<byte> plaintext)
    {
        Cs.Response response;
        try
        {
            response = Cs.Response.Parser.ParseFrom(plaintext);
        }
        catch (InvalidProtocolBufferException ex)
        {
            throw new TeslaCommandException("Unable to parse the vehicle's infotainment reply.", innerException: ex);
        }

        if (response.ActionStatus is { Result: Cs.OperationStatus_E.Rror } status)
        {
            var reason = status.ResultReason?.PlainText;
            return CommandResult.Refused(string.IsNullOrEmpty(reason) ? "unspecified error" : reason);
        }

        return CommandResult.Succeeded;
    }

    private static CommandResult InterpretVcsec(ReadOnlySpan<byte> plaintext)
    {
        Vc.FromVCSECMessage message;
        try
        {
            message = Vc.FromVCSECMessage.Parser.ParseFrom(plaintext);
        }
        catch (InvalidProtocolBufferException ex)
        {
            throw new TeslaCommandException("Unable to parse the vehicle's security reply.", innerException: ex);
        }

        if (message.SubMessageCase == Vc.FromVCSECMessage.SubMessageOneofCase.NominalError)
        {
            return CommandResult.Refused(EnumName(message.NominalError.GenericError, "GENERICERROR_"));
        }

        if (message.SubMessageCase == Vc.FromVCSECMessage.SubMessageOneofCase.CommandStatus)
        {
            var status = message.CommandStatus;
            switch (status.OperationStatus)
            {
                case Vc.OperationStatus_E.OperationstatusWait:
                    throw new TeslaCommandException(
                        "The vehicle's security domain is busy (OPERATIONSTATUS_WAIT).", fault: TeslaMessageFault.Busy);

                case Vc.OperationStatus_E.Rror:
                    throw new TeslaCommandException("The vehicle's security domain reported an error" + ErrorDetail(status) + ".");
            }
        }

        return CommandResult.Succeeded;
    }

    private static string ErrorDetail(Vc.CommandStatus status)
    {
        if (status.SubMessageCase == Vc.CommandStatus.SubMessageOneofCase.WhitelistOperationStatus
            && status.WhitelistOperationStatus.WhitelistOperationInformation != Vc.WhitelistOperation_information_E.WhitelistoperationInformationNone)
        {
            return ": " + EnumName(status.WhitelistOperationStatus.WhitelistOperationInformation, "WHITELISTOPERATION_INFORMATION_");
        }

        if (status.SubMessageCase == Vc.CommandStatus.SubMessageOneofCase.SignedMessageStatus)
        {
            return ": " + EnumName(status.SignedMessageStatus.SignedMessageInformation, "SIGNEDMESSAGE_INFORMATION_");
        }

        return string.Empty;
    }

    /// <summary>The enum member's name from the .proto (generated C# names are mangled), minus a common prefix.</summary>
    private static string EnumName<T>(T value, string prefix) where T : struct, Enum
    {
        var field = typeof(T).GetField(value.ToString());
        var name = field?.GetCustomAttributes(typeof(OriginalNameAttribute), false)
            .OfType<OriginalNameAttribute>().FirstOrDefault()?.Name;
        if (name is null)
            return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return name.StartsWith(prefix, StringComparison.Ordinal) ? name[prefix.Length..] : name;
    }
}
