using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame
{
    /// <summary>
    /// Manages character display functionality including character info, stats, and descriptions
    /// Extracted from Character class to follow Single Responsibility Principle
    /// </summary>
    public class CharacterDisplayManager
    {
        private readonly Character character;

        public CharacterDisplayManager(Character character)
        {
            this.character = character;
        }

        /// <summary>
        /// Displays comprehensive character information
        /// </summary>
        public void DisplayCharacterInfo()
        {
            UIManager.WriteLine($"=== CHARACTER INFORMATION ===");
            UIManager.WriteLine($"Name: {character.Name}");
            UIManager.WriteLine($"Class: {character.GetCurrentClass()}");
            UIManager.WriteLine($"Level: {character.Level}");
            UIManager.WriteLine($"Health: {character.CurrentHealth}/{character.MaxHealth}");
            UIManager.WriteLine($"XP: {character.XP}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== STATS ===");
            UIManager.WriteLine($"Strength: {character.Strength}");
            UIManager.WriteLine($"Agility: {character.Agility}");
            UIManager.WriteLine($"Technique: {character.Technique}");
            UIManager.WriteLine($"Intelligence: {character.Intelligence}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== CLASS POINTS ===");
            UIManager.WriteLine($"Barbarian (Mace): {character.BarbarianPoints}");
            UIManager.WriteLine($"Warrior (Sword): {character.WarriorPoints}");
            UIManager.WriteLine($"Rogue (Dagger): {character.RoguePoints}");
            UIManager.WriteLine($"Wizard (Wand): {character.WizardPoints}");
            UIManager.WriteBlankLine();
            UIManager.WriteLine("=== EQUIPMENT ===");
            UIManager.WriteLine($"Weapon: {(character.Weapon?.Name ?? "None")}");
            UIManager.WriteLine($"Head: {(character.Head?.Name ?? "None")}");
            UIManager.WriteLine($"Body: {(character.Body?.Name ?? "None")}");
            UIManager.WriteLine($"Feet: {(character.Feet?.Name ?? "None")}");
            UIManager.WriteBlankLine();
        }

        /// <summary>
        /// Gets the character's basic description
        /// </summary>
        /// <returns>Character description string</returns>
        public string GetDescription()
        {
            return character.Combat.GetCombatDescription();
        }

        /// <summary>
        /// Gets combo information as a formatted string
        /// </summary>
        /// <returns>Formatted combo information</returns>
        public string GetComboInfo()
        {
            return character.Combat.GetComboInfo();
        }

        /// <summary>
        /// Displays character statistics for inventory display
        /// </summary>
        public void DisplayCharacterStats()
        {
            UIManager.WriteMenuLine($"{character.Name} (Level {character.Level}) - {character.GetCurrentClass()}");
            UIManager.WriteMenuLine($"Health: {character.CurrentHealth}/{character.MaxHealth}  STR: {character.GetEffectiveStrength()}  AGI: {character.GetEffectiveAgility()}  TEC: {character.GetEffectiveTechnique()}  INT: {character.GetEffectiveIntelligence()}");
            UIManager.WriteMenuLine(character.Combat.GetDetailedCombatStats());

            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            if (character.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({character.BarbarianPoints})");
            if (character.WarriorPoints > 0) classPointsInfo.Add($"Warrior({character.WarriorPoints})");
            if (character.RoguePoints > 0) classPointsInfo.Add($"Rogue({character.RoguePoints})");
            if (character.WizardPoints > 0) classPointsInfo.Add($"Wizard({character.WizardPoints})");

            if (classPointsInfo.Count > 0)
            {
                UIManager.WriteMenuLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                UIManager.WriteMenuLine($"Next Upgrades: {character.GetClassUpgradeInfo()}");
            }
        }

        /// <summary>
        /// Displays currently equipped items with their stats
        /// </summary>
        public void DisplayCurrentEquipment()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Currently Equipped:");

            // Show weapon with indented stats
            if (character.Weapon is WeaponItem weapon)
            {
                UIManager.WriteMenuLine($"Weapon: {weapon.Name}");
                UIManager.WriteMenuLine($"    Damage: {weapon.GetTotalDamage()}, Attack Speed: {weapon.GetAttackSpeedMultiplier():F1}x");

                // Show weapon bonuses and modifications
                DisplayItemBonuses(weapon);
            }
            else
            {
                UIManager.WriteMenuLine("Weapon: None");
            }

            // Show armor pieces with indented stats
            if (character.Head is HeadItem head)
            {
                UIManager.WriteMenuLine($"Head: {head.Name}");
                UIManager.WriteMenuLine($"    Armor: {head.GetTotalArmor()}");

                // Show head armor bonuses and modifications
                DisplayItemBonuses(head);
            }
            else
            {
                UIManager.WriteMenuLine("Head: None");
            }

            if (character.Body is ChestItem chest)
            {
                UIManager.WriteMenuLine($"Body: {chest.Name}");
                UIManager.WriteMenuLine($"    Armor: {chest.GetTotalArmor()}");

                // Show chest armor bonuses and modifications
                DisplayItemBonuses(chest);
            }
            else
            {
                UIManager.WriteMenuLine("Body: None");
            }

            if (character.Feet is FeetItem feet)
            {
                UIManager.WriteMenuLine($"Feet: {feet.Name}");
                UIManager.WriteMenuLine($"    Armor: {feet.GetTotalArmor()}");

                // Show feet armor bonuses and modifications
                DisplayItemBonuses(feet);
            }
            else
            {
                UIManager.WriteMenuLine("Feet: None");
            }
        }

        /// <summary>
        /// Displays combo information and management options
        /// </summary>
        public void DisplayComboInfo()
        {
            UIManager.WriteMenuLine("");
            UIManager.WriteMenuLine("Combo Actions:");
            if (character.ComboSequence.Count == 0)
            {
                UIManager.WriteMenuLine("(No combo actions set)");
            }
            else
            {
                for (int i = 0; i < character.ComboSequence.Count; i++)
                {
                    var action = character.ComboSequence[i];
                    UIManager.WriteMenuLine($"{i + 1}. {action.Name} (Length: {action.Length:F1})");
                }
            }
        }

        /// <summary>
        /// Shows detailed item bonuses and modifications
        /// </summary>
        /// <param name="item">Item to display bonuses for</param>
        private void DisplayItemBonuses(Item item)
        {
            // Show stat bonuses
            if (item.StatBonuses.Count > 0)
            {
                UIManager.WriteMenuLine($"    Stat Bonuses: {string.Join(", ", item.StatBonuses.Select(b => $"{b.StatType} +{b.Value}"))}");
            }

            // Show action bonuses (legacy system)
            if (item.ActionBonuses.Count > 0)
            {
                UIManager.WriteMenuLine($"    Action Bonuses: {string.Join(", ", item.ActionBonuses.Select(b => $"{b.Name} +{b.Weight}"))}");
            }

            // Show modifications
            if (item.Modifications.Count > 0)
            {
                var modificationTexts = item.Modifications.Select(m => $"{m.Name} ({m.RolledValue:F1})");
                UIManager.WriteMenuLine($"    Modifications: {string.Join(", ", modificationTexts)}");
            }
        }

        /// <summary>
        /// Gets the display type for an item
        /// </summary>
        /// <param name="item">Item to get display type for</param>
        /// <returns>Display type string</returns>
        public string GetDisplayType(Item item)
        {
            return item.Type switch
            {
                ItemType.Weapon => GetWeaponClassDisplay(item as WeaponItem),
                ItemType.Head => "Head",
                ItemType.Chest => "Chest",
                ItemType.Feet => "Feet",
                _ => "Item"
            };
        }

        /// <summary>
        /// Gets the weapon class display string
        /// </summary>
        /// <param name="weapon">Weapon to get display type for</param>
        /// <returns>Weapon class display string</returns>
        private string GetWeaponClassDisplay(WeaponItem? weapon)
        {
            if (weapon == null) return "Weapon";

            return weapon.WeaponType switch
            {
                WeaponType.Mace => "Barbarian Weapon",
                WeaponType.Sword => "Warrior Weapon",
                WeaponType.Dagger => "Rogue Weapon",
                WeaponType.Wand => "Wizard Weapon",
                _ => "Weapon"
            };
        }

        /// <summary>
        /// Gets item actions as a formatted string
        /// </summary>
        /// <param name="item">Item to get actions for</param>
        /// <returns>Formatted actions string</returns>
        public string GetItemActions(Item item)
        {
            var actions = new List<string>();

            if (item is WeaponItem weapon)
            {
                var weaponActions = GetWeaponActionsFromJson(weapon.WeaponType);
                actions.AddRange(weaponActions);
            }
            else if (ShouldArmorHaveActions(item))
            {
                var armorActions = GetRandomArmorActionFromJson(item);
                actions.AddRange(armorActions);
            }

            return actions.Count > 0 ? " | " + string.Join(" | ", actions) : "";
        }

        /// <summary>
        /// Gets armor difference display for comparison
        /// </summary>
        /// <param name="invItem">Inventory item</param>
        /// <param name="equipped">Currently equipped item</param>
        /// <returns>Armor difference string</returns>
        public string GetArmorDiff(Item invItem, Item? equipped)
        {
            if (equipped == null) return " (NEW)";

            int invArmor = invItem switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };

            int equippedArmor = equipped switch
            {
                HeadItem head => head.GetTotalArmor(),
                ChestItem chest => chest.GetTotalArmor(),
                FeetItem feet => feet.GetTotalArmor(),
                _ => 0
            };

            int diff = invArmor - equippedArmor;
            if (diff > 0) return $" (+{diff})";
            if (diff < 0) return $" ({diff})";
            return " (=)";
        }

        /// <summary>
        /// Gets weapon difference display for comparison
        /// </summary>
        /// <param name="invWeapon">Inventory weapon</param>
        /// <param name="equipped">Currently equipped weapon</param>
        /// <returns>Weapon difference string</returns>
        public string GetWeaponDiff(WeaponItem invWeapon, WeaponItem? equipped)
        {
            if (equipped == null) return " (NEW)";

            int damageDiff = invWeapon.GetTotalDamage() - equipped.GetTotalDamage();
            if (damageDiff > 0) return $" (+{damageDiff} damage)";
            if (damageDiff < 0) return $" ({damageDiff} damage)";
            return " (= damage)";
        }

        /// <summary>
        /// Determines if an armor piece should have special actions
        /// </summary>
        /// <param name="armor">Armor item to check</param>
        /// <returns>True if armor should have actions</returns>
        private bool ShouldArmorHaveActions(Item armor)
        {
            // Check if this armor piece has special properties that indicate it should have actions

            // 1. Check if it has modifications (indicates special gear)
            if (armor.Modifications.Count > 0)
            {
                return true;
            }

            // 2. Check if it has stat bonuses (indicates special gear)
            if (armor.StatBonuses.Count > 0)
            {
                return true;
            }

            // 3. Check if it has action bonuses (legacy system)
            if (armor.ActionBonuses.Count > 0)
            {
                return true;
            }

            // 4. Check if it's not basic starter gear by name
            // Basic gear names moved to GameData configuration
            string[] basicGearNames = BasicGearConfig.GetBasicGearNames();
            if (basicGearNames.Contains(armor.Name))
            {
                return false; // Basic starter gear should have no actions
            }

            // 5. If it has a special name or properties, it might be special gear
            // For now, assume any non-basic gear might have actions
            return true;
        }

        /// <summary>
        /// Gets weapon actions from JSON data
        /// </summary>
        /// <param name="weaponType">Type of weapon</param>
        /// <returns>List of weapon action names</returns>
        private List<string> GetWeaponActionsFromJson(WeaponType weaponType)
        {
            var allActions = ActionLoader.GetAllActions();
            var weaponActions = new List<string>();

            // Get actions that match the weapon type and have "weapon" tag, but exclude "unique" actions
            foreach (var actionData in allActions)
            {
                if (actionData.Tags.Contains("weapon") &&
                    actionData.Tags.Contains(weaponType.ToString().ToLower()) &&
                    !actionData.Tags.Contains("unique"))
                {
                    weaponActions.Add(actionData.Name);
                }
            }

            return weaponActions;
        }

        /// <summary>
        /// Gets random armor actions from JSON data
        /// </summary>
        /// <param name="armor">Armor item</param>
        /// <returns>List of armor action names</returns>
        private List<string> GetRandomArmorActionFromJson(Item armor)
        {
            // Return empty list - no action bonuses to display
            return new List<string>();
        }
    }
}
