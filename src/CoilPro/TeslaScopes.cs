namespace CoilPro;

/// <summary>
/// Tesla Fleet API OAuth scopes. Verify the current set against developer.tesla.com.
/// </summary>
[Flags]
public enum TeslaScopes
{
    /// <summary>No scopes.</summary>
    None = 0,
    /// <summary><c>openid</c>: identify the user.</summary>
    Openid = 1 << 0,
    /// <summary><c>offline_access</c>: issue refresh tokens.</summary>
    OfflineAccess = 1 << 1,
    /// <summary><c>user_data</c>: account and profile information.</summary>
    UserData = 1 << 2,
    /// <summary><c>vehicle_device_data</c>: read vehicle information and data.</summary>
    VehicleDeviceData = 1 << 3,
    /// <summary><c>vehicle_location</c>: read vehicle location.</summary>
    VehicleLocation = 1 << 4,
    /// <summary><c>vehicle_cmds</c>: send vehicle commands.</summary>
    VehicleCmds = 1 << 5,
    /// <summary><c>vehicle_charging_cmds</c>: send vehicle charging commands.</summary>
    VehicleChargingCmds = 1 << 6,
    /// <summary><c>energy_device_data</c>: read energy product data.</summary>
    EnergyDeviceData = 1 << 7,
    /// <summary><c>energy_cmds</c>: send energy product commands.</summary>
    EnergyCmds = 1 << 8,
}

/// <summary>Helpers for <see cref="TeslaScopes"/>.</summary>
public static class TeslaScopesExtensions
{
    /// <summary>
    /// Serializes the flags to the space-separated scope string the OAuth endpoint expects,
    /// e.g. <c>"openid offline_access vehicle_device_data"</c>.
    /// </summary>
    public static string ToScopeString(this TeslaScopes scopes)
    {
        var parts = new List<string>();

        if (scopes.HasFlag(TeslaScopes.Openid)) parts.Add("openid");
        if (scopes.HasFlag(TeslaScopes.OfflineAccess)) parts.Add("offline_access");
        if (scopes.HasFlag(TeslaScopes.UserData)) parts.Add("user_data");
        if (scopes.HasFlag(TeslaScopes.VehicleDeviceData)) parts.Add("vehicle_device_data");
        if (scopes.HasFlag(TeslaScopes.VehicleLocation)) parts.Add("vehicle_location");
        if (scopes.HasFlag(TeslaScopes.VehicleCmds)) parts.Add("vehicle_cmds");
        if (scopes.HasFlag(TeslaScopes.VehicleChargingCmds)) parts.Add("vehicle_charging_cmds");
        if (scopes.HasFlag(TeslaScopes.EnergyDeviceData)) parts.Add("energy_device_data");
        if (scopes.HasFlag(TeslaScopes.EnergyCmds)) parts.Add("energy_cmds");

        return string.Join(' ', parts);
    }
}
