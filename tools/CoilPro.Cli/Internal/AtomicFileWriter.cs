namespace CoilPro.Cli.Internal;

/// <summary>Writes text files atomically (temp file + rename) so a crash never leaves a half-written file.</summary>
internal static class AtomicFileWriter
{
    /// <summary>Owner read/write only (<c>0600</c>) — the default, for secrets such as tokens and private keys.</summary>
    public const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Owner read/write, everyone else read (<c>0644</c>) — for files meant to be published, such as the
    /// public key. Tools like <c>sftp put</c> carry the local mode over to the server, and a <c>0600</c> file
    /// there makes the web server answer <c>403 Forbidden</c>.
    /// </summary>
    public const UnixFileMode WorldReadable = OwnerOnly | UnixFileMode.GroupRead | UnixFileMode.OtherRead;

    /// <summary>
    /// Writes <paramref name="contents"/> to <paramref name="path"/> via a temp file in the same directory,
    /// then an atomic rename. On non-Windows platforms the temp file (and thus the final file, since rename
    /// preserves it) gets <paramref name="mode"/> before the rename — <see cref="OwnerOnly"/> unless the
    /// caller says otherwise.
    /// </summary>
    public static async Task WriteAsync(string path, string contents, CancellationToken ct = default, UnixFileMode mode = OwnerOnly)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var tempPath = Path.Combine(
            string.IsNullOrEmpty(dir) ? "." : dir,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(tempPath, contents, ct).ConfigureAwait(false);

            if (!OperatingSystem.IsWindows())
            {
                try
                {
                    File.SetUnixFileMode(tempPath, mode);
                }
                catch
                {
                    // Best-effort; not fatal if the filesystem doesn't support POSIX permissions.
                }
            }

            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    // Best-effort cleanup of a leftover temp file (e.g. if the move itself failed).
                }
            }
        }
    }
}
