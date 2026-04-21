using System.Text.Json;
using System.Diagnostics;
using System.IO;

namespace K622RGBController.Services;

/// <summary>
/// Manages saving/loading of keyboard lighting profiles as JSON files.
/// Compatible with profiles saved by the Python version.
/// </summary>
public class ProfileManager
{
    private readonly string _profileDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public ProfileManager()
    {
        _profileDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "K622_RGB", "profiles");
        Directory.CreateDirectory(_profileDir);
    }

    public List<string> ListProfiles()
    {
        try
        {
            return Directory.GetFiles(_profileDir, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => n != null)
                .Cast<string>()
                .OrderBy(n => n)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error listing profiles: {ex.Message}");
            return new List<string>();
        }
    }

    public bool SaveProfile(string name, Dictionary<string, object> data)
    {
        string safeName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').ToArray()).Trim();
        if (string.IsNullOrEmpty(safeName)) return false;

        data["name"] = name;
        data["saved_at"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string path = Path.Combine(_profileDir, $"{safeName}.json");
        try
        {
            string json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(path, json);
            Debug.WriteLine($"Profile saved: {name}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving profile '{name}': {ex.Message}");
            return false;
        }
    }

    public Dictionary<string, JsonElement>? LoadProfile(string name)
    {
        string safeName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').ToArray()).Trim();
        string path = Path.Combine(_profileDir, $"{safeName}.json");

        if (!File.Exists(path)) return null;

        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading profile '{name}': {ex.Message}");
            return null;
        }
    }

    public bool DeleteProfile(string name)
    {
        string safeName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '_').ToArray()).Trim();
        string path = Path.Combine(_profileDir, $"{safeName}.json");

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error deleting profile '{name}': {ex.Message}");
            return false;
        }
    }
}
