using System.Net;
using System.Net.Http.Headers;
using Coil.Tests.TestSupport;

namespace Coil.Tests;

public class CoilClientErrorHandlingTests
{
    private static HttpResponseMessage JsonError(HttpStatusCode status, string errorCode, TimeSpan? retryAfter = null)
    {
        var response = StubHttpMessageHandler.JsonResponse($$"""{"error":"{{errorCode}}"}""", status);
        if (retryAfter.HasValue)
        {
            response.Headers.RetryAfter = new RetryConditionHeaderValue(retryAfter.Value);
        }

        return response;
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "invalid_token")]
    [InlineData(HttpStatusCode.NotFound, "not_found")]
    public async Task NonRetryableClientError_ThrowsTeslaApiException_WithStatusAndErrorCode_WithoutRetrying(
        HttpStatusCode status, string errorCode)
    {
        var handler = new StubHttpMessageHandler(JsonError(status, errorCode));
        var client = CoilClientFactory.Create(handler);

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.Equal(status, ex.StatusCode);
        Assert.Equal(errorCode, ex.ErrorCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task ErrorResponse_WithRetryAfterHeader_SurfacesItOnTheException()
    {
        var handler = new StubHttpMessageHandler(JsonError(HttpStatusCode.TooManyRequests, "rate_limited", TimeSpan.FromSeconds(5)));
        var options = new CoilClientOptions { MaxRetries = 0, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilClientFactory.Create(handler, options: options);

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.Equal(TimeSpan.FromSeconds(5), ex.RetryAfter);
    }

    [Fact]
    public async Task ErrorResponse_CarriesRawResponseBody()
    {
        var handler = new StubHttpMessageHandler(JsonError(HttpStatusCode.NotFound, "not_found"));
        var options = new CoilClientOptions { MaxRetries = 0, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilClientFactory.Create(handler, options: options);

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.NotNull(ex.ResponseBody);
        Assert.Contains("not_found", ex.ResponseBody!);
    }

    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task TransientError_ExhaustingAllRetries_ThrowsTeslaApiException_AfterMaxRetriesPlusOneAttempts(
        HttpStatusCode status)
    {
        const int maxRetries = 2;
        var responses = Enumerable.Range(0, maxRetries + 1)
            .Select(_ => JsonError(status, "server_error"))
            .ToArray();
        var handler = new StubHttpMessageHandler(responses);
        var options = new CoilClientOptions { MaxRetries = maxRetries, BaseRetryDelay = TimeSpan.Zero };
        var client = CoilClientFactory.Create(handler, options: options);

        var ex = await Assert.ThrowsAsync<TeslaApiException>(() => client.GetVehiclesAsync());

        Assert.Equal(status, ex.StatusCode);
        // Initial attempt + MaxRetries retries, all exhausted.
        Assert.Equal(maxRetries + 1, handler.Requests.Count);
    }
}
