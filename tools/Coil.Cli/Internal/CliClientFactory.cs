namespace Coil.Cli.Internal;

/// <summary>
/// Builds the <see cref="ICoilClient"/> (and the <see cref="HttpClient"/>s it depends on) needed to run
/// authenticated Fleet API calls from a persisted <see cref="CliSession"/>, without re-running <c>coil login</c>.
/// </summary>
internal static class CliClientFactory
{
    /// <summary>
    /// Bundles the <see cref="ICoilClient"/> and the two <see cref="HttpClient"/> instances backing it
    /// (auth + Fleet API), so callers can dispose them together with a single <c>using</c>.
    /// </summary>
    public sealed class CliClients(HttpClient authHttp, HttpClient apiHttp, ICoilClient coilClient) : IDisposable
    {
        public ICoilClient CoilClient { get; } = coilClient;

        public void Dispose()
        {
            authHttp.Dispose();
            apiHttp.Dispose();
        }
    }

    /// <summary>
    /// Creates a token provider (backed by <see cref="FileTokenStore"/> and a real
    /// <see cref="TeslaAuthClient"/> for refresh) and an <see cref="ICoilClient"/> pointed at the
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
        ICoilClient coilClient = new CoilClient(apiHttp, tokenProvider);

        return new CliClients(authHttp, apiHttp, coilClient);
    }
}
