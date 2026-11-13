using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Threading;
using Microsoft.Win32;
using SnowDesktop.Models;
using SnowDesktop.Native;
using SnowDesktop.Rendering;
using SnowDesktop.Services;
using SnowDesktop.Simulation;
using SnowDesktop.Tracking;

namespace SnowDesktop.Overlay;

public sealed class OverlayManager : IDisposable
{
    private readonly SnowSettings _settings;
    private readonly WindowSurfaceTracker _tracker = new();
    private readonly SnowSimulation _simulation = new();
    private readonly SnowRenderer _renderer = new();
    private readonly SnowAudioService _audio = new();
    private readonly List<OverlayWindow> _windows = new();
    private readonly HashSet<IntPtr> _overlayHwnds = new();
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();

    private Rectangle[] _monitors = Array.Empty<Rectangle>();
    private float _surfaceSyncAccum = 1f;
    private float _topMostAccum = 0f;
    private bool _playMode;
    private bool _disposed;

    public bool IsPlayMode => _playMode;
    public event Action<bool>? PlayModeChanged;

    public OverlayManager(SnowSettings settings)
    {
        _settings = settings;

        RebuildOverlays();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _timer.Tick += OnTick;
    }

    public void Start()
    {
        _stopwatch.Restart();
        _surfaceSyncAccum = 1f; // Immediate initial surface capture
        _timer.Start();
    }

    public void SetPaused(bool paused)
    {
        _settings.IsPaused = paused;
        _settings.Save();

        if (paused)
        {
            _audio.StopAll();
            SetPlayMode(false);
            for (int i = 0; i < _windows.Count; i++)
            {
                _windows[i].Clear();
            }
        }
        else
        {
            _stopwatch.Restart();
            _surfaceSyncAccum = 1f;
        }
    }

    public void SetPlayMode(bool enabled)
    {
        if (_playMode == enabled)
            return;

        _playMode = enabled;
        if (_windows.Count > 0)
        {
            _windows[0].SetExitPlayHotkey(enabled);
        }

        UpdateWindowClickThrough(hasCursor: false, 0, 0);
        PlayModeChanged?.Invoke(_playMode);
    }

    public void TogglePlayMode() => SetPlayMode(!_playMode);

    public void ShakeSnow()
    {
        _simulation.ShakeAllSnow();
    }

    public void TriggerSnowplow()
    {
        _simulation.TriggerSnowplow();
    }

    public void SpawnSnowmanOnTaskbar()
    {
        _simulation.SpawnSnowmanOnTaskbar();
    }

    public void ClearSnowmen()
    {
        _simulation.ClearSnowmen();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(RebuildOverlays);
    }

    private void RebuildOverlays()
    {
        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].Dispose();
        }
        _windows.Clear();
        _overlayHwnds.Clear();

        Screen[] screens = Screen.AllScreens;
        _monitors = new Rectangle[screens.Length];

        for (int i = 0; i < screens.Length; i++)
        {
            Rectangle bounds = screens[i].Bounds;
            _monitors[i] = bounds;

            bool isPrimary = i == 0;
            var overlay = new OverlayWindow(bounds, registerHotkeys: isPrimary);
            if (isPrimary)
            {
                overlay.TogglePlayModeRequested += TogglePlayMode;
                overlay.ExitPlayModeRequested += () => SetPlayMode(false);
                overlay.SetExitPlayHotkey(_playMode);
            }

            overlay.PointerDown += (x, y) => _simulation.BeginPointerAction(x, y);
            overlay.PointerMove += (x, y) => _simulation.MovePointerAction(x, y, _settings);
            overlay.PointerUp += (x, y) => _simulation.EndPointerAction(x, y);
            overlay.SecondaryPointerDown += (x, y) => _simulation.BreakSnowmanOrBallAt(x, y);
            overlay.ShouldShowHandCursor = (x, y) => _simulation.HasSnowOrBallNear(x, y);

            _windows.Add(overlay);
            _overlayHwnds.Add(overlay.Handle);
        }

        _simulation.SetMonitors(_monitors);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        if (_disposed || _settings.IsPaused)
            return;

        float dt = (float)_stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();
        if (dt <= 0f)
            dt = 0.016f;

        _surfaceSyncAccum += dt;
        if (_surfaceSyncAccum >= 0.035f)
        {
            _surfaceSyncAccum = 0f;
            SurfaceCaptureResult capture = _tracker.CaptureSurfaces(
                _overlayHwnds,
                _monitors,
                _settings.AccumulateOnWindows,
                _settings.AccumulateOnTaskbar);
            _simulation.SyncSurfaces(capture);
        }

        _topMostAccum += dt;
        if (_topMostAccum >= 2.5f)
        {
            _topMostAccum = 0f;
            for (int i = 0; i < _windows.Count; i++)
            {
                _windows[i].EnsureTopMost();
            }
        }

        bool hasCursor = Win32.GetCursorPos(out Win32.POINT cursorPt);
        UpdateWindowClickThrough(hasCursor, cursorPt.X, cursorPt.Y);

        _simulation.Step(dt, _settings, cursorPt.X, cursorPt.Y, hasCursor);
        RenderSnapshot snapshot = _simulation.CreateSnapshot(_playMode);
        SoundFrameEvents soundEvents = _simulation.ConsumeSoundEvents();

        if (!_settings.SoundEnabled || _settings.SoundVolume <= 0.01)
        {
            _audio.StopAll();
        }
        else
        {
            float baseVol = (float)Math.Clamp(_settings.SoundVolume, 0.0, 1.0);
            _audio.SetTrainMovement(snapshot.Train is not null, baseVol * 0.90f);
            if (soundEvents.PlayTrainWhistle)
            {
                _audio.PlayTrainWhistle(baseVol);
            }
            int shatters = Math.Min(soundEvents.SnowballShatterCount, 2);
            for (int s = 0; s < shatters; s++)
            {
                _audio.PlaySnowballShatter(baseVol);
            }
        }

        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].RenderAndPresent(_renderer, snapshot);
        }
    }

    private void UpdateWindowClickThrough(bool hasCursor, float cursorX, float cursorY)
    {
        bool allowClicks =
            _playMode ||
            _simulation.IsHoldingPointer ||
            (hasCursor && IsModifierPressed(_settings.SnowballModifier) && _simulation.HasSnowOrBallNear(cursorX, cursorY));

        bool clickThrough = !allowClicks;
        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].SetClickThrough(clickThrough);
        }
    }

    private static bool IsModifierPressed(string modifier)
    {
        int vk = modifier switch
        {
            "Shift" => Win32.VK_SHIFT,
            "Ctrl" => Win32.VK_CONTROL,
            _ => Win32.VK_MENU
        };
        return (Win32.GetAsyncKeyState(vk) & 0x8000) != 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        _timer.Stop();
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        for (int i = 0; i < _windows.Count; i++)
        {
            _windows[i].Dispose();
        }
        _windows.Clear();
        _overlayHwnds.Clear();

        _audio.Dispose();
        _renderer.Dispose();
    }
}
