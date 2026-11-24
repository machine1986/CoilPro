using System.Net;
using CoilPro.Tests.TestSupport;

namespace CoilPro.Tests;

public class CoilProClientRetryTests
{
    private const string EmptyVehiclesJson = """{ "response": [], "count": 0 }""";

    [Fact]
    public async Task TransientTooManyRequestsThenSuccess_RetriesOnceAndSucceeds()
    {
        var responses = new[]
        {
            StubHttpMessageHandler.JsonResponse("""{"error":"rate_limited"}""", HttpStatusCode.TooManyRequests),
            StubHttpMessageHandler.JsonResponse(EmptyVehiclesJson),
        };
        var handler = new StubHttpMessageHandler(responses);
        var options = new CoilProClientOptions { MaxRetries = 1, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilProClientFactory.Create(handler, options: options);

        var vehicles = await client.GetVehiclesAsync();

        Assert.Empty(vehicles);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task NonRetryableBadRequest_IsNotRetried_EvenWithRetriesAvailable()
    {
        var handler = new StubHttpMessageHandler(
            StubHttpMessageHandler.JsonResponse("""{"error":"bad_request"}""", HttpStatusCode.BadRequest));
        var options = new CoilProClientOptions { MaxRetries = 3, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilProClientFactory.Create(handler, options: options);

        await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.Single(handler.Requests);
    }
}
