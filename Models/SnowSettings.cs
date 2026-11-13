using System.IO;
using System.Text.Json;

namespace SnowDesktop.Models;

public enum SnowPreset
{
    Custom,
    QuietNight,
    CozyEvening,
    Blizzard,
    WinterPlayground
}

public sealed class SnowSettings
{
    public int SnowflakeCount { get; set; } = 320;
    public double FallSpeed { get; set; } = 1.0;
    public double WindStrength { get; set; } = 0.25;
    public double BlizzardIntensity { get; set; } = 0.8;
    public double MaxDriftHeight { get; set; } = 28.0;
    public double AccumulationRate { get; set; } = 1.5;
    public double MeltRate { get; set; } = 0.0;
    public bool AccumulateOnWindows { get; set; } = true;
    public bool AccumulateOnTaskbar { get; set; } = true;
    public bool MouseInteraction { get; set; } = true;
    public bool SnowplowEnabled { get; set; } = true;
    public bool TrainSantaWagon { get; set; } = true;
    public bool TrainTreeWagon { get; set; } = true;
    public double SnowplowIntervalSeconds { get; set; } = 60.0;
    public bool SnowmanAnimations { get; set; } = true;
    public double SnowmanScale { get; set; } = 1.0;
    public bool CursorFrostTrail { get; set; } = true;
    public bool GarlandEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
    public double SoundVolume { get; set; } = 0.55;
    public string SnowballModifier { get; set; } = "Alt";
    public bool IsPaused { get; set; } = false;

    public event Action? Changed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string SettingsFilePath
    {
        get
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Snezhok", "settings.json");
        }
    }

    public static SnowSettings Load()
    {
        SnowSettings? loaded = null;
        try
        {
            string path = SettingsFilePath;
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                loaded = JsonSerializer.Deserialize<SnowSettings>(json, JsonOptions);
            }
            else
            {
                // One-time migration from legacy SnowDesktop settings folder if present.
                string legacyPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "SnowDesktop",
                    "settings.json");
                if (File.Exists(legacyPath))
                {
                    string json = File.ReadAllText(legacyPath);
                    loaded = JsonSerializer.Deserialize<SnowSettings>(json, JsonOptions);
                }
            }
        }
        catch
        {
            // Ignore malformed settings and fall back to defaults.
        }

        SnowSettings settings = loaded ?? new SnowSettings();
        settings.Clamp();
        // Always ensure %APPDATA%\Snezhok\settings.json exists on startup for portability and manual editing.
        settings.Save();
        return settings;
    }

    public void Save()
    {
        try
        {
            Clamp();
            string path = SettingsFilePath;
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json);
            Changed?.Invoke();
        }
        catch
        {
            // Non-fatal if settings file cannot be written.
        }
    }

    public static string GetPresetDisplayName(SnowPreset preset) => preset switch
    {
        SnowPreset.QuietNight => "Тихая ночь",
        SnowPreset.CozyEvening => "Снежный вечер",
        SnowPreset.Blizzard => "Метель",
        SnowPreset.WinterPlayground => "Зимний двор",
        _ => "Пользовательский"
    };

    public void ApplyPreset(SnowPreset preset)
    {
        switch (preset)
        {
            case SnowPreset.QuietNight:
                SnowflakeCount = 180;
                FallSpeed = 0.75;
                WindStrength = 0.15;
                BlizzardIntensity = 0.25;
                MaxDriftHeight = 14.0;
                AccumulationRate = 0.6;
                MeltRate = 0.25;
                AccumulateOnWindows = true;
                AccumulateOnTaskbar = true;
                MouseInteraction = true;
                SnowplowEnabled = false;
                SnowplowIntervalSeconds = 90.0;
                SnowmanAnimations = true;
                CursorFrostTrail = false;
                GarlandEnabled = false;
                break;

            case SnowPreset.CozyEvening:
                SnowflakeCount = 320;
                FallSpeed = 1.0;
                WindStrength = 0.25;
                BlizzardIntensity = 0.8;
                MaxDriftHeight = 28.0;
                AccumulationRate = 1.5;
                MeltRate = 0.0;
                AccumulateOnWindows = true;
                AccumulateOnTaskbar = true;
                MouseInteraction = true;
                SnowplowEnabled = true;
                SnowplowIntervalSeconds = 60.0;
                SnowmanAnimations = true;
                CursorFrostTrail = true;
                GarlandEnabled = true;
                break;

            case SnowPreset.Blizzard:
                SnowflakeCount = 650;
                FallSpeed = 1.55;
                WindStrength = 0.95;
                BlizzardIntensity = 1.75;
                MaxDriftHeight = 48.0;
                AccumulationRate = 2.6;
                MeltRate = 0.0;
                AccumulateOnWindows = true;
                AccumulateOnTaskbar = true;
                MouseInteraction = true;
                SnowplowEnabled = true;
                SnowplowIntervalSeconds = 40.0;
                SnowmanAnimations = true;
                CursorFrostTrail = true;
                GarlandEnabled = true;
                break;

            case SnowPreset.WinterPlayground:
                SnowflakeCount = 360;
                FallSpeed = 1.05;
                WindStrength = 0.20;
                BlizzardIntensity = 0.60;
                MaxDriftHeight = 32.0;
                AccumulationRate = 1.9;
                MeltRate = 0.05;
                AccumulateOnWindows = true;
                AccumulateOnTaskbar = true;
                MouseInteraction = true;
                SnowplowEnabled = true;
                SnowplowIntervalSeconds = 45.0;
                SnowmanAnimations = true;
                CursorFrostTrail = true;
                GarlandEnabled = true;
                break;
        }
    }

    public SnowPreset DetectActivePreset()
    {
        if (MatchesPreset(SnowPreset.QuietNight)) return SnowPreset.QuietNight;
        if (MatchesPreset(SnowPreset.CozyEvening)) return SnowPreset.CozyEvening;
        if (MatchesPreset(SnowPreset.Blizzard)) return SnowPreset.Blizzard;
        if (MatchesPreset(SnowPreset.WinterPlayground)) return SnowPreset.WinterPlayground;
        return SnowPreset.Custom;
    }

    private bool MatchesPreset(SnowPreset preset)
    {
        var probe = new SnowSettings();
        probe.ApplyPreset(preset);
        return SnowflakeCount == probe.SnowflakeCount
            && Math.Abs(FallSpeed - probe.FallSpeed) < 0.01
            && Math.Abs(WindStrength - probe.WindStrength) < 0.01
            && Math.Abs(BlizzardIntensity - probe.BlizzardIntensity) < 0.01
            && Math.Abs(MaxDriftHeight - probe.MaxDriftHeight) < 0.1
            && Math.Abs(AccumulationRate - probe.AccumulationRate) < 0.05
            && Math.Abs(MeltRate - probe.MeltRate) < 0.01
            && AccumulateOnWindows == probe.AccumulateOnWindows
            && AccumulateOnTaskbar == probe.AccumulateOnTaskbar
            && MouseInteraction == probe.MouseInteraction
            && SnowplowEnabled == probe.SnowplowEnabled
            && Math.Abs(SnowplowIntervalSeconds - probe.SnowplowIntervalSeconds) < 0.5
            && SnowmanAnimations == probe.SnowmanAnimations
            && CursorFrostTrail == probe.CursorFrostTrail
            && GarlandEnabled == probe.GarlandEnabled;
    }

    public void ResetToDefaults()
    {
        SnowSettings defaults = new();
        SnowflakeCount = defaults.SnowflakeCount;
        FallSpeed = defaults.FallSpeed;
        WindStrength = defaults.WindStrength;
        BlizzardIntensity = defaults.BlizzardIntensity;
        MaxDriftHeight = defaults.MaxDriftHeight;
        AccumulationRate = defaults.AccumulationRate;
        MeltRate = defaults.MeltRate;
        AccumulateOnWindows = defaults.AccumulateOnWindows;
        AccumulateOnTaskbar = defaults.AccumulateOnTaskbar;
        MouseInteraction = defaults.MouseInteraction;
        SnowplowEnabled = defaults.SnowplowEnabled;
        TrainSantaWagon = defaults.TrainSantaWagon;
        TrainTreeWagon = defaults.TrainTreeWagon;
        SnowplowIntervalSeconds = defaults.SnowplowIntervalSeconds;
        SnowmanAnimations = defaults.SnowmanAnimations;
        SnowmanScale = defaults.SnowmanScale;
        CursorFrostTrail = defaults.CursorFrostTrail;
        GarlandEnabled = defaults.GarlandEnabled;
        SoundEnabled = defaults.SoundEnabled;
        SoundVolume = defaults.SoundVolume;
        SnowballModifier = defaults.SnowballModifier;
    }

    public void Clamp()
    {
        SnowflakeCount = Math.Clamp(SnowflakeCount, 20, 800);
        FallSpeed = Math.Clamp(FallSpeed, 0.25, 3.0);
        WindStrength = Math.Clamp(WindStrength, -2.0, 2.0);
        BlizzardIntensity = Math.Clamp(BlizzardIntensity, 0.0, 2.0);
        MaxDriftHeight = Math.Clamp(MaxDriftHeight, 4.0, 65.0);
        AccumulationRate = Math.Clamp(AccumulationRate, 0.2, 4.0);
        MeltRate = Math.Clamp(MeltRate, 0.0, 1.5);
        SnowplowIntervalSeconds = Math.Clamp(SnowplowIntervalSeconds, 15.0, 300.0);
        SnowmanScale = Math.Clamp(SnowmanScale, 0.6, 2.0);
        SoundVolume = Math.Clamp(SoundVolume, 0.0, 1.0);
        if (SnowballModifier is not ("Alt" or "Shift" or "Ctrl"))
            SnowballModifier = "Alt";
    }
}
