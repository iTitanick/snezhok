using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SkiaSharp;
using SnowDesktop.Native;
using SnowDesktop.Rendering;
using SnowDesktop.Simulation;

namespace SnowDesktop.Overlay;

public sealed class OverlayWindow : NativeWindow, IDisposable
{
    private const int HotkeyTogglePlayId = 101;
    private const int HotkeyExitPlayId = 102;

    public Rectangle Bounds { get; }

    public event Action? TogglePlayModeRequested;
    public event Action? ExitPlayModeRequested;
    public event Action<float, float>? PointerDown;
    public event Action<float, float>? PointerMove;
    public event Action<float, float>? PointerUp;
    public event Action<float, float>? SecondaryPointerDown;
    public Func<float, float, bool>? ShouldShowHandCursor;

    private IntPtr _screenDc;
    private IntPtr _memDc;
    private IntPtr _hBitmap;
    private IntPtr _oldBitmap;
    private IntPtr _dibBits;
    private SKSurface? _skSurface;
    private bool _isClickThrough = true;
    private bool _isCapturing;
    private bool _hotkeysRegistered;
    private bool _exitHotkeyRegistered;
    private bool _disposed;

    public OverlayWindow(Rectangle bounds, bool registerHotkeys)
    {
        Bounds = bounds;

        var cp = new CreateParams
        {
            Caption = "SnezhokOverlay",
            X = bounds.Left,
            Y = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            Style = Win32.WS_POPUP,
            ExStyle = Win32.WS_EX_LAYERED |
                      Win32.WS_EX_TRANSPARENT |
                      Win32.WS_EX_TOPMOST |
                      Win32.WS_EX_TOOLWINDOW |
                      Win32.WS_EX_NOACTIVATE
        };

        CreateHandle(cp);
        InitializeSurface(bounds.Width, bounds.Height);

        Win32.SetWindowPos(
            Handle,
            Win32.HWND_TOPMOST,
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            Win32.SWP_NOACTIVATE | Win32.SWP_SHOWWINDOW);

        if (registerHotkeys)
        {
            _hotkeysRegistered = Win32.RegisterHotKey(Handle, HotkeyTogglePlayId, Win32.MOD_NOREPEAT, Win32.VK_F9);
        }
    }

    public void SetClickThrough(bool clickThrough)
    {
        if (_disposed || Handle == IntPtr.Zero || _isClickThrough == clickThrough)
            return;

        _isClickThrough = clickThrough;
        long exStyle = Win32.GetWindowLongPtr(Handle, Win32.GWL_EXSTYLE);
        if (clickThrough)
            exStyle |= Win32.WS_EX_TRANSPARENT;
        else
            exStyle &= ~Win32.WS_EX_TRANSPARENT;

        Win32.SetWindowLongPtr(Handle, Win32.GWL_EXSTYLE, exStyle);
        Win32.SetWindowPos(
            Handle,
            IntPtr.Zero,
            0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOZORDER | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED);
    }

    public void SetExitPlayHotkey(bool enabled)
    {
        if (_disposed || Handle == IntPtr.Zero)
            return;

        if (enabled && !_exitHotkeyRegistered)
        {
            _exitHotkeyRegistered = Win32.RegisterHotKey(Handle, HotkeyExitPlayId, Win32.MOD_NOREPEAT, Win32.VK_ESCAPE);
        }
        else if (!enabled && _exitHotkeyRegistered)
        {
            Win32.UnregisterHotKey(Handle, HotkeyExitPlayId);
            _exitHotkeyRegistered = false;
        }
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case Win32.WM_HOTKEY:
            {
                int id = m.WParam.ToInt32();
                if (id == HotkeyTogglePlayId)
                {
                    TogglePlayModeRequested?.Invoke();
                    return;
                }
                if (id == HotkeyExitPlayId)
                {
                    ExitPlayModeRequested?.Invoke();
                    return;
                }
                break;
            }
            case Win32.WM_LBUTTONDOWN:
            {
                _isCapturing = true;
                Win32.SetCapture(Handle);
                Win32.GetCursorPos(out Win32.POINT pt);
                PointerDown?.Invoke(pt.X, pt.Y);
                m.Result = IntPtr.Zero;
                return;
            }
            case Win32.WM_MOUSEMOVE:
            {
                if (_isCapturing)
                {
                    Win32.GetCursorPos(out Win32.POINT pt);
                    PointerMove?.Invoke(pt.X, pt.Y);
                    m.Result = IntPtr.Zero;
                    return;
                }
                break;
            }
            case Win32.WM_LBUTTONUP:
            {
                if (_isCapturing)
                {
                    _isCapturing = false;
                    Win32.ReleaseCapture();
                    Win32.GetCursorPos(out Win32.POINT pt);
                    PointerUp?.Invoke(pt.X, pt.Y);
                    m.Result = IntPtr.Zero;
                    return;
                }
                break;
            }
            case Win32.WM_RBUTTONDOWN:
            {
                Win32.GetCursorPos(out Win32.POINT pt);
                SecondaryPointerDown?.Invoke(pt.X, pt.Y);
                m.Result = IntPtr.Zero;
                return;
            }
            case Win32.WM_RBUTTONUP:
            {
                m.Result = IntPtr.Zero;
                return;
            }
            case Win32.WM_CAPTURECHANGED:
            {
                if (_isCapturing)
                {
                    _isCapturing = false;
                    Win32.GetCursorPos(out Win32.POINT pt);
                    PointerUp?.Invoke(pt.X, pt.Y);
                }
                break;
            }
            case Win32.WM_SETCURSOR:
            {
                if (!_isClickThrough)
                {
                    Win32.GetCursorPos(out Win32.POINT pt);
                    bool hand = _isCapturing || (ShouldShowHandCursor?.Invoke(pt.X, pt.Y) == true);
                    Win32.SetCursor(Win32.LoadCursor(IntPtr.Zero, hand ? Win32.IDC_HAND : Win32.IDC_ARROW));
                    m.Result = new IntPtr(1);
                    return;
                }
                break;
            }
        }

        base.WndProc(ref m);
    }

    private void InitializeSurface(int width, int height)
    {
        _screenDc = Win32.GetDC(IntPtr.Zero);
        _memDc = Win32.CreateCompatibleDC(_screenDc);

        var bmi = new Win32.BITMAPINFO
        {
            bmiHeader = new Win32.BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<Win32.BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // Top-down DIB to match SkiaSharp coordinates
                biPlanes = 1,
                biBitCount = 32,
                biCompression = Win32.BI_RGB
            }
        };

        _hBitmap = Win32.CreateDIBSection(
            _memDc,
            ref bmi,
            Win32.DIB_RGB_COLORS,
            out _dibBits,
            IntPtr.Zero,
            0);

        if (_hBitmap == IntPtr.Zero || _dibBits == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create DIB section for snow overlay.");
        }

        _oldBitmap = Win32.SelectObject(_memDc, _hBitmap);

        var imageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        _skSurface = SKSurface.Create(imageInfo, _dibBits, width * 4);
    }

    public void RenderAndPresent(SnowRenderer renderer, RenderSnapshot snapshot)
    {
        if (_disposed || _skSurface is null || Handle == IntPtr.Zero)
            return;

        renderer.Render(_skSurface.Canvas, Bounds, snapshot);
        _skSurface.Canvas.Flush();
        Present();
    }

    public void Clear()
    {
        if (_disposed || _skSurface is null || Handle == IntPtr.Zero)
            return;

        _skSurface.Canvas.Clear(SKColors.Transparent);
        _skSurface.Canvas.Flush();
        Present();
    }

    public void EnsureTopMost()
    {
        if (_disposed || Handle == IntPtr.Zero)
            return;

        Win32.SetWindowPos(
            Handle,
            Win32.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE);
    }

    private void Present()
    {
        var dstPt = new Win32.POINT(Bounds.Left, Bounds.Top);
        var size = new Win32.SIZE(Bounds.Width, Bounds.Height);
        var srcPt = new Win32.POINT(0, 0);
        var blend = new Win32.BLENDFUNCTION
        {
            BlendOp = Win32.AC_SRC_OVER,
            BlendFlags = 0,
            SourceConstantAlpha = 255,
            AlphaFormat = Win32.AC_SRC_ALPHA
        };

        Win32.UpdateLayeredWindow(
            Handle,
            _screenDc,
            ref dstPt,
            ref size,
            _memDc,
            ref srcPt,
            0,
            ref blend,
            Win32.ULW_ALPHA);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;

        if (Handle != IntPtr.Zero)
        {
            if (_hotkeysRegistered)
            {
                Win32.UnregisterHotKey(Handle, HotkeyTogglePlayId);
                _hotkeysRegistered = false;
            }
            if (_exitHotkeyRegistered)
            {
                Win32.UnregisterHotKey(Handle, HotkeyExitPlayId);
                _exitHotkeyRegistered = false;
            }
        }

        _skSurface?.Dispose();
        _skSurface = null;

        if (_memDc != IntPtr.Zero)
        {
            if (_oldBitmap != IntPtr.Zero)
            {
                Win32.SelectObject(_memDc, _oldBitmap);
                _oldBitmap = IntPtr.Zero;
            }
            Win32.DeleteDC(_memDc);
            _memDc = IntPtr.Zero;
        }

        if (_hBitmap != IntPtr.Zero)
        {
            Win32.DeleteObject(_hBitmap);
            _hBitmap = IntPtr.Zero;
        }

        if (_screenDc != IntPtr.Zero)
        {
            Win32.ReleaseDC(IntPtr.Zero, _screenDc);
            _screenDc = IntPtr.Zero;
        }

        if (Handle != IntPtr.Zero)
        {
            DestroyHandle();
        }
    }
}
