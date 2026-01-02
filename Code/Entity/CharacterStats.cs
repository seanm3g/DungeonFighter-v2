using System;

namespace RPGGame
{
    /// <summary>
    /// Manages character base stats and stat calculations
    /// </summary>
    public class CharacterStats
    {
        // Base attributes
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }

        // Temporary stat bonuses
        public int TempStrengthBonus { get; set; } = 0;
        public int TempAgilityBonus { get; set; } = 0;
        public int TempTechniqueBonus { get; set; } = 0;
        public int TempIntelligenceBonus { get; set; } = 0;
        public int TempStatBonusTurns { get; set; } = 0;

        public CharacterStats(int level = 1)
        {
            var tuning = GameConfiguration.Instance;
            
            // Get base attributes from config, with fallback defaults if config not loaded
            int baseStrength = tuning.Attributes.PlayerBaseAttributes?.Strength ?? 0;
            int baseAgility = tuning.Attributes.PlayerBaseAttributes?.Agility ?? 0;
            int baseTechnique = tuning.Attributes.PlayerBaseAttributes?.Technique ?? 0;
            int baseIntelligence = tuning.Attributes.PlayerBaseAttributes?.Intelligence ?? 0;
            int attributesPerLevel = tuning.Attributes.PlayerAttributesPerLevel;
            
            // Fallback to sensible defaults if config values are 0 (config not loaded or invalid)
            // Check if all base attributes are 0, which indicates config wasn't loaded properly
            if (baseStrength == 0 && baseAgility == 0 && baseTechnique == 0 && baseIntelligence == 0)
            {
                baseStrength = 3;
                baseAgility = 3;
                baseTechnique = 3;
                baseIntelligence = 3;
            }
            if (attributesPerLevel == 0)
            {
                attributesPerLevel = 2;
            }
            
            // Initialize attributes based on tuning config
            Strength = baseStrength + (level - 1) * attributesPerLevel;
            Agility = baseAgility + (level - 1) * attributesPerLevel;
            Technique = baseTechnique + (level - 1) * attributesPerLevel;
            Intelligence = baseIntelligence + (level - 1) * attributesPerLevel;
        }

        public void LevelUp(WeaponType weaponType)
        {
            // Increase stats based on weapon class: +3 for weapon class, +1 for others
            switch (weaponType)
            {
                case WeaponType.Mace: // Barbarian - Strength
                    Strength += 3;
                    Agility += 1;
                    Technique += 1;
                    Intelligence += 1;
                    break;
                case WeaponType.Sword: // Warrior - Agility
                    Strength += 1;
                    Agility += 3;
                    Technique += 1;
                    Intelligence += 1;
                    break;
                case WeaponType.Dagger: // Rogue - Technique
                    Strength += 1;
                    Agility += 1;
                    Technique += 3;
                    Intelligence += 1;
                    break;
                case WeaponType.Wand: // Wizard - Intelligence
                    Strength += 1;
                    Agility += 1;
                    Technique += 1;
                    Intelligence += 3;
                    break;
                default:
                    // Fallback: equal increases if unknown weapon type
                    Strength += 2;
                    Agility += 2;
                    Technique += 2;
                    Intelligence += 2;
                    break;
            }
        }

        public void LevelUpNoWeapon()
        {
            // No weapon equipped - equal stat increases
            Strength += 2;
            Agility += 2;
            Technique += 2;
            Intelligence += 2;
        }

        public void ApplyStatBonus(int bonus, string statType, int duration)
        {
            switch (statType.ToUpper())
            {
                case "STR":
                    TempStrengthBonus = bonus;
                    break;
                case "AGI":
                    TempAgilityBonus = bonus;
                    break;
                case "TEC":
                    TempTechniqueBonus = bonus;
                    break;
                case "INT":
                    TempIntelligenceBonus = bonus;
                    break;
            }
            TempStatBonusTurns = duration;
        }

        public void UpdateTempEffects(double actionLength = 1.0)
        {
            // Calculate how many turns this action represents
            double turnsPassed = actionLength / Character.DEFAULT_ACTION_LENGTH;
            
            // Update temporary stat bonuses
            if (TempStatBonusTurns > 0)
            {
                TempStatBonusTurns = Math.Max(0, TempStatBonusTurns - (int)Math.Ceiling(turnsPassed));
                if (TempStatBonusTurns == 0)
                {
                    TempStrengthBonus = 0;
                    TempAgilityBonus = 0;
                    TempTechniqueBonus = 0;
                    TempIntelligenceBonus = 0;
                }
            }
        }

        public int GetEffectiveStrength(int equipmentBonus = 0, int godlikeBonus = 0)
        {
            return Strength + TempStrengthBonus + equipmentBonus + godlikeBonus;
        }

        public int GetEffectiveAgility(int equipmentBonus = 0)
        {
            return Agility + TempAgilityBonus + equipmentBonus;
        }

        public int GetEffectiveTechnique(int equipmentBonus = 0)
        {
            return Technique + TempTechniqueBonus + equipmentBonus;
        }

        public int GetEffectiveIntelligence(int equipmentBonus = 0)
        {
            return Intelligence + TempIntelligenceBonus + equipmentBonus;
        }

        public string GetStatIncreaseMessage(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Mace => "+3 STR, +1 AGI, +1 TEC, +1 INT",    // Barbarian - Strength
                WeaponType.Sword => "+1 STR, +3 AGI, +1 TEC, +1 INT",   // Warrior - Agility
                WeaponType.Dagger => "+1 STR, +1 AGI, +3 TEC, +1 INT",  // Rogue - Technique
                WeaponType.Wand => "+1 STR, +1 AGI, +1 TEC, +3 INT",    // Wizard - Intelligence
                _ => "+2 all stats"
            };
        }

        public bool MeetsStatThreshold(string statType, double threshold, int equipmentBonus = 0, int godlikeBonus = 0)
        {
            return statType.ToUpper() switch
            {
                "STR" => GetEffectiveStrength(equipmentBonus, godlikeBonus) >= threshold,
                "AGI" => GetEffectiveAgility(equipmentBonus) >= threshold,
                "TEC" => GetEffectiveTechnique(equipmentBonus) >= threshold,
                "INT" => GetEffectiveIntelligence(equipmentBonus) >= threshold,
                _ => false
            };
        }

        public void SetTechniqueForTesting(int value)
        {
            Technique = value;
        }
    }
}
