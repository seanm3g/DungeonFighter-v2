using System;
using System.Collections.Generic;

namespace RPGGame
{
    public class ScalingManager
    {
        private static TuningConfig Config => TuningConfig.Instance;
        
        public static double CalculateWeaponDamage(int baseDamage, int tier, int level)
        {
            // First apply the tier-based damage ranges from weapon scaling config
            var weaponScaling = Config.WeaponScaling;
            if (weaponScaling != null)
            {
                // Get the appropriate damage range for this tier
                var tierRange = tier switch
                {
                    1 => weaponScaling.TierDamageRanges.Tier1,
                    2 => weaponScaling.TierDamageRanges.Tier2,
                    3 => weaponScaling.TierDamageRanges.Tier3,
                    4 => weaponScaling.TierDamageRanges.Tier4,
                    5 => weaponScaling.TierDamageRanges.Tier5,
                    _ => weaponScaling.TierDamageRanges.Tier1
                };
                
                // Use the tier range instead of base damage
                baseDamage = tierRange.Max; // Use max value for the tier
            }
            
            var formula = Config.ItemScaling?.WeaponDamageFormula;
            if (formula == null || string.IsNullOrEmpty(formula.Formula))
            {
                // Fallback to simple calculation if no formula
                double fallbackResult = baseDamage * (1 + (tier - 1) * 0.5);
                
                // Apply global damage multiplier
                if (weaponScaling != null)
                {
                    fallbackResult *= weaponScaling.GlobalDamageMultiplier;
                }
                
                return fallbackResult;
            }
            
            var variables = new Dictionary<string, double>
            {
                ["BaseDamage"] = baseDamage,
                ["Tier"] = tier,
                ["Level"] = level,
                ["TierScaling"] = formula.TierScaling,
                ["LevelScaling"] = formula.LevelScaling,
                ["BaseMultiplier"] = formula.BaseMultiplier
            };
            
            double formulaResult = FormulaEvaluator.Evaluate(formula.Formula, variables);
            
            // Apply global damage multiplier
            if (weaponScaling != null)
            {
                formulaResult *= weaponScaling.GlobalDamageMultiplier;
            }
            
            return formulaResult;
        }
        
        public static double CalculateArmorValue(int baseArmor, int tier, int level)
        {
            var formula = Config.ItemScaling?.ArmorValueFormula;
            if (formula == null || string.IsNullOrEmpty(formula.Formula))
            {
                // Fallback to simple calculation if no formula
                return baseArmor * (1 + (tier - 1) * 0.3);
            }
            
            var variables = new Dictionary<string, double>
            {
                ["BaseArmor"] = baseArmor,
                ["Tier"] = tier,
                ["Level"] = level,
                ["TierScaling"] = formula.TierScaling,
                ["LevelScaling"] = formula.LevelScaling,
                ["BaseMultiplier"] = formula.BaseMultiplier
            };
            
            return FormulaEvaluator.Evaluate(formula.Formula, variables);
        }
        
        public static double CalculateWeaponSpeed(double baseSpeed, int tier, int level, string weaponType)
        {
            // Get the weapon type config
            var weaponTypeConfig = Config.ItemScaling?.WeaponTypes?.GetValueOrDefault(weaponType);
            if (weaponTypeConfig == null || string.IsNullOrEmpty(weaponTypeConfig.SpeedFormula))
            {
                // Fallback to simple calculation if no formula
                return baseSpeed * (1 + (tier - 1) * 0.1);
            }
            
            var variables = new Dictionary<string, double>
            {
                ["BaseSpeed"] = baseSpeed,
                ["Tier"] = tier,
                ["Level"] = level
            };
            
            return FormulaEvaluator.Evaluate(weaponTypeConfig.SpeedFormula, variables);
        }
        
        public static double CalculateDropChance(string rarityTier, int playerLevel)
        {
            var formulas = Config.RarityScaling?.RollChanceFormulas;
            if (formulas == null)
            {
                return 0.1; // Default 10% chance
            }
            
            // Use base chance from rarity data
            double baseChance = GetBaseChanceForRarity(rarityTier);
            
            var variables = new Dictionary<string, double>
            {
                ["BaseChance"] = baseChance,
                ["PlayerLevel"] = playerLevel
            };
            
            // Use StatBonusChance formula as default
            string formula = formulas.StatBonusChance ?? "BaseChance";
            return FormulaEvaluator.Evaluate(formula, variables);
        }
        
        public static double GetRarityMultiplier(string rarity)
        {
            var multipliers = Config.RarityScaling?.StatBonusMultipliers;
            if (multipliers == null) return 1.0;
            
            return rarity?.ToLower() switch
            {
                "common" => multipliers.Common,
                "uncommon" => multipliers.Uncommon,
                "rare" => multipliers.Rare,
                "epic" => multipliers.Epic,
                "legendary" => multipliers.Legendary,
                _ => 1.0
            };
        }
        
        public static double CalculateExperienceRequired(int level)
        {
            var formula = Config.ProgressionCurves?.ExperienceFormula;
            if (formula == null || string.IsNullOrEmpty(formula))
            {
                // Fallback to existing system
                return Config.Progression.BaseXPToLevel2 * Math.Pow(Config.Progression.XPScalingFactor, level - 2);
            }
            
            var variables = new Dictionary<string, double>
            {
                ["BaseXP"] = Config.Progression.BaseXPToLevel2,
                ["Level"] = level,
                ["ExponentFactor"] = Config.ProgressionCurves?.ExponentFactor ?? 1.5
            };
            
            return FormulaEvaluator.Evaluate(formula, variables);
        }
        
        public static double CalculateAttributeGrowth(int baseAttributes, int level)
        {
            var formula = Config.ProgressionCurves?.AttributeGrowth;
            if (formula == null || string.IsNullOrEmpty(formula))
            {
                // Fallback to existing system
                return baseAttributes + (level * Config.Attributes.PlayerAttributesPerLevel);
            }
            
            var variables = new Dictionary<string, double>
            {
                ["BaseAttributes"] = baseAttributes,
                ["Level"] = level,
                ["LinearGrowth"] = Config.ProgressionCurves?.LinearGrowth ?? 1.0,
                ["QuadraticGrowth"] = Config.ProgressionCurves?.QuadraticGrowth ?? 0.1
            };
            
            return FormulaEvaluator.Evaluate(formula, variables);
        }
        
        private static double GetBaseChanceForRarity(string rarity)
        {
            // Base chances derived from current rarity weights
            return rarity?.ToLower() switch
            {
                "common" => 0.4,
                "uncommon" => 0.3,
                "rare" => 0.2,
                "epic" => 0.09,
                "legendary" => 0.01,
                _ => 0.1
            };
        }
        
        public static int CalculateXPReward(int enemyLevel, int playerLevel, int roomsCompleted = 0)
        {
            var config = Config.XPRewards;
            if (config == null) 
            {
                // Fallback to old system
                return Config.Progression.EnemyXPBase + (enemyLevel * Config.Progression.EnemyXPPerLevel);
            }
            
            // Calculate base XP using formula
            var baseXPVariables = new Dictionary<string, double>
            {
                ["EnemyXPBase"] = Config.Progression.EnemyXPBase,
                ["EnemyLevel"] = enemyLevel,
                ["EnemyXPPerLevel"] = Config.Progression.EnemyXPPerLevel
            };
            
            double baseXP = FormulaEvaluator.Evaluate(config.BaseXPFormula, baseXPVariables);
            
            // Calculate level difference multiplier
            int levelDifference = enemyLevel - playerLevel;
            double levelMultiplier = GetLevelDifferenceMultiplier(levelDifference);
            
            // Calculate final XP
            var xpVariables = new Dictionary<string, double>
            {
                ["BaseXP"] = baseXP,
                ["LevelMultiplier"] = levelMultiplier,
                ["DifficultyMultiplier"] = 1.0 // Can be expanded for future difficulty systems
            };
            
            double finalXP = FormulaEvaluator.Evaluate(config.GroupXPFormula, xpVariables);
            
            // Add dungeon completion bonus if applicable
            if (roomsCompleted > 0 && config.DungeonCompletionBonus != null)
            {
                double completionBonus = config.DungeonCompletionBonus.BaseBonus + 
                                       (roomsCompleted * config.DungeonCompletionBonus.BonusPerRoom);
                completionBonus *= levelMultiplier * config.DungeonCompletionBonus.LevelDifferenceMultiplier;
                finalXP += completionBonus;
            }
            
            // Apply caps
            finalXP = Math.Max(config.MinimumXP, finalXP);
            double maxXP = baseXP * config.MaximumXPMultiplier;
            finalXP = Math.Min(finalXP, maxXP);
            
            return (int)Math.Round(finalXP);
        }
        
        public static double GetLevelDifferenceMultiplier(int levelDifference)
        {
            var multipliers = Config.XPRewards?.LevelDifferenceMultipliers;
            if (multipliers == null) return 1.0;
            
            // Find the appropriate multiplier based on level difference
            foreach (var kvp in multipliers)
            {
                var multiplier = kvp.Value;
                
                // Handle ranges for extreme cases
                if (multiplier.LevelDifference == -3 && levelDifference <= -3) return multiplier.Multiplier;
                if (multiplier.LevelDifference == 3 && levelDifference >= 3) return multiplier.Multiplier;
                
                // Exact match
                if (multiplier.LevelDifference == levelDifference) return multiplier.Multiplier;
            }
            
            return 1.0; // Default to normal multiplier
        }
        
        public static void TestXPCalculations()
        {
            Console.WriteLine("=== XP CALCULATION TESTS ===");
            
            int playerLevel = 5;
            Console.WriteLine($"Player Level: {playerLevel}");
            Console.WriteLine("Enemy Level\tBase XP\tMultiplier\tFinal XP\tDescription");
            
            for (int enemyLevel = 2; enemyLevel <= 8; enemyLevel++)
            {
                int levelDiff = enemyLevel - playerLevel;
                double multiplier = GetLevelDifferenceMultiplier(levelDiff);
                int baseXP = Config.Progression.EnemyXPBase + (enemyLevel * Config.Progression.EnemyXPPerLevel);
                int finalXP = CalculateXPReward(enemyLevel, playerLevel);
                
                string description = GetDifficultyDescription(levelDiff);
                Console.WriteLine($"{enemyLevel}\t\t{baseXP}\t{multiplier:F1}x\t\t{finalXP}\t{description}");
            }
            
            Console.WriteLine("\nDungeon Completion Bonus Test:");
            for (int rooms = 1; rooms <= 5; rooms++)
            {
                int xpWithBonus = CalculateXPReward(playerLevel, playerLevel, rooms);
                int xpWithoutBonus = CalculateXPReward(playerLevel, playerLevel, 0);
                int bonus = xpWithBonus - xpWithoutBonus;
                Console.WriteLine($"  {rooms} rooms: +{bonus} XP bonus");
            }
        }
        
        private static string GetDifficultyDescription(int levelDifference)
        {
            var multipliers = Config.XPRewards?.LevelDifferenceMultipliers;
            if (multipliers == null) return "Normal";
            
            foreach (var kvp in multipliers)
            {
                var multiplier = kvp.Value;
                if (multiplier.LevelDifference == levelDifference ||
                    (multiplier.LevelDifference == -3 && levelDifference <= -3) ||
                    (multiplier.LevelDifference == 3 && levelDifference >= 3))
                {
                    return multiplier.Description;
                }
            }
            
            return "Normal";
        }

        public static void TestScalingFormulas()
        {
            Console.WriteLine("=== SCALING FORMULA TESTS ===");
            
            // Test weapon damage scaling
            Console.WriteLine("Weapon Damage Scaling:");
            for (int tier = 1; tier <= 5; tier++)
            {
                double damage = CalculateWeaponDamage(10, tier, 5);
                Console.WriteLine($"  Tier {tier}: Base 10 -> {damage:F1}");
            }
            
            // Test armor scaling
            Console.WriteLine("\nArmor Value Scaling:");
            for (int tier = 1; tier <= 5; tier++)
            {
                double armor = CalculateArmorValue(5, tier, 5);
                Console.WriteLine($"  Tier {tier}: Base 5 -> {armor:F1}");
            }
            
            // Test rarity multipliers
            Console.WriteLine("\nRarity Multipliers:");
            string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
            foreach (string rarity in rarities)
            {
                double multiplier = GetRarityMultiplier(rarity);
                Console.WriteLine($"  {rarity}: {multiplier:F1}x");
            }
            
            Console.WriteLine();
            TestXPCalculations();
            
            Console.WriteLine();
            // TestScaling.TestNewScalingValues(); // Removed - TestScaling.cs deleted
        }
    }
}
