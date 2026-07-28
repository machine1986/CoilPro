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

    /// <summary>Directory holding the Vehicle Command Protocol key pair.</summary>
    public static string KeysDir => Path.Combine(ConfigDir, "keys");

    /// <summary>Path to the P-256 private key (PKCS#8 PEM). Secret — owner-readable only.</summary>
    public static string PrivateKeyFile => Path.Combine(KeysDir, "private-key.pem");

    /// <summary>
    /// Path to the public key (SubjectPublicKeyInfo PEM) — the file to publish at
    /// <c>/.well-known/appspecific/com.tesla.3p.public-key.pem</c>.
    /// </summary>
    public static string PublicKeyFile => Path.Combine(KeysDir, "public-key.pem");

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
