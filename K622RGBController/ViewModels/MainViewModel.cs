using K622RGBController.Helpers;
using K622RGBController.Models;
using K622RGBController.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;

namespace K622RGBController.ViewModels;

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly HidController _hid;
    private readonly AudioAnalyzer _audio;
    private readonly NotificationListener _notif;
    private readonly EffectEngine _engine;
    private readonly ProfileManager _profileManager;
    private readonly AppSettings _settings;
    private readonly DispatcherTimer _uiTimer;

    private bool _isRunning;
    private string _statusText = "Desconectado";
    private string _fpsText = "-- FPS";
    private double _brightness;
    private double _sensitivity;
    private double _smoothing;
    private double _barDecay;
    private double _fluidity;
    private double _angle;
    private double _bassLevel;
    private double _midsLevel;
    private double _trebleLevel;
    private bool _notifEnabled;
    private double _notifDuration;
    private double _notifIntensity;
    private bool _startWithWindows;
    private string _deviceName = "No conectado";

    // ── Localization ──
    public LocalizationManager Loc => LocalizationManager.Instance;

    // ── Public Properties ──
    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; OnPropertyChanged(); OnPropertyChanged(nameof(ToggleButtonText)); OnPropertyChanged(nameof(IsNotRunning)); OnPropertyChanged(nameof(StatusText)); }
    }
    public bool IsNotRunning => !_isRunning;
    public string ToggleButtonText => _isRunning ? Loc["BtnStop"] : Loc["BtnStart"];
    public string StatusText 
    { 
        get 
        {
            if (!_isRunning) return Loc["StatusDisconnected"];
            if (_statusText.Contains("Error")) return Loc["StatusConnError"];
            return Loc["StatusConnected"];
        }
        set { _statusText = value; OnPropertyChanged(); } 
    }
    public string FpsText { get => _fpsText; set { _fpsText = value; OnPropertyChanged(); } }
    public string DeviceName 
    { 
        get => _deviceName == "No conectado" || _deviceName == "Not connected" ? Loc["DeviceNone"] : _deviceName; 
        set { _deviceName = value; OnPropertyChanged(); } 
    }

    public double Brightness
    {
        get => _brightness;
        set { _brightness = value; OnPropertyChanged(); OnPropertyChanged(nameof(BrightnessText)); _settings.Brightness = value; _engine.Brightness = value; SaveSettings(); }
    }
    public string BrightnessText => $"{(int)(_brightness * 100)}%";

    public double Sensitivity
    {
        get => _sensitivity;
        set { _sensitivity = value; OnPropertyChanged(); OnPropertyChanged(nameof(SensitivityText)); _settings.Sensitivity = value; _audio.Sensitivity = value; SaveSettings(); }
    }
    public string SensitivityText => $"{_sensitivity:F1}x";

    public double Smoothing
    {
        get => _smoothing;
        set { _smoothing = value; OnPropertyChanged(); OnPropertyChanged(nameof(SmoothingText)); _settings.Smoothing = value; _audio.Smoothing = value; SaveSettings(); }
    }
    public string SmoothingText => $"{_smoothing:F2}";

    public double BarDecay
    {
        get => _barDecay;
        set { _barDecay = value; OnPropertyChanged(); OnPropertyChanged(nameof(BarDecayText)); _settings.BarDecay = value; _audio.BarDecay = value; SaveSettings(); }
    }
    public string BarDecayText => $"{_barDecay:F2}";

    public double Fluidity
    {
        get => _fluidity;
        set { _fluidity = value; OnPropertyChanged(); OnPropertyChanged(nameof(FluidityText)); _settings.Fluidity = value; _engine.Fluidity = value; SaveSettings(); }
    }
    public string FluidityText => $"{(int)(_fluidity * 100)}%";

    public double Angle
    {
        get => _angle;
        set { _angle = value; OnPropertyChanged(); OnPropertyChanged(nameof(AngleText)); _settings.Angle = value; _engine.Angle = value; SaveSettings(); }
    }
    public string AngleText => $"{(int)_angle}°";

    public double BassLevel { get => _bassLevel; set { _bassLevel = value; OnPropertyChanged(); } }
    public double MidsLevel { get => _midsLevel; set { _midsLevel = value; OnPropertyChanged(); } }
    public double TrebleLevel { get => _trebleLevel; set { _trebleLevel = value; OnPropertyChanged(); } }

    public bool NotifEnabled
    {
        get => _notifEnabled;
        set { _notifEnabled = value; OnPropertyChanged(); _settings.NotifEnabled = value; _notif.Enabled = value; SaveSettings(); }
    }
    public double NotifDuration
    {
        get => _notifDuration;
        set { _notifDuration = value; OnPropertyChanged(); OnPropertyChanged(nameof(NotifDurationText)); _settings.NotifDuration = value; _notif.FlashDuration = value; SaveSettings(); }
    }
    public string NotifDurationText => $"{_notifDuration:F1}s";

    public double NotifIntensity
    {
        get => _notifIntensity;
        set { _notifIntensity = value; OnPropertyChanged(); OnPropertyChanged(nameof(NotifIntensityText)); _settings.NotifIntensity = value; _notif.FlashIntensity = value; SaveSettings(); }
    }
    public string NotifIntensityText => $"{(int)(_notifIntensity * 100)}%";

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { _startWithWindows = value; OnPropertyChanged(); _settings.StartWithWindows = value; StartupManager.SetStartup(value); SaveSettings(); }
    }

    // ── Idle Mode Properties ──
    public bool IdleEnabled
    {
        get => _settings.IdleEnabled;
        set { _settings.IdleEnabled = value; OnPropertyChanged(); _engine.IdleEnabled = value; SaveSettings(); }
    }

    public double IdleIntensity
    {
        get => _settings.IdleIntensity;
        set { _settings.IdleIntensity = value; OnPropertyChanged(); _engine.IdleIntensity = value; SaveSettings(); }
    }

    public double IdleSpeed
    {
        get => _settings.IdleSpeed;
        set { _settings.IdleSpeed = value; OnPropertyChanged(); _engine.IdleSpeed = value; SaveSettings(); }
    }

    // ── UI Color Binding Brushes ──
    public SolidColorBrush[] GradientBrushes { get; } = new SolidColorBrush[4];
    public SolidColorBrush[] IdleBrushes { get; } = new SolidColorBrush[4];

    // ── Commands ──
    public RelayCommand ToggleCommand { get; }
    public RelayCommand ToggleLanguageCommand { get; }
    public RelayCommand TestNotifCommand { get; }
    public RelayCommand QuitCommand { get; }
    
    // Commands implementation inline due to parameter needs
    public RelayCommand<string> EditGradientColorCommand { get; }
    public RelayCommand<string> EditIdleColorCommand { get; }

    // ── Keyboard preview colors event ──
    public event Action<(byte R, byte G, byte B)[]>? KeyboardColorsUpdated;

    public MainViewModel()
    {
        _settings = AppSettings.Load();
        Loc.CurrentLanguage = _settings.Language;

        _hid = new HidController();
        _audio = new AudioAnalyzer();
        _notif = new NotificationListener();
        _engine = new EffectEngine(_hid, _audio, _notif);

        _profileManager = new ProfileManager();

        // Load settings
        _brightness = _settings.Brightness;
        _sensitivity = _settings.Sensitivity;
        _smoothing = _settings.Smoothing;
        _barDecay = _settings.BarDecay;
        _fluidity = _settings.Fluidity;
        _angle = _settings.Angle;
        _notifEnabled = _settings.NotifEnabled;
        _notifDuration = _settings.NotifDuration;
        _notifIntensity = _settings.NotifIntensity;
        _startWithWindows = StartupManager.IsStartupEnabled();

        // Apply to services
        _audio.Sensitivity = _sensitivity;
        _audio.Smoothing = _smoothing;
        _audio.BarDecay = _barDecay;
        _engine.Brightness = _brightness;
        _engine.Angle = _angle;
        _engine.Fluidity = _fluidity;
        _engine.GradientColors = _settings.GradientColors;
        _notif.Enabled = _notifEnabled;
        _notif.FlashDuration = _notifDuration;
        _notif.FlashIntensity = _notifIntensity;
        _notif.FlashColor = ((byte)_settings.NotifColor[0], (byte)_settings.NotifColor[1], (byte)_settings.NotifColor[2]);

        // Forward frame events
        _engine.FrameRendered += colors => KeyboardColorsUpdated?.Invoke(colors);

        // Commands
        ToggleCommand = new RelayCommand(ToggleRunning);
        ToggleLanguageCommand = new RelayCommand(ToggleLanguage);
        TestNotifCommand = new RelayCommand(() => { if (_isRunning) _notif.TriggerNotification(); });
        QuitCommand = new RelayCommand(() => System.Windows.Application.Current?.Shutdown());
        
        EditGradientColorCommand = new RelayCommand<string>(EditGradientColor);
        EditIdleColorCommand = new RelayCommand<string>(EditIdleColor);

        // UI update timer (~15 FPS for meters)
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(66) };
        _uiTimer.Tick += UiTimerTick;
        
        UpdateColorBrushes();
    }

    private void ToggleLanguage()
    {
        _settings.Language = _settings.Language == AppLanguage.English ? AppLanguage.Spanish : AppLanguage.English;
        Loc.CurrentLanguage = _settings.Language;
        OnPropertyChanged(nameof(ToggleButtonText));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(DeviceName));
        SaveSettings();
    }
    
    private void UpdateColorBrushes()
    {
        for (int i = 0; i < 4; i++)
        {
            var gc = _settings.GradientColors[i];
            GradientBrushes[i] = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)gc[0], (byte)gc[1], (byte)gc[2]));
            
            var ic = _settings.IdleColors[i];
            IdleBrushes[i] = new SolidColorBrush(System.Windows.Media.Color.FromRgb((byte)ic[0], (byte)ic[1], (byte)ic[2]));
        }
        OnPropertyChanged(nameof(GradientBrushes));
        OnPropertyChanged(nameof(IdleBrushes));
    }

    private void EditGradientColor(string? indexStr)
    {
        if (int.TryParse(indexStr, out int index) && index >= 0 && index < 4)
        {
            var current = _settings.GradientColors[index];
            using var d = new System.Windows.Forms.ColorDialog();
            d.Color = System.Drawing.Color.FromArgb(255, current[0], current[1], current[2]);
            d.FullOpen = true;
            
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _settings.GradientColors[index] = new int[] { d.Color.R, d.Color.G, d.Color.B };
                _engine.GradientColors = _settings.GradientColors;
                _engine.UpdateGradient();
                UpdateColorBrushes();
                SaveSettings();
            }
        }
    }

    private void EditIdleColor(string? indexStr)
    {
        if (int.TryParse(indexStr, out int index) && index >= 0 && index < 4)
        {
            var current = _settings.IdleColors[index];
            using var d = new System.Windows.Forms.ColorDialog();
            d.Color = System.Drawing.Color.FromArgb(255, current[0], current[1], current[2]);
            d.FullOpen = true;
            
            if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _settings.IdleColors[index] = new int[] { d.Color.R, d.Color.G, d.Color.B };
                _engine.IdleColors = _settings.IdleColors;
                _engine.UpdateGradient();
                UpdateColorBrushes();
                SaveSettings();
            }
        }
    }

    public void AutoStart()
    {
        if (_settings.AutoConnect)
        {
            ToggleRunning();
        }
    }

    private void ToggleRunning()
    {
        if (_isRunning)
            StopAll();
        else
            StartAll();
    }

    private void StartAll()
    {
        if (!_hid.Connect())
        {
            StatusText = "⚠ Error de conexión";
            return;
        }

        DeviceName = _hid.DeviceName;
        StatusText = "● Conectado";

        _audio.Start();
        _notif.Start();
        _engine.Start();

        IsRunning = true;
        _uiTimer.Start();
    }

    private void StopAll()
    {
        _uiTimer.Stop();
        _engine.Stop();
        _notif.Stop();
        _audio.Stop();
        _hid.Disconnect();

        IsRunning = false;
        StatusText = "Desconectado";
        DeviceName = "No conectado";
        FpsText = "-- FPS";
        BassLevel = 0;
        MidsLevel = 0;
        TrebleLevel = 0;
    }

    private void UiTimerTick(object? sender, EventArgs e)
    {
        if (!_isRunning) return;

        var (bass, mids, treble, _) = _audio.GetLevels();
        BassLevel = bass;
        MidsLevel = mids;
        TrebleLevel = treble;

        FpsText = $"{_engine.ActualFps:F0} FPS";
    }

    private void SaveSettings()
    {
        _settings.Save();
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void Dispose()
    {
        _uiTimer.Stop();
        _engine.Dispose();
        _notif.Dispose();
        _audio.Dispose();
        _hid.Dispose();
        _settings.Save();
        GC.SuppressFinalize(this);
    }
}
