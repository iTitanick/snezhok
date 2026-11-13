using System.Windows;
using System.Windows.Media;
using SnowDesktop.Models;
using SnowDesktop.Overlay;
using SnowDesktop.Services;

namespace SnowDesktop.UI;

public partial class SettingsWindow : Window
{
    private readonly SnowSettings _settings;
    private readonly OverlayManager _overlayManager;
    private readonly Action _onAutostartSynced;
    private bool _isLoading = true;

    public SettingsWindow(
        SnowSettings settings,
        OverlayManager overlayManager,
        Action onAutostartSynced)
    {
        _settings = settings;
        _overlayManager = overlayManager;
        _onAutostartSynced = onAutostartSynced;

        MaxHeight = Math.Max(440, SystemParameters.WorkArea.Height - 36);

        _isLoading = true;
        InitializeComponent();
        LoadValues();

        _settings.Changed += OnExternalSettingsChanged;
        Closed += (_, _) => _settings.Changed -= OnExternalSettingsChanged;
    }

    private void OnExternalSettingsChanged()
    {
        if (!IsVisible || _isLoading)
            return;

        Dispatcher.BeginInvoke(() =>
        {
            if (!_isLoading)
            {
                LoadValues();
            }
        });
    }

    private void LoadValues()
    {
        _isLoading = true;

        CountSlider.Value = _settings.SnowflakeCount;
        SpeedSlider.Value = _settings.FallSpeed;
        WindSlider.Value = _settings.WindStrength;
        BlizzardSlider.Value = _settings.BlizzardIntensity;
        MaxDriftSlider.Value = _settings.MaxDriftHeight;
        AccumRateSlider.Value = _settings.AccumulationRate;
        MeltRateSlider.Value = _settings.MeltRate;
        SnowmanScaleSlider.Value = _settings.SnowmanScale;
        SnowplowIntervalSlider.Value = _settings.SnowplowIntervalSeconds;
        SoundVolumeSlider.Value = Math.Round(_settings.SoundVolume * 100.0);

        WindowsCheck.IsChecked = _settings.AccumulateOnWindows;
        TaskbarCheck.IsChecked = _settings.AccumulateOnTaskbar;
        MouseInteractionCheck.IsChecked = _settings.MouseInteraction;
        FrostTrailCheck.IsChecked = _settings.CursorFrostTrail;
        GarlandCheck.IsChecked = _settings.GarlandEnabled;
        SnowplowCheck.IsChecked = _settings.SnowplowEnabled;
        TrainSantaWagonCheck.IsChecked = _settings.TrainSantaWagon;
        TrainTreeWagonCheck.IsChecked = _settings.TrainTreeWagon;
        SnowmanAnimCheck.IsChecked = _settings.SnowmanAnimations;
        SoundCheck.IsChecked = _settings.SoundEnabled;
        AutostartCheck.IsChecked = AutostartService.IsEnabled();

        UpdateWagonChecksEnabled();

        ModAltRadio.IsChecked = _settings.SnowballModifier == "Alt";
        ModShiftRadio.IsChecked = _settings.SnowballModifier == "Shift";
        ModCtrlRadio.IsChecked = _settings.SnowballModifier == "Ctrl";

        UpdateLabels();
        UpdatePresetHighlight();
        _isLoading = false;
    }

    private void UpdateLabels()
    {
        CountValueText.Text = $"{(int)CountSlider.Value}";
        SpeedValueText.Text = $"{SpeedSlider.Value:0.00}x";
        WindValueText.Text = $"{WindSlider.Value:+0.00;-0.00;0.00}";
        BlizzardValueText.Text = $"{BlizzardSlider.Value:0.00}x";
        MaxDriftValueText.Text = $"{(int)MaxDriftSlider.Value} px";
        AccumRateValueText.Text = $"{AccumRateSlider.Value:0.0}x";
        MeltRateValueText.Text = $"{MeltRateSlider.Value:0.00}x";
        SnowmanScaleValueText.Text = $"{SnowmanScaleSlider.Value:0.00}x";
        SnowplowIntervalValueText.Text = $"{(int)SnowplowIntervalSlider.Value} сек";
        SoundVolumeValueText.Text = $"{(int)SoundVolumeSlider.Value}%";
    }

    private void UpdatePresetHighlight()
    {
        SnowPreset active = _settings.DetectActivePreset();
        ActivePresetBadge.Text = SnowSettings.GetPresetDisplayName(active);

        StylePresetPill(PresetQuietBtn, active == SnowPreset.QuietNight);
        StylePresetPill(PresetEveningBtn, active == SnowPreset.CozyEvening);
        StylePresetPill(PresetBlizzardBtn, active == SnowPreset.Blizzard);
        StylePresetPill(PresetPlaygroundBtn, active == SnowPreset.WinterPlayground);
    }

    private static void StylePresetPill(System.Windows.Controls.Button btn, bool active)
    {
        btn.Background = new SolidColorBrush(active
            ? System.Windows.Media.Color.FromRgb(0x28, 0x51, 0x85)
            : System.Windows.Media.Color.FromRgb(0x1E, 0x33, 0x50));
        btn.BorderBrush = new SolidColorBrush(active
            ? System.Windows.Media.Color.FromRgb(0x63, 0xA8, 0xF0)
            : System.Windows.Media.Color.FromRgb(0x32, 0x53, 0x7D));
    }

    private void ApplyPreset(SnowPreset preset)
    {
        _settings.ApplyPreset(preset);
        _settings.Save();
        LoadValues();
    }

    private void OnPresetQuietClick(object sender, RoutedEventArgs e) => ApplyPreset(SnowPreset.QuietNight);
    private void OnPresetEveningClick(object sender, RoutedEventArgs e) => ApplyPreset(SnowPreset.CozyEvening);
    private void OnPresetBlizzardClick(object sender, RoutedEventArgs e) => ApplyPreset(SnowPreset.Blizzard);
    private void OnPresetPlaygroundClick(object sender, RoutedEventArgs e) => ApplyPreset(SnowPreset.WinterPlayground);

    private void SaveFromUi()
    {
        _isLoading = true;
        try
        {
            _settings.Save();
        }
        finally
        {
            _isLoading = false;
        }
        UpdatePresetHighlight();
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading)
            return;

        _settings.SnowflakeCount = (int)CountSlider.Value;
        _settings.FallSpeed = SpeedSlider.Value;
        _settings.WindStrength = WindSlider.Value;
        _settings.BlizzardIntensity = BlizzardSlider.Value;
        _settings.MaxDriftHeight = MaxDriftSlider.Value;
        _settings.AccumulationRate = AccumRateSlider.Value;
        _settings.MeltRate = MeltRateSlider.Value;
        _settings.SnowmanScale = SnowmanScaleSlider.Value;
        _settings.SnowplowIntervalSeconds = SnowplowIntervalSlider.Value;
        _settings.SoundVolume = SoundVolumeSlider.Value / 100.0;

        UpdateLabels();
        SaveFromUi();
    }

    private void OnCheckChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        _settings.AccumulateOnWindows = WindowsCheck.IsChecked == true;
        _settings.AccumulateOnTaskbar = TaskbarCheck.IsChecked == true;
        _settings.MouseInteraction = MouseInteractionCheck.IsChecked == true;
        _settings.CursorFrostTrail = FrostTrailCheck.IsChecked == true;
        _settings.GarlandEnabled = GarlandCheck.IsChecked == true;
        _settings.SnowplowEnabled = SnowplowCheck.IsChecked == true;
        _settings.TrainSantaWagon = TrainSantaWagonCheck.IsChecked == true;
        _settings.TrainTreeWagon = TrainTreeWagonCheck.IsChecked == true;
        _settings.SnowmanAnimations = SnowmanAnimCheck.IsChecked == true;
        _settings.SoundEnabled = SoundCheck.IsChecked == true;

        UpdateWagonChecksEnabled();
        SaveFromUi();
    }

    private void UpdateWagonChecksEnabled()
    {
        bool trainOn = SnowplowCheck.IsChecked == true;
        TrainSantaWagonCheck.IsEnabled = trainOn;
        TrainTreeWagonCheck.IsEnabled = trainOn;
    }

    private void OnModifierChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        if (ModShiftRadio.IsChecked == true)
            _settings.SnowballModifier = "Shift";
        else if (ModCtrlRadio.IsChecked == true)
            _settings.SnowballModifier = "Ctrl";
        else
            _settings.SnowballModifier = "Alt";

        SaveFromUi();
    }

    private void OnAutostartChanged(object sender, RoutedEventArgs e)
    {
        if (_isLoading)
            return;

        AutostartService.SetEnabled(AutostartCheck.IsChecked == true);
        _onAutostartSynced();
    }

    private void OnShakeClick(object sender, RoutedEventArgs e)
    {
        _overlayManager.ShakeSnow();
    }

    private void OnSnowplowClick(object sender, RoutedEventArgs e)
    {
        _overlayManager.TriggerSnowplow();
    }

    private void OnSpawnSnowmanClick(object sender, RoutedEventArgs e)
    {
        _overlayManager.SpawnSnowmanOnTaskbar();
    }

    private void OnClearSnowmenClick(object sender, RoutedEventArgs e)
    {
        _overlayManager.ClearSnowmen();
    }

    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        _settings.ResetToDefaults();
        _settings.Save();
        LoadValues();
    }

    private void OnHideClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
