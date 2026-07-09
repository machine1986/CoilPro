using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Coil.AspNetCore;

/// <summary>
/// Dependency-injection extensions that wire up the Coil.NET Tesla Fleet API SDK
/// (<see cref="ITeslaAuthClient"/>, <see cref="ITokenStore"/>) for ASP.NET Core / generic-host apps.
/// </summary>
public static class CoilServiceCollectionExtensions
{
    /// <summary>Logical name of the <see cref="HttpClient"/> registered for <see cref="ITeslaAuthClient"/>.</summary>
    public const string HttpClientName = "Coil.Auth";

    /// <summary>
    /// Registers the Coil.NET Tesla Fleet API client services:
    /// <list type="bullet">
    /// <item><description>Binds/configures <see cref="TeslaAuthOptions"/> via <paramref name="configure"/>.</description></item>
    /// <item><description>Registers <see cref="ITeslaAuthClient"/> → <see cref="TeslaAuthClient"/>, backed by a
    /// named <see cref="HttpClient"/> (<see cref="HttpClientName"/>) obtained from <c>IHttpClientFactory</c>.</description></item>
    /// <item><description>Registers <see cref="ITokenStore"/> → <see cref="InMemoryTokenStore"/> as the default
    /// (registered with <c>TryAdd</c>, so callers can register their own <see cref="ITokenStore"/> — e.g. one backed
    /// by ASP.NET Core Data Protection — before or after calling this method).</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="configure">Callback that sets <see cref="TeslaAuthOptions.ClientId"/> and other required options.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddCoilApi(this IServiceCollection services, Action<TeslaAuthOptions> configure)
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

        return services;
    }
}
