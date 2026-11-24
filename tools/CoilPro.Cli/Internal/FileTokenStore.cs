using System.Text.Json;

namespace CoilPro.Cli.Internal;

/// <summary>
/// <see cref="ITokenStore"/> that persists <see cref="TeslaTokens"/> as JSON to
/// <c>~/.coilpro/tokens.json</c> (see <see cref="CoilProPaths"/> for the exact resolution rules).
/// Writes are atomic (temp file + rename) and, on non-Windows platforms, restricted to the
/// current user. This is the CLI's persistent counterpart to <see cref="InMemoryTokenStore"/>.
/// </summary>
internal sealed class FileTokenStore : ITokenStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _path;

    /// <summary>Creates a store backed by <paramref name="path"/>, or the default <c>~/.coilpro/tokens.json</c> when null.</summary>
    public FileTokenStore(string? path = null)
    {
        _path = path ?? CoilProPaths.TokensFile;
    }

    /// <summary>The default path tokens are persisted to.</summary>
    public static string DefaultPath => CoilProPaths.TokensFile;

    /// <inheritdoc />
    public async ValueTask<TeslaTokens?> GetAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_path))
        {
            return null;
        }

        await using var stream = File.OpenRead(_path);
        try
        {
            return await JsonSerializer.DeserializeAsync<TeslaTokens>(stream, JsonOptions, ct).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            // A corrupt/foreign-format token file shouldn't crash the CLI — treat it as "no tokens".
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask SaveAsync(TeslaTokens tokens, CancellationToken ct = default)
    {
        CoilProPaths.EnsureConfigDir();
        var json = JsonSerializer.Serialize(tokens, JsonOptions);
        await AtomicFileWriter.WriteAsync(_path, json, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask ClearAsync(CancellationToken ct = default)
    {
        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // Best-effort.
        }

        return default;
    }
}
