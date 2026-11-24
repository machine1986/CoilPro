using Coil.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Coil.AspNetCore;

/// <summary>
/// Dependency-injection extensions for the signed vehicle commands of <c>Coil.Commands</c>
/// (<see cref="ICoilCommands"/>). Call <see cref="CoilServiceCollectionExtensions.AddCoilApi"/> first — the
/// commands reuse its Fleet API <see cref="HttpClient"/>, <see cref="ITeslaTokenProvider"/> and
/// <see cref="ICoilClient"/>.
/// </summary>
public static class CoilCommandsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ICoilCommands"/> → <see cref="CoilCommands"/> as a singleton. The
    /// <see cref="TeslaKeyPair"/> must be registered separately, for example
    /// <c>services.AddSingleton(TeslaKeyPair.FromPrivateKeyPem(pem))</c> — or use one of the overloads that
    /// takes a PEM string or a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional callback to customize <see cref="CoilCommandsOptions"/> (AutoWake, timeouts).</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddCoilCommands(
        this IServiceCollection services,
        Action<CoilCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CoilCommandsOptions();
        configure?.Invoke(options);
        services.TryAddSingleton(options);

        services.TryAddSingleton<ICoilCommands>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient(CoilServiceCollectionExtensions.ApiHttpClientName);
            return new CoilCommands(
                http,
                sp.GetRequiredService<ITeslaTokenProvider>(),
                sp.GetRequiredService<TeslaKeyPair>(),
                sp.GetService<ICoilClient>(),
                sp.GetRequiredService<CoilCommandsOptions>());
        });

        return services;
    }

    /// <summary>
    /// Like <see cref="AddCoilCommands(IServiceCollection, Action{CoilCommandsOptions}?)"/>, and also registers the
    /// <see cref="TeslaKeyPair"/> parsed from <paramref name="privateKeyPem"/> (PKCS#8 or SEC1 P-256 PEM, e.g. the
    /// content of the file written by <c>coil keys generate</c>).
    /// </summary>
    public static IServiceCollection AddCoilCommands(
        this IServiceCollection services,
        string privateKeyPem,
        Action<CoilCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);

        services.TryAddSingleton(_ => TeslaKeyPair.FromPrivateKeyPem(privateKeyPem));
        return services.AddCoilCommands(configure);
    }

    /// <summary>
    /// Like <see cref="AddCoilCommands(IServiceCollection, Action{CoilCommandsOptions}?)"/>, and also registers the
    /// <see cref="TeslaKeyPair"/> produced by <paramref name="keyPairFactory"/> (e.g. loading the key from a secret store).
    /// The container disposes the key pair on shutdown.
    /// </summary>
    public static IServiceCollection AddCoilCommands(
        this IServiceCollection services,
        Func<IServiceProvider, TeslaKeyPair> keyPairFactory,
        Action<CoilCommandsOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(keyPairFactory);

        services.TryAddSingleton(keyPairFactory);
        return services.AddCoilCommands(configure);
    }
}
