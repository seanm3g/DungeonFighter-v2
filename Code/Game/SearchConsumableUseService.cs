namespace RPGGame
{
    /// <summary>
    /// Room-search food and potions are applied immediately on find (not bagged).
    /// </summary>
    public static class SearchConsumableUseService
    {
        /// <summary>
        /// Applies food heal or dungeon potion buff from <paramref name="item"/>; does not touch inventory.
        /// </summary>
        /// <returns>False when <paramref name="item"/> is not a valid room-search consumable.</returns>
        public static bool ApplyImmediately(Character? player, Item? item, out string message)
        {
            message = "";
            if (player == null)
            {
                message = "No active character.";
                return false;
            }

            if (item == null || item.Type != ItemType.Consumable || item.RoomSearchConsumableKind == RoomSearchConsumableKind.None)
            {
                message = "That is not usable here.";
                return false;
            }

            switch (item.RoomSearchConsumableKind)
            {
                case RoomSearchConsumableKind.Food:
                    int heal = Math.Max(1, item.ConsumableHealAmount);
                    int before = player.CurrentHealth;
                    player.Heal(heal);
                    int gained = player.CurrentHealth - before;
                    message = gained > 0
                        ? $"You eat the {item.Name} on the spot and recover {gained} health."
                        : $"You eat the {item.Name} (already at full health).";
                    return true;

                case RoomSearchConsumableKind.PotionStrength:
                    player.DungeonSearchBuffs.AddStrength(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Strength is bolstered for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionAgility:
                    player.DungeonSearchBuffs.AddAgility(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Agility is bolstered for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionTechnique:
                    player.DungeonSearchBuffs.AddTechnique(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Technique is bolstered for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionIntelligence:
                    player.DungeonSearchBuffs.AddIntelligence(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Intelligence is bolstered for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionHit:
                    player.DungeonSearchBuffs.AddHitThresholdAdjustment(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Your hit threshold is easier for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionCombo:
                    player.DungeonSearchBuffs.AddComboThresholdAdjustment(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Your combo threshold is easier for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionCrit:
                    player.DungeonSearchBuffs.AddCritThresholdAdjustment(item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. Your critical threshold is easier for this dungeon (+{item.ConsumablePotionPotency}).";
                    return true;

                case RoomSearchConsumableKind.PotionCritMiss:
                    player.DungeonSearchBuffs.AddCritMissThresholdAdjustment(-item.ConsumablePotionPotency);
                    message = $"You drink the {item.Name} immediately. You feel steadier against critical misses for this dungeon.";
                    return true;

                default:
                    message = "That is not usable here.";
                    return false;
            }
        }
    }
}
