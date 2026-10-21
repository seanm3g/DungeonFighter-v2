using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.Combat.Narrative
{
    /// <summary>Builds a <see cref="BattleLimerickPacket"/> from combat end state.</summary>
    public static class BattleLimerickPacketBuilder
    {
        public const int MaxHighlights = 5;

        public static BattleLimerickPacket Build(
            Character hero,
            Enemy enemy,
            string? location,
            bool heroWon,
            FunMomentTracker? funMoments,
            BattleNarrative? battleNarrative = null)
        {
            if (hero == null) throw new ArgumentNullException(nameof(hero));
            if (enemy == null) throw new ArgumentNullException(nameof(enemy));

            var highlights = new List<string>();

            var summary = funMoments?.GetSummary();
            if (summary?.TopMoments != null)
            {
                foreach (var moment in summary.TopMoments
                             .OrderByDescending(m => m.Intensity)
                             .Take(MaxHighlights))
                {
                    if (!string.IsNullOrWhiteSpace(moment.Description))
                        highlights.Add(SanitizeHighlight(moment.Description));
                }
            }

            if (highlights.Count < 2 && battleNarrative != null)
            {
                foreach (var evt in battleNarrative.GetAllEvents().TakeLast(8))
                {
                    if (highlights.Count >= MaxHighlights)
                        break;
                    string? line = DescribeEvent(evt);
                    if (!string.IsNullOrWhiteSpace(line) && !highlights.Contains(line))
                        highlights.Add(line);
                }
            }

            return new BattleLimerickPacket
            {
                GearNames = CollectGearNames(hero),
                ActionNames = CollectActionNames(hero, battleNarrative),
                HeroName = hero.Name ?? "Hero",
                EnemyName = enemy.Name ?? "Foe",
                Location = string.IsNullOrWhiteSpace(location) ? "the dungeon" : location.Trim(),
                HeroWon = heroWon,
                HeroHpLeft = Math.Max(0, hero.CurrentHealth),
                HeroMaxHp = Math.Max(1, hero.GetEffectiveMaxHealth()),
                Highlights = highlights
            };
        }

        public const int MaxNames = 4;

        /// <summary>Equipped weapon first, then armor pieces.</summary>
        internal static List<string> CollectGearNames(Character hero)
        {
            var names = new List<string>();
            foreach (var item in new[] { hero.Weapon, hero.Head, hero.Body, hero.Legs, hero.Feet })
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Name) && names.Count < MaxNames)
                    names.Add(item.Name.Trim());
            }
            return names;
        }

        /// <summary>Actions the hero actually used this fight (most recent first), else their combo strip.</summary>
        internal static List<string> CollectActionNames(Character hero, BattleNarrative? battleNarrative)
        {
            var names = new List<string>();
            if (battleNarrative != null)
            {
                foreach (var evt in battleNarrative.GetAllEvents().AsEnumerable().Reverse())
                {
                    if (names.Count >= MaxNames)
                        break;
                    if (evt == null || !string.Equals(evt.Actor, hero.Name, StringComparison.Ordinal)
                        || string.IsNullOrWhiteSpace(evt.Action))
                        continue;
                    string action = evt.Action.Trim();
                    if (!names.Contains(action, StringComparer.OrdinalIgnoreCase))
                        names.Add(action);
                }
            }
            if (names.Count == 0)
            {
                foreach (var a in hero.GetComboActions())
                {
                    if (names.Count >= MaxNames)
                        break;
                    if (!string.IsNullOrWhiteSpace(a?.Name) && !names.Contains(a.Name, StringComparer.OrdinalIgnoreCase))
                        names.Add(a.Name.Trim());
                }
            }
            return names;
        }

        /// <summary>Copy of <paramref name="packet"/> with loot names attached.</summary>
        public static BattleLimerickPacket WithLoot(BattleLimerickPacket packet, IEnumerable<string> lootNames) =>
            new BattleLimerickPacket
            {
                HeroName = packet.HeroName,
                EnemyName = packet.EnemyName,
                Location = packet.Location,
                HeroWon = packet.HeroWon,
                HeroHpLeft = packet.HeroHpLeft,
                HeroMaxHp = packet.HeroMaxHp,
                Highlights = packet.Highlights,
                GearNames = packet.GearNames,
                ActionNames = packet.ActionNames,
                LootNames = lootNames.Where(n => !string.IsNullOrWhiteSpace(n)).Take(MaxNames).ToList()
            };

        private static string SanitizeHighlight(string text)
        {
            // Strip digit runs so prompts stay narrative-friendly.
            var chars = text.Where(c => !char.IsDigit(c)).ToArray();
            string cleaned = new string(chars);
            while (cleaned.Contains("  ", StringComparison.Ordinal))
                cleaned = cleaned.Replace("  ", " ", StringComparison.Ordinal);
            return cleaned.Trim().TrimEnd('.', '!', '?');
        }

        private static string? DescribeEvent(BattleEvent evt)
        {
            if (evt == null) return null;
            if (evt.IsCritical && evt.IsSuccess)
                return $"{evt.Actor} landed a critical {evt.Action} on {evt.Target}";
            if (evt.IsCombo && evt.IsSuccess)
                return $"{evt.Actor} chained {evt.Action} against {evt.Target}";
            if (!evt.IsSuccess && evt.NaturalRoll == 1)
                return $"{evt.Actor} critically missed";
            if (evt.IsHeal && evt.HealAmount > 0)
                return $"{evt.Actor} recovered strength";
            if (evt.IsSuccess && !string.IsNullOrWhiteSpace(evt.Action))
                return $"{evt.Actor} struck with {evt.Action}";
            return null;
        }
    }
}
