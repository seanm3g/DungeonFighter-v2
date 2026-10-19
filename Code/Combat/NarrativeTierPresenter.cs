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
        /// Lines to render beneath this swing's action block, or null when the swing stays a
        /// pure ledger entry. Null is the pre-prototype behaviour and the default.
        /// </summary>
        /// <summary>
        /// The one line this swing has earned, if any, with the tier that chose it.
        /// </summary>
        public static bool TryGetFlavorLine(BattleNarrative? battleNarrative, out string text, out NarrativeTier tier)
        {
            text = string.Empty;
            tier = NarrativeTier.Ledger;

            if (battleNarrative == null || !NarrativePacing.Active.NarratorEnabled)
            {
                return false;
            }

            var tagged = battleNarrative.GetTriggeredNarrativesTagged();
            if (tagged.Count == 0)
            {
                return false;
            }

            // A swing can trigger several banks at once (first blood and a lead change on the
            // same hit). Showing both flattens the moment, so the loudest tier wins.
            foreach (var entry in tagged)
            {
                if (string.IsNullOrWhiteSpace(entry.Text))
                {
                    continue;
                }

                var entryTier = NarrativeTiers.For(entry.Bank);
                if (entryTier > tier)
                {
                    tier = entryTier;
                    text = entry.Text;
                }
            }

            return tier != NarrativeTier.Ledger && !string.IsNullOrWhiteSpace(text);
        }

        public static List<List<ColoredText>>? BuildFlavorLines(BattleNarrative? battleNarrative)
        {

            if (battleNarrative == null || !NarrativePacing.Active.NarratorEnabled)
            {
                return null;
            }

            var tagged = battleNarrative.GetTriggeredNarrativesTagged();

            if (tagged.Count == 0)
            {
                return null;
            }

            // A swing can trigger several banks at once (first blood and a lead change on the
            // same hit). Showing both flattens the moment, so the loudest tier wins and the
            // rest stay quiet - that is the whole point of tiering.
            var best = NarrativeTier.Ledger;
            string? bestText = null;

            foreach (var entry in tagged)
            {
                if (string.IsNullOrWhiteSpace(entry.Text))
                {
                    continue;
                }

                var tier = NarrativeTiers.For(entry.Bank);
                if (tier > best)
                {
                    best = tier;
                    bestText = entry.Text;
                }
            }

            if (best == NarrativeTier.Ledger || string.IsNullOrWhiteSpace(bestText))
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
                .Add(Indent + bestText, best == NarrativeTier.SetPiece ? ColorPalette.Purple : ColorPalette.Gray)
                .Build();

            return new List<List<ColoredText>> { segments };
        }
    }
}
