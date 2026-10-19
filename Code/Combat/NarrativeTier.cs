using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// A narrative line plus the bank it came from.
    ///
    /// The bank is the signal every consumer previously had to guess at. The old crit filter
    /// sniffed the prose for "devastating blow" to work out whether a line was a critical hit;
    /// tiering needs the same answer and cannot be built on string matching. Generation knows
    /// the bank, so generation reports it.
    /// </summary>
    public readonly record struct TriggeredNarrative(string Bank, string Text);

    /// <summary>
    /// How much room a moment earns in the combat log.
    ///
    /// The model is borrowed from how fight scenes are actually written. Most exchanges are a
    /// ledger - a verb and a number - and one or two moments per fight get the full treatment.
    /// Contrast is the whole effect: rich prose only reads as rich when it is surrounded by
    /// terse. A log that narrates every swing at the same weight reads flat however good the
    /// sentences are, which is what the per-step sequence prose did.
    /// </summary>
    public enum NarrativeTier
    {
        /// <summary>No prose. The mechanical block alone.</summary>
        Ledger = 0,

        /// <summary>One short line, the world's own voice: third person, present tense.</summary>
        Beat = 1,

        /// <summary>The overseer. First person, past tense. Reserved for the turns.</summary>
        SetPiece = 2
    }

    /// <summary>
    /// Maps a narrative bank to the weight its moment deserves.
    ///
    /// Tier is decided by which bank fired, never by a dice roll. A fight where the critical
    /// hit is muted and a stance change speaks is worse than no pacing at all, so randomness
    /// belongs in *which line* a tier draws, never in *whether* a moment is allowed to speak.
    /// </summary>
    public static class NarrativeTiers
    {
        private static readonly Dictionary<string, NarrativeTier> TierByBank =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // The turns. These decide fights, and they are rare enough to hold.
                ["criticalHit"] = NarrativeTier.SetPiece,
                ["enemyDefeated"] = NarrativeTier.SetPiece,
                ["playerDefeated"] = NarrativeTier.SetPiece,
                ["below10Percent"] = NarrativeTier.SetPiece,

                // Real, but ordinary. One line in the world's voice.
                ["firstBlood"] = NarrativeTier.Beat,
                ["below50Percent"] = NarrativeTier.Beat,
                ["healthLeadChange"] = NarrativeTier.Beat,
                ["intenseBattle"] = NarrativeTier.Beat,
                ["criticalMiss"] = NarrativeTier.Beat,
                ["environmentalAction"] = NarrativeTier.Beat,
                ["goodCombo"] = NarrativeTier.Beat,

                // Noise at volume. Present in the data, silent in the log.
                ["healthRecovery"] = NarrativeTier.Ledger,
                ["playerTaunt"] = NarrativeTier.Ledger,
                ["enemyTaunt"] = NarrativeTier.Ledger
            };

        /// <summary>
        /// Tier for <paramref name="bank"/>. Unknown banks fall to <see cref="NarrativeTier.Beat"/>
        /// rather than Ledger, so a bank nobody has classified yet is still heard - silence by
        /// omission is the harder failure to notice.
        /// </summary>
        public static NarrativeTier For(string? bank)
        {
            if (string.IsNullOrEmpty(bank))
            {
                return NarrativeTier.Beat;
            }

            // Biome-suffixed taunt banks (playerTaunt_lava, enemyTaunt_crypt, ...) inherit the
            // family's tier rather than needing their own entry per biome.
            if (TierByBank.TryGetValue(bank, out var tier))
            {
                return tier;
            }

            int underscore = bank.IndexOf('_');
            if (underscore > 0 && TierByBank.TryGetValue(bank[..underscore], out var familyTier))
            {
                return familyTier;
            }

            return NarrativeTier.Beat;
        }

        /// <summary>True when this bank's prose should reach the combat log at all.</summary>
        public static bool Speaks(string? bank) => For(bank) != NarrativeTier.Ledger;
    }
}
