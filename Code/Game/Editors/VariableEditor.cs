using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Tuning;
using RPGGame.Utils;

namespace RPGGame.Editors
{
    /// <summary>
    /// Editor for tweaking all game variables from GameConfiguration and GameSettings
    /// Organized by category for easy access
    /// </summary>
    public class VariableEditor
    {
        private readonly GameSettings gameSettings;
        private readonly Dictionary<string, List<EditableVariable>> variablesByCategory;

        public VariableEditor()
        {
            gameSettings = GameSettings.Instance;
            variablesByCategory = new Dictionary<string, List<EditableVariable>>();
            InitializeVariables();
        }

        /// <summary>
        /// Initialize all editable variables organized by category
        /// </summary>
        private void InitializeVariables()
        {
            // Combat balance knobs (HP, damage, rolls, combo, goals) live in Settings → Combat tuning.

            // Attack timing / agility speed curve (not in Combat tuning panel)
            var combatTimingVars = new List<EditableVariable>
            {
                new EditableVariable("Combat.CriticalHitThreshold", () => GameConfiguration.Instance.Combat.CriticalHitThreshold, v => GameConfiguration.Instance.Combat.CriticalHitThreshold = Convert.ToInt32(v), "Critical hit threshold (1-20)"),
                new EditableVariable("Combat.BaseAttackTime", () => GameConfiguration.Instance.Combat.BaseAttackTime, v => GameConfiguration.Instance.Combat.BaseAttackTime = Convert.ToDouble(v), "Base attack time in seconds"),
                new EditableVariable("Combat.MinimumAttackTime", () => GameConfiguration.Instance.Combat.MinimumAttackTime, v => GameConfiguration.Instance.Combat.MinimumAttackTime = Convert.ToDouble(v), "Minimum attack time"),
                new EditableVariable("Combat.AgilityMin", () => GameConfiguration.Instance.Combat.AgilityMin, v => GameConfiguration.Instance.Combat.AgilityMin = Convert.ToInt32(v), "Minimum agility value for speed calculation"),
                new EditableVariable("Combat.AgilityMax", () => GameConfiguration.Instance.Combat.AgilityMax, v => GameConfiguration.Instance.Combat.AgilityMax = Convert.ToInt32(v), "Maximum agility value for speed calculation"),
                new EditableVariable("Combat.AgilityMinSpeedMultiplier", () => GameConfiguration.Instance.Combat.AgilityMinSpeedMultiplier, v => GameConfiguration.Instance.Combat.AgilityMinSpeedMultiplier = Convert.ToDouble(v), "Speed multiplier at minimum agility"),
                new EditableVariable("Combat.AgilityMaxSpeedMultiplier", () => GameConfiguration.Instance.Combat.AgilityMaxSpeedMultiplier, v => GameConfiguration.Instance.Combat.AgilityMaxSpeedMultiplier = Convert.ToDouble(v), "Speed multiplier at maximum agility"),
                new EditableVariable("CombatBalance.CriticalHitChance", () => GameConfiguration.Instance.CombatBalance.CriticalHitChance, v => GameConfiguration.Instance.CombatBalance.CriticalHitChance = Convert.ToDouble(v), "Base critical hit chance"),
                new EditableVariable("EnemySystem.GlobalMultipliers.SpeedMultiplier", () => GameConfiguration.Instance.EnemySystem.GlobalMultipliers.SpeedMultiplier, v => GameConfiguration.Instance.EnemySystem.GlobalMultipliers.SpeedMultiplier = Convert.ToDouble(v), "Global enemy speed multiplier"),
                new EditableVariable("EnemySystem.ProgressionScales.BaseHealthScale", () => GameConfiguration.Instance.EnemySystem.ProgressionScales.BaseHealthScale, v => GameConfiguration.Instance.EnemySystem.ProgressionScales.BaseHealthScale = Convert.ToDouble(v), "Scales enemy base health (see also Enemy tuning tab)"),
                new EditableVariable("EnemySystem.ProgressionScales.HealthGrowthScale", () => GameConfiguration.Instance.EnemySystem.ProgressionScales.HealthGrowthScale, v => GameConfiguration.Instance.EnemySystem.ProgressionScales.HealthGrowthScale = Convert.ToDouble(v), "Scales enemy HP per level (see also Enemy tuning tab)"),
                new EditableVariable("EnemySystem.ProgressionScales.AttributeGrowthScale", () => GameConfiguration.Instance.EnemySystem.ProgressionScales.AttributeGrowthScale, v => GameConfiguration.Instance.EnemySystem.ProgressionScales.AttributeGrowthScale = Convert.ToDouble(v), "Scales enemy attribute growth (see also Enemy tuning tab)"),
            };
            variablesByCategory["CombatTiming"] = combatTimingVars;

            var attributeVars = new List<EditableVariable>
            {
                new EditableVariable("Attributes.EnemyAttributesPerLevel", () => GameConfiguration.Instance.Attributes.EnemyAttributesPerLevel, v => GameConfiguration.Instance.Attributes.EnemyAttributesPerLevel = Convert.ToInt32(v), "Enemy attributes per level"),
                new EditableVariable("Attributes.EnemyPrimaryAttributeBonus", () => GameConfiguration.Instance.Attributes.EnemyPrimaryAttributeBonus, v => GameConfiguration.Instance.Attributes.EnemyPrimaryAttributeBonus = Convert.ToInt32(v), "Enemy primary attribute bonus"),
                new EditableVariable("Attributes.IntelligenceRollBonusPer", () => GameConfiguration.Instance.Attributes.IntelligenceRollBonusPer, v => GameConfiguration.Instance.Attributes.IntelligenceRollBonusPer = Convert.ToInt32(v), "Legacy flat INT roll bonus (unused; TECH milestones shift thresholds)"),
                new EditableVariable("EnemySystem.BaselineStats.Agility", () => GameConfiguration.Instance.EnemySystem.BaselineStats.Agility, v => GameConfiguration.Instance.EnemySystem.BaselineStats.Agility = Convert.ToInt32(v), "Enemy baseline agility"),
                new EditableVariable("EnemySystem.BaselineStats.Technique", () => GameConfiguration.Instance.EnemySystem.BaselineStats.Technique, v => GameConfiguration.Instance.EnemySystem.BaselineStats.Technique = Convert.ToInt32(v), "Enemy baseline technique"),
                new EditableVariable("EnemySystem.BaselineStats.Intelligence", () => GameConfiguration.Instance.EnemySystem.BaselineStats.Intelligence, v => GameConfiguration.Instance.EnemySystem.BaselineStats.Intelligence = Convert.ToInt32(v), "Enemy baseline intelligence"),
                new EditableVariable("EnemySystem.ScalingPerLevel.Attributes", () => GameConfiguration.Instance.EnemySystem.ScalingPerLevel.Attributes, v => GameConfiguration.Instance.EnemySystem.ScalingPerLevel.Attributes = Convert.ToDouble(v), "Enemy attributes per level"),
                new EditableVariable("EnemySystem.ScalingPerLevel.Armor", () => GameConfiguration.Instance.EnemySystem.ScalingPerLevel.Armor, v => GameConfiguration.Instance.EnemySystem.ScalingPerLevel.Armor = Convert.ToDouble(v), "Enemy armor per level"),
            };
            variablesByCategory["Attributes"] = attributeVars;

            // Items/Equipment
            var itemVars = new List<EditableVariable>
            {
                new EditableVariable("EquipmentScaling.WeaponDamagePerTier", () => GameConfiguration.Instance.EquipmentScaling.WeaponDamagePerTier, v => GameConfiguration.Instance.EquipmentScaling.WeaponDamagePerTier = Convert.ToInt32(v), "Weapon damage per tier"),
                new EditableVariable("EquipmentScaling.ArmorValuePerTier", () => GameConfiguration.Instance.EquipmentScaling.ArmorValuePerTier, v => GameConfiguration.Instance.EquipmentScaling.ArmorValuePerTier = Convert.ToInt32(v), "Armor value per tier"),
                new EditableVariable("EquipmentScaling.SpeedBonusPerTier", () => GameConfiguration.Instance.EquipmentScaling.SpeedBonusPerTier, v => GameConfiguration.Instance.EquipmentScaling.SpeedBonusPerTier = Convert.ToDouble(v), "Speed bonus per tier"),
                new EditableVariable("EquipmentScaling.MaxTier", () => GameConfiguration.Instance.EquipmentScaling.MaxTier, v => GameConfiguration.Instance.EquipmentScaling.MaxTier = Convert.ToInt32(v), "Maximum tier"),
                new EditableVariable("EquipmentScaling.EnchantmentChance", () => GameConfiguration.Instance.EquipmentScaling.EnchantmentChance, v => GameConfiguration.Instance.EquipmentScaling.EnchantmentChance = Convert.ToDouble(v), "Enchantment chance"),
                new EditableVariable("WeaponScaling.GlobalDamageMultiplier", () => GameConfiguration.Instance.WeaponScaling?.GlobalDamageMultiplier ?? 1.0, v => { if (GameConfiguration.Instance.WeaponScaling != null) GameConfiguration.Instance.WeaponScaling.GlobalDamageMultiplier = Convert.ToDouble(v); }, "Global weapon damage multiplier"),
                // Loot System Variables
                new EditableVariable("LootSystem.BaseDropChance", () => GameConfiguration.Instance.LootSystem.BaseDropChance, v => GameConfiguration.Instance.LootSystem.BaseDropChance = Convert.ToDouble(v), "Base loot drop chance (0-1)"),
                new EditableVariable("LootSystem.DropChancePerLevel", () => GameConfiguration.Instance.LootSystem.DropChancePerLevel, v => GameConfiguration.Instance.LootSystem.DropChancePerLevel = Convert.ToDouble(v), "Drop chance increase per level"),
                new EditableVariable("LootSystem.MagicFindEffectiveness", () => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness, v => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness = Convert.ToDouble(v), "Magic find effectiveness"),
                new EditableVariable("LootSystem.RarityUpgrade.Enabled", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled = Convert.ToBoolean(v), "Enable rarity upgrades"),
                new EditableVariable("LootSystem.RarityUpgrade.BaseUpgradeChance", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.BaseUpgradeChance, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.BaseUpgradeChance = Convert.ToDouble(v), "Base upgrade chance (0-1)"),
                new EditableVariable("LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier = Convert.ToDouble(v), "Upgrade decay per tier"),
            };
            variablesByCategory["Items"] = itemVars;

            // Progression/XP
            var progressionVars = new List<EditableVariable>
            {
                new EditableVariable("Progression.BaseXPToLevel2", () => GameConfiguration.Instance.Progression.BaseXPToLevel2, v => GameConfiguration.Instance.Progression.BaseXPToLevel2 = Convert.ToInt32(v), "L1→2 XP bar; 0 = one tier-1 dungeon completion; >0 scales whole curve to that first bar"),
                new EditableVariable("Progression.XPScalingFactor", () => GameConfiguration.Instance.Progression.XPScalingFactor, v => GameConfiguration.Instance.Progression.XPScalingFactor = Convert.ToDouble(v), "Multiplies every XP bar; ≤0 = 1.0 (dungeon-paced curve: 1, 1.5, 2, 3… completions/level)"),
                new EditableVariable("Progression.EnemyXPBase", () => GameConfiguration.Instance.Progression.EnemyXPBase, v => GameConfiguration.Instance.Progression.EnemyXPBase = Convert.ToInt32(v), "Base XP from enemies"),
                new EditableVariable("Progression.EnemyXPPerLevel", () => GameConfiguration.Instance.Progression.EnemyXPPerLevel, v => GameConfiguration.Instance.Progression.EnemyXPPerLevel = Convert.ToInt32(v), "XP per enemy level"),
                new EditableVariable("Progression.EnemyGoldBase", () => GameConfiguration.Instance.Progression.EnemyGoldBase, v => GameConfiguration.Instance.Progression.EnemyGoldBase = Convert.ToInt32(v), "Base gold from enemies"),
                new EditableVariable("Progression.EnemyGoldPerLevel", () => GameConfiguration.Instance.Progression.EnemyGoldPerLevel, v => GameConfiguration.Instance.Progression.EnemyGoldPerLevel = Convert.ToInt32(v), "Gold per enemy level"),
                new EditableVariable("ExperienceSystem.LevelCap", () => GameConfiguration.Instance.ExperienceSystem.LevelCap, v => GameConfiguration.Instance.ExperienceSystem.LevelCap = Convert.ToInt32(v), "Maximum level"),
                new EditableVariable("ExperienceSystem.StatPointsPerLevel", () => GameConfiguration.Instance.ExperienceSystem.StatPointsPerLevel, v => GameConfiguration.Instance.ExperienceSystem.StatPointsPerLevel = Convert.ToInt32(v), "Stat points per level"),
                new EditableVariable("ExperienceSystem.SkillPointsPerLevel", () => GameConfiguration.Instance.ExperienceSystem.SkillPointsPerLevel, v => GameConfiguration.Instance.ExperienceSystem.SkillPointsPerLevel = Convert.ToInt32(v), "Skill points per level"),
                new EditableVariable("ExperienceSystem.AttributeCap", () => GameConfiguration.Instance.ExperienceSystem.AttributeCap, v => GameConfiguration.Instance.ExperienceSystem.AttributeCap = Convert.ToInt32(v), "Maximum attribute value"),
            };
            variablesByCategory["Progression"] = progressionVars;

            // Status Effects - dynamically generated from dictionary
            var statusVars = new List<EditableVariable>();
            GameConfiguration.Instance.StatusEffects.InitializeDefaults();
            foreach (var kvp in GameConfiguration.Instance.StatusEffects.Effects)
            {
                string effectName = kvp.Key;
                var effectConfig = kvp.Value;
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.DamagePerTick", 
                    () => effectConfig.DamagePerTick, 
                    v => effectConfig.DamagePerTick = Convert.ToInt32(v), 
                    $"{effectName} damage per tick"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.TickInterval", 
                    () => effectConfig.TickInterval, 
                    v => effectConfig.TickInterval = Convert.ToDouble(v), 
                    $"{effectName} tick interval"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.MaxStacks", 
                    () => effectConfig.MaxStacks, 
                    v => effectConfig.MaxStacks = Convert.ToInt32(v), 
                    $"{effectName} max stacks"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.StacksPerApplication", 
                    () => effectConfig.StacksPerApplication, 
                    v => effectConfig.StacksPerApplication = Convert.ToInt32(v), 
                    $"{effectName} stacks per application"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.SpeedReduction", 
                    () => effectConfig.SpeedReduction, 
                    v => effectConfig.SpeedReduction = Convert.ToDouble(v), 
                    $"{effectName} speed reduction"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.Duration", 
                    () => effectConfig.Duration, 
                    v => effectConfig.Duration = Convert.ToDouble(v), 
                    $"{effectName} duration"));
                
                statusVars.Add(new EditableVariable($"StatusEffects.{effectName}.SkipTurns", 
                    () => effectConfig.SkipTurns, 
                    v => effectConfig.SkipTurns = Convert.ToInt32(v), 
                    $"{effectName} skip turns"));
            }
            variablesByCategory["StatusEffects"] = statusVars;

            // Dungeon Generation
            var dungeonVars = new List<EditableVariable>
            {
                new EditableVariable("DungeonScaling.RoomCountBase", () => GameConfiguration.Instance.DungeonScaling.RoomCountBase, v => GameConfiguration.Instance.DungeonScaling.RoomCountBase = Convert.ToInt32(v), "Base room count"),
                new EditableVariable("DungeonScaling.RoomCountPerLevel", () => GameConfiguration.Instance.DungeonScaling.RoomCountPerLevel, v => GameConfiguration.Instance.DungeonScaling.RoomCountPerLevel = Convert.ToDouble(v), "Room count per level"),
                new EditableVariable("DungeonScaling.EnemyCountPerRoom", () => GameConfiguration.Instance.DungeonScaling.EnemyCountPerRoom, v => GameConfiguration.Instance.DungeonScaling.EnemyCountPerRoom = Convert.ToInt32(v), "Enemies per room"),
                new EditableVariable("DungeonScaling.BossRoomChance", () => GameConfiguration.Instance.DungeonScaling.BossRoomChance, v => GameConfiguration.Instance.DungeonScaling.BossRoomChance = Convert.ToDouble(v), "Boss room chance"),
                new EditableVariable("DungeonScaling.TrapRoomChance", () => GameConfiguration.Instance.DungeonScaling.TrapRoomChance, v => GameConfiguration.Instance.DungeonScaling.TrapRoomChance = Convert.ToDouble(v), "Trap room chance"),
                new EditableVariable("DungeonScaling.TreasureRoomChance", () => GameConfiguration.Instance.DungeonScaling.TreasureRoomChance, v => GameConfiguration.Instance.DungeonScaling.TreasureRoomChance = Convert.ToDouble(v), "Treasure room chance"),
            };
            variablesByCategory["Dungeon"] = dungeonVars;

            // Loot System
            var lootVars = new List<EditableVariable>
            {
                new EditableVariable("LootSystem.BaseDropChance", () => GameConfiguration.Instance.LootSystem.BaseDropChance, v => GameConfiguration.Instance.LootSystem.BaseDropChance = Convert.ToDouble(v), "Base drop chance"),
                new EditableVariable("LootSystem.DropChancePerLevel", () => GameConfiguration.Instance.LootSystem.DropChancePerLevel, v => GameConfiguration.Instance.LootSystem.DropChancePerLevel = Convert.ToDouble(v), "Drop chance per level"),
                new EditableVariable("LootSystem.MaxDropChance", () => GameConfiguration.Instance.LootSystem.MaxDropChance, v => GameConfiguration.Instance.LootSystem.MaxDropChance = Convert.ToDouble(v), "Maximum drop chance"),
                new EditableVariable("LootSystem.GuaranteedLootChance", () => GameConfiguration.Instance.LootSystem.GuaranteedLootChance, v => GameConfiguration.Instance.LootSystem.GuaranteedLootChance = Convert.ToDouble(v), "Guaranteed loot chance"),
                new EditableVariable("LootSystem.MagicFindEffectiveness", () => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness, v => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness = Convert.ToDouble(v), "Magic find effectiveness"),
                new EditableVariable("LootSystem.AffixMagicFindMaxWeightBoost", () => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxWeightBoost, v => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxWeightBoost = Convert.ToDouble(v), "MF 100: max affix-line tier weight boost (prefix/suffix pools)"),
                new EditableVariable("LootSystem.AffixMagicFindMaxExtraChanceBoost", () => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxExtraChanceBoost, v => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxExtraChanceBoost = Convert.ToDouble(v), "MF 100: max multiplier on optional affix extra chances"),
                new EditableVariable("LootSystem.GoldDropMultiplier", () => GameConfiguration.Instance.LootSystem.GoldDropMultiplier, v => GameConfiguration.Instance.LootSystem.GoldDropMultiplier = Convert.ToDouble(v), "Gold drop multiplier"),
            };
            variablesByCategory["Loot"] = lootVars;

            // Game Settings (non-combat difficulty; combat multipliers are in Combat tuning tab)
            var gameSettingsVars = new List<EditableVariable>
            {
                new EditableVariable("GameSettings.NarrativeBalance", () => gameSettings.NarrativeBalance, v => gameSettings.NarrativeBalance = Convert.ToDouble(v), "Narrative balance (0.0-1.0)"),
                new EditableVariable("GameSettings.CombatSpeed", () => gameSettings.CombatSpeed, v => gameSettings.CombatSpeed = Convert.ToDouble(v), "Combat speed multiplier"),
            };
            variablesByCategory["GameSettings"] = gameSettingsVars;
        }

        /// <summary>
        /// Get all editable variables (flattened list)
        /// </summary>
        public List<EditableVariable> GetVariables()
        {
            return variablesByCategory.Values.SelectMany(v => v).ToList();
        }

        /// <summary>
        /// Get all categories
        /// </summary>
        public List<string> GetCategories()
        {
            return variablesByCategory.Keys.OrderBy(k => k).ToList();
        }

        /// <summary>
        /// Get variables by category
        /// </summary>
        public List<EditableVariable> GetVariablesByCategory(string category)
        {
            return variablesByCategory.ContainsKey(category) 
                ? variablesByCategory[category] 
                : new List<EditableVariable>();
        }

        /// <summary>
        /// Get a variable by name
        /// </summary>
        public EditableVariable? GetVariable(string name)
        {
            return GetVariables().FirstOrDefault(v => v.Name == name);
        }

        /// <summary>
        /// Writes current in-memory tuning and game settings to disk without resetting the configuration singleton.
        /// Called when a single variable is committed in the variable editor UI.
        /// </summary>
        public bool PersistCurrentValues()
        {
            try
            {
                bool configSaved = GameConfiguration.Instance.SaveToFile();
                gameSettings.SaveSettings();
                return configSaved;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"VariableEditor: Error persisting values: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Applies tuning to the running player (when present) and persists to disk after a variable edit is committed.
        /// </summary>
        public void ApplyAndPersistAfterEdit(Character? player)
        {
            if (player != null)
                PlayerTuningApplier.ApplyToCurrentPlayer(player);
            PersistCurrentValues();
        }

        /// <summary>
        /// Save changes to TuningConfig.json and gamesettings.json
        /// After saving, reloads GameConfiguration to ensure new characters use updated values
        /// </summary>
        public bool SaveChanges()
        {
            try
            {
                // Save GameConfiguration to TuningConfig.json
                bool configSaved = GameConfiguration.Instance.SaveToFile();
                
                // Save GameSettings to gamesettings.json
                gameSettings.SaveSettings();
                
                // Reload GameConfiguration singleton to ensure new characters use updated values
                // This is important because GameConfiguration is a singleton that's cached,
                // and new characters read from it when they're created
                if (configSaved)
                {
                    GameConfiguration.ResetInstance();
                    // The next access to GameConfiguration.Instance will reload from the saved file
                }
                
                return configSaved;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"VariableEditor: Error saving changes: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Represents an editable game variable
    /// </summary>
    public class EditableVariable
    {
        public string Name { get; }
        public string Description { get; }
        private readonly Func<object> getter;
        private readonly Action<object> setter;

        public EditableVariable(string name, Func<object> getter, Action<object> setter, string description)
        {
            Name = name;
            this.getter = getter;
            this.setter = setter;
            Description = description;
        }

        public object GetValue()
        {
            return getter();
        }

        public void SetValue(object value)
        {
            setter(value);
        }

        public Type GetValueType()
        {
            return getter().GetType();
        }
    }
}

