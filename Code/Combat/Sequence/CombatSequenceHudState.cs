using System.Collections.Generic;
using RPGGame;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence
{
    /// <summary>
    /// Live HUD panel under the action strip: all calculation steps laid out horizontally,
    /// with the active step highlighted. <see cref="IsBandReserved"/> keeps the two-row framed panel (plus a
    /// one-row gap above the combat log) for the dungeon run (exploration + combat) and the Action Lab —
    /// never on Skill Tree, hub, or other menus.
    /// F7 narrative mode can reveal columns by kind (selective) so a ROLL cell appears with its prose line,
    /// even when narrative order differs from left-to-right HUD order.
    /// </summary>
    public static class CombatSequenceHudState
    {
        /// <summary>
        /// When non-null, phases come from this set instead of left-to-right <see cref="CurrentIndex"/>.
        /// </summary>
        private static HashSet<int>? _selectivelyRevealed;

        public static bool IsBandReserved { get; set; }

        /// <summary>
        /// Sequence HUD is dungeon chrome from the moment a dungeon is selected (exploration + combat)
        /// and in the Action Lab sandbox. Hidden on Skill Tree, inventory, hub, selection, and completion.
        /// F7 narrative mode keeps the band reserved; poetic log lines drive selective column reveal.
        /// </summary>
        public static bool ShouldReserveBand(GameState? state) =>
            state == GameState.Combat
            || state == GameState.Dungeon
            || state == GameState.ActionInteractionLab;

        /// <summary>
        /// Aligns <see cref="IsBandReserved"/> with <paramref name="state"/>. No-ops when state is unknown
        /// so tests can set the flag directly. Leaving dungeon/combat clears leftover swing columns.
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

        /// <summary>True while F7 is revealing HUD columns by narrative kind (not left-to-right).</summary>
        public static bool IsSelectiveReveal => _selectivelyRevealed != null;

        public static bool HasVisibleSequence =>
            IsBandReserved
            && Steps.Count > 0
            && (CurrentIndex >= 0 || (_selectivelyRevealed != null && _selectivelyRevealed.Count > 0));

        public static void Begin(IReadOnlyList<CombatSequenceStep>? steps)
        {
            _selectivelyRevealed = null;
            Steps = steps != null && steps.Count > 0
                ? new List<CombatSequenceStep>(steps)
                : new List<CombatSequenceStep>();
            CurrentIndex = Steps.Count > 0 ? 0 : -1;
            ResultRevealed = false;
            VisibleResult = new List<ColoredText>();
        }

        /// <summary>
        /// Start a swing with no columns filled yet; F7 reveals each HUD kind when its prose line plays.
        /// </summary>
        public static void BeginSelective(IReadOnlyList<CombatSequenceStep>? steps)
        {
            Begin(steps);
            if (Steps.Count == 0)
                return;

            _selectivelyRevealed = new HashSet<int>();
            // Pending titles until the first narrative-correlated column reveals.
            CurrentIndex = -1;
            ResultRevealed = false;
            VisibleResult = new List<ColoredText>();
        }

        public static void SetActive(int index, bool resultRevealed, IReadOnlyList<ColoredText>? visibleResult = null)
        {
            CurrentIndex = index;
            ResultRevealed = resultRevealed;
            VisibleResult = visibleResult ?? new List<ColoredText>();
        }

        /// <summary>
        /// Marks the first unrevealed step of <paramref name="kind"/> complete (selective F7 mode).
        /// No-ops for narrative-only kinds (no HUD column) or when that column is absent.
        /// </summary>
        public static bool RevealStepKind(CombatSequenceStepKind kind)
        {
            if (_selectivelyRevealed == null || Steps.Count == 0)
                return false;
            if (CombatSequenceHudLayout.ColumnIndexFor(kind) < 0)
                return false;

            for (int i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Kind != kind)
                    continue;
                if (!_selectivelyRevealed.Add(i))
                    continue;

                CurrentIndex = i;
                ResultRevealed = true;
                VisibleResult = Steps[i].Result;
                return true;
            }

            return false;
        }

        /// <summary>Phase for a step index (selective set or left-to-right cursor).</summary>
        public static CombatSequenceHudLayout.Phase GetStepPhase(int stepIndex)
        {
            if (_selectivelyRevealed != null)
            {
                if (!_selectivelyRevealed.Contains(stepIndex))
                    return CombatSequenceHudLayout.Phase.Pending;
                if (stepIndex == CurrentIndex && CurrentIndex >= 0 && CurrentIndex < Steps.Count)
                    return CombatSequenceHudLayout.Phase.Active;
                return CombatSequenceHudLayout.Phase.Complete;
            }

            return CombatSequenceHudLayout.GetPhase(stepIndex, CurrentIndex);
        }

        /// <summary>Leave every column complete so the finished calculation stays visible until the next swing.</summary>
        public static void FinishSequence()
        {
            if (Steps.Count == 0)
            {
                ClearStep();
                return;
            }

            _selectivelyRevealed = null;
            CurrentIndex = Steps.Count;
            ResultRevealed = true;
        }

        public static void ClearStep()
        {
            _selectivelyRevealed = null;
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
