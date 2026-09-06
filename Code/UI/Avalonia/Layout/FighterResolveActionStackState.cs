using System;
using RPGGame;
using RPGGame.Actions;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI.Avalonia.Layout
{
    /// <summary>Stages actions until ACTION reveal and retains separate bounded histories for each side.</summary>
    public static class FighterResolveActionStackState
    {
        public readonly record struct PlayedCard(ActionPanelInfo Info, bool Unused, string Result = "");
        private static readonly System.Collections.Generic.List<PlayedCard> _fighterHistory = new();
        private static readonly System.Collections.Generic.List<PlayedCard> _enemyHistory = new();
        public static System.Collections.Generic.IReadOnlyList<PlayedCard> FighterHistory => _fighterHistory;
        public static System.Collections.Generic.IReadOnlyList<PlayedCard> EnemyHistory => _enemyHistory;
        private static ActionPanelInfo? _fighterPrevious;
        private static ActionPanelInfo? _enemyPrevious;
        private static ActionPanelInfo? _staged;
        private static bool _revealed;
        private static long _sourceId;
        public static string CurrentResult { get; private set; } = string.Empty;
        public static long ResultTimestamp { get; private set; }
        private static bool _stagedIsEnemy;
        private static bool _stagedMissed;
        private static bool _fighterPreviousMissed;
        private static bool _enemyPreviousMissed;

        /// <summary>Fighter past-action (left of center).</summary>
        public static ActionPanelInfo? Previous => _fighterPrevious;

        /// <summary>Enemy past-action (right of center).</summary>
        public static ActionPanelInfo? EnemyPrevious => _enemyPrevious;

        public static ActionPanelInfo? Current => _revealed ? _staged : null;

        public static bool CurrentIsEnemy => _revealed && _stagedIsEnemy && _staged.HasValue;

        public static bool EnemyPreviousIsEnemy => _enemyPrevious.HasValue;

        /// <summary>True when the visible centered card did not use the action (miss, fail, or plain hit).</summary>
        public static bool CurrentMissed => _revealed && _stagedMissed && _staged.HasValue;

        /// <summary>True when the fighter past-action card was unused (miss, fail, or plain hit).</summary>
        public static bool PreviousMissed => _fighterPreviousMissed && _fighterPrevious.HasValue;

        /// <summary>True when the enemy past-action card was unused (miss, fail, or plain hit).</summary>
        public static bool EnemyPreviousMissed => _enemyPreviousMissed && _enemyPrevious.HasValue;

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
            _sourceId = CombatVisualPlayback.ActorId(actor);
            CurrentResult = string.Empty;
            ResultTimestamp = 0;
            _stagedIsEnemy = actor is Enemy;
            _stagedMissed = false;
            _revealed = !CombatSequencePresenter.ShouldPlay();
        }

        /// <summary>
        /// Stamp a red X unless this swing used the action (combo or crit).
        /// Plain hits, misses, and fails leave the strip action unused.
        /// </summary>
        public static void ApplyResolveOutcome(bool isCombo, bool isCritical)
        {
            if (!isCombo && !isCritical)
                MarkCurrentMissed();
        }

        /// <summary>Stamp the staged card unused (already known when the ACTION column reveals).</summary>
        public static void MarkCurrentMissed()
        {
            if (_staged.HasValue)
                _stagedMissed = true;
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
            var history = _stagedIsEnemy ? _enemyHistory : _fighterHistory;
            history.Insert(0, new PlayedCard(_staged.Value, _stagedMissed, CurrentResult));
            if (history.Count > 12) history.RemoveAt(history.Count - 1);
            if (_stagedIsEnemy)
            {
                _enemyPrevious = _staged;
                _enemyPreviousMissed = _stagedMissed;
            }
            else
            {
                _fighterPrevious = _staged;
                _fighterPreviousMissed = _stagedMissed;
            }
            _staged = null;
            _revealed = false;
            _stagedIsEnemy = false;
            _stagedMissed = false;
        }

        /// <summary>Legacy name for fighter-left archive; routes by whose card is current.</summary>
        public static void ArchiveCurrentToPrevious() => ArchiveCurrent();

        public static void ApplyVisualResult(CombatVisualAction action, string phase)
        {
            if (action.SourceId != _sourceId || !_staged.HasValue || action.Name != _staged.Value.Name) return;
            if (phase == "effect") { if (!string.IsNullOrWhiteSpace(action.Effects)) CurrentResult += " · " + action.Effects; return; }
            if (phase is not ("impact" or "heal" or "miss" or "guard")) return;
            ResultTimestamp = System.Diagnostics.Stopwatch.GetTimestamp();
            CurrentResult = !action.Hit ? "Miss — no damage" : action.Heal > 0 ? $"Healed {action.Heal}" :
                $"{action.Damage} damage" + (action.Blocked is int block ? $" · {block} blocked" : "");
            if (action.Hit && !action.ActionUsed) CurrentResult += " · Basic hit";
        }

        public static void Clear()
        {
            CurrentResult = string.Empty;
            _sourceId = 0;
            ResultTimestamp = 0;
            _fighterHistory.Clear();
            _enemyHistory.Clear();
            _fighterPrevious = null;
            _enemyPrevious = null;
            _staged = null;
            _revealed = false;
            _stagedIsEnemy = false;
            _stagedMissed = false;
            _fighterPreviousMissed = false;
            _enemyPreviousMissed = false;
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
                int currentIndex = -1;
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
                    return CombatActionStripBuilder.BuildPanelForAction(actor, selectedAction);
                return panels[currentIndex];
            }

            return CombatActionStripBuilder.BuildPanelForAction(actor, selectedAction);
        }
    }
}




