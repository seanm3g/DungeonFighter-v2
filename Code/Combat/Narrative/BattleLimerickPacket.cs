using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Compact battle context for victory limerick generation.</summary>
    public sealed class BattleLimerickPacket
    {
        public string HeroName { get; init; } = string.Empty;
        public string EnemyName { get; init; } = string.Empty;
        public string Location { get; init; } = string.Empty;
        public bool HeroWon { get; init; }
        public int HeroHpLeft { get; init; }
        public int HeroMaxHp { get; init; }
        public IReadOnlyList<string> Highlights { get; init; } = System.Array.Empty<string>();
        /// <summary>Equipped gear names (optional flavor for the verse).</summary>
        public IReadOnlyList<string> GearNames { get; init; } = System.Array.Empty<string>();
        /// <summary>Hero action names used in the fight (optional flavor).</summary>
        public IReadOnlyList<string> ActionNames { get; init; } = System.Array.Empty<string>();
        /// <summary>Loot found this dungeon run (optional flavor; filled at completion when known).</summary>
        public IReadOnlyList<string> LootNames { get; init; } = System.Array.Empty<string>();

        public string ToPromptBlock()
        {
            string epithet = VictoryHeroEpithet.FromName(HeroName);
            var sb = new System.Text.StringBuilder();
            sb.Append("Hero name (inspiration only — do NOT print this name): ").Append(HeroName).AppendLine();
            sb.Append("Suggested epithet (pun/metaphor for the hero): ").Append(epithet).AppendLine();
            sb.Append("Enemy: ").Append(EnemyName).AppendLine();
            sb.Append("Place: ").Append(string.IsNullOrWhiteSpace(Location) ? "the dungeon" : Location).AppendLine();
            sb.Append("Outcome: ").Append(HeroWon ? "hero victory" : "hero defeat");
            if (HeroWon && HeroMaxHp > 0)
            {
                double pct = 100.0 * HeroHpLeft / HeroMaxHp;
                string condition = pct >= 75 ? "barely scratched"
                    : pct >= 40 ? "bloodied but standing"
                    : "battered and nearly spent";
                sb.Append(", ").Append(condition);
            }
            sb.AppendLine();
            sb.AppendLine("Highlights:");
            if (Highlights.Count == 0)
            {
                sb.AppendLine("- a hard-fought clash of steel and will");
            }
            else
            {
                foreach (var line in Highlights)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    string scrubbed = VictoryHeroEpithet.ScrubHeroName(line.Trim(), HeroName, epithet);
                    sb.Append("- ").Append(scrubbed).AppendLine();
                }
            }

            AppendOptionalNames(sb, "Hero's gear", GearNames);
            AppendOptionalNames(sb, "Hero's moves", ActionNames);
            AppendOptionalNames(sb, "Loot found", LootNames);

            return sb.ToString().TrimEnd();
        }

        /// <summary>Optional name pools: the verse may borrow any of them alongside the enemy and place, none are required.</summary>
        private static void AppendOptionalNames(System.Text.StringBuilder sb, string label, IReadOnlyList<string> names)
        {
            var clean = new List<string>();
            foreach (var n in names)
            {
                if (string.IsNullOrWhiteSpace(n))
                    continue;
                string t = new string(n.Trim().Where(c => !char.IsDigit(c)).ToArray()).Trim();
                if (t.Length > 0 && !clean.Contains(t))
                    clean.Add(t);
            }
            if (clean.Count == 0)
                return;
            sb.Append(label).Append(" (optional, use any or none): ").Append(string.Join(", ", clean)).AppendLine();
        }
    }
}
