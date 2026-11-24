namespace Coil;

/// <summary>
/// Selectable data segments for <c>vehicle_data</c>. Requesting only what you need reduces
/// Fleet API cost (pay-per-use). Combine with bitwise OR.
/// </summary>
[Flags]
public enum VehicleDataEndpoint
{
    /// <summary>No filter — let the API return its default set.</summary>
    None = 0,
    /// <summary>Include <c>charge_state</c> in the response.</summary>
    ChargeState = 1 << 0,
    /// <summary>Include <c>climate_state</c> in the response.</summary>
    ClimateState = 1 << 1,
    /// <summary>Include <c>drive_state</c> in the response.</summary>
    DriveState = 1 << 2,
    /// <summary>Include <c>gui_settings</c> in the response.</summary>
    GuiSettings = 1 << 3,
    /// <summary>Include <c>vehicle_state</c> in the response.</summary>
    VehicleState = 1 << 4,
    /// <summary>Include <c>vehicle_config</c> in the response.</summary>
    VehicleConfig = 1 << 5,
    /// <summary>Include <c>location_data</c> in the response.</summary>
    LocationData = 1 << 6,
    /// <summary>Include <c>closures_state</c> in the response.</summary>
    ClosuresState = 1 << 7,
}

/// <summary>Helpers for <see cref="VehicleDataEndpoint"/>.</summary>
public static class VehicleDataEndpointExtensions
{
    /// <summary>
    /// Serializes the selected segments to the semicolon-separated value the <c>endpoints</c>
    /// query parameter expects (e.g. <c>charge_state;drive_state</c>). Returns null for <see cref="VehicleDataEndpoint.None"/>.
    /// </summary>
    public static string? ToQueryValue(this VehicleDataEndpoint endpoints)
    {
        if (endpoints == VehicleDataEndpoint.None)
        {
            return null;
        }

        var parts = new List<string>();
        if (endpoints.HasFlag(VehicleDataEndpoint.ChargeState)) parts.Add("charge_state");
        if (endpoints.HasFlag(VehicleDataEndpoint.ClimateState)) parts.Add("climate_state");
        if (endpoints.HasFlag(VehicleDataEndpoint.DriveState)) parts.Add("drive_state");
        if (endpoints.HasFlag(VehicleDataEndpoint.GuiSettings)) parts.Add("gui_settings");
        if (endpoints.HasFlag(VehicleDataEndpoint.VehicleState)) parts.Add("vehicle_state");
        if (endpoints.HasFlag(VehicleDataEndpoint.VehicleConfig)) parts.Add("vehicle_config");
        if (endpoints.HasFlag(VehicleDataEndpoint.LocationData)) parts.Add("location_data");
        if (endpoints.HasFlag(VehicleDataEndpoint.ClosuresState)) parts.Add("closures_state");

        return string.Join(';', parts);
    }
}
