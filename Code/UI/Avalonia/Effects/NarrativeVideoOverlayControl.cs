using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using RPGGame;
using RPGGame.UI.Avalonia.Layout;
using AvLayout = Avalonia.Layout;

namespace RPGGame.UI.Avalonia.Effects
{
    /// <summary>
    /// Soft-decodes an MP4 and paints it over the center combat-log band with per-cell opacity
    /// derived from narrative glyph luminance (plus optional halo). Playback starts only once
    /// the band has non-whitespace glyphs while F5 video feed, F7 narrative, and dungeon gates pass
    /// (not at window load / menus). Hit-test invisible.
    /// </summary>
    public sealed class NarrativeVideoOverlayControl : Control, IDisposable
    {
        private static bool _libVlcCoreInitialized;
        private static readonly object LibVlcInitLock = new();

        private readonly object _frameLock = new();
        private LibVLC? _libVlc;
        private MediaPlayer? _mediaPlayer;
        private Media? _media;
        private WriteableBitmap? _frameBitmap;
        private WriteableBitmap? _maskBitmap;
        private IntPtr _frameBuffer = IntPtr.Zero;
        private int _frameWidth;
        private int _frameHeight;
        private int _framePitch;
        private int _frameBufferBytes;
        private bool _frameDirty;
        private bool _disposed;
        private bool _playbackStarted;
        private bool _endReachedHooked;
        private int _restartQueued;
        private string? _loadedPath;

        private GameCanvasControl? _canvas;
        private NarrativeVideoOverlayConfig _config = new();
        private DispatcherTimer? _maskTimer;
        private float[]? _opacityGrid;
        private char[]? _glyphScratch;
        private Color[]? _colorScratch;
        private int _maskCols;
        private int _maskRows;
        private int _maskCellPxW = 1;
        private int _maskCellPxH = 1;
        private bool _forceMaskRebuild = true;
        /// <summary>True after the latest sample finds non-whitespace glyphs in the combat-log band.</summary>
        private bool _logHasContent;
        /// <summary>Optional live check for an active dungeon run (excludes victory/death screens).</summary>
        private Func<bool>? _inDungeonProvider;

        public NarrativeVideoOverlayControl()
        {
            IsHitTestVisible = false;
            Focusable = false;
            IsVisible = false;
            HorizontalAlignment = AvLayout.HorizontalAlignment.Left;
            VerticalAlignment = AvLayout.VerticalAlignment.Top;
        }

        /// <summary>Binds the main game canvas used for glyph sampling and panel metrics.</summary>
        public void AttachCanvas(GameCanvasControl canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            canvas.GridDimensionsChanged += OnGridChanged;
            EnsureMaskTimer();
        }

        /// <summary>
        /// Supplies a live active-dungeon-run check (typically
        /// <see cref="NarrativeVideoOverlayGate.CountsAsActiveDungeonRunForOverlay"/>).
        /// </summary>
        public void SetInDungeonProvider(Func<bool>? provider)
        {
            _inDungeonProvider = provider;
            UpdatePlaybackState();
        }

        /// <summary>Reloads settings from <see cref="UIConfiguration"/> and (re)starts playback if enabled.</summary>
        public void ReloadConfigAndMaybeStart()
        {
            try
            {
                _config = CloneConfig(UIConfiguration.LoadFromFile().NarrativeVideoOverlay);
            }
            catch
            {
                _config = new NarrativeVideoOverlayConfig();
            }

            _forceMaskRebuild = true;
            UpdateLayoutFromCanvas();
            RebuildMaskFromCanvas();
            UpdatePlaybackState();
        }

        /// <summary>
        /// Applies overlay knobs live (settings preview) without requiring a file reload.
        /// Clamps opacity / halo values to safe ranges.
        /// </summary>
        public void ApplyConfig(NarrativeVideoOverlayConfig? config)
        {
            _config = CloneConfig(config);
            _forceMaskRebuild = true;
            UpdateLayoutFromCanvas();
            RebuildMaskFromCanvas();
            UpdatePlaybackState();
            InvalidateVisual();
        }

        /// <summary>Current effective overlay config (copy-safe for settings UI).</summary>
        public NarrativeVideoOverlayConfig GetConfigSnapshot() => CloneConfig(_config);

        private static NarrativeVideoOverlayConfig CloneConfig(NarrativeVideoOverlayConfig? source) =>
            NarrativeVideoOverlayConfig.Normalize(source);

        /// <summary>Call when F7 narrative mode, dungeon membership, overlays, or combat context may have changed.</summary>
        public void NotifyContextChanged()
        {
            UpdateLayoutFromCanvas();
            RebuildMaskFromCanvas();
            UpdatePlaybackState();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _maskTimer?.Stop();
            _maskTimer = null;
            if (_canvas != null)
                _canvas.GridDimensionsChanged -= OnGridChanged;
            if (_mediaPlayer != null && _endReachedHooked)
            {
                _mediaPlayer.EndReached -= OnMediaEndReached;
                _endReachedHooked = false;
            }
            StopPlayback();
            FreeFrameBuffer();
            _frameBitmap = null;
            _maskBitmap = null;
            _media?.Dispose();
            _media = null;
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
            _libVlc?.Dispose();
            _libVlc = null;
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (!IsVisible || Bounds.Width <= 1 || Bounds.Height <= 1)
                return;

            WriteableBitmap? frame;
            lock (_frameLock)
            {
                if (_frameDirty && _frameBitmap != null && _frameBuffer != IntPtr.Zero && _frameWidth > 0)
                    CopyFrameBufferToBitmap(_frameBitmap);
                _frameDirty = false;
                frame = _frameBitmap;
            }

            if (frame == null || _maskBitmap == null)
                return;

            var dest = new Rect(0, 0, Bounds.Width, Bounds.Height);
            using (context.PushOpacityMask(new ImageBrush(_maskBitmap)
            {
                Stretch = Stretch.Fill,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            }, dest))
            {
                context.DrawImage(frame, new Rect(0, 0, frame.PixelSize.Width, frame.PixelSize.Height), dest);
            }
        }

        private void OnGridChanged()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _forceMaskRebuild = true;
                UpdateLayoutFromCanvas();
            }, DispatcherPriority.Render);
        }

        private void EnsureMaskTimer()
        {
            if (_maskTimer != null)
                return;
            _maskTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(66) // ~15 Hz mask refresh
            };
            _maskTimer.Tick += (_, _) =>
            {
                UpdateLayoutFromCanvas();
                RebuildMaskFromCanvas();
                UpdatePlaybackState();
            };
            _maskTimer.Start();
        }

        private void UpdateLayoutFromCanvas()
        {
            if (_canvas == null)
                return;

            double cw = _canvas.GetCharWidth();
            double ch = _canvas.GetCharHeight();
            if (cw <= 0 || ch <= 0)
                return;

            int x = LayoutConstants.CENTER_PANEL_X;
            int y = LayoutConstants.CENTER_PANEL_Y;
            int w = LayoutConstants.CENTER_PANEL_WIDTH;
            int h = LayoutConstants.CENTER_PANEL_HEIGHT;
            if (w <= 0 || h <= 0)
            {
                IsVisible = false;
                return;
            }

            Margin = new Thickness(x * cw, y * ch, 0, 0);
            Width = w * cw;
            Height = h * ch;

            int cellPxW = Math.Max(1, (int)Math.Round(cw));
            int cellPxH = Math.Max(1, (int)Math.Round(ch));
            if (cellPxW != _maskCellPxW || cellPxH != _maskCellPxH || w != _maskCols || h != _maskRows)
            {
                _maskCellPxW = cellPxW;
                _maskCellPxH = cellPxH;
                _maskCols = w;
                _maskRows = h;
                _forceMaskRebuild = true;
                EnsureScratchBuffers(w, h);
            }
        }

        private void EnsureScratchBuffers(int cols, int rows)
        {
            int n = cols * rows;
            if (_opacityGrid == null || _opacityGrid.Length < n)
                _opacityGrid = new float[n];
            if (_glyphScratch == null || _glyphScratch.Length < n)
                _glyphScratch = new char[n];
            if (_colorScratch == null || _colorScratch.Length < n)
                _colorScratch = new Color[n];
        }

        private void RebuildMaskFromCanvas()
        {
            if (_canvas == null || _maskCols <= 0 || _maskRows <= 0 || _opacityGrid == null
                || _glyphScratch == null || _colorScratch == null)
                return;

            if (!ShouldAllowOverlayMode())
            {
                _logHasContent = false;
                if (IsVisible)
                    IsVisible = false;
                return;
            }

            _canvas.SampleCombatLogCellColors(
                LayoutConstants.CENTER_PANEL_X,
                LayoutConstants.CENTER_PANEL_Y,
                _maskCols,
                _maskRows,
                _glyphScratch,
                _colorScratch);

            int cellCount = _maskCols * _maskRows;
            _logHasContent = NarrativeVideoCellMask.HasOccupiedGlyphs(_glyphScratch.AsSpan(0, cellCount));
            if (!_logHasContent)
            {
                if (IsVisible)
                    IsVisible = false;
                return;
            }

            NarrativeVideoCellMask.BuildOpacityGrid(
                _opacityGrid,
                _maskCols,
                _maskRows,
                _glyphScratch.AsSpan(0, cellCount),
                _colorScratch.AsSpan(0, cellCount),
                _config.MaxOpacity,
                _config.HaloCells,
                _config.HaloOpacityScale);

            int bmpW = Math.Max(1, _maskCols * _maskCellPxW);
            int bmpH = Math.Max(1, _maskRows * _maskCellPxH);
            if (_maskBitmap == null
                || _maskBitmap.PixelSize.Width != bmpW
                || _maskBitmap.PixelSize.Height != bmpH
                || _forceMaskRebuild)
            {
                _maskBitmap = new WriteableBitmap(
                    new PixelSize(bmpW, bmpH),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);
                _forceMaskRebuild = false;
            }

            var packed = new byte[bmpW * bmpH * 4];
            NarrativeVideoCellMask.WriteOpacityMaskBgra(
                packed, bmpW, bmpH, _opacityGrid, _maskCols, _maskRows, _maskCellPxW, _maskCellPxH);
            using (var fb = _maskBitmap.Lock())
            {
                if (fb.RowBytes == bmpW * 4)
                {
                    Marshal.Copy(packed, 0, fb.Address, packed.Length);
                }
                else
                {
                    for (int y = 0; y < bmpH; y++)
                    {
                        Marshal.Copy(packed, y * bmpW * 4, IntPtr.Add(fb.Address, y * fb.RowBytes), bmpW * 4);
                    }
                }
            }

            IsVisible = true;
            InvalidateVisual();
        }

        /// <summary>
        /// Config + F7 + dungeon-run gate. Playback also requires combat-log glyphs (<see cref="_logHasContent"/>).
        /// </summary>
        private bool ShouldAllowOverlayMode()
        {
            bool inDungeon = _inDungeonProvider?.Invoke() == true;
            return NarrativeVideoOverlayGate.ShouldAllowOverlayMode(
                _config.Enabled && DeveloperModeState.IsNarrativeVideoFeedEnabled,
                _disposed,
                _config.OnlyWhenNarrativeLog,
                DeveloperModeState.IsNarrativeCombatLog,
                _config.OnlyWhenInDungeon,
                inDungeon);
        }

        private bool ShouldShowOverlay() =>
            NarrativeVideoOverlayGate.ShouldShowOverlay(ShouldAllowOverlayMode(), _logHasContent);

        private void UpdatePlaybackState()
        {
            if (!ShouldShowOverlay())
            {
                StopPlayback();
                IsVisible = false;
                return;
            }

            string? path = ResolveVideoPath();
            if (path == null)
            {
                StopPlayback();
                IsVisible = false;
                return;
            }

            EnsureLibVlc();
            if (_libVlc == null || _mediaPlayer == null)
                return;

            if (!string.Equals(_loadedPath, path, StringComparison.OrdinalIgnoreCase))
            {
                StopPlayback();
                try
                {
                    _media?.Dispose();
                    // Prefer path URI; DASH/fMP4 (YouTube-style videoplayback) loops cleaner via
                    // EndReached restart than :input-repeat (avoids fragment sequence spam).
                    _media = new Media(_libVlc, path, FromType.FromPath);
                    _media.AddOption(":no-audio");
                    _mediaPlayer.Media = _media;
                    _loadedPath = path;
                    EnsureEndReachedHook();
                }
                catch
                {
                    _loadedPath = null;
                    return;
                }
            }

            _mediaPlayer.Mute = _config.MuteAudio;
            if (!_playbackStarted)
            {
                _mediaPlayer.Play();
                _playbackStarted = true;
            }
        }

        private void EnsureEndReachedHook()
        {
            if (_mediaPlayer == null || _endReachedHooked)
                return;
            _mediaPlayer.EndReached += OnMediaEndReached;
            _endReachedHooked = true;
        }

        private void OnMediaEndReached(object? sender, EventArgs e)
        {
            if (_disposed || !_config.Loop || !ShouldShowOverlay())
                return;
            // LibVLC forbids Play/Stop on the EndReached callback thread.
            if (Interlocked.Exchange(ref _restartQueued, 1) != 0)
                return;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    if (_disposed || _mediaPlayer == null)
                        return;
                    _mediaPlayer.Stop();
                    if (_config.Loop && ShouldShowOverlay())
                    {
                        _mediaPlayer.Play();
                        _playbackStarted = true;
                    }
                    else
                    {
                        _playbackStarted = false;
                    }
                }
                catch
                {
                    _playbackStarted = false;
                }
                finally
                {
                    Interlocked.Exchange(ref _restartQueued, 0);
                }
            });
        }

        private string? ResolveVideoPath()
        {
            string relative = (_config.FileName ?? "").Replace('/', Path.DirectorySeparatorChar);
            if (string.IsNullOrWhiteSpace(relative))
                return null;
            string? existing = GameConstants.TryGetExistingGameDataFilePath(relative);
            if (existing != null && File.Exists(existing))
                return existing;
            string candidate = GameConstants.GetGameDataFilePath(relative);
            return File.Exists(candidate) ? candidate : null;
        }

        private void EnsureLibVlc()
        {
            if (_mediaPlayer != null)
                return;
            try
            {
                lock (LibVlcInitLock)
                {
                    if (!_libVlcCoreInitialized)
                    {
                        Core.Initialize();
                        _libVlcCoreInitialized = true;
                    }
                }

                // Soft-decode into callbacks only; disable HW decode (mismatched with dummy vout)
                // and quiet demux chatter from DASH/fMP4 sources.
                // Use --avcodec-hw=none only; --no-hw-dec is not recognized by this LibVLC build
                // and caused ctor failure + retry spam ("unknown option '--no-hw-dec'").
                _libVlc = new LibVLC(
                    "--vout=dummy",
                    "--no-audio",
                    "--avcodec-hw=none",
                    "--quiet");
                _mediaPlayer = new MediaPlayer(_libVlc);
                _mediaPlayer.SetVideoFormatCallbacks(VideoFormat, VideoCleanup);
                _mediaPlayer.SetVideoCallbacks(LockVideo, null, DisplayVideo);
                _endReachedHooked = false;
            }
            catch
            {
                _mediaPlayer?.Dispose();
                _mediaPlayer = null;
                _libVlc?.Dispose();
                _libVlc = null;
            }
        }

        private void StopPlayback()
        {
            try
            {
                if (_mediaPlayer != null && _playbackStarted)
                    _mediaPlayer.Stop();
            }
            catch { /* ignore */ }
            _playbackStarted = false;
        }

        private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
        {
            byte[] fourcc = Encoding.ASCII.GetBytes("RV32");
            Marshal.Copy(fourcc, 0, chroma, 4);

            // Keep visible width/height; only pad pitches/lines (LibVLC recommends ×32).
            // Undersizing the buffer here causes h264 get_buffer() / no frame! spam.
            NarrativeVideoFrameLayout.ComputeRv32Storage(width, height, out pitches, out lines, out int bufferBytes);

            int w = (int)width;
            int h = (int)height;
            int pitch = (int)pitches;

            lock (_frameLock)
            {
                FreeFrameBuffer_NoLock();
                _frameWidth = w;
                _frameHeight = h;
                _framePitch = pitch;
                _frameBufferBytes = bufferBytes;
                _frameBuffer = Marshal.AllocHGlobal(bufferBytes);
            }

            Dispatcher.UIThread.Post(() =>
            {
                lock (_frameLock)
                {
                    if (_disposed || _frameWidth != w || _frameHeight != h)
                        return;
                    _frameBitmap = new WriteableBitmap(
                        new PixelSize(w, h),
                        new Vector(96, 96),
                        PixelFormat.Bgra8888,
                        AlphaFormat.Opaque);
                }
            }, DispatcherPriority.Render);

            return 1;
        }

        private void VideoCleanup(ref IntPtr opaque)
        {
            lock (_frameLock)
                FreeFrameBuffer_NoLock();
        }

        private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
        {
            lock (_frameLock)
            {
                if (_frameBuffer == IntPtr.Zero)
                {
                    // Decoder can lock during format teardown; never hand back null.
                    if (_frameBufferBytes <= 0)
                        _frameBufferBytes = (int)(NarrativeVideoFrameLayout.Alignment * NarrativeVideoFrameLayout.Alignment);
                    _frameBuffer = Marshal.AllocHGlobal(_frameBufferBytes);
                }
                Marshal.WriteIntPtr(planes, _frameBuffer);
            }
            return IntPtr.Zero;
        }

        private void DisplayVideo(IntPtr opaque, IntPtr picture)
        {
            lock (_frameLock)
                _frameDirty = true;
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        }

        private void CopyFrameBufferToBitmap(WriteableBitmap bitmap)
        {
            if (_frameBuffer == IntPtr.Zero || _frameWidth <= 0 || _frameHeight <= 0)
                return;
            using var fb = bitmap.Lock();
            int rows = Math.Min(fb.Size.Height, _frameHeight);
            // Copy visible RGB32 row only — pitch may be 32-aligned larger than width*4.
            int rowBytes = Math.Min(fb.RowBytes, _frameWidth * NarrativeVideoFrameLayout.BytesPerPixel);
            var row = new byte[rowBytes];
            for (int y = 0; y < rows; y++)
            {
                Marshal.Copy(_frameBuffer + y * _framePitch, row, 0, rowBytes);
                Marshal.Copy(row, 0, IntPtr.Add(fb.Address, y * fb.RowBytes), rowBytes);
            }
        }

        private void FreeFrameBuffer()
        {
            lock (_frameLock)
                FreeFrameBuffer_NoLock();
        }

        private void FreeFrameBuffer_NoLock()
        {
            if (_frameBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_frameBuffer);
                _frameBuffer = IntPtr.Zero;
            }
            _frameWidth = 0;
            _frameHeight = 0;
            _framePitch = 0;
            _frameBufferBytes = 0;
        }
    }
}
