using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests;

public class CoilProClientGetVehicleDataTests
{
    // Mimics what the real API would return when only charge_state and drive_state were
    // requested: the other sub-states are simply absent from the payload.
    private const string PartialVehicleDataJson = """
        {
          "response": {
            "id": 100,
            "vin": "5YJ3E1EA1JF000001",
            "state": "online",
            "charge_state": { "battery_level": 80, "charging_state": "Charging" },
            "drive_state": { "latitude": 37.7, "longitude": -122.4, "speed": 0 }
          }
        }
        """;

    private const string FullVehicleDataJson = """
        {
          "response": {
            "id": 100,
            "vin": "5YJ3E1EA1JF000001",
            "state": "online",
            "charge_state": { "battery_level": 80 },
            "climate_state": { "inside_temp": 21.5 },
            "drive_state": { "latitude": 37.7, "longitude": -122.4 },
            "vehicle_state": { "odometer": 12345.6 },
            "gui_settings": { "gui_distance_units": "km/hr" }
          }
        }
        """;

    [Fact]
    public async Task GetVehicleDataAsync_WithChargeStateAndDriveState_SendsExpectedEndpointsQuery()
    {
        var handler = StubHttpMessageHandler.ReturningJson(PartialVehicleDataJson);
        var client = CoilProClientFactory.Create(handler);

        await client.GetVehicleDataAsync("100", VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/1/vehicles/100/vehicle_data", request.Uri.AbsolutePath);
        Assert.Equal("charge_state;drive_state", request.QueryValues["endpoints"]);
    }

    [Fact]
    public async Task GetVehicleDataAsync_ParsesRequestedSubStates_AndLeavesUnrequestedOnesNull()
    {
        var handler = StubHttpMessageHandler.ReturningJson(PartialVehicleDataJson);
        var client = CoilProClientFactory.Create(handler);

        var data = await client.GetVehicleDataAsync("100", VehicleDataEndpoint.ChargeState | VehicleDataEndpoint.DriveState);

        Assert.Equal(100, data.Id);
        Assert.NotNull(data.ChargeState);
        Assert.Equal(80, data.ChargeState!.BatteryLevel);
        Assert.Equal("Charging", data.ChargeState.ChargingState);

        Assert.NotNull(data.DriveState);
        Assert.Equal(37.7, data.DriveState!.Latitude);
        Assert.Equal(-122.4, data.DriveState.Longitude);

        Assert.Null(data.ClimateState);
        Assert.Null(data.VehicleState);
        Assert.Null(data.GuiSettings);
    }

    [Fact]
    public async Task GetVehicleDataAsync_WithNone_DoesNotSendEndpointsQueryParameter()
    {
        var handler = StubHttpMessageHandler.ReturningJson(FullVehicleDataJson);
        var client = CoilProClientFactory.Create(handler);

        await client.GetVehicleDataAsync("100", VehicleDataEndpoint.None);

        var request = Assert.Single(handler.Requests);
        Assert.False(request.QueryValues.ContainsKey("endpoints"));
    }

    [Fact]
    public async Task GetVehicleDataAsync_WithNone_ParsesAllSubStatesPresentInResponse()
    {
        var handler = StubHttpMessageHandler.ReturningJson(FullVehicleDataJson);
        var client = CoilProClientFactory.Create(handler);

        var data = await client.GetVehicleDataAsync("100");

        Assert.NotNull(data.ChargeState);
        Assert.NotNull(data.ClimateState);
        Assert.NotNull(data.DriveState);
        Assert.NotNull(data.VehicleState);
        Assert.NotNull(data.GuiSettings);
        Assert.Equal(21.5, data.ClimateState!.InsideTemp);
        Assert.Equal(12345.6, data.VehicleState!.Odometer);
        Assert.Equal("km/hr", data.GuiSettings!.GuiDistanceUnits);
    }
}
