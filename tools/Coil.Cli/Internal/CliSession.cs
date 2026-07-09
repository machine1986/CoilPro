using System.Text.Json;
using System.Text.Json.Serialization;

namespace Coil.Cli.Internal;

/// <summary>
/// The minimal session context the CLI persists so <c>coil vehicles</c>/<c>coil data</c> can rebuild a
/// <see cref="TeslaAuthOptions"/> and <see cref="TeslaTokenProvider"/> for an already-authenticated
/// account without re-running <c>coil login</c>.
/// </summary>
/// <remarks>
/// The client SECRET is intentionally never stored here — only the client id and region. Token refresh
/// (<see cref="TeslaAuthClient.RefreshAsync"/>) doesn't require the secret, so this is enough to keep a
/// public/native client working across CLI invocations. Persisted as JSON alongside the tokens
/// (see <see cref="CoilPaths.SessionFile"/>).
/// </remarks>
internal sealed record CliSession
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>OAuth client id used to log in.</summary>
    public required string ClientId { get; init; }

    /// <summary>Fleet API region selected at login.</summary>
    public required TeslaRegion Region { get; init; }

    /// <summary>The default path the session is persisted to.</summary>
    public static string DefaultPath => CoilPaths.SessionFile;

    /// <summary>Loads the persisted session, or null if none exists (or it's unreadable/corrupt).</summary>
    public static async ValueTask<CliSession?> LoadAsync(string? path = null, CancellationToken ct = default)
    {
        path ??= DefaultPath;
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        try
        {
            return await JsonSerializer.DeserializeAsync<CliSession>(stream, JsonOptions, ct).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Persists this session, replacing any existing one.</summary>
    public async ValueTask SaveAsync(string? path = null, CancellationToken ct = default)
    {
        path ??= DefaultPath;
        CoilPaths.EnsureConfigDir();
        var json = JsonSerializer.Serialize(this, JsonOptions);
        await AtomicFileWriter.WriteAsync(path, json, ct).ConfigureAwait(false);
    }
}
