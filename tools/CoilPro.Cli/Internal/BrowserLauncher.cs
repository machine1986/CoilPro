using System.Diagnostics;

namespace CoilPro.Cli.Internal;

/// <summary>Best-effort cross-platform "open URL in the default browser" helper.</summary>
internal static class BrowserLauncher
{
    /// <summary>Attempts to open <paramref name="url"/> in the user's default browser. Never throws.</summary>
    public static bool TryOpen(Uri url)
    {
        // AbsoluteUri, never ToString(): ToString() unescapes "%20" to a space. macOS `open` then treats the
        // string as an invalid URL and percent-encodes all of it again, "%" included, so an already encoded
        // redirect_uri arrives at Tesla as "http%253A%252F%252F..." and is rejected as "not registered".
        var target = url.AbsoluteUri;

        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{target}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("open", $"\"{target}\"") { UseShellExecute = false });
            }
            else
            {
                Process.Start(new ProcessStartInfo("xdg-open", $"\"{target}\"") { UseShellExecute = false });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
