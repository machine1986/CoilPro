using CoilPro.Commands.Internal;
using CoilPro.Commands.Internal.Commands;
using CoilPro.Commands.Internal.Dispatch;
using CoilPro.Commands.Internal.Protocol;
using CoilPro.Commands.Internal.Transport;
using UniversalMessage;

namespace CoilPro.Commands;

/// <summary>
/// Default <see cref="ICoilProCommands"/>: drives the session handshake, signing, transport and retries for
/// signed vehicle commands. Thread-safe; commands to the same VIN are serialized.
/// </summary>
public sealed class CoilProCommands : ICoilProCommands
{
    private readonly ISignedCommandTransport _transport;
    private readonly IVehicleProtocol _protocol;
    private readonly ICoilProClient? _coilClient;
    private readonly CoilProCommandsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly Func<Domain, ReadOnlyMemory<byte>, CommandResult> _interpreter;
    private readonly VehicleSessionCache _cache = new();

    /// <summary>Creates the command client.</summary>
    /// <param name="fleetApiHttp">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host (same as for <c>CoilProClient</c>).</param>
    /// <param name="tokenProvider">Supplies the user's bearer token (scopes <c>vehicle_cmds</c> / <c>vehicle_charging_cmds</c>).</param>
    /// <param name="keyPair">The P-256 key pair registered with Tesla and installed as virtual key on the vehicle. Not disposed by this class.</param>
    /// <param name="coilClient">Used to wake the vehicle when <see cref="CoilProCommandsOptions.AutoWake"/> is enabled; may be null otherwise.</param>
    /// <param name="options">Options; defaults when null.</param>
    public CoilProCommands(
        HttpClient fleetApiHttp,
        ITeslaTokenProvider tokenProvider,
        TeslaKeyPair keyPair,
        ICoilProClient? coilClient = null,
        CoilProCommandsOptions? options = null)
        : this(
            new FleetSignedCommandTransport(fleetApiHttp, tokenProvider),
            CreateProtocol(keyPair, options),
            coilClient,
            options ?? new CoilProCommandsOptions(),
            TimeProvider.System)
    {
    }

    /// <summary>Test seam: inject transport, protocol, clock and (optionally) the reply interpreter.</summary>
    internal CoilProCommands(
        ISignedCommandTransport transport,
        IVehicleProtocol protocol,
        ICoilProClient? coilClient,
        CoilProCommandsOptions options,
        TimeProvider timeProvider,
        Func<Domain, ReadOnlyMemory<byte>, CommandResult>? interpreter = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (options.AutoWake && coilClient is null)
        {
            throw new ArgumentException($"{nameof(CoilProCommandsOptions.AutoWake)} requires an {nameof(ICoilProClient)}.", nameof(coilClient));
        }

        _transport = transport;
        _protocol = protocol;
        _coilClient = coilClient;
        _options = options;
        _timeProvider = timeProvider;
        _interpreter = interpreter ?? ((domain, payload) => ReplyInterpreter.Interpret(domain, payload.Span));
    }

    private static VehicleProtocol CreateProtocol(TeslaKeyPair keyPair, CoilProCommandsOptions? options)
    {
        ArgumentNullException.ThrowIfNull(keyPair);
        return new VehicleProtocol(
            keyPair,
            TimeProvider.System,
            SystemRandomSource.Instance,
            (options ?? new CoilProCommandsOptions()).MessageLifetime);
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

    /// <inheritdoc />
    public Task<CommandResult> RemoteStartDriveAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.RemoteStartDrive, ct);

    /// <inheritdoc />
    public Task<CommandResult> OpenFrunkAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.OpenFrunk, ct);

    /// <inheritdoc />
    public Task<CommandResult> ActuateRearTrunkAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ActuateRearTrunk, ct);

    /// <inheritdoc />
    public Task<CommandResult> CloseRearTrunkAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.CloseRearTrunk, ct);

    /// <inheritdoc />
    public Task<CommandResult> AutoSecureModelXAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.AutoSecureModelX, ct);

    /// <inheritdoc />
    public Task<CommandResult> OpenTonneauAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.OpenTonneau, ct);

    /// <inheritdoc />
    public Task<CommandResult> CloseTonneauAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.CloseTonneau, ct);

    /// <inheritdoc />
    public Task<CommandResult> StopTonneauAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.StopTonneau, ct);

    /// <inheritdoc />
    public Task<CommandResult> CloseChargePortAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.CloseChargePort, ct);

    /// <inheritdoc />
    public Task<CommandResult> ChargeStandardRangeAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ChargeStandardRange, ct);

    /// <inheritdoc />
    public Task<CommandResult> ChargeMaxRangeAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ChargeMaxRange, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetScheduledChargingAsync(string vin, bool enabled, TimeOnly time, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetScheduledCharging(enabled, time), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetScheduledDepartureAsync(string vin, ScheduledDeparture departure, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetScheduledDeparture(departure), ct);

    /// <inheritdoc />
    public Task<CommandResult> ClearScheduledDepartureAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ClearScheduledDeparture, ct);

    /// <inheritdoc />
    public Task<CommandResult> AddChargeScheduleAsync(string vin, ChargeSchedule schedule, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.AddChargeSchedule(schedule), ct);

    /// <inheritdoc />
    public Task<CommandResult> RemoveChargeScheduleAsync(string vin, ulong scheduleId, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.RemoveChargeSchedule(scheduleId), ct);

    /// <inheritdoc />
    public Task<CommandResult> RemoveChargeSchedulesAsync(string vin, ScheduleLocations locations, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.RemoveChargeSchedules(locations), ct);

    /// <inheritdoc />
    public Task<CommandResult> AddPreconditionScheduleAsync(string vin, PreconditionSchedule schedule, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.AddPreconditionSchedule(schedule), ct);

    /// <inheritdoc />
    public Task<CommandResult> RemovePreconditionScheduleAsync(string vin, ulong scheduleId, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.RemovePreconditionSchedule(scheduleId), ct);

    /// <inheritdoc />
    public Task<CommandResult> RemovePreconditionSchedulesAsync(string vin, ScheduleLocations locations, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.RemovePreconditionSchedules(locations), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetLowPowerModeAsync(string vin, bool enabled, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetLowPowerMode(enabled), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetKeepAccessoryPowerModeAsync(string vin, bool enabled, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetKeepAccessoryPowerMode(enabled), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetPreconditioningMaxAsync(string vin, bool on, bool manualOverride = false, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetPreconditioningMax(on, manualOverride), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetBioweaponModeAsync(string vin, bool on, bool manualOverride = false, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetBioweaponMode(on, manualOverride), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetCabinOverheatProtectionAsync(string vin, bool on, bool fanOnly = false, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetCabinOverheatProtection(on, fanOnly), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetCabinOverheatProtectionTemperatureAsync(string vin, CabinOverheatTemperature temperature, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetCabinOverheatProtectionTemperature(temperature), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetClimateKeeperModeAsync(string vin, ClimateKeeperMode mode, bool manualOverride = false, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetClimateKeeperMode(mode, manualOverride), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetSeatHeaterAsync(string vin, SeatPosition seat, SeatHeaterLevel level, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetSeatHeater(seat, level), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetSeatCoolerAsync(string vin, FrontSeat seat, SeatCoolerLevel level, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetSeatCooler(seat, level), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetAutoSeatClimateAsync(string vin, FrontSeat seat, bool on, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetAutoSeatClimate(seat, on), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetSteeringWheelHeaterAsync(string vin, bool on, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetSteeringWheelHeater(on), ct);

    /// <inheritdoc />
    public Task<CommandResult> VentWindowsAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.VentWindows, ct);

    /// <inheritdoc />
    public Task<CommandResult> CloseWindowsAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.CloseWindows, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetVolumeAsync(string vin, float volume, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetVolume(volume), ct);

    /// <inheritdoc />
    public Task<CommandResult> VolumeUpAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.VolumeUp, ct);

    /// <inheritdoc />
    public Task<CommandResult> VolumeDownAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.VolumeDown, ct);

    /// <inheritdoc />
    public Task<CommandResult> NextTrackAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.NextTrack, ct);

    /// <inheritdoc />
    public Task<CommandResult> PreviousTrackAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.PreviousTrack, ct);

    /// <inheritdoc />
    public Task<CommandResult> NextFavoriteAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.NextFavorite, ct);

    /// <inheritdoc />
    public Task<CommandResult> PreviousFavoriteAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.PreviousFavorite, ct);

    /// <inheritdoc />
    public Task<CommandResult> TogglePlaybackAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.TogglePlayback, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetSentryModeAsync(string vin, bool on, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetSentryMode(on), ct);

    /// <inheritdoc />
    public Task<CommandResult> EnableValetModeAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.EnableValetMode(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> DisableValetModeAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.DisableValetMode, ct);

    /// <inheritdoc />
    public Task<CommandResult> ResetValetPinAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ResetValetPin, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetPinToDriveAsync(string vin, bool on, string? pin = null, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetPinToDrive(on, pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> ClearPinToDriveAdminAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ClearPinToDriveAdmin, ct);

    /// <inheritdoc />
    public Task<CommandResult> ResetPinToDriveAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ResetPinToDrive, ct);

    /// <inheritdoc />
    public Task<CommandResult> ActivateSpeedLimitAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.ActivateSpeedLimit(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> DeactivateSpeedLimitAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.DeactivateSpeedLimit(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> ClearSpeedLimitPinAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.ClearSpeedLimitPin(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> ClearSpeedLimitPinAdminAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ClearSpeedLimitPinAdmin, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetSpeedLimitAsync(string vin, double limitMph, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetSpeedLimit(limitMph), ct);

    /// <inheritdoc />
    public Task<CommandResult> ActivateParentalControlsAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.ActivateParentalControls(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> DeactivateParentalControlsAsync(string vin, string pin, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.DeactivateParentalControls(pin), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetParentalControlsSettingAsync(string vin, ParentalControlsSetting setting, bool enable, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetParentalControlsSetting(setting, enable), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetParentalControlsSpeedLimitAsync(string vin, double limitMph, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetParentalControlsSpeedLimit(limitMph), ct);

    /// <inheritdoc />
    public Task<CommandResult> ClearParentalControlsPinAdminAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.ClearParentalControlsPinAdmin, ct);

    /// <inheritdoc />
    public Task<CommandResult> SetGuestModeAsync(string vin, bool on, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetGuestMode(on), ct);

    /// <inheritdoc />
    public Task<CommandResult> EraseUserDataAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.EraseUserData, ct);

    /// <inheritdoc />
    public Task<CommandResult> ScheduleSoftwareUpdateAsync(string vin, TimeSpan delay, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.ScheduleSoftwareUpdate(delay), ct);

    /// <inheritdoc />
    public Task<CommandResult> CancelSoftwareUpdateAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.CancelSoftwareUpdate, ct);

    /// <inheritdoc />
    public Task<CommandResult> TriggerHomelinkAsync(string vin, double latitude, double longitude, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.TriggerHomelink(latitude, longitude), ct);

    /// <inheritdoc />
    public Task<CommandResult> SetVehicleNameAsync(string vin, string name, CancellationToken ct = default) =>
        ExecuteAsync(vin, () => CommandPayloads.SetVehicleName(name), ct);

    /// <inheritdoc />
    public Task<CommandResult> PingAsync(string vin, CancellationToken ct = default) =>
        ExecuteAsync(vin, CommandPayloads.Ping, ct);

    /// <summary>
    /// Orchestration: validate VIN, build the payload (argument errors surface synchronously-ish before any I/O),
    /// serialize per VIN, get/establish the session for the command's domain, sign, send, open the reply,
    /// interpret; on retryable faults adopt the attached session info, wait <see cref="CoilProCommandsOptions.RetryDelay"/>
    /// and re-sign until <see cref="CoilProCommandsOptions.CommandTimeout"/>; on asleep optionally wake and poll.
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
            Diagnose(() => $"{command.Name} → {command.Domain}: signed with {prepared.Message.SignatureData?.SigTypeCase}");
            RoutableMessage? reply = null;
            var opened = false;
            try
            {
                reply = await _transport.SendAsync(vin, prepared.Message, ct).ConfigureAwait(false);
                Diagnose(() => $"{command.Name} ← {command.Domain}: reply {DescribeReply(reply)}");
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
            Diagnose(() => $"handshake ← {domain}: reply {DescribeReply(reply)}");
            try
            {
                var session = _protocol.EstablishSession(vin, request, reply, sentAt);
                Diagnose(() => $"handshake ← {domain}: session established, vehicle supports detached AES-GCM = {session.SupportsDetached}");
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
    /// wake-up) every <see cref="CoilProCommandsOptions.WakePollInterval"/> until the vehicle reports state "online"
    /// or <see cref="CoilProCommandsOptions.WakeTimeout"/> elapses.
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

    private void Diagnose(Func<string> message)
    {
        if (_options.DiagnosticLog is { } log)
        {
            log(message());
        }
    }

    /// <summary>Shape of a reply for diagnostics — structure only, no key material or payload bytes.</summary>
    private static string DescribeReply(RoutableMessage reply)
    {
        var fault = reply.SignedMessageStatus is { } status
            ? $"{status.OperationStatus}/{(TeslaMessageFault)(int)status.SignedMessageFault}"
            : "none";
        return $"from={reply.FromDestination?.SubDestinationCase.ToString() ?? "-"}:{(reply.FromDestination?.SubDestinationCase == Destination.SubDestinationOneofCase.Domain ? reply.FromDestination.Domain.ToString() : "-")}, " +
               $"payload={reply.PayloadCase}, signature={reply.SignatureData?.SigTypeCase.ToString() ?? "none"}, " +
               $"flags={reply.Flags}, request_uuid={(reply.RequestUuid.IsEmpty ? "absent" : $"{reply.RequestUuid.Length} bytes")}, " +
               $"status={fault}";
    }

    private sealed class RunState
    {
        public TeslaMessageFault? LastFault { get; set; }
    }
}
