using System.Diagnostics;
using System.Runtime.InteropServices;

namespace K622RGBController.Services;

/// <summary>
/// Monitors Windows for notification events and produces wave animation data.
/// Uses Win32 EnumWindows to detect toast notification windows.
/// </summary>
public class NotificationListener : IDisposable
{
    // Win32 API declarations
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextW(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    // Known notification window classes
    private static readonly HashSet<string> NotifClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "windows.ui.core.corewindow",
        "applicationframewindow",
    };

    // Known notification title keywords
    private static readonly string[] NotifKeywords =
    {
        "notification", "notificación", "notificacion",
        "new notification", "nueva notificación",
        "toast", "action center", "centro de actividades",
    };

    private Thread? _thread;
    private volatile bool _running;

    // Public settings
    public bool Enabled { get; set; } = true;
    public (byte R, byte G, byte B) FlashColor { get; set; } = (255, 255, 255);
    public double FlashDuration { get; set; } = 2.0;
    public double FlashIntensity { get; set; } = 1.0;

    // Wave animation state (read by EffectEngine)
    public bool IsActive { get; private set; }
    public double FlashAlpha { get; private set; }
    public double WaveProgress { get; private set; }

    private double _waveStartTime;
    private double _lastTriggerTime;
    private const double Cooldown = 3.0;
    private readonly HashSet<IntPtr> _knownNotifHwnds = new();

    public void Start()
    {
        if (_running) return;
        _running = true;
        _thread = new Thread(MonitorLoop) { IsBackground = true, Name = "NotifListener" };
        _thread.Start();
        Debug.WriteLine("Notification listener started");
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(2000);
        _thread = null;
        IsActive = false;
        Debug.WriteLine("Notification listener stopped");
    }

    public void TriggerNotification()
    {
        double now = GetTime();
        if (now - _lastTriggerTime < 1.0) return;
        _lastTriggerTime = now;
        StartWave();
        Debug.WriteLine("Notification wave triggered");
    }

    private void StartWave()
    {
        IsActive = true;
        _waveStartTime = GetTime();
        FlashAlpha = 1.0;
        WaveProgress = 0.0;
    }

    private void UpdateWave()
    {
        if (!IsActive) return;

        double elapsed = GetTime() - _waveStartTime;
        double duration = FlashDuration;

        if (elapsed >= duration)
        {
            IsActive = false;
            FlashAlpha = 0.0;
            WaveProgress = 1.0;
            return;
        }

        double t = elapsed / duration;

        // Wave progress: 0 → 1 over the first 70% of duration
        WaveProgress = t < 0.7 ? t / 0.7 : 1.0;

        // Alpha: full for 60%, then fade out
        if (t < 0.6)
            FlashAlpha = 1.0;
        else
            FlashAlpha = Math.Max(0.0, 1.0 - (t - 0.6) / 0.4);
    }

    private void MonitorLoop()
    {
        while (_running)
        {
            try
            {
                UpdateWave();

                if (Enabled && !IsActive)
                    CheckNotifications();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Notification monitor error: {ex.Message}");
            }

            Thread.Sleep(150);
        }
    }

    private void CheckNotifications()
    {
        double now = GetTime();
        if (now - _lastTriggerTime < Cooldown) return;

        bool foundNotif = false;
        var classBuffer = new char[256];
        var titleBuffer = new char[512];

        EnumWindows((hWnd, _) =>
        {
            if (!IsWindowVisible(hWnd)) return true;
            if (_knownNotifHwnds.Contains(hWnd)) return true;

            try
            {
                GetClassNameW(hWnd, classBuffer, classBuffer.Length);
                string className = new string(classBuffer).TrimEnd('\0').ToLower();

                GetWindowTextW(hWnd, titleBuffer, titleBuffer.Length);
                string title = new string(titleBuffer).TrimEnd('\0').ToLower();

                bool isNotif = false;

                if (NotifClasses.Contains(className))
                {
                    foreach (var kw in NotifKeywords)
                    {
                        if (title.Contains(kw))
                        {
                            isNotif = true;
                            break;
                        }
                    }
                }

                if (title.Contains("shellexperiencehost") ||
                    className.Contains("windows.ui.notifications"))
                {
                    isNotif = true;
                }

                if (isNotif)
                {
                    _knownNotifHwnds.Add(hWnd);
                    foundNotif = true;
                    return false; // Stop enumeration
                }
            }
            catch { }

            return true;
        }, IntPtr.Zero);

        // Also check foreground window
        try
        {
            var fg = GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                GetWindowTextW(fg, titleBuffer, titleBuffer.Length);
                string title = new string(titleBuffer).TrimEnd('\0').ToLower();
                if ((title.Contains("notification") || title.Contains("notificación") ||
                     title.Contains("action center")) && !_knownNotifHwnds.Contains(fg))
                {
                    _knownNotifHwnds.Add(fg);
                    foundNotif = true;
                }
            }
        }
        catch { }

        if (foundNotif)
        {
            _lastTriggerTime = now;
            StartWave();
            Debug.WriteLine("Notification wave triggered (detected)");
        }

        // Clean up old handles periodically
        if (_knownNotifHwnds.Count > 100)
            _knownNotifHwnds.Clear();
    }

    private static double GetTime()
    {
        return Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
