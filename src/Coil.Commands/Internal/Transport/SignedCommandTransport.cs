using UniversalMessage;

namespace Coil.Commands.Internal.Transport;

/// <summary>
/// Sends a <see cref="RoutableMessage"/> to <c>POST api/1/vehicles/{vin}/signed_command</c> and returns the
/// vehicle's reply. FROZEN CONTRACT (M3 stage 0).
/// </summary>
internal interface ISignedCommandTransport
{
    /// <summary>
    /// Body <c>{"routable_message":"&lt;std base64&gt;"}</c>; reply <c>{"response":"&lt;base64 RoutableMessage&gt;"}</c>.
    /// Resends the same bytes only where that is safe (408 not "offline", 421 with host switch, network errors
    /// before the request was sent); never after other 5xx, because the command may have executed.
    /// </summary>
    /// <exception cref="TeslaVehicleAsleepException">503, or 408 with "vehicle is offline" in the body.</exception>
    /// <exception cref="TeslaCommandException">Any other non-success status (with <see cref="TeslaCommandException.StatusCode"/>), or an undecodable reply.</exception>
    Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct);
}

/// <summary>HTTP implementation of <see cref="ISignedCommandTransport"/> against the Fleet API.</summary>
internal sealed class FleetSignedCommandTransport : ISignedCommandTransport
{
    /// <param name="http">HttpClient whose <c>BaseAddress</c> is the regional Fleet API host.</param>
    /// <param name="tokenProvider">Supplies the user's bearer token.</param>
    public FleetSignedCommandTransport(HttpClient http, ITeslaTokenProvider tokenProvider)
    {
        ArgumentNullException.ThrowIfNull(http);
        ArgumentNullException.ThrowIfNull(tokenProvider);
    }

    public Task<RoutableMessage> SendAsync(string vin, RoutableMessage message, CancellationToken ct) =>
        throw new NotImplementedException("TODO(M3 transport): signed_command");
}
