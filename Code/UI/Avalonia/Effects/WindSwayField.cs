using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Mouse-velocity wind field: one wake circle with near→far gradient, rear-biased by
    /// motion direction, sampled over a decaying trail.
    /// Honors <see cref="DeveloperModeState.AreDistortionEffectsEnabled"/> (F6) in addition to config.
    /// Hot path: call <see cref="BeginFrame"/> once per paint, then <see cref="SampleOffset"/> (lock-free).
    /// </summary>
    public sealed class WindSwayField
    {
        private readonly struct TrailPoint
        {
            public TrailPoint(double x, double y, double time, double windX, double windY, double speed)
            {
                X = x;
                Y = y;
                Time = time;
                WindX = windX;
                WindY = windY;
                Speed = speed;
            }

            public double X { get; }
            public double Y { get; }
            public double Time { get; }
            public double WindX { get; }
            public double WindY { get; }
            public double Speed { get; }
        }

        /// <summary>Matches <see cref="IsActive"/> / BeginFrame activity cutoffs so rest is exact zero.</summary>
        internal const double ActivityEpsilon = 0.01;

        /// <summary>
        /// Ignore OS/micro jitter below this pixel travel so idle cannot sustain residual wind.
        /// Sized above typical still-hover noise while reading item tooltips (~1–2px).
        /// </summary>
        internal const double MotionDeadzonePixels = 3.0;

        /// <summary>
        /// After this much stillness, residual wake is forced to exact rest.
        /// Soft decay still runs during the grace window; past it, pixel fonts must not keep CA smear.
        /// </summary>
        internal const double IdleGraceSeconds = 0.16;

        /// <summary>
        /// Visual offsets/CA clear this quickly after the last meaningful motion so still-hover
        /// (reading an item tip) does not keep smearing stats. Field energy may linger until
        /// <see cref="IdleGraceSeconds"/> so the settle timer can paint the clean rest frame.
        /// </summary>
        internal const double IdleVisualRestSeconds = 0.04;

        /// <summary>
        /// Sampled glyph offsets below this pixel magnitude paint as rest (avoids subpixel AA smear on pixel fonts).
        /// Kept under typical near-wake test magnitudes (~0.3px) so real sway still registers.
        /// Paint still ignores sub-<see cref="WindSwayChromatic.IdleEpsilon"/> poses for CA.
        /// </summary>
        internal const double PixelRestEpsilon = 0.25;

        private readonly object _lock = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<TrailPoint> _trail = new();

        private WindSwayConfig _config = new();
        private double _windX;
        private double _windY;
        private double _speedCellsPerSec;
        private double _lastSampleSeconds;
        private double _lastPushSeconds;
        private double _lastMeaningfulMotionSeconds;
        private double? _lastMouseX;
        private double? _lastMouseY;
        private double? _lastDepositX;
        private double? _lastDepositY;
        private double _mouseX;
        private double _mouseY;
        private bool _hasMouse;
        private bool _showWakeRadiusDebug;
        // Unit motion direction in pixel space; major ellipse axis aligns with this (default upright).
        private double _motionDirX;
        private double _motionDirY = 1.0;

        // Per-paint snapshot (filled by BeginFrame; SampleOffset reads without locking).
        private bool _frameActive;
        private WindSwayConfig _frameConfig = new();
        private double _frameNow;
        private double _frameWindX;
        private double _frameWindY;
        private double _frameSpeed;
        private double _frameAmpFactor;
        private bool _frameHasMouse;
        private double _frameMouseX;
        private double _frameMouseY;
        private TrailPoint[] _frameTrail = Array.Empty<TrailPoint>();
        private int _frameTrailCount;
        private double _frameRadiusCells;
        private double _frameRadiusMinorPx;
        private double _frameRadiusMajorPx;
        private double _frameMotionDirX;
        private double _frameMotionDirY;
        private double _frameWakeMinX;
        private double _frameWakeMaxX;
        private double _frameWakeMinY;
        private double _frameWakeMaxY;
        private bool _frameHasBounds;
        private double _frameIdleFor;

        /// <summary>Current configuration (replaced atomically via <see cref="ApplyConfig"/>).</summary>
        public WindSwayConfig Config
        {
            get
            {
                lock (_lock)
                    return _config;
            }
        }

        /// <summary>
        /// When true, the main canvas draws the wake radius circle around the last mouse position.
        /// Runtime-only (not persisted).
        /// </summary>
        public bool ShowWakeRadiusDebug
        {
            get
            {
                lock (_lock)
                    return _showWakeRadiusDebug;
            }
            set
            {
                lock (_lock)
                    _showWakeRadiusDebug = value;
            }
        }

        public void ApplyConfig(WindSwayConfig? config)
        {
            lock (_lock)
            {
                _config = config ?? new WindSwayConfig();
            }
        }

        /// <summary>
        /// Updates the tracked mouse position without applying wind impulse (for debug overlay / idle tracking).
        /// Also refreshes motion facing so the wake oval orients while wind/F6 is off.
        /// </summary>
        public void TrackMousePosition(double mouseX, double mouseY)
        {
            lock (_lock)
            {
                if (_hasMouse)
                {
                    double dxPx = mouseX - _mouseX;
                    double dyPx = mouseY - _mouseY;
                    double distPx = Math.Sqrt(dxPx * dxPx + dyPx * dyPx);
                    if (distPx > 1e-6)
                    {
                        _motionDirX = dxPx / distPx;
                        _motionDirY = dyPx / distPx;
                    }
                }

                _mouseX = mouseX;
                _mouseY = mouseY;
                _hasMouse = true;
            }
        }

        /// <summary>
        /// Returns the wake debug ellipse in pixel space when a mouse sample exists.
        /// Shape matches the old cell-aspect oval (<c>cells×charWidth</c> × <c>cells×charHeight</c>);
        /// <paramref name="rotationRadians"/> rotates that upright oval so the major axis follows mouse motion.
        /// </summary>
        public bool TryGetWakeDebugEllipse(
            double charWidth,
            double charHeight,
            out double centerX,
            out double centerY,
            out double radiusX,
            out double radiusY,
            out double rotationRadians)
        {
            lock (_lock)
            {
                centerX = _mouseX;
                centerY = _mouseY;
                radiusX = 0;
                radiusY = 0;
                rotationRadians = 0;
                if (!_hasMouse || charWidth <= 0 || charHeight <= 0)
                    return false;

                double cells = Math.Max(0.01, _config.WakeRadiusCells);
                // Minor = character-width axis; major = character-height axis (old tall oval).
                radiusX = cells * charWidth;
                radiusY = cells * charHeight;
                // Upright oval has major along +Y; rotate so major aligns with motion direction.
                rotationRadians = Math.Atan2(_motionDirY, _motionDirX) - (Math.PI * 0.5);
                return true;
            }
        }

        /// <summary>True when config + F6 runtime allow sway and residual wake is meaningful.</summary>
        public bool IsActive
        {
            get
            {
                // Do not short-circuit on _frameActive: that skipped Decay/idle-rest and could
                // leave settle thinking the wake was still live (stuck CA on idle stats text).
                lock (_lock)
                {
                    if (!IsEffectivelyEnabledUnlocked())
                        return false;
                    double now = _clock.Elapsed.TotalSeconds;
                    DecayUnlocked(now);
                    PruneTrailUnlocked(now);
                    return MagnitudeUnlocked() > ActivityEpsilon
                        || _speedCellsPerSec > ActivityEpsilon
                        || _trail.Count > 0;
                }
            }
        }

        /// <summary>
        /// Snapshots wind/trail state once for a paint pass. Returns false when sway should be skipped.
        /// Pair with <see cref="EndFrame"/>.
        /// </summary>
        public bool BeginFrame(double charWidth, double charHeight)
        {
            lock (_lock)
            {
                _frameActive = false;
                _frameHasBounds = false;
                _frameTrailCount = 0;

                if (!IsEffectivelyEnabledUnlocked() || charWidth <= 0 || charHeight <= 0)
                    return false;

                double now = _clock.Elapsed.TotalSeconds;
                DecayUnlocked(now);
                PruneTrailUnlocked(now);

                bool alive = MagnitudeUnlocked() > ActivityEpsilon
                    || _speedCellsPerSec > ActivityEpsilon
                    || _trail.Count > 0;
                if (!alive)
                    return false;

                _frameConfig = _config;
                _frameNow = now;
                _frameIdleFor = now - _lastMeaningfulMotionSeconds;
                _frameWindX = _windX;
                _frameWindY = _windY;
                _frameSpeed = _speedCellsPerSec;
                _frameHasMouse = _hasMouse;
                _frameMouseX = _mouseX;
                _frameMouseY = _mouseY;

                double mag = MagnitudeUnlocked();
                double speedRef = Math.Max(0.01, _config.SpeedReferenceCellsPerSec);
                double speedFactor = Math.Clamp(_speedCellsPerSec / speedRef, 0.0, 1.0);
                double windFactor = Math.Clamp(mag / Math.Max(0.01, _config.MaxWind), 0.0, 1.0);
                _frameAmpFactor = Math.Max(speedFactor, windFactor * 0.9);

                _frameRadiusCells = Math.Max(0.01, _config.WakeRadiusCells);
                _frameRadiusMinorPx = _frameRadiusCells * charWidth;
                _frameRadiusMajorPx = _frameRadiusCells * charHeight;
                _frameMotionDirX = _motionDirX;
                _frameMotionDirY = _motionDirY;

                EnsureFrameTrailCapacity(_trail.Count);
                for (int i = 0; i < _trail.Count; i++)
                    _frameTrail[i] = _trail[i];
                _frameTrailCount = _trail.Count;

                BuildWakeBoundsUnlocked(charWidth, charHeight);

                _frameActive = true;
                return true;
            }
        }

        /// <summary>Clears the paint-pass snapshot.</summary>
        public void EndFrame()
        {
            _frameActive = false;
            _frameHasBounds = false;
            _frameTrailCount = 0;
        }

        /// <summary>
        /// True while a paint <see cref="BeginFrame"/> snapshot is live.
        /// Prefer this over <see cref="IsActive"/> during paint to avoid per-glyph locks.
        /// </summary>
        public bool FrameActive => _frameActive;

        /// <summary>
        /// Config snapshotted by the current paint <see cref="BeginFrame"/> (or last BeginFrame).
        /// Safe to read during paint without locking.
        /// </summary>
        public WindSwayConfig FrameConfig => _frameConfig;

        /// <summary>
        /// Fast AABB test: true when a text run on this grid row might sit inside the wake.
        /// Requires an active <see cref="BeginFrame"/> snapshot.
        /// </summary>
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

        public void PushFromMouseDelta(double mouseX, double mouseY, double charWidth, double charHeight)
            => PushFromMouseDelta(mouseX, mouseY, charWidth, charHeight, dtOverrideSeconds: null);

        public void PushFromMouseDelta(
            double mouseX,
            double mouseY,
            double charWidth,
            double charHeight,
            double? dtOverrideSeconds)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return;

            lock (_lock)
            {
                if (!IsEffectivelyEnabledUnlocked())
                    return;

                double now = _clock.Elapsed.TotalSeconds;
                DecayUnlocked(now);
                PruneTrailUnlocked(now);

                _mouseX = mouseX;
                _mouseY = mouseY;
                _hasMouse = true;

                if (_lastMouseX.HasValue && _lastMouseY.HasValue)
                {
                    double dxPx = mouseX - _lastMouseX.Value;
                    double dyPx = mouseY - _lastMouseY.Value;
                    double distPx = Math.Sqrt(dxPx * dxPx + dyPx * dyPx);

                    // Deadzone: track position but do not inject wind/speed from micro-jitter.
                    if (distPx > MotionDeadzonePixels)
                    {
                        double dxCells = dxPx / charWidth;
                        double dyCells = dyPx / charHeight;

                        double pushDt = dtOverrideSeconds ?? (now - _lastPushSeconds);
                        if (pushDt <= 0)
                            pushDt = 1e-3;

                        double distCells = Math.Sqrt(dxCells * dxCells + dyCells * dyCells);
                        _speedCellsPerSec = distCells / pushDt;

                        double gain = Math.Max(0, _config.ImpulseGain);
                        _windX += dxCells * gain;
                        _windY += dyCells * gain;
                        ClampWindUnlocked();

                        // Orient the wake ellipse's major axis along the mouse motion vector.
                        _motionDirX = dxPx / distPx;
                        _motionDirY = dyPx / distPx;
                        _lastMeaningfulMotionSeconds = now;

                        TryDepositTrailUnlocked(mouseX, mouseY, charWidth, charHeight, now);
                    }
                }
                else
                {
                    // First sample: establish position without inventing an impulse.
                    _lastMeaningfulMotionSeconds = now;
                }

                _lastMouseX = mouseX;
                _lastMouseY = mouseY;
                _lastPushSeconds = now;
            }
        }

        public (double OffsetX, double OffsetY) SampleOffset(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (_frameActive)
                return SampleOffsetFromFrame(gridX, gridY, glyphIndex, charWidth, charHeight);

            // Test / non-paint callers: snapshot locally for one sample.
            if (!BeginFrame(charWidth, charHeight))
                return (0, 0);
            try
            {
                return SampleOffsetFromFrame(gridX, gridY, glyphIndex, charWidth, charHeight);
            }
            finally
            {
                EndFrame();
            }
        }

        /// <summary>
        /// Radial wake influence 0..1 for interactive-text highlight.
        /// Uses the same cell-aspect oval + near→far falloff as sway, but does not
        /// require wind amp (works while the pointer is hovering still).
        /// Safe to call outside a paint <see cref="BeginFrame"/> (locks briefly).
        /// </summary>
        public double SampleProximityInfluence(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return 0;

            lock (_lock)
            {
                if (!_config.InteractiveHighlightEnabled || !_hasMouse)
                    return 0;

                double radiusCells = Math.Max(0.01, _config.WakeRadiusCells);
                double radiusMinorPx = radiusCells * charWidth;
                double radiusMajorPx = radiusCells * charHeight;
                double nearInf = Math.Max(0, _config.NearInfluence);
                double farInf = Math.Max(0, _config.FarInfluence);
                bool useQuadratic = _config.WakeFalloffPower > 1.05;

                double glyphCenterX = ((gridX + glyphIndex) + 0.5) * charWidth;
                double glyphCenterY = (gridY + 0.5) * charHeight;
                double dx = glyphCenterX - _mouseX;
                double dy = glyphCenterY - _mouseY;
                double t = EllipticalNormalizedRadius(
                    dx, dy, _motionDirX, _motionDirY, radiusMinorPx, radiusMajorPx);
                if (t >= 1.0)
                    return 0;

                double curved = useQuadratic ? t * t : t;
                // Normalize so peak (near) maps toward 1 for highlight strength.
                double radial = Lerp(nearInf, farInf, curved);
                double peak = Math.Max(nearInf, farInf);
                if (peak <= 1e-6)
                    return 0;
                return Math.Clamp(radial / peak, 0.0, 1.0);
            }
        }

        private (double OffsetX, double OffsetY) SampleOffsetFromFrame(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (!_frameActive || charWidth <= 0 || charHeight <= 0)
                return (0, 0);

            // Still cursor: no visual smear even if field energy has not hard-zeroed yet.
            if (_frameIdleFor >= IdleVisualRestSeconds)
                return (0, 0);

            if (_frameTrailCount == 0 && !_frameHasMouse)
                return (0, 0);

            if (_frameAmpFactor < 1e-6 && _frameTrailCount == 0)
                return (0, 0);

            double glyphCenterX = ((gridX + glyphIndex) + 0.5) * charWidth;
            double glyphCenterY = (gridY + 0.5) * charHeight;

            var cfg = _frameConfig;
            double nearInf = Math.Max(0, cfg.NearInfluence);
            double farInf = Math.Max(0, cfg.FarInfluence);
            double rearBias = Math.Clamp(cfg.WakeRearBias, 0.0, 1.0);
            double lifetime = Math.Max(0.01, cfg.TrailLifetimeSeconds);
            // Quadratic falloff (cheap stand-in for the old t^power curve).
            bool useQuadratic = cfg.WakeFalloffPower > 1.05;

            double bestLayer = 0;
            double bestWindX = _frameWindX;
            double bestWindY = _frameWindY;
            double bestPointSpeed = _frameSpeed;

            if (_frameHasMouse)
            {
                ConsiderPoint(
                    glyphCenterX, glyphCenterY,
                    _frameMouseX, _frameMouseY, 1.0,
                    _frameWindX, _frameWindY, _frameSpeed,
                    _frameRadiusMinorPx, _frameRadiusMajorPx,
                    _frameMotionDirX, _frameMotionDirY,
                    nearInf, farInf, useQuadratic, rearBias,
                    ref bestLayer, ref bestWindX, ref bestWindY, ref bestPointSpeed);
            }

            for (int i = 0; i < _frameTrailCount; i++)
            {
                var p = _frameTrail[i];
                double ageFade = 1.0 - (_frameNow - p.Time) / lifetime;
                if (ageFade <= 0)
                    continue;
                ResolveFacingFromWind(p.WindX, p.WindY, charWidth, charHeight,
                    _frameMotionDirX, _frameMotionDirY, out double stampDirX, out double stampDirY);
                ConsiderPoint(
                    glyphCenterX, glyphCenterY,
                    p.X, p.Y, ageFade,
                    p.WindX, p.WindY, p.Speed,
                    _frameRadiusMinorPx, _frameRadiusMajorPx,
                    stampDirX, stampDirY,
                    nearInf, farInf, useQuadratic, rearBias,
                    ref bestLayer, ref bestWindX, ref bestWindY, ref bestPointSpeed);
            }

            if (bestLayer < 1e-6)
                return (0, 0);

            double speedRef = Math.Max(0.01, cfg.SpeedReferenceCellsPerSec);
            double pointSpeedFactor = Math.Clamp(bestPointSpeed / speedRef, 0.0, 1.0);
            double pointWindMag = Math.Sqrt(bestWindX * bestWindX + bestWindY * bestWindY);
            double pointWindFactor = Math.Clamp(pointWindMag / Math.Max(0.01, cfg.MaxWind), 0.0, 1.0);
            double pointAmp = Math.Max(pointSpeedFactor, pointWindFactor * 0.9);
            double combinedAmp = Math.Max(_frameAmpFactor, pointAmp);
            if (combinedAmp < 1e-6)
                return (0, 0);

            double dirMag = pointWindMag;
            if (dirMag <= 1e-6)
                return (0, 0);

            double dirX = bestWindX / dirMag;
            double dirY = bestWindY / dirMag;
            double vert = Math.Clamp(cfg.VerticalScale, 0, 1);
            double strength = Math.Max(0, cfg.MaxOffsetFraction) * bestLayer * combinedAmp;

            // Direction-only offset (no per-glyph sine ripple — was already subdued and costly).
            double ox = dirX * strength * charWidth;
            double oy = dirY * strength * charHeight * vert;
            if (Math.Sqrt(ox * ox + oy * oy) < PixelRestEpsilon)
                return (0, 0);
            return (ox, oy);
        }

        public void Reset()
        {
            lock (_lock)
            {
                _windX = 0;
                _windY = 0;
                _speedCellsPerSec = 0;
                _lastMouseX = null;
                _lastMouseY = null;
                _lastDepositX = null;
                _lastDepositY = null;
                _hasMouse = false;
                _mouseX = 0;
                _mouseY = 0;
                _motionDirX = 0;
                _motionDirY = 1;
                _trail.Clear();
                double now = _clock.Elapsed.TotalSeconds;
                _lastSampleSeconds = now;
                _lastPushSeconds = now;
                _lastMeaningfulMotionSeconds = now;
            }
            EndFrame();
        }

        public (double X, double Y) GetWindForTests()
        {
            lock (_lock)
            {
                DecayUnlocked(_clock.Elapsed.TotalSeconds);
                return (_windX, _windY);
            }
        }

        public double GetSpeedForTests()
        {
            lock (_lock)
            {
                DecayUnlocked(_clock.Elapsed.TotalSeconds);
                return _speedCellsPerSec;
            }
        }

        public int GetTrailCountForTests()
        {
            lock (_lock)
            {
                PruneTrailUnlocked(_clock.Elapsed.TotalSeconds);
                return _trail.Count;
            }
        }

        /// <summary>
        /// Test helper: pretends the pointer has been still past the idle grace, then applies idle rest.
        /// </summary>
        public void ForceIdleRestForTests()
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _lastMeaningfulMotionSeconds = now - IdleGraceSeconds - 0.05;
                DecayUnlocked(now);
                PruneTrailUnlocked(now);
            }
        }

        /// <summary>
        /// Test helper: sets stillness duration without sleeping. Does not force hard-zero unless
        /// <paramref name="idleSeconds"/> reaches <see cref="IdleGraceSeconds"/>.
        /// </summary>
        public void SetIdleSecondsForTests(double idleSeconds)
        {
            lock (_lock)
            {
                double now = _clock.Elapsed.TotalSeconds;
                _lastMeaningfulMotionSeconds = now - Math.Max(0, idleSeconds);
                DecayUnlocked(now);
                PruneTrailUnlocked(now);
            }
        }

        /// <summary>Seconds since last meaningful mouse motion (test diagnostics).</summary>
        public double GetIdleSecondsForTests()
        {
            lock (_lock)
                return _clock.Elapsed.TotalSeconds - _lastMeaningfulMotionSeconds;
        }

        private void EnsureFrameTrailCapacity(int count)
        {
            if (_frameTrail.Length >= count)
                return;
            int next = Math.Max(count, Math.Max(8, _frameTrail.Length * 2));
            _frameTrail = new TrailPoint[next];
        }

        private void BuildWakeBoundsUnlocked(double charWidth, double charHeight)
        {
            bool any = false;
            double minX = 0, maxX = 0, minY = 0, maxY = 0;

            void Expand(double x, double y, double dirX, double dirY)
            {
                OrientedEllipsePad(dirX, dirY, _frameRadiusMinorPx, _frameRadiusMajorPx,
                    out double padX, out double padY);
                double x0 = x - padX;
                double x1 = x + padX;
                double y0 = y - padY;
                double y1 = y + padY;
                if (!any)
                {
                    minX = x0;
                    maxX = x1;
                    minY = y0;
                    maxY = y1;
                    any = true;
                    return;
                }

                if (x0 < minX) minX = x0;
                if (x1 > maxX) maxX = x1;
                if (y0 < minY) minY = y0;
                if (y1 > maxY) maxY = y1;
            }

            if (_frameHasMouse)
                Expand(_frameMouseX, _frameMouseY, _frameMotionDirX, _frameMotionDirY);
            for (int i = 0; i < _frameTrailCount; i++)
            {
                var p = _frameTrail[i];
                ResolveFacingFromWind(p.WindX, p.WindY, charWidth, charHeight,
                    _frameMotionDirX, _frameMotionDirY, out double stampDirX, out double stampDirY);
                Expand(p.X, p.Y, stampDirX, stampDirY);
            }

            _frameHasBounds = any;
            _frameWakeMinX = minX;
            _frameWakeMaxX = maxX;
            _frameWakeMinY = minY;
            _frameWakeMaxY = maxY;
        }

        private static void ConsiderPoint(
            double glyphCenterX,
            double glyphCenterY,
            double pointX,
            double pointY,
            double ageFade,
            double windX,
            double windY,
            double speed,
            double radiusMinorPx,
            double radiusMajorPx,
            double motionDirX,
            double motionDirY,
            double nearInfluence,
            double farInfluence,
            bool useQuadratic,
            double rearBias,
            ref double bestLayer,
            ref double bestWindX,
            ref double bestWindY,
            ref double bestPointSpeed)
        {
            double dx = glyphCenterX - pointX;
            double dy = glyphCenterY - pointY;
            double t = EllipticalNormalizedRadius(dx, dy, motionDirX, motionDirY, radiusMinorPx, radiusMajorPx);
            if (t >= 1.0)
                return;

            double curved = useQuadratic ? t * t : t;
            double radial = Lerp(nearInfluence, farInfluence, curved);

            // Wake stamps need a wind vector; zero-wind points would otherwise paint
            // full isotropic radial and wash out rear bias / trail directionality.
            double velMagSq = windX * windX + windY * windY;
            if (velMagSq <= 1e-8)
                return;

            double dirFactor = 1.0;
            if (rearBias > 1e-6 && t > 1e-6)
            {
                // Compare behind/front in pixel space along the ellipse orientation.
                double distPx = Math.Sqrt(dx * dx + dy * dy);
                if (distPx > 1e-6)
                {
                    double toGlyphX = dx / distPx;
                    double toGlyphY = dy / distPx;
                    // behind = 1 when glyph is opposite motion (in the wake), 0 in front
                    double behind = 0.5 * (1.0 + (-motionDirX * toGlyphX + -motionDirY * toGlyphY));
                    behind = Math.Clamp(behind, 0.0, 1.0);
                    dirFactor = Lerp(1.0 - rearBias, 1.0, behind);
                }
            }

            double layer = radial * dirFactor * Math.Clamp(ageFade, 0.0, 1.0);
            if (layer <= bestLayer)
                return;

            bestLayer = layer;
            bestWindX = windX;
            bestWindY = windY;
            bestPointSpeed = speed;
        }

        /// <summary>
        /// Normalized elliptical radius: 0 at center, 1 on the boundary.
        /// Major axis aligns with <paramref name="dirX"/>/<paramref name="dirY"/> (mouse motion).
        /// </summary>
        internal static double EllipticalNormalizedRadius(
            double dx,
            double dy,
            double dirX,
            double dirY,
            double radiusMinorPx,
            double radiusMajorPx)
        {
            double minor = Math.Max(1e-6, radiusMinorPx);
            double major = Math.Max(1e-6, radiusMajorPx);
            double along = dx * dirX + dy * dirY;
            double perp = -dx * dirY + dy * dirX;
            double nx = perp / minor;
            double ny = along / major;
            return Math.Sqrt(nx * nx + ny * ny);
        }

        internal static void OrientedEllipsePad(
            double dirX,
            double dirY,
            double radiusMinorPx,
            double radiusMajorPx,
            out double padX,
            out double padY)
        {
            double perpX = -dirY;
            double perpY = dirX;
            padX = Math.Sqrt(
                (radiusMinorPx * perpX) * (radiusMinorPx * perpX)
                + (radiusMajorPx * dirX) * (radiusMajorPx * dirX));
            padY = Math.Sqrt(
                (radiusMinorPx * perpY) * (radiusMinorPx * perpY)
                + (radiusMajorPx * dirY) * (radiusMajorPx * dirY));
        }

        internal static void ResolveFacingFromWind(
            double windXCells,
            double windYCells,
            double charWidth,
            double charHeight,
            double fallbackDirX,
            double fallbackDirY,
            out double dirX,
            out double dirY)
        {
            double px = windXCells * charWidth;
            double py = windYCells * charHeight;
            double mag = Math.Sqrt(px * px + py * py);
            if (mag <= 1e-8)
            {
                dirX = fallbackDirX;
                dirY = fallbackDirY;
                return;
            }

            dirX = px / mag;
            dirY = py / mag;
        }

        private static double Lerp(double a, double b, double t) => a + (b - a) * t;

        private void TryDepositTrailUnlocked(
            double mouseX,
            double mouseY,
            double charWidth,
            double charHeight,
            double now)
        {
            bool shouldDeposit = !_lastDepositX.HasValue || !_lastDepositY.HasValue;
            if (!shouldDeposit)
            {
                double dxCells = (mouseX - _lastDepositX!.Value) / charWidth;
                double dyCells = (mouseY - _lastDepositY!.Value) / charHeight;
                double distCells = Math.Sqrt(dxCells * dxCells + dyCells * dyCells);
                shouldDeposit = distCells >= Math.Max(0.01, _config.TrailMinSpacingCells);
            }

            if (!shouldDeposit)
                return;

            _trail.Add(new TrailPoint(mouseX, mouseY, now, _windX, _windY, _speedCellsPerSec));
            _lastDepositX = mouseX;
            _lastDepositY = mouseY;

            int maxPoints = Math.Max(1, _config.TrailMaxPoints);
            while (_trail.Count > maxPoints)
                _trail.RemoveAt(0);
        }

        private void PruneTrailUnlocked(double nowSeconds)
        {
            double lifetime = Math.Max(0.01, _config.TrailLifetimeSeconds);
            _trail.RemoveAll(p => nowSeconds - p.Time > lifetime);
        }

        private bool IsEffectivelyEnabledUnlocked() =>
            _config.Enabled && DeveloperModeState.AreDistortionEffectsEnabled;

        private void DecayUnlocked(double nowSeconds)
        {
            // Idle rest must run even when dt==0 (multiple settle/IsActive checks in one tick).
            double idleFor = nowSeconds - _lastMeaningfulMotionSeconds;
            if (idleFor >= IdleGraceSeconds)
            {
                _windX = 0;
                _windY = 0;
                _speedCellsPerSec = 0;
                if (_trail.Count > 0)
                    _trail.Clear();
                if (nowSeconds > _lastSampleSeconds)
                    _lastSampleSeconds = nowSeconds;
                return;
            }

            double dt = nowSeconds - _lastSampleSeconds;
            if (dt <= 0)
                return;

            _lastSampleSeconds = nowSeconds;
            double decay = Math.Max(0, _config.DecayPerSecond);
            if (decay <= 0)
                return;

            double factor = Math.Exp(-decay * dt);
            _windX *= factor;
            _windY *= factor;
            _speedCellsPerSec *= factor;

            // Snap at the same cutoff IsActive uses so settle ends on a true rest pose.
            if (MagnitudeUnlocked() < ActivityEpsilon)
            {
                _windX = 0;
                _windY = 0;
            }
            if (_speedCellsPerSec < ActivityEpsilon)
                _speedCellsPerSec = 0;
        }

        private double MagnitudeUnlocked() => Math.Sqrt(_windX * _windX + _windY * _windY);

        private void ClampWindUnlocked()
        {
            double max = Math.Max(0.01, _config.MaxWind);
            double mag = MagnitudeUnlocked();
            if (mag > max)
            {
                double scale = max / mag;
                _windX *= scale;
                _windY *= scale;
            }
        }
    }
}
