using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using SnowDesktop.Models;
using SnowDesktop.Native;
using SnowDesktop.Overlay;
using SnowDesktop.Services;
using SnowDesktop.UI;

namespace SnowDesktop;

public partial class App : System.Windows.Application
{
    static App()
    {
        Win32.SetProcessDpiAwarenessContext(Win32.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
    }
    private Mutex? _singleInstanceMutex;
    private NotifyIcon? _trayIcon;
    private Icon? _trayIconGraphic;
    private ToolStripMenuItem? _pauseItem;
    private ToolStripMenuItem? _playModeItem;
    private ToolStripMenuItem? _autostartItem;
    private OverlayManager? _overlayManager;
    private SnowSettings? _settings;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, @"Local\Snezhok_SingleInstance_Mutex", out bool isNew);
        if (!isNew)
        {
            Shutdown();
            return;
        }

        _settings = SnowSettings.Load();
        _overlayManager = new OverlayManager(_settings);

        InitializeTray();

        if (!_settings.IsPaused)
        {
            _overlayManager.Start();
        }
        else
        {
            _overlayManager.Start();
            _overlayManager.SetPaused(true);
        }

        if (e.Args.Contains("--settings", StringComparer.OrdinalIgnoreCase))
        {
            OpenSettings();
        }
    }

    private void InitializeTray()
    {
        _trayIconGraphic = TrayIconHelper.CreateSnowflakeIcon();

        var menu = new ContextMenuStrip();

        _pauseItem = new ToolStripMenuItem(GetPauseMenuText())
        {
            Checked = _settings?.IsPaused == true
        };
        _pauseItem.Click += (_, _) => TogglePause();

        var shakeItem = new ToolStripMenuItem("Стряхнуть весь снег");
        shakeItem.Click += (_, _) => _overlayManager?.ShakeSnow();

        var plowItem = new ToolStripMenuItem("Запустить паровозик");
        plowItem.Click += (_, _) => _overlayManager?.TriggerSnowplow();

        var snowmanItem = new ToolStripMenuItem("Поставить снеговика");
        snowmanItem.Click += (_, _) => _overlayManager?.SpawnSnowmanOnTaskbar();

        var clearSnowmenItem = new ToolStripMenuItem("Убрать снеговиков");
        clearSnowmenItem.Click += (_, _) => _overlayManager?.ClearSnowmen();

        var presetsMenu = new ToolStripMenuItem("Пресеты");
        var presetQuietItem = CreatePresetMenuItem("Тихая ночь", SnowPreset.QuietNight);
        var presetEveningItem = CreatePresetMenuItem("Снежный вечер", SnowPreset.CozyEvening);
        var presetBlizzardItem = CreatePresetMenuItem("Метель", SnowPreset.Blizzard);
        var presetPlaygroundItem = CreatePresetMenuItem("Зимний двор", SnowPreset.WinterPlayground);
        presetsMenu.DropDownItems.AddRange(new ToolStripItem[]
        {
            presetQuietItem,
            presetEveningItem,
            presetBlizzardItem,
            presetPlaygroundItem
        });

        var soundItem = new ToolStripMenuItem("Звуковые эффекты")
        {
            Checked = _settings?.SoundEnabled == true
        };
        soundItem.Click += (_, _) =>
        {
            if (_settings is null) return;
            _settings.SoundEnabled = !_settings.SoundEnabled;
            _settings.Save();
        };

        var garlandItem = new ToolStripMenuItem("Гирлянда сверху экрана")
        {
            Checked = _settings?.GarlandEnabled == true
        };
        garlandItem.Click += (_, _) =>
        {
            if (_settings is null) return;
            _settings.GarlandEnabled = !_settings.GarlandEnabled;
            _settings.Save();
        };

        _playModeItem = new ToolStripMenuItem("Режим снежков (F9)")
        {
            Checked = _overlayManager?.IsPlayMode == true
        };
        _playModeItem.Click += (_, _) => _overlayManager?.TogglePlayMode();

        if (_overlayManager is not null)
        {
            _overlayManager.PlayModeChanged += active =>
            {
                if (_playModeItem is not null)
                {
                    _playModeItem.Checked = active;
                }
            };
        }

        var settingsItem = new ToolStripMenuItem("Настройки...")
        {
            Font = new Font(menu.Font, System.Drawing.FontStyle.Bold)
        };
        settingsItem.Click += (_, _) => OpenSettings();

        _autostartItem = new ToolStripMenuItem("Автозапуск с Windows")
        {
            Checked = AutostartService.IsEnabled()
        };
        _autostartItem.Click += (_, _) => ToggleAutostart();

        var exitItem = new ToolStripMenuItem("Выход");
        exitItem.Click += (_, _) => Shutdown();

        menu.Items.Add(_pauseItem);
        menu.Items.Add(_playModeItem);
        menu.Items.Add(presetsMenu);
        menu.Items.Add(soundItem);
        menu.Items.Add(garlandItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(shakeItem);
        menu.Items.Add(plowItem);
        menu.Items.Add(snowmanItem);
        menu.Items.Add(clearSnowmenItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(_autostartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        menu.Opening += (_, _) =>
        {
            if (_settings is null) return;
            SnowPreset active = _settings.DetectActivePreset();
            presetQuietItem.Checked = active == SnowPreset.QuietNight;
            presetEveningItem.Checked = active == SnowPreset.CozyEvening;
            presetBlizzardItem.Checked = active == SnowPreset.Blizzard;
            presetPlaygroundItem.Checked = active == SnowPreset.WinterPlayground;
            soundItem.Checked = _settings.SoundEnabled;
            garlandItem.Checked = _settings.GarlandEnabled;
        };

        _trayIcon = new NotifyIcon
        {
            Icon = _trayIconGraphic,
            Text = "Snezhok — Снег на рабочем столе",
            ContextMenuStrip = menu,
            Visible = true
        };

        _trayIcon.DoubleClick += (_, _) => OpenSettings();
    }

    private ToolStripMenuItem CreatePresetMenuItem(string title, SnowPreset preset)
    {
        var item = new ToolStripMenuItem(title);
        item.Click += (_, _) =>
        {
            if (_settings is null) return;
            _settings.ApplyPreset(preset);
            _settings.Save();
        };
        return item;
    }

    private string GetPauseMenuText()
    {
        return _settings?.IsPaused == true ? "Возобновить снегопад" : "Пауза";
    }

    private void TogglePause()
    {
        if (_settings is null || _overlayManager is null || _pauseItem is null)
            return;

        bool nextPaused = !_settings.IsPaused;
        _overlayManager.SetPaused(nextPaused);
        _pauseItem.Text = GetPauseMenuText();
        _pauseItem.Checked = nextPaused;
    }

    private void ToggleAutostart()
    {
        bool next = !AutostartService.IsEnabled();
        AutostartService.SetEnabled(next);
        SyncAutostartCheck();
    }

    private void SyncAutostartCheck()
    {
        if (_autostartItem is not null)
        {
            _autostartItem.Checked = AutostartService.IsEnabled();
        }
    }

    private void OpenSettings()
    {
        if (_settings is null || _overlayManager is null)
            return;

        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
                _settingsWindow.WindowState = WindowState.Normal;
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _overlayManager, SyncAutostartCheck);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        _trayIconGraphic?.Dispose();
        _trayIconGraphic = null;

        _overlayManager?.Dispose();
        _overlayManager = null;

        if (_singleInstanceMutex is not null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch
            {
                // Ignore if mutex was not acquired.
            }
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        base.OnExit(e);
    }
}
