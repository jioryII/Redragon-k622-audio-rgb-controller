using Microsoft.Win32;
using System.Diagnostics;

namespace K622RGBController.Services;

/// <summary>
/// Manages auto-start with Windows via the Registry Run key.
/// </summary>
public static class StartupManager
{
    private const string AppName = "K622RGBController";
    private const string RunKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Check if the app is set to start with Windows.
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enable or disable starting with Windows.
    /// </summary>
    public static void SetStartup(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (!string.IsNullOrEmpty(exePath))
                {
                    // Add --minimized flag so the app starts hidden in the tray
                    key.SetValue(AppName, $"\"{exePath}\" --minimized");
                    Debug.WriteLine($"Startup enabled: {exePath}");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                Debug.WriteLine("Startup disabled");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error setting startup: {ex.Message}");
        }
    }
}
