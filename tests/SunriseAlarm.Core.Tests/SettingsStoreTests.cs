using SunriseAlarm.Core;
using Xunit;

namespace SunriseAlarm.Core.Tests;

public class SettingsStoreTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"sunrise-test-{Guid.NewGuid():N}.json");

    [Fact]
    public void Save_then_load_round_trips_all_fields()
    {
        var store = new SettingsStore(_path);
        var settings = new Settings
        {
            WakeTime = new TimeOnly(6, 45),
            RampMinutes = 20,
            GradualBrightness = false,
            GradualLoudness = true,
            MaxBrightness = 0.7,
            MaxVolume = 0.4,
            Track = "ocean",
            SnoozeMinutes = 5,
            LaunchOnStartup = true,
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings, loaded);
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_missing()
    {
        var store = new SettingsStore(_path);
        Assert.Equal(new Settings(), store.Load());
    }

    [Fact]
    public void Load_returns_defaults_when_file_is_corrupt()
    {
        File.WriteAllText(_path, "{ this is not valid json");
        var store = new SettingsStore(_path);

        Assert.Equal(new Settings(), store.Load());
    }

    [Fact]
    public void Save_creates_missing_directories()
    {
        var nested = Path.Combine(
            Path.GetTempPath(), $"sunrise-test-{Guid.NewGuid():N}", "deep", "settings.json");
        var store = new SettingsStore(nested);

        store.Save(new Settings { Track = "rain" });

        Assert.True(File.Exists(nested));
        Assert.Equal("rain", store.Load().Track);

        Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(nested))!, recursive: true);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
