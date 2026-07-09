namespace Coil;

/// <summary>
/// Tesla Fleet API OAuth scopes. Verify the current set against developer.tesla.com.
/// </summary>
[Flags]
public enum TeslaScopes
{
    None = 0,
    Openid = 1 << 0,
    OfflineAccess = 1 << 1,
    UserData = 1 << 2,
    VehicleDeviceData = 1 << 3,
    VehicleLocation = 1 << 4,
    VehicleCmds = 1 << 5,
    VehicleChargingCmds = 1 << 6,
    EnergyDeviceData = 1 << 7,
    EnergyCmds = 1 << 8,
}

/// <summary>Helpers for <see cref="TeslaScopes"/>.</summary>
public static class TeslaScopesExtensions
{
    /// <summary>
    /// Serializes the flags to the space-separated scope string the OAuth endpoint expects,
    /// e.g. <c>"openid offline_access vehicle_device_data"</c>.
    /// </summary>
    public static string ToScopeString(this TeslaScopes scopes) =>
        throw new NotImplementedException("TODO(M1 core-impl): flags -> snake_case scope string");
}
