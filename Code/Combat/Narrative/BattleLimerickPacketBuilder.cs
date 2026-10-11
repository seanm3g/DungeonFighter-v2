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
                HeroName = hero.Name ?? "Hero",
                EnemyName = enemy.Name ?? "Foe",
                Location = string.IsNullOrWhiteSpace(location) ? "the dungeon" : location.Trim(),
                HeroWon = heroWon,
                HeroHpLeft = Math.Max(0, hero.CurrentHealth),
                HeroMaxHp = Math.Max(1, hero.GetEffectiveMaxHealth()),
                Highlights = highlights
            };
        }

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
