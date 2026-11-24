using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace CoilPro.AspNetCore;

/// <summary>
/// Dependency-injection extensions that wire up the CoilPro Tesla Fleet API SDK
/// (<see cref="ITeslaAuthClient"/>, <see cref="ITokenStore"/>, <see cref="ITeslaTokenProvider"/>,
/// <see cref="ICoilProClient"/>, <see cref="ICoilProEnergyClient"/>) for ASP.NET Core / generic-host apps.
/// </summary>
public static class CoilProServiceCollectionExtensions
{
    /// <summary>Logical name of the <see cref="HttpClient"/> registered for <see cref="ITeslaAuthClient"/>.</summary>
    public const string HttpClientName = "CoilPro.Auth";

    /// <summary>Logical name of the <see cref="HttpClient"/> registered for <see cref="ICoilProClient"/>, pointed at
    /// the configured account's Tesla Fleet API host.</summary>
    public const string ApiHttpClientName = "CoilPro.Api";

    /// <summary>
    /// Registers the CoilPro Tesla Fleet API client services:
    /// <list type="bullet">
    /// <item><description>Binds/configures <see cref="TeslaAuthOptions"/> via <paramref name="configure"/>.</description></item>
    /// <item><description>Registers <see cref="ITeslaAuthClient"/> → <see cref="TeslaAuthClient"/>, backed by a
    /// named <see cref="HttpClient"/> (<see cref="HttpClientName"/>) obtained from <c>IHttpClientFactory</c>.</description></item>
    /// <item><description>Registers <see cref="ITokenStore"/> → <see cref="InMemoryTokenStore"/> as the default
    /// (registered with <c>TryAdd</c>, so callers can register their own <see cref="ITokenStore"/> — e.g. one backed
    /// by ASP.NET Core Data Protection — before or after calling this method).</description></item>
    /// <item><description>Registers <see cref="ITeslaTokenProvider"/> → <see cref="TeslaTokenProvider"/>, wired to
    /// the resolved <see cref="ITokenStore"/> and <see cref="ITeslaAuthClient"/> (registered with
    /// <c>TryAdd</c>).</description></item>
    /// <item><description>Registers a second named <see cref="HttpClient"/> (<see cref="ApiHttpClientName"/>) whose
    /// <see cref="HttpClient.BaseAddress"/> is the Tesla Fleet API host for the configured
    /// <see cref="TeslaAuthOptions.Region"/> (or the explicit <see cref="TeslaAuthOptions.Endpoints"/> override,
    /// when set).</description></item>
    /// <item><description>Registers <see cref="ICoilProClient"/> → <see cref="CoilProClient"/> (registered with
    /// <c>TryAdd</c>), backed by the <see cref="ApiHttpClientName"/> client, the resolved
    /// <see cref="ITeslaTokenProvider"/>, and a <see cref="CoilProClientOptions"/> instance — defaulted, or customized
    /// via <paramref name="configureClient"/>.</description></item>
    /// <item><description>Registers <see cref="ICoilProEnergyClient"/> → <see cref="CoilProEnergyClient"/> (registered with
    /// <c>TryAdd</c>) for Powerwall, solar and Wall Connector sites, sharing the same <see cref="ApiHttpClientName"/>
    /// client, token provider and <see cref="CoilProClientOptions"/>. Its calls need the <c>energy_device_data</c> /
    /// <c>energy_cmds</c> scopes.</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">Callback that sets <see cref="TeslaAuthOptions.ClientId"/> and other required options.</param>
    /// <param name="configureClient">Optional callback to customize <see cref="CoilProClientOptions"/> (retry/backoff
    /// tunables) for the registered <see cref="ICoilProClient"/>. When omitted, defaults are used.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddCoilProApi(
        this IServiceCollection services,
        Action<TeslaAuthOptions> configure,
        Action<CoilProClientOptions>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<TeslaAuthOptions>().Configure(configure);

        // TeslaAuthClient takes a plain TeslaAuthOptions (not IOptions<TeslaAuthOptions>); bridge the two so it
        // can be constructed via normal DI activation.
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<TeslaAuthOptions>>().Value);

        services.AddHttpClient(HttpClientName);

        services.TryAddSingleton<ITeslaAuthClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var http = httpClientFactory.CreateClient(HttpClientName);
            var options = sp.GetRequiredService<TeslaAuthOptions>();
            return new TeslaAuthClient(http, options);
        });

        services.TryAddSingleton<ITokenStore, InMemoryTokenStore>();

        services.TryAddSingleton<ITeslaTokenProvider>(sp =>
            new TeslaTokenProvider(
                sp.GetRequiredService<ITokenStore>(),
                sp.GetRequiredService<ITeslaAuthClient>(),
                sp.GetService<TimeProvider>()));

        // CoilProClientOptions has no meaningful "reload" semantics (plain retry/backoff tunables), so a single
        // resolved-and-configured instance is registered directly rather than round-tripping through IOptions.
        var clientOptions = new CoilProClientOptions();
        configureClient?.Invoke(clientOptions);
        services.TryAddSingleton(clientOptions);

        services.AddHttpClient(ApiHttpClientName, (sp, http) =>
        {
            var authOptions = sp.GetRequiredService<TeslaAuthOptions>();
            var endpoints = authOptions.Endpoints ?? TeslaEndpoints.For(authOptions.Region);
            http.BaseAddress = endpoints.FleetApiBaseUrl;
        });

        services.TryAddSingleton<ICoilProClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var http = httpClientFactory.CreateClient(ApiHttpClientName);
            var tokenProvider = sp.GetRequiredService<ITeslaTokenProvider>();
            var options = sp.GetRequiredService<CoilProClientOptions>();
            return new CoilProClient(http, tokenProvider, options);
        });

        services.TryAddSingleton<ICoilProEnergyClient>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var http = httpClientFactory.CreateClient(ApiHttpClientName);
            var tokenProvider = sp.GetRequiredService<ITeslaTokenProvider>();
            var options = sp.GetRequiredService<CoilProClientOptions>();
            return new CoilProEnergyClient(http, tokenProvider, options);
        });

        return services;
    }
}
