using System;
using System.Diagnostics;
using RPGGame;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Short radial push that auto-reforms. Action-word glyphs inside an exclude rect stay rigid.
    /// Honors <see cref="DeveloperModeState.AreDistortionEffectsEnabled"/> (F6) plus config.
    /// Hot path: <see cref="BeginFrame"/> once per paint, then <see cref="Sample"/> (lock-free).
    /// </summary>
    public sealed class TextEmphasisPulseField
    {
        private readonly object _lock = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private double? _testNowSeconds;

        private TextEmphasisPulseConfig _config = new();
        private bool _active;
        private double _startSeconds;
        private double _originX;
        private double _originY;
        private double _excludeMinX;
        private double _excludeMaxX;
        private double _excludeMinY;
        private double _excludeMaxY;

        // Per-paint snapshot
        private bool _frameActive;
        private TextEmphasisPulseConfig _frameConfig = new();
        private double _frameNow;
        private double _frameStartSeconds;
        private double _frameOriginX;
        private double _frameOriginY;
        private double _frameExcludeMinX;
        private double _frameExcludeMaxX;
        private double _frameExcludeMinY;
        private double _frameExcludeMaxY;
        private double _frameRadiusPx;
        private double _frameWakeMinX;
        private double _frameWakeMaxX;
        private double _frameWakeMinY;
        private double _frameWakeMaxY;
        private bool _frameHasBounds;

        public TextEmphasisPulseConfig Config
        {
            get
            {
                lock (_lock)
                    return _config;
            }
        }

        public void ApplyConfig(TextEmphasisPulseConfig? config)
        {
            lock (_lock)
            {
                _config = config ?? new TextEmphasisPulseConfig();
            }
        }

        public bool IsActive
        {
            get
            {
                if (_frameActive)
                    return true;

                lock (_lock)
                {
                    if (!IsEffectivelyEnabledUnlocked())
                        return false;
                    double now = NowUnlocked();
                    return IsAliveUnlocked(now);
                }
            }
        }

        public bool FrameActive => _frameActive;

        public TextEmphasisPulseConfig FrameConfig => _frameConfig;

        public bool BeginFrame(double charWidth, double charHeight)
        {
            lock (_lock)
            {
                _frameActive = false;
                _frameHasBounds = false;

                if (!IsEffectivelyEnabledUnlocked() || charWidth <= 0 || charHeight <= 0)
                    return false;

                double now = NowUnlocked();
                if (!IsAliveUnlocked(now))
                {
                    _active = false;
                    return false;
                }

                _frameConfig = CloneConfig(_config);
                _frameNow = now;
                _frameStartSeconds = _startSeconds;
                _frameOriginX = _originX;
                _frameOriginY = _originY;
                _frameExcludeMinX = _excludeMinX;
                _frameExcludeMaxX = _excludeMaxX;
                _frameExcludeMinY = _excludeMinY;
                _frameExcludeMaxY = _excludeMaxY;
                _frameRadiusPx = Math.Max(0.01, _config.ImpulseRadiusCells) * charWidth;

                _frameWakeMinX = _originX - _frameRadiusPx;
                _frameWakeMaxX = _originX + _frameRadiusPx;
                _frameWakeMinY = _originY - _frameRadiusPx;
                _frameWakeMaxY = _originY + _frameRadiusPx;
                _frameHasBounds = true;
                _frameActive = true;
                return true;
            }
        }

        public void EndFrame()
        {
            _frameActive = false;
            _frameHasBounds = false;
        }

        public bool MayAffectText(int gridX, int gridY, int length, double charWidth, double charHeight)
        {
            if (!_frameActive || !_frameHasBounds || charWidth <= 0 || charHeight <= 0 || length <= 0)
                return false;

            double minX = gridX * charWidth;
            double maxX = (gridX + length) * charWidth;
            double minY = gridY * charHeight;
            double maxY = (gridY + 1) * charHeight;

            return !(maxX < _frameWakeMinX
                || minX > _frameWakeMaxX
                || maxY < _frameWakeMinY
                || minY > _frameWakeMaxY);
        }

        /// <summary>
        /// Starts a mild push centered at the origin. Glyphs inside the exclude rect stay rigid.
        /// </summary>
        public void Trigger(
            double originX,
            double originY,
            double excludeMinX,
            double excludeMaxX,
            double excludeMinY,
            double excludeMaxY,
            double charWidth,
            double charHeight,
            double? nowOverrideSeconds = null)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return;

            lock (_lock)
            {
                if (!IsEffectivelyEnabledUnlocked())
                    return;

                double now = nowOverrideSeconds ?? NowUnlocked();
                _active = true;
                _startSeconds = now;
                _originX = originX;
                _originY = originY;
                _excludeMinX = Math.Min(excludeMinX, excludeMaxX);
                _excludeMaxX = Math.Max(excludeMinX, excludeMaxX);
                _excludeMinY = Math.Min(excludeMinY, excludeMaxY);
                _excludeMaxY = Math.Max(excludeMinY, excludeMaxY);
            }
        }

        public BurstGlyphSample Sample(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (_frameActive)
                return SampleFromFrame(gridX, gridY, glyphIndex, charWidth, charHeight);

            if (!BeginFrame(charWidth, charHeight))
                return BurstGlyphSample.Zero;
            try
            {
                return SampleFromFrame(gridX, gridY, glyphIndex, charWidth, charHeight);
            }
            finally
            {
                EndFrame();
            }
        }

        public (double OffsetX, double OffsetY) SampleOffset(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            var s = Sample(gridX, gridY, glyphIndex, charWidth, charHeight);
            return (s.OffsetX, s.OffsetY);
        }

        public void Reset()
        {
            lock (_lock)
            {
                _active = false;
                _frameActive = false;
                _frameHasBounds = false;
            }
        }

        public void SetTestClockSeconds(double? seconds)
        {
            lock (_lock)
            {
                _testNowSeconds = seconds;
            }
        }

        public void TickForTests(double nowSeconds)
        {
            lock (_lock)
            {
                _testNowSeconds = nowSeconds;
                if (!IsAliveUnlocked(nowSeconds))
                    _active = false;
            }
        }

        public bool IsAliveForTests()
        {
            lock (_lock)
            {
                return IsAliveUnlocked(NowUnlocked());
            }
        }

        private BurstGlyphSample SampleFromFrame(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (!_frameActive || charWidth <= 0 || charHeight <= 0)
                return BurstGlyphSample.Zero;

            double glyphCenterX = ((gridX + glyphIndex) + 0.5) * charWidth;
            double glyphCenterY = (gridY + 0.5) * charHeight;

            if (glyphCenterX >= _frameExcludeMinX
                && glyphCenterX <= _frameExcludeMaxX
                && glyphCenterY >= _frameExcludeMinY
                && glyphCenterY <= _frameExcludeMaxY)
            {
                return BurstGlyphSample.Zero;
            }

            double dx = glyphCenterX - _frameOriginX;
            double dy = glyphCenterY - _frameOriginY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double radius = Math.Max(1e-3, _frameRadiusPx);
            double t = dist / radius;
            if (t >= 1.0)
                return BurstGlyphSample.Zero;

            double falloff = 1.0 - t;
            falloff *= falloff;

            double envelope = EvaluateEnvelope(_frameNow - _frameStartSeconds, _frameConfig);
            if (envelope <= 1e-6)
                return BurstGlyphSample.Zero;

            double peak = Math.Max(0, _frameConfig.MaxOffsetFraction) * charWidth * falloff * envelope;
            double len = Math.Max(1e-6, dist);
            double ux = dx / len;
            double uy = dy / len;
            // Glyphs at the exact origin get a tiny outward bias so neighbors still move.
            if (dist < 1e-3)
            {
                ux = 1;
                uy = 0;
            }

            double ox = ux * peak;
            double oy = uy * peak * Math.Max(0, _frameConfig.VerticalScale);

            // Approximate velocity for chromatic fringe during the out phase.
            double outSec = Math.Max(0.01, _frameConfig.OutSeconds);
            double elapsed = _frameNow - _frameStartSeconds;
            double vx = 0;
            double vy = 0;
            if (elapsed < outSec && outSec > 0)
            {
                double speed = peak / outSec;
                vx = ux * speed;
                vy = uy * speed * Math.Max(0, _frameConfig.VerticalScale);
            }

            return new BurstGlyphSample(ox, oy, vx, vy, rotationRadians: 0);
        }

        private static double EvaluateEnvelope(double elapsed, TextEmphasisPulseConfig cfg)
        {
            double outSec = Math.Max(0.01, cfg.OutSeconds);
            double reformSec = Math.Max(0.01, cfg.ReformSeconds);
            if (elapsed < 0)
                return 0;
            if (elapsed <= outSec)
            {
                double u = elapsed / outSec;
                // Ease-out cubic
                double inv = 1.0 - u;
                return 1.0 - inv * inv * inv;
            }

            double reformElapsed = elapsed - outSec;
            if (reformElapsed >= reformSec)
                return 0;

            double r = reformElapsed / reformSec;
            // Ease-in back to rest
            double remain = 1.0 - r;
            return remain * remain;
        }

        private bool IsAliveUnlocked(double now)
        {
            if (!_active)
                return false;
            double lifetime = Math.Max(0.01, _config.OutSeconds) + Math.Max(0.01, _config.ReformSeconds);
            return (now - _startSeconds) < lifetime;
        }

        private bool IsEffectivelyEnabledUnlocked() =>
            _config.Enabled && DeveloperModeState.AreDistortionEffectsEnabled;

        private double NowUnlocked() =>
            _testNowSeconds ?? _clock.Elapsed.TotalSeconds;

        private static TextEmphasisPulseConfig CloneConfig(TextEmphasisPulseConfig src) =>
            new()
            {
                Enabled = src.Enabled,
                ImpulseRadiusCells = src.ImpulseRadiusCells,
                MaxOffsetFraction = src.MaxOffsetFraction,
                OutSeconds = src.OutSeconds,
                ReformSeconds = src.ReformSeconds,
                VerticalScale = src.VerticalScale,
                ChromaticVelocitySeconds = src.ChromaticVelocitySeconds
            };
    }
}
