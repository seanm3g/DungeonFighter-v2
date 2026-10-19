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
        private const string Indent = "  ";

        /// <summary>
        /// Lines to render beneath this swing's action block, or null when the swing stays a
        /// pure ledger entry. Null is the pre-prototype behaviour and the default.
        /// </summary>
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

            var segments = new ColoredTextBuilder()
                .Add(Indent, ColorPalette.Info)
                .Add(bestText, best == NarrativeTier.SetPiece ? ColorPalette.Info : ColorPalette.Gray)
                .Build();

            return new List<List<ColoredText>> { segments };
        }
    }
}
