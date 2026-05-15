using System;
using System.Collections.Generic;
using System.Globalization;

namespace RPGGame
{
    /// <summary>
    /// Generates food and dungeon potions for post-combat room search from <see cref="RoomSearchConsumableCatalog"/>
    /// (<c>Consumables.json</c> / CONSUMABLES sheet).
    /// </summary>
    public static class RoomSearchConsumableGenerator
    {
        public static Item Generate(Random random, Character player, int dungeonLevel)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            IReadOnlyList<RoomSearchConsumableDefinition> defs = RoomSearchConsumableCatalog.GetDefinitions();
            if (defs.Count == 0)
                throw new InvalidOperationException("Consumables catalog is empty.");

            RoomSearchConsumableDefinition def = defs[random.Next(defs.Count)];
            return CreateItemFromDefinition(def, player, dungeonLevel);
        }

        private static Item CreateItemFromDefinition(RoomSearchConsumableDefinition def, Character player, int dungeonLevel)
        {
            int tier = Math.Clamp(dungeonLevel, 1, 99);
            string name = def.DisplayName;

            if (def.Kind == RoomSearchConsumableKind.Food)
            {
                int heal = ResolveFoodHeal(player, def.PotencyRaw);
                return new Item(ItemType.Consumable, name, tier, 0)
                {
                    Level = tier,
                    Rarity = "Common",
                    RoomSearchConsumableKind = RoomSearchConsumableKind.Food,
                    ConsumableHealAmount = heal,
                    ConsumablePotionPotency = 0,
                    Tags = new List<string> { "search_food", "consumable" }
                };
            }

            int potency = ResolvePotionPotency(player, def.Kind, def.PotencyRaw);
            string rarity = "Uncommon";
            return new Item(ItemType.Consumable, name, tier, 0)
            {
                Level = tier,
                Rarity = rarity,
                RoomSearchConsumableKind = def.Kind,
                ConsumableHealAmount = 0,
                ConsumablePotionPotency = potency,
                Tags = new List<string> { "search_potion", "consumable" }
            };
        }

        private static int ResolveFoodHeal(Character player, string potencyRaw)
        {
            if (!TryParsePotency(potencyRaw, out bool isPercent, out int magnitude))
                return 1;
            if (isPercent)
                return Math.Max(1, (int)Math.Round(player.MaxHealth * (magnitude / 100.0)));
            return Math.Max(1, magnitude);
        }

        private static int ResolvePotionPotency(Character player, RoomSearchConsumableKind kind, string potencyRaw)
        {
            if (!TryParsePotency(potencyRaw, out bool isPercent, out int magnitude))
                return 1;

            bool isStat = kind is RoomSearchConsumableKind.PotionStrength or RoomSearchConsumableKind.PotionAgility
                or RoomSearchConsumableKind.PotionTechnique or RoomSearchConsumableKind.PotionIntelligence;

            if (isStat)
            {
                if (isPercent)
                {
                    int baseStat = kind switch
                    {
                        RoomSearchConsumableKind.PotionStrength => player.Stats.Strength,
                        RoomSearchConsumableKind.PotionAgility => player.Stats.Agility,
                        RoomSearchConsumableKind.PotionTechnique => player.Stats.Technique,
                        RoomSearchConsumableKind.PotionIntelligence => player.Stats.Intelligence,
                        _ => 1
                    };
                    return Math.Max(1, (int)Math.Round(baseStat * (magnitude / 100.0)));
                }

                return Math.Max(1, magnitude);
            }

            // Roll-threshold potions: sheet uses flat integers; if a % appears, use the numeric part as magnitude.
            return Math.Max(1, magnitude);
        }

        private static bool TryParsePotency(string? raw, out bool isPercent, out int magnitude)
        {
            isPercent = false;
            magnitude = 1;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            raw = raw.Trim();
            if (raw.EndsWith("%", StringComparison.Ordinal))
            {
                isPercent = true;
                string n = raw[..^1].Trim();
                return int.TryParse(n, NumberStyles.Integer, CultureInfo.InvariantCulture, out magnitude) && magnitude > 0;
            }

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out magnitude) && magnitude > 0;
        }
    }
}
