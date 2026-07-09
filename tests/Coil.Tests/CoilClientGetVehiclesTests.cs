using Coil.Tests.TestSupport;

namespace Coil.Tests;

public class CoilClientGetVehiclesTests
{
    private const string VehiclesJson = """
        {
          "response": [
            { "id": 100, "vehicle_id": 200, "vin": "5YJ3E1EA1JF000001", "display_name": "My Model 3", "state": "online", "color": "red" },
            { "id": 101, "vehicle_id": 201, "vin": "5YJ3E1EA1JF000002", "display_name": "My Model Y", "state": "asleep" }
          ],
          "count": 2
        }
        """;

    [Fact]
    public async Task GetVehiclesAsync_ParsesEnvelopeIntoVehicleList()
    {
        var handler = StubHttpMessageHandler.ReturningJson(VehiclesJson);
        var client = CoilClientFactory.Create(handler);

        var vehicles = await client.GetVehiclesAsync();

        Assert.Equal(2, vehicles.Count);

        Assert.Equal(100, vehicles[0].Id);
        Assert.Equal(200, vehicles[0].VehicleId);
        Assert.Equal("5YJ3E1EA1JF000001", vehicles[0].Vin);
        Assert.Equal("My Model 3", vehicles[0].DisplayName);
        Assert.Equal("online", vehicles[0].State);

        Assert.Equal(101, vehicles[1].Id);
        Assert.Equal("asleep", vehicles[1].State);
    }

    [Fact]
    public async Task GetVehiclesAsync_RequestsExpectedPath_WithBearerTokenFromProvider()
    {
        var handler = StubHttpMessageHandler.ReturningJson(VehiclesJson);
        var client = CoilClientFactory.Create(handler, new FakeTeslaTokenProvider("tok"));

        await client.GetVehiclesAsync();

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/1/vehicles", request.Uri.AbsolutePath);
        Assert.Equal("Bearer tok", request.AuthorizationHeader);
    }

    [Fact]
    public async Task GetVehiclesAsync_UnknownJsonFields_LandInAdditionalData()
    {
        var handler = StubHttpMessageHandler.ReturningJson(VehiclesJson);
        var client = CoilClientFactory.Create(handler);

        var vehicles = await client.GetVehiclesAsync();

        Assert.NotNull(vehicles[0].AdditionalData);
        Assert.True(vehicles[0].AdditionalData!.ContainsKey("color"));
        Assert.Equal("red", vehicles[0].AdditionalData!["color"].GetString());

        // Second vehicle has no unmodeled fields in the fixture.
        Assert.False(vehicles[1].AdditionalData?.ContainsKey("color") ?? false);
    }

    [Fact]
    public async Task GetVehiclesAsync_EmptyResponseList_ReturnsEmptyList()
    {
        var handler = StubHttpMessageHandler.ReturningJson("""{ "response": [], "count": 0 }""");
        var client = CoilClientFactory.Create(handler);

        var vehicles = await client.GetVehiclesAsync();

        Assert.Empty(vehicles);
    }
}
