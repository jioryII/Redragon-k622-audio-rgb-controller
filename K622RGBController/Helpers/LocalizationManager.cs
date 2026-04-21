using System.ComponentModel;
using System.Runtime.CompilerServices;
using K622RGBController.Models;

namespace K622RGBController.Helpers;

public class LocalizationManager : INotifyPropertyChanged
{
    public static LocalizationManager Instance { get; } = new();

    private AppLanguage _currentLanguage = AppLanguage.English;

    public AppLanguage CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                OnPropertyChanged(string.Empty); // Update all bindings
            }
        }
    }

    public string this[string key] => GetString(key);

    private string GetString(string key)
    {
        return _currentLanguage switch
        {
            AppLanguage.Spanish => GetSpanish(key),
            _ => GetEnglish(key)
        };
    }

    private string GetSpanish(string key) => key switch
    {
        "AppTitle" => "K622 RGB Controller",
        "AppSubtitle" => "  Controlador",
        "StatusDisconnected" => "Desconectado",
        "StatusConnected" => "● Conectado",
        "StatusConnError" => "⚠ Error de conexión",
        "DeviceNone" => "No conectado",
        "BtnStart" => "▶ Iniciar",
        "BtnStop" => "⏹ Detener",
        "TabMusic" => "🎵 Música",
        "TabNotif" => "🔔 Notif.",
        "TabSettings" => "⚙ Ajustes",
        "HeaderMusic" => "Visualizador de Espectro",
        "SubMusic" => "Frecuencias del audio mapeadas al teclado con dirección configurable",
        "FieldDirection" => "Dirección del espectro",
        "HeaderAudioAdjust" => "Ajustes de audio",
        "FieldSensitivity" => "Sensibilidad",
        "FieldSmoothing" => "Suavizado",
        "FieldBarDecay" => "Caída barras",
        "FieldFluidity" => "Fluidez",
        "HeaderSpectrumColors" => "Colores del Espectro",
        "HeaderIdle" => "Modo Reposo (Idle)",
        "FieldIdleEnable" => "  Habiliar manchas relajantes sin audio",
        "FieldIntensity" => "Intensidad",
        "FieldSpeed" => "Velocidad",
        "HeaderIdleColors" => "Colores del Reposo",
        "HeaderRealTimeLevels" => "Niveles en tiempo real",
        "HeaderNotif" => "Notificaciones del Sistema",
        "SubNotif" => "Parpadeo en el teclado al recibir notificaciones de Windows",
        "FieldNotifEnable" => "  Habilitar parpadeo en notificaciones",
        "FieldDuration" => "Duración",
        "BtnTestNotif" => "⚡ Probar Parpadeo",
        "NotifDesc" => "Cuando Windows muestra una notificación, el teclado realizará un parpadeo suave con una onda diagonal. Tiene prioridad sobre el visualizador de música.",
        "HeaderGeneral" => "Configuración General",
        "SubGeneral" => "Comportamiento de la aplicación",
        "FieldStartup" => "  Iniciar con Windows (en segundo plano)",
        "StartupDesc" => "La aplicación se iniciará automáticamente con Windows y se mantendrá en el área de iconos ocultos de la barra de tareas.",
        "FieldDevice" => "Dispositivo",
        "HeaderAbout" => "Acerca de",
        "FieldBrightness" => "☀ Brillo:",
        "PreviewTitle" => "Vista previa del teclado",
        "EditColor" => "Editar Color",
        "LangToggle" => "EN / ES",
        _ => key
    };

    private string GetEnglish(string key) => key switch
    {
        "AppTitle" => "K622 RGB Controller",
        "AppSubtitle" => "  Controller",
        "StatusDisconnected" => "Disconnected",
        "StatusConnected" => "● Connected",
        "StatusConnError" => "⚠ Connection Error",
        "DeviceNone" => "Not connected",
        "BtnStart" => "▶ Start",
        "BtnStop" => "⏹ Stop",
        "TabMusic" => "🎵 Music",
        "TabNotif" => "🔔 Notif.",
        "TabSettings" => "⚙ Settings",
        "HeaderMusic" => "Spectrum Visualizer",
        "SubMusic" => "Audio frequencies mapped to the keyboard with configurable direction",
        "FieldDirection" => "Spectrum direction",
        "HeaderAudioAdjust" => "Audio adjustments",
        "FieldSensitivity" => "Sensitivity",
        "FieldSmoothing" => "Smoothing",
        "FieldBarDecay" => "Bar decay",
        "FieldFluidity" => "Fluidity",
        "HeaderSpectrumColors" => "Spectrum Colors",
        "HeaderIdle" => "Idle Mode",
        "FieldIdleEnable" => "  Enable relaxing spots without audio",
        "FieldIntensity" => "Intensity",
        "FieldSpeed" => "Speed",
        "HeaderIdleColors" => "Idle Colors",
        "HeaderRealTimeLevels" => "Real-time levels",
        "HeaderNotif" => "System Notifications",
        "SubNotif" => "Flash keyboard when receiving Windows notifications",
        "FieldNotifEnable" => "  Enable notification flashing",
        "FieldDuration" => "Duration",
        "BtnTestNotif" => "⚡ Test Flash",
        "NotifDesc" => "When Windows shows a notification, the keyboard will perform a soft diagonal wave flash. It has priority over the music visualizer.",
        "HeaderGeneral" => "General Configuration",
        "SubGeneral" => "Application behavior",
        "FieldStartup" => "  Start with Windows (in background)",
        "StartupDesc" => "The application will start automatically with Windows and stay in the system tray.",
        "FieldDevice" => "Device",
        "HeaderAbout" => "About",
        "FieldBrightness" => "☀ Brightness:",
        "PreviewTitle" => "Keyboard preview",
        "EditColor" => "Edit Color",
        "LangToggle" => "ES / EN",
        _ => key
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
