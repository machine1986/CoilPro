namespace CoilPro.Cli.Internal;

/// <summary>
/// Builds the <see cref="ICoilProClient"/> (and the <see cref="HttpClient"/>s it depends on) needed to run
/// authenticated Fleet API calls from a persisted <see cref="CliSession"/>, without re-running <c>coilpro login</c>.
/// </summary>
internal static class CliClientFactory
{
    /// <summary>
    /// Bundles the <see cref="ICoilProClient"/> and the two <see cref="HttpClient"/> instances backing it
    /// (auth + Fleet API), so callers can dispose them together with a single <c>using</c>.
    /// </summary>
    public sealed class CliClients(HttpClient authHttp, HttpClient apiHttp, ICoilProClient coilClient, ITeslaTokenProvider tokenProvider) : IDisposable
    {
        public ICoilProClient CoilProClient { get; } = coilClient;

        /// <summary>The Fleet API <see cref="HttpClient"/> (regional base address), shared with signed commands.</summary>
        public HttpClient ApiHttp { get; } = apiHttp;

        public ITeslaTokenProvider TokenProvider { get; } = tokenProvider;

        public void Dispose()
        {
            authHttp.Dispose();
            ApiHttp.Dispose();
        }
    }

    /// <summary>
    /// Creates a token provider (backed by <see cref="FileTokenStore"/> and a real
    /// <see cref="TeslaAuthClient"/> for refresh) and an <see cref="ICoilProClient"/> pointed at the
    /// Fleet API host for <paramref name="session"/>'s region.
    /// </summary>
    public static CliClients Create(CliSession session)
    {
        var authOptions = new TeslaAuthOptions
        {
            ClientId = session.ClientId,
            Region = session.Region,
        };

        var authHttp = new HttpClient();
        ITeslaAuthClient authClient = new TeslaAuthClient(authHttp, authOptions);

        var tokenStore = new FileTokenStore();
        ITeslaTokenProvider tokenProvider = new TeslaTokenProvider(tokenStore, authClient);

        var apiHttp = new HttpClient { BaseAddress = TeslaEndpoints.For(session.Region).FleetApiBaseUrl };
        ICoilProClient coilClient = new CoilProClient(apiHttp, tokenProvider);

        return new CliClients(authHttp, apiHttp, coilClient, tokenProvider);
    }
}
