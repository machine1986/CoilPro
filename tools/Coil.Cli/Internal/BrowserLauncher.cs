using System.Diagnostics;

namespace Coil.Cli.Internal;

/// <summary>Best-effort cross-platform "open URL in the default browser" helper.</summary>
internal static class BrowserLauncher
{
    /// <summary>Attempts to open <paramref name="url"/> in the user's default browser. Never throws.</summary>
    public static bool TryOpen(Uri url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                Process.Start(new ProcessStartInfo("open", $"\"{url}\"") { UseShellExecute = false });
            }
            else
            {
                Process.Start(new ProcessStartInfo("xdg-open", $"\"{url}\"") { UseShellExecute = false });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
