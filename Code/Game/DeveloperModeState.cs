using System;
using System.Threading;
using RPGGame.Config;

namespace RPGGame
{
    /// <summary>
    /// Runtime-only combat pacing / display state (not persisted). Page Up/Page Down move through
    /// the speed ladder, scaling combat log / action-block pacing, message-type delays,
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
        private static readonly int[] CombatSpeedSteps = { 1, 2, 5, 20 };
        /// <summary>Baseline pacing: 1x plays at what used to be the 2x step.</summary>
        private const int BaseCombatSpeedFactor = 2;
        private static readonly object SpeedLock = new object();
        private static int _combatSpeedStepIndex;
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

        /// <summary>Current runtime combat speed multiplier: 1x, 2x, 5x, or 20x.</summary>
        public static int CombatSpeedMultiplier
        {
            get
            {
                int index = Volatile.Read(ref _combatSpeedStepIndex);
                if (index < 0) return CombatSpeedSteps[0];
                if (index >= CombatSpeedSteps.Length) return CombatSpeedSteps[^1];
                return CombatSpeedSteps[index];
            }
        }

        public static bool IsCombatSpeedAccelerated => IsCombatLogInstant || CombatSpeedMultiplier > 1;

        public static string CombatSpeedLabel => $"{CombatSpeedMultiplier}x";

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

        public static int IncreaseCombatSpeed()
        {
            lock (SpeedLock)
            {
                int nextIndex = Math.Min(CombatSpeedSteps.Length - 1, Volatile.Read(ref _combatSpeedStepIndex) + 1);
                Volatile.Write(ref _combatSpeedStepIndex, nextIndex);
                return CombatSpeedSteps[nextIndex];
            }
        }

        public static int DecreaseCombatSpeed()
        {
            lock (SpeedLock)
            {
                int nextIndex = Math.Max(0, Volatile.Read(ref _combatSpeedStepIndex) - 1);
                Volatile.Write(ref _combatSpeedStepIndex, nextIndex);
                return CombatSpeedSteps[nextIndex];
            }
        }

        public static void SetCombatSpeedMultiplier(int multiplier)
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

            int speed = Math.Max(1, CombatSpeedMultiplier) * BaseCombatSpeedFactor;
            int scaled = Math.Max(1, (int)Math.Ceiling(delayMs / (double)speed));

            if (PreWeaponTrainingFlow.IsTutorialCombatSlowPacingActive)
            {
                scaled = (int)Math.Ceiling(scaled * TextDelayConfiguration.GetTutorialCombatDelayMultiplier());
            }

            return scaled;
        }
    }
}
