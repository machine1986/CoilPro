namespace Coil.Commands;

/// <summary>
/// Protocol-level fault reported by the vehicle for a signed message (<c>MessageFault_E</c> in Tesla's
/// <c>universal_message.proto</c>). Values match the wire values.
/// </summary>
public enum TeslaMessageFault
{
    /// <summary>No fault.</summary>
    None = 0,
    /// <summary>The vehicle is busy; retry shortly.</summary>
    Busy = 1,
    /// <summary>The vehicle timed out handling the message.</summary>
    Timeout = 2,
    /// <summary>The vehicle does not know the signing key; the virtual key is not paired.</summary>
    UnknownKeyId = 3,
    /// <summary>The key is known but currently inactive.</summary>
    InactiveKey = 4,
    /// <summary>The message signature or HMAC tag was rejected.</summary>
    InvalidSignature = 5,
    /// <summary>The session counter or token was invalid (typically a stale or replayed counter).</summary>
    InvalidTokenOrCounter = 6,
    /// <summary>The key lacks the privileges needed for this command.</summary>
    InsufficientPrivileges = 7,
    /// <summary>The message addressed an invalid domain.</summary>
    InvalidDomains = 8,
    /// <summary>The vehicle rejected the command as invalid.</summary>
    InvalidCommand = 9,
    /// <summary>The vehicle could not decode the message.</summary>
    Decoding = 10,
    /// <summary>Internal vehicle error.</summary>
    Internal = 11,
    /// <summary>The message was personalized for a different vehicle.</summary>
    WrongPersonalization = 12,
    /// <summary>A command parameter was rejected.</summary>
    BadParameter = 13,
    /// <summary>The vehicle keychain has no free slot.</summary>
    KeychainIsFull = 14,
    /// <summary>The session epoch does not match; the session must be re-established.</summary>
    IncorrectEpoch = 15,
    /// <summary>The initialization vector had an incorrect length.</summary>
    IvIncorrectLength = 16,
    /// <summary>The message expiry time has passed.</summary>
    TimeExpired = 17,
    /// <summary>The vehicle is not provisioned with an identity.</summary>
    NotProvisionedWithIdentity = 18,
    /// <summary>The vehicle could not hash the message metadata.</summary>
    CouldNotHashMetadata = 19,
    /// <summary>The requested message lifetime is too long.</summary>
    TimeToLiveTooLong = 20,
    /// <summary>Remote access is disabled on the vehicle.</summary>
    RemoteAccessDisabled = 21,
    /// <summary>Remote service access is disabled on the vehicle.</summary>
    RemoteServiceAccessDisabled = 22,
    /// <summary>The command requires account credentials rather than a virtual key.</summary>
    CommandRequiresAccountCredentials = 23,
    /// <summary>The request exceeds the transport MTU.</summary>
    RequestMtuExceeded = 24,
    /// <summary>The response would exceed the transport MTU.</summary>
    ResponseMtuExceeded = 25,
    /// <summary>The message counter was already used.</summary>
    RepeatedCounter = 26,
    /// <summary>The key handle is invalid.</summary>
    InvalidKeyHandle = 27,
    /// <summary>The command requires an encrypted response.</summary>
    RequiresResponseEncryption = 28,
}

/// <summary>Helpers for <see cref="TeslaMessageFault"/>.</summary>
public static class TeslaMessageFaultExtensions
{
    /// <summary>
    /// True for faults that Tesla's reference client retries by re-signing the command with a fresh counter
    /// and expiry, after adopting the session info the vehicle attached to the fault reply
    /// (<c>pkg/protocol/error.go</c> in teslamotors/vehicle-command).
    /// </summary>
    public static bool IsRetryable(this TeslaMessageFault fault) => fault is
        TeslaMessageFault.Busy or
        TeslaMessageFault.Timeout or
        TeslaMessageFault.InvalidSignature or
        TeslaMessageFault.InvalidTokenOrCounter or
        TeslaMessageFault.Internal or
        TeslaMessageFault.IncorrectEpoch or
        TeslaMessageFault.TimeExpired or
        TeslaMessageFault.TimeToLiveTooLong;

    /// <summary>True when the vehicle does not know the key — the virtual key is not paired.</summary>
    public static bool IsKeyNotPaired(this TeslaMessageFault fault) => fault == TeslaMessageFault.UnknownKeyId;
}
