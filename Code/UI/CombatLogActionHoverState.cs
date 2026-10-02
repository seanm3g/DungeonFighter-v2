using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Threading;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI
{
    /// <summary>
    /// Tracks which narrative combat-log prose paragraph is under the pointer (F7),
    /// and holds that swing's mechanical combat-log info tip lines.
    /// Tip appears only after a continuous hover of <see cref="ShowDelayMs"/>;
    /// a click while hovering dismisses it until the pointer leaves that paragraph.
    /// </summary>
    public static class CombatLogActionHoverState
    {
        public const int ShowDelayMs = 1200;

        private static readonly object Lock = new();
        private static readonly TimeSpan TimerPollInterval = TimeSpan.FromMilliseconds(50);

        private static List<List<ColoredText>>? _infoLines;
        private static string _fingerprint = "";
        private static int _targetX;
        private static int _targetY;
        private static int _targetWidth;
        private static int _targetHeight;
        private static bool _hasTarget;

        private static string _hoverFingerprint = "";
        private static List<List<ColoredText>>? _pendingLines;
        private static int _pendingX;
        private static int _pendingY;
        private static int _pendingWidth;
        private static int _pendingHeight;
        private static DateTimeOffset _hoverStartedAt;
        private static string _dismissedFingerprint = "";
        private static bool _isPending;

        private static DispatcherTimer? _revealTimer;
        private static System.Action? _visibilityChanged;

        /// <summary>For unit tests: fixed clock; when null, <see cref="DateTimeOffset.UtcNow"/> is used.</summary>
        internal static Func<DateTimeOffset>? UtcNowProviderForTests;

        private static DateTimeOffset Now() => UtcNowProviderForTests?.Invoke() ?? DateTimeOffset.UtcNow;

        /// <summary>
        /// Invoked on the UI thread when the tip becomes visible or is cleared by the reveal timer.
        /// </summary>
        public static void SetVisibilityChangedCallback(System.Action? callback)
        {
            lock (Lock)
                _visibilityChanged = callback;
        }

        public static IReadOnlyList<List<ColoredText>>? InfoLines
        {
            get
            {
                lock (Lock) return _infoLines;
            }
        }

        public static bool IsActive
        {
            get
            {
                lock (Lock) return _infoLines != null && _infoLines.Count > 0;
            }
        }

        /// <summary>Pointer is over a tip target waiting for <see cref="ShowDelayMs"/> (not dismissed, not yet shown).</summary>
        public static bool IsPending
        {
            get
            {
                lock (Lock) return _isPending;
            }
        }

        public static bool TryGetTargetBounds(out int y, out int height)
        {
            return TryGetTargetBounds(out _, out y, out _, out height);
        }

        /// <summary>
        /// Prose band under the tip (same grid rect registered by <see cref="CombatLogProseHoverMap"/>).
        /// </summary>
        public static bool TryGetTargetBounds(out int x, out int y, out int width, out int height)
        {
            lock (Lock)
            {
                x = _targetX;
                y = _targetY;
                width = _targetWidth;
                height = _targetHeight;
                return _hasTarget && _infoLines != null && _infoLines.Count > 0;
            }
        }

        /// <summary>
        /// Updates from <see cref="CombatLogProseHoverMap"/>. Returns true when the visible tip changed.
        /// </summary>
        public static bool UpdateFromPointer(int gridX, int gridY)
        {
            List<List<ColoredText>>? next = null;
            int nextX = 0;
            int nextY = 0;
            int nextW = 0;
            int nextH = 0;
            bool hasBounds = false;
            if (CombatLogProseHoverMap.TryHit(gridX, gridY, out var hitLines, out int hitX, out int hitY, out int hitW, out int hitH)
                && hitLines.Count > 0)
            {
                next = hitLines;
                nextX = hitX;
                nextY = hitY;
                nextW = hitW;
                nextH = hitH;
                hasBounds = true;
            }

            string nextFp = CombatLogProseHoverInfo.Fingerprint(next);

            lock (Lock)
            {
                bool wasActive = _infoLines != null && _infoLines.Count > 0;

                if (!hasBounds || next == null || next.Count == 0)
                {
                    StopRevealTimer_NoLock();
                    ClearHoverAndShown_NoLock();
                    _dismissedFingerprint = "";
                    return wasActive;
                }

                if (string.Equals(_dismissedFingerprint, nextFp, StringComparison.Ordinal))
                {
                    _hoverFingerprint = nextFp;
                    _pendingLines = next;
                    _pendingX = nextX;
                    _pendingY = nextY;
                    _pendingWidth = nextW;
                    _pendingHeight = nextH;
                    _isPending = false;
                    StopRevealTimer_NoLock();
                    if (wasActive)
                        ClearShown_NoLock();
                    return wasActive;
                }

                if (!string.Equals(_hoverFingerprint, nextFp, StringComparison.Ordinal))
                {
                    _hoverFingerprint = nextFp;
                    _pendingLines = next;
                    _pendingX = nextX;
                    _pendingY = nextY;
                    _pendingWidth = nextW;
                    _pendingHeight = nextH;
                    _hoverStartedAt = Now();
                    _isPending = true;
                    if (wasActive)
                        ClearShown_NoLock();
                    EnsureRevealTimer_NoLock();
                    return wasActive;
                }

                _pendingLines = next;
                _pendingX = nextX;
                _pendingY = nextY;
                _pendingWidth = nextW;
                _pendingHeight = nextH;

                if (wasActive)
                {
                    bool boundsChanged = _targetX != nextX || _targetY != nextY
                        || _targetWidth != nextW || _targetHeight != nextH;
                    _infoLines = next;
                    _fingerprint = nextFp;
                    _targetX = nextX;
                    _targetY = nextY;
                    _targetWidth = nextW;
                    _targetHeight = nextH;
                    _hasTarget = true;
                    _isPending = false;
                    StopRevealTimer_NoLock();
                    return boundsChanged;
                }

                if ((Now() - _hoverStartedAt).TotalMilliseconds >= ShowDelayMs)
                {
                    ActivatePending_NoLock();
                    StopRevealTimer_NoLock();
                    return true;
                }

                _isPending = true;
                EnsureRevealTimer_NoLock();
                return false;
            }
        }

        /// <summary>
        /// Click while hovering a prose tip target: hide the tip (if shown) and keep it hidden
        /// until the pointer leaves that paragraph.
        /// </summary>
        /// <returns>True when the visible tip was cleared and needs a redraw.</returns>
        public static bool TryDismissFromClick(int gridX, int gridY)
        {
            if (!CombatLogProseHoverMap.TryHit(gridX, gridY, out var hitLines, out _, out _)
                || hitLines.Count == 0)
                return false;

            string fp = CombatLogProseHoverInfo.Fingerprint(hitLines);
            lock (Lock)
            {
                if (!string.Equals(_hoverFingerprint, fp, StringComparison.Ordinal)
                    && !string.Equals(_fingerprint, fp, StringComparison.Ordinal))
                    return false;

                _dismissedFingerprint = fp;
                _isPending = false;
                StopRevealTimer_NoLock();
                bool wasActive = _infoLines != null && _infoLines.Count > 0;
                if (wasActive)
                    ClearShown_NoLock();
                return wasActive;
            }
        }

        public static void Clear()
        {
            lock (Lock)
            {
                StopRevealTimer_NoLock();
                ClearHoverAndShown_NoLock();
                _dismissedFingerprint = "";
                UtcNowProviderForTests = null;
            }
        }

        private static void ActivatePending_NoLock()
        {
            _infoLines = _pendingLines;
            _fingerprint = _hoverFingerprint;
            _targetX = _pendingX;
            _targetY = _pendingY;
            _targetWidth = _pendingWidth;
            _targetHeight = _pendingHeight;
            _hasTarget = _infoLines != null && _infoLines.Count > 0;
            _isPending = false;
        }

        private static void ClearShown_NoLock()
        {
            _infoLines = null;
            _fingerprint = "";
            _targetX = 0;
            _targetY = 0;
            _targetWidth = 0;
            _targetHeight = 0;
            _hasTarget = false;
        }

        private static void ClearHoverAndShown_NoLock()
        {
            ClearShown_NoLock();
            _hoverFingerprint = "";
            _pendingLines = null;
            _pendingX = 0;
            _pendingY = 0;
            _pendingWidth = 0;
            _pendingHeight = 0;
            _hoverStartedAt = default;
            _isPending = false;
        }

        private static void EnsureRevealTimer_NoLock()
        {
            if (Application.Current == null)
                return;

            if (_revealTimer == null)
            {
                _revealTimer = new DispatcherTimer { Interval = TimerPollInterval };
                _revealTimer.Tick += OnRevealTimerTick;
            }

            if (!_revealTimer.IsEnabled)
                _revealTimer.Start();
        }

        private static void StopRevealTimer_NoLock()
        {
            _revealTimer?.Stop();
        }

        private static void OnRevealTimerTick(object? sender, EventArgs e)
        {
            bool becameActive = false;
            System.Action? notify = null;
            lock (Lock)
            {
                bool dismissed = _dismissedFingerprint.Length > 0
                    && string.Equals(_dismissedFingerprint, _hoverFingerprint, StringComparison.Ordinal);
                if (!_isPending || dismissed)
                {
                    StopRevealTimer_NoLock();
                    return;
                }

                if ((Now() - _hoverStartedAt).TotalMilliseconds < ShowDelayMs)
                    return;

                bool wasActive = _infoLines != null && _infoLines.Count > 0;
                ActivatePending_NoLock();
                StopRevealTimer_NoLock();
                becameActive = !wasActive && _infoLines != null && _infoLines.Count > 0;
                if (becameActive)
                    notify = _visibilityChanged;
            }

            notify?.Invoke();
        }
    }
}
