using Coil.Commands.Internal;
using Coil.Commands.Internal.Commands;
using Coil.Commands.Internal.Protocol;
using Coil.Commands.Internal.Transport;

namespace Coil.Commands;

/// <summary>
/// Default <see cref="ICoilCommands"/>: drives the session handshake, signing, transport and retries for
/// signed vehicle commands. Thread-safe; commands to the same VIN are serialized.
/// </summary>
public sealed class CoilCommands : ICoilCommands
{
    /// <summary>Creates the command client.</summary>
    /// <param name="fleetApiHttp">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host (same as for <c>CoilClient</c>).</param>
    /// <param name="tokenProvider">Supplies the user's bearer token (scopes <c>vehicle_cmds</c> / <c>vehicle_charging_cmds</c>).</param>
    /// <param name="keyPair">The P-256 key pair registered with Tesla and installed as virtual key on the vehicle. Not disposed by this class.</param>
    /// <param name="coilClient">Used to wake the vehicle when <see cref="CoilCommandsOptions.AutoWake"/> is enabled; may be null otherwise.</param>
    /// <param name="options">Options; defaults when null.</param>
    public CoilCommands(
        HttpClient fleetApiHttp,
        ITeslaTokenProvider tokenProvider,
        TeslaKeyPair keyPair,
        ICoilClient? coilClient = null,
        CoilCommandsOptions? options = null)
        : this(
            new FleetSignedCommandTransport(fleetApiHttp, tokenProvider),
            CreateProtocol(keyPair, options),
            coilClient,
            options ?? new CoilCommandsOptions(),
            TimeProvider.System)
    {
    }

    /// <summary>Test seam: inject transport, protocol and clock.</summary>
    internal CoilCommands(
        ISignedCommandTransport transport,
        IVehicleProtocol protocol,
        ICoilClient? coilClient,
        CoilCommandsOptions options,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (options.AutoWake && coilClient is null)
        {
            throw new ArgumentException($"{nameof(CoilCommandsOptions.AutoWake)} requires an {nameof(ICoilClient)}.", nameof(coilClient));
        }
    }

    private static VehicleProtocol CreateProtocol(TeslaKeyPair keyPair, CoilCommandsOptions? options)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        return new VehicleProtocol(
            keyPair,
            TimeProvider.System,
            SystemRandomSource.Instance,
            (options ?? new CoilCommandsOptions()).MessageLifetime);
    }

    /// <inheritdoc />
    public Task<CommandResult> LockAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.Lock, ct);

    /// <inheritdoc />
    public Task<CommandResult> UnlockAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.Unlock, ct);

    /// <inheritdoc />
    public Task<CommandResult> HonkHornAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.HonkHorn, ct);

    /// <inheritdoc />
    public Task<CommandResult> FlashLightsAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.FlashLights, ct);

    /// <inheritdoc />
    public Task<CommandResult> StartClimateAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.StartClimate, ct);

    /// <inheritdoc />
    public Task<CommandResult> StopClimateAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.StopClimate, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetTemperaturesAsync(string vin, float driverCelsius, float passengerCelsius, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetTemperatures(driverCelsius, passengerCelsius), ct);

    /// <inheritdoc />
    public Task<CommandResult> StartChargingAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.StartCharging, ct);

    /// <inheritdoc />
    public Task<CommandResult> StopChargingAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.StopCharging, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetChargeLimitAsync(string vin, int percent, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetChargeLimit(percent), ct);

    /// <inheritdoc />
    public Task<CommandResult> OpenChargePortAsync(string vin, CancellationToken ct = default) => ExecuteAsync(vin, CommandPayloads.OpenChargePort, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetChargingAmpsAsync(string vin, int amps, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetChargingAmps(amps), ct);

    /// <summary>
    /// Orchestration: validate VIN, build the payload (argument errors surface synchronously-ish before any I/O),
    /// serialize per VIN, get/establish the session for the command's domain, sign, send, open the reply,
    /// interpret; on retryable faults adopt the attached session info, wait <see cref="CoilCommandsOptions.RetryDelay"/>
    /// and re-sign until <see cref="CoilCommandsOptions.CommandTimeout"/>; on asleep optionally wake and poll.
    /// </summary>
    private Task<CommandResult> ExecuteAsync(string vin, Func<VehicleCommand> buildCommand, CancellationToken ct) =>
        throw new NotImplementedException("TODO(M3 orchestration): ExecuteAsync");
}
