namespace Coil.Commands;

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
public interface ICoilCommands
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
}
