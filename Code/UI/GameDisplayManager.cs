using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Unified display manager that consolidates inventory and character display functionality
    /// Replaces both InventoryDisplayManager and CharacterDisplayManager to eliminate duplication
    /// </summary>
    public class GameDisplayManager
    {
        private readonly Character player;
        private readonly List<Item> inventory;
        private readonly EquipmentDisplayService equipmentService;

        public GameDisplayManager(Character player, List<Item> inventory)
        {
            this.player = player;
            this.inventory = inventory;
            this.equipmentService = new EquipmentDisplayService();
        }

        /// <summary>
        /// Shows the main inventory display with all sections
        /// </summary>
        public void ShowMainDisplay()
        {
            // Reset menu delay counter at the start of inventory display
            UIManager.ResetMenuDelayCounter();
            
            UIManager.WriteMenuLine("\n--- Inventory ---");
            ShowCharacterStats();
            UIManager.WriteMenuLine("---------------------");
            equipmentService.DisplayCurrentEquipment(player, UIManager.WriteMenuLine);
            UIManager.WriteMenuLine("---------------------");
            ShowComboInfo();
            ShowInventory();
            UIManager.WriteMenuLine("---------------------");
            ShowOptions();
            
            // Reset menu delay counter after inventory display is complete
            UIManager.ResetMenuDelayCounter();
        }

        /// <summary>
        /// Displays comprehensive character information
        /// </summary>
        public void DisplayCharacterInfo()
        {
            UIManager.WriteLine($"=== CHARACTER INFORMATION ===");
            UIManager.WriteLine($"Name: {player.Name}");
            UIManager.WriteLine($"Class: {player.GetCurrentClass()}");
            UIManager.WriteLine($"Level: {player.Level}");
            UIManager.WriteLine($"Health: {player.CurrentHealth}/{player.MaxHealth}");
            UIManager.WriteLine($"XP: {player.XP}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== STATS ===");
            UIManager.WriteLine($"Strength: {player.Strength}");
            UIManager.WriteLine($"Agility: {player.Agility}");
            UIManager.WriteLine($"Technique: {player.Technique}");
            UIManager.WriteLine($"Intelligence: {player.Intelligence}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== CLASS POINTS ===");
            UIManager.WriteLine($"Barbarian (Mace): {player.BarbarianPoints}");
            UIManager.WriteLine($"Warrior (Sword): {player.WarriorPoints}");
            UIManager.WriteLine($"Rogue (Dagger): {player.RoguePoints}");
            UIManager.WriteLine($"Wizard (Wand): {player.WizardPoints}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== EQUIPMENT ===");
            UIManager.WriteLine($"Weapon: {(player.Weapon != null ? ItemDisplayFormatter.GetColoredItemName(player.Weapon) : "None")}");
            UIManager.WriteLine($"Head: {(player.Head != null ? ItemDisplayFormatter.GetColoredItemName(player.Head) : "None")}");
            UIManager.WriteLine($"Body: {(player.Body != null ? ItemDisplayFormatter.GetColoredItemName(player.Body) : "None")}");
            UIManager.WriteLine($"Feet: {(player.Feet != null ? ItemDisplayFormatter.GetColoredItemName(player.Feet) : "None")}");
            UIManager.WriteBlankLine();
        }

        /// <summary>
        /// Shows currently equipped items
        /// </summary>
        public void ShowCurrentEquipment()
        {
            equipmentService.DisplayCurrentEquipment(player, UIManager.WriteMenuLine);
        }

        /// <summary>
        /// Displays character statistics and class information
        /// </summary>
        public void ShowCharacterStats()
        {
            int weaponDamage = (player.Weapon is WeaponItem w) ? w.GetTotalDamage() : 0;
            int equipmentDamageBonus = player.GetEquipmentDamageBonus();
            int modificationDamageBonus = player.GetModificationDamageBonus();
            int damage = player.GetEffectiveStrength() + weaponDamage + equipmentDamageBonus + modificationDamageBonus;
            double attackSpeed = player.GetTotalAttackSpeed();
            int armor = player.GetTotalArmor();
            UIManager.WriteMenuLine($"{player.Name} (Level {player.Level}) - {player.GetCurrentClass()}");
            UIManager.WriteMenuLine($"Health: {player.CurrentHealth}/{player.GetEffectiveMaxHealth()}  Strength: {player.GetEffectiveStrength()}  Agility: {player.GetEffectiveAgility()}  Technique: {player.GetEffectiveTechnique()}  Intelligence: {player.GetEffectiveIntelligence()}");
            int totalRollBonus = player.GetIntelligenceRollBonus() + player.GetModificationRollBonus() + player.GetEquipmentRollBonus();
            double secondsPerAttack = attackSpeed;
            // Get next amplification (what will be applied when combo executes)
            double nextAmplification = player.GetNextComboAmplification();
            int magicFind = player.GetMagicFind();
            UIManager.WriteMenuLine($"Damage: {damage} (Strength: {player.GetEffectiveStrength()} + Weapon: {weaponDamage} + Equipment: {equipmentDamageBonus} + Mods: {modificationDamageBonus})  Attack Time: {attackSpeed:0.00}s  Amplification: {nextAmplification:F2}x  Roll Bonus: +{totalRollBonus}  Armor: {armor}");
            if (magicFind > 0)
            {
                UIManager.WriteMenuLine($"Magic Find: +{magicFind} (improves rare item drop chances)");
            }
            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            if (player.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({player.BarbarianPoints})");
            if (player.WarriorPoints > 0) classPointsInfo.Add($"Warrior({player.WarriorPoints})");
            if (player.RoguePoints > 0) classPointsInfo.Add($"Rogue({player.RoguePoints})");
            if (player.WizardPoints > 0) classPointsInfo.Add($"Wizard({player.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                UIManager.WriteMenuLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                UIManager.WriteMenuLine($"Next Upgrades: {player.GetClassUpgradeInfo()}");
            }
        }

        /// <summary>
        /// Displays the inventory list with item stats and bonuses
        /// </summary>
        public void ShowInventory()
        {
            // Don't display anything if inventory is empty
            if (inventory.Count == 0)
            {
                return;
            }

            UIManager.WriteMenuLine("---------------------");
            UIManager.WriteMenuLine("Inventory:");

            for (int i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                string itemStats = ItemDisplayFormatter.GetItemStatsDisplay(item, player);
                string displayType = ItemDisplayFormatter.GetDisplayType(item);
                string itemActions = equipmentService.GetItemActions(item);
                
                // Show item rarity, type and name on first line with color
                string rarity = item.Rarity?.Trim() ?? "Common";
                string coloredName = ItemDisplayFormatter.GetColoredItemName(item);
                UIManager.WriteMenuLine($"{i + 1}. [{rarity}] ({displayType}) {coloredName}");
                
                // Show stats on indented line
                if (!string.IsNullOrEmpty(itemStats))
                {
                    UIManager.WriteMenuLine($"    {itemStats}");
                }
                
                // Show actions on indented line if any
                if (!string.IsNullOrEmpty(itemActions))
                {
                    UIManager.WriteMenuLine($"    Actions: {itemActions.Substring(3)}"); // Remove " | " prefix
                }
                
                // Show affix bonuses if the item has any (with colors)
                if (item.StatBonuses.Count > 0 || item.ActionBonuses.Count > 0 || item.Modifications.Count > 0)
                {
                    ItemDisplayFormatter.FormatItemBonusesWithColor(item, UIManager.WriteMenuLine);
                }
            }
        }

        /// <summary>
        /// Displays combo information and management options
        /// </summary>
        public void ShowComboInfo()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Combo Actions:");
            if (player.ComboSequence.Count == 0)
            {
                UIManager.WriteMenuLine("(No combo actions set)");
            }
            else
            {
                for (int i = 0; i < player.ComboSequence.Count; i++)
                {
                    var action = player.ComboSequence[i];
                    UIManager.WriteMenuLine($"{i + 1}. {action.Name} (Length: {action.Length:F1})");
                }
            }
        }

        /// <summary>
        /// Displays the main menu options
        /// </summary>
        public void ShowOptions()
        {
            var options = MenuConfiguration.GetInventoryMenuOptions();
            foreach (var option in options)
            {
                if (string.IsNullOrEmpty(option))
                {
                    UIManager.WriteMenuLine("");
                }
                else
                {
                    UIManager.WriteMenuLine(option);
                }
            }
            UIManager.WriteMenuLine(""); // Add blank line after menu options
            UIManager.Write("Enter your choice: ");
        }

        /// <summary>
        /// Gets the character's basic description
        /// </summary>
        /// <returns>Character description string</returns>
        public string GetDescription()
        {
            return player.Combat.GetCombatDescription();
        }

        /// <summary>
        /// Gets combo information as a formatted string
        /// </summary>
        /// <returns>Formatted combo information</returns>
        public string GetComboInfo()
        {
            return player.Combat.GetComboInfo();
        }

        /// <summary>
        /// Gets the display type for an item (delegates to ItemDisplayFormatter)
        /// </summary>
        /// <param name="item">Item to get display type for</param>
        /// <returns>Display type string</returns>
        public string GetDisplayType(Item item)
        {
            return ItemDisplayFormatter.GetDisplayType(item);
        }

        /// <summary>
        /// Gets item actions as a formatted string (delegates to EquipmentDisplayService)
        /// </summary>
        /// <param name="item">Item to get actions for</param>
        /// <returns>Formatted actions string</returns>
        public string GetItemActions(Item item)
        {
            return equipmentService.GetItemActions(item);
        }

        /// <summary>
        /// Gets armor difference display for comparison (delegates to ItemDisplayFormatter)
        /// </summary>
        /// <param name="invItem">Inventory item</param>
        /// <param name="equipped">Currently equipped item</param>
        /// <returns>Armor difference string</returns>
        public string GetArmorDiff(Item invItem, Item? equipped)
        {
            return ItemDisplayFormatter.GetArmorDiff(invItem, equipped);
        }

        /// <summary>
        /// Gets weapon difference display for comparison (delegates to ItemDisplayFormatter)
        /// </summary>
        /// <param name="invWeapon">Inventory weapon</param>
        /// <param name="equipped">Currently equipped weapon</param>
        /// <returns>Weapon difference string</returns>
        public string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            return ItemDisplayFormatter.GetWeaponDiff(invWeapon, equipped);
        }
    }
}
