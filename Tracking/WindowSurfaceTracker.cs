using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using SnowDesktop.Native;

namespace SnowDesktop.Tracking;

public sealed record TrackedSurface(
    IntPtr Hwnd,
    bool IsTaskbar,
    Rectangle Bounds,
    int TopY,
    IReadOnlyList<(int StartX, int EndX)> VisibleSpans);

public sealed record WindowCollider(
    IntPtr Hwnd,
    bool IsTaskbar,
    bool IsMaximized,
    Rectangle Bounds);

public sealed record SurfaceCaptureResult(
    IReadOnlyList<TrackedSurface> Surfaces,
    IReadOnlyList<WindowCollider> Colliders);

public sealed class WindowSurfaceTracker
{
    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.Ordinal)
    {
        "Progman",
        "WorkerW",
        "DummyDWMListenerWindow",
        "EdgeUiInputWndClass",
        "EdgeUiInputTopWndClass",
        "NotifyIconOverflowWindow",
        "TopLevelWindowForOverflowXamlIsland",
        "Shell_InputSwitchTopLevelWindow",
        "ForegroundStaging",
        "LockScreenBackstopFrame",
        "tooltips_class32",
        "SysShadow",
        "#32768",
        "ComboLBox"
    };

    private static readonly int RectSize = Marshal.SizeOf<Win32.RECT>();

    public SurfaceCaptureResult CaptureSurfaces(
        IReadOnlyCollection<IntPtr> excludedHwnds,
        IReadOnlyList<Rectangle> monitorBounds,
        bool includeWindows,
        bool includeTaskbar)
    {
        var results = new List<TrackedSurface>(24);
        var colliders = new List<WindowCollider>(32);
        var occluders = new List<Rectangle>(32);
        var classBuffer = new StringBuilder(128);

        Win32.EnumWindows((hwnd, _) =>
        {
            if (excludedHwnds.Contains(hwnd))
                return true;

            if (!Win32.IsWindowVisible(hwnd) || Win32.IsIconic(hwnd))
                return true;

            if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0 && cloaked != 0)
                return true;

            classBuffer.Clear();
            Win32.GetClassName(hwnd, classBuffer, classBuffer.Capacity);
            string className = classBuffer.ToString();

            if (IgnoredClasses.Contains(className))
                return true;

            bool isTaskbar = className is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";

            long exStyle = Win32.GetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE);
            long style = Win32.GetWindowLongPtr(hwnd, Win32.GWL_STYLE);

            if (!isTaskbar)
            {
                if ((exStyle & Win32.WS_EX_LAYERED) != 0 &&
                    Win32.GetLayeredWindowAttributes(hwnd, out uint _, out byte alpha, out uint flags) &&
                    (flags & Win32.LWA_ALPHA) != 0 &&
                    alpha < 10)
                {
                    return true;
                }

                // Skip floating utility/tooltip windows that have neither AppWindow nor a caption.
                if ((exStyle & Win32.WS_EX_TOOLWINDOW) != 0 &&
                    (exStyle & Win32.WS_EX_APPWINDOW) == 0 &&
                    (style & Win32.WS_CAPTION) == 0)
                {
                    return true;
                }
            }

            Rectangle rect = GetVisibleWindowRect(hwnd);
            if (rect.Width < 80 || rect.Height < (isTaskbar ? 20 : 40))
                return true;

            // Check whether this window intersects any active monitor.
            Rectangle? ownerMonitor = FindPrimaryMonitorForRect(rect, monitorBounds);
            if (ownerMonitor is null)
                return true;

            int topY = rect.Top;

            if ((isTaskbar && includeTaskbar) || (!isTaskbar && includeWindows))
            {
                // Skip accumulation if the top edge sits flush against or above the top of the monitor
                // (e.g., maximized windows), since snow on top of that edge would sit off-screen.
                bool hasSpaceAboveTop = isTaskbar
                    ? (topY > ownerMonitor.Value.Top + 24 && topY < ownerMonitor.Value.Bottom - 6)
                    : (topY > ownerMonitor.Value.Top + 4 && topY < ownerMonitor.Value.Bottom - 10);

                if (hasSpaceAboveTop)
                {
                    int inset = isTaskbar ? 0 : 4;
                    int segLeft = rect.Left + inset;
                    int segRight = rect.Right - inset;

                    if (segRight - segLeft >= 16)
                    {
                        var visibleSpans = SubtractOccluders(segLeft, segRight, topY, occluders);
                        if (visibleSpans.Count > 0)
                        {
                            results.Add(new TrackedSurface(hwnd, isTaskbar, rect, topY, visibleSpans));
                        }
                    }
                }
            }

            bool isMaximized = !isTaskbar &&
                rect.Width >= ownerMonitor.Value.Width - 16 &&
                rect.Height >= ownerMonitor.Value.Height - 60;

            occluders.Add(rect);
            colliders.Add(new WindowCollider(hwnd, isTaskbar, isMaximized, rect));
            return true;
        }, IntPtr.Zero);

        return new SurfaceCaptureResult(results, colliders);
    }

    private static Rectangle GetVisibleWindowRect(IntPtr hwnd)
    {
        if (Win32.DwmGetWindowAttribute(hwnd, Win32.DWMWA_EXTENDED_FRAME_BOUNDS, out Win32.RECT dwmRect, RectSize) == 0)
        {
            return Rectangle.FromLTRB(dwmRect.Left, dwmRect.Top, dwmRect.Right, dwmRect.Bottom);
        }

        if (Win32.GetWindowRect(hwnd, out Win32.RECT rawRect))
        {
            return Rectangle.FromLTRB(rawRect.Left, rawRect.Top, rawRect.Right, rawRect.Bottom);
        }

        return Rectangle.Empty;
    }

    private static Rectangle? FindPrimaryMonitorForRect(Rectangle rect, IReadOnlyList<Rectangle> monitors)
    {
        Rectangle? best = null;
        long bestArea = 0;

        for (int i = 0; i < monitors.Count; i++)
        {
            Rectangle mon = monitors[i];
            Rectangle intersection = Rectangle.Intersect(rect, mon);
            if (intersection.Width > 0 && intersection.Height > 0)
            {
                long area = (long)intersection.Width * intersection.Height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = mon;
                }
            }
        }

        return best;
    }

    private static List<(int StartX, int EndX)> SubtractOccluders(
        int segLeft,
        int segRight,
        int lineY,
        List<Rectangle> occluders)
    {
        var spans = new List<(int StartX, int EndX)>(4) { (segLeft, segRight) };

        for (int i = 0; i < occluders.Count; i++)
        {
            Rectangle occ = occluders[i];
            if (occ.Top > lineY || occ.Bottom <= lineY)
                continue;

            int cutLeft = occ.Left;
            int cutRight = occ.Right;
            if (cutRight <= cutLeft)
                continue;

            for (int s = spans.Count - 1; s >= 0; s--)
            {
                (int a, int b) = spans[s];
                if (cutRight <= a || cutLeft >= b)
                    continue;

                spans.RemoveAt(s);
                if (cutLeft > a && cutLeft - a >= 12)
                {
                    spans.Insert(s, (a, cutLeft));
                    s++;
                }

                if (cutRight < b && b - cutRight >= 12)
                {
                    spans.Insert(s, (cutRight, b));
                }
            }

            if (spans.Count == 0)
                break;
        }

        return spans;
    }
}
