namespace Coil.Tests.TestSupport;

/// <summary>
/// Builds a <see cref="CoilClient"/> wired to a <see cref="StubHttpMessageHandler"/> (or any
/// <see cref="HttpMessageHandler"/>) with the Fleet API base address pre-set, so individual test
/// files don't repeat that plumbing. Defaults <see cref="CoilClientOptions.BaseRetryDelay"/> to
/// <see cref="TimeSpan.Zero"/> so tests never sleep during retries unless a test explicitly overrides
/// the options with its own (non-zero) delay.
/// </summary>
internal static class CoilClientFactory
{
    public const string FleetApiBaseUrl = "https://fleet-api.prd.eu.vn.cloud.tesla.com";

    public static CoilClient Create(
        HttpMessageHandler handler,
        ITeslaTokenProvider? tokenProvider = null,
        CoilClientOptions? options = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(FleetApiBaseUrl) };
        return new CoilClient(
            http,
            tokenProvider ?? new FakeTeslaTokenProvider(),
            options ?? new CoilClientOptions { BaseRetryDelay = TimeSpan.Zero });
    }
}
