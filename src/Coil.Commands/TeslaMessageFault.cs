namespace Coil.Commands;

/// <summary>
/// Protocol-level fault reported by the vehicle for a signed message (<c>MessageFault_E</c> in Tesla's
/// <c>universal_message.proto</c>). Values match the wire values.
/// </summary>
public enum TeslaMessageFault
{
    None = 0,
    Busy = 1,
    Timeout = 2,
    UnknownKeyId = 3,
    InactiveKey = 4,
    InvalidSignature = 5,
    InvalidTokenOrCounter = 6,
    InsufficientPrivileges = 7,
    InvalidDomains = 8,
    InvalidCommand = 9,
    Decoding = 10,
    Internal = 11,
    WrongPersonalization = 12,
    BadParameter = 13,
    KeychainIsFull = 14,
    IncorrectEpoch = 15,
    IvIncorrectLength = 16,
    TimeExpired = 17,
    NotProvisionedWithIdentity = 18,
    CouldNotHashMetadata = 19,
    TimeToLiveTooLong = 20,
    RemoteAccessDisabled = 21,
    RemoteServiceAccessDisabled = 22,
    CommandRequiresAccountCredentials = 23,
    RequestMtuExceeded = 24,
    ResponseMtuExceeded = 25,
    RepeatedCounter = 26,
    InvalidKeyHandle = 27,
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
