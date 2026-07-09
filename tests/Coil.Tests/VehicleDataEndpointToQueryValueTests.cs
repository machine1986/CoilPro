namespace Coil.Tests;

public class VehicleDataEndpointToQueryValueTests
{
    [Fact]
    public void ToQueryValue_None_ReturnsNull()
    {
        Assert.Null(VehicleDataEndpoint.None.ToQueryValue());
    }

    [Fact]
    public void ToQueryValue_SingleFlag_ReturnsSingleSnakeCaseSegment()
    {
        Assert.Equal("charge_state", VehicleDataEndpoint.ChargeState.ToQueryValue());
        Assert.Equal("drive_state", VehicleDataEndpoint.DriveState.ToQueryValue());
        Assert.Equal("vehicle_config", VehicleDataEndpoint.VehicleConfig.ToQueryValue());
        Assert.Equal("location_data", VehicleDataEndpoint.LocationData.ToQueryValue());
        Assert.Equal("closures_state", VehicleDataEndpoint.ClosuresState.ToQueryValue());
    }

    [Fact]
    public void ToQueryValue_MultipleFlags_ReturnsSemicolonSeparatedSegments()
    {
        var endpoints = VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState;

        var value = endpoints.ToQueryValue();

        Assert.NotNull(value);
        var segments = value!.Split(';');
        Assert.Equal(2, segments.Length);
        Assert.Contains("charge_state", segments);
        Assert.Contains("drive_state", segments);
    }

    [Fact]
    public void ToQueryValue_AllFlags_IncludesEverySegmentExactlyOnce()
    {
        var all = VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.ClimateState | VehicleDataEndpoint.DriveState
            | VehicleDataEndpoint.GuiSettings | VehicleDataEndpoint.VehicleState | VehicleDataEndpoint.VehicleConfig
            | VehicleDataEndpoint.LocationData | VehicleDataEndpoint.ClosuresState;

        var segments = all.ToQueryValue()!.Split(';');

        Assert.Equal(8, segments.Length);
        Assert.Equal(8, segments.Distinct().Count());
        foreach (var expected in new[]
                 {
                     "charge_state", "climate_state", "drive_state", "gui_settings",
                     "vehicle_state", "vehicle_config", "location_data", "closures_state",
                 })
        {
            Assert.Contains(expected, segments);
        }
    }
}
