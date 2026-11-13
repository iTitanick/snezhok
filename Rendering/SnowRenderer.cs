using System.Drawing;
using SkiaSharp;
using SnowDesktop.Simulation;

namespace SnowDesktop.Rendering;

public sealed class SnowRenderer : IDisposable
{
    private const int SpriteSize = 64;
    private const int AlphaBucketCount = 8;

    private readonly SKImage[] _flakeSprites;
    private readonly SKPaint[] _flakeAlphaPaints;

    private readonly SKPaint _farFlakePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _glintPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round
    };

    private readonly SKPaint _crumblePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _driftFillPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        Color = new SKColor(245, 250, 255, 248)
    };

    private readonly SKPaint _driftShadowStrokePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.9f,
        StrokeCap = SKStrokeCap.Round,
        StrokeJoin = SKStrokeJoin.Round,
        Color = new SKColor(126, 168, 214, 110)
    };

    private readonly SKPaint _driftHighlightPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.35f,
        StrokeCap = SKStrokeCap.Round,
        Color = new SKColor(255, 255, 255, 245)
    };

    private readonly SKPaint _driftInnerPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPath _driftPath = new();
    private readonly SKPath _topRidgePath = new();
    private readonly SKPath _driftInnerPath = new();
    private readonly SKPath _trainPath = new();

    private readonly SKPaint _smokePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _trainFillPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _trainStrokePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round
    };

    private readonly SKPaint _ballPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _ballStrokePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke
    };

    private readonly SKPaint _hudBgPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill,
        Color = new SKColor(16, 28, 46, 215)
    };

    private readonly SKPaint _hudBorderPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.2f,
        Color = new SKColor(135, 195, 255, 175)
    };

    private readonly SKPaint _hudTextPaint = new()
    {
        IsAntialias = true,
        Color = new SKColor(238, 247, 255, 245),
        TextSize = 13.5f,
        Typeface = SKTypeface.FromFamilyName("Segoe UI", new SKFontStyle(SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright)),
        TextAlign = SKTextAlign.Center
    };

    private readonly SKPaint _garlandWirePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeWidth = 1.35f,
        StrokeCap = SKStrokeCap.Round,
        Color = new SKColor(20, 44, 26)
    };

    private readonly SKPaint _garlandNeedlePaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Stroke,
        StrokeCap = SKStrokeCap.Round
    };

    private readonly SKPaint _garlandBulbPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPaint _garlandGlowPaint = new()
    {
        IsAntialias = true,
        Style = SKPaintStyle.Fill
    };

    private readonly SKPath _garlandPath = new();
    private readonly SKPath _garlandStarPath = new();
    private readonly SKPath _garlandBulbPath = new();

    public SnowRenderer()
    {
        _flakeSprites = CreateSnowflakeSprites();
        _flakeAlphaPaints = new SKPaint[AlphaBucketCount];
        for (int i = 0; i < AlphaBucketCount; i++)
        {
            byte alpha = (byte)(135 + i * (120 / (AlphaBucketCount - 1)));
            _flakeAlphaPaints[i] = new SKPaint
            {
                IsAntialias = true,
                FilterQuality = SKFilterQuality.Medium,
                Color = new SKColor(255, 255, 255, alpha)
            };
        }

        _garlandStarPath.MoveTo(0f, -3.8f);
        _garlandStarPath.LineTo(1.2f, -1.1f);
        _garlandStarPath.LineTo(3.8f, 0f);
        _garlandStarPath.LineTo(1.2f, 1.1f);
        _garlandStarPath.LineTo(0f, 3.8f);
        _garlandStarPath.LineTo(-1.2f, 1.1f);
        _garlandStarPath.LineTo(-3.8f, 0f);
        _garlandStarPath.LineTo(-1.2f, -1.1f);
        _garlandStarPath.Close();

        _garlandBulbPath.MoveTo(0f, 2.4f);
        _garlandBulbPath.CubicTo(2.2f, 3.0f, 2.4f, 6.8f, 0f, 9.2f);
        _garlandBulbPath.CubicTo(-2.4f, 6.8f, -2.2f, 3.0f, 0f, 2.4f);
        _garlandBulbPath.Close();
    }

    public void Render(SKCanvas canvas, Rectangle monitorBounds, RenderSnapshot snapshot)
    {
        canvas.Clear(SKColors.Transparent);

        float offsetX = -monitorBounds.Left;
        float offsetY = -monitorBounds.Top;

        // 1. Draw distant background snowflakes as tiny glowing pinprick dots for depth/parallax.
        for (int i = 0; i < snapshot.Flakes.Length; i++)
        {
            SnowflakeSnapshot f = snapshot.Flakes[i];
            if (f.Layer != 0)
                continue;

            float x = f.X + offsetX;
            float y = f.Y + offsetY;
            if (x < -8f || x > monitorBounds.Width + 8f || y < -8f || y > monitorBounds.Height + 8f)
                continue;

            // Soft luminous halo + crisp star-like white pinpoint core
            _farFlakePaint.Color = new SKColor(195, 226, 255, (byte)(f.Alpha * 0.24f));
            canvas.DrawCircle(x, y, f.Radius * 2.5f, _farFlakePaint);

            _farFlakePaint.Color = new SKColor(242, 249, 255, f.Alpha);
            canvas.DrawCircle(x, y, f.Radius * 0.9f, _farFlakePaint);
        }

        // 2. Draw accumulated snowdrifts/heaps on windows and taskbar.
        for (int i = 0; i < snapshot.Drifts.Count; i++)
        {
            DriftSnapshot drift = snapshot.Drifts[i];
            float localTopY = drift.TopY + offsetY;
            if (localTopY < -80f || localTopY > monitorBounds.Height + 80f)
                continue;

            float driftLeft = drift.StartX + offsetX;
            float driftRight = driftLeft + drift.Heights.Length * drift.BucketSize;
            if (driftRight < -10f || driftLeft > monitorBounds.Width + 10f)
                continue;

            DrawDrift(canvas, drift, offsetX, localTopY, monitorBounds.Width, snapshot.Time);
        }

        // 3. Draw steam/smoke puffs and the snowplow train (if active on the taskbar).
        for (int i = 0; i < snapshot.SmokePuffs.Length; i++)
        {
            SmokePuffSnapshot sp = snapshot.SmokePuffs[i];
            if (sp.Alpha < 4)
                continue;

            float x = sp.X + offsetX;
            float y = sp.Y + offsetY;
            if (x < -30f || x > monitorBounds.Width + 30f || y < -30f || y > monitorBounds.Height + 30f)
                continue;

            _smokePaint.Color = new SKColor(238, 245, 255, sp.Alpha);
            canvas.DrawCircle(x, y, sp.Radius, _smokePaint);
        }

        if (snapshot.Train is SnowplowSnapshot train)
        {
            float tx = train.PlowX + offsetX;
            float ty = train.TopY + offsetY;
            float cullMarginX = 180f
                + (train.ShowSantaWagon ? 95f : 0f)
                + (train.ShowTreeWagon ? 85f : 0f);
            if (tx > -cullMarginX && tx < monitorBounds.Width + cullMarginX && ty > -90f && ty < monitorBounds.Height + 90f)
            {
                DrawSnowplowTrain(canvas, tx, ty, train);
            }
        }

        // 3b. Draw snowballs and snow boulders (resting, held, or flying).
        for (int i = 0; i < snapshot.Snowballs.Length; i++)
        {
            SnowballSnapshot b = snapshot.Snowballs[i];
            float x = b.X + offsetX;
            float y = b.Y + offsetY;
            if (x < -b.Radius - 10f || x > monitorBounds.Width + b.Radius + 10f ||
                y < -b.Radius - 10f || y > monitorBounds.Height + b.Radius + 10f)
            {
                continue;
            }

            DrawSnowball(
                canvas,
                x,
                y,
                b.Radius,
                b.Angle,
                b.Seed,
                b.IsHeld,
                b.ShadowY + offsetY,
                b.ShadowAlpha);
        }

        // 3c. Draw snowmen (sitting on windows/taskbar, held, or gliding down cleanly).
        for (int i = 0; i < snapshot.Snowmen.Length; i++)
        {
            SnowmanSnapshot m = snapshot.Snowmen[i];
            float x = m.X + offsetX;
            float baseY = m.BaseY + offsetY;
            if (x < -60f * m.Scale || x > monitorBounds.Width + 60f * m.Scale ||
                baseY < -30f || baseY > monitorBounds.Height + 115f * m.Scale)
            {
                continue;
            }

            DrawSnowman(canvas, x, baseY, m);
        }

        // 4. Draw falling crumble / corner-splash particles.
        for (int i = 0; i < snapshot.Crumbles.Length; i++)
        {
            CrumbleSnapshot c = snapshot.Crumbles[i];
            if (c.Alpha < 4)
                continue;

            float x = c.X + offsetX;
            float y = c.Y + offsetY;
            if (x < -10f || x > monitorBounds.Width + 10f || y < -10f || y > monitorBounds.Height + 10f)
                continue;

            _crumblePaint.Color = new SKColor(240, 248, 255, c.Alpha);
            canvas.DrawCircle(x, y, c.Radius, _crumblePaint);
        }

        // 4. Draw corner sparkle glints where snowflakes clipped a window corner.
        for (int i = 0; i < snapshot.Glints.Length; i++)
        {
            GlintSnapshot g = snapshot.Glints[i];
            float x = g.X + offsetX;
            float y = g.Y + offsetY;
            if (x < -20f || x > monitorBounds.Width + 20f || y < -20f || y > monitorBounds.Height + 20f)
                continue;

            float fade = MathF.Sin(g.Progress * MathF.PI); // Smooth 0 -> 1 -> 0 pulse
            byte alpha = (byte)Math.Clamp(fade * 240f, 0f, 255f);
            if (alpha < 6)
                continue;

            float arm = g.MaxSize * (0.35f + 0.65f * g.Progress);
            _glintPaint.StrokeWidth = 1.4f;
            _glintPaint.Color = new SKColor(235, 248, 255, alpha);
            canvas.DrawLine(x - arm, y, x + arm, y, _glintPaint);
            canvas.DrawLine(x, y - arm, x, y + arm, _glintPaint);

            float diag = arm * 0.52f;
            _glintPaint.StrokeWidth = 1.0f;
            _glintPaint.Color = new SKColor(185, 225, 255, (byte)(alpha * 0.75f));
            canvas.DrawLine(x - diag, y - diag, x + diag, y + diag, _glintPaint);
            canvas.DrawLine(x - diag, y + diag, x + diag, y - diag, _glintPaint);
        }

        // 4b. Delicate cursor frost trail sparkles & dust
        for (int i = 0; i < snapshot.FrostTrail.Length; i++)
        {
            FrostParticleSnapshot fp = snapshot.FrostTrail[i];
            if (fp.Alpha < 5)
                continue;

            float x = fp.X + offsetX;
            float y = fp.Y + offsetY;
            if (x < -12f || x > monitorBounds.Width + 12f || y < -12f || y > monitorBounds.Height + 12f)
                continue;

            _farFlakePaint.Color = new SKColor(168, 226, 255, (byte)(fp.Alpha * 0.28f));
            canvas.DrawCircle(x, y, fp.Radius * 2.2f, _farFlakePaint);

            if (fp.IsStar)
            {
                float arm = fp.Radius * 1.95f;
                float diag = arm * 0.50f;
                float rad = fp.Angle * MathF.PI / 180f;
                float cos = MathF.Cos(rad);
                float sin = MathF.Sin(rad);

                _glintPaint.StrokeWidth = Math.Max(0.85f, fp.Radius * 0.55f);
                _glintPaint.Color = new SKColor(235, 249, 255, fp.Alpha);
                canvas.DrawLine(x - cos * arm, y - sin * arm, x + cos * arm, y + sin * arm, _glintPaint);
                canvas.DrawLine(x + sin * arm, y - cos * arm, x - sin * arm, y + cos * arm, _glintPaint);

                _farFlakePaint.Color = new SKColor(255, 255, 255, fp.Alpha);
                canvas.DrawCircle(x, y, diag * 0.36f, _farFlakePaint);
            }
            else
            {
                _farFlakePaint.Color = new SKColor(238, 250, 255, fp.Alpha);
                canvas.DrawCircle(x, y, fp.Radius * 0.85f, _farFlakePaint);
            }
        }

        // 5. Draw mid and foreground crystalline snowflakes.
        for (int i = 0; i < snapshot.Flakes.Length; i++)
        {
            SnowflakeSnapshot f = snapshot.Flakes[i];
            if (f.Layer == 0)
                continue;

            float x = f.X + offsetX;
            float y = f.Y + offsetY;
            float half = f.Radius * 1.45f;

            if (x + half < 0f || x - half > monitorBounds.Width ||
                y + half < 0f || y - half > monitorBounds.Height)
            {
                continue;
            }

            int variant = (uint)f.Variant < (uint)_flakeSprites.Length ? f.Variant : 0;
            int alphaIdx = Math.Clamp((f.Alpha - 135) * (AlphaBucketCount - 1) / 120, 0, AlphaBucketCount - 1);

            canvas.Save();
            canvas.Translate(x, y);
            canvas.RotateDegrees(f.Angle);
            canvas.DrawImage(_flakeSprites[variant], new SKRect(-half, -half, half, half), _flakeAlphaPaints[alphaIdx]);
            canvas.Restore();
        }

        // 5b. Festive delicate garland across the top edge of the monitor.
        if (snapshot.ShowGarland)
        {
            DrawGarland(canvas, monitorBounds.Width, snapshot.Time);
        }

        // 6. Play Mode indicator pill at top-center of primary monitor.
        if (snapshot.IsPlayMode && monitorBounds.Left == 0 && monitorBounds.Top == 0)
        {
            DrawPlayModeBadge(canvas, monitorBounds.Width);
        }
    }

    private void DrawDrift(
        SKCanvas canvas,
        DriftSnapshot drift,
        float offsetX,
        float localTopY,
        int viewportWidth,
        float time)
    {
        float[] h = drift.Heights;
        bool[] vis = drift.Visible;
        int bs = drift.BucketSize;
        int len = h.Length;

        int i = 0;
        while (i < len)
        {
            while (i < len && (!vis[i] || h[i] <= 0.25f))
                i++;

            if (i >= len)
                break;

            int runStart = i;
            float maxRunH = 0f;
            while (i < len && vis[i] && h[i] > 0.25f)
            {
                if (h[i] > maxRunH)
                    maxRunH = h[i];
                i++;
            }
            int runEnd = i - 1;

            float startX = drift.StartX + runStart * bs + offsetX;
            float endX = drift.StartX + (runEnd + 1) * bs + offsetX;

            if (endX < -10f || startX > viewportWidth + 10f)
                continue;

            _driftPath.Reset();
            _topRidgePath.Reset();
            _driftInnerPath.Reset();

            float baseY = localTopY + 1.4f;
            _driftPath.MoveTo(startX, baseY);
            _driftPath.LineTo(startX, localTopY);

            float firstX = startX + bs * 0.5f;
            float firstY = localTopY - h[runStart];
            _driftPath.QuadTo(startX, localTopY - h[runStart] * 0.45f, firstX, firstY);

            _topRidgePath.MoveTo(startX, localTopY);
            _topRidgePath.QuadTo(startX, localTopY - h[runStart] * 0.45f, firstX, firstY);

            // Secondary soft undulating subsurface shadow band (~42% height inside drift)
            float firstInnerY = localTopY - h[runStart] * 0.44f;
            _driftInnerPath.MoveTo(startX, baseY);
            _driftInnerPath.LineTo(startX, localTopY - h[runStart] * 0.20f);
            _driftInnerPath.LineTo(firstX, firstInnerY);

            float prevX = firstX;
            float prevY = firstY;
            float prevInnerY = firstInnerY;

            for (int k = runStart + 1; k <= runEnd; k++)
            {
                float curX = drift.StartX + k * bs + bs * 0.5f + offsetX;
                float curY = localTopY - h[k];
                float midX = (prevX + curX) * 0.5f;
                float midY = (prevY + curY) * 0.5f;

                _driftPath.QuadTo(prevX, prevY, midX, midY);
                _topRidgePath.QuadTo(prevX, prevY, midX, midY);

                float worldWave = 0.42f + 0.08f * MathF.Sin((drift.StartX + k * bs) * 0.045f);
                float curInnerY = localTopY - h[k] * worldWave;
                _driftInnerPath.QuadTo(prevX, prevInnerY, midX, (prevInnerY + curInnerY) * 0.5f);

                prevX = curX;
                prevY = curY;
                prevInnerY = curInnerY;
            }

            _driftPath.LineTo(prevX, prevY);
            _driftPath.QuadTo(endX, localTopY - h[runEnd] * 0.45f, endX, localTopY);
            _driftPath.LineTo(endX, baseY);
            _driftPath.Close();

            _topRidgePath.LineTo(prevX, prevY);
            _topRidgePath.QuadTo(endX, localTopY - h[runEnd] * 0.45f, endX, localTopY);

            _driftInnerPath.LineTo(prevX, prevInnerY);
            _driftInnerPath.LineTo(endX, localTopY - h[runEnd] * 0.20f);
            _driftInnerPath.LineTo(endX, baseY);
            _driftInnerPath.Close();

            // 1. Soft contact shadow grounding snow onto window frame.
            _driftInnerPaint.Color = new SKColor(68, 102, 146, 62);
            canvas.DrawRect(startX + 1f, localTopY + 0.2f, MathF.Max(1f, endX - startX - 2f), 1.8f, _driftInnerPaint);

            // 2. Volumetric top-to-bottom snow gradient fill.
            float gradTopY = localTopY - MathF.Max(5f, maxRunH);
            using (var grad = SKShader.CreateLinearGradient(
                new SKPoint(0f, gradTopY),
                new SKPoint(0f, baseY),
                new[]
                {
                    new SKColor(253, 254, 255, 250),
                    new SKColor(239, 247, 255, 248),
                    new SKColor(196, 222, 248, 245)
                },
                new[] { 0f, 0.55f, 1f },
                SKShaderTileMode.Clamp))
            {
                _driftFillPaint.Shader = grad;
                canvas.DrawPath(_driftPath, _driftFillPaint);
                _driftFillPaint.Shader = null;
            }

            // 3. Clipped soft layered wind-dune depth band inside the lower half of the drift.
            if (maxRunH >= 4.5f)
            {
                canvas.Save();
                canvas.ClipPath(_driftPath);
                _driftInnerPaint.Color = new SKColor(168, 206, 242, 72);
                canvas.DrawPath(_driftInnerPath, _driftInnerPaint);
                canvas.Restore();
            }

            // 4. Soft frosty upper contour + crisp sunlit crest highlight.
            canvas.DrawPath(_topRidgePath, _driftShadowStrokePaint);
            canvas.Save();
            canvas.Translate(0f, -0.35f);
            canvas.DrawPath(_topRidgePath, _driftHighlightPaint);
            canvas.Restore();

            // 5. Subtle crystalline ice sparkles twinkling across snowdrifts.
            if (maxRunH >= 4.0f)
            {
                DrawDriftSparkles(canvas, drift, runStart, runEnd, offsetX, localTopY, time);
            }
        }
    }

    private void DrawDriftSparkles(
        SKCanvas canvas,
        DriftSnapshot drift,
        int runStart,
        int runEnd,
        float offsetX,
        float localTopY,
        float time)
    {
        float[] h = drift.Heights;
        int bs = drift.BucketSize;

        for (int b = runStart + 2; b <= runEnd - 2; b++)
        {
            float height = h[b];
            if (height < 4.2f)
                continue;

            int worldBucket = drift.StartX / Math.Max(1, bs) + b;
            uint hash = unchecked((uint)(worldBucket * 73856093 ^ drift.TopY * 19349663));
            hash ^= hash >> 13;
            hash *= 0x5BD1E995u;
            if ((hash & 0xF) != 0) // ~6.2% of buckets hold subtle ice crystal facets
                continue;

            float phase = (hash & 0xFF) / 255f * MathF.Tau;
            float speed = 1.8f + ((hash >> 8) & 0x3F) / 63f * 1.8f;
            float wave = MathF.Max(0f, MathF.Sin(time * speed + phase));
            float intensity = MathF.Pow(wave, 5f);
            if (intensity < 0.08f)
                continue;

            float x = drift.StartX + b * bs + bs * 0.5f + offsetX;
            float depthFrac = 0.24f + ((hash >> 16) & 0xFF) / 255f * 0.52f;
            float y = localTopY - height * depthFrac;

            float arm = (1.6f + 1.8f * intensity);
            byte alpha = (byte)Math.Clamp(intensity * 235f, 0f, 240f);

            _glintPaint.StrokeWidth = 1.05f;
            _glintPaint.Color = new SKColor(255, 255, 255, alpha);
            canvas.DrawLine(x - arm, y, x + arm, y, _glintPaint);
            canvas.DrawLine(x, y - arm, x, y + arm, _glintPaint);

            float diag = arm * 0.52f;
            _glintPaint.StrokeWidth = 0.85f;
            _glintPaint.Color = new SKColor(195, 236, 255, (byte)(alpha * 0.72f));
            canvas.DrawLine(x - diag, y - diag, x + diag, y + diag, _glintPaint);
            canvas.DrawLine(x - diag, y + diag, x + diag, y - diag, _glintPaint);
        }
    }

    private static SKImage[] CreateSnowflakeSprites()
    {
        var sprites = new SKImage[5];
        for (int v = 0; v < sprites.Length; v++)
        {
            sprites[v] = BakeSnowflakeSprite(v);
        }
        return sprites;
    }

    private static SKImage BakeSnowflakeSprite(int variant)
    {
        var info = new SKImageInfo(SpriteSize, SpriteSize, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        SKCanvas c = surface.Canvas;
        c.Clear(SKColors.Transparent);

        float center = SpriteSize * 0.5f;
        float radius = SpriteSize * 0.38f;

        // Soft radial luminous glow behind each snowflake (matches starry/glowing winter look).
        using (var glowShader = SKShader.CreateRadialGradient(
            new SKPoint(center, center),
            SpriteSize * 0.48f,
            new[]
            {
                new SKColor(225, 242, 255, 85),
                new SKColor(175, 218, 255, 28),
                SKColors.Transparent
            },
            new[] { 0f, 0.45f, 1f },
            SKShaderTileMode.Clamp))
        using (var glowPaint = new SKPaint { IsAntialias = true, Shader = glowShader })
        {
            c.DrawCircle(center, center, SpriteSize * 0.48f, glowPaint);
        }

        // First pass: delicate cool-blue outline so crystals stay readable even on white windows.
        using var shadowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = new SKColor(102, 148, 204, 115)
        };

        // Second pass: crisp white crystal core.
        using var whitePaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = SKColors.White
        };

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = SKColors.White
        };

        DrawCrystalGeometry(c, center, radius, variant, shadowPaint, extraThickness: 1.4f, fillPaint: null);
        DrawCrystalGeometry(c, center, radius, variant, whitePaint, extraThickness: 0f, fillPaint: fillPaint);

        return surface.Snapshot();
    }

    private static void DrawCrystalGeometry(
        SKCanvas c,
        float center,
        float radius,
        int variant,
        SKPaint strokePaint,
        float extraThickness,
        SKPaint? fillPaint)
    {
        float armWidth = variant switch
        {
            1 => 2.5f,
            3 => 2.2f,
            4 => 2.6f,
            _ => 2.3f
        } + extraThickness;

        float branchWidth = 1.8f + extraThickness;

        for (int i = 0; i < 6; i++)
        {
            c.Save();
            c.Translate(center, center);
            c.RotateDegrees(i * 60f);

            // Main arm
            strokePaint.StrokeWidth = armWidth;
            c.DrawLine(0f, 0f, 0f, -radius, strokePaint);

            strokePaint.StrokeWidth = branchWidth;

            switch (variant)
            {
                case 0: // Classic dendrite with two pairs of fern branches
                    DrawBranchPair(c, -radius * 0.42f, radius * 0.28f, 52f, strokePaint);
                    DrawBranchPair(c, -radius * 0.72f, radius * 0.22f, 48f, strokePaint);
                    break;

                case 1: // Star plate with inner hexagon + arrowhead tips
                    DrawBranchPair(c, -radius * 0.56f, radius * 0.24f, 55f, strokePaint);
                    DrawBranchPair(c, -radius * 0.85f, radius * 0.14f, 42f, strokePaint);
                    if (fillPaint is not null)
                    {
                        c.DrawCircle(0f, -radius * 0.96f, 2.0f, fillPaint);
                    }
                    break;

                case 2: // Delicate frost with 3 fine branch pairs
                    DrawBranchPair(c, -radius * 0.32f, radius * 0.20f, 55f, strokePaint);
                    DrawBranchPair(c, -radius * 0.56f, radius * 0.26f, 50f, strokePaint);
                    DrawBranchPair(c, -radius * 0.78f, radius * 0.16f, 45f, strokePaint);
                    break;

                case 3: // Geometric crystal with crossbar + forked tips
                    DrawBranchPair(c, -radius * 0.48f, radius * 0.22f, 60f, strokePaint);
                    DrawBranchPair(c, -radius * 0.76f, radius * 0.20f, 38f, strokePaint);
                    break;

                default: // Plump winter star with diamond tips
                    DrawBranchPair(c, -radius * 0.50f, radius * 0.28f, 50f, strokePaint);
                    if (fillPaint is not null)
                    {
                        c.DrawCircle(0f, -radius * 0.92f, 2.4f, fillPaint);
                    }
                    break;
            }

            c.Restore();
        }

        // Inner hexagonal ring on variants 1 and 3
        if (variant is 1 or 3)
        {
            float hexR = radius * 0.30f;
            strokePaint.StrokeWidth = 1.7f + extraThickness * 0.8f;
            using var hexPath = new SKPath();
            for (int i = 0; i < 6; i++)
            {
                float rad = (i * 60f - 90f) * MathF.PI / 180f;
                float px = center + MathF.Cos(rad) * hexR;
                float py = center + MathF.Sin(rad) * hexR;
                if (i == 0) hexPath.MoveTo(px, py);
                else hexPath.LineTo(px, py);
            }
            hexPath.Close();
            c.DrawPath(hexPath, strokePaint);
        }

        if (fillPaint is not null)
        {
            c.DrawCircle(center, center, 2.4f, fillPaint);
        }
    }

    private static void DrawBranchPair(
        SKCanvas c,
        float startY,
        float length,
        float angleDeg,
        SKPaint paint)
    {
        float rad = angleDeg * MathF.PI / 180f;
        float dx = MathF.Sin(rad) * length;
        float dy = -MathF.Cos(rad) * length;

        c.DrawLine(0f, startY, dx, startY + dy, paint);
        c.DrawLine(0f, startY, -dx, startY + dy, paint);
    }

    public void Dispose()
    {
        for (int i = 0; i < _flakeSprites.Length; i++)
        {
            _flakeSprites[i].Dispose();
        }

        for (int i = 0; i < _flakeAlphaPaints.Length; i++)
        {
            _flakeAlphaPaints[i].Dispose();
        }

        _farFlakePaint.Dispose();
        _glintPaint.Dispose();
        _crumblePaint.Dispose();
        _driftFillPaint.Dispose();
        _driftShadowStrokePaint.Dispose();
        _driftHighlightPaint.Dispose();
        _driftInnerPaint.Dispose();
        _driftPath.Dispose();
        _topRidgePath.Dispose();
        _driftInnerPath.Dispose();
        _trainPath.Dispose();
        _smokePaint.Dispose();
        _trainFillPaint.Dispose();
        _trainStrokePaint.Dispose();
        _ballPaint.Dispose();
        _ballStrokePaint.Dispose();
        _hudBgPaint.Dispose();
        _hudBorderPaint.Dispose();
        _hudTextPaint.Dispose();
        _garlandWirePaint.Dispose();
        _garlandNeedlePaint.Dispose();
        _garlandBulbPaint.Dispose();
        _garlandGlowPaint.Dispose();
        _garlandPath.Dispose();
        _garlandStarPath.Dispose();
        _garlandBulbPath.Dispose();
    }

    private void DrawSnowball(
        SKCanvas canvas,
        float x,
        float y,
        float radius,
        float angle,
        int seed,
        bool isHeld,
        float shadowY,
        float shadowAlpha)
    {
        // 1. Cast elliptical shadow onto the snowdrift during rolling or resting.
        if (shadowAlpha > 0.01f)
        {
            float shX = x + radius * 0.12f;
            float rx = radius * 1.24f;
            float ry = MathF.Max(2.2f, radius * 0.30f);

            canvas.Save();
            canvas.Translate(shX, shadowY);
            canvas.Scale(1f, ry / MathF.Max(1f, rx));
            using (var shShader = SKShader.CreateRadialGradient(
                new SKPoint(0f, 0f),
                rx,
                new[]
                {
                    new SKColor(52, 84, 128, (byte)(118 * shadowAlpha)),
                    new SKColor(78, 114, 160, (byte)(56 * shadowAlpha)),
                    new SKColor(78, 114, 160, 0)
                },
                new[] { 0f, 0.58f, 1f },
                SKShaderTileMode.Clamp))
            {
                _ballPaint.Color = SKColors.White;
                _ballPaint.Shader = shShader;
                canvas.DrawCircle(0f, 0f, rx, _ballPaint);
                _ballPaint.Shader = null;
            }
            canvas.Restore();

            _ballPaint.Color = new SKColor(42, 74, 118, (byte)(76 * shadowAlpha));
            canvas.DrawOval(x + radius * 0.04f, shadowY - ry * 0.06f, radius * 0.68f, MathF.Max(1.2f, ry * 0.32f), _ballPaint);
        }

        // 2. Build organically uneven, hand-packed snowball silhouette rotated by current roll angle.
        int lobeCount = radius >= 10f ? 14 : 10;
        Span<SKPoint> pts = stackalloc SKPoint[lobeCount];
        float baseRotRad = angle * (MathF.PI / 180f);
        for (int i = 0; i < lobeCount; i++)
        {
            uint h = unchecked((uint)(seed * 73856093 ^ (i + 1) * 19349663));
            float a = baseRotRad + i * (MathF.Tau / lobeCount);
            float jitter = (((h & 0xFF) / 255f) - 0.5f) * 2f; // -1..+1
            float bump = 1f + jitter * (radius >= 8f ? 0.055f : 0.035f);
            float r = radius * bump;
            pts[i] = new SKPoint(x + MathF.Cos(a) * r, y + MathF.Sin(a) * r);
        }

        _driftPath.Reset();
        SKPoint startMid = new((pts[lobeCount - 1].X + pts[0].X) * 0.5f, (pts[lobeCount - 1].Y + pts[0].Y) * 0.5f);
        _driftPath.MoveTo(startMid);
        for (int i = 0; i < lobeCount; i++)
        {
            SKPoint cur = pts[i];
            SKPoint next = pts[(i + 1) % lobeCount];
            SKPoint mid = new((cur.X + next.X) * 0.5f, (cur.Y + next.Y) * 0.5f);
            _driftPath.QuadTo(cur, mid);
        }
        _driftPath.Close();

        // 3. Opaque matte pure-white snow body with gentle underside shading.
        using (var matteGrad = SKShader.CreateLinearGradient(
            new SKPoint(x - radius * 0.45f, y - radius * 0.85f),
            new SKPoint(x + radius * 0.45f, y + radius * 0.92f),
            new[]
            {
                new SKColor(255, 255, 255, 255),
                new SKColor(246, 250, 255, 255),
                new SKColor(214, 230, 248, 255)
            },
            new[] { 0f, 0.62f, 1f },
            SKShaderTileMode.Clamp))
        {
            _ballPaint.Color = SKColors.White;
            _ballPaint.Shader = matteGrad;
            canvas.DrawPath(_driftPath, _ballPaint);
            _ballPaint.Shader = null;
        }

        // 4. Subtle matte snow creases/fluffs inside clipped uneven contour (rotates naturally as you roll).
        canvas.Save();
        canvas.ClipPath(_driftPath);
        canvas.Translate(x, y);
        canvas.RotateDegrees(angle);

        int creaseCount = radius > 16f ? 6 : (radius > 8f ? 4 : 2);
        for (int i = 0; i < creaseCount; i++)
        {
            uint h = unchecked((uint)(seed * 2654435761u ^ (i + 3) * 83492791u));
            float a = (h & 0xFF) / 255f * MathF.Tau;
            float d = radius * (0.20f + ((h >> 8) & 0xFF) / 255f * 0.46f);
            float cx = MathF.Cos(a) * d;
            float cy = MathF.Sin(a) * d;
            float rx = MathF.Max(1.2f, radius * (0.16f + ((h >> 16) & 0x3F) / 63f * 0.14f));
            float ry = rx * 0.58f;

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(((h >> 22) & 0xFF) * 1.4f);
            _ballPaint.Color = new SKColor(208, 224, 244, 90);
            canvas.DrawOval(0f, 0f, rx, ry, _ballPaint);
            _ballPaint.Color = new SKColor(255, 255, 255, 130);
            canvas.DrawOval(-rx * 0.14f, -ry * 0.25f, rx * 0.80f, ry * 0.65f, _ballPaint);
            canvas.Restore();
        }
        canvas.Restore();

        // 5. Soft matte snow outline so the uneven white ball separates cleanly on bright windows.
        _ballStrokePaint.StrokeWidth = 1.15f;
        _ballStrokePaint.Color = new SKColor(138, 168, 204, 130);
        canvas.DrawPath(_driftPath, _ballStrokePaint);

        // 6. Subtle soft snow lip in front of bottom contact when actively rolling through drift.
        if (isHeld && shadowAlpha > 0.85f && radius >= 5.5f)
        {
            float lipW = radius * 0.85f;
            float lipY = shadowY + 0.5f;
            _driftPath.Reset();
            _driftPath.MoveTo(x - lipW, lipY + 1.6f);
            _driftPath.QuadTo(x - lipW * 0.42f, lipY - MathF.Min(3.8f, radius * 0.20f), x, lipY - MathF.Min(2.2f, radius * 0.12f));
            _driftPath.QuadTo(x + lipW * 0.42f, lipY - MathF.Min(3.6f, radius * 0.19f), x + lipW, lipY + 1.6f);
            _driftPath.QuadTo(x, lipY + 2.8f, x - lipW, lipY + 1.6f);
            _driftPath.Close();

            _ballPaint.Color = new SKColor(248, 252, 255, 242);
            canvas.DrawPath(_driftPath, _ballPaint);
            _ballStrokePaint.StrokeWidth = 0.9f;
            _ballStrokePaint.Color = new SKColor(148, 178, 212, 110);
            canvas.DrawPath(_driftPath, _ballStrokePaint);
        }
    }

    private void DrawSnowman(SKCanvas canvas, float baseX, float baseY, SnowmanSnapshot m)
    {
        float springX = 1f;
        float springY = 1f;
        if (m.PopTimer > 0f)
        {
            float p = Math.Clamp(m.PopTimer / 0.45f, 0f, 1f);
            float squash = MathF.Sin(p * MathF.PI * 2f) * p * 0.12f;
            springX = 1f + squash;
            springY = 1f - squash * 0.85f;
        }

        canvas.Save();
        canvas.Translate(baseX, baseY);
        canvas.RotateDegrees(m.TiltAngle);
        canvas.Scale(m.Scale * springX, m.Scale * springY);

        // 1. Twig arms extending upward-outward from the middle torso (with live wave/juggle/throw rotation).
        DrawTwigArm(canvas, side: -1f, m.LeftArmAngle);
        DrawTwigArm(canvas, side: 1f, m.RightArmAngle);

        // 2. Three stacked snowballs with soft contour + lower snow shading so they read cleanly on light/dark desktops.
        DrawSnowmanSphere(canvas, 0f, -17.5f, 21.5f, 18.2f);
        DrawSnowmanSphere(canvas, 0f, -40.0f, 15.8f, 13.8f);
        DrawSnowmanSphere(canvas, 0f, -60.5f, 11.8f, 11.2f);

        // 3. Coat buttons (2 on bottom sphere, 2 on middle sphere).
        DrawSnowmanButton(canvas, 0f, -12.0f, 2.3f);
        DrawSnowmanButton(canvas, 0f, -21.5f, 2.3f);
        DrawSnowmanButton(canvas, 0f, -36.5f, 2.0f);
        DrawSnowmanButton(canvas, 0f, -44.0f, 2.0f);

        // 4. Outfit-themed scarf around neck + vertical hanging ribbon tail to the right.
        (SKColor scarfTail, SKColor scarfNeck, SKColor scarfStroke, SKColor scarfHi, SKColor scarfKnot) = m.Outfit switch
        {
            SnowmanOutfit.CozyBeanie => (
                new SKColor(34, 114, 198, 248),
                new SKColor(46, 134, 222, 252),
                new SKColor(20, 74, 138, 210),
                new SKColor(115, 190, 252, 175),
                new SKColor(26, 98, 178, 245)),
            SnowmanOutfit.Bucket => (
                new SKColor(32, 132, 74, 248),
                new SKColor(42, 154, 88, 252),
                new SKColor(18, 92, 48, 210),
                new SKColor(102, 206, 138, 170),
                new SKColor(24, 116, 62, 245)),
            SnowmanOutfit.Santa => (
                new SKColor(206, 28, 38, 248),
                new SKColor(224, 38, 46, 252),
                new SKColor(148, 16, 22, 210),
                new SKColor(252, 196, 84, 185),
                new SKColor(190, 22, 30, 245)),
            SnowmanOutfit.Cool => (
                new SKColor(92, 72, 188, 248),
                new SKColor(112, 88, 214, 252),
                new SKColor(56, 40, 128, 210),
                new SKColor(172, 152, 248, 175),
                new SKColor(78, 58, 166, 245)),
            _ => (
                new SKColor(196, 26, 34, 248),
                new SKColor(214, 34, 42, 252),
                new SKColor(145, 16, 22, 210),
                new SKColor(240, 74, 78, 170),
                new SKColor(186, 22, 30, 245))
        };

        _trainPath.Reset();
        _trainPath.MoveTo(1.2f, -50.8f);
        _trainPath.LineTo(6.6f, -50.8f);
        _trainPath.QuadTo(8.5f, -40.5f, 8.2f, -29.5f);
        _trainPath.LineTo(3.2f, -29.0f);
        _trainPath.QuadTo(2.9f, -40.5f, 1.2f, -50.8f);
        _trainPath.Close();
        _ballPaint.Color = scarfTail;
        canvas.DrawPath(_trainPath, _ballPaint);

        _ballStrokePaint.Color = scarfStroke;
        _ballStrokePaint.StrokeWidth = 0.9f;
        canvas.DrawPath(_trainPath, _ballStrokePaint);

        // Scarf neck collar
        var neckRect = new SKRoundRect(new SKRect(-10.8f, -53.6f, 10.8f, -47.8f), 3.0f, 3.0f);
        _ballPaint.Color = scarfNeck;
        canvas.DrawRoundRect(neckRect, _ballPaint);
        _ballStrokePaint.Color = scarfStroke;
        _ballStrokePaint.StrokeWidth = 0.9f;
        canvas.DrawRoundRect(neckRect, _ballStrokePaint);

        // Highlight fold on neck scarf
        _ballPaint.Color = scarfHi;
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-8.5f, -52.6f, 7.5f, -50.8f), 1.0f, 1.0f), _ballPaint);

        // Scarf knot fold where tail emerges
        _ballPaint.Color = scarfKnot;
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(1.0f, -53.2f, 6.4f, -47.4f), 1.8f, 1.8f), _ballPaint);

        // 5. Face: 2 coal eyes (or cool shades!), curved 7-dot coal smile, round orange carrot nose.
        float eyeDx = m.LookX * 1.4f;
        float eyeDy = m.LookY * 1.0f;
        float noseDx = m.LookX * 1.8f;
        float noseDy = m.LookY * 1.2f;
        float smileDx = m.LookX * 0.9f;
        float smileDy = m.LookY * 0.6f;

        if (m.Outfit == SnowmanOutfit.Cool)
        {
            // Sleek dark sunglasses shifting subtly toward cursor
            float gx = eyeDx * 0.85f;
            float gy = -63.2f + eyeDy * 0.7f;
            _ballPaint.Color = new SKColor(18, 22, 30, 250);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(-8.4f + gx, gy - 2.4f, -0.8f + gx, gy + 2.5f), 1.4f, 1.4f), _ballPaint);
            canvas.DrawRoundRect(new SKRoundRect(new SKRect(0.8f + gx, gy - 2.4f, 8.4f + gx, gy + 2.5f), 1.4f, 1.4f), _ballPaint);
            _trainStrokePaint.Color = new SKColor(28, 32, 42, 245);
            _trainStrokePaint.StrokeWidth = 1.4f;
            canvas.DrawLine(-10.2f + gx, gy - 1.1f, 10.2f + gx, gy - 1.1f, _trainStrokePaint);

            // Crisp diagonal white lens shine
            _trainStrokePaint.Color = new SKColor(210, 236, 255, 195);
            _trainStrokePaint.StrokeWidth = 1.0f;
            canvas.DrawLine(-6.6f + gx, gy - 1.5f, -4.6f + gx, gy + 1.1f, _trainStrokePaint);
            canvas.DrawLine(2.6f + gx, gy - 1.5f, 4.6f + gx, gy + 1.1f, _trainStrokePaint);
        }
        else if (m.IsBlinking)
        {
            _trainStrokePaint.Color = new SKColor(22, 24, 28, 245);
            _trainStrokePaint.StrokeWidth = 1.4f;
            canvas.DrawLine(-5.5f + eyeDx, -63.0f + eyeDy, -2.5f + eyeDx, -63.0f + eyeDy, _trainStrokePaint);
            canvas.DrawLine(2.5f + eyeDx, -63.0f + eyeDy, 5.5f + eyeDx, -63.0f + eyeDy, _trainStrokePaint);
        }
        else
        {
            _ballPaint.Color = new SKColor(22, 24, 28, 250);
            canvas.DrawCircle(-4.0f + eyeDx, -63.0f + eyeDy, 1.65f, _ballPaint);
            canvas.DrawCircle(4.0f + eyeDx, -63.0f + eyeDy, 1.65f, _ballPaint);

            // Tiny white eye catchlights
            _ballPaint.Color = new SKColor(255, 255, 255, 220);
            canvas.DrawCircle(-4.5f + eyeDx * 1.1f, -63.5f + eyeDy * 1.1f, 0.48f, _ballPaint);
            canvas.DrawCircle(3.5f + eyeDx * 1.1f, -63.5f + eyeDy * 1.1f, 0.48f, _ballPaint);
        }

        // Curved coal smile (7 dots)
        _ballPaint.Color = new SKColor(22, 24, 28, 245);
        for (int i = 0; i < 7; i++)
        {
            float t = (i - 3) / 3f; // -1 .. +1
            float sx = t * 5.2f + smileDx;
            float sy = -56.4f - t * t * 1.6f + smileDy;
            canvas.DrawCircle(sx, sy, 0.85f, _ballPaint);
        }

        // Bright orange carrot nose
        _ballPaint.Color = new SKColor(218, 92, 16, 250);
        canvas.DrawCircle(0.6f + noseDx, -60.2f + noseDy, 2.25f, _ballPaint);
        _ballPaint.Color = new SKColor(246, 134, 34, 252);
        canvas.DrawCircle(0.2f + noseDx, -60.5f + noseDy, 1.95f, _ballPaint);
        _ballPaint.Color = new SKColor(255, 198, 120, 220);
        canvas.DrawCircle(-0.4f + noseDx, -61.2f + noseDy, 0.68f, _ballPaint);

        // 6. Outfit headwear (lifts/tilts cleanly when waving at train).
        canvas.Save();
        canvas.Translate(0f, -69.5f - m.HatLift);
        canvas.RotateDegrees(m.HatTilt);
        DrawSnowmanHeadwear(canvas, m.Outfit);
        canvas.Restore();

        canvas.Restore();
    }

    private void DrawSnowmanHeadwear(SKCanvas canvas, SnowmanOutfit outfit)
    {
        switch (outfit)
        {
            case SnowmanOutfit.CozyBeanie:
            {
                // Knit dome
                _trainPath.Reset();
                _trainPath.MoveTo(-11.2f, -2.0f);
                _trainPath.QuadTo(-10.5f, -15.5f, 0f, -16.8f);
                _trainPath.QuadTo(10.5f, -15.5f, 11.2f, -2.0f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(42, 126, 214, 252);
                canvas.DrawPath(_trainPath, _ballPaint);

                // Lighter knit stripe highlight
                _trainPath.Reset();
                _trainPath.MoveTo(-8.2f, -2.5f);
                _trainPath.QuadTo(-6.2f, -13.0f, -2.5f, -15.0f);
                _trainPath.QuadTo(-0.5f, -13.0f, -1.8f, -2.5f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(84, 162, 238, 160);
                canvas.DrawPath(_trainPath, _ballPaint);

                // Folded ribbed white/light cuff
                var brim = new SKRoundRect(new SKRect(-12.2f, -4.4f, 12.2f, 1.2f), 2.8f, 2.8f);
                _ballPaint.Color = new SKColor(225, 240, 255, 252);
                canvas.DrawRoundRect(brim, _ballPaint);
                _ballStrokePaint.Color = new SKColor(130, 174, 216, 185);
                _ballStrokePaint.StrokeWidth = 0.85f;
                canvas.DrawRoundRect(brim, _ballStrokePaint);

                // Fluffy pom-pom on top
                _ballPaint.Color = new SKColor(190, 214, 240, 230);
                canvas.DrawCircle(0.3f, -18.5f, 4.4f, _ballPaint);
                _ballPaint.Color = new SKColor(248, 252, 255, 255);
                canvas.DrawCircle(-0.2f, -19.0f, 3.8f, _ballPaint);
                break;
            }
            case SnowmanOutfit.Bucket:
            {
                canvas.Save();
                canvas.RotateDegrees(7.5f);

                // Tilted pewter/tin pail body
                _trainPath.Reset();
                _trainPath.MoveTo(-11.0f, 0.5f);
                _trainPath.LineTo(-8.2f, -15.8f);
                _trainPath.QuadTo(0f, -17.2f, 8.2f, -15.8f);
                _trainPath.LineTo(11.0f, 0.5f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(126, 144, 166, 250);
                canvas.DrawPath(_trainPath, _ballPaint);

                // Metallic sheen
                _trainPath.Reset();
                _trainPath.MoveTo(-7.2f, 0f);
                _trainPath.LineTo(-5.4f, -15.2f);
                _trainPath.LineTo(-2.0f, -15.5f);
                _trainPath.LineTo(-3.0f, 0f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(190, 210, 232, 165);
                canvas.DrawPath(_trainPath, _ballPaint);

                // Rim + top cap
                _ballPaint.Color = new SKColor(94, 110, 130, 252);
                canvas.DrawOval(0f, 0.5f, 11.6f, 2.2f, _ballPaint);
                _ballPaint.Color = new SKColor(156, 174, 196, 248);
                canvas.DrawOval(0f, -15.8f, 8.2f, 1.5f, _ballPaint);
                _ballStrokePaint.Color = new SKColor(72, 86, 104, 210);
                _ballStrokePaint.StrokeWidth = 0.85f;
                canvas.DrawOval(0f, 0.5f, 11.6f, 2.2f, _ballStrokePaint);

                canvas.Restore();
                break;
            }
            case SnowmanOutfit.Santa:
            {
                // Floppy festive red hat draping cleanly to the right
                _trainPath.Reset();
                _trainPath.MoveTo(-11.2f, -2.0f);
                _trainPath.QuadTo(-8.0f, -17.5f, 3.0f, -18.5f);
                _trainPath.QuadTo(13.5f, -19.2f, 15.8f, -7.2f);
                _trainPath.QuadTo(12.0f, -5.5f, 10.6f, -2.0f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(214, 32, 42, 252);
                canvas.DrawPath(_trainPath, _ballPaint);
                _ballStrokePaint.Color = new SKColor(148, 16, 22, 195);
                _ballStrokePaint.StrokeWidth = 0.85f;
                canvas.DrawPath(_trainPath, _ballStrokePaint);

                // Fluffy white cuff band
                var cuff = new SKRoundRect(new SKRect(-12.4f, -4.6f, 12.0f, 1.4f), 3.0f, 3.0f);
                _ballPaint.Color = new SKColor(246, 250, 255, 255);
                canvas.DrawRoundRect(cuff, _ballPaint);
                _ballStrokePaint.Color = new SKColor(170, 198, 228, 175);
                _ballStrokePaint.StrokeWidth = 0.8f;
                canvas.DrawRoundRect(cuff, _ballStrokePaint);

                // Fluffy white tail pom-pom
                _ballPaint.Color = new SKColor(195, 218, 242, 220);
                canvas.DrawCircle(16.2f, -6.4f, 3.8f, _ballPaint);
                _ballPaint.Color = new SKColor(250, 253, 255, 255);
                canvas.DrawCircle(15.8f, -6.9f, 3.3f, _ballPaint);
                break;
            }
            default:
            {
                // Classic / Cool sleek black cylindrical top hat
                _ballPaint.Color = new SKColor(26, 28, 34, 245);
                canvas.DrawOval(0f, 0f, 15.8f, 3.0f, _ballPaint);
                _ballStrokePaint.Color = new SKColor(72, 78, 88, 180);
                _ballStrokePaint.StrokeWidth = 0.8f;
                canvas.DrawOval(0f, 0f, 15.8f, 3.0f, _ballStrokePaint);

                _trainPath.Reset();
                _trainPath.MoveTo(-9.6f, 0f);
                _trainPath.LineTo(-11.2f, -17.0f);
                _trainPath.QuadTo(0f, -18.7f, 11.2f, -17.0f);
                _trainPath.LineTo(9.6f, 0f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(34, 36, 42, 252);
                canvas.DrawPath(_trainPath, _ballPaint);

                _trainPath.Reset();
                _trainPath.MoveTo(-7.2f, -0.5f);
                _trainPath.LineTo(-8.4f, -16.3f);
                _trainPath.LineTo(-4.0f, -16.7f);
                _trainPath.LineTo(-3.4f, -0.5f);
                _trainPath.Close();
                _ballPaint.Color = new SKColor(68, 72, 82, 165);
                canvas.DrawPath(_trainPath, _ballPaint);

                _ballPaint.Color = outfit == SnowmanOutfit.Cool
                    ? new SKColor(92, 72, 188, 245)
                    : new SKColor(18, 20, 24, 245);
                canvas.DrawRect(-9.7f, -4.1f, 19.4f, 3.8f, _ballPaint);

                _ballPaint.Color = new SKColor(52, 56, 64, 248);
                canvas.DrawOval(0f, -17.0f, 11.2f, 1.9f, _ballPaint);
                break;
            }
        }
    }

    private void DrawSnowmanSphere(SKCanvas canvas, float cx, float cy, float rx, float ry)
    {
        // Soft slate-blue outer contour so white snow reads cleanly against bright windows.
        _ballPaint.Color = new SKColor(135, 168, 204, 155);
        canvas.DrawOval(cx, cy + 0.9f, rx + 1.0f, ry + 1.0f, _ballPaint);

        // Base shaded cool-white snow volume.
        _ballPaint.Color = new SKColor(220, 234, 248, 252);
        canvas.DrawOval(cx, cy, rx, ry, _ballPaint);

        // Lit main upper-left snow volume.
        _ballPaint.Color = new SKColor(248, 252, 255, 252);
        canvas.DrawOval(cx - rx * 0.08f, cy - ry * 0.10f, rx * 0.90f, ry * 0.88f, _ballPaint);

        // Top-left bright highlight
        _ballPaint.Color = new SKColor(255, 255, 255, 230);
        canvas.DrawOval(cx - rx * 0.28f, cy - ry * 0.30f, rx * 0.38f, ry * 0.34f, _ballPaint);
    }

    private void DrawSnowmanButton(SKCanvas canvas, float x, float y, float r)
    {
        _ballPaint.Color = new SKColor(20, 22, 26, 250);
        canvas.DrawCircle(x, y, r, _ballPaint);
        _ballStrokePaint.Color = new SKColor(75, 82, 92, 200);
        _ballStrokePaint.StrokeWidth = 0.65f;
        canvas.DrawCircle(x, y, r * 0.65f, _ballStrokePaint);
    }

    private void DrawTwigArm(SKCanvas canvas, float side, float armAngle)
    {
        canvas.Save();
        canvas.Translate(side * 14.0f, -43.0f);
        canvas.RotateDegrees(side * armAngle);

        float ex = side * 18.0f;
        float ey = -12.0f;

        _trainStrokePaint.Color = new SKColor(82, 50, 28, 248);
        _trainStrokePaint.StrokeWidth = 2.4f;
        canvas.DrawLine(0f, 0f, ex, ey, _trainStrokePaint);

        // Forked little twig fingers
        _trainStrokePaint.StrokeWidth = 1.6f;
        canvas.DrawLine(ex, ey, ex + side * 6.5f, ey - 5.5f, _trainStrokePaint);
        canvas.DrawLine(ex, ey, ex + side * 7.5f, ey + 1.0f, _trainStrokePaint);
        canvas.DrawLine(ex - side * 2.5f, ey + 1.2f, ex + side * 1.5f, ey - 8.0f, _trainStrokePaint);

        canvas.Restore();
    }

    private void DrawPlayModeBadge(SKCanvas canvas, int viewportWidth)
    {
        const string text = "Режим снежков: ЛКМ — лепить / кинуть  •  ПКМ — сломать  •  F9 / Esc — выход";
        float textWidth = _hudTextPaint.MeasureText(text);
        float padX = 18f;
        float w = textWidth + padX * 2f;
        float h = 30f;
        float left = (viewportWidth - w) * 0.5f;
        float top = 12f;

        var rect = new SKRoundRect(new SKRect(left, top, left + w, top + h), 15f, 15f);
        canvas.DrawRoundRect(rect, _hudBgPaint);
        canvas.DrawRoundRect(rect, _hudBorderPaint);
        canvas.DrawText(text, viewportWidth * 0.5f, top + 19.5f, _hudTextPaint);
    }

    private void DrawSnowplowTrain(SKCanvas canvas, float plowX, float bottomY, SnowplowSnapshot train)
    {
        float wheelAngle = train.WheelAngle;
        float time = train.Time;

        canvas.Save();
        canvas.Translate(plowX, bottomY);
        canvas.Scale(train.Direction, 1f);

        // Local coordinates: train faces RIGHT (+X). Tip of snowplow blade is at x = 0, wheels sit on y = 0.

        // 0. Soft warm headlight beam in front of the train.
        _trainPath.Reset();
        _trainPath.MoveTo(-13f, -20f);
        _trainPath.LineTo(55f, -32f);
        _trainPath.LineTo(55f, -3f);
        _trainPath.LineTo(-13f, -15f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(255, 242, 175, 48);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // 1. Iron coupler bar between wagon and cab.
        _trainFillPaint.Color = new SKColor(38, 43, 50);
        canvas.DrawRect(-88f, -14f, 10f, 3.5f, _trainFillPaint);

        // 2. Rear tender / snow wagon (dark slate blue-gray with subtle vertical ribs).
        _trainFillPaint.Color = new SKColor(58, 76, 94);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-124f, -27f, -86f, -9f), 3f, 3f), _trainFillPaint);

        _trainFillPaint.Color = new SKColor(46, 61, 77);
        canvas.DrawRect(-117f, -25f, 3f, 14f, _trainFillPaint);
        canvas.DrawRect(-107f, -25f, 3f, 14f, _trainFillPaint);
        canvas.DrawRect(-97f, -25f, 3f, 14f, _trainFillPaint);

        // Snow pile on rear wagon roof.
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-125f, -32.5f, -85f, -25.5f), 3.5f, 3.5f), _trainFillPaint);

        // 3. Chimney (dark charcoal with flared rim + snow cap).
        _trainFillPaint.Color = new SKColor(42, 47, 54);
        canvas.DrawRect(-30f, -42f, 9f, 13f, _trainFillPaint);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-32.5f, -45.5f, -18.5f, -41f), 2f, 2f), _trainFillPaint);
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-32f, -47.5f, -19f, -44.2f), 1.6f, 1.6f), _trainFillPaint);

        // 4. Locomotive boiler (burgundy red + 2 golden vertical bands + snow layer).
        _trainFillPaint.Color = new SKColor(156, 44, 38);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-56f, -30f, -14f, -9f), 4f, 4f), _trainFillPaint);

        // Golden bands on boiler.
        _trainFillPaint.Color = new SKColor(219, 169, 72);
        canvas.DrawRect(-47f, -30f, 3.2f, 21f, _trainFillPaint);
        canvas.DrawRect(-33f, -30f, 3.2f, 21f, _trainFillPaint);

        // Snow layer on top of boiler.
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-56f, -33.5f, -19f, -28.5f), 2.5f, 2.5f), _trainFillPaint);

        // 5. Terracotta circular boiler nose & warm glowing headlight.
        _trainFillPaint.Color = new SKColor(172, 100, 76);
        canvas.DrawCircle(-16.5f, -19.5f, 7.2f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(255, 245, 198);
        canvas.DrawCircle(-13.2f, -19.5f, 3.6f, _trainFillPaint);

        // 6. Silver-gray angled snowplow blade at the front (down to y = 0).
        _trainPath.Reset();
        _trainPath.MoveTo(-18f, -17f);
        _trainPath.LineTo(-6f, -14f);
        _trainPath.LineTo(0f, 0f);
        _trainPath.LineTo(-20f, 0f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(168, 180, 194);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // Inner bevel shading on snowplow blade.
        _trainPath.Reset();
        _trainPath.MoveTo(-9f, -11.5f);
        _trainPath.LineTo(-1.5f, 0f);
        _trainPath.LineTo(-12f, 0f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(126, 138, 152);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // 7. Locomotive cab (deep burgundy + overhanging roof + snow cap + glowing window).
        _trainFillPaint.Color = new SKColor(120, 31, 28);
        canvas.DrawRect(-80f, -48f, 26f, 39f, _trainFillPaint);

        // Roof ledge.
        _trainFillPaint.Color = new SKColor(94, 24, 21);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-83f, -51.5f, -51f, -47f), 1.5f, 1.5f), _trainFillPaint);

        // Snow cap on cab roof.
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-83.5f, -56f, -50.5f, -50.5f), 2.8f, 2.8f), _trainFillPaint);

        // Glowing cab window with golden frame.
        _trainFillPaint.Color = new SKColor(220, 168, 70);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-74f, -42.5f, -60f, -28.5f), 2.5f, 2.5f), _trainFillPaint);
        _trainFillPaint.Color = new SKColor(255, 234, 162);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-71.8f, -40.3f, -62.2f, -30.7f), 1.5f, 1.5f), _trainFillPaint);

        // 8. Rotating spoked wheels resting on taskbar (y = 0).
        DrawTrainWheel(canvas, -113f, -6.5f, 6.5f, wheelAngle);
        DrawTrainWheel(canvas, -96f, -6.5f, 6.5f, wheelAngle);
        DrawTrainWheel(canvas, -68f, -5.5f, 5.5f, wheelAngle * 1.15f);
        DrawTrainWheel(canvas, -46f, -8.0f, 8.0f, wheelAngle);
        DrawTrainWheel(canvas, -28f, -8.0f, 8.0f, wheelAngle);

        // 9. Optional coupled festive wagons extending behind the tender (x = -124 -> leftward).
        float tailX = -124f;
        if (train.ShowSantaWagon)
        {
            tailX = DrawFestiveCarriageWagon(canvas, tailX, 82f, wheelAngle, time, isTreeWagon: false);
        }
        if (train.ShowTreeWagon)
        {
            DrawFestiveCarriageWagon(canvas, tailX, 74f, wheelAngle, time, isTreeWagon: true);
        }

        canvas.Restore();
    }

    private float DrawFestiveCarriageWagon(
        SKCanvas canvas,
        float connectX,
        float width,
        float wheelAngle,
        float time,
        bool isTreeWagon)
    {
        // Iron coupler bar linking to preceding car.
        _trainFillPaint.Color = new SKColor(38, 43, 50);
        canvas.DrawRect(connectX - 9f, -14f, 10f, 3.5f, _trainFillPaint);

        float rightX = connectX - 7f;
        float leftX = rightX - width;
        float centerX = (leftX + rightX) * 0.5f;
        const float deckY = -14f;

        // Rear backrest / end wooden pillars behind characters & tree.
        _trainFillPaint.Color = isTreeWagon ? new SKColor(22, 58, 38) : new SKColor(88, 22, 28);
        canvas.DrawRect(leftX + 1f, -25f, width - 2f, 13f, _trainFillPaint);

        // Tall corner end posts + cozy glowing carriage lanterns.
        SKColor pillarColor = new(56, 40, 34);
        DrawCarriageLanternPost(canvas, leftX + 2.5f, pillarColor, time, 0.0f);
        DrawCarriageLanternPost(canvas, rightX - 2.5f, pillarColor, time, 1.7f);

        // Interior cargo (Ded Moroz + Snegurochka OR decorated tree + stacked gifts).
        if (isTreeWagon)
        {
            DrawWagonTreeAndGifts(canvas, centerX, deckY, time);
        }
        else
        {
            DrawWagonSantaAndSnegurochka(canvas, centerX, deckY, time);
        }

        // Front decorative open-gondola side panel overlapping bottom edge cleanly.
        _trainFillPaint.Color = isTreeWagon ? new SKColor(32, 86, 56) : new SKColor(138, 34, 42);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(leftX, -20.5f, rightX, -9f), 2.5f, 2.5f), _trainFillPaint);

        // Golden trim stripe & dark steel undercarriage beam.
        _trainFillPaint.Color = new SKColor(222, 174, 72);
        canvas.DrawRect(leftX + 2f, -18.5f, width - 4f, 2.2f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(36, 42, 50);
        canvas.DrawRect(leftX - 1f, -11.5f, width + 2f, 3.0f, _trainFillPaint);

        // Pine garland swag & snow dusting along top front rail.
        _trainStrokePaint.Color = new SKColor(34, 110, 56);
        _trainStrokePaint.StrokeWidth = 2.4f;
        int swags = 3;
        float swagW = (width - 8f) / swags;
        for (int s = 0; s < swags; s++)
        {
            float sx0 = leftX + 4f + s * swagW;
            float sx1 = sx0 + swagW;
            _trainPath.Reset();
            _trainPath.MoveTo(sx0, -19.8f);
            _trainPath.QuadTo((sx0 + sx1) * 0.5f, -13.2f, sx1, -19.8f);
            canvas.DrawPath(_trainPath, _trainStrokePaint);

            _trainFillPaint.Color = new SKColor(220, 44, 52);
            canvas.DrawCircle((sx0 + sx1) * 0.5f, -16.2f, 1.5f, _trainFillPaint);
        }

        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(leftX - 1f, -21.8f, rightX + 1f, -19.4f), 1.4f, 1.4f), _trainFillPaint);

        // Two rotating spoked wheels resting on taskbar (y = 0).
        DrawTrainWheel(canvas, leftX + 14f, -6.5f, 6.5f, wheelAngle);
        DrawTrainWheel(canvas, rightX - 14f, -6.5f, 6.5f, wheelAngle);

        return leftX;
    }

    private void DrawCarriageLanternPost(SKCanvas canvas, float postX, SKColor pillarColor, float time, float phase)
    {
        _trainFillPaint.Color = pillarColor;
        canvas.DrawRect(postX - 2f, -29f, 4f, 18f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(208, 162, 68);
        canvas.DrawRect(postX - 2.6f, -31.5f, 5.2f, 2.5f, _trainFillPaint);

        float pulse = 0.85f + 0.15f * MathF.Sin(time * 4.5f + phase);
        _trainFillPaint.Color = new SKColor(255, 224, 128, (byte)(52 * pulse));
        canvas.DrawCircle(postX, -33.5f, 6.5f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(255, 240, 175, 245);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(postX - 2f, -35.8f, postX + 2f, -31.5f), 1f, 1f), _trainFillPaint);
    }

    private void DrawWagonSantaAndSnegurochka(SKCanvas canvas, float cx, float floorY, float time)
    {
        // --- 1. Snegurochka (left side of Ded Moroz: x = cx - 17) ---
        float sx = cx - 17f;

        // Golden braid falling down left shoulder
        _trainStrokePaint.Color = new SKColor(235, 192, 82);
        _trainStrokePaint.StrokeWidth = 2.6f;
        canvas.DrawLine(sx - 3.6f, floorY - 26f, sx - 6.2f, floorY - 15f, _trainStrokePaint);
        _trainFillPaint.Color = new SKColor(80, 175, 245);
        canvas.DrawCircle(sx - 6.3f, floorY - 14.5f, 1.4f, _trainFillPaint);

        // Azure winter coat (shuba)
        _trainPath.Reset();
        _trainPath.MoveTo(sx - 8.5f, floorY - 3f);
        _trainPath.LineTo(sx - 5.2f, floorY - 23.5f);
        _trainPath.LineTo(sx + 5.2f, floorY - 23.5f);
        _trainPath.LineTo(sx + 8.5f, floorY - 3f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(52, 146, 224);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // White fur vertical trim + collar
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRect(sx - 1.5f, floorY - 23.5f, 3f, 20f, _trainFillPaint);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(sx - 5.8f, floorY - 25.0f, sx + 5.8f, floorY - 21.8f), 1.8f, 1.8f), _trainFillPaint);

        // Sleeves & white mittens
        _trainStrokePaint.Color = new SKColor(46, 134, 208);
        _trainStrokePaint.StrokeWidth = 3.4f;
        canvas.DrawLine(sx - 4.8f, floorY - 21.5f, sx - 8.6f, floorY - 14.5f, _trainStrokePaint);
        canvas.DrawLine(sx + 4.8f, floorY - 21.5f, sx + 8.2f, floorY - 15.0f, _trainStrokePaint);
        _trainFillPaint.Color = new SKColor(244, 250, 255);
        canvas.DrawCircle(sx - 8.8f, floorY - 14.2f, 2.0f, _trainFillPaint);
        canvas.DrawCircle(sx + 8.4f, floorY - 14.6f, 2.0f, _trainFillPaint);

        // Face
        _trainFillPaint.Color = new SKColor(255, 224, 200);
        canvas.DrawCircle(sx, floorY - 28.5f, 4.5f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(245, 148, 152, 145);
        canvas.DrawCircle(sx - 2.3f, floorY - 27.6f, 1.1f, _trainFillPaint);
        canvas.DrawCircle(sx + 2.3f, floorY - 27.6f, 1.1f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(34, 46, 68);
        canvas.DrawCircle(sx - 1.5f, floorY - 29.0f, 0.85f, _trainFillPaint);
        canvas.DrawCircle(sx + 1.5f, floorY - 29.0f, 0.85f, _trainFillPaint);

        // Iconic festive blue kokoshnik crown with silver-white pearl trim
        _trainPath.Reset();
        _trainPath.MoveTo(sx - 6.0f, floorY - 31.2f);
        _trainPath.QuadTo(sx, floorY - 40.2f, sx + 6.0f, floorY - 31.2f);
        _trainPath.QuadTo(sx, floorY - 33.4f, sx - 6.0f, floorY - 31.2f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(36, 118, 204);
        canvas.DrawPath(_trainPath, _trainFillPaint);
        _trainStrokePaint.Color = new SKColor(235, 246, 255);
        _trainStrokePaint.StrokeWidth = 1.2f;
        canvas.DrawPath(_trainPath, _trainStrokePaint);
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(sx - 5.2f, floorY - 32.8f, sx + 5.2f, floorY - 30.6f), 1.4f, 1.4f), _trainFillPaint);

        // --- 2. Ded Moroz with magic staff (right side: x = cx + 15) ---
        float dx = cx + 15f;

        // Long crimson boyar winter coat
        _trainPath.Reset();
        _trainPath.MoveTo(dx - 10.5f, floorY - 2f);
        _trainPath.LineTo(dx - 6.8f, floorY - 26f);
        _trainPath.LineTo(dx + 6.8f, floorY - 26f);
        _trainPath.LineTo(dx + 10.5f, floorY - 2f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(196, 32, 42);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // White fur vertical trim, collar, and golden belt sash
        _trainFillPaint.Color = new SKColor(246, 251, 255);
        canvas.DrawRect(dx - 1.8f, floorY - 26f, 3.6f, 23f, _trainFillPaint);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(dx - 7.0f, floorY - 27.5f, dx + 7.0f, floorY - 24f), 2f, 2f), _trainFillPaint);
        _trainFillPaint.Color = new SKColor(226, 178, 66);
        canvas.DrawRect(dx - 8.2f, floorY - 14.5f, 16.4f, 2.2f, _trainFillPaint);

        // Left waving arm + right arm holding the staff
        float waveY = floorY - 28.5f + MathF.Sin(time * 5.0f) * 1.4f;
        _trainStrokePaint.Color = new SKColor(182, 28, 38);
        _trainStrokePaint.StrokeWidth = 3.8f;
        canvas.DrawLine(dx - 6.2f, floorY - 23f, dx - 11.0f, waveY, _trainStrokePaint);
        canvas.DrawLine(dx + 6.2f, floorY - 23f, dx + 11.8f, floorY - 18f, _trainStrokePaint);
        _trainFillPaint.Color = new SKColor(245, 250, 255);
        canvas.DrawCircle(dx - 11.2f, waveY, 2.2f, _trainFillPaint);
        canvas.DrawCircle(dx + 12.0f, floorY - 17.8f, 2.2f, _trainFillPaint);

        // Rosy face
        _trainFillPaint.Color = new SKColor(255, 218, 192);
        canvas.DrawCircle(dx, floorY - 31.2f, 5.0f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(32, 40, 56);
        canvas.DrawCircle(dx - 1.7f, floorY - 32.2f, 0.9f, _trainFillPaint);
        canvas.DrawCircle(dx + 1.7f, floorY - 32.2f, 0.9f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(224, 92, 92);
        canvas.DrawCircle(dx, floorY - 30.6f, 1.25f, _trainFillPaint);

        // Lush flowing white beard
        _trainPath.Reset();
        _trainPath.MoveTo(dx - 5.4f, floorY - 30.2f);
        _trainPath.QuadTo(dx - 6.2f, floorY - 21f, dx, floorY - 16.5f);
        _trainPath.QuadTo(dx + 6.2f, floorY - 21f, dx + 5.4f, floorY - 30.2f);
        _trainPath.QuadTo(dx, floorY - 28.0f, dx - 5.4f, floorY - 30.2f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(248, 252, 255);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // Crimson boyar fur hat
        _trainPath.Reset();
        _trainPath.MoveTo(dx - 5.6f, floorY - 34.5f);
        _trainPath.QuadTo(dx, floorY - 43.2f, dx + 5.6f, floorY - 34.5f);
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(196, 32, 42);
        canvas.DrawPath(_trainPath, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(248, 252, 255);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(dx - 6.2f, floorY - 36.2f, dx + 6.2f, floorY - 33.2f), 2f, 2f), _trainFillPaint);

        // Magic crystal staff (posoh) held slightly forward
        float staffBaseX = dx + 11.2f;
        float staffTopX = dx + 14.2f;
        float staffTopY = floorY - 43.5f;
        _trainStrokePaint.Color = new SKColor(220, 178, 76);
        _trainStrokePaint.StrokeWidth = 2.3f;
        canvas.DrawLine(staffBaseX, floorY - 2f, staffTopX, staffTopY, _trainStrokePaint);
        _trainStrokePaint.Color = new SKColor(168, 224, 255);
        _trainStrokePaint.StrokeWidth = 1.0f;
        canvas.DrawLine(staffBaseX + 0.5f, floorY - 11f, staffTopX - 0.8f, staffTopY + 5f, _trainStrokePaint);

        float orbGlow = 0.78f + 0.22f * MathF.Sin(time * 4.2f);
        _trainFillPaint.Color = new SKColor(125, 222, 255, (byte)(95 * orbGlow));
        canvas.DrawCircle(staffTopX, staffTopY - 1.5f, 5.4f, _trainFillPaint);
        _trainFillPaint.Color = new SKColor(215, 246, 255);
        canvas.DrawCircle(staffTopX, staffTopY - 1.5f, 2.7f, _trainFillPaint);
        _trainFillPaint.Color = SKColors.White;
        canvas.DrawCircle(staffTopX - 0.6f, staffTopY - 2.1f, 1.0f, _trainFillPaint);
    }

    private void DrawWagonTreeAndGifts(SKCanvas canvas, float cx, float floorY, float time)
    {
        float tx = cx - 3f;

        // 1. Tree trunk
        _trainFillPaint.Color = new SKColor(102, 66, 38);
        canvas.DrawRect(tx - 2.4f, floorY - 7f, 4.8f, 6f, _trainFillPaint);

        // 2. Four lush spruce tiers + gentle snow patches on boughs
        ReadOnlySpan<(float TopY, float BottomY, float HalfW, byte G)> tiers = stackalloc (float, float, float, byte)[]
        {
            (-23f, -5.5f, 17.5f, 106),
            (-30f, -15.5f, 14.5f, 120),
            (-36.5f, -24.5f, 11.0f, 134),
            (-42.5f, -32.5f, 7.2f, 148)
        };
        for (int t = 0; t < tiers.Length; t++)
        {
            var tier = tiers[t];
            _trainPath.Reset();
            _trainPath.MoveTo(tx, floorY + tier.TopY);
            _trainPath.LineTo(tx + tier.HalfW, floorY + tier.BottomY);
            _trainPath.QuadTo(tx, floorY + tier.BottomY + 2.2f, tx - tier.HalfW, floorY + tier.BottomY);
            _trainPath.Close();
            _trainFillPaint.Color = new SKColor(28, tier.G, 58);
            canvas.DrawPath(_trainPath, _trainFillPaint);

            // Soft snow dusting on left/right branch tips
            _trainFillPaint.Color = new SKColor(238, 248, 255, 210);
            canvas.DrawCircle(tx - tier.HalfW * 0.72f, floorY + tier.BottomY - 1.2f, 1.6f, _trainFillPaint);
            canvas.DrawCircle(tx + tier.HalfW * 0.72f, floorY + tier.BottomY - 1.2f, 1.6f, _trainFillPaint);
        }

        // 3. Golden 5-point star on the tree top
        float starY = floorY - 44.2f;
        _trainFillPaint.Color = new SKColor(255, 224, 92, 70);
        canvas.DrawCircle(tx, starY, 6.0f, _trainFillPaint);
        _trainPath.Reset();
        for (int p = 0; p < 10; p++)
        {
            float angle = -MathF.PI * 0.5f + p * (MathF.PI / 5f);
            float r = (p & 1) == 0 ? 4.3f : 1.9f;
            float px = tx + MathF.Cos(angle) * r;
            float py = starY + MathF.Sin(angle) * r;
            if (p == 0) _trainPath.MoveTo(px, py);
            else _trainPath.LineTo(px, py);
        }
        _trainPath.Close();
        _trainFillPaint.Color = new SKColor(255, 214, 68);
        canvas.DrawPath(_trainPath, _trainFillPaint);

        // 4. Twinkling colorful ornaments & garland lights
        ReadOnlySpan<(float Dx, float Dy, byte R, byte G, byte B)> ornaments = stackalloc (float, float, byte, byte, byte)[]
        {
            (-2.5f, -35.0f, 238, 66, 72),
            (3.2f, -31.5f, 255, 214, 72),
            (-5.8f, -26.5f, 84, 206, 255),
            (1.2f, -24.5f, 240, 96, 178),
            (7.4f, -21.0f, 238, 66, 72),
            (-9.2f, -17.5f, 255, 214, 72),
            (-1.5f, -16.0f, 102, 232, 168),
            (9.6f, -14.2f, 84, 206, 255)
        };
        for (int o = 0; o < ornaments.Length; o++)
        {
            var orn = ornaments[o];
            float blink = 0.65f + 0.35f * MathF.Sin(time * 5.2f + o * 1.15f);
            _trainFillPaint.Color = new SKColor(orn.R, orn.G, orn.B, (byte)(255 * blink));
            canvas.DrawCircle(tx + orn.Dx, floorY + orn.Dy, 1.85f, _trainFillPaint);
        }

        // 5. Wrapped gift boxes tucked under & beside the tree (visible above the front wagon rail at -6.5 above deck)
        DrawGiftBox(canvas, cx - 22.5f, floorY - 3.5f, 10.5f, 10.5f, new SKColor(214, 48, 56), new SKColor(246, 204, 72));
        DrawGiftBox(canvas, cx - 18.5f, floorY - 14.0f, 8.0f, 7.2f, new SKColor(242, 186, 56), new SKColor(208, 42, 48));
        DrawGiftBox(canvas, cx + 18.0f, floorY - 3.5f, 12.0f, 11.5f, new SKColor(52, 168, 218), new SKColor(244, 250, 255));
        DrawGiftBox(canvas, cx + 19.5f, floorY - 15.0f, 8.5f, 7.5f, new SKColor(152, 86, 212), new SKColor(248, 210, 82));
    }

    private void DrawGiftBox(
        SKCanvas canvas,
        float centerX,
        float bottomY,
        float width,
        float height,
        SKColor boxColor,
        SKColor ribbonColor)
    {
        float x0 = centerX - width * 0.5f;
        float y0 = bottomY - height;
        _trainFillPaint.Color = boxColor;
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(x0, y0, x0 + width, bottomY), 1.3f, 1.3f), _trainFillPaint);

        // Cross ribbons
        _trainFillPaint.Color = ribbonColor;
        canvas.DrawRect(centerX - 1.2f, y0, 2.4f, height, _trainFillPaint);
        canvas.DrawRect(x0, y0 + height * 0.44f, width, 2.1f, _trainFillPaint);

        // Ribbon bow loops on top
        _trainStrokePaint.Color = ribbonColor;
        _trainStrokePaint.StrokeWidth = 1.3f;
        canvas.DrawCircle(centerX - 1.8f, y0 - 1.3f, 1.5f, _trainStrokePaint);
        canvas.DrawCircle(centerX + 1.8f, y0 - 1.3f, 1.5f, _trainStrokePaint);
    }

    private void DrawTrainWheel(SKCanvas canvas, float cx, float cy, float radius, float angleDeg)
    {
        // Outer dark tire
        _trainFillPaint.Color = new SKColor(34, 39, 46);
        canvas.DrawCircle(cx, cy, radius, _trainFillPaint);

        // Inner dark gray rim
        _trainFillPaint.Color = new SKColor(58, 65, 75);
        canvas.DrawCircle(cx, cy, radius * 0.72f, _trainFillPaint);

        // Rotating spokes
        _trainStrokePaint.Color = new SKColor(126, 136, 148);
        _trainStrokePaint.StrokeWidth = 1.3f;
        float baseRad = angleDeg * MathF.PI / 180f;
        float spokeR = radius * 0.68f;
        for (int i = 0; i < 3; i++)
        {
            float a = baseRad + i * (MathF.PI / 3f);
            float dx = MathF.Cos(a) * spokeR;
            float dy = MathF.Sin(a) * spokeR;
            canvas.DrawLine(cx - dx, cy - dy, cx + dx, cy + dy, _trainStrokePaint);
        }

        // Center hub
        _trainFillPaint.Color = new SKColor(175, 184, 196);
        canvas.DrawCircle(cx, cy, radius * 0.22f, _trainFillPaint);
    }

    private static readonly (byte R, byte G, byte B)[] GarlandPalette = new (byte, byte, byte)[]
    {
        (255, 60, 60),    // Ruby Red
        (255, 195, 45),   // Warm Golden Amber
        (46, 218, 112),   // Vivid Emerald
        (56, 178, 255),   // Sapphire Blue
        (238, 80, 210),   // Orchid Purple
        (255, 245, 195),  // Warm Candle Glow
        (45, 235, 215)    // Ice Cyan
    };

    private void DrawGarland(SKCanvas canvas, float width, float time)
    {
        if (width <= 10f)
            return;

        const float wireAnchorY = 0.5f;
        float curX = 0f;
        int swagIdx = 0;
        int globalBulbCount = 0;

        while (curX < width)
        {
            float span = 125f + ((swagIdx * 53) % 45);
            if (curX + span > width - 40f)
                span = width - curX;

            float nextX = Math.Min(width, curX + span);
            float depth = 13f + ((swagIdx * 37) % 11) + (swagIdx % 2 == 1 ? 5f : 0f);
            float midOff = (((swagIdx * 19) % 20) - 10f) * 0.45f;
            float midX = (curX + nextX) * 0.5f + midOff;
            float ctrlY = wireAnchorY + depth * 1.85f;

            // 1. Wire arch for this swag
            _garlandPath.Reset();
            _garlandPath.MoveTo(curX, wireAnchorY);
            _garlandPath.QuadTo(midX, ctrlY, nextX, wireAnchorY);
            canvas.DrawPath(_garlandPath, _garlandWirePaint);

            // 2. Delicate pine needles branching off the sagging wire
            int sprigCount = Math.Max(2, (int)((nextX - curX) / 40f));
            for (int s = 1; s <= sprigCount; s++)
            {
                float st = (float)s / (sprigCount + 1);
                float sx = (1 - st) * (1 - st) * curX + 2 * (1 - st) * st * midX + st * st * nextX;
                float sy = (1 - st) * (1 - st) * wireAnchorY + 2 * (1 - st) * st * ctrlY + st * st * wireAnchorY;

                _garlandNeedlePaint.StrokeWidth = 1.1f;
                _garlandNeedlePaint.Color = new SKColor(22, 54, 30);
                canvas.DrawLine(sx, sy, sx - 2.5f, sy + 3.8f, _garlandNeedlePaint);
                _garlandNeedlePaint.Color = new SKColor(38, 86, 48);
                canvas.DrawLine(sx, sy, sx + 2.2f, sy + 4.2f, _garlandNeedlePaint);
            }

            // 3. Top anchor sprig & frosted berries at curX
            DrawAnchorSprig(canvas, curX, wireAnchorY, swagIdx);

            // 4. Festive bulbs: rhythmically spaced with dangling drops & gentle sway
            float swagW = nextX - curX;
            int bulbCount = Math.Max(3, (int)Math.Round(swagW / 42f));
            for (int b = 0; b < bulbCount; b++)
            {
                float t = (b + 0.6f) / (bulbCount + 0.2f);
                float bx = (1 - t) * (1 - t) * curX + 2 * (1 - t) * t * midX + t * t * nextX;
                float by = (1 - t) * (1 - t) * wireAnchorY + 2 * (1 - t) * t * ctrlY + t * t * wireAnchorY;

                float dx = 2 * (1 - t) * (midX - curX) + 2 * t * (nextX - midX);
                float dy = 2 * (1 - t) * (ctrlY - wireAnchorY) + 2 * t * (wireAnchorY - ctrlY);
                float wireAngle = MathF.Atan2(dy, dx);

                int bulbId = globalBulbCount++;
                uint seed = unchecked((uint)(bulbId * 2654435761u ^ (uint)swagIdx * 131));

                // Natural icicle drop rhythm: center drops lowest, adjacent drops higher
                int dropSlot = b % 3;
                float dropLen = dropSlot switch
                {
                    1 => 11.5f + (seed % 5),
                    2 => 5.5f + (seed % 4),
                    _ => 0f
                };

                float swayFreq = 1.4f + (seed % 5) * 0.22f;
                float swayPhase = bulbId * 0.95f;
                float swayAngle = MathF.Sin(time * swayFreq + swayPhase) * (0.04f + dropLen * 0.0035f);

                int shapeType = (int)((seed >> 6) % 5);
                if (shapeType > 2) shapeType = 0;

                var col = GarlandPalette[bulbId % GarlandPalette.Length];

                float twSpeed = 2.4f + (seed % 4) * 0.5f;
                float twPulse = 0.55f + 0.45f * MathF.Sin(time * twSpeed + swayPhase);
                float wave = 0.5f + 0.5f * MathF.Sin(time * 1.3f - bx * 0.006f);
                float intensity = Math.Clamp(0.50f + 0.36f * twPulse + 0.14f * wave, 0.38f, 1.0f);

                DrawFestiveBulb(canvas, bx, by, dropLen, wireAngle, swayAngle, col, intensity, shapeType);
            }

            curX = nextX;
            swagIdx++;
        }

        // Final right-edge anchor sprig
        DrawAnchorSprig(canvas, width, wireAnchorY, swagIdx);
    }

    private void DrawAnchorSprig(SKCanvas canvas, float x, float y, int idx)
    {
        _garlandNeedlePaint.StrokeWidth = 1.25f;
        int count = 6;
        for (int n = 0; n < count; n++)
        {
            float ang = MathF.PI * 0.5f + (n - (count - 1) * 0.5f) * 0.30f + MathF.Sin(idx * 4 + n) * 0.06f;
            float len = 6.0f + ((n * 11 + idx * 7) % 4);
            float ex = x + MathF.Cos(ang) * len;
            float ey = y + MathF.Sin(ang) * len;

            _garlandNeedlePaint.Color = (n % 2 == 0) ? new SKColor(16, 46, 24) : new SKColor(32, 82, 45);
            canvas.DrawLine(x, y, ex, ey, _garlandNeedlePaint);

            if ((n + idx) % 2 == 0)
            {
                _garlandNeedlePaint.Color = new SKColor(235, 248, 255, 220);
                canvas.DrawLine(ex - MathF.Cos(ang) * 1.5f, ey - MathF.Sin(ang) * 1.5f, ex, ey, _garlandNeedlePaint);
            }
        }

        if (idx % 2 == 0)
        {
            _garlandBulbPaint.Color = new SKColor(215, 34, 42);
            canvas.DrawCircle(x - 1.8f, y + 2.2f, 1.7f, _garlandBulbPaint);
            canvas.DrawCircle(x + 1.6f, y + 2.6f, 1.5f, _garlandBulbPaint);
            _garlandBulbPaint.Color = new SKColor(255, 190, 200, 230);
            canvas.DrawCircle(x - 2.2f, y + 1.6f, 0.55f, _garlandBulbPaint);
        }

        _garlandBulbPaint.Color = new SKColor(245, 252, 255, 240);
        canvas.DrawCircle(x, y + 0.6f, 1.4f, _garlandBulbPaint);
    }

    private void DrawFestiveBulb(
        SKCanvas canvas,
        float wireX,
        float wireY,
        float dropLen,
        float wireAngle,
        float swayAngle,
        (byte R, byte G, byte B) col,
        float intensity,
        int shapeType)
    {
        canvas.Save();

        float bulbX = wireX;
        float bulbY = wireY;

        if (dropLen > 0.5f)
        {
            float cos = MathF.Cos(MathF.PI * 0.5f + swayAngle);
            float sin = MathF.Sin(MathF.PI * 0.5f + swayAngle);
            float endX = wireX + cos * dropLen;
            float endY = wireY + sin * dropLen;

            _garlandNeedlePaint.Color = new SKColor(20, 44, 26, 210);
            _garlandNeedlePaint.StrokeWidth = 0.95f;
            canvas.DrawLine(wireX, wireY, endX, endY, _garlandNeedlePaint);

            bulbX = endX;
            bulbY = endY;
            canvas.Translate(bulbX, bulbY);
            canvas.RotateRadians(swayAngle);
        }
        else
        {
            canvas.Translate(bulbX, bulbY);
            canvas.RotateRadians(wireAngle * 0.32f + swayAngle);
        }

        // Socket (цоколь)
        _garlandBulbPaint.Color = new SKColor(20, 36, 24);
        canvas.DrawRoundRect(new SKRoundRect(new SKRect(-1.35f, 0f, 1.35f, 2.4f), 0.5f, 0.5f), _garlandBulbPaint);
        _garlandBulbPaint.Color = new SKColor(175, 145, 68, 220);
        canvas.DrawRect(-1.15f, 1.7f, 2.3f, 0.65f, _garlandBulbPaint);

        float centerY = 5.8f;

        // Smooth multi-step soft glow
        float maxR = 12.0f + 3.0f * intensity;
        _garlandGlowPaint.Color = new SKColor(col.R, col.G, col.B, (byte)(11f * intensity));
        canvas.DrawCircle(0f, centerY, maxR, _garlandGlowPaint);

        _garlandGlowPaint.Color = new SKColor(col.R, col.G, col.B, (byte)(24f * intensity));
        canvas.DrawCircle(0f, centerY, maxR * 0.65f, _garlandGlowPaint);

        _garlandGlowPaint.Color = new SKColor(col.R, col.G, col.B, (byte)(45f * intensity));
        canvas.DrawCircle(0f, centerY, maxR * 0.38f, _garlandGlowPaint);

        byte bodyAlpha = (byte)(215 + 40 * intensity);
        _garlandBulbPaint.Color = new SKColor(col.R, col.G, col.B, bodyAlpha);

        if (shapeType == 1)
        {
            // Globe / Round Sphere
            canvas.DrawCircle(0f, centerY, 3.1f, _garlandBulbPaint);
            _garlandBulbPaint.Color = new SKColor(255, 255, 255, (byte)(160 + 95 * intensity));
            canvas.DrawCircle(-0.85f, centerY - 0.85f, 0.85f, _garlandBulbPaint);
        }
        else if (shapeType == 2)
        {
            // Fairy Star / Diamond Sparkle
            canvas.Save();
            canvas.Translate(0f, centerY);
            canvas.DrawPath(_garlandStarPath, _garlandBulbPaint);
            canvas.Restore();

            _garlandBulbPaint.Color = new SKColor(255, 255, 255, (byte)(210 + 45 * intensity));
            canvas.DrawCircle(0f, centerY, 1.1f, _garlandBulbPaint);
        }
        else
        {
            // Classic Vintage C9 Teardrop
            canvas.DrawPath(_garlandBulbPath, _garlandBulbPaint);

            _garlandBulbPaint.Color = new SKColor(255, 255, 255, (byte)(175 + 80 * intensity));
            canvas.DrawCircle(-0.65f, centerY - 0.9f, 0.85f, _garlandBulbPaint);
        }

        canvas.Restore();
    }
}
