using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Non-hostile room encounters (Documentation/CORE_DESIGN_PASS_2026-11-17.md, D8.6, D8.10): 1 in 4 rooms without
    /// enemies holds a wanderer (a true hint about the boss), a shrine (a short rest), a trapped creature (a funny
    /// outcome) or a merchant (a consumable). Built from what exists: room-search consumables and the enemy families.
    /// No choice yet: each plays out on entry (REVIEW: the design pictures choices).
    /// </summary>
    public static class RoomEncounters
    {
        public const double Chance = 0.25;
        public const double ShrineHealShare = 0.15;
        public const double BiteShare = 0.05;

        public static readonly string[] Kinds = { "wanderer", "shrine", "trapped", "merchant" };

        /// <summary>Rolls and plays the encounter for an empty room; returns its lines (empty when nothing happens).</summary>
        public static List<string> TryEncounter(Character? hero, Dungeon? dungeon, Random rng)
        {
            var lines = new List<string>();
            if (hero == null || rng.NextDouble() >= Chance)
                return lines;
            return Play(Kinds[rng.Next(Kinds.Length)], hero, dungeon, rng);
        }

        public static List<string> Play(string kind, Character hero, Dungeon? dungeon, Random rng)
        {
            var lines = new List<string>();
            switch (kind)
            {
                case "wanderer":
                    var boss = dungeon?.Rooms?.LastOrDefault()?.GetEnemies()?.FirstOrDefault();
                    var family = boss == null ? null : EnemyFamilies.For(boss.Archetype.ToString(), boss.Tags);
                    lines.Add(family != null
                        ? $"A wanderer shares your fire. \"The thing at the end is a {family.Name}: {family.PlaysLike}. It hates {family.PunishedBy}.\""
                        : "A wanderer shares your fire, says nothing useful, and leaves with your best spoon.");
                    lines.Add("Fernis: Don't listen to him. Walk in backwards, it confuses them.");
                    break;
                case "shrine":
                    int heal = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * ShrineHealShare));
                    int before = hero.CurrentHealth;
                    hero.Heal(heal);
                    lines.Add($"A quiet shrine. You rest a moment: +{Math.Max(0, hero.CurrentHealth - before)} HP.");
                    lines.Add("Fernis: Lick the altar. For luck.");
                    break;
                case "trapped":
                    if (rng.NextDouble() < 0.5)
                    {
                        lines.Add("A creature is stuck under a fallen beam. You free it. It leaves you something shiny.");
                        lines.AddRange(GiveConsumable(hero, dungeon, rng));
                    }
                    else
                    {
                        int bite = Math.Max(1, (int)Math.Round(hero.GetEffectiveMaxHealth() * BiteShare));
                        hero.CurrentHealth = Math.Max(1, hero.CurrentHealth - bite);
                        lines.Add($"A creature is stuck under a fallen beam. You free it. It bites you (-{bite} HP) and runs.");
                    }
                    lines.Add("Fernis: Gratitude! Disgusting.");
                    break;
                default:
                    lines.Add("A merchant has set up shop in the dark, somehow. You trade a story for a supply.");
                    lines.AddRange(GiveConsumable(hero, dungeon, rng));
                    lines.Add("Fernis: You overpaid. I'd have stolen it.");
                    break;
            }
            return lines;
        }

        private static IEnumerable<string> GiveConsumable(Character hero, Dungeon? dungeon, Random rng)
        {
            Item item;
            try
            {
                item = RoomSearchConsumableGenerator.Generate(rng, hero, Math.Max(1, dungeon?.MinLevel ?? hero.Level));
            }
            catch (InvalidOperationException)
            {
                yield break;
            }
            if (SearchConsumableUseService.ApplyImmediately(hero, item, out string message) && !string.IsNullOrWhiteSpace(message))
                yield return message;
        }
    }
}
