namespace Coil.Cli.Internal;

/// <summary>
/// Resolves the directory the CLI persists its state in (tokens + session; see
/// <see cref="Coil.Cli.Internal.FileTokenStore"/> and <see cref="Coil.Cli.Internal.CliSession"/>).
/// </summary>
/// <remarks>
/// Defaults to <c>~/.coil</c>. Honors <c>%APPDATA%</c> on Windows and <c>$XDG_CONFIG_HOME</c>
/// elsewhere when set, and <c>COIL_CONFIG_DIR</c> everywhere as an explicit override (handy for tests
/// and for anyone who wants to keep multiple accounts side by side).
/// </remarks>
internal static class CoilPaths
{
    private const string DirName = "coil";

    /// <summary>The resolved config directory (not guaranteed to exist yet — call <see cref="EnsureConfigDir"/>).</summary>
    public static string ConfigDir { get; } = ResolveConfigDir();

    /// <summary>Path to the persisted OAuth tokens.</summary>
    public static string TokensFile => Path.Combine(ConfigDir, "tokens.json");

    /// <summary>Path to the persisted CLI session (client id + region).</summary>
    public static string SessionFile => Path.Combine(ConfigDir, "session.json");

    /// <summary>
    /// Creates <see cref="ConfigDir"/> if it doesn't exist yet, restricting it to the current user on
    /// platforms that support POSIX permissions. Best-effort: failures to tighten permissions are ignored.
    /// </summary>
    public static void EnsureConfigDir()
    {
        Directory.CreateDirectory(ConfigDir);

        if (!OperatingSystem.IsWindows())
        {
            try
            {
                File.SetUnixFileMode(ConfigDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch
            {
                // Best-effort; some filesystems (e.g. certain network mounts) don't support POSIX modes.
            }
        }
    }

    private static string ResolveConfigDir()
    {
        var overrideDir = Environment.GetEnvironmentVariable("COIL_CONFIG_DIR");
        if (!string.IsNullOrEmpty(overrideDir))
        {
            return overrideDir;
        }

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrEmpty(appData))
            {
                return Path.Combine(appData, DirName);
            }
        }
        else
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            if (!string.IsNullOrEmpty(xdgConfigHome))
            {
                return Path.Combine(xdgConfigHome, DirName);
            }
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home))
        {
            home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        return Path.Combine(home, $".{DirName}");
    }
}
