using System;
using System.Threading;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Runtime-only combat pacing / display state (not persisted). Page Up/Page Down move through
    /// the speed ladder (pause, 0.25x, 0.5x, 1x, 2x, 5x, 20x), scaling combat log / action-block pacing, message-type delays,
    /// progressive menu line delays, and chunked text reveal delays.
    /// F10 toggles zero-delay dev mode (<see cref="IsZeroDelayDevMode"/>): combat, room, travel,
    /// and reward pauses are skipped. F7 toggles the narrative combat log (sequence beats as flavor
    /// lines; HUD band stays up).
    /// F5 toggles the narrative combat-log video overlay (<see cref="IsNarrativeVideoFeedEnabled"/>).
    /// F6 toggles mouse-wind glyph distortion (<see cref="AreDistortionEffectsEnabled"/>).
    /// Does not change JSON config.
    /// </summary>
    public static class DeveloperModeState
    {
        /// <summary>Page Down walks left to slow motion and then pause (0); Page Up walks right to fast-forward.</summary>
        private static readonly double[] CombatSpeedSteps = { 0, 0.25, 0.5, 1, 2, 5, 20 };
        private const int DefaultCombatSpeedStepIndex = 3;
        /// <summary>Baseline pacing: 1x plays at the configured delays (half the earlier 2x baseline).</summary>
        private const double BaseCombatSpeedFactor = 1.0;
        private static readonly object SpeedLock = new object();
        private static int _combatSpeedStepIndex = DefaultCombatSpeedStepIndex;
        private static int _combatLogInstant;
        /// <summary>0 = off (default). F10 skips gameplay pacing delays.</summary>
        private static int _zeroDelayDevMode;
        /// <summary>0 = off (default). F7 narrative combat log (prose paragraphs); mechanical combat log is default.</summary>
        private static int _narrativeCombatLog = 0;
        /// <summary>0 = off (default). F5 narrative video overlay over the combat log.</summary>
        private static int _narrativeVideoFeed = 0;
        /// <summary>1 = on (default). Mouse-wind glyph sway / distortion. F6 toggles.</summary>
        private static int _distortionEffects = 1;

        /// <summary>
        /// True when combat/UI pacing should dump instantly. Includes the legacy instant latch and
        /// F10 zero-delay dev mode so every existing skip site honors the hotkey.
        /// </summary>
        public static bool IsCombatLogInstant =>
            Volatile.Read(ref _combatLogInstant) != 0 || IsZeroDelayDevMode;

        /// <summary>
        /// F10 runtime toggle (not persisted). When on, gameplay delays are skipped: combat log,
        /// action blocks, room intros, travel steps, and dungeon leave/reward pauses.
        /// </summary>
        public static bool IsZeroDelayDevMode => Volatile.Read(ref _zeroDelayDevMode) != 0;

        /// <summary>
        /// When true, combat plays sequence beats as timed poetic flavor lines in the log.
        /// Off by default (mechanical combat log). The two-row sequence HUD band stays reserved;
        /// animated HUD playback is skipped while narrative is on. Toggled with F7; not persisted.
        /// </summary>
        public static bool IsNarrativeCombatLog => Volatile.Read(ref _narrativeCombatLog) != 0;

        /// <summary>
        /// When true, the narrative MP4 overlay may play over the combat log when other gates pass.
        /// Off by default. Toggled with F5; not persisted. Does not rewrite <c>UIConfiguration.json</c>.
        /// </summary>
        public static bool IsNarrativeVideoFeedEnabled => Volatile.Read(ref _narrativeVideoFeed) != 0;

        /// <summary>
        /// When true (default), mouse-wind glyph sway / distortion is allowed if config enables it.
        /// Toggled with F6; not persisted. Does not rewrite <c>UIConfiguration.json</c>.
        /// </summary>
        public static bool AreDistortionEffectsEnabled => Volatile.Read(ref _distortionEffects) != 0;

        /// <summary>Current runtime combat speed multiplier: 0 (paused), 0.25x, 0.5x, 1x, 2x, 5x, or 20x.</summary>
        public static double CombatSpeedMultiplier
        {
            get
            {
                int index = Volatile.Read(ref _combatSpeedStepIndex);
                if (index < 0) return CombatSpeedSteps[0];
                if (index >= CombatSpeedSteps.Length) return CombatSpeedSteps[^1];
                return CombatSpeedSteps[index];
            }
        }

        /// <summary>True on the bottom Page Down step: live combat holds before its next beat until speed goes back up.</summary>
        public static bool IsCombatPaused => CombatSpeedMultiplier <= 0;

        public static bool IsCombatSpeedAccelerated => IsCombatLogInstant || CombatSpeedMultiplier > 1;

        public static string CombatSpeedLabel => FormatCombatSpeed(CombatSpeedMultiplier);

        public static string FormatCombatSpeed(double speed) =>
            speed <= 0 ? "paused" : $"{speed.ToString(System.Globalization.CultureInfo.InvariantCulture)}x";

        /// <summary>
        /// Waits while combat is paused (Page Down past 0.25x). Returns at once when not paused; polls so a Page Up
        /// from the UI thread resumes within a frame or two.
        /// </summary>
        public static async System.Threading.Tasks.Task WaitWhilePausedAsync(CancellationToken cancellationToken = default)
        {
            while (IsCombatPaused && !IsCombatLogInstant)
                await System.Threading.Tasks.Task.Delay(50, cancellationToken);
        }

        public static void SetCombatLogInstant(bool enabled) =>
            Volatile.Write(ref _combatLogInstant, enabled ? 1 : 0);

        public static void ToggleCombatLogInstant() =>
            SetCombatLogInstant(Volatile.Read(ref _combatLogInstant) == 0);

        public static void SetZeroDelayDevMode(bool enabled) =>
            Volatile.Write(ref _zeroDelayDevMode, enabled ? 1 : 0);

        /// <summary>Toggles F10 zero-delay dev mode. Returns the new enabled state.</summary>
        public static bool ToggleZeroDelayDevMode()
        {
            bool next = !IsZeroDelayDevMode;
            SetZeroDelayDevMode(next);
            return next;
        }

        public static void SetNarrativeCombatLog(bool enabled) =>
            Volatile.Write(ref _narrativeCombatLog, enabled ? 1 : 0);

        public static bool ToggleNarrativeCombatLog()
        {
            bool next = !IsNarrativeCombatLog;
            SetNarrativeCombatLog(next);
            return next;
        }

        public static void SetNarrativeVideoFeedEnabled(bool enabled) =>
            Volatile.Write(ref _narrativeVideoFeed, enabled ? 1 : 0);

        public static bool ToggleNarrativeVideoFeed()
        {
            bool next = !IsNarrativeVideoFeedEnabled;
            SetNarrativeVideoFeedEnabled(next);
            return next;
        }

        public static void SetDistortionEffectsEnabled(bool enabled) =>
            Volatile.Write(ref _distortionEffects, enabled ? 1 : 0);

        public static bool ToggleDistortionEffects()
        {
            bool next = !AreDistortionEffectsEnabled;
            SetDistortionEffectsEnabled(next);
            return next;
        }

        public static double IncreaseCombatSpeed()
        {
            lock (SpeedLock)
            {
                int nextIndex = Math.Min(CombatSpeedSteps.Length - 1, Volatile.Read(ref _combatSpeedStepIndex) + 1);
                Volatile.Write(ref _combatSpeedStepIndex, nextIndex);
                return CombatSpeedSteps[nextIndex];
            }
        }

        public static double DecreaseCombatSpeed()
        {
            lock (SpeedLock)
            {
                int nextIndex = Math.Max(0, Volatile.Read(ref _combatSpeedStepIndex) - 1);
                Volatile.Write(ref _combatSpeedStepIndex, nextIndex);
                return CombatSpeedSteps[nextIndex];
            }
        }

        public static void SetCombatSpeedMultiplier(double multiplier)
        {
            int targetIndex = 0;
            for (int i = 0; i < CombatSpeedSteps.Length; i++)
            {
                if (multiplier >= CombatSpeedSteps[i])
                    targetIndex = i;
            }

            Volatile.Write(ref _combatSpeedStepIndex, targetIndex);
        }

        public static int ScaleDelayMs(int delayMs)
        {
            if (delayMs <= 0 || IsCombatLogInstant)
                return 0;

            // Paused paces like the slowest step; the pause itself is the WaitWhilePausedAsync gate.
            double speed = Math.Max(CombatSpeedSteps[1], CombatSpeedMultiplier) * BaseCombatSpeedFactor;
            int scaled = Math.Max(1, (int)Math.Ceiling(delayMs / (double)speed));

            if (PreWeaponTrainingFlow.IsTutorialCombatSlowPacingActive)
            {
                scaled = (int)Math.Ceiling(scaled * TextDelayConfiguration.GetTutorialCombatDelayMultiplier());
            }

            return scaled;
        }
    }
}
