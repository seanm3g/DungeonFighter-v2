using System.Collections.Generic;
using RPGGame.Combat.Sequence;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// PROTOTYPE (flavor sandbox, branch joey_sandbox_v2 - not reviewed by Max).
    ///
    /// Turns the last swing's triggered narratives into the lines that sit under the mechanical
    /// block, keeping only what its tier says is worth saying.
    ///
    /// Two voices share this slot, separated by person and tense so a reader never has to
    /// wonder who is speaking:
    ///   Beat     - the world. Third person, present tense. "Wounds open. Neither of them stops."
    ///   SetPiece - the overseer. First person, past tense. "He found the seam."
    /// Present tense is happening now; past tense is someone recounting it afterwards. The
    /// overseer is audibly outside the fight from his first word.
    /// </summary>
    public static class NarrativeTierPresenter
    {
        /// <summary>
        /// The combat log's continuation-row marker, not a cosmetic two-space inset.
        ///
        /// The centre panel decides left/right placement in
        /// <see cref="UI.Avalonia.Display.CombatCenterPanelEnemyLineAlignment"/>, and it decides
        /// it per row by asking two questions: is this row indented with
        /// <see cref="UI.BlockDisplay.BlockMessageCollector.ActionBlockSubsequentIndent"/>, and
        /// if not, does its text START WITH THE ENEMY'S NAME. A non-indented row that opens with
        /// the enemy's name right-aligns; everything else goes left.
        ///
        /// With a shorter indent the narrator's line was not recognised as a continuation row,
        /// so it fell through to the name sniff and its position on screen was decided by its
        /// FIRST WORD. "{name} put the whole fight through a gap" right-aligned because the bank
        /// line happens to open with the token; "Half of {name} is gone" left-aligned onto the
        /// player's side while describing the enemy. Rewording a bank moved the line across the
        /// screen, which makes layout a property of the prose - the same magic-string coupling
        /// the TriggeredNarrative bank tag exists to kill, and a direct breach of ground rule 2:
        /// adding content must never require a code change, and a format that only lays out
        /// correctly for one phrasing is not a format.
        ///
        /// Using the real marker makes the line a continuation of the block it describes, so it
        /// inherits that swing's side. Enemy swing, enemy side; player swing, player side;
        /// independent of wording, for every line Joey writes from here on.
        /// </summary>
        private const string Indent = UI.BlockDisplay.BlockMessageCollector.ActionBlockSubsequentIndent;

        /// <summary>
        /// Per-battle memory for the tier decision.
        ///
        /// Held in a ConditionalWeakTable keyed on the BattleNarrative rather than in a static
        /// field, because a static would carry one fight's cooldown into the next one and there
        /// is no reset hook to hang a clear off. A battle's entry dies with the battle.
        /// </summary>
        private sealed class SwingMemo
        {
            public int DecidedForSwing = -1;
            public NarrativeTier DecidedTier = NarrativeTier.Ledger;
            public string DecidedText = string.Empty;
            public int LastSetPieceSwing = int.MinValue;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BattleNarrative, SwingMemo> Memos = new();

        /// <summary>
        /// Banks that end the fight. They fire once, so they can never produce a run of purple,
        /// and they carry the moment the whole fight was heading towards.
        /// </summary>
        private static readonly string[] TerminalBanks = { "enemyDefeated", "playerDefeated" };

        /// <summary>
        /// True when the line chosen for this swing came from a bank that ends the fight.
        /// Matched on the BANK, never on the prose - the same rule the crit filter broke.
        /// </summary>
        private static bool IsTerminal(List<TriggeredNarrative> tagged, string chosenText)
        {
            foreach (var entry in tagged)
            {
                if (!string.Equals(entry.Text, chosenText, System.StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (string bank in TerminalBanks)
                {
                    if (string.Equals(entry.Bank, bank, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The one line this swing has earned, if any, with the tier that chose it.
        ///
        /// Both display surfaces ask through here, so they always agree, and the set-piece
        /// cooldown is applied exactly once per swing however many times it is asked.
        /// </summary>
        public static bool TryGetFlavorLine(BattleNarrative? battleNarrative, out string text, out NarrativeTier tier)
        {
            tier = Resolve(battleNarrative, out text);
            return tier != NarrativeTier.Ledger && !string.IsNullOrWhiteSpace(text);
        }

        /// <summary>
        /// Lines to render beneath this swing's action block, or null when the swing stays a
        /// pure ledger entry. Null is the pre-prototype behaviour and the default.
        /// </summary>
        public static List<List<ColoredText>>? BuildFlavorLines(BattleNarrative? battleNarrative)
        {
            var tier = Resolve(battleNarrative, out string text);
            if (tier == NarrativeTier.Ledger || string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            // The indent is part of the text segment, NOT a segment of its own.
            // ColoredTextBuilder.Build runs SpacingHelper.ProcessSegments, which normalizes any
            // whitespace-only segment to a single space - so an indent added separately was
            // silently collapsed to one space and never matched the continuation marker the
            // centre panel looks for. That collapse is the root cause of the placement bug:
            // without the marker every narrator line was treated as a primary line and had its
            // side decided by whether its first word was the enemy's name.
            //
            // Carrying the indent inside the coloured segment also satisfies decision 13's
            // "flat across the line" - one colour, no two-tone leading run.
            var segments = new ColoredTextBuilder()
                .Add(Indent + text, tier == NarrativeTier.SetPiece ? ColorPalette.Purple : ColorPalette.Gray)
                .Build();

            return new List<List<ColoredText>> { segments };
        }

        /// <summary>
        /// Picks this swing's line and tier, applying the set-piece cooldown.
        ///
        /// Idempotent per swing: the live path asks twice, once for the mechanical block and
        /// once for the prose presenter, and the second question must neither change the answer
        /// nor count as another swing against the cooldown.
        /// </summary>
        private static NarrativeTier Resolve(BattleNarrative? battleNarrative, out string text)
        {
            text = string.Empty;

            if (battleNarrative == null || !NarrativePacing.Active.NarratorEnabled)
            {
                return NarrativeTier.Ledger;
            }

            int swing = battleNarrative.CurrentEventIndex;
            if (swing < 0)
            {
                return NarrativeTier.Ledger;
            }

            var memo = Memos.GetOrCreateValue(battleNarrative);
            lock (memo)
            {
                if (memo.DecidedForSwing == swing)
                {
                    text = memo.DecidedText;
                    return memo.DecidedTier;
                }

                var tagged = battleNarrative.GetTriggeredNarrativesTagged();

                // A swing can trigger several banks at once (first blood and a lead change on
                // the same hit). Showing both flattens the moment, so the loudest tier wins and
                // the rest stay quiet - that is the whole point of tiering. The runner-up is
                // kept because the cooldown may need to fall back to it.
                var best = NarrativeTier.Ledger;
                string bestText = string.Empty;
                var runnerUp = NarrativeTier.Ledger;
                string runnerUpText = string.Empty;

                foreach (var entry in tagged)
                {
                    if (string.IsNullOrWhiteSpace(entry.Text))
                    {
                        continue;
                    }

                    var entryTier = NarrativeTiers.For(entry.Bank);
                    if (entryTier > best)
                    {
                        runnerUp = best;
                        runnerUpText = bestText;
                        best = entryTier;
                        bestText = entry.Text;
                    }
                    else if (entryTier > runnerUp && entryTier < best)
                    {
                        runnerUp = entryTier;
                        runnerUpText = entry.Text;
                    }
                }

                // The set-piece adjacency rule.
                //
                // below10Percent fires when the enemy crosses 10% and enemyDefeated fires when
                // it dies, and those are nearly always a swing or two apart - so without this,
                // the normal ending of a fight is two purple lines in a row. That is not just
                // dense: it makes the overseer contradict himself, because "I will not say
                // which way" is followed immediately by saying which way. Withholding is one of
                // the five rules his voice runs on.
                //
                // Rarity is also the whole mechanism decision 13 rests on. Two adjacent purple
                // lines are a block of purple, not a highlight, and von Restorff isolation only
                // pays out when the isolated thing is actually isolated.
                //
                // A suppressed set piece falls back to this swing's Beat if it has one, so the
                // moment is still marked in the world's voice, and goes silent otherwise.
                // The ending is exempt. A death happens once per fight, so it can never cause a
                // run of purple, and muting it costs the payoff of the whole fight - the
                // cooldown's first draft did exactly that, keeping "Skeleton is nearly out" and
                // silencing the blow that killed it. The pairing this rule exists to prevent is
                // handled upstream instead: below10Percent stands down when the fight is one
                // hit from over (NarrativeTriggerEvaluator.IsOneHitFromTheEnd).
                bool terminal = IsTerminal(tagged, bestText);

                if (best == NarrativeTier.SetPiece && !terminal)
                {
                    int cooldown = NarrativePacing.Active.SetPieceCooldownSwings;
                    if (cooldown > 0
                        && memo.LastSetPieceSwing != int.MinValue
                        && swing - memo.LastSetPieceSwing <= cooldown)
                    {
                        best = runnerUp;
                        bestText = runnerUpText;
                    }
                }

                if (best == NarrativeTier.Ledger || string.IsNullOrWhiteSpace(bestText))
                {
                    best = NarrativeTier.Ledger;
                    bestText = string.Empty;
                }
                else if (best == NarrativeTier.SetPiece)
                {
                    memo.LastSetPieceSwing = swing;
                }

                memo.DecidedForSwing = swing;
                memo.DecidedTier = best;
                memo.DecidedText = bestText;

                text = bestText;
                return best;
            }
        }
    }
}
