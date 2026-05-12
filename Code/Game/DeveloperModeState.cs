using System;
using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Runtime-only combat pacing state (not persisted). Page Up/Page Down move through
    /// the speed ladder, scaling combat log / action-block pacing, message-type delays,
    /// progressive menu line delays, and chunked text reveal delays.
    /// Does not change JSON config.
    /// </summary>
    public static class DeveloperModeState
    {
        private static readonly int[] CombatSpeedSteps = { 1, 2, 5, 20 };
        private static readonly object SpeedLock = new object();
        private static int _combatSpeedStepIndex;
        private static int _combatLogInstant;

        /// <summary>Legacy instant toggle. New UI uses <see cref="CombatSpeedMultiplier"/> instead.</summary>
        public static bool IsCombatLogInstant => Volatile.Read(ref _combatLogInstant) != 0;

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
            SetCombatLogInstant(!IsCombatLogInstant);

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

            int speed = Math.Max(1, CombatSpeedMultiplier);
            return Math.Max(1, (int)Math.Ceiling(delayMs / (double)speed));
        }
    }
}
