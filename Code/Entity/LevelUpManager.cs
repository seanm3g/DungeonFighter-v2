using System;
using System.Collections.Generic;
using RPGGame.Combat.Calculators;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame
{
    /// <summary>
    /// Manages level up for the player character. Level comes from XP (<see cref="CharacterProgression.AddXP"/>).
    /// Stat growth follows the equipped weapon type; exactly one class point is awarded on that weapon path when a weapon is equipped at level-up (attributes do not award class points).
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
            var levelUpInfo = LevelUpWithInfo();
            // Display level-up info immediately (for backwards compatibility when not in dungeon completion)
            DisplayLevelUpInfo(levelUpInfo);
        }

        /// <summary>
        /// Applies one level's stat, health, and class-point rewards when <see cref="CharacterProgression.AddXP"/>
        /// has already incremented level, and prints the level-up block for the step that reached <paramref name="displayedNewLevel"/>.
        /// </summary>
        public void ApplyProgressionLevelRewardsAndDisplay(int displayedNewLevel)
        {
            var levelUpInfo = LevelUpWithInfo(levelAlreadyIncremented: true, displayedNewLevel: displayedNewLevel);
            DisplayLevelUpInfo(levelUpInfo);
        }
        
        /// <summary>
        /// Handles the complete level up process and returns level-up information
        /// </summary>
        /// <param name="levelAlreadyIncremented">If true, level was already incremented by AddXP. If false (default), increment it now.</param>
        /// <param name="displayedNewLevel">When <paramref name="levelAlreadyIncremented"/> is true after a multi-level XP batch, pass the level this step represents (e.g. first step = old+1). When null, uses <see cref="Character.Level"/>.</param>
        public LevelUpInfo LevelUpWithInfo(bool levelAlreadyIncremented = false, int? displayedNewLevel = null)
        {
            // If level hasn't been incremented yet (e.g., called directly from tests),
            // increment it now. Otherwise, it was already incremented by Progression.AddXP()
            int newLevel;
            if (!levelAlreadyIncremented)
            {
                _character.Progression.LevelUp();
            }
            newLevel = displayedNewLevel ?? _character.Level;
            
            _character.Stats.LevelUp((_character.Equipment.Weapon as WeaponItem)?.WeaponType ?? WeaponType.Mace);
            
            // Track level up statistics
            _character.RecordLevelUp(newLevel);
            
            int healthIncrease = GetHealthIncreaseForLevelStep();
            
            // Apply health increase
            _character.Health.MaxHealth += healthIncrease;
            
            // Heal to full effective max health (including equipment bonuses)
            _character.Health.CurrentHealth = _character.GetEffectiveMaxHealth();
            
            // Award class point and stat increases based on equipped weapon
            LevelUpInfo levelUpInfo;
            if (_character.Equipment.Weapon is WeaponItem equippedWeapon)
            {
                levelUpInfo = HandleWeaponBasedLevelUpWithInfo(equippedWeapon, newLevel);
            }
            else
            {
                levelUpInfo = HandleNoWeaponLevelUpWithInfo(newLevel);
            }

            // Re-add class actions when points change
            _character.Actions.AddClassActions(_character, _character.Progression, (_character.Equipment.Weapon as WeaponItem)?.WeaponType);
            
            // Invalidate damage cache since stats changed
            DamageCalculator.InvalidateCache(_character);
            
            return levelUpInfo;
        }

        /// <summary>
        /// Reverses one level gained via <see cref="LevelUpWithInfo"/> (stats, health, class point). No UI output.
        /// </summary>
        public void LevelDownWithInfo()
        {
            if (_character.Progression.Level <= 1)
                return;

            var weapon = _character.Equipment.Weapon as WeaponItem;
            int healthIncrease = GetHealthIncreaseForLevelStep();

            // Mirror LevelUpWithInfo in reverse order (last forward effect undone first).
            if (weapon != null)
                _character.Progression.RemoveClassPoint(weapon.WeaponType);
            else
                _character.Stats.UndoLevelUpNoWeapon();

            _character.Health.MaxHealth = Math.Max(1, _character.Health.MaxHealth - healthIncrease);
            int cap = _character.GetEffectiveMaxHealth();
            if (_character.Health.CurrentHealth > cap)
                _character.Health.CurrentHealth = cap;

            if (weapon != null)
                _character.Stats.UndoLevelUp(weapon.WeaponType);
            else
                _character.Stats.UndoLevelUp(WeaponType.Mace);

            _character.Progression.Level--;

            _character.RecordLevelUp(_character.Level);
            _character.Actions.AddClassActions(_character, _character.Progression, weapon?.WeaponType);
            DamageCalculator.InvalidateCache(_character);
        }

        private int GetHealthIncreaseForLevelStep()
        {
            var tuning = GameConfiguration.Instance;

            int healthIncrease = tuning.Character.HealthPerLevel;
            var classBalance = tuning.ClassBalance;

            if (classBalance != null && _character.Equipment.Weapon is WeaponItem weapon)
            {
                var classMultipliers = GetClassMultipliers(weapon.WeaponType, classBalance);
                if (classMultipliers.HealthMultiplier > 0)
                    healthIncrease = (int)(tuning.Character.HealthPerLevel * classMultipliers.HealthMultiplier);
            }

            if (healthIncrease <= 0)
            {
                healthIncrease = tuning.Character.HealthPerLevel > 0
                    ? tuning.Character.HealthPerLevel
                    : 1;
            }

            return healthIncrease;
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
        /// Handles level up when a weapon is equipped and returns level-up info
        /// </summary>
        private LevelUpInfo HandleWeaponBasedLevelUpWithInfo(WeaponItem equippedWeapon, int newLevel)
        {
            string className = GetClassName(equippedWeapon.WeaponType);
            
            _character.Progression.AwardClassPoint(equippedWeapon.WeaponType);
            
            var levelUpInfo = new LevelUpInfo
            {
                NewLevel = newLevel,
                ClassName = className,
                StatIncreaseMessage = _character.Stats.GetStatIncreaseMessage(equippedWeapon.WeaponType),
                CurrentClass = _character.GetCurrentClass(),
                FullNameWithQualifier = _character.Progression.GetFullNameWithQualifier(_character.Name, _character.Stats),
                HasWeapon = true
            };
            
            // Build class points info
            var classPointsInfo = new List<string>();
            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            if (_character.Progression.BarbarianPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Mace)}({_character.Progression.BarbarianPoints})");
            if (_character.Progression.WarriorPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Sword)}({_character.Progression.WarriorPoints})");
            if (_character.Progression.RoguePoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Dagger)}({_character.Progression.RoguePoints})");
            if (_character.Progression.WizardPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Wand)}({_character.Progression.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                levelUpInfo.ClassPointsInfo = string.Join(" ", classPointsInfo);
                levelUpInfo.ClassUpgradeInfo = _character.Progression.GetClassUpgradeInfo();
            }
            
            return levelUpInfo;
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
        /// Handles level up when no weapon is equipped and returns level-up info
        /// </summary>
        private LevelUpInfo HandleNoWeaponLevelUpWithInfo(int newLevel)
        {
            _character.Stats.LevelUpNoWeapon();
            
            return new LevelUpInfo
            {
                NewLevel = newLevel,
                HasWeapon = false
            };
        }

        /// <summary>
        /// Gets class name from weapon type
        /// </summary>
        private string GetClassName(WeaponType weaponType) =>
            GameConfiguration.Instance.ClassPresentation.EnsureNormalized().GetDisplayName(weaponType);

        /// <summary>
        /// Displays level up message
        /// </summary>
        private void DisplayLevelUpMessage(string className, WeaponItem equippedWeapon)
        {
            UIManager.WriteLine($"\n*** LEVEL UP! ***");
            UIManager.WriteLine($"You reached level {_character.Progression.Level}!");
            UIManager.WriteLine($"Gained +1 {className} class point!");
            UIManager.WriteLine($"Stats increased: {_character.Stats.GetStatIncreaseMessage(equippedWeapon.WeaponType)}");
            UIManager.WriteLine($"Current class: {_character.GetCurrentClass()}");
            UIManager.WriteLine($"You are now known as: {_character.Progression.GetFullNameWithQualifier(_character.Name, _character.Stats)}");
        }

        /// <summary>
        /// Displays class points information
        /// </summary>
        private void DisplayClassPointsInfo()
        {
            // Show only classes with points > 0
            var classPointsInfo = new List<string>();
            var pres = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            if (_character.Progression.BarbarianPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Mace)}({_character.Progression.BarbarianPoints})");
            if (_character.Progression.WarriorPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Sword)}({_character.Progression.WarriorPoints})");
            if (_character.Progression.RoguePoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Dagger)}({_character.Progression.RoguePoints})");
            if (_character.Progression.WizardPoints > 0) classPointsInfo.Add($"{pres.GetDisplayName(WeaponType.Wand)}({_character.Progression.WizardPoints})");
            
            if (classPointsInfo.Count > 0)
            {
                UIManager.WriteLine($"Class Points: {string.Join(" ", classPointsInfo)}");
                UIManager.WriteLine($"Next Upgrades: {_character.Progression.GetClassUpgradeInfo()}");
            }
            UIManager.WriteBlankLine();
        }
        
        /// <summary>
        /// Displays level-up information (for backwards compatibility)
        /// </summary>
        private void DisplayLevelUpInfo(LevelUpInfo levelUpInfo)
        {
            if (!levelUpInfo.IsValid) return;

            UIManager.WriteBlankLine();
            foreach (var line in LevelUpDisplayColoredText.BuildDisplayLines(levelUpInfo))
            {
                UIManager.WriteLineColoredSegments(line);
            }
            UIManager.WriteBlankLine();
        }
    }
}
