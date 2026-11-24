using CoilPro.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoilPro.AspNetCore;

/// <summary>
/// Dependency-injection extensions for the signed vehicle commands of <c>CoilPro.Commands</c>
/// (<see cref="ICoilProCommands"/>). Call <see cref="CoilProServiceCollectionExtensions.AddCoilProApi"/> first — the
/// commands reuse its Fleet API <see cref="HttpClient"/>, <see cref="ITeslaTokenProvider"/> and
/// <see cref="ICoilProClient"/>.
/// </summary>
public static class CoilProCommandsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICoilProCommands"/> → <see cref="CoilProCommands"/> and
    /// <see cref="IFleetTelemetryConfigurator"/> → <see cref="FleetTelemetryConfigurator"/> (signed Fleet Telemetry
    /// configuration) as singletons. The
    /// <see cref="TeslaKeyPair"/> must be registered separately, for example
    /// <c>services.AddSingleton(TeslaKeyPair.FromPrivateKeyPem(pem))</c> — or use one of the overloads that
    /// takes a PEM string or a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional callback to customize <see cref="CoilProCommandsOptions"/> (AutoWake, timeouts).</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddCoilProCommands(
        this IServiceCollection services,
        Action<CoilProCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CoilProCommandsOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<ICoilProCommands>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(CoilProServiceCollectionExtensions.ApiHttpClientName);
            return new CoilProCommands(
                http,
                sp.GetRequiredService<ITeslaTokenProvider>(),
                sp.GetRequiredService<TeslaKeyPair>(),
                sp.GetService<ICoilProClient>(),
                sp.GetRequiredService<CoilProCommandsOptions>());
        });

        services.TryAddSingleton<IFleetTelemetryConfigurator>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(CoilProServiceCollectionExtensions.ApiHttpClientName);
            return new FleetTelemetryConfigurator(
                http,
                sp.GetRequiredService<ITeslaTokenProvider>(),
                sp.GetRequiredService<TeslaKeyPair>());
        });

        return services;
    }

    /// <summary>
    /// Like <see cref="AddCoilProCommands(IServiceCollection, Action{CoilProCommandsOptions}?)"/>, and also registers the
    /// <see cref="TeslaKeyPair"/> parsed from <paramref name="privateKeyPem"/> (PKCS#8 or SEC1 P-256 PEM, e.g. the
    /// content of the file written by <c>coilpro keys generate</c>).
    /// </summary>
    public static IServiceCollection AddCoilProCommands(
        this IServiceCollection services,
        string privateKeyPem,
        Action<CoilProCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        services.TryAddSingleton(_ => TeslaKeyPair.FromPrivateKeyPem(privateKeyPem));
        return services.AddCoilProCommands(configure);
    }

    /// <summary>
    /// Like <see cref="AddCoilProCommands(IServiceCollection, Action{CoilProCommandsOptions}?)"/>, and also registers the
    /// <see cref="TeslaKeyPair"/> produced by <paramref name="keyPairFactory"/> (e.g. loading the key from a secret store).
    /// The container disposes the key pair on shutdown.
    /// </summary>
    public static IServiceCollection AddCoilProCommands(
        this IServiceCollection services,
        Func<IServiceProvider, TeslaKeyPair> keyPairFactory,
        Action<CoilProCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(keyPairFactory);

        services.TryAddSingleton(keyPairFactory);
        return services.AddCoilProCommands(configure);
    }
}
