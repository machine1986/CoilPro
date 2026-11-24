using CoilPro.Commands.Internal.Crypto;
using UniversalMessage;

namespace CoilPro.Commands.Internal.Protocol;

/// <summary>Session state managed by <see cref="VehicleProtocol"/> (port of the Go <c>Signer</c>/<c>Peer</c> state).</summary>
internal sealed partial class VehicleSession
{
    internal VehicleSession(Domain domain, SessionKeys keys, byte[] vehiclePublicKey, byte[] epoch, uint counter, uint clockTime, DateTimeOffset timeZero)
        : this(domain)
    {
        Keys = keys;
        VehiclePublicKey = vehiclePublicKey;
        Epoch = epoch;
        Counter = counter;
        ClockTime = clockTime;
        TimeZero = timeZero;
    }

    /// <summary>Keys derived from ECDH with <see cref="VehiclePublicKey"/>.</summary>
    internal SessionKeys Keys { get; } = null!;

    /// <summary>The vehicle domain's 65-byte public key; never changes for the lifetime of the session.</summary>
    internal byte[] VehiclePublicKey { get; } = [];

    /// <summary>The vehicle's 16-byte session epoch.</summary>
    internal byte[] Epoch { get; set; } = [];

    /// <summary>Last counter value used (or reported by the vehicle). Pre-incremented by each signed command.</summary>
    internal uint Counter { get; set; }

    /// <summary>Vehicle clock (seconds into the epoch) of the latest adopted session info.</summary>
    internal uint ClockTime { get; set; }

    /// <summary>Local time at which the vehicle's epoch started: vehicle time = seconds since this instant.</summary>
    internal DateTimeOffset TimeZero { get; set; }

    /// <summary>Whether the vehicle advertised <c>SESSION_INFO_FLAG_SUPPORTS_DETACHED_ENCRYPTION</c>.</summary>
    internal bool SupportsDetached { get; init; }

    /// <summary>Key handle the vehicle assigned (informational).</summary>
    internal uint Handle { get; init; }
}
