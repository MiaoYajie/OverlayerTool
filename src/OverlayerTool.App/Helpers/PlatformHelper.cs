using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OverlayerTool.App.Helpers;

public static class PlatformHelper
{
    public static void OpenFolder(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", path);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
            return;
        }

        Process.Start("xdg-open", path);
    }
}
