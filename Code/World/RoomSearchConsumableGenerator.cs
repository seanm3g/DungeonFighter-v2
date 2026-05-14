using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Generates food and dungeon potions for post-combat room search (replaces equipment loot rolls).
    /// </summary>
    public static class RoomSearchConsumableGenerator
    {
        private static readonly string[] FoodNamePrefixes =
        {
            "Traveler's", "Hearty", "Smoked", "Salted", "Honeyed", "Field", "Camp", "Waxed"
        };

        private static readonly string[] FoodNameSuffixes =
        {
            "Rations", "Jerky", "Bread", "Stew", "Cheese", "Biscuits", "Sausage", "Pie"
        };

        public static Item Generate(Random random, Character player, int dungeonLevel)
        {
            if (random == null)
                throw new ArgumentNullException(nameof(random));
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            bool food = random.NextDouble() < 0.5;
            if (food)
                return CreateFood(random, dungeonLevel);

            return CreatePotion(random, dungeonLevel);
        }

        private static Item CreateFood(Random random, int dungeonLevel)
        {
            int tier = Math.Clamp(dungeonLevel, 1, 99);
            // Varied heal: low floors modest, scales with dungeon depth, plus a small random band.
            int heal = 6 + tier * 2 + random.Next(0, 7);
            heal = Math.Clamp(heal, 5, 80);

            string name = $"{FoodNamePrefixes[random.Next(FoodNamePrefixes.Length)]} {FoodNameSuffixes[random.Next(FoodNameSuffixes.Length)]}";
            var item = new Item(ItemType.Consumable, name, tier, 0)
            {
                Level = tier,
                Rarity = "Common",
                RoomSearchConsumableKind = RoomSearchConsumableKind.Food,
                ConsumableHealAmount = heal,
                ConsumablePotionPotency = 0,
                Tags = new List<string> { "search_food", "consumable" }
            };
            return item;
        }

        private static Item CreatePotion(Random random, int dungeonLevel)
        {
            int tier = Math.Clamp(dungeonLevel, 1, 99);
            var kinds = new[]
            {
                RoomSearchConsumableKind.PotionStrength,
                RoomSearchConsumableKind.PotionAgility,
                RoomSearchConsumableKind.PotionTechnique,
                RoomSearchConsumableKind.PotionIntelligence,
                RoomSearchConsumableKind.PotionHit,
                RoomSearchConsumableKind.PotionCombo,
                RoomSearchConsumableKind.PotionCrit,
                RoomSearchConsumableKind.PotionCritMiss
            };
            var kind = kinds[random.Next(kinds.Length)];

            int statPotency = 1 + tier / 8 + (random.NextDouble() < 0.25 ? 1 : 0);
            statPotency = Math.Clamp(statPotency, 2, 6);

            int thresholdPotency = 1 + (tier >= 10 ? 1 : 0) + (random.NextDouble() < 0.2 ? 1 : 0);
            thresholdPotency = Math.Clamp(thresholdPotency, 1, 3);

            bool isStat = kind is RoomSearchConsumableKind.PotionStrength or RoomSearchConsumableKind.PotionAgility
                or RoomSearchConsumableKind.PotionTechnique or RoomSearchConsumableKind.PotionIntelligence;

            string name = BuildPotionName(kind);
            var item = new Item(ItemType.Consumable, name, tier, 0)
            {
                Level = tier,
                Rarity = "Uncommon",
                RoomSearchConsumableKind = kind,
                ConsumableHealAmount = 0,
                ConsumablePotionPotency = isStat ? statPotency : thresholdPotency,
                Tags = new List<string> { "search_potion", "consumable" }
            };
            return item;
        }

        private static string BuildPotionName(RoomSearchConsumableKind kind) => kind switch
        {
            RoomSearchConsumableKind.PotionStrength => "Vial of Iron Blood",
            RoomSearchConsumableKind.PotionAgility => "Elixir of Quicksilver",
            RoomSearchConsumableKind.PotionTechnique => "Tincture of Fine Motion",
            RoomSearchConsumableKind.PotionIntelligence => "Draught of Clear Thought",
            RoomSearchConsumableKind.PotionHit => "Oil of True Aim",
            RoomSearchConsumableKind.PotionCombo => "Serum of Flow",
            RoomSearchConsumableKind.PotionCrit => "Essence of Razor's Edge",
            RoomSearchConsumableKind.PotionCritMiss => "Balm of Steady Hands",
            _ => "Mysterious Flask"
        };
    }
}
