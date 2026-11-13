using System.Drawing;
using SnowDesktop.Models;
using SnowDesktop.Tracking;

namespace SnowDesktop.Simulation;

public sealed class Snowflake
{
    public float X;
    public float Y;
    public float Radius;
    public float Depth;
    public float BaseVy;
    public float Phase;
    public float SwayAmp;
    public float SwayFreq;
    public float Angle;
    public float SpinSpeed;
    public float TurbuScale;
    public float PushVx;
    public float PushVy;
    public int Variant;
    public int Layer; // 0 = far background, 1 = mid, 2 = near foreground
    public byte Alpha;
    public int Seed;
}

public sealed class CrumbleParticle
{
    public float X;
    public float Y;
    public float Vx;
    public float Vy;
    public float Radius;
    public float Life;
    public float MaxLife;
    public byte Alpha;
}

public sealed class CornerGlint
{
    public float X;
    public float Y;
    public float MaxSize;
    public float Life;
    public float MaxLife;
}

public sealed class SmokePuff
{
    public float X;
    public float Y;
    public float Vx;
    public float Vy;
    public float Radius;
    public float Life;
    public float MaxLife;
}

public enum SnowballState
{
    Held,
    Resting,
    Flying
}

public sealed class Snowball
{
    public int Id;
    public int StackedOnId;
    public int FromSnowmanId;
    public float X;
    public float Y;
    public float TargetX;
    public float TargetY;
    public float Vx;
    public float Vy;
    public float Radius;
    public float TargetRadius;
    public float Angle;
    public float SpinSpeed;
    public SnowballState State;
    public bool IsRollingOnSurface;
    public IntPtr RollingSurfaceHwnd;
    public IntPtr RestingHwnd;
    public float RestingOffsetX;
    public float RestingYOffset;
    public IntPtr IgnoreCollisionHwnd;
    public float IgnoreHwndTimer;
    public float GlobalLaunchGrace;
    public bool HasImpactTarget;
    public IntPtr TargetWindowHwnd;
    public float ImpactTargetX;
    public float ImpactTargetY;
    public float TargetFlightElapsed;
    public float TargetFlightDuration;
    public int Seed;
}

public enum SnowmanState
{
    Held,
    Resting,
    Falling
}

public enum SnowmanAction
{
    Idle,
    WaveHat,
    Throw
}

public sealed class Snowman
{
    public int Id;
    public float X;
    public float BaseY;
    public float Vx;
    public float Vy;
    public float TiltAngle;
    public float BaseScale = 1.0f;
    public float Scale;
    public float PopTimer;
    public SnowmanState State;
    public IntPtr RestingHwnd;
    public float RestingOffsetX;
    public float RestingYOffset;
    public IntPtr IgnoreSurfaceHwnd;
    public float IgnoreSurfaceTimer;

    // Live ambient & scheduled behavior state
    public SnowmanAction Action;
    public float ActionTimer;
    public float ActionDuration;
    public float NextActionCooldown = 7f;
    public float NextBlinkCooldown = 3.5f;
    public float BlinkTimer;
    public float LookX;
    public float LookY;
    public float HatLift;
    public float HatTilt;
    public float LeftArmAngle;
    public float RightArmAngle;
    public float HopRenderOffsetY;
    public bool ActionTriggered;
    public float TrainWaveCooldown;
    public bool HasPendingThrowTarget;
    public IntPtr PendingThrowHwnd;
    public float PendingThrowX;
    public float PendingThrowY;
    public int ThrowAimDir = 1;
    public SnowmanOutfit Outfit;
}

public enum SnowmanOutfit
{
    Classic = 0,
    CozyBeanie = 1,
    Bucket = 2,
    Santa = 3,
    Cool = 4
}

public sealed class FrostParticle
{
    public float X;
    public float Y;
    public float Vx;
    public float Vy;
    public float Radius;
    public float Angle;
    public float Spin;
    public float Life;
    public float MaxLife;
    public bool IsStar;
}

public readonly record struct SoundFrameEvents(
    bool PlayTrainWhistle,
    int SnowballShatterCount);

public sealed class SnowplowTrain
{
    public const float BaseLength = 128f;
    public const float SantaWagonLength = 90f;
    public const float TreeWagonLength = 80f;

    public bool Active;
    public IntPtr TargetHwnd;
    public float PlowX;
    public float TopY;
    public int Direction; // +1 = left->right, -1 = right->left
    public float Speed;
    public float WheelAngle;
    public float SmokeTimer;
    public bool ShowSantaWagon = true;
    public bool ShowTreeWagon = true;

    public float CurrentTotalLength => BaseLength
        + (ShowSantaWagon ? SantaWagonLength : 0f)
        + (ShowTreeWagon ? TreeWagonLength : 0f);
}

public sealed class WindowSnowSurface
{
    public const int BucketSize = 3;

    public IntPtr Hwnd { get; }
    public bool IsTaskbar { get; }
    public Rectangle Bounds { get; private set; }
    public int StartX { get; private set; }
    public int EndX { get; private set; }
    public int TopY { get; private set; }
    public float[] Heights { get; private set; }
    public bool[] BucketVisible { get; private set; }

    public WindowSnowSurface(TrackedSurface tracked)
    {
        Hwnd = tracked.Hwnd;
        IsTaskbar = tracked.IsTaskbar;
        Bounds = tracked.Bounds;
        int inset = tracked.IsTaskbar ? 0 : 4;
        StartX = tracked.Bounds.Left + inset;
        EndX = Math.Max(StartX + BucketSize, tracked.Bounds.Right - inset);
        TopY = tracked.TopY;

        int bucketCount = Math.Max(1, (EndX - StartX + BucketSize - 1) / BucketSize);
        Heights = new float[bucketCount];
        BucketVisible = new bool[bucketCount];
        UpdateVisibility(tracked.VisibleSpans);
    }

    public void MoveOrResize(TrackedSurface tracked)
    {
        Bounds = tracked.Bounds;
        int inset = tracked.IsTaskbar ? 0 : 4;
        StartX = tracked.Bounds.Left + inset;
        EndX = Math.Max(StartX + BucketSize, tracked.Bounds.Right - inset);
        TopY = tracked.TopY;

        int newCount = Math.Max(1, (EndX - StartX + BucketSize - 1) / BucketSize);
        if (newCount != Heights.Length)
        {
            var newHeights = new float[newCount];
            int copyCount = Math.Min(Heights.Length, newCount);
            Array.Copy(Heights, newHeights, copyCount);
            Heights = newHeights;
            BucketVisible = new bool[newCount];
        }

        UpdateVisibility(tracked.VisibleSpans);
    }

    public void UpdateVisibility(IReadOnlyList<(int StartX, int EndX)> spans)
    {
        Array.Fill(BucketVisible, false);

        for (int s = 0; s < spans.Count; s++)
        {
            (int spanStart, int spanEnd) = spans[s];
            int iStart = Math.Clamp((spanStart - StartX) / BucketSize, 0, BucketVisible.Length - 1);
            int iEnd = Math.Clamp((spanEnd - StartX) / BucketSize, 0, BucketVisible.Length - 1);
            for (int i = iStart; i <= iEnd; i++)
            {
                BucketVisible[i] = true;
            }
        }
    }
}

public sealed class SnowSimulation
{
    private const int MaxCrumbleParticles = 1600;
    private const int MaxGlints = 64;
    private const float CornerZonePx = 14f;

    private readonly Random _rng = new();
    private readonly List<Snowflake> _flakes = new(450);
    private readonly List<CrumbleParticle> _crumbles = new(512);
    private readonly List<CornerGlint> _glints = new(32);
    private readonly List<SmokePuff> _smoke = new(64);
    private readonly List<Snowball> _snowballs = new(48);
    private readonly List<Snowman> _snowmen = new(24);
    private readonly List<FrostParticle> _frostParticles = new(180);
    private readonly SnowplowTrain _train = new();
    private readonly Dictionary<IntPtr, WindowSnowSurface> _surfaces = new();
    private readonly List<WindowSnowSurface> _surfaceList = new();
    private readonly List<WindowCollider> _colliders = new(32);
    private readonly Dictionary<IntPtr, Rectangle> _colliderMap = new(32);
    private readonly object _sync = new();

    private readonly (float X, float Y, float Time)[] _pointerHistory = new (float, float, float)[10];
    private int _pointerHistCount;
    private int _pointerHistHead;
    private bool _pointerDown;
    private Snowball? _heldBall;
    private Snowman? _heldSnowman;
    private float _heldSnowmanOffsetX;
    private float _heldSnowmanOffsetBaseY;
    private float _lastPointerX;
    private float _lastPointerY;
    private int _nextBallId = 1;
    private int _nextSnowmanId = 1;

    public bool IsHoldingPointer
    {
        get { lock (_sync) return _pointerDown; }
    }

    private float[] _smoothScratch = new float[1024];
    private Rectangle[] _monitors = Array.Empty<Rectangle>();
    private int _minLeft;
    private int _maxRight = 1920;
    private int _maxBottom = 1080;
    private float _time;
    private float _snowplowCooldown = 30f;
    private bool _hasLastCursor;
    private float _lastCursorX;
    private float _lastCursorY;
    private readonly HashSet<IntPtr> _knownColliderHwnds = new();
    private readonly Dictionary<IntPtr, bool> _knownMaximizedState = new();
    private bool _hasInitialColliderSnapshot;
    private float _windowTriggerCooldown;
    private float _snowmanScaleMultiplier = 1.0f;
    private float _frostSpawnDist;
    private int _lastSnowmanClickId;
    private float _lastSnowmanClickTime = -10f;
    private float _pointerDownStartX;
    private float _pointerDownStartY;
    private bool _garlandEnabled = true;
    private int _heldSnowmanIdOnDown;
    private int _nextOutfitCursor;
    private bool _pendingTrainWhistle;
    private int _pendingShatterCount;

    public SoundFrameEvents ConsumeSoundEvents()
    {
        lock (_sync)
        {
            var ev = new SoundFrameEvents(
                _pendingTrainWhistle,
                _pendingShatterCount);
            _pendingTrainWhistle = false;
            _pendingShatterCount = 0;
            return ev;
        }
    }

    private SnowmanOutfit NextOutfit()
    {
        return (SnowmanOutfit)(_nextOutfitCursor++ % 5);
    }

    public void SetMonitors(IReadOnlyList<Rectangle> monitors)
    {
        lock (_sync)
        {
            _monitors = monitors.Count > 0
                ? monitors.ToArray()
                : new[] { new Rectangle(0, 0, 1920, 1080) };

            _minLeft = _monitors.Min(m => m.Left);
            _maxRight = _monitors.Max(m => m.Right);
            _maxBottom = _monitors.Max(m => m.Bottom);
        }
    }

    public void SyncSurfaces(SurfaceCaptureResult capture)
    {
        IReadOnlyList<TrackedSurface> trackedSurfaces = capture.Surfaces;
        lock (_sync)
        {
            _colliders.Clear();
            _colliders.AddRange(capture.Colliders);
            _colliderMap.Clear();
            for (int i = 0; i < _colliders.Count; i++)
            {
                _colliderMap[_colliders[i].Hwnd] = _colliders[i].Bounds;
            }

            if (!_hasInitialColliderSnapshot)
            {
                _knownColliderHwnds.Clear();
                _knownMaximizedState.Clear();
                for (int i = 0; i < _colliders.Count; i++)
                {
                    WindowCollider col = _colliders[i];
                    _knownColliderHwnds.Add(col.Hwnd);
                    _knownMaximizedState[col.Hwnd] = col.IsMaximized;
                }
                _hasInitialColliderSnapshot = true;
            }
            else
            {
                if (_windowTriggerCooldown <= 0f && _snowmen.Count > 0)
                {
                    for (int i = 0; i < _colliders.Count; i++)
                    {
                        WindowCollider col = _colliders[i];
                        if (col.IsTaskbar || col.Bounds.Width < 220 || col.Bounds.Height < 140)
                            continue;

                        bool newlyAppeared = !_knownColliderHwnds.Contains(col.Hwnd);
                        bool newlyMaximized = _knownMaximizedState.TryGetValue(col.Hwnd, out bool wasMax) &&
                            !wasMax &&
                            col.IsMaximized;

                        if (newlyAppeared || newlyMaximized)
                        {
                            float targetX = col.Bounds.Left + col.Bounds.Width * 0.5f;
                            float targetY = col.Bounds.Top + col.Bounds.Height * 0.5f;

                            Snowman? bestThrower = null;
                            float bestScore = float.MaxValue;
                            for (int s = 0; s < _snowmen.Count; s++)
                            {
                                Snowman cand = _snowmen[s];
                                if (cand.State != SnowmanState.Resting)
                                    continue;

                                float score = MathF.Abs(cand.X - targetX) +
                                    MathF.Abs(cand.BaseY - targetY) * 0.5f +
                                    (cand.Action == SnowmanAction.Idle ? 0f : 4000f) +
                                    (cand.HasPendingThrowTarget ? 8000f : 0f);
                                if (score < bestScore)
                                {
                                    bestScore = score;
                                    bestThrower = cand;
                                }
                            }

                            if (bestThrower is not null)
                            {
                                bestThrower.HasPendingThrowTarget = true;
                                bestThrower.PendingThrowHwnd = col.Hwnd;
                                bestThrower.PendingThrowX = targetX;
                                bestThrower.PendingThrowY = targetY;
                                _windowTriggerCooldown = 0.85f;
                            }
                            break;
                        }
                    }
                }

                _knownColliderHwnds.Clear();
                _knownMaximizedState.Clear();
                for (int i = 0; i < _colliders.Count; i++)
                {
                    WindowCollider col = _colliders[i];
                    _knownColliderHwnds.Add(col.Hwnd);
                    _knownMaximizedState[col.Hwnd] = col.IsMaximized;
                }
            }

            var seen = new HashSet<IntPtr>(trackedSurfaces.Count);

            for (int i = 0; i < trackedSurfaces.Count; i++)
            {
                TrackedSurface tracked = trackedSurfaces[i];
                seen.Add(tracked.Hwnd);

                if (_surfaces.TryGetValue(tracked.Hwnd, out WindowSnowSurface? existing))
                {
                    if (existing.Bounds != tracked.Bounds)
                    {
                        // Snowdrifts stay on the window and move/resize together with it.
                        existing.MoveOrResize(tracked);
                    }
                    else
                    {
                        existing.UpdateVisibility(tracked.VisibleSpans);
                    }
                }
                else
                {
                    _surfaces[tracked.Hwnd] = new WindowSnowSurface(tracked);
                }
            }

            // Crumble snow only when a window is closed, minimized, or hidden.
            List<IntPtr>? toRemove = null;
            foreach (KeyValuePair<IntPtr, WindowSnowSurface> kv in _surfaces)
            {
                if (!seen.Contains(kv.Key))
                {
                    ReleaseAllSnow(kv.Value);
                    toRemove ??= new List<IntPtr>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove is not null)
            {
                for (int i = 0; i < toRemove.Count; i++)
                {
                    _surfaces.Remove(toRemove[i]);
                }
            }

            _surfaceList.Clear();
            for (int i = 0; i < trackedSurfaces.Count; i++)
            {
                if (_surfaces.TryGetValue(trackedSurfaces[i].Hwnd, out WindowSnowSurface? surf))
                {
                    _surfaceList.Add(surf);
                }
            }
        }
    }

    public void ShakeAllSnow()
    {
        lock (_sync)
        {
            foreach (WindowSnowSurface surface in _surfaces.Values)
            {
                ReleaseAllSnow(surface);
            }

            for (int i = _snowballs.Count - 1; i >= 0; i--)
            {
                Snowball b = _snowballs[i];
                if (b.State == SnowballState.Resting)
                {
                    b.State = SnowballState.Flying;
                    b.StackedOnId = 0;
                    b.RestingHwnd = IntPtr.Zero;
                    b.Vx = (_rng.NextSingle() - 0.5f) * 80f;
                    b.Vy = 20f;
                }
            }

            for (int i = 0; i < _snowmen.Count; i++)
            {
                Snowman m = _snowmen[i];
                if (m.State == SnowmanState.Resting && m.RestingHwnd != IntPtr.Zero)
                {
                    m.State = SnowmanState.Falling;
                    m.RestingHwnd = IntPtr.Zero;
                    m.Vx = (_rng.NextSingle() - 0.5f) * 45f;
                    m.Vy = -35f;
                }
            }
        }
    }

    public void TriggerSnowplow()
    {
        lock (_sync)
        {
            LaunchSnowplow(requireExistingSnow: false);
        }
    }

    public void SpawnSnowmanOnTaskbar()
    {
        lock (_sync)
        {
            if (_snowmen.Count >= 16)
            {
                _snowmen.RemoveAt(0);
            }

            WindowSnowSurface? taskbarSurf = null;
            for (int i = 0; i < _surfaceList.Count; i++)
            {
                if (_surfaceList[i].IsTaskbar)
                {
                    taskbarSurf = _surfaceList[i];
                    break;
                }
            }

            if (taskbarSurf is not null)
            {
                float minX = taskbarSurf.StartX + 42f;
                float maxX = Math.Max(minX, taskbarSurf.EndX - 42f);
                float spawnX = PickFreeTaskbarSnowmanX(minX, maxX, taskbarSurf);
                float localH = GetSurfaceHeightAt(taskbarSurf, spawnX);
                float yOff = localH * 0.25f;

                var snowman = new Snowman
                {
                    Id = _nextSnowmanId++,
                    X = spawnX,
                    BaseY = taskbarSurf.TopY - yOff,
                    BaseScale = 1.08f,
                    Scale = Math.Clamp(1.08f * _snowmanScaleMultiplier, 0.50f, 2.50f),
                    PopTimer = 0.45f,
                    State = SnowmanState.Resting,
                    RestingHwnd = taskbarSurf.Hwnd,
                    RestingOffsetX = spawnX - taskbarSurf.Bounds.Left,
                    RestingYOffset = yOff,
                    Outfit = NextOutfit()
                };
                _snowmen.Add(snowman);
                SpawnSnowmanMagic(snowman.X, snowman.BaseY - 40f * snowman.Scale, snowman.Scale);
                return;
            }

            // Fallback if taskbar snow accumulation is disabled: place via collider or screen bottom.
            for (int i = 0; i < _colliders.Count; i++)
            {
                if (_colliders[i].IsTaskbar)
                {
                    Rectangle r = _colliders[i].Bounds;
                    float minX = r.Left + 42f;
                    float maxX = Math.Max(minX, r.Right - 42f);
                    float spawnX = minX + (maxX - minX) * (0.25f + _rng.NextSingle() * 0.5f);

                    var snowman = new Snowman
                    {
                        Id = _nextSnowmanId++,
                        X = spawnX,
                        BaseY = r.Top,
                        BaseScale = 1.08f,
                        Scale = Math.Clamp(1.08f * _snowmanScaleMultiplier, 0.50f, 2.50f),
                        PopTimer = 0.45f,
                        State = SnowmanState.Falling,
                        Vx = 0f,
                        Vy = 0f,
                        Outfit = NextOutfit()
                    };
                    _snowmen.Add(snowman);
                    SpawnSnowmanMagic(snowman.X, snowman.BaseY - 40f * snowman.Scale, snowman.Scale);
                    return;
                }
            }
        }
    }

    public void ClearSnowmen()
    {
        lock (_sync)
        {
            for (int i = _snowmen.Count - 1; i >= 0; i--)
            {
                SpawnSnowmanBreak(_snowmen[i], 0f, -25f);
                _snowmen.RemoveAt(i);
            }

            _heldSnowman = null;
        }
    }

    public void BreakSnowmanOrBallAt(float x, float y)
    {
        lock (_sync)
        {
            Snowman? hitSnowman = FindSnowmanAt(x, y);
            if (hitSnowman is not null)
            {
                SpawnSnowmanBreak(hitSnowman, 0f, -40f);
                _snowmen.Remove(hitSnowman);
                if (_heldSnowman == hitSnowman)
                {
                    _heldSnowman = null;
                }
                return;
            }

            // Also allow breaking a resting snowball with right-click.
            Snowball? bestBall = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball b = _snowballs[i];
                if (b.State == SnowballState.Held)
                    continue;

                float dx = b.X - x;
                float dy = b.Y - y;
                float distSq = dx * dx + dy * dy;
                float hitR = b.Radius + 16f;
                if (distSq <= hitR * hitR && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestBall = b;
                }
            }

            if (bestBall is not null)
            {
                for (int i = 0; i < _snowballs.Count; i++)
                {
                    Snowball child = _snowballs[i];
                    if (child.StackedOnId == bestBall.Id)
                    {
                        child.StackedOnId = 0;
                        child.State = SnowballState.Flying;
                        child.RestingHwnd = IntPtr.Zero;
                        child.Vx = (_rng.NextSingle() - 0.5f) * 35f;
                        child.Vy = 15f;
                    }
                }

                ShatterSnowball(bestBall, 0f, -60f, IntPtr.Zero, 0f, -1f, 28f);
                _snowballs.Remove(bestBall);
            }
        }
    }

    public bool HasSnowOrBallNear(float x, float y)
    {
        lock (_sync)
        {
            if (FindSnowmanAt(x, y) is not null)
                return true;

            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball b = _snowballs[i];
                if (b.State == SnowballState.Held)
                    continue;
                float dx = b.X - x;
                float dy = b.Y - y;
                float hitR = b.Radius + 16f;
                if (dx * dx + dy * dy <= hitR * hitR)
                    return true;
            }

            for (int i = 0; i < _surfaceList.Count; i++)
            {
                WindowSnowSurface s = _surfaceList[i];
                if (x < s.StartX - 10 || x > s.EndX + 10)
                    continue;
                if (y < s.TopY - 60 || y > s.TopY + 24)
                    continue;
                if (HasSnowNearX(s, x, 6))
                    return true;
            }

            return false;
        }
    }

    public void BeginPointerAction(float x, float y)
    {
        lock (_sync)
        {
            _pointerDown = true;
            _pointerDownStartX = x;
            _pointerDownStartY = y;
            _heldSnowmanIdOnDown = 0;
            _pointerHistCount = 0;
            _pointerHistHead = 0;
            PushPointerSample(x, y);
            _lastPointerX = x;
            _lastPointerY = y;
            _heldBall = null;
            _heldSnowman = null;

            // 0. Check if grabbing or double-clicking an existing snowman first.
            Snowman? hitSnowman = FindSnowmanAt(x, y);
            if (hitSnowman is not null)
            {
                if (_lastSnowmanClickId == hitSnowman.Id && (_time - _lastSnowmanClickTime) <= 0.42f)
                {
                    hitSnowman.Outfit = (SnowmanOutfit)(((int)hitSnowman.Outfit + 1) % 5);
                    hitSnowman.PopTimer = 0.34f;
                    SpawnSnowmanMagic(hitSnowman.X, hitSnowman.BaseY - 46f * hitSnowman.Scale, hitSnowman.Scale * 0.72f);
                    _lastSnowmanClickId = 0;
                    _lastSnowmanClickTime = -10f;
                    _pointerDown = false;
                    return;
                }

                _heldSnowmanIdOnDown = hitSnowman.Id;
                hitSnowman.State = SnowmanState.Held;
                hitSnowman.IgnoreSurfaceHwnd = hitSnowman.RestingHwnd;
                hitSnowman.RestingHwnd = IntPtr.Zero;
                hitSnowman.Vx = 0f;
                hitSnowman.Vy = 0f;
                _heldSnowman = hitSnowman;
                _heldSnowmanOffsetX = hitSnowman.X - x;
                _heldSnowmanOffsetBaseY = hitSnowman.BaseY - y;
                return;
            }

            // 1. Try grabbing an existing resting or flying snowball near cursor.
            Snowball? bestBall = null;
            float bestDistSq = float.MaxValue;
            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball b = _snowballs[i];
                if (b.State == SnowballState.Held)
                    continue;

                float dx = b.X - x;
                float dy = b.Y - y;
                float distSq = dx * dx + dy * dy;
                float hitR = b.Radius + 16f;
                if (distSq <= hitR * hitR && distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestBall = b;
                }
            }

            if (bestBall is not null)
            {
                // Detach any snowball that was stacked on top of the grabbed ball.
                for (int i = 0; i < _snowballs.Count; i++)
                {
                    Snowball child = _snowballs[i];
                    if (child.StackedOnId == bestBall.Id)
                    {
                        child.StackedOnId = 0;
                        child.State = SnowballState.Flying;
                        child.RestingHwnd = IntPtr.Zero;
                        child.Vx = (_rng.NextSingle() - 0.5f) * 35f;
                        child.Vy = 15f;
                    }
                }

                bestBall.State = SnowballState.Held;
                bestBall.StackedOnId = 0;
                bestBall.IsRollingOnSurface = false;
                bestBall.RollingSurfaceHwnd = IntPtr.Zero;
                bestBall.IgnoreCollisionHwnd = bestBall.RestingHwnd;
                bestBall.RestingHwnd = IntPtr.Zero;
                bestBall.Vx = 0f;
                bestBall.Vy = 0f;
                bestBall.X = x;
                bestBall.Y = y;
                bestBall.TargetX = x;
                bestBall.TargetY = y;
                bestBall.TargetRadius = bestBall.Radius;
                _heldBall = bestBall;
                return;
            }

            // 2. Otherwise try scooping a fresh snowball strictly from a window frame or taskbar drift.
            TryScoopNewSnowballAt(x, y);
        }
    }

    public void MovePointerAction(float x, float y, SnowSettings settings)
    {
        lock (_sync)
        {
            if (!_pointerDown)
                return;

            PushPointerSample(x, y);
            float dx = x - _lastPointerX;
            _lastPointerX = x;
            _lastPointerY = y;

            if (_heldSnowman is not null)
            {
                _heldSnowman.X = x + _heldSnowmanOffsetX;
                _heldSnowman.BaseY = y + _heldSnowmanOffsetBaseY;
                _heldSnowman.TiltAngle = Math.Clamp(_heldSnowman.TiltAngle * 0.85f + dx * 0.45f, -20f, 20f);
                return;
            }

            if (_heldBall is null)
            {
                TryScoopNewSnowballAt(x, y);
            }

            if (_heldBall is null)
                return;

            // Keep new snowballs snapped strictly onto the window top frame / taskbar while rolling,
            // detaching into free hand only when pulled off the frame to lift and throw.
            if (_heldBall.IsRollingOnSurface &&
                _heldBall.RollingSurfaceHwnd != IntPtr.Zero &&
                _surfaces.TryGetValue(_heldBall.RollingSurfaceHwnd, out WindowSnowSurface? rollSurf))
            {
                float detachUp = 58f + _heldBall.Radius * 0.65f;
                bool pulledOff = y < rollSurf.TopY - detachUp ||
                                 y > rollSurf.TopY + 34f ||
                                 x < rollSurf.StartX - 28f ||
                                 x > rollSurf.EndX + 28f;

                if (pulledOff)
                {
                    _heldBall.IsRollingOnSurface = false;
                    _heldBall.IgnoreCollisionHwnd = rollSurf.Hwnd;
                    _heldBall.RollingSurfaceHwnd = IntPtr.Zero;
                    _heldBall.TargetX = x;
                    _heldBall.TargetY = y;
                    _heldBall.X = x;
                    _heldBall.Y = y;
                }
                else
                {
                    float margin = MathF.Min(_heldBall.TargetRadius * 0.35f, 8f);
                    float minX = rollSurf.StartX + margin;
                    float maxX = Math.Max(minX, rollSurf.EndX - margin);
                    float prevTargetX = _heldBall.TargetX;
                    float nextX = Math.Clamp(x, minX, maxX);
                    float rollDx = nextX - prevTargetX;
                    float rollDist = MathF.Abs(rollDx);

                    if (rollDist > 0.4f)
                    {
                        RollGrowBallOnSurface(_heldBall, rollSurf, prevTargetX, nextX, (float)settings.MaxDriftHeight);
                    }

                    _heldBall.TargetX = nextX;
                    float localH = GetSurfaceHeightAt(rollSurf, nextX);
                    _heldBall.TargetY = ComputeSnowSurfaceCenterY(rollSurf.TopY, localH, _heldBall.TargetRadius);
                    return;
                }
            }
            else
            {
                _heldBall.IsRollingOnSurface = false;
                _heldBall.RollingSurfaceHwnd = IntPtr.Zero;
            }

            _heldBall.TargetX = x;
            _heldBall.TargetY = y;
            _heldBall.X = x;
            _heldBall.Y = y;
            _heldBall.Angle = (_heldBall.Angle + dx * (180f / (MathF.PI * MathF.Max(5f, _heldBall.Radius)))) % 360f;
        }
    }

    public void EndPointerAction(float x, float y)
    {
        lock (_sync)
        {
            if (!_pointerDown)
                return;

            PushPointerSample(x, y);

            if (_heldSnowman is not null)
            {
                (float vx, float vy) = ComputeReleaseVelocity();
                float speed = MathF.Sqrt(vx * vx + vy * vy);

                if (speed < 200f && TryFindSnowmanSurfaceAt(_heldSnowman.X, _heldSnowman.BaseY, out WindowSnowSurface? snapSurf))
                {
                    float clampX = Math.Clamp(_heldSnowman.X, snapSurf!.StartX + 12f, Math.Max(snapSurf.StartX + 12f, snapSurf.EndX - 12f));
                    float localH = GetSurfaceHeightAt(snapSurf, clampX);
                    _heldSnowman.State = SnowmanState.Resting;
                    _heldSnowman.RestingHwnd = snapSurf.Hwnd;
                    _heldSnowman.X = clampX;
                    _heldSnowman.RestingOffsetX = clampX - snapSurf.Bounds.Left;
                    _heldSnowman.RestingYOffset = localH * 0.25f;
                    _heldSnowman.BaseY = snapSurf.TopY - _heldSnowman.RestingYOffset;
                    _heldSnowman.Vx = 0f;
                    _heldSnowman.Vy = 0f;
                    _heldSnowman.TiltAngle = 0f;
                    _heldSnowman.PopTimer = 0.26f;
                    SpawnLandingDust(clampX, _heldSnowman.BaseY, _heldSnowman.Scale);
                }
                else
                {
                    // Throw / release: snowman never breaks, just glides & drops gently to nearest surface below!
                    _heldSnowman.State = SnowmanState.Falling;
                    _heldSnowman.Vx = Math.Clamp(vx * 0.90f, -1100f, 1100f);
                    _heldSnowman.Vy = Math.Clamp(vy * 0.90f, -1100f, 1100f);
                    _heldSnowman.IgnoreSurfaceTimer = 0.16f;
                }

                float clickMove = MathF.Abs(x - _pointerDownStartX) + MathF.Abs(y - _pointerDownStartY);
                if (_heldSnowmanIdOnDown == _heldSnowman.Id && clickMove < 10f && speed < 150f)
                {
                    _lastSnowmanClickId = _heldSnowman.Id;
                    _lastSnowmanClickTime = _time;
                }
                else
                {
                    _lastSnowmanClickId = 0;
                }

                _heldSnowman = null;
                _pointerDown = false;
                return;
            }

            if (_heldBall is not null)
            {
                (float vx, float vy) = ComputeReleaseVelocity();
                float speed = MathF.Sqrt(vx * vx + vy * vy);

                bool launchedFromRoll = _heldBall.IsRollingOnSurface;
                WindowSnowSurface? rolledSurf = launchedFromRoll && _heldBall.RollingSurfaceHwnd != IntPtr.Zero
                    && _surfaces.TryGetValue(_heldBall.RollingSurfaceHwnd, out WindowSnowSurface? foundRoll)
                        ? foundRoll
                        : null;

                _heldBall.IsRollingOnSurface = false;
                _heldBall.RollingSurfaceHwnd = IntPtr.Zero;

                if (speed < 360f && TryStackOrBuildSnowman(_heldBall))
                {
                    _heldBall = null;
                    _pointerDown = false;
                    return;
                }

                if (rolledSurf is not null && speed < 340f && vy > -220f)
                {
                    _heldBall.Radius = MathF.Max(_heldBall.Radius, _heldBall.TargetRadius);
                    float localH = GetSurfaceHeightAt(rolledSurf, _heldBall.X);
                    _heldBall.State = SnowballState.Resting;
                    _heldBall.StackedOnId = 0;
                    _heldBall.RestingHwnd = rolledSurf.Hwnd;
                    _heldBall.RestingOffsetX = _heldBall.X - rolledSurf.Bounds.Left;
                    _heldBall.RestingYOffset = localH * 0.56f + _heldBall.Radius * 0.84f;
                    _heldBall.Y = rolledSurf.TopY - _heldBall.RestingYOffset;
                    _heldBall.Vx = 0f;
                    _heldBall.Vy = 0f;
                    _heldBall.SpinSpeed = 0f;
                }
                else if (!launchedFromRoll && speed < 180f && TryFindRestingSurfaceAt(_heldBall.X, _heldBall.Y, _heldBall.Radius, out WindowSnowSurface? restSurf, out float restTopH))
                {
                    _heldBall.State = SnowballState.Resting;
                    _heldBall.StackedOnId = 0;
                    _heldBall.RestingHwnd = restSurf!.Hwnd;
                    _heldBall.RestingOffsetX = _heldBall.X - restSurf.Bounds.Left;
                    _heldBall.RestingYOffset = MathF.Max(_heldBall.Radius * 0.65f, restTopH * 0.55f + _heldBall.Radius * 0.62f);
                    _heldBall.Y = restSurf.TopY - _heldBall.RestingYOffset;
                    _heldBall.Vx = 0f;
                    _heldBall.Vy = 0f;
                    _heldBall.SpinSpeed = 0f;
                }
                else
                {
                    // Smooth, natural throw matching snowman glide feel.
                    const float throwScale = 0.90f;
                    vx *= throwScale;
                    vy *= throwScale;
                    float throwSpeed = MathF.Sqrt(vx * vx + vy * vy);
                    const float maxThrowSpeed = 1100f;
                    if (throwSpeed > maxThrowSpeed)
                    {
                        float scale = maxThrowSpeed / throwSpeed;
                        vx *= scale;
                        vy *= scale;
                    }

                    _heldBall.State = SnowballState.Flying;
                    _heldBall.StackedOnId = 0;
                    _heldBall.Vx = vx;
                    _heldBall.Vy = vy;
                    _heldBall.SpinSpeed = Math.Clamp(vx * 0.30f, -260f, 260f);
                    _heldBall.GlobalLaunchGrace = 0.085f;
                    _heldBall.IgnoreHwndTimer = 0.22f;
                }
            }

            _heldBall = null;
            _pointerDown = false;
        }
    }

    public void Step(float dt, SnowSettings settings, float cursorX = 0f, float cursorY = 0f, bool hasCursor = false)
    {
        dt = Math.Clamp(dt, 0.001f, 0.05f);

        lock (_sync)
        {
            if (_monitors.Length == 0)
                return;

            _time += dt;
            AdjustFlakeCount(settings.SnowflakeCount);

            _snowmanScaleMultiplier = Math.Clamp((float)settings.SnowmanScale, 0.50f, 2.50f);
            _garlandEnabled = settings.GarlandEnabled;
            float speedMul = (float)settings.FallSpeed;
            float windBase = (float)settings.WindStrength * 60f;
            float blizzard = (float)settings.BlizzardIntensity;
            float maxDrift = (float)settings.MaxDriftHeight;
            float accRate = (float)settings.AccumulationRate;

            float cursorVx = 0f;
            float cursorVy = 0f;
            float cursorSpeed = 0f;
            bool hasCursorSegment = false;
            float prevCursorX = cursorX;
            float prevCursorY = cursorY;

            if (hasCursor)
            {
                if (_hasLastCursor)
                {
                    prevCursorX = _lastCursorX;
                    prevCursorY = _lastCursorY;
                    float cdx = cursorX - prevCursorX;
                    float cdy = cursorY - prevCursorY;
                    float moveDist = MathF.Sqrt(cdx * cdx + cdy * cdy);
                    if (moveDist > 0.2f && moveDist < 650f)
                    {
                        hasCursorSegment = true;
                        cursorVx = Math.Clamp(cdx / dt, -1400f, 1400f);
                        cursorVy = Math.Clamp(cdy / dt, -1400f, 1400f);
                        cursorSpeed = MathF.Sqrt(cursorVx * cursorVx + cursorVy * cursorVy);
                    }
                    if (settings.MouseInteraction && !_pointerDown && moveDist > 0.6f && moveDist < 650f)
                    {
                        ApplyCursorSweep(prevCursorX, prevCursorY, cursorX, cursorY, dt, maxDrift);
                    }

                    if (settings.CursorFrostTrail && hasCursorSegment && !_pointerDown)
                    {
                        _frostSpawnDist += moveDist;
                        int spawned = 0;
                        while (_frostSpawnDist >= 11f && spawned < 4 && _frostParticles.Count < 160)
                        {
                            _frostSpawnDist -= 11f;
                            float tSeg = _rng.NextSingle();
                            float jitterX = (_rng.NextSingle() - 0.5f) * 14f;
                            float jitterY = (_rng.NextSingle() - 0.5f) * 14f;
                            float maxLife = 0.48f + _rng.NextSingle() * 0.38f;
                            _frostParticles.Add(new FrostParticle
                            {
                                X = prevCursorX + cdx * tSeg + jitterX,
                                Y = prevCursorY + cdy * tSeg + jitterY,
                                Vx = cursorVx * 0.05f + (_rng.NextSingle() - 0.5f) * 26f,
                                Vy = cursorVy * 0.05f + (10f + _rng.NextSingle() * 24f),
                                Radius = 1.15f + _rng.NextSingle() * 1.45f,
                                Angle = _rng.NextSingle() * 360f,
                                Spin = (_rng.NextSingle() - 0.5f) * 140f,
                                Life = maxLife,
                                MaxLife = maxLife,
                                IsStar = _rng.NextSingle() < 0.42f
                            });
                            spawned++;
                        }
                        if (_frostSpawnDist > 44f)
                        {
                            _frostSpawnDist = 0f;
                        }
                    }
                }
                _lastCursorX = cursorX;
                _lastCursorY = cursorY;
                _hasLastCursor = true;
            }
            else
            {
                _hasLastCursor = false;
            }

            // Dynamic wind gusts (layered frequencies + occasional stronger surges).
            float gustWave =
                MathF.Sin(_time * 0.52f) * 0.55f +
                MathF.Sin(_time * 1.38f + 1.2f) * 0.35f +
                MathF.Pow(MathF.Max(0f, MathF.Sin(_time * 0.24f + 0.4f)), 3f) * 1.45f;

            float windGustX = gustWave * 90f * blizzard;
            float windSpinBoost = 1f + MathF.Abs(gustWave) * 1.5f * blizzard;
            float flakeDrag = MathF.Exp(-1.65f * dt);

            for (int i = 0; i < _flakes.Count; i++)
            {
                Snowflake flake = _flakes[i];
                float prevY = flake.Y;

                // Cursor-as-wind repulsion: flakes gently part in a compact zone around the cursor / swipe path.
                if (hasCursor)
                {
                    const float windRadius = 120f;
                    float refX = cursorX;
                    float refY = cursorY;
                    if (hasCursorSegment)
                    {
                        float segX = cursorX - prevCursorX;
                        float segY = cursorY - prevCursorY;
                        float segLenSq = segX * segX + segY * segY;
                        if (segLenSq > 1f)
                        {
                            float proj = Math.Clamp(((flake.X - prevCursorX) * segX + (flake.Y - prevCursorY) * segY) / segLenSq, 0f, 1f);
                            refX = prevCursorX + segX * proj;
                            refY = prevCursorY + segY * proj;
                        }
                    }

                    float dx = flake.X - refX;
                    float dy = flake.Y - refY;
                    float distSq = dx * dx + dy * dy;
                    if (distSq < windRadius * windRadius)
                    {
                        float dist = MathF.Sqrt(distSq);
                        float falloff = MathF.Pow(1f - dist / windRadius, 0.95f);

                        float invDist = dist > 1.5f ? (1f / dist) : 0f;
                        float dirX = invDist > 0f ? dx * invDist : ((_rng.NextSingle() - 0.5f) * 2f);
                        float dirY = invDist > 0f ? dy * invDist : -0.7f;

                        float depthScale = flake.Layer == 0 ? 0.62f : (0.89f + 0.33f * flake.Depth);
                        float pushAccel = (1150f + cursorSpeed * 1.05f) * falloff * depthScale;

                        flake.PushVx += dirX * pushAccel * dt;
                        flake.PushVy += dirY * pushAccel * dt;

                        // Gentle air cushion push so snowflakes curve around even a resting cursor.
                        flake.X += dirX * 72f * falloff * depthScale * dt;
                        flake.Y += dirY * 72f * falloff * depthScale * dt;

                        if (cursorSpeed > 25f)
                        {
                            float wakeFactor = falloff * depthScale * 3.8f * dt;
                            flake.PushVx += cursorVx * wakeFactor;
                            flake.PushVy += cursorVy * wakeFactor;
                        }

                        const float maxPush = 640f;
                        float pushSq = flake.PushVx * flake.PushVx + flake.PushVy * flake.PushVy;
                        if (pushSq > maxPush * maxPush)
                        {
                            float s = maxPush / MathF.Sqrt(pushSq);
                            flake.PushVx *= s;
                            flake.PushVy *= s;
                        }

                        if (flake.Layer > 0)
                        {
                            flake.Angle = (flake.Angle + (dirX * 160f + cursorVx * 0.15f) * falloff * dt) % 360f;
                        }
                    }
                }

                // Coherent spatial turbulence eddies so snowflakes swirl together like in a snowstorm.
                float swirlX =
                    MathF.Sin(flake.X * 0.0042f + flake.Y * 0.0031f + _time * 1.35f) * 48f +
                    MathF.Cos(flake.Y * 0.0058f - _time * 1.85f + flake.Phase) * 26f;
                float swirlY =
                    MathF.Cos(flake.X * 0.0048f - flake.Y * 0.0028f + _time * 1.55f) * 20f;

                float sway = MathF.Sin(_time * flake.SwayFreq + flake.Phase) * flake.SwayAmp;
                float depthWindFactor = 0.35f + 0.70f * flake.Depth;

                float vx = (windBase + windGustX) * depthWindFactor
                         + swirlX * blizzard * flake.TurbuScale * depthWindFactor
                         + sway
                         + flake.PushVx;
                float vy = flake.BaseVy * speedMul + swirlY * blizzard * flake.Depth + flake.PushVy;
                if (flake.PushVy > -15f && vy < 10f)
                    vy = 10f;

                flake.X += vx * dt;
                flake.Y += vy * dt;
                flake.PushVx *= flakeDrag;
                flake.PushVy *= flakeDrag;

                if (flake.SpinSpeed != 0f)
                {
                    flake.Angle = (flake.Angle + flake.SpinSpeed * windSpinBoost * dt) % 360f;
                }

                // Wrap horizontally across virtual desktop.
                float totalWidth = Math.Max(1, _maxRight - _minLeft);
                if (flake.X < _minLeft - 10)
                    flake.X += totalWidth + 20;
                else if (flake.X > _maxRight + 10)
                    flake.X -= totalWidth + 20;

                bool landed = false;

                // Far background flakes (Layer 0) drift behind windows and rarely catch only the taskbar.
                if (flake.Layer > 0)
                {
                    for (int s = 0; s < _surfaceList.Count; s++)
                    {
                        WindowSnowSurface surface = _surfaceList[s];
                        if (flake.X < surface.StartX || flake.X >= surface.EndX)
                            continue;

                        if (!ShouldLandOn(flake, surface))
                            continue;

                        int bucket = Math.Clamp((int)((flake.X - surface.StartX) / WindowSnowSurface.BucketSize), 0, surface.Heights.Length - 1);
                        if (!surface.BucketVisible[bucket])
                            continue;

                        float surfaceTop = surface.TopY - surface.Heights[bucket];
                        if (prevY <= surfaceTop + 4f && flake.Y >= surfaceTop)
                        {
                            bool nearLeftCorner = !surface.IsTaskbar && (flake.X - surface.StartX) <= CornerZonePx;
                            bool nearRightCorner = !surface.IsTaskbar && (surface.EndX - flake.X) <= CornerZonePx;

                            if (nearLeftCorner || nearRightCorner)
                            {
                                SpawnCornerSplash(flake.X, surfaceTop, nearLeftCorner);
                                DepositSnow(surface, bucket, flake.Radius, maxDrift, accRate * 0.45f);
                            }
                            else
                            {
                                DepositSnow(surface, bucket, flake.Radius, maxDrift, accRate);
                            }

                            RespawnFlakeAtTop(flake);
                            landed = true;
                            break;
                        }
                    }
                }

                if (!landed && flake.Y > _maxBottom + 12f)
                {
                    RespawnFlakeAtTop(flake);
                }
            }

            // Update falling crumble / corner-splash particles.
            for (int i = _crumbles.Count - 1; i >= 0; i--)
            {
                CrumbleParticle p = _crumbles[i];
                p.Vy += 400f * dt;
                p.Vx *= MathF.Max(0f, 1f - 1.1f * dt);
                p.X += p.Vx * dt;
                p.Y += p.Vy * dt;
                p.Life -= dt;

                if (p.Life <= 0f || p.Y > _maxBottom + 15f)
                {
                    _crumbles.RemoveAt(i);
                }
            }

            // Update corner sparkle glints.
            for (int i = _glints.Count - 1; i >= 0; i--)
            {
                CornerGlint g = _glints[i];
                g.Life -= dt;
                if (g.Life <= 0f)
                {
                    _glints.RemoveAt(i);
                }
            }

            // Update delicate cursor frost trail particles.
            for (int i = _frostParticles.Count - 1; i >= 0; i--)
            {
                FrostParticle fp = _frostParticles[i];
                fp.Vx *= MathF.Max(0f, 1f - 1.8f * dt);
                fp.Vy += 26f * dt;
                fp.X += fp.Vx * dt;
                fp.Y += fp.Vy * dt;
                fp.Angle += fp.Spin * dt;
                fp.Life -= dt;
                if (fp.Life <= 0f)
                {
                    _frostParticles.RemoveAt(i);
                }
            }

            // Update snowplow train & smoke puffs on the taskbar.
            if (_windowTriggerCooldown > 0f)
            {
                _windowTriggerCooldown = MathF.Max(0f, _windowTriggerCooldown - dt);
            }
            UpdateSnowplow(dt, settings);
            UpdateSmokePuffs(dt, windBase + windGustX * 0.4f);

            // Update resting/flying snowballs and snowmen.
            UpdateSnowballs(dt, windBase + windGustX * 0.35f, maxDrift, settings.SnowmanAnimations);
            UpdateSnowmen(dt, settings, cursorX, cursorY, hasCursor);

            // Relax slopes, round sharp peaks, and gently melt accumulated drifts.
            float melt = (float)settings.MeltRate * 0.08f * dt;
            for (int s = 0; s < _surfaceList.Count; s++)
            {
                UpdateSurfaceDrift(_surfaceList[s], maxDrift, melt);
            }
        }
    }

    public RenderSnapshot CreateSnapshot(bool isPlayMode = false)
    {
        lock (_sync)
        {
            var flakes = new SnowflakeSnapshot[_flakes.Count];
            for (int i = 0; i < _flakes.Count; i++)
            {
                Snowflake f = _flakes[i];
                flakes[i] = new SnowflakeSnapshot(f.X, f.Y, f.Radius, f.Angle, f.Variant, f.Layer, f.Alpha);
            }

            var crumbles = new CrumbleSnapshot[_crumbles.Count];
            for (int i = 0; i < _crumbles.Count; i++)
            {
                CrumbleParticle c = _crumbles[i];
                float lifeRatio = Math.Clamp(c.Life / c.MaxLife, 0f, 1f);
                byte alpha = (byte)(c.Alpha * lifeRatio);
                crumbles[i] = new CrumbleSnapshot(c.X, c.Y, c.Radius, alpha);
            }

            var glints = new GlintSnapshot[_glints.Count];
            for (int i = 0; i < _glints.Count; i++)
            {
                CornerGlint g = _glints[i];
                float progress = 1f - Math.Clamp(g.Life / g.MaxLife, 0f, 1f);
                glints[i] = new GlintSnapshot(g.X, g.Y, g.MaxSize, progress);
            }

            var smoke = new SmokePuffSnapshot[_smoke.Count];
            for (int i = 0; i < _smoke.Count; i++)
            {
                SmokePuff sp = _smoke[i];
                float lifeRatio = Math.Clamp(sp.Life / sp.MaxLife, 0f, 1f);
                byte alpha = (byte)(155f * lifeRatio);
                smoke[i] = new SmokePuffSnapshot(sp.X, sp.Y, sp.Radius, alpha);
            }

            var snowballs = new SnowballSnapshot[_snowballs.Count];
            for (int i = 0; i < _snowballs.Count; i++)
            {
                Snowball b = _snowballs[i];
                float shadowY = b.Y + b.Radius * 0.88f;
                float shadowAlpha = 0f;
                if (b.State == SnowballState.Held &&
                    b.IsRollingOnSurface &&
                    _surfaces.TryGetValue(b.RollingSurfaceHwnd, out WindowSnowSurface? rollSurf))
                {
                    float localH = GetSurfaceHeightAt(rollSurf, b.X);
                    shadowY = rollSurf.TopY - localH * 0.50f;
                    shadowAlpha = 1.0f;
                }
                else if (b.State == SnowballState.Resting &&
                         b.StackedOnId == 0 &&
                         _surfaces.TryGetValue(b.RestingHwnd, out WindowSnowSurface? restSurf))
                {
                    float localH = GetSurfaceHeightAt(restSurf, b.X);
                    shadowY = restSurf.TopY - localH * 0.50f;
                    shadowAlpha = localH > 0.5f ? 0.75f : 0.45f;
                }

                snowballs[i] = new SnowballSnapshot(
                    b.X,
                    b.Y,
                    b.Radius,
                    b.Angle,
                    b.Seed,
                    b.State == SnowballState.Held,
                    shadowY,
                    shadowAlpha);
            }

            var snowmen = new SnowmanSnapshot[_snowmen.Count];
            for (int i = 0; i < _snowmen.Count; i++)
            {
                Snowman m = _snowmen[i];
                snowmen[i] = new SnowmanSnapshot(
                    m.X,
                    m.BaseY + m.HopRenderOffsetY,
                    m.TiltAngle,
                    m.Scale,
                    m.PopTimer,
                    m.BlinkTimer > 0f,
                    m.LookX,
                    m.LookY,
                    m.HatLift,
                    m.HatTilt,
                    m.LeftArmAngle,
                    m.RightArmAngle,
                    m.Outfit);
            }

            var frostTrail = new FrostParticleSnapshot[_frostParticles.Count];
            for (int i = 0; i < _frostParticles.Count; i++)
            {
                FrostParticle fp = _frostParticles[i];
                float lifeRatio = Math.Clamp(fp.Life / Math.Max(0.01f, fp.MaxLife), 0f, 1f);
                byte alpha = (byte)(220f * MathF.Pow(lifeRatio, 0.85f));
                frostTrail[i] = new FrostParticleSnapshot(fp.X, fp.Y, fp.Radius, fp.Angle, alpha, fp.IsStar);
            }

            SnowplowSnapshot? train = _train.Active
                ? new SnowplowSnapshot(
                    _train.PlowX,
                    _train.TopY,
                    _train.Direction,
                    _train.WheelAngle,
                    _train.ShowSantaWagon,
                    _train.ShowTreeWagon,
                    _time)
                : null;

            var drifts = new List<DriftSnapshot>(_surfaceList.Count);
            for (int i = 0; i < _surfaceList.Count; i++)
            {
                WindowSnowSurface s = _surfaceList[i];
                bool hasSnow = false;
                for (int b = 0; b < s.Heights.Length; b++)
                {
                    if (s.Heights[b] > 0.35f && s.BucketVisible[b])
                    {
                        hasSnow = true;
                        break;
                    }
                }

                if (!hasSnow)
                    continue;

                var heightsCopy = new float[s.Heights.Length];
                Array.Copy(s.Heights, heightsCopy, heightsCopy.Length);
                var visCopy = new bool[s.BucketVisible.Length];
                Array.Copy(s.BucketVisible, visCopy, visCopy.Length);

                drifts.Add(new DriftSnapshot(s.StartX, s.TopY, WindowSnowSurface.BucketSize, heightsCopy, visCopy));
            }

            return new RenderSnapshot(flakes, crumbles, glints, smoke, snowballs, snowmen, frostTrail, train, drifts, isPlayMode, _time, _garlandEnabled);
        }
    }

    private void AdjustFlakeCount(int targetCount)
    {
        while (_flakes.Count < targetCount)
        {
            Snowflake flake = CreateFlake(scatterVertically: _flakes.Count < targetCount);
            _flakes.Add(flake);
        }

        if (_flakes.Count > targetCount)
        {
            _flakes.RemoveRange(targetCount, _flakes.Count - targetCount);
        }
    }

    private Snowflake CreateFlake(bool scatterVertically)
    {
        var flake = new Snowflake();
        Rectangle mon = PickRandomMonitor();
        flake.X = mon.Left + _rng.NextSingle() * mon.Width;
        flake.Y = scatterVertically
            ? mon.Top + _rng.NextSingle() * mon.Height
            : mon.Top - _rng.NextSingle() * 30f;

        InitFlakeAppearance(flake);
        return flake;
    }

    private void RespawnFlakeAtTop(Snowflake flake)
    {
        Rectangle mon = PickRandomMonitor();
        flake.X = mon.Left + _rng.NextSingle() * mon.Width;
        flake.Y = mon.Top - _rng.NextSingle() * 24f;
        InitFlakeAppearance(flake);
    }

    private void InitFlakeAppearance(Snowflake flake)
    {
        float layerRoll = _rng.NextSingle();
        if (layerRoll < 0.50f)
        {
            // Layer 0: Distant pinprick snowflakes with soft glow (tiny star-like specks)
            flake.Layer = 0;
            flake.Depth = 0.12f + _rng.NextSingle() * 0.18f;
            flake.Radius = 0.65f + _rng.NextSingle() * 0.75f;
            flake.BaseVy = 20f + _rng.NextSingle() * 16f;
            flake.Alpha = (byte)(125 + _rng.Next(95));
            flake.SpinSpeed = 0f;
        }
        else if (layerRoll < 0.85f)
        {
            // Layer 1: Mid-ground delicate crystalline snowflakes
            flake.Layer = 1;
            flake.Depth = 0.42f + _rng.NextSingle() * 0.28f;
            flake.Radius = 1.75f + _rng.NextSingle() * 1.20f;
            flake.BaseVy = 44f + _rng.NextSingle() * 26f;
            flake.Alpha = (byte)(160 + _rng.Next(65));
            flake.SpinSpeed = _rng.NextSingle() < 0.55f
                ? (_rng.NextSingle() - 0.5f) * 115f
                : 0f;
        }
        else
        {
            // Layer 2: Compact foreground crystalline snowflakes
            flake.Layer = 2;
            flake.Depth = 0.76f + _rng.NextSingle() * 0.24f;
            flake.Radius = 2.85f + _rng.NextSingle() * 1.35f;
            flake.BaseVy = 70f + _rng.NextSingle() * 34f;
            flake.Alpha = (byte)(210 + _rng.Next(45));
            flake.SpinSpeed = _rng.NextSingle() < 0.65f
                ? (_rng.NextSingle() - 0.5f) * 145f
                : 0f;
        }

        flake.PushVx = 0f;
        flake.PushVy = 0f;
        flake.Phase = _rng.NextSingle() * MathF.Tau;
        flake.SwayAmp = 10f + _rng.NextSingle() * 24f * flake.Depth;
        flake.SwayFreq = 1.0f + _rng.NextSingle() * 1.9f;
        flake.TurbuScale = 0.65f + _rng.NextSingle() * 0.7f;
        flake.Angle = _rng.NextSingle() * 360f;
        flake.Variant = _rng.Next(5);
        flake.Seed = _rng.Next();
    }

    private Rectangle PickRandomMonitor()
    {
        if (_monitors.Length == 1)
            return _monitors[0];

        int totalWidth = 0;
        for (int i = 0; i < _monitors.Length; i++)
            totalWidth += _monitors[i].Width;

        int pick = _rng.Next(Math.Max(1, totalWidth));
        int acc = 0;
        for (int i = 0; i < _monitors.Length; i++)
        {
            acc += _monitors[i].Width;
            if (pick < acc)
                return _monitors[i];
        }

        return _monitors[0];
    }

    private static bool ShouldLandOn(Snowflake flake, WindowSnowSurface surface)
    {
        int hash = unchecked((flake.Seed * 397) ^ surface.Hwnd.GetHashCode());
        int roll = hash & 0xFF;
        return surface.IsTaskbar ? roll < 235 : roll < 170;
    }

    private static void DepositSnow(
        WindowSnowSurface surface,
        int bucket,
        float radius,
        float maxDrift,
        float accRate)
    {
        float amount = (3.4f + radius * 0.85f) * accRate;
        float[] h = surface.Heights;
        bool[] vis = surface.BucketVisible;

        ReadOnlySpan<int> offsets = stackalloc int[] { -2, -1, 0, 1, 2 };
        ReadOnlySpan<float> weights = stackalloc float[] { 0.11f, 0.22f, 0.34f, 0.22f, 0.11f };

        for (int k = 0; k < offsets.Length; k++)
        {
            int idx = bucket + offsets[k];
            if (idx >= 0 && idx < h.Length && vis[idx])
            {
                // Natural wind-sculpted dune profile + preserve cursor-pushed drifts above maxDrift.
                float localCap = GetLocalDriftCap(surface, idx, maxDrift);
                if (h[idx] < localCap)
                {
                    h[idx] = Math.Min(localCap, h[idx] + amount * weights[k]);
                }
            }
        }
    }

    private static float GetLocalDriftCap(WindowSnowSurface surface, int bucket, float maxDrift)
    {
        float wx = surface.StartX + bucket * WindowSnowSurface.BucketSize;
        float seed = (surface.Hwnd.ToInt64() & 0x3FF) * 0.031f;
        float wave =
            0.55f * MathF.Sin(wx * 0.021f + seed) +
            0.30f * MathF.Sin(wx * 0.052f - seed * 1.4f) +
            0.15f * MathF.Sin(wx * 0.115f + seed * 0.7f);
        return MathF.Max(3.0f, maxDrift * (0.86f + 0.22f * wave));
    }

    private void UpdateSurfaceDrift(WindowSnowSurface surface, float maxDrift, float melt)
    {
        float[] h = surface.Heights;
        bool[] vis = surface.BucketVisible;
        int len = h.Length;
        const float maxStepDiff = 0.85f;
        float pushCap = maxDrift * 1.85f;

        if (_smoothScratch.Length < len)
        {
            _smoothScratch = new float[len];
        }

        // 1. Slope relaxation so piles never form unnaturally steep cliffs.
        for (int i = 0; i < len - 1; i++)
        {
            if (!vis[i] || !vis[i + 1])
                continue;

            float diff = h[i] - h[i + 1];
            if (diff > maxStepDiff)
            {
                float transfer = (diff - maxStepDiff) * 0.28f;
                h[i] -= transfer;
                h[i + 1] = Math.Min(pushCap, h[i + 1] + transfer);
            }
            else if (diff < -maxStepDiff)
            {
                float transfer = (-diff - maxStepDiff) * 0.28f;
                h[i + 1] -= transfer;
                h[i] = Math.Min(pushCap, h[i] + transfer);
            }
        }

        for (int i = len - 2; i >= 0; i--)
        {
            if (!vis[i] || !vis[i + 1])
                continue;

            float diff = h[i] - h[i + 1];
            if (diff > maxStepDiff)
            {
                float transfer = (diff - maxStepDiff) * 0.28f;
                h[i] -= transfer;
                h[i + 1] = Math.Min(pushCap, h[i + 1] + transfer);
            }
            else if (diff < -maxStepDiff)
            {
                float transfer = (-diff - maxStepDiff) * 0.28f;
                h[i + 1] -= transfer;
                h[i] = Math.Min(pushCap, h[i] + transfer);
            }
        }

        // 2. Curvature-thresholded Laplacian smoothing so sharp triangular peaks round into smooth mounds.
        if (len >= 3)
        {
            Array.Copy(h, _smoothScratch, len);
            for (int i = 1; i < len - 1; i++)
            {
                if (!vis[i] || !vis[i - 1] || !vis[i + 1])
                    continue;

                float avg = 0.5f * (_smoothScratch[i - 1] + _smoothScratch[i + 1]);
                float lap = avg - _smoothScratch[i];
                if (MathF.Abs(lap) > 0.06f)
                {
                    h[i] = Math.Clamp(_smoothScratch[i] + lap * 0.28f, 0f, pushCap);
                }
            }
        }

        for (int i = 0; i < len; i++)
        {
            if (h[i] <= 0f)
                continue;

            if (melt > 0f)
            {
                h[i] = Math.Max(0f, h[i] - melt);
            }

            if (h[i] > pushCap)
            {
                h[i] = pushCap;
            }
        }
    }

    private void ApplyCursorSweep(
        float x0,
        float y0,
        float x1,
        float y1,
        float dt,
        float maxDrift)
    {
        float dx = x1 - x0;
        float dy = y1 - y0;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        float speed = dist / MathF.Max(0.004f, dt);

        const float brushRadius = 22f;
        const int brushBuckets = 7;
        float pushCap = maxDrift * 1.85f;

        int dir = MathF.Abs(dx) >= 0.9f ? (dx > 0f ? 1 : -1) : 0;
        int steps = Math.Clamp((int)MathF.Ceiling(dist / 6f), 1, 36);

        float segMinX = MathF.Min(x0, x1) - brushRadius;
        float segMaxX = MathF.Max(x0, x1) + brushRadius;
        float segMinY = MathF.Min(y0, y1) - 12f;
        float segMaxY = MathF.Max(y0, y1) + 12f;

        // Wide smooth bell-shaped distribution so swept snow forms rounded mounds instead of triangles.
        ReadOnlySpan<float> pushWeights = stackalloc float[]
        {
            0.05f, 0.09f, 0.14f, 0.17f, 0.17f, 0.15f, 0.11f, 0.07f, 0.03f, 0.02f
        };
        int sprayCountThisFrame = 0;

        for (int s = 0; s < _surfaceList.Count; s++)
        {
            WindowSnowSurface surface = _surfaceList[s];
            if (segMaxX < surface.StartX || segMinX > surface.EndX)
                continue;
            if (segMaxY < surface.TopY - pushCap - 8f || segMinY > surface.TopY + 12f)
                continue;

            float[] h = surface.Heights;
            bool[] vis = surface.BucketVisible;
            int len = h.Length;

            for (int step = 0; step < steps; step++)
            {
                float t = (step + 1) / (float)steps;
                float cx = x0 + dx * t;
                float cy = y0 + dy * t;

                int cb = (int)((cx - surface.StartX) / WindowSnowSurface.BucketSize);
                int bMin = Math.Max(0, cb - brushBuckets);
                int bMax = Math.Min(len - 1, cb + brushBuckets);

                for (int b = bMin; b <= bMax; b++)
                {
                    if (!vis[b] || h[b] <= 0.15f)
                        continue;

                    if (cy < surface.TopY - h[b] - 10f || cy > surface.TopY + 10f)
                        continue;

                    float bx = surface.StartX + b * WindowSnowSurface.BucketSize + WindowSnowSurface.BucketSize * 0.5f;
                    float normDist = MathF.Abs(bx - cx) / brushRadius;
                    if (normDist >= 1f)
                        continue;

                    // Smooth cosine bell falloff (rounded brush profile).
                    float falloff = 0.5f * (1f + MathF.Cos(MathF.PI * normDist));
                    float scoopFraction = Math.Clamp(0.48f * falloff, 0f, 0.72f);
                    float taken = h[b] * scoopFraction;
                    if (taken <= 0.04f)
                        continue;

                    h[b] -= taken;

                    if (dir != 0)
                    {
                        int aheadBase = cb + dir * (brushBuckets - 1);
                        for (int k = 0; k < pushWeights.Length; k++)
                        {
                            int target = aheadBase + dir * k;
                            float share = taken * pushWeights[k];

                            if (target >= 0 && target < len && vis[target])
                            {
                                float room = pushCap - h[target];
                                if (room >= share)
                                {
                                    h[target] += share;
                                }
                                else if (room > 0f)
                                {
                                    h[target] = pushCap;
                                    int nextTarget = target + dir * 2;
                                    if (nextTarget >= 0 && nextTarget < len && vis[nextTarget])
                                    {
                                        h[nextTarget] = Math.Min(pushCap, h[nextTarget] + (share - room));
                                    }
                                }
                            }
                            else if (sprayCountThisFrame < 14 && share > 0.25f)
                            {
                                float edgeX = target < 0
                                    ? surface.StartX
                                    : (target >= len ? surface.EndX : bx);
                                SpawnSweepSpray(edgeX, surface.TopY - 2f, dir * (55f + speed * 0.18f), -(25f + _rng.NextSingle() * 45f));
                                sprayCountThisFrame++;
                            }
                        }
                    }
                    else
                    {
                        // Vertical poke pushes snow smoothly outward into rounded ridges on both sides.
                        for (int k = 0; k < 5; k++)
                        {
                            float w = taken * 0.10f;
                            int lt = cb - (brushBuckets + k);
                            int rt = cb + (brushBuckets + k);
                            if (lt >= 0 && lt < len && vis[lt])
                                h[lt] = Math.Min(pushCap, h[lt] + w);
                            if (rt >= 0 && rt < len && vis[rt])
                                h[rt] = Math.Min(pushCap, h[rt] + w);
                        }
                    }

                    if (sprayCountThisFrame < 12 && speed > 140f && taken > 0.5f && _rng.NextSingle() < 0.30f)
                    {
                        float sprayVx = (dx / MathF.Max(1f, dist)) * MathF.Min(190f, speed * 0.28f) + (_rng.NextSingle() - 0.5f) * 35f;
                        float sprayVy = -(28f + _rng.NextSingle() * 58f);
                        SpawnSweepSpray(bx, surface.TopY - h[b] * 0.6f, sprayVx, sprayVy);
                        sprayCountThisFrame++;
                    }
                }
            }
        }
    }

    private void UpdateSnowplow(float dt, SnowSettings settings)
    {
        if (!_train.Active)
        {
            if (!settings.SnowplowEnabled)
                return;

            _snowplowCooldown -= dt;
            if (_snowplowCooldown <= 0f)
            {
                if (!LaunchSnowplow(requireExistingSnow: true))
                {
                    _snowplowCooldown = 8f;
                }
                else
                {
                    float baseInt = (float)settings.SnowplowIntervalSeconds;
                    _snowplowCooldown = baseInt * (0.75f + _rng.NextSingle() * 0.5f);
                }
            }
            return;
        }

        WindowSnowSurface? taskbar = null;
        for (int i = 0; i < _surfaceList.Count; i++)
        {
            if (_surfaceList[i].Hwnd == _train.TargetHwnd)
            {
                taskbar = _surfaceList[i];
                break;
            }
        }

        if (taskbar is null)
        {
            _train.Active = false;
            return;
        }

        _train.ShowSantaWagon = settings.TrainSantaWagon;
        _train.ShowTreeWagon = settings.TrainTreeWagon;
        float trainLength = _train.CurrentTotalLength;

        _train.TopY = taskbar.TopY;
        float prevPlowX = _train.PlowX;
        _train.PlowX += _train.Direction * _train.Speed * dt;
        _train.WheelAngle = (_train.WheelAngle + _train.Direction * (_train.Speed * dt / 7.5f) * (180f / MathF.PI)) % 360f;

        // Clear all snow swept by the plow blade and throw a joyful snow spray forward!
        float minSweepX = MathF.Min(prevPlowX, _train.PlowX) - 6f;
        float maxSweepX = MathF.Max(prevPlowX, _train.PlowX) + 6f;
        if (_train.Direction > 0)
        {
            minSweepX = MathF.Min(minSweepX, _train.PlowX - trainLength);
            maxSweepX += 4f;
        }
        else
        {
            minSweepX -= 4f;
            maxSweepX = MathF.Max(maxSweepX, _train.PlowX + trainLength);
        }

        int bStart = Math.Clamp((int)((minSweepX - taskbar.StartX) / WindowSnowSurface.BucketSize), 0, taskbar.Heights.Length - 1);
        int bEnd = Math.Clamp((int)((maxSweepX - taskbar.StartX) / WindowSnowSurface.BucketSize), 0, taskbar.Heights.Length - 1);

        int sprayBudget = 16;
        for (int b = bStart; b <= bEnd; b++)
        {
            float h = taskbar.Heights[b];
            if (h <= 0.1f)
                continue;

            float bx = taskbar.StartX + b * WindowSnowSurface.BucketSize + WindowSnowSurface.BucketSize * 0.5f;
            bool atBlade = MathF.Abs(bx - _train.PlowX) <= 18f;

            if (atBlade && sprayBudget > 0 && h > 1.2f && _rng.NextSingle() < 0.75f)
            {
                int burst = h > 10f ? 2 : 1;
                for (int k = 0; k < burst && _crumbles.Count < MaxCrumbleParticles; k++)
                {
                    float maxLife = 0.5f + _rng.NextSingle() * 0.55f;
                    _crumbles.Add(new CrumbleParticle
                    {
                        X = _train.PlowX + _train.Direction * (_rng.NextSingle() * 5f),
                        Y = _train.TopY - _rng.NextSingle() * MathF.Min(h, 16f),
                        Vx = _train.Direction * (75f + _rng.NextSingle() * 135f),
                        Vy = -(55f + _rng.NextSingle() * 105f),
                        Radius = 1.4f + _rng.NextSingle() * 2.0f,
                        Life = maxLife,
                        MaxLife = maxLife,
                        Alpha = (byte)(215 + _rng.Next(40))
                    });
                }
                sprayBudget--;
            }

            taskbar.Heights[b] = 0f;
        }

        // Puff white steam/smoke from the chimney.
        _train.SmokeTimer -= dt;
        if (_train.SmokeTimer <= 0f)
        {
            _train.SmokeTimer = 0.095f + _rng.NextSingle() * 0.045f;
            if (_smoke.Count < 80)
            {
                float chimneyX = _train.PlowX - _train.Direction * 25.5f;
                float chimneyY = _train.TopY - 47f;
                float maxLife = 0.9f + _rng.NextSingle() * 0.6f;
                _smoke.Add(new SmokePuff
                {
                    X = chimneyX + (_rng.NextSingle() - 0.5f) * 3f,
                    Y = chimneyY,
                    Vx = -_train.Direction * (18f + _rng.NextSingle() * 18f),
                    Vy = -(26f + _rng.NextSingle() * 18f),
                    Radius = 3.2f + _rng.NextSingle() * 2.2f,
                    Life = maxLife,
                    MaxLife = maxLife
                });
            }
        }

        // Exit once the tail of the train has completely left the taskbar.
        bool exited = _train.Direction > 0
            ? (_train.PlowX - trainLength > taskbar.EndX + 24f)
            : (_train.PlowX + trainLength < taskbar.StartX - 24f);

        if (exited)
        {
            _train.Active = false;
            float baseInt = (float)settings.SnowplowIntervalSeconds;
            _snowplowCooldown = baseInt * (0.75f + _rng.NextSingle() * 0.5f);
        }
    }

    private bool LaunchSnowplow(bool requireExistingSnow)
    {
        if (_train.Active)
            return true;

        WindowSnowSurface? bestTaskbar = null;
        float bestSnowScore = -1f;

        for (int i = 0; i < _surfaceList.Count; i++)
        {
            WindowSnowSurface s = _surfaceList[i];
            if (!s.IsTaskbar)
                continue;

            float sum = 0f;
            for (int b = 0; b < s.Heights.Length; b++)
            {
                if (s.BucketVisible[b])
                    sum += s.Heights[b];
            }

            if (sum > bestSnowScore)
            {
                bestSnowScore = sum;
                bestTaskbar = s;
            }
        }

        if (bestTaskbar is null)
            return false;

        if (requireExistingSnow && bestSnowScore < 20f)
            return false;

        int dir = _rng.Next(2) == 0 ? 1 : -1;
        _train.Active = true;
        _train.TargetHwnd = bestTaskbar.Hwnd;
        _train.Direction = dir;
        _train.TopY = bestTaskbar.TopY;
        _train.Speed = 175f + _rng.NextSingle() * 35f;
        _train.PlowX = dir > 0
            ? bestTaskbar.StartX - 18f
            : bestTaskbar.EndX + 18f;
        _train.WheelAngle = 0f;
        _train.SmokeTimer = 0f;
        _pendingTrainWhistle = true;
        return true;
    }

    private void UpdateSmokePuffs(float dt, float windX)
    {
        for (int i = _smoke.Count - 1; i >= 0; i--)
        {
            SmokePuff p = _smoke[i];
            p.X += (p.Vx + windX * 0.35f) * dt;
            p.Y += p.Vy * dt;
            p.Radius += 6.5f * dt;
            p.Life -= dt;

            if (p.Life <= 0f)
            {
                _smoke.RemoveAt(i);
            }
        }
    }

    private void SpawnSweepSpray(float x, float y, float vx, float vy)
    {
        if (_crumbles.Count >= MaxCrumbleParticles)
            return;

        float maxLife = 0.45f + _rng.NextSingle() * 0.45f;
        _crumbles.Add(new CrumbleParticle
        {
            X = x,
            Y = y,
            Vx = vx,
            Vy = vy,
            Radius = 1.1f + _rng.NextSingle() * 1.5f,
            Life = maxLife,
            MaxLife = maxLife,
            Alpha = (byte)(200 + _rng.Next(55))
        });
    }

    private void SpawnCornerSplash(float x, float y, bool isLeftCorner)
    {
        if (_glints.Count < MaxGlints)
        {
            float maxLife = 0.34f + _rng.NextSingle() * 0.16f;
            _glints.Add(new CornerGlint
            {
                X = x,
                Y = y - 1f,
                MaxSize = 7f + _rng.NextSingle() * 5f,
                Life = maxLife,
                MaxLife = maxLife
            });
        }

        float dir = isLeftCorner ? -1f : 1f;
        int count = 5 + _rng.Next(3);
        for (int k = 0; k < count && _crumbles.Count < MaxCrumbleParticles; k++)
        {
            float maxLife = 0.55f + _rng.NextSingle() * 0.55f;
            _crumbles.Add(new CrumbleParticle
            {
                X = x + (_rng.NextSingle() - 0.5f) * 3f,
                Y = y - 1f,
                Vx = dir * (28f + _rng.NextSingle() * 78f) + (_rng.NextSingle() - 0.5f) * 18f,
                Vy = -(38f + _rng.NextSingle() * 68f),
                Radius = 1.0f + _rng.NextSingle() * 1.4f,
                Life = maxLife,
                MaxLife = maxLife,
                Alpha = (byte)(210 + _rng.Next(45))
            });
        }
    }

    private void ReleaseAllSnow(WindowSnowSurface surface)
    {
        float[] h = surface.Heights;
        for (int i = 0; i < h.Length; i++)
        {
            if (h[i] > 0.6f)
            {
                if (_crumbles.Count < MaxCrumbleParticles)
                {
                    SpawnCrumbleForBucket(surface, i, h[i]);
                }
                h[i] = 0f;
            }
        }
    }

    private void SpawnCrumbleForBucket(WindowSnowSurface surface, int bucketIndex, float height)
    {
        int count = Math.Clamp((int)MathF.Ceiling(height / 4.5f), 1, 4);
        float baseX = surface.StartX + bucketIndex * WindowSnowSurface.BucketSize + WindowSnowSurface.BucketSize * 0.5f;

        for (int k = 0; k < count && _crumbles.Count < MaxCrumbleParticles; k++)
        {
            float maxLife = 1.1f + _rng.NextSingle() * 1.4f;
            _crumbles.Add(new CrumbleParticle
            {
                X = baseX + (_rng.NextSingle() - 0.5f) * 6f,
                Y = surface.TopY - _rng.NextSingle() * height,
                Vx = (_rng.NextSingle() - 0.5f) * 85f,
                Vy = 15f + _rng.NextSingle() * 65f,
                Radius = 1.3f + _rng.NextSingle() * 1.9f,
                Life = maxLife,
                MaxLife = maxLife,
                Alpha = (byte)(185 + _rng.Next(70))
            });
        }
    }

    private void PushPointerSample(float x, float y)
    {
        _pointerHistory[_pointerHistHead] = (x, y, _time);
        _pointerHistHead = (_pointerHistHead + 1) % _pointerHistory.Length;
        if (_pointerHistCount < _pointerHistory.Length)
            _pointerHistCount++;
    }

    private (float Vx, float Vy) ComputeReleaseVelocity()
    {
        if (_pointerHistCount < 2)
            return (0f, 0f);

        int newestIdx = (_pointerHistHead - 1 + _pointerHistory.Length) % _pointerHistory.Length;
        (float nx, float ny, float nt) = _pointerHistory[newestIdx];

        // Pick a sample ~40-90ms old for a smooth, responsive throw vector.
        int bestIdx = (_pointerHistHead - 2 + _pointerHistory.Length) % _pointerHistory.Length;
        for (int k = 2; k <= _pointerHistCount; k++)
        {
            int idx = (_pointerHistHead - k + _pointerHistory.Length) % _pointerHistory.Length;
            float age = nt - _pointerHistory[idx].Time;
            bestIdx = idx;
            if (age >= 0.055f)
                break;
        }

        (float ox, float oy, float ot) = _pointerHistory[bestIdx];
        float dt = nt - ot;
        if (dt < 0.008f)
            return (0f, 0f);

        return ((nx - ox) / dt, (ny - oy) / dt);
    }

    private static bool HasSnowNearX(WindowSnowSurface s, float x, int radiusBuckets)
    {
        int cb = Math.Clamp((int)((x - s.StartX) / WindowSnowSurface.BucketSize), 0, s.Heights.Length - 1);
        int bMin = Math.Max(0, cb - radiusBuckets);
        int bMax = Math.Min(s.Heights.Length - 1, cb + radiusBuckets);
        for (int b = bMin; b <= bMax; b++)
        {
            if (s.BucketVisible[b] && s.Heights[b] > 0.45f)
                return true;
        }
        return false;
    }

    private static float GetSurfaceHeightAt(WindowSnowSurface s, float x)
    {
        if (s.Heights.Length == 0)
            return 0f;
        int b = Math.Clamp((int)((x - s.StartX) / WindowSnowSurface.BucketSize), 0, s.Heights.Length - 1);
        return s.Heights[b];
    }

    private static float ComputeSnowSurfaceCenterY(float topY, float localH, float radius)
    {
        // Nestles the rolling/resting snowball into the upper-middle layer of the snowdrift
        // (between window frame topY and drift crest topY - localH).
        float contactDepth = localH > 0.4f ? localH * 0.56f : 0f;
        return topY - contactDepth - radius * 0.84f;
    }

    private void TryScoopNewSnowballAt(float x, float y)
    {
        for (int i = 0; i < _surfaceList.Count; i++)
        {
            WindowSnowSurface s = _surfaceList[i];
            if (x < s.StartX - 10 || x > s.EndX + 10)
                continue;
            if (y < s.TopY - 58 || y > s.TopY + 24)
                continue;

            float snappedX = Math.Clamp(x, s.StartX + 4f, Math.Max(s.StartX + 4f, s.EndX - 4f));
            float localH = GetSurfaceHeightAt(s, snappedX);
            if (localH > 0.6f)
            {
                if (_snowballs.Count >= 48)
                {
                    _snowballs.RemoveAt(0);
                }

                const float startR = 4.2f;
                float centerY = ComputeSnowSurfaceCenterY(s.TopY, localH, startR);
                var ball = new Snowball
                {
                    Id = _nextBallId++,
                    X = snappedX,
                    Y = centerY,
                    TargetX = snappedX,
                    TargetY = centerY,
                    Radius = startR,
                    TargetRadius = 4.8f,
                    Angle = _rng.NextSingle() * 360f,
                    State = SnowballState.Held,
                    IsRollingOnSurface = true,
                    RollingSurfaceHwnd = s.Hwnd,
                    IgnoreCollisionHwnd = s.Hwnd,
                    Seed = _rng.Next()
                };
                _snowballs.Add(ball);
                _heldBall = ball;
                return;
            }
        }
    }

    private static void RollGrowBallOnSurface(Snowball ball, WindowSnowSurface s, float prevX, float nextX, float maxDrift)
    {
        float rollDist = MathF.Abs(nextX - prevX);
        if (rollDist <= 0.1f || s.Heights.Length == 0)
            return;

        float segMin = MathF.Min(prevX, nextX);
        float segMax = MathF.Max(prevX, nextX);
        int bMin = Math.Clamp((int)((segMin - s.StartX) / WindowSnowSurface.BucketSize), 0, s.Heights.Length - 1);
        int bMax = Math.Clamp((int)((segMax - s.StartX) / WindowSnowSurface.BucketSize), 0, s.Heights.Length - 1);

        float sumH = 0f;
        int visibleCount = 0;
        for (int b = bMin; b <= bMax; b++)
        {
            if (s.BucketVisible[b])
            {
                sumH += s.Heights[b];
                visibleCount++;
            }
        }

        if (visibleCount == 0)
            return;

        float avgH = sumH / visibleCount;
        if (avgH <= 0.6f)
            return;

        // Grow smoothly without erasing the snowdrift beneath the rolling path.
        float effectiveStep = MathF.Min(rollDist, 32f);
        float areaGain = effectiveStep * (0.38f + 0.050f * MathF.Min(avgH, maxDrift));
        float newArea = ball.TargetRadius * ball.TargetRadius + areaGain;
        ball.TargetRadius = MathF.Min(30f, MathF.Sqrt(newArea));
    }

    private bool TryFindRestingSurfaceAt(
        float x,
        float y,
        float radius,
        out WindowSnowSurface? found,
        out float topHeight)
    {
        for (int i = 0; i < _surfaceList.Count; i++)
        {
            WindowSnowSurface s = _surfaceList[i];
            if (x < s.StartX || x > s.EndX)
                continue;

            if (y >= s.TopY - radius - 34f && y <= s.TopY + 26f)
            {
                int b = Math.Clamp((int)((x - s.StartX) / WindowSnowSurface.BucketSize), 0, s.Heights.Length - 1);
                if (!s.BucketVisible[b])
                    continue;

                found = s;
                topHeight = s.Heights[b];
                return true;
            }
        }

        found = null;
        topHeight = 0f;
        return false;
    }

    private void UpdateSnowballs(float dt, float windX, float maxDrift, bool snowmanAnimations)
    {
        for (int i = _snowballs.Count - 1; i >= 0; i--)
        {
            Snowball ball = _snowballs[i];

            if (ball.State == SnowballState.Held)
            {
                if (ball.IsRollingOnSurface &&
                    ball.RollingSurfaceHwnd != IntPtr.Zero &&
                    _surfaces.TryGetValue(ball.RollingSurfaceHwnd, out WindowSnowSurface? rollSurf))
                {
                    float prevX = ball.X;
                    float moveLerp = 1f - MathF.Exp(-24f * dt);
                    float growLerp = 1f - MathF.Exp(-10f * dt);

                    ball.X += (ball.TargetX - ball.X) * moveLerp;
                    ball.Radius += (ball.TargetRadius - ball.Radius) * growLerp;

                    float localH = GetSurfaceHeightAt(rollSurf, ball.X);
                    float desiredY = ComputeSnowSurfaceCenterY(rollSurf.TopY, localH, ball.Radius);
                    ball.Y += (desiredY - ball.Y) * moveLerp;

                    float stepDx = ball.X - prevX;
                    if (MathF.Abs(stepDx) > 0.02f)
                    {
                        ball.Angle = (ball.Angle + stepDx * (180f / (MathF.PI * MathF.Max(4.5f, ball.Radius)))) % 360f;
                        if (localH > 0.6f && MathF.Abs(stepDx) > 0.35f && _rng.NextSingle() < 0.48f)
                        {
                            SpawnSweepSpray(
                                ball.X - MathF.Sign(stepDx) * ball.Radius * 0.55f,
                                rollSurf.TopY - localH * 0.52f,
                                -stepDx * 14f + (_rng.NextSingle() - 0.5f) * 32f,
                                -(20f + _rng.NextSingle() * 36f));
                        }
                    }
                }
                else
                {
                    float freeLerp = 1f - MathF.Exp(-28f * dt);
                    ball.X += (ball.TargetX - ball.X) * freeLerp;
                    ball.Y += (ball.TargetY - ball.Y) * freeLerp;
                }
                continue;
            }

            if (ball.State == SnowballState.Resting)
            {
                if (ball.StackedOnId != 0)
                {
                    Snowball? baseBall = FindSnowballById(ball.StackedOnId);
                    if (baseBall is null || baseBall.State != SnowballState.Resting)
                    {
                        ball.StackedOnId = 0;
                        ball.State = SnowballState.Flying;
                        ball.RestingHwnd = IntPtr.Zero;
                        ball.Vx = 0f;
                        ball.Vy = 25f;
                        ball.GlobalLaunchGrace = 0.05f;
                        continue;
                    }

                    if (_surfaces.TryGetValue(baseBall.RestingHwnd, out WindowSnowSurface? baseSurf))
                    {
                        baseBall.X = baseSurf.Bounds.Left + baseBall.RestingOffsetX;
                        baseBall.Y = baseSurf.TopY - baseBall.RestingYOffset;
                    }

                    ball.RestingHwnd = baseBall.RestingHwnd;
                    ball.X = baseBall.X;
                    ball.Y = baseBall.Y - (baseBall.Radius * 0.72f + ball.Radius * 0.72f);

                    if (_train.Active && ball.RestingHwnd == _train.TargetHwnd && MathF.Abs(ball.X - _train.PlowX) <= ball.Radius + 16f)
                    {
                        ShatterSnowball(ball, _train.Direction * 220f, -150f, IntPtr.Zero, _train.Direction, -0.4f, maxDrift);
                        _snowballs.RemoveAt(i);
                    }
                    continue;
                }

                if (_surfaces.TryGetValue(ball.RestingHwnd, out WindowSnowSurface? surf))
                {
                    ball.X = surf.Bounds.Left + ball.RestingOffsetX;
                    ball.Y = surf.TopY - ball.RestingYOffset;

                    // If snowplow drives through a resting snowball on the taskbar, smash it!
                    if (_train.Active && surf.Hwnd == _train.TargetHwnd && MathF.Abs(ball.X - _train.PlowX) <= ball.Radius + 16f)
                    {
                        ShatterSnowball(ball, _train.Direction * 220f, -150f, IntPtr.Zero, _train.Direction, -0.4f, maxDrift);
                        _snowballs.RemoveAt(i);
                    }
                }
                else
                {
                    // Window closed or minimized -> snowball drops!
                    ball.State = SnowballState.Flying;
                    ball.RestingHwnd = IntPtr.Zero;
                    ball.Vx = 0f;
                    ball.Vy = 25f;
                    ball.GlobalLaunchGrace = 0.05f;
                }
                continue;
            }

            // Flying ballistics
            if (ball.GlobalLaunchGrace > 0f)
                ball.GlobalLaunchGrace = MathF.Max(0f, ball.GlobalLaunchGrace - dt);
            if (ball.IgnoreHwndTimer > 0f)
                ball.IgnoreHwndTimer = MathF.Max(0f, ball.IgnoreHwndTimer - dt);

            if (ball.HasImpactTarget &&
                ball.TargetWindowHwnd != IntPtr.Zero &&
                !_colliderMap.ContainsKey(ball.TargetWindowHwnd))
            {
                // Window closed or minimized mid-flight -> fall naturally onto whatever is below.
                ball.HasImpactTarget = false;
            }

            ball.Vy += 680f * dt;
            if (ball.HasImpactTarget)
            {
                ball.Vx *= MathF.Max(0f, 1f - 0.30f * dt);
            }
            else
            {
                ball.Vx = (ball.Vx + windX * 0.25f * dt) * MathF.Max(0f, 1f - 1.35f * dt);
            }
            ball.Angle = (ball.Angle + ball.SpinSpeed * dt) % 360f;

            float stepX = ball.Vx * dt;
            float stepY = ball.Vy * dt;
            float stepDist = MathF.Sqrt(stepX * stepX + stepY * stepY);
            int substeps = Math.Clamp((int)MathF.Ceiling(stepDist / 10f), 1, 14);

            bool shattered = false;
            float startX = ball.X;
            float startY = ball.Y;
            float prevElapsed = ball.TargetFlightElapsed;
            if (ball.HasImpactTarget)
            {
                ball.TargetFlightElapsed += dt;
            }

            for (int step = 1; step <= substeps; step++)
            {
                float t = step / (float)substeps;
                float nx = startX + stepX * t;
                float ny = startY + stepY * t;

                if (ball.HasImpactTarget)
                {
                    float tdx = nx - ball.ImpactTargetX;
                    float tdy = ny - ball.ImpactTargetY;
                    float stepElapsed = prevElapsed + dt * t;
                    if (tdx * tdx + tdy * tdy <= 20f * 20f ||
                        (ball.TargetFlightDuration > 0f && stepElapsed >= ball.TargetFlightDuration))
                    {
                        ball.X = ball.ImpactTargetX;
                        ball.Y = ball.ImpactTargetY;
                        ShatterSnowball(ball, ball.Vx, ball.Vy, ball.TargetWindowHwnd, 0f, -0.35f, maxDrift);
                        shattered = true;
                        break;
                    }
                    continue;
                }

                if (ball.GlobalLaunchGrace <= 0f)
                {
                    // Check direct player snowball hits against standing/falling snowmen
                    if (ball.FromSnowmanId == 0)
                    {
                        for (int mIdx = _snowmen.Count - 1; mIdx >= 0; mIdx--)
                        {
                            Snowman m = _snowmen[mIdx];
                            if (m.State == SnowmanState.Held)
                                continue;

                            float s = m.Scale;
                            if (HitsSnowmanCircle(nx, ny, ball.Radius, m.X, m.BaseY - 17.5f * s, 21f * s) ||
                                HitsSnowmanCircle(nx, ny, ball.Radius, m.X, m.BaseY - 40.0f * s, 16f * s) ||
                                HitsSnowmanCircle(nx, ny, ball.Radius, m.X, m.BaseY - 62.0f * s, 13f * s))
                            {
                                ball.X = nx;
                                ball.Y = ny;
                                float normX = nx >= m.X ? 1f : -1f;
                                ShatterSnowball(ball, ball.Vx, ball.Vy, IntPtr.Zero, normX, -0.35f, maxDrift);
                                SpawnSnowmanBreak(m, ball.Vx, ball.Vy);
                                _snowmen.RemoveAt(mIdx);

                                shattered = true;
                                break;
                            }
                        }
                    }

                    if (shattered)
                        break;

                    for (int c = 0; c < _colliders.Count; c++)
                    {
                        WindowCollider col = _colliders[c];
                        if (ball.IgnoreHwndTimer > 0f && col.Hwnd == ball.IgnoreCollisionHwnd)
                            continue;

                        // Never shatter against the middle of maximized windows — let snowballs fly across to taskbar or floating frames.
                        if (col.IsMaximized)
                            continue;

                        Rectangle r = col.Bounds;
                        float pad = MathF.Min(ball.Radius * 0.50f, 11f);

                        if (col.IsTaskbar)
                        {
                            if (nx >= r.Left - pad && nx <= r.Right + pad &&
                                ny + pad >= r.Top && ny - pad <= r.Bottom)
                            {
                                ball.X = nx;
                                ball.Y = MathF.Min(ny, r.Top - 1f);
                                ShatterSnowball(ball, ball.Vx, ball.Vy, col.Hwnd, 0f, -1f, maxDrift);
                                shattered = true;
                                break;
                            }
                            continue;
                        }

                        // Skip frames hidden behind an upper non-maximized window in Z-order.
                        bool occludedByUpperWindow = false;
                        for (int p = 0; p < c; p++)
                        {
                            WindowCollider upper = _colliders[p];
                            if (!upper.IsMaximized && !upper.IsTaskbar && upper.Bounds.Contains((int)nx, (int)ny))
                            {
                                occludedByUpperWindow = true;
                                break;
                            }
                        }
                        if (occludedByUpperWindow)
                            continue;

                        // Collide ONLY with the 4 outer frame borders of open non-maximized windows.
                        float frameThickness = Math.Clamp(11f + ball.Radius * 0.30f, 11f, 20f);
                        if (nx >= r.Left - pad && nx <= r.Right + pad &&
                            ny >= r.Top - pad && ny <= r.Bottom + pad)
                        {
                            bool hitTop = ny <= r.Top + frameThickness;
                            bool hitBottom = ny >= r.Bottom - frameThickness;
                            bool hitLeft = nx <= r.Left + frameThickness;
                            bool hitRight = nx >= r.Right - frameThickness;

                            if (hitTop || hitBottom || hitLeft || hitRight)
                            {
                                float dTop = MathF.Abs(ny - r.Top);
                                float dBottom = MathF.Abs(ny - r.Bottom);
                                float dLeft = MathF.Abs(nx - r.Left);
                                float dRight = MathF.Abs(nx - r.Right);
                                float minD = MathF.Min(MathF.Min(dTop, dBottom), MathF.Min(dLeft, dRight));

                                float normX = 0f;
                                float normY = -1f;
                                if (minD == dBottom) { normX = 0f; normY = 1f; }
                                else if (minD == dLeft) { normX = -1f; normY = 0f; }
                                else if (minD == dRight) { normX = 1f; normY = 0f; }

                                ball.X = nx;
                                ball.Y = ny;
                                ShatterSnowball(ball, ball.Vx, ball.Vy, col.Hwnd, normX, normY, maxDrift);
                                shattered = true;
                                break;
                            }
                        }
                    }
                }

                if (shattered)
                    break;
            }

            if (shattered)
            {
                _snowballs.RemoveAt(i);
                continue;
            }

            ball.X = startX + stepX;
            ball.Y = startY + stepY;

            // Fallback shatter at screen bottom if taskbar is hidden on this monitor.
            if (ball.Y + ball.Radius * 0.45f >= _maxBottom)
            {
                ball.Y = _maxBottom - 2f;
                ShatterSnowball(ball, ball.Vx, ball.Vy, IntPtr.Zero, 0f, -1f, maxDrift);
                _snowballs.RemoveAt(i);
                continue;
            }

            if (ball.X < _minLeft - 240f || ball.X > _maxRight + 240f)
            {
                _snowballs.RemoveAt(i);
            }
        }
    }

    private void ShatterSnowball(
        Snowball ball,
        float impactVx,
        float impactVy,
        IntPtr hitHwnd,
        float normX,
        float normY,
        float maxDrift)
    {
        _pendingShatterCount++;

        // 1. Crisp crystalline sparkle glint at the impact point (matching corner-splash glints).
        if (_glints.Count < MaxGlints)
        {
            float glintLife = 0.34f + _rng.NextSingle() * 0.18f;
            _glints.Add(new CornerGlint
            {
                X = ball.X,
                Y = ball.Y,
                MaxSize = Math.Clamp(6.5f + ball.Radius * 0.48f, 7f, 19f),
                Life = glintLife,
                MaxLife = glintLife
            });
        }

        // 2. Burst into fine sparkling snow particles spraying outward along the hit border/taskbar normal.
        int particleCount = Math.Clamp((int)(ball.Radius * 2.6f), 12, 72);
        float impactSpeed = MathF.Sqrt(impactVx * impactVx + impactVy * impactVy);
        float baseSpreadSpeed = Math.Clamp(52f + impactSpeed * 0.22f + ball.Radius * 3.8f, 65f, 320f);
        float normalAngle = MathF.Atan2(normY, normX);

        for (int k = 0; k < particleCount && _crumbles.Count < MaxCrumbleParticles; k++)
        {
            float angle = (k & 3) == 0
                ? _rng.NextSingle() * MathF.Tau
                : normalAngle + (_rng.NextSingle() - 0.5f) * (MathF.PI * 1.15f);

            float mag = (0.28f + _rng.NextSingle() * 0.88f) * baseSpreadSpeed;
            float maxLife = 0.50f + _rng.NextSingle() * 0.75f;

            _crumbles.Add(new CrumbleParticle
            {
                X = ball.X + (_rng.NextSingle() - 0.5f) * MathF.Min(ball.Radius * 0.7f, 10f),
                Y = ball.Y + (_rng.NextSingle() - 0.5f) * MathF.Min(ball.Radius * 0.7f, 10f),
                Vx = MathF.Cos(angle) * mag + impactVx * 0.14f,
                Vy = MathF.Sin(angle) * mag - 32f,
                Radius = 0.85f + _rng.NextSingle() * 1.35f,
                Life = maxLife,
                MaxLife = maxLife,
                Alpha = (byte)(205 + _rng.Next(50))
            });
        }

        // 3. If hit along the top ledge of a tracked surface, deposit snow into that drift.
        if (hitHwnd != IntPtr.Zero && _surfaces.TryGetValue(hitHwnd, out WindowSnowSurface? hitSurf))
        {
            if (MathF.Abs(ball.Y - hitSurf.TopY) <= ball.Radius + 18f)
            {
                int cb = Math.Clamp((int)((ball.X - hitSurf.StartX) / WindowSnowSurface.BucketSize), 0, hitSurf.Heights.Length - 1);
                int spread = Math.Clamp((int)(ball.Radius / 2.5f) + 2, 3, 10);
                float pushCap = maxDrift * 1.85f;
                for (int b = Math.Max(0, cb - spread); b <= Math.Min(hitSurf.Heights.Length - 1, cb + spread); b++)
                {
                    if (!hitSurf.BucketVisible[b])
                        continue;
                    float falloff = 1f - MathF.Abs(b - cb) / (spread + 1f);
                    hitSurf.Heights[b] = Math.Min(pushCap, hitSurf.Heights[b] + ball.Radius * 0.36f * falloff);
                }
            }
        }
    }

    private Snowball? FindSnowballById(int id)
    {
        for (int i = 0; i < _snowballs.Count; i++)
        {
            if (_snowballs[i].Id == id)
                return _snowballs[i];
        }
        return null;
    }

    private bool TryStackOrBuildSnowman(Snowball ball)
    {
        Snowball? bestTarget = null;
        float bestY = float.MaxValue;

        for (int i = 0; i < _snowballs.Count; i++)
        {
            Snowball candidate = _snowballs[i];
            if (candidate == ball || candidate.State != SnowballState.Resting)
                continue;

            float maxHoriz = candidate.Radius + ball.Radius * 0.75f + 12f;
            if (MathF.Abs(ball.X - candidate.X) > maxHoriz)
                continue;

            if (ball.Y > candidate.Y + candidate.Radius * 0.70f ||
                ball.Y < candidate.Y - candidate.Radius - ball.Radius * 2.4f - 30f)
            {
                continue;
            }

            // Pick the topmost eligible ball in this column.
            if (candidate.Y < bestY)
            {
                bestY = candidate.Y;
                bestTarget = candidate;
            }
        }

        if (bestTarget is null)
            return false;

        if (bestTarget.StackedOnId == 0)
        {
            // Ensure no other ball is already sitting on bestTarget.
            for (int i = 0; i < _snowballs.Count; i++)
            {
                if (_snowballs[i] != ball && _snowballs[i].StackedOnId == bestTarget.Id)
                    return false;
            }

            ball.State = SnowballState.Resting;
            ball.StackedOnId = bestTarget.Id;
            ball.RestingHwnd = bestTarget.RestingHwnd;
            ball.X = bestTarget.X;
            ball.Y = bestTarget.Y - (bestTarget.Radius * 0.72f + ball.Radius * 0.72f);
            ball.RestingOffsetX = bestTarget.RestingOffsetX;
            ball.RestingYOffset = bestTarget.RestingYOffset + bestTarget.Radius * 0.72f + ball.Radius * 0.72f;
            ball.Vx = 0f;
            ball.Vy = 0f;
            ball.SpinSpeed = 0f;
            SpawnLandingDust(ball.X, ball.Y + ball.Radius * 0.6f, 0.7f);
            return true;
        }

        // Third snowball placed onto a 2-snowball stack -> transform into a full Snowman!
        Snowball? baseBall = FindSnowballById(bestTarget.StackedOnId);
        float avgR = ((baseBall?.Radius ?? bestTarget.Radius) + bestTarget.Radius + ball.Radius) / 3f;
        float baseScale = Math.Clamp(avgR / 13.0f, 0.85f, 1.55f);
        float scale = Math.Clamp(baseScale * _snowmanScaleMultiplier, 0.50f, 2.50f);

        IntPtr surfHwnd = baseBall?.RestingHwnd ?? bestTarget.RestingHwnd;
        float baseX = baseBall?.X ?? bestTarget.X;
        float baseY = (baseBall is not null ? baseBall.Y + baseBall.Radius * 0.75f : bestTarget.Y + bestTarget.Radius * 1.4f);
        float restingOffsetX = 0f;
        float restingYOffset = 0f;

        if (_surfaces.TryGetValue(surfHwnd, out WindowSnowSurface? surf))
        {
            float clampX = Math.Clamp(baseX, surf.StartX + 12f, Math.Max(surf.StartX + 12f, surf.EndX - 12f));
            float localH = GetSurfaceHeightAt(surf, clampX);
            baseX = clampX;
            restingOffsetX = clampX - surf.Bounds.Left;
            restingYOffset = localH * 0.25f;
            baseY = surf.TopY - restingYOffset;
        }

        _snowballs.Remove(ball);
        _snowballs.Remove(bestTarget);
        if (baseBall is not null)
        {
            _snowballs.Remove(baseBall);
        }

        if (_snowmen.Count >= 16)
        {
            _snowmen.RemoveAt(0);
        }

        var snowman = new Snowman
        {
            Id = _nextSnowmanId++,
            X = baseX,
            BaseY = baseY,
            BaseScale = baseScale,
            Scale = scale,
            PopTimer = 0.45f,
            State = _surfaces.ContainsKey(surfHwnd) ? SnowmanState.Resting : SnowmanState.Falling,
            RestingHwnd = _surfaces.ContainsKey(surfHwnd) ? surfHwnd : IntPtr.Zero,
            RestingOffsetX = restingOffsetX,
            RestingYOffset = restingYOffset,
            Outfit = NextOutfit()
        };
        _snowmen.Add(snowman);
        SpawnSnowmanMagic(snowman.X, snowman.BaseY - 42f * snowman.Scale, snowman.Scale);
        return true;
    }

    private Snowman? FindSnowmanAt(float x, float y)
    {
        for (int i = _snowmen.Count - 1; i >= 0; i--)
        {
            Snowman m = _snowmen[i];
            if (m.State == SnowmanState.Held)
                continue;

            float halfW = 26f * m.Scale + 12f;
            float topY = m.BaseY - 94f * m.Scale - 12f;
            if (x >= m.X - halfW && x <= m.X + halfW && y >= topY && y <= m.BaseY + 14f)
                return m;
        }
        return null;
    }

    private bool TryFindSnowmanSurfaceAt(float x, float baseY, out WindowSnowSurface? found)
    {
        for (int i = 0; i < _surfaceList.Count; i++)
        {
            WindowSnowSurface s = _surfaceList[i];
            if (x < s.StartX - 10f || x > s.EndX + 10f)
                continue;

            if (baseY >= s.TopY - 38f && baseY <= s.TopY + 34f &&
                IsSurfaceVisibleNear(s, Math.Clamp(x, s.StartX, s.EndX)))
            {
                found = s;
                return true;
            }
        }

        found = null;
        return false;
    }

    private static bool IsSurfaceVisibleNear(WindowSnowSurface s, float x)
    {
        if (s.BucketVisible.Length == 0)
            return false;
        int cb = Math.Clamp((int)((x - s.StartX) / WindowSnowSurface.BucketSize), 0, s.BucketVisible.Length - 1);
        int bMin = Math.Max(0, cb - 3);
        int bMax = Math.Min(s.BucketVisible.Length - 1, cb + 3);
        for (int b = bMin; b <= bMax; b++)
        {
            if (s.BucketVisible[b])
                return true;
        }
        return false;
    }

    private float PickFreeTaskbarSnowmanX(float minX, float maxX, WindowSnowSurface taskbarSurf)
    {
        float span = Math.Max(1f, maxX - minX);
        float bestX = minX + span * (0.25f + _rng.NextSingle() * 0.5f);
        float bestClearance = -1f;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float candidate = minX + span * (0.12f + _rng.NextSingle() * 0.76f);
            if (!IsSurfaceVisibleNear(taskbarSurf, candidate))
                continue;

            float nearest = float.MaxValue;
            for (int i = 0; i < _snowmen.Count; i++)
            {
                nearest = MathF.Min(nearest, MathF.Abs(_snowmen[i].X - candidate));
            }

            if (nearest > bestClearance)
            {
                bestClearance = nearest;
                bestX = candidate;
            }
        }

        return bestX;
    }

    private void SpawnSnowmanMagic(float cx, float cy, float scale)
    {
        for (int i = 0; i < 4 && _glints.Count < MaxGlints; i++)
        {
            float maxLife = 0.42f + _rng.NextSingle() * 0.25f;
            _glints.Add(new CornerGlint
            {
                X = cx + (_rng.NextSingle() - 0.5f) * 38f * scale,
                Y = cy + (_rng.NextSingle() - 0.5f) * 48f * scale,
                MaxSize = 8f + _rng.NextSingle() * 7f,
                Life = maxLife,
                MaxLife = maxLife
            });
        }

        for (int k = 0; k < 22 && _crumbles.Count < MaxCrumbleParticles; k++)
        {
            float angle = _rng.NextSingle() * MathF.Tau;
            float spd = 35f + _rng.NextSingle() * 95f;
            float maxLife = 0.45f + _rng.NextSingle() * 0.55f;
            _crumbles.Add(new CrumbleParticle
            {
                X = cx + (_rng.NextSingle() - 0.5f) * 20f * scale,
                Y = cy + (_rng.NextSingle() - 0.5f) * 32f * scale,
                Vx = MathF.Cos(angle) * spd,
                Vy = MathF.Sin(angle) * spd - 35f,
                Radius = 0.9f + _rng.NextSingle() * 1.4f,
                Life = maxLife,
                MaxLife = maxLife,
                Alpha = (byte)(215 + _rng.Next(40))
            });
        }
    }

    private static bool HitsSnowmanCircle(float bx, float by, float ballR, float cx, float cy, float targetR)
    {
        float dx = bx - cx;
        float dy = by - cy;
        float rSum = targetR + ballR * 0.65f;
        return dx * dx + dy * dy <= rSum * rSum;
    }

    private void SpawnSnowmanBreak(Snowman m, float impactVx, float impactVy)
    {
        float s = m.Scale;
        for (int i = 0; i < 4 && _glints.Count < MaxGlints; i++)
        {
            float maxLife = 0.36f + _rng.NextSingle() * 0.22f;
            _glints.Add(new CornerGlint
            {
                X = m.X + (_rng.NextSingle() - 0.5f) * 34f * s,
                Y = m.BaseY - (14f + _rng.NextSingle() * 56f) * s,
                MaxSize = 8f + _rng.NextSingle() * 7f,
                Life = maxLife,
                MaxLife = maxLife
            });
        }

        // Crumble the three snow tiers cleanly into scattered snow particles.
        ReadOnlySpan<(float OffsetY, float Radius)> tiers = stackalloc (float, float)[]
        {
            (-17.5f, 19.0f),
            (-40.0f, 14.0f),
            (-60.5f, 10.5f)
        };

        for (int t = 0; t < tiers.Length; t++)
        {
            float cy = m.BaseY + tiers[t].OffsetY * s;
            float tierR = tiers[t].Radius * s;
            int count = 24;
            for (int k = 0; k < count && _crumbles.Count < MaxCrumbleParticles; k++)
            {
                float angle = _rng.NextSingle() * MathF.Tau;
                float dist = MathF.Sqrt(_rng.NextSingle()) * tierR;
                float spd = 55f + _rng.NextSingle() * 145f;
                float maxLife = 0.65f + _rng.NextSingle() * 0.85f;
                _crumbles.Add(new CrumbleParticle
                {
                    X = m.X + MathF.Cos(angle) * dist,
                    Y = cy + MathF.Sin(angle) * dist,
                    Vx = MathF.Cos(angle) * spd + impactVx * 0.16f,
                    Vy = MathF.Sin(angle) * spd + impactVy * 0.10f - 45f,
                    Radius = 0.95f + _rng.NextSingle() * 1.55f,
                    Life = maxLife,
                    MaxLife = maxLife,
                    Alpha = (byte)(210 + _rng.Next(45))
                });
            }
        }
    }

    private void SpawnLandingDust(float x, float baseY, float scale)
    {
        for (int k = 0; k < 7 && _crumbles.Count < MaxCrumbleParticles; k++)
        {
            float side = k % 2 == 0 ? -1f : 1f;
            SpawnSweepSpray(
                x + side * (6f + _rng.NextSingle() * 14f) * scale,
                baseY - 2f,
                side * (24f + _rng.NextSingle() * 48f),
                -(16f + _rng.NextSingle() * 34f));
        }
    }

    private void LaunchPlayfulSnowmanSnowball(Snowman thrower)
    {
        if (_snowballs.Count >= 48)
        {
            _snowballs.RemoveAt(0);
        }

        bool hasTarget = thrower.HasPendingThrowTarget;
        IntPtr targetHwnd = thrower.PendingThrowHwnd;
        float targetX = thrower.PendingThrowX;
        float targetY = thrower.PendingThrowY;
        thrower.HasPendingThrowTarget = false;
        thrower.PendingThrowHwnd = IntPtr.Zero;

        float launchX = thrower.X + thrower.ThrowAimDir * 14f * thrower.Scale;
        float launchY = thrower.BaseY - 48f * thrower.Scale;

        float vx;
        float vy;
        float flightDuration = 0f;

        if (hasTarget)
        {
            float dx = targetX - launchX;
            float dy = targetY - launchY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            float flightT = Math.Clamp(dist / 520f, 0.48f, 1.40f);
            float effT = (1f - MathF.Exp(-0.30f * flightT)) / 0.30f;
            vx = dx / MathF.Max(0.25f, effT);
            vy = dy / flightT - 0.5f * 680f * flightT;

            if (MathF.Sqrt(vx * vx + vy * vy) > 1050f && dist > 10f)
            {
                flightT = Math.Clamp(dist / 420f, flightT, 1.85f);
                effT = (1f - MathF.Exp(-0.30f * flightT)) / 0.30f;
                vx = dx / MathF.Max(0.25f, effT);
                vy = dy / flightT - 0.5f * 680f * flightT;
            }

            flightDuration = flightT;
            thrower.ThrowAimDir = dx >= 0f ? 1 : -1;
        }
        else
        {
            int dir = thrower.ThrowAimDir;
            vx = dir * (165f + _rng.NextSingle() * 205f);
            vy = -(350f + _rng.NextSingle() * 210f);
        }

        _snowballs.Add(new Snowball
        {
            Id = _nextBallId++,
            FromSnowmanId = thrower.Id,
            X = launchX,
            Y = launchY,
            Vx = vx,
            Vy = vy,
            Radius = Math.Clamp(5.6f * thrower.Scale, 5.0f, 8.5f),
            Angle = _rng.NextSingle() * 360f,
            SpinSpeed = Math.Clamp(vx * 0.35f, -240f, 240f),
            State = SnowballState.Flying,
            IgnoreCollisionHwnd = thrower.RestingHwnd,
            IgnoreHwndTimer = 0.25f,
            GlobalLaunchGrace = 0.05f,
            HasImpactTarget = hasTarget,
            TargetWindowHwnd = targetHwnd,
            ImpactTargetX = targetX,
            ImpactTargetY = targetY,
            TargetFlightElapsed = 0f,
            TargetFlightDuration = flightDuration,
            Seed = _rng.Next()
        });
    }

    private void UpdateSnowmen(float dt, SnowSettings settings, float cursorX, float cursorY, bool hasCursor)
    {
        bool animEnabled = settings.SnowmanAnimations;

        for (int i = 0; i < _snowmen.Count; i++)
        {
            Snowman m = _snowmen[i];
            m.HopRenderOffsetY = 0f;

            float targetScale = Math.Clamp(m.BaseScale * _snowmanScaleMultiplier, 0.50f, 2.50f);
            m.Scale += (targetScale - m.Scale) * MathF.Min(1f, 14f * dt);

            if (m.PopTimer > 0f)
            {
                m.PopTimer = MathF.Max(0f, m.PopTimer - dt);
            }
            if (m.TrainWaveCooldown > 0f)
            {
                m.TrainWaveCooldown = MathF.Max(0f, m.TrainWaveCooldown - dt);
            }

            // Ambient blinking & eye tracking toward nearby cursor
            if (animEnabled)
            {
                if (m.BlinkTimer > 0f)
                {
                    m.BlinkTimer = MathF.Max(0f, m.BlinkTimer - dt);
                }
                else
                {
                    m.NextBlinkCooldown -= dt;
                    if (m.NextBlinkCooldown <= 0f)
                    {
                        m.BlinkTimer = 0.16f;
                        m.NextBlinkCooldown = 2.8f + _rng.NextSingle() * 4.4f;
                    }
                }

                float targetLookX = 0f;
                float targetLookY = 0f;
                if (hasCursor && m.State != SnowmanState.Held)
                {
                    float faceX = m.X;
                    float faceY = m.BaseY - 60f * m.Scale;
                    float dx = cursorX - faceX;
                    float dy = cursorY - faceY;
                    float distSq = dx * dx + dy * dy;
                    if (distSq < 280f * 280f)
                    {
                        targetLookX = Math.Clamp(dx / 110f, -1f, 1f);
                        targetLookY = Math.Clamp(dy / 110f, -1f, 1f);
                    }
                }

                float lookLerp = MathF.Min(1f, 8f * dt);
                m.LookX += (targetLookX - m.LookX) * lookLerp;
                m.LookY += (targetLookY - m.LookY) * lookLerp;
            }
            else
            {
                m.BlinkTimer = 0f;
                m.LookX *= MathF.Max(0f, 1f - 10f * dt);
                m.LookY *= MathF.Max(0f, 1f - 10f * dt);
                m.Action = SnowmanAction.Idle;
                m.HasPendingThrowTarget = false;
            }

            if (m.State == SnowmanState.Held)
            {
                m.Action = SnowmanAction.Idle;
                m.HatLift *= MathF.Max(0f, 1f - 12f * dt);
                m.HatTilt *= MathF.Max(0f, 1f - 12f * dt);
                m.LeftArmAngle *= MathF.Max(0f, 1f - 12f * dt);
                m.RightArmAngle *= MathF.Max(0f, 1f - 12f * dt);
                continue;
            }

            if (m.State == SnowmanState.Resting)
            {
                WindowSnowSurface? surf = null;
                if (m.RestingHwnd == IntPtr.Zero)
                {
                    // Fallback bottom-of-screen rest if no taskbar surface existed: re-check if one appeared.
                    if (TryFindSnowmanSurfaceAt(m.X, m.BaseY, out WindowSnowSurface? newSurf))
                    {
                        m.RestingHwnd = newSurf!.Hwnd;
                        m.RestingOffsetX = m.X - newSurf.Bounds.Left;
                        m.RestingYOffset = GetSurfaceHeightAt(newSurf, m.X) * 0.25f;
                        m.BaseY = newSurf.TopY - m.RestingYOffset;
                        surf = newSurf;
                    }
                }
                else if (_surfaces.TryGetValue(m.RestingHwnd, out WindowSnowSurface? foundSurf))
                {
                    surf = foundSurf;
                    float clampX = Math.Clamp(
                        surf.Bounds.Left + m.RestingOffsetX,
                        surf.StartX + 12f,
                        Math.Max(surf.StartX + 12f, surf.EndX - 12f));
                    m.X = clampX;
                    m.BaseY = surf.TopY - m.RestingYOffset;
                }
                else
                {
                    // Window moved/minimized/closed/maximized -> snowman falls smoothly to nearest surface below!
                    m.State = SnowmanState.Falling;
                    m.Action = SnowmanAction.Idle;
                    m.RestingHwnd = IntPtr.Zero;
                    m.Vx = 0f;
                    m.Vy = 25f;
                    continue;
                }

                if (animEnabled)
                {
                    // Check if snowplow train is chugging past this snowman -> cheerful jump & hat wave!
                    if (m.Action != SnowmanAction.WaveHat &&
                        _train.Active &&
                        m.RestingHwnd == _train.TargetHwnd &&
                        MathF.Abs(m.X - _train.PlowX) <= 78f &&
                        m.TrainWaveCooldown <= 0f)
                    {
                        m.Action = SnowmanAction.WaveHat;
                        m.ActionTimer = 0f;
                        m.ActionDuration = 1.35f;
                        m.TrainWaveCooldown = 5.5f;
                    }

                    if (m.Action == SnowmanAction.Idle)
                    {
                        m.TiltAngle *= MathF.Max(0f, 1f - 10f * dt);
                        m.HatLift *= MathF.Max(0f, 1f - 10f * dt);
                        m.HatTilt *= MathF.Max(0f, 1f - 10f * dt);
                        m.LeftArmAngle *= MathF.Max(0f, 1f - 10f * dt);
                        m.RightArmAngle *= MathF.Max(0f, 1f - 10f * dt);

                        if (m.HasPendingThrowTarget)
                        {
                            m.Action = SnowmanAction.Throw;
                            m.ActionTimer = 0f;
                            m.ActionDuration = 0.78f;
                            m.ActionTriggered = false;
                            m.ThrowAimDir = m.PendingThrowX >= m.X ? 1 : -1;
                        }
                        else
                        {
                            m.NextActionCooldown -= dt;
                            if (m.NextActionCooldown <= 0f)
                            {
                                m.Action = SnowmanAction.Throw;
                                m.ActionTimer = 0f;
                                m.ActionDuration = 0.90f;
                                m.ActionTriggered = false;
                                m.ThrowAimDir = _rng.Next(2) == 0 ? 1 : -1;
                                m.NextActionCooldown = 20f + _rng.NextSingle() * 26f;
                            }
                        }
                    }
                    else
                    {
                        m.ActionTimer += dt;
                        float p = Math.Clamp(m.ActionTimer / Math.Max(0.01f, m.ActionDuration), 0f, 1f);

                        switch (m.Action)
                        {
                            case SnowmanAction.WaveHat:
                            {
                                float arc = MathF.Sin(p * MathF.PI);
                                m.HopRenderOffsetY = -arc * 17f * m.Scale;
                                m.HatLift = arc * 10.5f;
                                m.HatTilt = arc * 14f;
                                m.RightArmAngle = -44f * MathF.Abs(MathF.Sin(p * MathF.PI * 3f));
                                m.LeftArmAngle = -14f * arc;
                                m.TiltAngle = -5f * MathF.Sin(p * MathF.Tau);
                                break;
                            }
                            case SnowmanAction.Throw:
                            {
                                float armSign = m.ThrowAimDir >= 0 ? 1f : -1f;
                                if (p < 0.45f)
                                {
                                    float wp = p / 0.45f;
                                    m.TiltAngle = -armSign * 8f * wp;
                                    if (m.ThrowAimDir >= 0) m.RightArmAngle = 38f * wp;
                                    else m.LeftArmAngle = 38f * wp;
                                }
                                else
                                {
                                    if (!m.ActionTriggered)
                                    {
                                        m.ActionTriggered = true;
                                        LaunchPlayfulSnowmanSnowball(m);
                                    }
                                    float fp = (p - 0.45f) / 0.55f;
                                    float swing = MathF.Sin((1f - fp) * MathF.PI * 0.5f);
                                    m.TiltAngle = armSign * 9f * swing;
                                    if (m.ThrowAimDir >= 0) m.RightArmAngle = -52f * swing;
                                    else m.LeftArmAngle = -52f * swing;
                                }
                                break;
                            }
                        }

                        if (m.ActionTimer >= m.ActionDuration)
                        {
                            m.Action = SnowmanAction.Idle;
                            m.ActionTimer = 0f;
                            m.ActionDuration = 0f;
                        }
                    }
                }
                else
                {
                    m.TiltAngle *= MathF.Max(0f, 1f - 10f * dt);
                    m.HatLift *= MathF.Max(0f, 1f - 10f * dt);
                    m.HatTilt *= MathF.Max(0f, 1f - 10f * dt);
                    m.LeftArmAngle *= MathF.Max(0f, 1f - 10f * dt);
                    m.RightArmAngle *= MathF.Max(0f, 1f - 10f * dt);
                }

                continue;
            }

            // Falling smoothly toward the nearest open window top edge or taskbar below.
            m.Action = SnowmanAction.Idle;
            m.HatLift *= MathF.Max(0f, 1f - 10f * dt);
            m.HatTilt *= MathF.Max(0f, 1f - 10f * dt);
            m.LeftArmAngle = -24f;
            m.RightArmAngle = -24f;
            if (m.IgnoreSurfaceTimer > 0f)
            {
                m.IgnoreSurfaceTimer = MathF.Max(0f, m.IgnoreSurfaceTimer - dt);
            }

            m.Vy += 680f * dt;
            m.Vx *= MathF.Max(0f, 1f - 1.35f * dt);
            m.TiltAngle = Math.Clamp(m.Vx * 0.025f, -18f, 18f);

            float stepX = m.Vx * dt;
            float stepY = m.Vy * dt;
            float stepDist = MathF.Sqrt(stepX * stepX + stepY * stepY);
            int substeps = Math.Clamp((int)MathF.Ceiling(stepDist / 8f), 1, 16);

            float startX = m.X;
            float startY = m.BaseY;
            float minBoundX = _minLeft + 24f * m.Scale;
            float maxBoundX = Math.Max(minBoundX, _maxRight - 24f * m.Scale);
            bool landed = false;

            for (int step = 1; step <= substeps; step++)
            {
                float t = step / (float)substeps;
                float prevY = startY + stepY * ((step - 1) / (float)substeps);
                float nx = startX + stepX * t;
                float ny = startY + stepY * t;

                if (nx < minBoundX)
                {
                    nx = minBoundX;
                    m.Vx = MathF.Abs(m.Vx) * 0.45f;
                    startX = nx - stepX * t;
                }
                else if (nx > maxBoundX)
                {
                    nx = maxBoundX;
                    m.Vx = -MathF.Abs(m.Vx) * 0.45f;
                    startX = nx - stepX * t;
                }

                if (m.Vy >= 0f)
                {
                    for (int s = 0; s < _surfaceList.Count; s++)
                    {
                        WindowSnowSurface surf = _surfaceList[s];
                        if (nx < surf.StartX - 6f || nx > surf.EndX + 6f)
                            continue;

                        if (m.IgnoreSurfaceTimer > 0f && surf.Hwnd == m.IgnoreSurfaceHwnd && prevY > surf.TopY + 10f)
                            continue;

                        float clampedX = Math.Clamp(nx, surf.StartX + 10f, Math.Max(surf.StartX + 10f, surf.EndX - 10f));
                        if (!IsSurfaceVisibleNear(surf, clampedX))
                            continue;

                        if (prevY <= surf.TopY + 8f && ny >= surf.TopY - 2f)
                        {
                            float localH = GetSurfaceHeightAt(surf, clampedX);
                            m.State = SnowmanState.Resting;
                            m.RestingHwnd = surf.Hwnd;
                            m.X = clampedX;
                            m.RestingOffsetX = clampedX - surf.Bounds.Left;
                            m.RestingYOffset = localH * 0.25f;
                            m.BaseY = surf.TopY - m.RestingYOffset;
                            m.Vx = 0f;
                            m.Vy = 0f;
                            m.TiltAngle = 0f;
                            m.PopTimer = 0.24f;
                            SpawnLandingDust(m.X, m.BaseY, m.Scale);
                            landed = true;
                            break;
                        }
                    }
                }

                if (landed)
                    break;
            }

            if (landed)
                continue;

            m.X = Math.Clamp(startX + stepX, minBoundX, maxBoundX);
            m.BaseY = startY + stepY;

            if (m.BaseY >= _maxBottom)
            {
                m.State = SnowmanState.Resting;
                m.RestingHwnd = IntPtr.Zero;
                m.BaseY = _maxBottom;
                m.Vx = 0f;
                m.Vy = 0f;
                m.TiltAngle = 0f;
                m.PopTimer = 0.22f;
                SpawnLandingDust(m.X, m.BaseY, m.Scale);
            }
        }
    }
}

public readonly record struct SnowflakeSnapshot(
    float X,
    float Y,
    float Radius,
    float Angle,
    int Variant,
    int Layer,
    byte Alpha);

public readonly record struct CrumbleSnapshot(float X, float Y, float Radius, byte Alpha);

public readonly record struct GlintSnapshot(float X, float Y, float MaxSize, float Progress);

public readonly record struct SmokePuffSnapshot(float X, float Y, float Radius, byte Alpha);

public readonly record struct SnowballSnapshot(
    float X,
    float Y,
    float Radius,
    float Angle,
    int Seed,
    bool IsHeld,
    float ShadowY,
    float ShadowAlpha);

public readonly record struct SnowmanSnapshot(
    float X,
    float BaseY,
    float TiltAngle,
    float Scale,
    float PopTimer,
    bool IsBlinking,
    float LookX,
    float LookY,
    float HatLift,
    float HatTilt,
    float LeftArmAngle,
    float RightArmAngle,
    SnowmanOutfit Outfit);

public readonly record struct FrostParticleSnapshot(
    float X,
    float Y,
    float Radius,
    float Angle,
    byte Alpha,
    bool IsStar);

public readonly record struct SnowplowSnapshot(
    float PlowX,
    float TopY,
    int Direction,
    float WheelAngle,
    bool ShowSantaWagon,
    bool ShowTreeWagon,
    float Time);

public readonly record struct DriftSnapshot(
    int StartX,
    int TopY,
    int BucketSize,
    float[] Heights,
    bool[] Visible);

public sealed record RenderSnapshot(
    SnowflakeSnapshot[] Flakes,
    CrumbleSnapshot[] Crumbles,
    GlintSnapshot[] Glints,
    SmokePuffSnapshot[] SmokePuffs,
    SnowballSnapshot[] Snowballs,
    SnowmanSnapshot[] Snowmen,
    FrostParticleSnapshot[] FrostTrail,
    SnowplowSnapshot? Train,
    List<DriftSnapshot> Drifts,
    bool IsPlayMode,
    float Time,
    bool ShowGarland);
