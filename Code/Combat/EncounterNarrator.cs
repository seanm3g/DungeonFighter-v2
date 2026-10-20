using System;
using System.Collections.Generic;
using System.Text;
using RPGGame.Combat.Sequence;
using RPGGame.UI.ColorSystem;

namespace RPGGame
{
    /// <summary>
    /// PROTOTYPE (flavor sandbox, branch joey_sandbox_v2 - not reviewed by Max).
    ///
    /// The overseer's line at the top of a fight: he names what is standing in front of you,
    /// then withdraws.
    ///
    /// Why this moment specifically. Before it existed he only spoke on crits, at ten percent
    /// and at death - every one of them late. By the time he first said anything the fight was
    /// half over and the player had read a dozen mechanical lines, so he read as an occasional
    /// effect rather than a presence. A Watcher establishes himself by being there BEFORE
    /// anything happens. The approved worked example opens on exactly such a line ("It was a
    /// man. It kept the shape and let the rest go.") and it had no bank; what shipped in that
    /// slot was AsciiArtAssets.EncounteredFormat, a hardcoded "A {0}{1} appears."
    ///
    /// It is also the one moment where enemy-specific canon pays off hardest. The Ghoul in the
    /// worked example gets its power from a fact about Ghouls - it goes for what a corpse
    /// carries before the corpse. That only lands if the line knows which creature it is
    /// looking at, and the encounter is where the player is looking at it.
    /// </summary>
    public static class EncounterNarrator
    {
        private const string BaseBank = "encounterOpening";

        /// <summary>
        /// Resolves the opening line, most specific bank first.
        ///
        ///   encounterOpening_&lt;enemy name&gt;   e.g. encounterOpening_ghoul
        ///   encounterOpening_&lt;tag&gt;          e.g. encounterOpening_undead, in the enemy's own tag order
        ///   encounterOpening                  the generic pool
        ///
        /// Ground rule 2: this never hardcodes which creatures or which tags exist. There are
        /// 129 enemies in Enemies.json and more will be added, so most of them will never have
        /// a bank of their own and must still get a line. Tags come straight off the enemy and
        /// are tried in the order the data lists them, which already runs most-significant
        /// first ("undead" before "frail", "elemental" before "fire"). A tag with no bank simply
        /// misses and costs nothing, so the anatomical tags (has_hands, has_legs) are skipped
        /// without needing a list of them in code - and writing encounterOpening_fire later
        /// needs no code change at all, which is the whole point.
        /// </summary>
        public static bool TryGetOpeningLine(Enemy? enemy, NarrativeTextProvider provider, out string text)
        {
            text = string.Empty;

            if (enemy == null || provider == null)
            {
                return false;
            }

            foreach (string bank in CandidateBanks(enemy))
            {
                if (provider.TryGetRandomNarrative(bank, out string line)
                    && !string.IsNullOrWhiteSpace(line))
                {
                    text = provider.ReplacePlaceholders(
                        line,
                        new Dictionary<string, string> { { "enemy", enemy.Name ?? string.Empty } });
                    return true;
                }
            }

            return false;
        }

        /// <summary>The bank names to try, most specific first.</summary>
        public static IEnumerable<string> CandidateBanks(Enemy enemy)
        {
            string name = NormalizeKey(enemy.Name);
            if (name.Length > 0)
            {
                yield return $"{BaseBank}_{name}";
            }

            var tags = enemy.Tags;
            if (tags != null)
            {
                foreach (string tag in tags)
                {
                    string key = NormalizeKey(tag);
                    if (key.Length > 0)
                    {
                        yield return $"{BaseBank}_{key}";
                    }
                }
            }

            yield return BaseBank;
        }

        /// <summary>
        /// Bank-key form of a name or tag: lowercase, letters and digits only.
        ///
        /// "Magma Beast" and "magma beast" and "Magma-Beast" all have to reach
        /// encounterOpening_magmabeast, because the bank is written by hand in JSON and the
        /// enemy name comes from a different file maintained by someone else.
        /// </summary>
        public static string NormalizeKey(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// The opening line as coloured segments, or null when the narrator is off or has
        /// nothing to say.
        ///
        /// Purple and flat across the line, the same treatment every other set piece gets - he
        /// is the same character here as he is on a crit, and the colour is the only thing
        /// carrying that across two completely different display paths.
        /// </summary>
        public static List<ColoredText>? BuildOpeningSegments(Enemy? enemy, NarrativeTextProvider provider)
        {
            if (!NarrativePacing.Active.NarratorEnabled)
            {
                return null;
            }

            if (!TryGetOpeningLine(enemy, provider, out string line))
            {
                return null;
            }

            return new ColoredTextBuilder().Add(line, ColorPalette.Purple).Build();
        }
    }
}
