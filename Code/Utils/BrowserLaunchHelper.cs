using System;
using System.Diagnostics;

namespace RPGGame.Utils
{
    /// <summary>Opens http(s) links in the system default browser.</summary>
    public static class BrowserLaunchHelper
    {
        public static bool TryOpenUrl(string? url)
        {
            string trimmed = (url ?? "").Trim();
            if (trimmed.Length == 0)
                return false;
            if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = trimmed,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BrowserLaunchHelper.TryOpenUrl: {ex.Message}");
                return false;
            }
        }
    }
}
