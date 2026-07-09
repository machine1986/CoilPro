using Coil.Tests.TestSupport;

namespace Coil.Tests;

public class CoilClientRateLimitTests
{
    private const string EmptyVehiclesJson = """{ "response": [], "count": 0 }""";

    [Fact]
    public async Task SuccessfulResponse_WithRateLimitHeaders_PopulatesLastRateLimit()
    {
        var resetUnix = DateTimeOffset.Parse("2026-07-09T12:00:00Z").ToUnixTimeSeconds();
        var response = StubHttpMessageHandler.JsonResponse(
            EmptyVehiclesJson,
            headers: new Dictionary<string, string>
            {
                ["x-ratelimit-limit"] = "100",
                ["x-ratelimit-remaining"] = "42",
                ["x-ratelimit-reset"] = resetUnix.ToString(),
            });
        var handler = new StubHttpMessageHandler(response);
        var client = CoilClientFactory.Create(handler);

        await client.GetVehiclesAsync();

        Assert.NotNull(client.LastRateLimit);
        Assert.Equal(100, client.LastRateLimit!.Limit);
        Assert.Equal(42, client.LastRateLimit.Remaining);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(resetUnix), client.LastRateLimit.ResetsAt);
    }

    [Fact]
    public async Task SuccessfulResponse_WithoutRateLimitHeaders_LeavesLastRateLimitNull()
    {
        var handler = StubHttpMessageHandler.ReturningJson(EmptyVehiclesJson);
        var client = CoilClientFactory.Create(handler);

        await client.GetVehiclesAsync();

        Assert.Null(client.LastRateLimit);
    }

    [Fact]
    public async Task LastRateLimit_IsUpdatedAcrossSuccessiveCalls()
    {
        var responses = new[]
        {
            StubHttpMessageHandler.JsonResponse(
                EmptyVehiclesJson,
                headers: new Dictionary<string, string> { ["x-ratelimit-remaining"] = "10" }),
            StubHttpMessageHandler.JsonResponse(
                EmptyVehiclesJson,
                headers: new Dictionary<string, string> { ["x-ratelimit-remaining"] = "9" }),
        };
        var handler = new StubHttpMessageHandler(responses);
        var client = CoilClientFactory.Create(handler);

        await client.GetVehiclesAsync();
        Assert.Equal(10, client.LastRateLimit!.Remaining);

        await client.GetVehiclesAsync();
        Assert.Equal(9, client.LastRateLimit!.Remaining);
    }
}
