using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests;

public class CoilProClientWakeUpTests
{
    private const string WokenVehicleJson = """
        {
          "response": { "id": 100, "vehicle_id": 200, "vin": "5YJ3E1EA1JF000001", "display_name": "My Model 3", "state": "waking" }
        }
        """;

    [Fact]
    public async Task WakeUpAsync_PostsToWakeUpEndpoint()
    {
        var handler = StubHttpMessageHandler.ReturningJson(WokenVehicleJson);
        var client = CoilProClientFactory.Create(handler, new FakeTeslaTokenProvider("tok"));

        await client.WakeUpAsync("100");

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("/api/1/vehicles/100/wake_up", request.Uri.AbsolutePath);
        Assert.Equal("Bearer tok", request.AuthorizationHeader);
    }

    [Fact]
    public async Task WakeUpAsync_ReturnsTheVehicleFromTheResponse()
    {
        var handler = StubHttpMessageHandler.ReturningJson(WokenVehicleJson);
        var client = CoilProClientFactory.Create(handler);

        var vehicle = await client.WakeUpAsync("100");

        Assert.Equal(100, vehicle.Id);
        Assert.Equal(200, vehicle.VehicleId);
        Assert.Equal("waking", vehicle.State);
        Assert.Equal("My Model 3", vehicle.DisplayName);
    }
}
