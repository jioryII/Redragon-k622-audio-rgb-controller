using K622RGBController.Helpers;
using K622RGBController.Models;
using System.Diagnostics;

namespace K622RGBController.Services;

/// <summary>
/// 360° directional spectrum visualizer engine.
/// Projects 16 frequency bands onto the physical keyboard layout
/// using a configurable angle vector.
/// </summary>
public class EffectEngine : IDisposable
{
    private readonly HidController _hid;
    private readonly AudioAnalyzer _audio;
    private readonly NotificationListener _notif;

    private Thread? _thread;
    private volatile bool _running;

    public EffectMode Mode { get; set; } = EffectMode.Music;
    public int TargetFps { get; set; } = 30;
    public double ActualFps { get; private set; }

    // ── Spectrum settings ──
    public int[][] GradientColors { get; set; }
    private (byte R, byte G, byte B)[] _gradientLut;

    public double Brightness { get; set; } = 1.0;
    public double Angle { get; set; } = 0.0;
    public double Fluidity { get; set; } = 0.5;

    // ── Idle Mode settings ──
    public bool IdleEnabled { get; set; } = true;
    public double IdleIntensity { get; set; } = 0.6;
    public double IdleSpeed { get; set; } = 0.5;
    public int[][] IdleColors { get; set; }
    private (byte R, byte G, byte B)[] _idleGradientLut;
    
    private double _idleTime = 0;
    private double _idleFade = 0;

    private (byte R, byte G, byte B)[]? _staticColors;
    private double _time;
    private int _numKeys;

    // Key coordinate data
    private readonly (int Col, int Row, double X, double Y, string Name)[] _keyData;

    /// <summary>
    /// Event fired after each frame with the computed colors.
    /// Subscribe to update the GUI keyboard preview.
    /// </summary>
    public event Action<(byte R, byte G, byte B)[]>? FrameRendered;

    public EffectEngine(HidController hid, AudioAnalyzer audio, NotificationListener notif)
    {
        _hid = hid;
        _audio = audio;
        _notif = notif;
        _numKeys = KeyboardLayout.TotalKeys;

        GradientColors = new[]
        {
            new[] { 0, 255, 65 },
            new[] { 255, 255, 0 },
            new[] { 255, 120, 0 },
            new[] { 255, 0, 60 },
        };
        
        IdleColors = new[]
        {
            new[] { 30, 0, 150 },
            new[] { 0, 200, 255 },
            new[] { 0, 0, 0 },
            new[] { 100, 0, 200 },
        };
        
        _gradientLut = ColorHelper.BuildGradientLut(GradientColors);
        _idleGradientLut = ColorHelper.BuildGradientLut(IdleColors);

        // Build key coordinate system
        _keyData = BuildKeyCoords();
    }

    private static (int Col, int Row, double X, double Y, string Name)[] BuildKeyCoords()
    {
        var keys = new List<(int, int, double, double, string)>();
        int numCols = KeyboardLayout.NumCols;
        int numRows = KeyboardLayout.NumRows;

        // Column-major iteration (same order as key_idx)
        for (int col = 0; col < numCols; col++)
        {
            for (int row = 0; row < numRows; row++)
            {
                string key = KeyboardLayout.Layout[row, col];
                if (key != "NAN")
                {
                    double x = (double)col / Math.Max(1, numCols - 1);
                    double y = (double)row / Math.Max(1, numRows - 1);
                    keys.Add((col, row, x, y, key));
                }
            }
        }

        return keys.ToArray();
    }

    public void UpdateGradient()
    {
        _gradientLut = ColorHelper.BuildGradientLut(GradientColors);
        _idleGradientLut = ColorHelper.BuildGradientLut(IdleColors);
    }

    public void SetStaticColors((byte R, byte G, byte B)[] colors)
    {
        _staticColors = (colors.Clone() as (byte, byte, byte)[])!;
    }

    public void Start()
    {
        if (_running) return;

        if (!_hid.Connected)
        {
            if (!_hid.Connect())
            {
                Debug.WriteLine("Cannot start: HID connection failed");
                return;
            }
        }

        UpdateGradient();
        _running = true;
        _time = 0.0;
        _thread = new Thread(RenderLoop) { IsBackground = true, Name = "EffectEngine", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
        Debug.WriteLine($"Effect engine started at {TargetFps} FPS target");
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(2000);
        _thread = null;
        Debug.WriteLine("Effect engine stopped");
    }

    private void RenderLoop()
    {
        double frameTime = 1.0 / Math.Max(1, TargetFps);
        var sw = Stopwatch.StartNew();
        long fpsTimer = sw.ElapsedTicks;
        int fpsCount = 0;

        while (_running)
        {
            long tStart = sw.ElapsedTicks;

            try
            {
                var colors = ComputeFrame();
                if (colors != null)
                {
                    _hid.SendColors(colors, Brightness);
                    FrameRendered?.Invoke(colors);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Render error: {ex.Message}");
            }

            _time += frameTime;
            fpsCount++;

            double elapsedFps = (double)(sw.ElapsedTicks - fpsTimer) / Stopwatch.Frequency;
            if (elapsedFps >= 1.0)
            {
                ActualFps = fpsCount / elapsedFps;
                fpsCount = 0;
                fpsTimer = sw.ElapsedTicks;
            }

            double tElapsed = (double)(sw.ElapsedTicks - tStart) / Stopwatch.Frequency;
            double sleepTime = frameTime - tElapsed;
            if (sleepTime > 0.001)
            {
                Thread.Sleep((int)(sleepTime * 1000));
            }
        }
    }

    private (byte R, byte G, byte B)[]? ComputeFrame()
    {
        int n = _numKeys;
        (byte R, byte G, byte B)[] colors;

        switch (Mode)
        {
            case EffectMode.Music:
                colors = ComputeSpectrumFrame(n);
                break;
            case EffectMode.Static:
                colors = ComputeStaticFrame(n);
                break;
            default:
                colors = new (byte, byte, byte)[n];
                break;
        }

        if (Mode == EffectMode.Music)
        {
            ApplyIdleModeOverlay(colors);
        }

        // Notification overlay (highest priority)
        if (_notif.Enabled && _notif.IsActive)
        {
            ApplyNotificationWave(colors);
        }

        return colors;
    }

    private (byte R, byte G, byte B)[] ComputeSpectrumFrame(int numKeys)
    {
        var colors = new (byte R, byte G, byte B)[numKeys];

        if (!_audio.IsRunning) return colors;

        var (spectrum, _) = _audio.GetSpectrum();
        double t = _time;

        // Direction vector from angle
        double angleRad = Angle * Math.PI / 180.0;
        double dx = Math.Cos(angleRad);
        double dy = Math.Sin(angleRad);

        for (int keyIdx = 0; keyIdx < Math.Min(numKeys, _keyData.Length); keyIdx++)
        {
            var kd = _keyData[keyIdx];

            // Center coordinates around (0.5, 0.5) then project
            double cx = kd.X - 0.5;
            double cy = kd.Y - 0.5;

            // Dot product with direction vector
            double proj = cx * dx + cy * dy;

            // Normalize to 0.0–1.0
            double projNorm = Math.Clamp(proj / 0.72 * 0.5 + 0.5, 0.0, 1.0);

            // Map to band index (0–15)
            int bandIdx = Math.Clamp((int)(projNorm * 15.999), 0, 15);
            double bandEnergy = bandIdx < spectrum.Length ? spectrum[bandIdx] : 0.0;

            if (bandEnergy < 0.02) continue;

            // Get gradient color based on projection position
            int lutIdx = Math.Clamp((int)(projNorm * 255), 0, 255);
            var baseColor = _gradientLut[lutIdx];

            // Apply energy as brightness
            double bright = bandEnergy;

            // Subtle shimmer when significant energy
            if (bandEnergy > 0.15)
            {
                double shimmer = 1.0 + 0.04 * Math.Sin(t * 3.0 + kd.X * 5.0 + kd.Y * 3.0);
                bright *= shimmer;
            }

            colors[keyIdx] = (
                (byte)Math.Clamp((int)(baseColor.R * bright), 0, 255),
                (byte)Math.Clamp((int)(baseColor.G * bright), 0, 255),
                (byte)Math.Clamp((int)(baseColor.B * bright), 0, 255)
            );
        }

        return colors;
    }

    private (byte R, byte G, byte B)[] ComputeStaticFrame(int numKeys)
    {
        if (_staticColors != null && _staticColors.Length >= numKeys)
            return _staticColors[..numKeys];

        if (_staticColors != null)
        {
            var padded = new (byte R, byte G, byte B)[numKeys];
            Array.Copy(_staticColors, padded, _staticColors.Length);
            return padded;
        }

        var result = new (byte R, byte G, byte B)[numKeys];
        Array.Fill(result, ((byte)40, (byte)40, (byte)40));
        return result;
    }

    private void ApplyNotificationWave((byte R, byte G, byte B)[] colors)
    {
        if (!_notif.IsActive) return;

        double alpha = _notif.FlashAlpha;
        if (alpha <= 0.0) return;

        var (nr, ng, nb) = _notif.FlashColor;
        double intensity = _notif.FlashIntensity;
        double progress = _notif.WaveProgress;

        for (int keyIdx = 0; keyIdx < Math.Min(colors.Length, _keyData.Length); keyIdx++)
        {
            var kd = _keyData[keyIdx];

            // Wave front: sweeps from top-right (1,0) to bottom-left (0,1)
            double waveDist = (1.0 - kd.X) + kd.Y;
            double waveDistNorm = waveDist / 2.0;

            double waveWidth = 0.35;
            double waveCenter = progress * 1.4 - 0.2;
            double distToWave = Math.Abs(waveDistNorm - waveCenter);

            if (distToWave < waveWidth)
            {
                double waveAlpha = (1.0 - distToWave / waveWidth) * alpha * intensity;

                var (r, g, b) = colors[keyIdx];
                colors[keyIdx] = (
                    (byte)Math.Min(255, (int)(r * (1.0 - waveAlpha) + nr * waveAlpha)),
                    (byte)Math.Min(255, (int)(g * (1.0 - waveAlpha) + ng * waveAlpha)),
                    (byte)Math.Min(255, (int)(b * (1.0 - waveAlpha) + nb * waveAlpha))
                );
            }
        }
    }

    private void ApplyIdleModeOverlay((byte R, byte G, byte B)[] colors)
    {
        if (!IdleEnabled) return;
        
        var (_, _, _, overall) = _audio.GetLevels();
        
        if (overall < 0.005)
            _idleTime += (1.0 / Math.Max(1, TargetFps));
        else
            _idleTime = 0;
            
        // Start fading into idle after 5 seconds of silence
        if (_idleTime > 5.0)
            _idleFade = Math.Min(1.0, _idleFade + 0.015);
        else
            _idleFade = Math.Max(0.0, _idleFade - 0.035);

        if (_idleFade <= 0.01) return;

        double t = _time * Math.Max(0.1, IdleSpeed);
        
        for (int i = 0; i < Math.Min(colors.Length, _keyData.Length); i++)
        {
            var kd = _keyData[i];
            
            // Generate some random organic blobs using wandering sine waves
            double blob1 = Math.Sin(kd.X * 3.0 + t * 2.1) * Math.Cos(kd.Y * 2.5 - t * 1.5);
            double blob2 = Math.Sin(kd.Y * 4.0 + t * 1.8 + Math.Sin(kd.X * 2.0));
            double noise = (blob1 + blob2) * 0.5; // range roughly -1 to 1
            
            // Map to 0-1 range for LUT indexing
            double val = (noise + 1.0) * 0.5;
            
            // Map to Idle LUT
            int lutIdx = Math.Clamp((int)(val * 255), 0, 255);
            var idleCol = _idleGradientLut[lutIdx];
            
            // Apply intensity (softer dark areas)
            double bright = IdleIntensity * (0.2 + 0.8 * val);
            
            byte rIdle = (byte)Math.Clamp((int)(idleCol.R * bright), 0, 255);
            byte gIdle = (byte)Math.Clamp((int)(idleCol.G * bright), 0, 255);
            byte bIdle = (byte)Math.Clamp((int)(idleCol.B * bright), 0, 255);
            
            var c = colors[i];
            colors[i] = (
                (byte)(c.R * (1 - _idleFade) + rIdle * _idleFade),
                (byte)(c.G * (1 - _idleFade) + gIdle * _idleFade),
                (byte)(c.B * (1 - _idleFade) + bIdle * _idleFade)
            );
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
