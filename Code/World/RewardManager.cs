using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Handles reward distribution for dungeon completion
    /// Extracted from DungeonManager to follow Single Responsibility Principle
    /// </summary>
    public class RewardManager
    {
        private readonly Random random = new Random();

        /// <summary>
        /// Awards loot and XP for dungeon completion
        /// </summary>
        /// <param name="player">The player character</param>
        /// <param name="inventory">Player's inventory</param>
        /// <param name="dungeonLevel">Level of the completed dungeon</param>
        public void AwardLootAndXP(Character player, List<Item> inventory, int dungeonLevel)
        {
            BlockDisplayManager.DisplaySystemBlock("\nDungeon completed!");
            
            // Track dungeon completion statistics
            player.RecordDungeonCompleted();
            
            // Heal character back to max health between dungeons
            HealPlayer(player);
            
            // Award XP (scaled by dungeon level using tuning config)
            AwardXP(player);
            
            // Award guaranteed loot for dungeon completion
            AwardLoot(player, inventory, dungeonLevel);
        }

        /// <summary>
        /// Heals the player to full health
        /// </summary>
        private void HealPlayer(Character player)
        {
            int effectiveMaxHealth = player.GetEffectiveMaxHealth();
            int healthRestored = effectiveMaxHealth - player.CurrentHealth;
            if (healthRestored > 0)
            {
                player.Heal(healthRestored);
                BlockDisplayManager.DisplaySystemBlock($"You have been fully healed! (+{healthRestored} health)");
            }
        }

        /// <summary>
        /// Awards XP to the player
        /// </summary>
        private void AwardXP(Character player)
        {
            var tuning = GameConfiguration.Instance;
            int xpReward = random.Next(tuning.Progression.EnemyXPBase, tuning.Progression.EnemyXPBase + 50) * player.Level;
            player.AddXP(xpReward);
            BlockDisplayManager.DisplaySystemBlock($"Gained {xpReward} XP!");
            UIManager.WriteBlankLine(); // Blank line between XP and loot

            if (player.Level > 1)
            {
                UIManager.WriteLine($"Level up! You are now level {player.Level}");
                UIManager.WriteBlankLine(); // Add line break after level up message
            }
        }

        /// <summary>
        /// Awards loot to the player
        /// </summary>
        private void AwardLoot(Character player, List<Item> inventory, int dungeonLevel)
        {
            Item? reward = LootGenerator.GenerateLoot(player.Level, dungeonLevel, player, guaranteedLoot: true);
            
            // If still no loot, notify about the issue
            if (reward == null)
            {
                HandleLootGenerationFailure(player, dungeonLevel);
                reward = CreateFallbackReward(player);
            }

            if (reward != null)
            {
                // Add to both inventories
                player.AddToInventory(reward);
                inventory.Add(reward);
                
                // Track item collection statistics
                player.RecordItemCollected(reward);
                
                // Display "You found:" without extra spacing
                Console.WriteLine("You found:");
                // Display item name with 2x beat delay (using title delay which is longer)
                UIManager.WriteTitleLine(FormatItemNameWithRarityInParentheses(reward));
                // Add blank line after item display
                UIManager.WriteSystemLine("");
            }
            else
            {
                // This should never happen with the fallback, but just in case
                BlockDisplayManager.DisplaySystemBlock("You found no loot this time.");
            }
        }

        /// <summary>
        /// Handles loot generation failure with detailed error reporting
        /// </summary>
        private void HandleLootGenerationFailure(Character player, int dungeonLevel)
        {
            UIManager.WriteLine("⚠️  WARNING: Guaranteed loot generation failed!");
            UIManager.WriteLine("   This indicates a serious issue with the loot generation system.");
            UIManager.WriteLine("   Please report this issue with the following details:");
            UIManager.WriteLine($"   - Player Level: {player.Level}");
            UIManager.WriteLine($"   - Dungeon Level: {dungeonLevel}");
            UIManager.WriteLine($"   - Guaranteed Loot Requested: Yes");
            UIManager.WriteSystemLine("");
        }

        /// <summary>
        /// Creates a fallback reward when loot generation fails
        /// </summary>
        private Item? CreateFallbackReward(Character player)
        {
            // Create a diagnostic fallback weapon to prevent game breaking
            var reward = Program.CreateFallbackWeapon(player.Level);
            if (reward == null)
            {
                // Ultimate fallback if weapon data loading fails
                reward = new WeaponItem("Basic Sword", player.Level, 5 + player.Level, 1.0, WeaponType.Sword);
                reward.Rarity = "Common";
                BlockDisplayManager.DisplaySystemBlock("   Created emergency fallback weapon to prevent game breaking.");
            }
            else
            {
                BlockDisplayManager.DisplaySystemBlock($"   Created fallback weapon: {reward.Name} (from weapon database)");
            }
            BlockDisplayManager.DisplaySystemBlock("");
            return reward;
        }

        /// <summary>
        /// Formats an item name to display rarity in parentheses instead of as a prefix
        /// </summary>
        /// <param name="item">The item to format</param>
        /// <returns>Formatted item name with rarity in parentheses</returns>
        private static string FormatItemNameWithRarityInParentheses(Item item)
        {
            string name = item.Name;
            
            // Check for rarity prefixes and move them to parentheses
            string[] rarities = { "Legendary", "Epic", "Rare", "Uncommon", "Common" };
            
            foreach (string rarity in rarities)
            {
                if (name.StartsWith(rarity + " "))
                {
                    // Remove the rarity prefix and add it in parentheses
                    string nameWithoutRarity = name.Substring(rarity.Length + 1);
                    return $"({rarity}) {nameWithoutRarity}";
                }
            }
            
            // If no rarity prefix found, return the name as-is
            return name;
        }
    }
}
