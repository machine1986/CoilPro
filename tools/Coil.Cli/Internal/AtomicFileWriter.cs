namespace Coil.Cli.Internal;

/// <summary>Writes text files atomically (temp file + rename) so a crash never leaves a half-written file.</summary>
internal static class AtomicFileWriter
{
    /// <summary>
    /// Writes <paramref name="contents"/> to <paramref name="path"/> via a temp file in the same directory,
    /// then an atomic rename. On non-Windows platforms the temp file (and thus the final file, since rename
    /// preserves it) is restricted to the current user before the rename.
    /// </summary>
    public static async Task WriteAsync(string path, string contents, CancellationToken ct = default)
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
                    File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
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
