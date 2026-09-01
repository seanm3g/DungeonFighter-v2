using System;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>
    /// Resolve cards in the center arena. Current stays hidden until the sequence HUD lands on ACTION,
    /// then sits in the middle. Fighter past-actions archive left; enemy past-actions archive right.
    /// Enemy current/past cards use a red border.
    /// </summary>
    public static class FighterResolveActionStackState
    {
        private static ActionPanelInfo? _fighterPrevious;
        private static ActionPanelInfo? _enemyPrevious;
        private static ActionPanelInfo? _staged;
        private static bool _revealed;
        private static bool _stagedIsEnemy;

        /// <summary>Fighter past-action (left of center).</summary>
        public static ActionPanelInfo? Previous => _fighterPrevious;

        /// <summary>Enemy past-action (right of center).</summary>
        public static ActionPanelInfo? EnemyPrevious => _enemyPrevious;

        public static ActionPanelInfo? Current => _revealed ? _staged : null;

        public static bool CurrentIsEnemy => _revealed && _stagedIsEnemy && _staged.HasValue;

        public static bool EnemyPreviousIsEnemy => _enemyPrevious.HasValue;

        /// <summary>
        /// Stage a fighter or enemy resolving action. Visible after <see cref="RevealCurrent"/> (ACTION column),
        /// or immediately when the sequence HUD is not playing.
        /// </summary>
        public static void BeginResolve(Character actor, Action selectedAction)
        {
            if (actor == null || selectedAction == null)
                return;

            if (_revealed && _staged.HasValue)
                ArchiveCurrent();

            _staged = BuildStagedPanel(actor, selectedAction);
            _stagedIsEnemy = actor is Enemy;
            _revealed = !CombatSequencePresenter.ShouldPlay();
        }

        /// <summary>Show the staged card in the center (sequence HUD reached ACTION).</summary>
        public static void RevealCurrent()
        {
            if (_staged.HasValue)
                _revealed = true;
        }

        /// <summary>
        /// Move the resolving card to its past-action slot: fighter left, enemy right.
        /// </summary>
        public static void ArchiveCurrent()
        {
            if (!_staged.HasValue)
                return;
            if (_stagedIsEnemy)
                _enemyPrevious = _staged;
            else
                _fighterPrevious = _staged;
            _staged = null;
            _revealed = false;
            _stagedIsEnemy = false;
        }

        /// <summary>Legacy name for fighter-left archive; routes by whose card is current.</summary>
        public static void ArchiveCurrentToPrevious() => ArchiveCurrent();

        public static void Clear()
        {
            _fighterPrevious = null;
            _enemyPrevious = null;
            _staged = null;
            _revealed = false;
            _stagedIsEnemy = false;
        }

        internal static void ResetForTests()
        {
            Clear();
        }

        private static ActionPanelInfo? BuildStagedPanel(Character actor, Action selectedAction)
        {
            var panels = CombatActionStripBuilder.BuildPanelData(actor);
            var combo = ActionUtilities.GetComboActions(actor);
            if (panels.Count > 0 && combo.Count > 0)
            {
                int step = actor.ComboStep % combo.Count;
                int currentIndex = step;
                for (int i = 0; i < combo.Count; i++)
                {
                    if (ReferenceEquals(combo[i], selectedAction)
                        || string.Equals(combo[i]?.Name, selectedAction.Name, StringComparison.Ordinal))
                    {
                        currentIndex = i;
                        break;
                    }
                }

                if (currentIndex < 0 || currentIndex >= panels.Count)
                    currentIndex = Math.Clamp(step, 0, panels.Count - 1);
                return panels[currentIndex];
            }

            return CombatActionStripBuilder.BuildPanelForAction(actor, selectedAction);
        }
    }
}
