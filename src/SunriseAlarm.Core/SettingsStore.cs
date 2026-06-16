using System.Text.Json;

namespace SunriseAlarm.Core;

/// <summary>
/// Loads and saves <see cref="Settings"/> as JSON at a fixed path. A missing or corrupt file
/// falls back to defaults rather than throwing, so a bad config can never brick the alarm.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _path;

    public SettingsStore(string path) =>
        _path = path ?? throw new ArgumentNullException(nameof(path));

    /// <summary>The conventional per-user location for the settings file.</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SunriseAlarm", "settings.json");

    public Settings Load()
    {
        if (!File.Exists(_path)) return new Settings();
        try
        {
            string json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Settings>(json, Options) ?? new Settings();
        }
        catch (JsonException)
        {
            return new Settings();
        }
    }

    public void Save(Settings settings)
    {
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(_path, JsonSerializer.Serialize(settings, Options));
    }
}
