using System.Threading;

namespace RPGGame
{
    /// <summary>
    /// Runtime-only developer toggle (not persisted). When enabled, combat log / action-block
    /// pacing, message-type delays after writes (including room/dungeon/system lines and colored text),
    /// progressive menu line delays, and chunked text reveal delays (console) are all treated as zero.
    /// Does not change JSON config. Toggled from the UI (e.g. F11).
    /// </summary>
    public static class DeveloperModeState
    {
        private static int _combatLogInstant;

        /// <summary>True when developer mode is on: combat-log pacing delays are treated as zero.</summary>
        public static bool IsCombatLogInstant => Volatile.Read(ref _combatLogInstant) != 0;

        public static void SetCombatLogInstant(bool enabled) =>
            Volatile.Write(ref _combatLogInstant, enabled ? 1 : 0);

        public static void ToggleCombatLogInstant() =>
            SetCombatLogInstant(!IsCombatLogInstant);
    }
}
