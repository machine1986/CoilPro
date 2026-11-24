using CoilPro.Commands.Internal.Crypto;
using UniversalMessage;

namespace CoilPro.Commands.Internal.Protocol;

/// <summary>Reply-matching state recorded by <see cref="VehicleProtocol.Sign"/>.</summary>
internal sealed partial class PreparedCommand
{
    internal PreparedCommand(RoutableMessage message, Domain domain, byte[] requestHash, uint counter, DateTimeOffset signedAt)
        : this(message)
    {
        Domain = domain;
        RequestHash = requestHash;
        Counter = counter;
        SignedAt = signedAt;
    }

    /// <summary>The destination domain.</summary>
    internal Domain Domain { get; }

    /// <summary><c>from_destination.routing_address</c> of the request; the reply is addressed to it.</summary>
    internal ReadOnlySpan<byte> RoutingAddress => Message.FromDestination.RoutingAddress.Span;

    /// <summary>The request <c>uuid</c>; Infotainment echoes it as <c>request_uuid</c>.</summary>
    internal ReadOnlySpan<byte> Uuid => Message.Uuid.Span;

    /// <summary><c>[signature type] ‖ tag</c> (17 bytes for VCSEC HMAC) — bound into the response's associated data.</summary>
    internal byte[] RequestHash { get; } = [];

    /// <summary>The anti-replay counter this command was signed with.</summary>
    internal uint Counter { get; }

    /// <summary>When the command was signed; session info attached to its reply is discarded after 10 s.</summary>
    internal DateTimeOffset SignedAt { get; }

    /// <summary>Rejects repeated response counters for this request.</summary>
    internal SlidingWindow ResponseWindow { get; } = new();
}
