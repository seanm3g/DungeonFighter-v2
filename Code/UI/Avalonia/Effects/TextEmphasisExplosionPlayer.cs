using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RPGGame;
using RPGGame.UI.Avalonia.Canvas;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// One-shot DistortionPulse explosion GIF centered on an action word.
    /// Drawn as a letter-masked overlay (same <see cref="DrawingContext.PushOpacityMask"/> approach
    /// as narrative video): the GIF only shows through occupied combat-log glyphs.
    /// Loads <c>GameData/Video/explosion.gif</c> (configurable) lazily; near-black keyed transparent.
    /// </summary>
    public sealed class TextEmphasisExplosionPlayer
    {
        private readonly object _lock = new();
        private readonly StopwatchClock _clock = new();
        private double? _testNowSeconds;

        private TextEmphasisPulseConfig _config = new();
        private IReadOnlyList<EmphasisExplosionGifDecoder.RawGifFrame>? _rawFrames;
        private List<Bitmap>? _bitmaps;
        private string? _loadedPath;
        private bool _loadAttempted;
        private string? _lastLoadError;

        private bool _playing;
        private double _startSeconds;
        private double _centerX;
        private double _centerY;
        private double _diameterPx;
        private double _charWidth;
        private double _charHeight;
        private int _totalDurationMs;

        // Reused mask scratch (UI paint thread).
        private float[]? _opacityGrid;
        private byte[]? _maskPacked;
        private WriteableBitmap? _maskBitmap;
        private ImageBrush? _maskBrush;
        private int _maskCols;
        private int _maskRows;
        private int _maskCellPxW;
        private int _maskCellPxH;

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
                if (_loadedPath != null
                    && !string.Equals(
                        NormalizePath(_config.ExplosionGifFileName),
                        NormalizePath(PathLeaf(_loadedPath)),
                        StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(
                        NormalizePath(_config.ExplosionGifFileName),
                        NormalizePath(_loadedPath),
                        StringComparison.OrdinalIgnoreCase))
                {
                    ClearFramesUnlocked();
                }
            }
        }

        public bool IsActive
        {
            get
            {
                lock (_lock)
                {
                    if (!_playing || !IsEffectivelyEnabledUnlocked())
                        return false;
                    return ElapsedMsUnlocked(NowUnlocked()) < _totalDurationMs;
                }
            }
        }

        public string? LastLoadError
        {
            get
            {
                lock (_lock)
                    return _lastLoadError;
            }
        }

        public int FrameCountForTests
        {
            get
            {
                lock (_lock)
                    return _rawFrames?.Count ?? 0;
            }
        }

        /// <summary>
        /// Starts a one-shot playback centered on the action word (content pixel space).
        /// </summary>
        public bool Trigger(double centerX, double centerY, double charWidth, double charHeight)
        {
            if (charWidth <= 0 || charHeight <= 0)
                return false;

            lock (_lock)
            {
                if (!IsEffectivelyEnabledUnlocked() || !_config.ExplosionGifEnabled)
                    return false;

                if (!EnsureRawFramesLoadedUnlocked())
                    return false;

                double cells = Math.Max(2, _config.ExplosionDiameterCells);
                _diameterPx = cells * charWidth;
                _centerX = centerX;
                _centerY = centerY;
                _charWidth = charWidth;
                _charHeight = charHeight;
                _startSeconds = NowUnlocked();
                // Visible window: cut-in then fully gone in ExplosionFadeOutSeconds (not full GIF length).
                int fadeMs = (int)Math.Round(Math.Max(0.01, _config.ExplosionFadeOutSeconds) * 1000.0);
                _totalDurationMs = fadeMs;
                _playing = true;
                return true;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                _playing = false;
            }
        }

        public void SetTestClockSeconds(double? seconds)
        {
            lock (_lock)
                _testNowSeconds = seconds;
        }

        public void TickForTests(double nowSeconds)
        {
            lock (_lock)
            {
                _testNowSeconds = nowSeconds;
                if (_playing && ElapsedMsUnlocked(nowSeconds) >= _totalDurationMs)
                    _playing = false;
            }
        }

        /// <summary>
        /// Opacity for cut-in + fade-out: full at t=0, linear to 0 over <paramref name="fadeOutMs"/>.
        /// </summary>
        public static double ResolveOpacity(int elapsedMs, int fadeOutMs)
        {
            if (fadeOutMs <= 0 || elapsedMs < 0)
                return 0.0;
            if (elapsedMs >= fadeOutMs)
                return 0.0;
            return Math.Clamp(1.0 - (elapsedMs / (double)fadeOutMs), 0.0, 1.0);
        }

        /// <summary>Current draw opacity for tests (0 when inactive).</summary>
        public double GetOpacityForTests()
        {
            lock (_lock)
            {
                if (!_playing || _totalDurationMs <= 0)
                    return 0.0;
                int elapsed = ElapsedMsUnlocked(NowUnlocked());
                return ResolveOpacity(elapsed, _totalDurationMs);
            }
        }

        /// <summary>
        /// Draws the current explosion frame as a letter-masked overlay. No-op when inactive.
        /// Uses occupied center-panel glyphs in the explosion footprint as the opacity mask
        /// (same technique as <see cref="NarrativeVideoOverlayControl"/>).
        /// </summary>
        public void Draw(
            DrawingContext context,
            IReadOnlyList<CanvasText>? textElements,
            double charWidth,
            double charHeight)
        {
            Bitmap? bmp = null;
            Rect dest = default;
            double opacity = 1.0;
            bool draw = false;
            double cw;
            double ch;

            lock (_lock)
            {
                if (!_playing || !IsEffectivelyEnabledUnlocked() || !_config.ExplosionGifEnabled)
                    return;
                if (_rawFrames == null || _rawFrames.Count == 0)
                    return;

                double now = NowUnlocked();
                int elapsed = ElapsedMsUnlocked(now);
                if (elapsed >= _totalDurationMs)
                {
                    _playing = false;
                    return;
                }

                opacity = ResolveOpacity(elapsed, _totalDurationMs);
                if (opacity <= 0.001)
                    return;

                if (!EnsureAvaloniaBitmapsUnlocked())
                    return;

                int frameIndex = ResolveFrameIndexUnlocked(elapsed);
                bmp = _bitmaps![frameIndex];
                double half = _diameterPx * 0.5;
                dest = new Rect(_centerX - half, _centerY - half, _diameterPx, _diameterPx);
                cw = charWidth > 0 ? charWidth : _charWidth;
                ch = charHeight > 0 ? charHeight : _charHeight;
                draw = true;
            }

            if (!draw || bmp == null || cw <= 0 || ch <= 0)
                return;

            if (!TryBuildLetterMask(textElements, dest, cw, ch, out ImageBrush? maskBrush) || maskBrush == null)
                return;

            using (context.PushOpacity(opacity))
            using (context.PushOpacityMask(maskBrush, dest))
            {
                context.DrawImage(bmp, dest);
            }
        }

        /// <summary>
        /// Compatibility draw without a letter mask (tests / callers with no text list).
        /// Prefer the overload that takes canvas text so the GIF is letter-masked.
        /// </summary>
        public void Draw(DrawingContext context) =>
            Draw(context, textElements: null, charWidth: 0, charHeight: 0);

        private bool TryBuildLetterMask(
            IReadOnlyList<CanvasText>? textElements,
            Rect dest,
            double charWidth,
            double charHeight,
            out ImageBrush? maskBrush)
        {
            maskBrush = null;
            EmphasisExplosionLetterMask.ResolveGridWindow(
                dest.X,
                dest.Y,
                dest.Right,
                dest.Bottom,
                charWidth,
                charHeight,
                out int originCol,
                out int originRow,
                out int cols,
                out int rows);
            if (cols <= 0 || rows <= 0)
                return false;

            int cellPxW = Math.Max(1, (int)Math.Round(charWidth));
            int cellPxH = Math.Max(1, (int)Math.Round(charHeight));
            int n = cols * rows;
            if (_opacityGrid == null || _opacityGrid.Length < n)
                _opacityGrid = new float[n];

            EmphasisExplosionLetterMask.BuildOccupiedGlyphOpacity(
                _opacityGrid,
                originCol,
                originRow,
                cols,
                rows,
                textElements ?? Array.Empty<CanvasText>(),
                maxOpacity: 1.0);

            bool any = false;
            for (int i = 0; i < n; i++)
            {
                if (_opacityGrid[i] > 0.001f)
                {
                    any = true;
                    break;
                }
            }

            if (!any)
                return false;

            // Mask bitmap matches `dest` exactly so Stretch.Fill aligns letter cells
            // even when the explosion AABB is not cell-grid aligned.
            int bmpW = Math.Max(1, (int)Math.Round(dest.Width));
            int bmpH = Math.Max(1, (int)Math.Round(dest.Height));
            int packedLen = bmpW * bmpH * 4;
            if (_maskPacked == null || _maskPacked.Length < packedLen)
                _maskPacked = new byte[packedLen];
            Array.Clear(_maskPacked, 0, packedLen);

            for (int gy = 0; gy < rows; gy++)
            {
                for (int gx = 0; gx < cols; gx++)
                {
                    float cellOpacity = _opacityGrid[gy * cols + gx];
                    if (cellOpacity <= 0.001f)
                        continue;

                    double cellMinX = (originCol + gx) * charWidth;
                    double cellMinY = (originRow + gy) * charHeight;
                    int x0 = (int)Math.Floor(cellMinX - dest.X);
                    int y0 = (int)Math.Floor(cellMinY - dest.Y);
                    int x1 = (int)Math.Ceiling(cellMinX + charWidth - dest.X);
                    int y1 = (int)Math.Ceiling(cellMinY + charHeight - dest.Y);
                    x0 = Math.Clamp(x0, 0, bmpW);
                    y0 = Math.Clamp(y0, 0, bmpH);
                    x1 = Math.Clamp(x1, 0, bmpW);
                    y1 = Math.Clamp(y1, 0, bmpH);
                    if (x0 >= x1 || y0 >= y1)
                        continue;

                    byte a = (byte)Math.Clamp((int)Math.Round(cellOpacity * 255.0), 0, 255);
                    for (int y = y0; y < y1; y++)
                    {
                        int row = y * bmpW * 4;
                        for (int x = x0; x < x1; x++)
                        {
                            int i = row + x * 4;
                            _maskPacked[i] = a;
                            _maskPacked[i + 1] = a;
                            _maskPacked[i + 2] = a;
                            _maskPacked[i + 3] = a;
                        }
                    }
                }
            }

            bool sizeChanged = _maskBitmap == null
                || _maskCols != cols
                || _maskRows != rows
                || _maskCellPxW != cellPxW
                || _maskCellPxH != cellPxH
                || _maskBitmap.PixelSize.Width != bmpW
                || _maskBitmap.PixelSize.Height != bmpH;
            if (sizeChanged)
            {
                _maskBitmap = new WriteableBitmap(
                    new PixelSize(bmpW, bmpH),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);
                _maskBrush = null;
                _maskCols = cols;
                _maskRows = rows;
                _maskCellPxW = cellPxW;
                _maskCellPxH = cellPxH;
            }

            using (var fb = _maskBitmap!.Lock())
            {
                if (fb.RowBytes == bmpW * 4)
                {
                    System.Runtime.InteropServices.Marshal.Copy(_maskPacked, 0, fb.Address, packedLen);
                }
                else
                {
                    for (int y = 0; y < bmpH; y++)
                    {
                        IntPtr rowPtr = fb.Address + y * fb.RowBytes;
                        System.Runtime.InteropServices.Marshal.Copy(
                            _maskPacked, y * bmpW * 4, rowPtr, bmpW * 4);
                    }
                }
            }

            maskBrush = EnsureMaskBrush();
            return true;
        }

        private ImageBrush EnsureMaskBrush()
        {
            if (_maskBrush != null && ReferenceEquals(_maskBrush.Source, _maskBitmap))
                return _maskBrush;

            _maskBrush = new ImageBrush(_maskBitmap)
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };
            return _maskBrush;
        }

        private bool EnsureRawFramesLoadedUnlocked()
        {
            if (_rawFrames != null && _rawFrames.Count > 0)
                return true;

            if (_loadAttempted && _rawFrames == null)
                return false;

            _loadAttempted = true;
            string relative = string.IsNullOrWhiteSpace(_config.ExplosionGifFileName)
                ? "Video/explosion.gif"
                : _config.ExplosionGifFileName.Trim().Replace('\\', '/');

            string? path = GameConstants.TryGetExistingGameDataFilePath(relative)
                ?? GameConstants.TryGetExistingGameDataFilePath(relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

            if (path == null)
            {
                _lastLoadError = $"missing {relative}";
                return false;
            }

            if (!EmphasisExplosionGifDecoder.TryLoad(path, out var frames, out string? error))
            {
                _lastLoadError = error ?? "decode failed";
                return false;
            }

            _rawFrames = frames;
            _loadedPath = path;
            _lastLoadError = null;
            return true;
        }

        private bool EnsureAvaloniaBitmapsUnlocked()
        {
            if (_bitmaps != null && _bitmaps.Count > 0)
                return true;
            if (_rawFrames == null || _rawFrames.Count == 0)
                return false;

            var list = new List<Bitmap>(_rawFrames.Count);
            for (int i = 0; i < _rawFrames.Count; i++)
            {
                var bmp = EmphasisExplosionGifDecoder.TryCreateAvaloniaBitmap(_rawFrames[i]);
                if (bmp == null)
                {
                    EmphasisExplosionGifDecoder.DisposeBitmaps(list);
                    _lastLoadError = "Avalonia bitmap unavailable";
                    return false;
                }
                list.Add(bmp);
            }

            _bitmaps = list;
            return true;
        }

        private int ResolveFrameIndexUnlocked(int elapsedMs)
        {
            if (_rawFrames == null || _rawFrames.Count == 0)
                return 0;

            int t = 0;
            for (int i = 0; i < _rawFrames.Count; i++)
            {
                t += _rawFrames[i].DelayMs;
                if (elapsedMs < t)
                    return i;
            }

            return _rawFrames.Count - 1;
        }

        private int ElapsedMsUnlocked(double nowSeconds) =>
            (int)Math.Max(0, (nowSeconds - _startSeconds) * 1000.0);

        private bool IsEffectivelyEnabledUnlocked() =>
            _config.Enabled && DeveloperModeState.AreDistortionEffectsEnabled;

        private double NowUnlocked() =>
            _testNowSeconds ?? _clock.ElapsedSeconds;

        private void ClearFramesUnlocked()
        {
            if (_bitmaps != null)
                EmphasisExplosionGifDecoder.DisposeBitmaps(_bitmaps);
            _bitmaps = null;
            _rawFrames = null;
            _loadedPath = null;
            _loadAttempted = false;
            _playing = false;
            _totalDurationMs = 0;
            _maskBitmap = null;
            _maskBrush = null;
        }

        private static string NormalizePath(string? path) =>
            (path ?? string.Empty).Trim().Replace('\\', '/');

        private static string PathLeaf(string path)
        {
            string n = NormalizePath(path);
            int idx = n.LastIndexOf("Video/", StringComparison.OrdinalIgnoreCase);
            return idx >= 0 ? n.Substring(idx) : n;
        }

        /// <summary>Stopwatch wrapper so tests can override time without Avalonia.</summary>
        private sealed class StopwatchClock
        {
            private readonly System.Diagnostics.Stopwatch _sw = System.Diagnostics.Stopwatch.StartNew();
            public double ElapsedSeconds => _sw.Elapsed.TotalSeconds;
        }
    }
}
