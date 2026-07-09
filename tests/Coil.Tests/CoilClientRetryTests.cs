using System.Net;
using Coil.Tests.TestSupport;

namespace Coil.Tests;

public class CoilClientRetryTests
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
        var options = new CoilClientOptions { MaxRetries = 1, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilClientFactory.Create(handler, options: options);

        var vehicles = await client.GetVehiclesAsync();

        Assert.Empty(vehicles);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task NonRetryableBadRequest_IsNotRetried_EvenWithRetriesAvailable()
    {
        var handler = new StubHttpMessageHandler(
            StubHttpMessageHandler.JsonResponse("""{"error":"bad_request"}""", HttpStatusCode.BadRequest));
        var options = new CoilClientOptions { MaxRetries = 3, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilClientFactory.Create(handler, options: options);

        await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.Single(handler.Requests);
    }
}
