using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Live HUD panel under the action strip during combat: all calculation steps laid out horizontally,
    /// with the active step highlighted. <see cref="IsBandReserved"/> keeps the two-row framed panel (plus a
    /// one-row gap above the combat log) only in Combat and the Action Lab — never on Skill Tree or other menus.
    /// </summary>
    public static class CombatSequenceHudState
    {
        public static bool IsBandReserved { get; set; }

        /// <summary>
        /// Sequence HUD is combat chrome: live fights and the Action Lab sandbox. Hidden everywhere else
        /// (Skill Tree, dungeon exploration, inventory, hub, completion).
        /// </summary>
        public static bool ShouldReserveBand(GameState? state) =>
            state == GameState.Combat
            || state == GameState.ActionInteractionLab;

        /// <summary>
        /// Aligns <see cref="IsBandReserved"/> with <paramref name="state"/>. No-ops when state is unknown
        /// so tests can set the flag directly. Leaving combat clears leftover swing columns.
        /// </summary>
        public static void SyncReservation(GameState? state)
        {
            if (state == null)
                return;

            bool next = ShouldReserveBand(state);
            if (!next)
                ClearStep();
            IsBandReserved = next;
        }

        public static IReadOnlyList<CombatSequenceStep> Steps { get; private set; } = new List<CombatSequenceStep>();

        /// <summary>Active column, or <see cref="Steps"/> count when the sequence has finished (all complete).</summary>
        public static int CurrentIndex { get; private set; } = -1;

        public static bool ResultRevealed { get; private set; }

        public static IReadOnlyList<ColoredText> VisibleResult { get; private set; } = new List<ColoredText>();

        public static bool HasVisibleSequence => IsBandReserved && Steps.Count > 0 && CurrentIndex >= 0;

        public static void Begin(IReadOnlyList<CombatSequenceStep>? steps)
        {
            Steps = steps != null && steps.Count > 0
                ? new List<CombatSequenceStep>(steps)
                : new List<CombatSequenceStep>();
            CurrentIndex = Steps.Count > 0 ? 0 : -1;
            ResultRevealed = false;
            VisibleResult = new List<ColoredText>();
        }

        public static void SetActive(int index, bool resultRevealed, IReadOnlyList<ColoredText>? visibleResult = null)
        {
            CurrentIndex = index;
            ResultRevealed = resultRevealed;
            VisibleResult = visibleResult ?? new List<ColoredText>();
        }

        /// <summary>Leave every column complete so the finished calculation stays visible until the next swing.</summary>
        public static void FinishSequence()
        {
            if (Steps.Count == 0)
            {
                ClearStep();
                return;
            }

            CurrentIndex = Steps.Count;
            ResultRevealed = true;
        }

        public static void ClearStep()
        {
            Steps = new List<CombatSequenceStep>();
            CurrentIndex = -1;
            ResultRevealed = false;
            VisibleResult = new List<ColoredText>();
        }

        public static void Reset()
        {
            IsBandReserved = false;
            ClearStep();
        }

        internal static void ResetForTests()
        {
            Reset();
        }
    }
}
