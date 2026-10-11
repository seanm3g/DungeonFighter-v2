using System;
using System.Collections.Generic;
using System.Diagnostics;
using RPGGame;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Per-glyph burst sample: landed position, flight velocity (for chromatic), and spin.
    /// </summary>
    public readonly struct BurstGlyphSample
    {
        public BurstGlyphSample(double offsetX, double offsetY, double velocityX, double velocityY, double rotationRadians)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            VelocityX = velocityX;
            VelocityY = velocityY;
            RotationRadians = rotationRadians;
        }

        public double OffsetX { get; }
        public double OffsetY { get; }
        public double VelocityX { get; }
        public double VelocityY { get; }
        public double RotationRadians { get; }

        public static BurstGlyphSample Zero => new(0, 0, 0, 0, 0);
    }

    /// <summary>
    /// Multi-zone click-charge → explode → stay scattered → click-rebuild force field.
    /// Each exploded region stays pinned to its click origin; clicks outside start another zone;
    /// clicks inside a mess rebuild that zone only, letters returning in reading order
    /// (top→bottom, left→right). Viewport scroll (<see cref="SyncViewportScroll"/>) shifts
    /// origins + glyph keys so messes ride with the text they exploded from. After
    /// <see cref="TextClickBurstConfig.AutoRebuildIdleSeconds"/> with no hole click, letters
    /// auto-fill one every <see cref="TextClickBurstConfig.AutoRebuildIntervalSeconds"/>.
    /// Honors <see cref="DeveloperModeState.AreDistortionEffectsEnabled"/> (F6) in addition to
    /// config. Hot path: <see cref="BeginFrame"/> once per paint, then <see cref="Sample"/>
    /// (lock-free). Affects any non-overlay canvas text (caller decides panel scope).
    /// </summary>
    public sealed class TextClickBurstField
    {
        private const int MaxZones = 12;

        private readonly object _lock = new();
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly List<BurstZone> _zones = new();
        private readonly Dictionary<string, int> _viewportScrollRows = new(StringComparer.Ordinal);
        private double? _testNowSeconds;

        private TextClickBurstConfig _config = new();
        private double _lastSampleSeconds;

        // Per-paint snapshot
        private bool _frameActive;
        private TextClickBurstConfig _frameConfig = new();
        private double _frameNow;
        private FrameZone[] _frameZones = Array.Empty<FrameZone>();
        private int _frameZoneCount;
        private double _frameRadiusPx;
        private double _frameWakeMinX;
        private double _frameWakeMaxX;
        private double _frameWakeMinY;
        private double _frameWakeMaxY;
        private bool _frameHasBounds;
        /// <summary>Glyph keys observed during the current paint (merged in <see cref="EndFrame"/>).</summary>
        private readonly List<(int ZoneIndex, long Key)> _frameGlyphNotes = new();

        public TextClickBurstConfig Config
        {
            get
            {
                lock (_lock)
                    return _config;
            }
        }

        public void ApplyConfig(TextClickBurstConfig? config)
        {
            lock (_lock)
            {
                _config = config ?? new TextClickBurstConfig();
            }
        }

        /// <summary>True when config + F6 allow effect and residual charge/impulse/explosion is alive.</summary>
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
                    DecayUnlocked(now);
                    AdvanceExplosionUnlocked(now);
                    return IsAliveUnlocked(now);
                }
            }
        }

        public bool BeginFrame(double charWidth, double charHeight)
        {
            lock (_lock)
            {
                _frameActive = false;
                _frameHasBounds = false;
                _frameZoneCount = 0;

                if (!IsEffectivelyEnabledUnlocked() || charWidth <= 0 || charHeight <= 0)
                    return false;

                double now = NowUnlocked();
                DecayUnlocked(now);
                AdvanceExplosionUnlocked(now);

                if (!IsAliveUnlocked(now))
                    return false;

                _frameConfig = _config;
                _frameNow = now;

                double cells = Math.Max(0.01, _config.ImpulseRadiusCells);
                _frameRadiusPx = cells * charWidth;

                EnsureFrameZoneCapacity(_zones.Count);
                _frameGlyphNotes.Clear();
                double wakeMinX = double.PositiveInfinity;
                double wakeMaxX = double.NegativeInfinity;
                double wakeMinY = double.PositiveInfinity;
                double wakeMaxY = double.NegativeInfinity;
                bool any = false;

                for (int i = 0; i < _zones.Count; i++)
                {
                    BurstZone z = _zones[i];
                    int letters = Math.Max(1, _config.LettersToRebuild);
                    int glyphCount = z.GlyphOrder.Count;
                    int perStep = glyphCount > 0
                        ? Math.Max(1, (glyphCount + letters - 1) / letters)
                        : 1;

                    _frameZones[_frameZoneCount++] = new FrameZone(
                        i,
                        z.OriginX,
                        z.OriginY,
                        z.Charge,
                        z.ImpulseStrength,
                        z.Exploding,
                        z.ExplodeStartSeconds,
                        z.RebuildProgress,
                        z.LastRebuildClickSeconds,
                        z.RebuiltLetterCount,
                        perStep,
                        z.GlyphOrderFrozen ? z.GlyphOrder : null);

                    wakeMinX = Math.Min(wakeMinX, z.OriginX - _frameRadiusPx);
                    wakeMaxX = Math.Max(wakeMaxX, z.OriginX + _frameRadiusPx);
                    wakeMinY = Math.Min(wakeMinY, z.OriginY - _frameRadiusPx);
                    wakeMaxY = Math.Max(wakeMaxY, z.OriginY + _frameRadiusPx);
                    any = true;
                }

                if (any)
                {
                    _frameWakeMinX = wakeMinX;
                    _frameWakeMaxX = wakeMaxX;
                    _frameWakeMinY = wakeMinY;
                    _frameWakeMaxY = wakeMaxY;
                    _frameHasBounds = true;
                }

                _frameActive = true;
                return true;
            }
        }

        public void EndFrame()
        {
            if (_frameGlyphNotes.Count > 0)
            {
                lock (_lock)
                    MergeFrameGlyphNotesUnlocked();
            }

            _frameActive = false;
            _frameHasBounds = false;
            _frameZoneCount = 0;
            _frameGlyphNotes.Clear();
        }

        /// <summary>
        /// True while a paint <see cref="BeginFrame"/> snapshot is live.
        /// Prefer this over <see cref="IsActive"/> during paint to avoid per-element locks.
        /// </summary>
        public bool FrameActive => _frameActive;

        /// <summary>
        /// Config snapshotted by the current paint <see cref="BeginFrame"/>.
        /// Safe to read during paint without locking.
        /// </summary>
        public TextClickBurstConfig FrameConfig => _frameConfig;

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
        /// Registers a left-click impulse at pixel coordinates.
        /// Clicks inside an exploded mess rebuild that zone; clicks outside charge/explode a new section.
        /// </summary>
        public void NotifyClick(double mouseX, double mouseY, double charWidth, double charHeight)
            => NotifyClick(mouseX, mouseY, charWidth, charHeight, nowOverrideSeconds: null);

        /// <param name="nowOverrideSeconds">Test hook: absolute clock seconds for deterministic timing.</param>
        public void NotifyClick(
            double mouseX,
            double mouseY,
            double charWidth,
            double charHeight,
            double? nowOverrideSeconds)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return;

            lock (_lock)
            {
                if (!IsEffectivelyEnabledUnlocked())
                    return;

                double now = nowOverrideSeconds ?? NowUnlocked();
                if (nowOverrideSeconds.HasValue && _lastSampleSeconds <= 0)
                    _lastSampleSeconds = now;

                DecayUnlocked(now);
                AdvanceExplosionUnlocked(now);

                double radiusPx = Math.Max(0.01, _config.ImpulseRadiusCells) * charWidth;

                BurstZone? hitExploded = FindClosestZoneUnlocked(
                    mouseX, mouseY, radiusPx, explodingOnly: true);
                if (hitExploded != null)
                {
                    RebuildZoneUnlocked(hitExploded, now);
                    return;
                }

                BurstZone? hitCharging = FindClosestZoneUnlocked(
                    mouseX, mouseY, radiusPx, explodingOnly: false);
                if (hitCharging != null)
                {
                    ChargeZoneUnlocked(hitCharging, mouseX, mouseY, now);
                    return;
                }

                if (_zones.Count >= MaxZones)
                    EvictOldestIdleZoneUnlocked();

                if (_zones.Count >= MaxZones)
                    return;

                var zone = new BurstZone
                {
                    OriginX = mouseX,
                    OriginY = mouseY,
                    LastClickSeconds = now
                };
                _zones.Add(zone);
                ChargeZoneUnlocked(zone, mouseX, mouseY, now);
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

        public void Reset()
        {
            lock (_lock)
            {
                _zones.Clear();
                _viewportScrollRows.Clear();
                _lastSampleSeconds = NowUnlocked();
                _frameActive = false;
                _frameHasBounds = false;
                _frameZoneCount = 0;
            }
        }

        /// <summary>
        /// Keeps burst zones attached to scrolled text. Call once per paint with the viewport's
        /// current content scroll (rows skipped from the top). First call for a
        /// <paramref name="viewportId"/> seeds the baseline without moving zones; later deltas
        /// shift origins and glyph keys inside <paramref name="viewportMinYPx"/>..<paramref name="viewportMaxYPx"/>.
        /// Positive <paramref name="scrollRows"/> growth means content moved up on screen.
        /// </summary>
        public void SyncViewportScroll(
            string viewportId,
            int scrollRows,
            double viewportMinYPx,
            double viewportMaxYPx,
            double charHeight)
        {
            if (string.IsNullOrEmpty(viewportId) || charHeight <= 0)
                return;
            if (viewportMaxYPx <= viewportMinYPx)
                return;

            lock (_lock)
            {
                if (!_viewportScrollRows.TryGetValue(viewportId, out int lastScroll))
                {
                    _viewportScrollRows[viewportId] = scrollRows;
                    return;
                }

                int deltaRows = scrollRows - lastScroll;
                _viewportScrollRows[viewportId] = scrollRows;
                if (deltaRows == 0 || _zones.Count == 0)
                    return;

                ApplyVerticalContentScrollUnlocked(deltaRows, viewportMinYPx, viewportMaxYPx, charHeight);
            }
        }

        /// <summary>Test hook: pin the field clock (null = wall clock).</summary>
        public void SetTestClockSeconds(double? seconds)
        {
            lock (_lock)
            {
                _testNowSeconds = seconds;
                if (seconds.HasValue && _lastSampleSeconds <= 0)
                    _lastSampleSeconds = seconds.Value;
            }
        }

        /// <summary>Test hook: current charge 0..1 (max among non-exploding zones).</summary>
        public double GetChargeForTests()
        {
            lock (_lock)
            {
                double max = 0;
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (!_zones[i].Exploding)
                        max = Math.Max(max, _zones[i].Charge);
                }
                return max;
            }
        }

        /// <summary>Test hook: whether any explode/scatter/rebuild is running.</summary>
        public bool IsExplodingForTests()
        {
            lock (_lock)
            {
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Exploding)
                        return true;
                }
                return false;
            }
        }

        /// <summary>Test hook: rebuild progress 0..1 while scattered (first exploding zone).</summary>
        public double GetRebuildProgressForTests()
        {
            lock (_lock)
            {
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Exploding)
                        return _zones[i].RebuildProgress;
                }
                return 0;
            }
        }

        /// <summary>Test hook: live zone count (charging + exploded).</summary>
        public int GetZoneCountForTests()
        {
            lock (_lock)
                return _zones.Count;
        }

        /// <summary>Test hook: first zone origin Y in content pixels (−1 if none).</summary>
        public double GetFirstOriginYForTests()
        {
            lock (_lock)
                return _zones.Count > 0 ? _zones[0].OriginY : -1;
        }

        /// <summary>Test hook: count of currently exploded zones.</summary>
        public int GetExplodingZoneCountForTests()
        {
            lock (_lock)
            {
                int n = 0;
                for (int i = 0; i < _zones.Count; i++)
                {
                    if (_zones[i].Exploding)
                        n++;
                }
                return n;
            }
        }

        /// <summary>Test hook: advance decay/explosion to the pinned or absolute clock seconds.</summary>
        public void TickForTests(double nowSeconds)
        {
            lock (_lock)
            {
                _testNowSeconds = nowSeconds;
                DecayUnlocked(nowSeconds);
                AdvanceExplosionUnlocked(nowSeconds);
            }
        }

        private double NowUnlocked() => _testNowSeconds ?? _clock.Elapsed.TotalSeconds;

        private void EnsureFrameZoneCapacity(int needed)
        {
            if (_frameZones.Length >= needed)
                return;
            int size = Math.Max(4, needed);
            _frameZones = new FrameZone[size];
        }

        private BurstZone? FindClosestZoneUnlocked(
            double mouseX,
            double mouseY,
            double radiusPx,
            bool explodingOnly)
        {
            BurstZone? best = null;
            double bestDistSq = double.PositiveInfinity;
            double radiusSq = radiusPx * radiusPx;

            for (int i = 0; i < _zones.Count; i++)
            {
                BurstZone z = _zones[i];
                if (explodingOnly)
                {
                    if (!z.Exploding)
                        continue;
                }
                else if (z.Exploding)
                {
                    continue;
                }

                double dx = mouseX - z.OriginX;
                double dy = mouseY - z.OriginY;
                double distSq = dx * dx + dy * dy;
                if (distSq > radiusSq || distSq >= bestDistSq)
                    continue;

                bestDistSq = distSq;
                best = z;
            }

            return best;
        }

        private void RebuildZoneUnlocked(BurstZone zone, double now)
        {
            zone.LastClickSeconds = now;

            double outSec = Math.Max(0.01, _config.ExplodeOutSeconds);
            double elapsed = now - zone.ExplodeStartSeconds;
            if (elapsed >= outSec && zone.RebuildProgress < 0.999)
            {
                AdvanceRebuildProgressUnlocked(zone, now);
                // Manual hole click resets the idle wait before autofill resumes.
                double idleSec = Math.Max(0, _config.AutoRebuildIdleSeconds);
                zone.AutoRebuildNextDueSeconds = now + idleSec;
            }

            zone.ImpulseStrength = Math.Max(zone.ImpulseStrength, 1.0);
        }

        private void AdvanceRebuildProgressUnlocked(BurstZone zone, double now)
        {
            zone.FreezeGlyphOrder();
            int n = zone.GlyphOrder.Count;
            int letters = Math.Max(1, _config.LettersToRebuild);
            if (n > 0)
            {
                // Home the next batch in reading order (at least one glyph per step).
                int perStep = Math.Max(1, (n + letters - 1) / letters);
                zone.RebuiltLetterCount = Math.Min(n, zone.RebuiltLetterCount + perStep);
                zone.RebuildProgress = zone.RebuiltLetterCount / (double)n;
            }
            else
            {
                zone.RebuildProgress = Math.Min(1.0, zone.RebuildProgress + (1.0 / letters));
            }

            zone.LastRebuildClickSeconds = now;
        }

        private void MergeFrameGlyphNotesUnlocked()
        {
            for (int i = 0; i < _frameGlyphNotes.Count; i++)
            {
                (int zoneIndex, long key) = _frameGlyphNotes[i];
                if (zoneIndex < 0 || zoneIndex >= _zones.Count)
                    continue;
                BurstZone z = _zones[zoneIndex];
                if (!z.Exploding || z.GlyphOrderFrozen)
                    continue;
                z.NoteGlyph(key);
            }
        }

        private void ChargeZoneUnlocked(BurstZone zone, double mouseX, double mouseY, double now)
        {
            // Keep charging origin sticky once established so the blast stays put;
            // only seed origin on the first click that creates the zone.
            if (zone.Charge < 1e-6 && zone.ImpulseStrength < 1e-6)
            {
                zone.OriginX = mouseX;
                zone.OriginY = mouseY;
            }

            zone.LastClickSeconds = now;

            int clicks = Math.Max(1, _config.ClicksToExplode);
            zone.Charge = Math.Clamp(zone.Charge + (1.0 / clicks), 0.0, 1.0);
            zone.ImpulseStrength = 1.0;

            if (zone.Charge >= 0.999)
            {
                zone.Charge = 0;
                zone.ImpulseStrength = 0;
                zone.Exploding = true;
                zone.ExplodeStartSeconds = now;
                zone.RebuildProgress = 0;
                zone.RebuiltLetterCount = 0;
                zone.LastRebuildClickSeconds = -1;
                // Schedule lazily in AdvanceExplosion (land time + idle).
                zone.AutoRebuildNextDueSeconds = -1;
                zone.ResetGlyphOrder();
            }
        }

        private void EvictOldestIdleZoneUnlocked()
        {
            int victim = -1;
            double oldest = double.PositiveInfinity;
            for (int i = 0; i < _zones.Count; i++)
            {
                BurstZone z = _zones[i];
                if (z.Exploding)
                    continue;
                if (z.LastClickSeconds < oldest)
                {
                    oldest = z.LastClickSeconds;
                    victim = i;
                }
            }

            if (victim >= 0)
                _zones.RemoveAt(victim);
        }

        private void ApplyVerticalContentScrollUnlocked(
            int deltaRows,
            double viewportMinYPx,
            double viewportMaxYPx,
            double charHeight)
        {
            // Positive deltaRows = more content skipped at top = glyphs move up on screen.
            double deltaYPx = deltaRows * charHeight;

            for (int i = 0; i < _zones.Count; i++)
            {
                BurstZone z = _zones[i];
                if (z.OriginY < viewportMinYPx || z.OriginY >= viewportMaxYPx)
                    continue;

                z.OriginY -= deltaYPx;
                ShiftZoneGlyphKeysUnlocked(z, deltaRows);
            }
        }

        private static void ShiftZoneGlyphKeysUnlocked(BurstZone zone, int deltaRows)
        {
            if (deltaRows == 0 || zone.GlyphOrder.Count == 0)
                return;

            zone.GlyphSet.Clear();
            for (int i = 0; i < zone.GlyphOrder.Count; i++)
            {
                long shifted = ShiftGlyphKeyRow(zone.GlyphOrder[i], deltaRows);
                zone.GlyphOrder[i] = shifted;
                zone.GlyphSet.Add(shifted);
            }

            if (zone.GlyphOrderFrozen)
                zone.GlyphOrder.Sort();
        }

        /// <summary>Shift the row (high bits) of a glyph key when content scrolls by <paramref name="deltaRows"/>.</summary>
        private static long ShiftGlyphKeyRow(long key, int deltaRows)
        {
            int col = (int)(uint)key;
            int row = (int)(key >> 32);
            return ((long)(row - deltaRows) << 32) | (uint)col;
        }

        private BurstGlyphSample SampleFromFrame(
            int gridX,
            int gridY,
            int glyphIndex,
            double charWidth,
            double charHeight)
        {
            if (!_frameActive || _frameZoneCount <= 0 || charWidth <= 0 || charHeight <= 0)
                return BurstGlyphSample.Zero;

            double glyphCenterX = ((gridX + glyphIndex) + 0.5) * charWidth;
            double glyphCenterY = (gridY + 0.5) * charHeight;

            double sumOx = 0;
            double sumOy = 0;
            double sumVx = 0;
            double sumVy = 0;
            double bestRot = 0;
            double bestMag = 0;

            long glyphKey = GlyphKey(gridX, gridY, glyphIndex);

            for (int i = 0; i < _frameZoneCount; i++)
            {
                // Hash relative to the zone origin so scatter look stays stable when scroll
                // shifts both the glyph and the origin by the same amount.
                FrameZone zone = _frameZones[i];
                int relX = (int)Math.Floor((glyphCenterX - zone.OriginX) / charWidth);
                int relY = (int)Math.Floor((glyphCenterY - zone.OriginY) / charHeight);
                int h = HashGlyph(relX, relY, glyphIndex);

                BurstGlyphSample part = SampleZoneFromFrame(
                    in zone, h, glyphKey, glyphCenterX, glyphCenterY, charWidth, charHeight);
                sumOx += part.OffsetX;
                sumOy += part.OffsetY;
                sumVx += part.VelocityX;
                sumVy += part.VelocityY;

                double mag = Math.Abs(part.OffsetX) + Math.Abs(part.OffsetY);
                if (mag > bestMag)
                {
                    bestMag = mag;
                    bestRot = part.RotationRadians;
                }
            }

            if (bestMag < 1e-6 && Math.Abs(sumVx) + Math.Abs(sumVy) < 1e-6)
                return BurstGlyphSample.Zero;

            return new BurstGlyphSample(sumOx, sumOy, sumVx, sumVy, bestRot);
        }

        private BurstGlyphSample SampleZoneFromFrame(
            in FrameZone zone,
            int h,
            long glyphKey,
            double glyphCenterX,
            double glyphCenterY,
            double charWidth,
            double charHeight)
        {
            double dx = glyphCenterX - zone.OriginX;
            double dy = glyphCenterY - zone.OriginY;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double radius = Math.Max(1e-3, _frameRadiusPx);
            double t = dist / radius;
            if (t >= 1.0)
                return BurstGlyphSample.Zero;

            double falloff = 1.0 - t;
            falloff *= falloff; // soft edge

            double dirX;
            double dirY;
            if (dist < 1e-4)
            {
                double angle = (h & 1023) * (Math.PI * 2.0 / 1024.0);
                dirX = Math.Cos(angle);
                dirY = Math.Sin(angle);
            }
            else
            {
                dirX = dx / dist;
                dirY = dy / dist;
            }

            var cfg = _frameConfig;
            double vScale = Math.Max(0, cfg.VerticalScale);

            if (zone.Exploding)
            {
                // Collect affected glyphs during scatter so rebuild can walk reading order.
                if (zone.GlyphOrder == null)
                    _frameGlyphNotes.Add((zone.ZoneIndex, glyphKey));

                return SampleExploding(
                    h, dirX, dirY, falloff, charWidth, charHeight, vScale, cfg,
                    zone.ExplodeStartSeconds, zone.RebuildProgress, zone.LastRebuildClickSeconds,
                    glyphKey, glyphCenterX, glyphCenterY, zone.OriginX, zone.OriginY, radius,
                    zone.GlyphOrder, zone.RebuiltLetterCount, zone.RebuildPerStep);
            }

            double charge = Math.Clamp(zone.Charge, 0.0, 1.0);
            double impulse = Math.Clamp(zone.Impulse, 0.0, 1.0);
            if (charge < 1e-4 && impulse < 1e-4)
                return BurstGlyphSample.Zero;

            double maxFrac = Math.Max(0, cfg.MaxImpulseOffsetFraction);
            double ampFraction = maxFrac * falloff * Math.Max(charge, impulse * 0.55);

            double jx = (((h >> 3) & 255) / 255.0 - 0.5) * 0.35 * charge;
            double jy = (((h >> 11) & 255) / 255.0 - 0.5) * 0.35 * charge;
            dirX += jx;
            dirY += jy;
            double dmag = Math.Sqrt(dirX * dirX + dirY * dirY);
            if (dmag > 1e-6)
            {
                dirX /= dmag;
                dirY /= dmag;
            }

            double ox = dirX * ampFraction * charWidth;
            double oy = dirY * ampFraction * charHeight * vScale;
            return new BurstGlyphSample(ox, oy, 0, 0, 0);
        }

        private BurstGlyphSample SampleExploding(
            int h,
            double dirX,
            double dirY,
            double falloff,
            double charWidth,
            double charHeight,
            double vScale,
            TextClickBurstConfig cfg,
            double explodeStartSeconds,
            double rebuildProgress,
            double lastRebuildClickSeconds,
            long glyphKey,
            double glyphCenterX,
            double glyphCenterY,
            double originX,
            double originY,
            double radius,
            List<long>? glyphOrder,
            int rebuiltLetterCount,
            int rebuildPerStep)
        {
            double elapsed = Math.Max(0, _frameNow - explodeStartSeconds);
            double outSec = Math.Max(0.01, cfg.ExplodeOutSeconds);
            double reformSec = Math.Max(0.01, cfg.ReformSeconds);
            double maxFrac = Math.Max(0, cfg.ExplodeMaxOffsetFraction);

            double distMin = Math.Clamp(cfg.DistanceVarianceMin, 0.0, 4.0);
            double distMax = Math.Clamp(cfg.DistanceVarianceMax, distMin, 4.0);
            double distU = ((h >> 5) & 255) / 255.0;
            double distMul = distMin + (distMax - distMin) * distU;

            double rotU = ((h >> 13) & 1023) / 1023.0;
            double peakRot = (rotU * 2.0 - 1.0) * Math.Max(0, cfg.MaxRotationRadians);
            // Secondary twist so neighboring letters don't share the same spin family.
            double twist = (((h >> 21) & 255) / 255.0 - 0.5) * Math.Max(0, cfg.MaxRotationRadians) * 0.85;
            peakRot += twist;

            double peakAmp = maxFrac * falloff * distMul;
            RebuildGlyphPhase phase = ResolveRebuildPhase(
                glyphKey, glyphCenterX, glyphCenterY, originX, originY, radius,
                glyphOrder, rebuiltLetterCount, rebuildPerStep, rebuildProgress, cfg);

            double ampFraction;
            double ampVelocity; // d(ampFraction)/dt
            double rot;
            double rotVelocity;

            if (elapsed <= outSec)
            {
                double u = elapsed / outSec;
                // Ease-out cubic: punches hard toward peak scatter.
                double oneMinus = 1.0 - u;
                double easeOut = 1.0 - oneMinus * oneMinus * oneMinus;
                double easeDeriv = 3.0 * oneMinus * oneMinus / outSec;
                ampFraction = peakAmp * easeOut;
                ampVelocity = peakAmp * easeDeriv;
                rot = peakRot * easeOut;
                rotVelocity = peakRot * easeDeriv;
            }
            else if (phase == RebuildGlyphPhase.Scattered)
            {
                // Still scattered — stay at peak (velocity 0 → clean letter, no CA).
                ampFraction = peakAmp;
                ampVelocity = 0;
                rot = peakRot;
                rotVelocity = 0;
            }
            else if (phase == RebuildGlyphPhase.Home)
            {
                // Already rebuilt home.
                return BurstGlyphSample.Zero;
            }
            else
            {
                // This step's letter(s): ease home.
                double since = lastRebuildClickSeconds < 0
                    ? reformSec
                    : Math.Max(0, _frameNow - lastRebuildClickSeconds);
                double v = Math.Clamp(since / reformSec, 0.0, 1.0);
                double oneMinus = 1.0 - v;
                double easeBack = oneMinus * oneMinus;
                double easeDeriv = -2.0 * oneMinus / reformSec;
                ampFraction = peakAmp * easeBack;
                ampVelocity = peakAmp * easeDeriv;
                rot = peakRot * easeBack;
                rotVelocity = peakRot * easeDeriv;
            }

            double ox = dirX * ampFraction * charWidth;
            double oy = dirY * ampFraction * charHeight * vScale;
            double vx = dirX * ampVelocity * charWidth;
            double vy = dirY * ampVelocity * charHeight * vScale;
            // Rotation velocity is unused for chromatic; stored only if callers want it later.
            _ = rotVelocity;
            return new BurstGlyphSample(ox, oy, vx, vy, rot);
        }

        private enum RebuildGlyphPhase
        {
            Scattered,
            Animating,
            Home
        }

        /// <summary>
        /// Reading-order rebuild: registered glyphs home from top→bottom, left→right.
        /// Falls back to spatial order when the paint registry is not frozen yet.
        /// </summary>
        private static RebuildGlyphPhase ResolveRebuildPhase(
            long glyphKey,
            double glyphCenterX,
            double glyphCenterY,
            double originX,
            double originY,
            double radius,
            List<long>? glyphOrder,
            int rebuiltLetterCount,
            int rebuildPerStep,
            double rebuildProgress,
            TextClickBurstConfig cfg)
        {
            if (glyphOrder != null && glyphOrder.Count > 0)
            {
                int idx = glyphOrder.BinarySearch(glyphKey);
                if (idx < 0)
                    return RebuildGlyphPhase.Scattered;

                int homeCount = Math.Clamp(rebuiltLetterCount, 0, glyphOrder.Count);
                if (idx >= homeCount)
                    return RebuildGlyphPhase.Scattered;

                int perStep = Math.Max(1, rebuildPerStep);
                int animStart = Math.Max(0, homeCount - perStep);
                return idx < animStart ? RebuildGlyphPhase.Home : RebuildGlyphPhase.Animating;
            }

            // Fallback before registry freeze: spatial reading order across the zone AABB.
            double diameter = Math.Max(1e-3, radius * 2.0);
            double localX = Math.Clamp((glyphCenterX - (originX - radius)) / diameter, 0.0, 1.0);
            double localY = Math.Clamp((glyphCenterY - (originY - radius)) / diameter, 0.0, 1.0);
            // Y dominates so rows finish top→bottom; X breaks ties left→right.
            double orderU = Math.Clamp((localY * 1024.0 + localX) / 1025.0, 0.0, 1.0);

            int letters = Math.Max(1, cfg.LettersToRebuild);
            double step = 1.0 / letters;
            double rebuild = Math.Clamp(rebuildProgress, 0.0, 1.0);
            double bandStart = rebuild - step;

            if (orderU >= rebuild - 1e-9)
                return RebuildGlyphPhase.Scattered;
            if (orderU < bandStart - 1e-9)
                return RebuildGlyphPhase.Home;
            return RebuildGlyphPhase.Animating;
        }

        /// <summary>Stable reading-order key: row in high bits, column (gridX+glyphIndex) in low bits.</summary>
        private static long GlyphKey(int gridX, int gridY, int glyphIndex)
        {
            int col = gridX + glyphIndex;
            return ((long)gridY << 32) | (uint)col;
        }

        private void DecayUnlocked(double nowSeconds)
        {
            double dt = nowSeconds - _lastSampleSeconds;
            if (dt <= 0)
                return;

            _lastSampleSeconds = nowSeconds;

            double chargeDecay = Math.Max(0, _config.ChargeDecayPerSecond);
            double impulseDecay = Math.Max(0, _config.ImpulseDecayPerSecond);

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                BurstZone z = _zones[i];
                if (z.Exploding)
                    continue;

                if (chargeDecay > 0 && z.Charge > 0)
                    z.Charge = Math.Max(0, z.Charge - chargeDecay * dt);

                if (impulseDecay > 0 && z.ImpulseStrength > 0)
                {
                    z.ImpulseStrength *= Math.Exp(-impulseDecay * dt);
                    if (z.ImpulseStrength < 1e-4)
                        z.ImpulseStrength = 0;
                }

                if (z.Charge < 0.01 && z.ImpulseStrength < 0.01
                    && (z.LastClickSeconds < 0 || nowSeconds - z.LastClickSeconds >= 0.05))
                {
                    _zones.RemoveAt(i);
                }
            }
        }

        private void AdvanceExplosionUnlocked(double nowSeconds)
        {
            double reformSec = Math.Max(0.01, _config.ReformSeconds);

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                BurstZone z = _zones[i];
                if (!z.Exploding)
                    continue;

                if (z.RebuildProgress < 0.999)
                    TryAutoRebuildUnlocked(z, nowSeconds);

                // Scatter stays until rebuild finishes (click or auto), including last letter anim.
                if (z.RebuildProgress < 0.999)
                    continue;

                double animEnd = (z.LastRebuildClickSeconds < 0 ? z.ExplodeStartSeconds : z.LastRebuildClickSeconds)
                    + reformSec;
                if (nowSeconds >= animEnd)
                    _zones.RemoveAt(i);
            }
        }

        /// <summary>
        /// After scatter lands, wait <see cref="TextClickBurstConfig.AutoRebuildIdleSeconds"/> with
        /// no hole click, then fill one letter every
        /// <see cref="TextClickBurstConfig.AutoRebuildIntervalSeconds"/>.
        /// </summary>
        private void TryAutoRebuildUnlocked(BurstZone zone, double nowSeconds)
        {
            double outSec = Math.Max(0.01, _config.ExplodeOutSeconds);
            double landTime = zone.ExplodeStartSeconds + outSec;
            if (nowSeconds < landTime)
                return;

            // Do not freeze here — keep collecting painted glyphs until the first rebuild step
            // (AdvanceRebuildProgressUnlocked). Freezing early drops letters registered on later paints.

            double idleSec = Math.Max(0, _config.AutoRebuildIdleSeconds);
            double intervalSec = Math.Max(0.01, _config.AutoRebuildIntervalSeconds);

            if (zone.AutoRebuildNextDueSeconds < 0)
                zone.AutoRebuildNextDueSeconds = landTime + idleSec;

            while (zone.RebuildProgress < 0.999 && nowSeconds >= zone.AutoRebuildNextDueSeconds)
            {
                double stepAt = zone.AutoRebuildNextDueSeconds;
                AdvanceRebuildProgressUnlocked(zone, stepAt);
                zone.AutoRebuildNextDueSeconds = stepAt + intervalSec;
            }
        }

        private bool IsAliveUnlocked(double nowSeconds)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                BurstZone z = _zones[i];
                if (z.Exploding)
                    return true;
                if (z.Charge > 0.01 || z.ImpulseStrength > 0.01)
                    return true;
                if (z.LastClickSeconds >= 0 && nowSeconds - z.LastClickSeconds < 0.05)
                    return true;
            }
            return false;
        }

        private bool IsEffectivelyEnabledUnlocked() =>
            _config.Enabled && DeveloperModeState.AreDistortionEffectsEnabled;

        private static int HashGlyph(int gridX, int gridY, int glyphIndex)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + gridX;
                h = h * 31 + gridY;
                h = h * 31 + glyphIndex;
                h ^= h << 13;
                h ^= h >> 17;
                h ^= h << 5;
                return h;
            }
        }

        private sealed class BurstZone
        {
            public double OriginX;
            public double OriginY;
            public double Charge;
            public double ImpulseStrength;
            public bool Exploding;
            public double ExplodeStartSeconds;
            public double RebuildProgress;
            public int RebuiltLetterCount;
            public double LastRebuildClickSeconds = -1;
            public double LastClickSeconds = -1;
            /// <summary>Wall-clock seconds when the next autofill letter is due (−1 = schedule from land+idle).</summary>
            public double AutoRebuildNextDueSeconds = -1;
            public readonly List<long> GlyphOrder = new();
            public readonly HashSet<long> GlyphSet = new();
            public bool GlyphOrderFrozen;

            public void NoteGlyph(long key)
            {
                if (GlyphOrderFrozen)
                    return;
                if (GlyphSet.Add(key))
                    GlyphOrder.Add(key);
            }

            public void FreezeGlyphOrder()
            {
                if (GlyphOrderFrozen)
                    return;
                GlyphOrder.Sort();
                GlyphOrderFrozen = true;
            }

            public void ResetGlyphOrder()
            {
                GlyphOrder.Clear();
                GlyphSet.Clear();
                GlyphOrderFrozen = false;
                RebuiltLetterCount = 0;
            }
        }

        private readonly struct FrameZone
        {
            public FrameZone(
                int zoneIndex,
                double originX,
                double originY,
                double charge,
                double impulse,
                bool exploding,
                double explodeStart,
                double rebuildProgress,
                double lastRebuildClick,
                int rebuiltLetterCount,
                int rebuildPerStep,
                List<long>? glyphOrder)
            {
                ZoneIndex = zoneIndex;
                OriginX = originX;
                OriginY = originY;
                Charge = charge;
                Impulse = impulse;
                Exploding = exploding;
                ExplodeStartSeconds = explodeStart;
                RebuildProgress = rebuildProgress;
                LastRebuildClickSeconds = lastRebuildClick;
                RebuiltLetterCount = rebuiltLetterCount;
                RebuildPerStep = rebuildPerStep;
                GlyphOrder = glyphOrder;
            }

            public int ZoneIndex { get; }
            public double OriginX { get; }
            public double OriginY { get; }
            public double Charge { get; }
            public double Impulse { get; }
            public bool Exploding { get; }
            public double ExplodeStartSeconds { get; }
            public double RebuildProgress { get; }
            public double LastRebuildClickSeconds { get; }
            public int RebuiltLetterCount { get; }
            public int RebuildPerStep { get; }
            public List<long>? GlyphOrder { get; }
        }
    }
}
