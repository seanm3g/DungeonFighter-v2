using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages level up operations for Character, handling complex level up logic
    /// Extracts level up logic from the main Character class
    /// </summary>
    public class LevelUpManager
    {
        private readonly Character _character;

        public LevelUpManager(Character character)
        {
            _character = character;
        }

        /// <summary>
        /// Handles the complete level up process
        /// </summary>
        public void LevelUp()
        {
            // Note: Level has already been incremented by Progression.AddXP()
            int newLevel = _character.Level;
            _character.Stats.LevelUp((_character.Equipment.Weapon as WeaponItem)?.WeaponType ?? WeaponType.Mace);
            
            // Track level up statistics
            _character.RecordLevelUp(newLevel);
            
            var tuning = GameConfiguration.Instance;
            
            // Apply class balance multipliers if available
            var classBalance = tuning.ClassBalance;
            if (classBalance != null && _character.Equipment.Weapon is WeaponItem weapon)
            {
                var classMultipliers = GetClassMultipliers(weapon.WeaponType, classBalance);
                _character.Health.MaxHealth += (int)(tuning.Character.HealthPerLevel * classMultipliers.HealthMultiplier);
            }
            else
            {
                _character.Health.MaxHealth += tuning.Character.HealthPerLevel;
            }
            
            _character.Health.CurrentHealth = _character.Health.MaxHealth;
            
            // Award class point and stat increases based on equipped weapon
            if (_character.Equipment.Weapon is WeaponItem equippedWeapon)
            {
                HandleWeaponBasedLevelUp(equippedWeapon);
            }
            else
            {
                HandleNoWeaponLevelUp();
            }

            // Re-add class actions when points change
            _character.Actions.AddClassActions(_character, _character.Progression, (_character.Equipment.Weapon as WeaponItem)?.WeaponType);
        }

        /// <summary>
        /// Gets class multipliers for a weapon type
        /// </summary>
        private ClassMultipliers GetClassMultipliers(WeaponType weaponType, ClassBalanceConfig classBalance)
        {
            return weaponType switch
            {
                WeaponType.Mace => classBalance.Barbarian,
                WeaponType.Sword => classBalance.Warrior,
                WeaponType.Dagger => classBalance.Rogue,
                WeaponType.Wand => classBalance.Wizard,
                _ => new ClassMultipliers() // Default multipliers
            };
        }

        /// <summary>
        /// Handles level up when a weapon is equipped
        /// </summary>
        private void HandleWeaponBasedLevelUp(WeaponItem equippedWeapon)
        {
            string className = GetClassName(equippedWeapon.WeaponType);
            
            _character.Progression.AwardClassPoint(equippedWeapon.WeaponType);
            
            DisplayLevelUpMessage(className, equippedWeapon);
            DisplayClassPointsInfo();
        }

        /// <summary>
        /// Handles level up when no weapon is equipped
        /// </summary>
        private void HandleNoWeaponLevelUp()
        {
            _character.Stats.LevelUpNoWeapon();
            
            UIManager.WriteLine($"\n*** LEVEL UP! ***");
            UIManager.WriteLine($"You reached level {_character.Progression.Level}!");
            UIManager.WriteLine("No weapon equipped - equal stat increases (+2 all stats)");
            UIManager.WriteBlankLine();
        }

        /// <summary>
        /// Gets class name from weapon type
        /// </summary>
        private string GetClassName(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Mace => "Barbarian",
                WeaponType.Sword => "Warrior",
                WeaponType.Dagger => "Rogue",
                WeaponType.Wand => "Wizard",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Displays level up message
        /// </summary>
        private void DisplayLevelUpMessage(string className, WeaponItem equippedWeapon)
        {
            UIManager.WriteLine($"\n*** LEVEL UP! ***");
            UIManager.WriteLine($"You reached level {_character.Progression.Level}!");
            UIManager.WriteLine($"Gained +1 {className} class point!");
            UIManager.WriteLine($"Stats increased: {_character.Stats.GetStatIncreaseMessage(equippedWeapon.WeaponType)}");
            UIManager.WriteLine($"Current class: {_character.Progression.GetCurrentClass()}");
            UIManager.WriteLine($"You are now known as: {_character.Progression.GetFullNameWithQualifier(_character.Name)}");
        }

        /// <summary>
        /// Displays class points information
        /// </summary>
        private void DisplayClassPointsInfo()
        {
            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            if (_character.Progression.BarbarianPoints > 0) classPointsInfo.Add($"Barbarian({_character.Progression.BarbarianPoints})");
            if (_character.Progression.WarriorPoints > 0) classPointsInfo.Add($"Warrior({_character.Progression.WarriorPoints})");
            if (_character.Progression.RoguePoints > 0) classPointsInfo.Add($"Rogue({_character.Progression.RoguePoints})");
            if (_character.Progression.WizardPoints > 0) classPointsInfo.Add($"Wizard({_character.Progression.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                UIManager.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                UIManager.WriteLine($"Next Upgrades: {_character.Progression.GetClassUpgradeInfo()}");
            }
            UIManager.WriteBlankLine();
        }
    }
}
