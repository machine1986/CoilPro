namespace CoilPro.Commands;

/// <summary>
/// Outcome of a command the vehicle received and authenticated. <see cref="Success"/> is false when the
/// vehicle refused to execute it — a normal outcome, not an error.
/// </summary>
/// <remarks>
/// Typical refusal reasons reported by the vehicle: <c>already_set</c>, <c>not_charging</c>,
/// <c>is_charging</c>, <c>complete</c>, <c>disconnected</c>, <c>car_wash</c>; for security commands e.g.
/// <c>VEHICLE_NOT_IN_PARK</c> or <c>CLOSURES_OPEN</c>. Failures to deliver or authenticate the command throw
/// <see cref="TeslaCommandException"/> instead.
/// </remarks>
public sealed record CommandResult
{
    /// <summary>True when the vehicle executed the command.</summary>
    public required bool Success { get; init; }

    /// <summary>The vehicle's reason for refusing the command; null on success.</summary>
    public string? Reason { get; init; }

    /// <summary>The shared success result.</summary>
    public static CommandResult Succeeded { get; } = new() { Success = true };

    /// <summary>Creates a refusal with the vehicle's reason.</summary>
    public static CommandResult Refused(string? reason) => new() { Success = false, Reason = reason };
}
