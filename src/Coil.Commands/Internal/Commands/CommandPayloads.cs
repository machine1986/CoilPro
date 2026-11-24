using UniversalMessage;

namespace Coil.Commands.Internal.Commands;

/// <summary>A command payload and the vehicle domain it must be sent to.</summary>
/// <param name="Name">Short name for logs and errors, e.g. <c>set_charge_limit</c>.</param>
/// <param name="Domain"><see cref="Domain.VehicleSecurity"/> (VCSEC) or <see cref="Domain.Infotainment"/>.</param>
/// <param name="Payload">Serialized <c>VCSEC.UnsignedMessage</c> (VCSEC) or <c>CarServer.Action</c> (Infotainment).</param>
internal sealed record VehicleCommand(string Name, Domain Domain, byte[] Payload);

/// <summary>
/// Builds the protobuf payload for each supported command. FROZEN CONTRACT (M3 stage 0);
/// message paths in <c>docs/m3-command-protocol.md</c> §2.
/// </summary>
internal static class CommandPayloads
{
    public static VehicleCommand Lock() => throw NotYet();

    public static VehicleCommand Unlock() => throw NotYet();

    public static VehicleCommand HonkHorn() => throw NotYet();

    public static VehicleCommand FlashLights() => throw NotYet();

    public static VehicleCommand StartClimate() => throw NotYet();

    public static VehicleCommand StopClimate() => throw NotYet();

    /// <exception cref="ArgumentOutOfRangeException">Temperature outside the vehicle's supported range.</exception>
    public static VehicleCommand SetTemperatures(float driverCelsius, float passengerCelsius) => throw NotYet();

    public static VehicleCommand StartCharging() => throw NotYet();

    public static VehicleCommand StopCharging() => throw NotYet();

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="percent"/> outside 50–100.</exception>
    public static VehicleCommand SetChargeLimit(int percent) => throw NotYet();

    public static VehicleCommand OpenChargePort() => throw NotYet();

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="amps"/> not positive.</exception>
    public static VehicleCommand SetChargingAmps(int amps) => throw NotYet();

    private static NotImplementedException NotYet() => new("TODO(M3 commands): payload builder");
}

/// <summary>
/// Turns the decrypted reply payload into a <see cref="CommandResult"/>. FROZEN CONTRACT (M3 stage 0).
/// </summary>
internal static class ReplyInterpreter
{
    /// <summary>
    /// Infotainment: parse <c>CarServer.Response</c>; <c>actionStatus.result == ERROR</c> → refused with
    /// <c>result_reason.plain_text</c>. VCSEC: parse <c>VCSEC.FromVCSECMessage</c>; empty → success;
    /// <c>nominalError</c> → refused with the error name.
    /// </summary>
    /// <exception cref="TeslaCommandException">
    /// VCSEC <c>commandStatus</c> WAIT → thrown with <see cref="TeslaMessageFault.Busy"/> so the orchestrator retries;
    /// unparseable payload → thrown without fault.
    /// </exception>
    public static CommandResult Interpret(Domain domain, ReadOnlySpan<byte> plaintext) =>
        throw new NotImplementedException("TODO(M3 commands): reply interpreter");
}
