using Hardcodet.Wpf.TaskbarNotification;
using K622RGBController.ViewModels;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace K622RGBController;

public partial class App : System.Windows.Application
{
    private static Mutex? _mutex;
    private TaskbarIcon? _trayIcon;
    private MainWindow? _mainWindow;
    private MainViewModel? _viewModel;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // ── Single instance enforcement ──
        const string mutexName = "K622RGBController_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);
        if (!createdNew)
        {
            System.Windows.MessageBox.Show("K622 RGB Controller ya está en ejecución.",
                "K622 RGB", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            System.Windows.Application.Current.Shutdown();
            return;
        }

        // ── Create ViewModel ──
        _viewModel = new MainViewModel();

        // ── Create system tray icon ──
        SetupTrayIcon();

        // ── Determine if we should start minimized ──
        bool startMinimized = e.Args.Contains("--minimized");

        // ── Create main window ──
        _mainWindow = new MainWindow(_viewModel);

        if (!startMinimized)
        {
            _mainWindow.Show();
        }

        // ── Auto-start the engine ──
        _viewModel.AutoStart();
    }

    private void SetupTrayIcon()
    {
        var contextMenu = new ContextMenu
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 22, 32)),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(224, 224, 232)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(42, 42, 58)),
            BorderThickness = new Thickness(1),
        };

        var openItem = new MenuItem { Header = "Abrir Interfaz", FontWeight = FontWeights.SemiBold };
        openItem.Click += (_, _) => ShowMainWindow();

        var toggleItem = new MenuItem { Header = "▶ Iniciar / ⏹ Detener" };
        toggleItem.Click += (_, _) => _viewModel?.ToggleCommand.Execute(null);

        var separator = new Separator();

        var quitItem = new MenuItem { Header = "✕ Salir", Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 74, 94)) };
        quitItem.Click += (_, _) => QuitApp();

        contextMenu.Items.Add(openItem);
        contextMenu.Items.Add(toggleItem);
        contextMenu.Items.Add(separator);
        contextMenu.Items.Add(quitItem);

        _trayIcon = new TaskbarIcon
        {
            Icon = CreateTrayIcon(),
            ToolTipText = "K622 RGB Controller",
            ContextMenu = contextMenu,
            DoubleClickCommand = new Helpers.RelayCommand(() => ShowMainWindow()),
        };
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_viewModel!);
        }

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    private void QuitApp()
    {
        _viewModel?.Dispose();
        _trayIcon?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    private static System.Drawing.Icon CreateTrayIcon()
    {
        // Create a simple 32x32 icon programmatically
        using var bmp = new System.Drawing.Bitmap(32, 32);
        using var g = System.Drawing.Graphics.FromImage(bmp);

        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.FromArgb(20, 20, 30));

        // Draw keyboard outline
        using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(0, 208, 132), 2);
        g.DrawRectangle(pen, 4, 10, 24, 12);

        // Draw LED dots
        using var greenBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 208, 132));
        using var orangeBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 140, 66));
        using var redBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(255, 0, 96));

        g.FillRectangle(greenBrush, 7, 13, 5, 3);
        g.FillRectangle(orangeBrush, 14, 13, 5, 3);
        g.FillRectangle(redBrush, 21, 13, 5, 3);
        g.FillRectangle(greenBrush, 7, 18, 8, 2);

        // Convert to icon
        IntPtr hIcon = bmp.GetHicon();
        return System.Drawing.Icon.FromHandle(hIcon);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        _viewModel?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
