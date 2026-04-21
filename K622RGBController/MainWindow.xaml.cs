using K622RGBController.Models;
using K622RGBController.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace K622RGBController;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Dictionary<string, System.Windows.Shapes.Rectangle> _keyRects = new();
    private readonly Dictionary<string, System.Windows.Controls.TextBlock> _keyLabels = new();

    private const double KeyUnit = 36;
    private const double KeyPad = 3;
    private const double CornerRadius = 4;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DrawKeyboard();
        _viewModel.KeyboardColorsUpdated += OnKeyboardColorsUpdated;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Don't actually close — hide to tray instead
        e.Cancel = true;
        Hide();
    }

    private void DrawKeyboard()
    {
        KeyboardCanvas.Children.Clear();
        _keyRects.Clear();
        _keyLabels.Clear();

        double ox = 8, oy = 8;

        foreach (var (keyName, (kx, ky, kw, kh)) in KeyboardLayout.VisualLayout)
        {
            double x1 = ox + kx * KeyUnit + KeyPad;
            double y1 = oy + ky * KeyUnit + KeyPad;
            double w = kw * KeyUnit - KeyPad * 2;
            double h = kh * KeyUnit - KeyPad * 2;

            // Key background
            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = w,
                Height = h,
                RadiusX = CornerRadius,
                RadiusY = CornerRadius,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 35, 52)),
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 50, 72)),
                StrokeThickness = 1,
            };
            Canvas.SetLeft(rect, x1);
            Canvas.SetTop(rect, y1);
            KeyboardCanvas.Children.Add(rect);
            _keyRects[keyName] = rect;

            // Key label
            string display = KeyboardLayout.DisplayLabels.TryGetValue(keyName, out var shortLabel) 
                ? shortLabel 
                : keyName;

            var label = new System.Windows.Controls.TextBlock
            {
                Text = display,
                FontSize = display.Length > 3 ? 8 : 9,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(110, 110, 140)),
                TextAlignment = TextAlignment.Center,
            };

            // Measure text to center it
            label.Measure(new System.Windows.Size(w, h));
            double tx = x1 + (w - label.DesiredSize.Width) / 2;
            double ty = y1 + (h - label.DesiredSize.Height) / 2;

            Canvas.SetLeft(label, tx);
            Canvas.SetTop(label, ty);
            KeyboardCanvas.Children.Add(label);
            _keyLabels[keyName] = label;
        }
    }

    private void OnKeyboardColorsUpdated((byte R, byte G, byte B)[] colors)
    {
        // Marshal to UI thread
        Dispatcher.BeginInvoke(() =>
        {
            var keyNames = KeyboardLayout.KeyNames;
            for (int i = 0; i < Math.Min(colors.Length, keyNames.Count); i++)
            {
                string name = keyNames[i];
                if (_keyRects.TryGetValue(name, out var rect))
                {
                    var (r, g, b) = colors[i];
                    rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));

                    // Adjust text color for readability
                    if (_keyLabels.TryGetValue(name, out var label))
                    {
                        double luminance = 0.299 * r + 0.587 * g + 0.114 * b;
                        label.Foreground = luminance > 140
                            ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 17, 34))
                            : new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 180, 200));
                    }
                }
            }
        });
    }
}
