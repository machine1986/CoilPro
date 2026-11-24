using Coil.Commands.Internal;
using Coil.Commands.Internal.Commands;
using Coil.Commands.Internal.Dispatch;
using Coil.Commands.Internal.Protocol;
using Coil.Commands.Internal.Transport;
using UniversalMessage;

namespace Coil.Commands;

/// <summary>
/// Default <see cref="ICoilCommands"/>: drives the session handshake, signing, transport and retries for
/// signed vehicle commands. Thread-safe; commands to the same VIN are serialized.
/// </summary>
public sealed class CoilCommands : ICoilCommands
{
    private readonly ISignedCommandTransport _transport;
    private readonly IVehicleProtocol _protocol;
    private readonly ICoilClient? _coilClient;
    private readonly CoilCommandsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<Domain, ReadOnlyMemory<byte>, CommandResult> _interpreter;
    private readonly VehicleSessionCache _cache = new();

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

    /// <summary>Test seam: inject transport, protocol, clock and (optionally) the reply interpreter.</summary>
    internal CoilCommands(
        ISignedCommandTransport transport,
        IVehicleProtocol protocol,
        ICoilClient? coilClient,
        CoilCommandsOptions options,
        TimeProvider timeProvider,
        Func<Domain, ReadOnlyMemory<byte>, CommandResult>? interpreter = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (options.AutoWake && coilClient is null)
        {
            throw new ArgumentException($"{nameof(CoilCommandsOptions.AutoWake)} requires an {nameof(ICoilClient)}.", nameof(coilClient));
        }

        _transport = transport;
        _protocol = protocol;
        _coilClient = coilClient;
        _options = options;
        _timeProvider = timeProvider;
        _interpreter = interpreter ?? ((domain, payload) => ReplyInterpreter.Interpret(domain, payload.Span));
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
    internal async Task<CommandResult> ExecuteAsync(string vin, Func<VehicleCommand> buildCommand, CancellationToken ct)
    {
        if (vin is null || vin.Length != 17)
        {
            throw new ArgumentException("A VIN must be exactly 17 characters.", nameof(vin));
        }

        var command = buildCommand();
        using var gate = await _cache.LockAsync(vin, ct).ConfigureAwait(false);

        try
        {
            return await RunWithDeadlineAsync(vin, command, ct).ConfigureAwait(false);
        }
        catch (TeslaVehicleAsleepException) when (_options.AutoWake && _coilClient is not null)
        {
            await WakeAsync(vin, ct).ConfigureAwait(false);
            return await RunWithDeadlineAsync(vin, command, ct).ConfigureAwait(false);
        }
    }

    /// <summary>One attempt at the whole command (handshake, sign, send, retries) bounded by <c>CommandTimeout</c>.</summary>
    private async Task<CommandResult> RunWithDeadlineAsync(string vin, VehicleCommand command, CancellationToken ct)
    {
        using var timeout = new CancellationTokenSource(_options.CommandTimeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        var state = new RunState();
        try
        {
            return await RunAsync(vin, command, state, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TeslaCommandException(
                $"Command '{command.Name}' timed out after {_options.CommandTimeout} waiting for vehicle {vin}" +
                (state.LastFault is { } fault ? $" (last fault: {fault})." : "."),
                vin,
                state.LastFault);
        }
    }

    private async Task<CommandResult> RunAsync(string vin, VehicleCommand command, RunState state, CancellationToken ct)
    {
        while (true)
        {
            var session = await GetSessionAsync(vin, command.Domain, state, ct).ConfigureAwait(false);
            var prepared = _protocol.Sign(session, vin, command.Payload);
            RoutableMessage? reply = null;
            var opened = false;
            try
            {
                reply = await _transport.SendAsync(vin, prepared.Message, ct).ConfigureAwait(false);
                var plaintext = _protocol.OpenReply(session, vin, prepared, reply);
                opened = true;
                return _interpreter(command.Domain, plaintext);
            }
            catch (TeslaKeyNotPairedException)
            {
                _cache.Drop(vin, session);
                throw;
            }
            catch (TeslaCommandException ex) when (ex.Fault is { } fault && fault.IsRetryable() && reply is not null)
            {
                state.LastFault = fault;

                // A fault raised by OpenReply: adopt the session info the vehicle attached to it. A fault raised by
                // the interpreter (VCSEC "wait") carries no session info, so the session stays as it is.
                if (!opened && !_protocol.TryUpdateSession(session, vin, prepared, reply))
                {
                    _cache.Drop(vin, session);
                }

                await Task.Delay(_options.RetryDelay, _timeProvider, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task<VehicleSession> GetSessionAsync(string vin, Domain domain, RunState state, CancellationToken ct)
    {
        if (_cache.Get(vin, domain) is { } cached)
        {
            return cached;
        }

        while (true)
        {
            var request = _protocol.CreateSessionInfoRequest(domain);
            var sentAt = _timeProvider.GetUtcNow();
            var reply = await _transport.SendAsync(vin, request, ct).ConfigureAwait(false);
            try
            {
                var session = _protocol.EstablishSession(vin, request, reply, sentAt);
                _cache.Set(vin, session);
                return session;
            }
            catch (TeslaCommandException ex) when (ex is not TeslaKeyNotPairedException && ex.Fault is { } fault && fault.IsRetryable())
            {
                state.LastFault = fault;
                await Task.Delay(_options.RetryDelay, _timeProvider, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Sends <c>wake_up</c> once, then polls <c>GetVehiclesAsync</c> (a list call that neither wakes nor bills a
    /// wake-up) every <see cref="CoilCommandsOptions.WakePollInterval"/> until the vehicle reports state "online"
    /// or <see cref="CoilCommandsOptions.WakeTimeout"/> elapses.
    /// </summary>
    private async Task WakeAsync(string vin, CancellationToken ct)
    {
        var coil = _coilClient!;
        using var timeout = new CancellationTokenSource(_options.WakeTimeout, _timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);
        try
        {
            var vehicle = await coil.WakeUpAsync(vin, linked.Token).ConfigureAwait(false);
            while (!IsOnline(vehicle?.State))
            {
                await Task.Delay(_options.WakePollInterval, _timeProvider, linked.Token).ConfigureAwait(false);
                var vehicles = await coil.GetVehiclesAsync(linked.Token).ConfigureAwait(false);
                vehicle = vehicles.FirstOrDefault(v => string.Equals(v.Vin, vin, StringComparison.OrdinalIgnoreCase));
            }
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TeslaVehicleAsleepException(vin, detail: $"did not come online within {_options.WakeTimeout}");
        }
    }

    private static bool IsOnline(string? state) => string.Equals(state, "online", StringComparison.OrdinalIgnoreCase);

    private sealed class RunState
    {
        public TeslaMessageFault? LastFault { get; set; }
    }
}
