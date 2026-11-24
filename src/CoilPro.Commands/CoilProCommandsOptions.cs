namespace CoilPro.Commands;

/// <summary>Options for <see cref="CoilProCommands"/>.</summary>
public sealed class CoilProCommandsOptions
{
    /// <summary>
    /// When true, a command to a sleeping vehicle first calls <c>wake_up</c> and polls until the vehicle is
    /// online (bounded by <see cref="WakeTimeout"/>), then sends the command. Default false: the command throws
    /// <see cref="TeslaVehicleAsleepException"/> immediately, so no wake-up is billed without the caller's consent.
    /// Requires an <c>ICoilProClient</c> passed to <see cref="CoilProCommands"/>.
    /// </summary>
    public bool AutoWake { get; set; }

    /// <summary>Maximum time to wait for a woken vehicle to come online. Default 60 s.</summary>
    public TimeSpan WakeTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Interval between online checks while waking. Default 3 s.</summary>
    public TimeSpan WakePollInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Overall deadline for one command, including the session handshake and retries of retryable faults.
    /// Default 30 s.
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Lifetime written into each signed message (its expiry on the vehicle's clock). Default 5 s, as in
    /// Tesla's reference client; the vehicle rejects values that are too far in the future.
    /// </summary>
    public TimeSpan MessageLifetime { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Delay before re-signing and resending after a retryable fault. Default 1 s.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Optional sink for protocol diagnostics, one line per step: handshake (session flags, whether the vehicle
    /// echoed the request uuid), the signature type chosen, and the shape of each reply (encrypted or not, flags,
    /// fault, attached session info). Never contains keys, tags, nonces, ciphertext or command payloads.
    /// Null (default) disables it.
    /// </summary>
    public Action<string>? DiagnosticLog { get; set; }
}
