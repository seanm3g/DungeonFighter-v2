using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Utils;

namespace RPGGame.Editors
{
    /// <summary>
    /// Editor for tweaking all game variables from GameConfiguration and GameSettings
    /// Organized by category for easy access
    /// </summary>
    public class VariableEditor
    {
        private readonly GameConfiguration config;
        private readonly GameSettings gameSettings;
        private readonly Dictionary<string, List<EditableVariable>> variablesByCategory;

        public VariableEditor()
        {
            config = GameConfiguration.Instance;
            gameSettings = GameSettings.Instance;
            variablesByCategory = new Dictionary<string, List<EditableVariable>>();
            InitializeVariables();
        }

        /// <summary>
        /// Initialize all editable variables organized by category
        /// </summary>
        private void InitializeVariables()
        {
            // Combat Parameters
            var combatVars = new List<EditableVariable>
            {
                new EditableVariable("Combat.CriticalHitThreshold", () => config.Combat.CriticalHitThreshold, v => config.Combat.CriticalHitThreshold = Convert.ToInt32(v), "Critical hit threshold (1-20)"),
                new EditableVariable("Combat.CriticalHitMultiplier", () => config.Combat.CriticalHitMultiplier, v => config.Combat.CriticalHitMultiplier = Convert.ToDouble(v), "Critical hit damage multiplier"),
                new EditableVariable("Combat.MinimumDamage", () => config.Combat.MinimumDamage, v => config.Combat.MinimumDamage = Convert.ToInt32(v), "Minimum damage dealt"),
                new EditableVariable("Combat.BaseAttackTime", () => config.Combat.BaseAttackTime, v => config.Combat.BaseAttackTime = Convert.ToDouble(v), "Base attack time in seconds"),
                new EditableVariable("Combat.AgilitySpeedReduction", () => config.Combat.AgilitySpeedReduction, v => config.Combat.AgilitySpeedReduction = Convert.ToDouble(v), "Agility speed reduction per point"),
                new EditableVariable("Combat.MinimumAttackTime", () => config.Combat.MinimumAttackTime, v => config.Combat.MinimumAttackTime = Convert.ToDouble(v), "Minimum attack time"),
                new EditableVariable("CombatBalance.CriticalHitChance", () => config.CombatBalance.CriticalHitChance, v => config.CombatBalance.CriticalHitChance = Convert.ToDouble(v), "Base critical hit chance"),
                new EditableVariable("CombatBalance.CriticalHitDamageMultiplier", () => config.CombatBalance.CriticalHitDamageMultiplier, v => config.CombatBalance.CriticalHitDamageMultiplier = Convert.ToDouble(v), "Critical hit damage multiplier"),
                new EditableVariable("CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier", () => config.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier, v => config.CombatBalance.RollDamageMultipliers.ComboRollDamageMultiplier = Convert.ToDouble(v), "Combo roll damage multiplier"),
                new EditableVariable("CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier", () => config.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier, v => config.CombatBalance.RollDamageMultipliers.BasicRollDamageMultiplier = Convert.ToDouble(v), "Basic roll damage multiplier"),
            };
            variablesByCategory["Combat"] = combatVars;

            // Roll System
            var rollSystemVars = new List<EditableVariable>
            {
                new EditableVariable("RollSystem.MissThreshold.Min", () => config.RollSystem.MissThreshold.Min, v => config.RollSystem.MissThreshold.Min = Convert.ToInt32(v), "Minimum roll for miss"),
                new EditableVariable("RollSystem.MissThreshold.Max", () => config.RollSystem.MissThreshold.Max, v => config.RollSystem.MissThreshold.Max = Convert.ToInt32(v), "Maximum roll for miss"),
                new EditableVariable("RollSystem.BasicAttackThreshold.Min", () => config.RollSystem.BasicAttackThreshold.Min, v => config.RollSystem.BasicAttackThreshold.Min = Convert.ToInt32(v), "Minimum roll for basic attack"),
                new EditableVariable("RollSystem.BasicAttackThreshold.Max", () => config.RollSystem.BasicAttackThreshold.Max, v => config.RollSystem.BasicAttackThreshold.Max = Convert.ToInt32(v), "Maximum roll for basic attack"),
                new EditableVariable("RollSystem.ComboThreshold.Min", () => config.RollSystem.ComboThreshold.Min, v => config.RollSystem.ComboThreshold.Min = Convert.ToInt32(v), "Minimum roll for combo"),
                new EditableVariable("RollSystem.ComboThreshold.Max", () => config.RollSystem.ComboThreshold.Max, v => config.RollSystem.ComboThreshold.Max = Convert.ToInt32(v), "Maximum roll for combo"),
                new EditableVariable("RollSystem.CriticalThreshold", () => config.RollSystem.CriticalThreshold, v => config.RollSystem.CriticalThreshold = Convert.ToInt32(v), "Roll required for critical hit"),
            };
            variablesByCategory["RollSystem"] = rollSystemVars;

            // Character/Player Settings
            var characterVars = new List<EditableVariable>
            {
                new EditableVariable("Character.PlayerBaseHealth", () => config.Character.PlayerBaseHealth, v => config.Character.PlayerBaseHealth = Convert.ToInt32(v), "Player base health at level 1"),
                new EditableVariable("Character.HealthPerLevel", () => config.Character.HealthPerLevel, v => config.Character.HealthPerLevel = Convert.ToInt32(v), "Health gained per level"),
                new EditableVariable("Attributes.PlayerBaseAttributes.Strength", () => config.Attributes.PlayerBaseAttributes.Strength, v => config.Attributes.PlayerBaseAttributes.Strength = Convert.ToInt32(v), "Base Strength"),
                new EditableVariable("Attributes.PlayerBaseAttributes.Agility", () => config.Attributes.PlayerBaseAttributes.Agility, v => config.Attributes.PlayerBaseAttributes.Agility = Convert.ToInt32(v), "Base Agility"),
                new EditableVariable("Attributes.PlayerBaseAttributes.Technique", () => config.Attributes.PlayerBaseAttributes.Technique, v => config.Attributes.PlayerBaseAttributes.Technique = Convert.ToInt32(v), "Base Technique"),
                new EditableVariable("Attributes.PlayerBaseAttributes.Intelligence", () => config.Attributes.PlayerBaseAttributes.Intelligence, v => config.Attributes.PlayerBaseAttributes.Intelligence = Convert.ToInt32(v), "Base Intelligence"),
                new EditableVariable("Attributes.PlayerAttributesPerLevel", () => config.Attributes.PlayerAttributesPerLevel, v => config.Attributes.PlayerAttributesPerLevel = Convert.ToInt32(v), "Attributes gained per level"),
                new EditableVariable("Attributes.EnemyAttributesPerLevel", () => config.Attributes.EnemyAttributesPerLevel, v => config.Attributes.EnemyAttributesPerLevel = Convert.ToInt32(v), "Enemy attributes per level"),
                new EditableVariable("Attributes.EnemyPrimaryAttributeBonus", () => config.Attributes.EnemyPrimaryAttributeBonus, v => config.Attributes.EnemyPrimaryAttributeBonus = Convert.ToInt32(v), "Enemy primary attribute bonus"),
                new EditableVariable("Attributes.IntelligenceRollBonusPer", () => config.Attributes.IntelligenceRollBonusPer, v => config.Attributes.IntelligenceRollBonusPer = Convert.ToInt32(v), "Intelligence roll bonus per 10 points"),
            };
            variablesByCategory["Character"] = characterVars;

            // Enemy System
            var enemyVars = new List<EditableVariable>
            {
                new EditableVariable("EnemySystem.GlobalMultipliers.HealthMultiplier", () => config.EnemySystem.GlobalMultipliers.HealthMultiplier, v => config.EnemySystem.GlobalMultipliers.HealthMultiplier = Convert.ToDouble(v), "Global enemy health multiplier"),
                new EditableVariable("EnemySystem.GlobalMultipliers.DamageMultiplier", () => config.EnemySystem.GlobalMultipliers.DamageMultiplier, v => config.EnemySystem.GlobalMultipliers.DamageMultiplier = Convert.ToDouble(v), "Global enemy damage multiplier"),
                new EditableVariable("EnemySystem.GlobalMultipliers.ArmorMultiplier", () => config.EnemySystem.GlobalMultipliers.ArmorMultiplier, v => config.EnemySystem.GlobalMultipliers.ArmorMultiplier = Convert.ToDouble(v), "Global enemy armor multiplier"),
                new EditableVariable("EnemySystem.GlobalMultipliers.SpeedMultiplier", () => config.EnemySystem.GlobalMultipliers.SpeedMultiplier, v => config.EnemySystem.GlobalMultipliers.SpeedMultiplier = Convert.ToDouble(v), "Global enemy speed multiplier"),
                new EditableVariable("EnemySystem.BaselineStats.Health", () => config.EnemySystem.BaselineStats.Health, v => config.EnemySystem.BaselineStats.Health = Convert.ToInt32(v), "Enemy baseline health"),
                new EditableVariable("EnemySystem.BaselineStats.Strength", () => config.EnemySystem.BaselineStats.Strength, v => config.EnemySystem.BaselineStats.Strength = Convert.ToInt32(v), "Enemy baseline strength"),
                new EditableVariable("EnemySystem.BaselineStats.Agility", () => config.EnemySystem.BaselineStats.Agility, v => config.EnemySystem.BaselineStats.Agility = Convert.ToInt32(v), "Enemy baseline agility"),
                new EditableVariable("EnemySystem.BaselineStats.Technique", () => config.EnemySystem.BaselineStats.Technique, v => config.EnemySystem.BaselineStats.Technique = Convert.ToInt32(v), "Enemy baseline technique"),
                new EditableVariable("EnemySystem.BaselineStats.Intelligence", () => config.EnemySystem.BaselineStats.Intelligence, v => config.EnemySystem.BaselineStats.Intelligence = Convert.ToInt32(v), "Enemy baseline intelligence"),
                new EditableVariable("EnemySystem.BaselineStats.Armor", () => config.EnemySystem.BaselineStats.Armor, v => config.EnemySystem.BaselineStats.Armor = Convert.ToInt32(v), "Enemy baseline armor"),
                new EditableVariable("EnemySystem.ScalingPerLevel.Health", () => config.EnemySystem.ScalingPerLevel.Health, v => config.EnemySystem.ScalingPerLevel.Health = Convert.ToInt32(v), "Enemy health per level"),
                new EditableVariable("EnemySystem.ScalingPerLevel.Attributes", () => config.EnemySystem.ScalingPerLevel.Attributes, v => config.EnemySystem.ScalingPerLevel.Attributes = Convert.ToInt32(v), "Enemy attributes per level"),
                new EditableVariable("EnemySystem.ScalingPerLevel.Armor", () => config.EnemySystem.ScalingPerLevel.Armor, v => config.EnemySystem.ScalingPerLevel.Armor = Convert.ToDouble(v), "Enemy armor per level"),
            };
            variablesByCategory["EnemySystem"] = enemyVars;

            // Items/Equipment
            var itemVars = new List<EditableVariable>
            {
                new EditableVariable("EquipmentScaling.WeaponDamagePerTier", () => config.EquipmentScaling.WeaponDamagePerTier, v => config.EquipmentScaling.WeaponDamagePerTier = Convert.ToInt32(v), "Weapon damage per tier"),
                new EditableVariable("EquipmentScaling.ArmorValuePerTier", () => config.EquipmentScaling.ArmorValuePerTier, v => config.EquipmentScaling.ArmorValuePerTier = Convert.ToInt32(v), "Armor value per tier"),
                new EditableVariable("EquipmentScaling.SpeedBonusPerTier", () => config.EquipmentScaling.SpeedBonusPerTier, v => config.EquipmentScaling.SpeedBonusPerTier = Convert.ToDouble(v), "Speed bonus per tier"),
                new EditableVariable("EquipmentScaling.MaxTier", () => config.EquipmentScaling.MaxTier, v => config.EquipmentScaling.MaxTier = Convert.ToInt32(v), "Maximum tier"),
                new EditableVariable("EquipmentScaling.EnchantmentChance", () => config.EquipmentScaling.EnchantmentChance, v => config.EquipmentScaling.EnchantmentChance = Convert.ToDouble(v), "Enchantment chance"),
                new EditableVariable("WeaponScaling.GlobalDamageMultiplier", () => config.WeaponScaling?.GlobalDamageMultiplier ?? 1.0, v => { if (config.WeaponScaling != null) config.WeaponScaling.GlobalDamageMultiplier = Convert.ToDouble(v); }, "Global weapon damage multiplier"),
            };
            variablesByCategory["Items"] = itemVars;

            // Progression/XP
            var progressionVars = new List<EditableVariable>
            {
                new EditableVariable("Progression.BaseXPToLevel2", () => config.Progression.BaseXPToLevel2, v => config.Progression.BaseXPToLevel2 = Convert.ToInt32(v), "Base XP to reach level 2"),
                new EditableVariable("Progression.XPScalingFactor", () => config.Progression.XPScalingFactor, v => config.Progression.XPScalingFactor = Convert.ToDouble(v), "XP scaling factor per level"),
                new EditableVariable("Progression.EnemyXPBase", () => config.Progression.EnemyXPBase, v => config.Progression.EnemyXPBase = Convert.ToInt32(v), "Base XP from enemies"),
                new EditableVariable("Progression.EnemyXPPerLevel", () => config.Progression.EnemyXPPerLevel, v => config.Progression.EnemyXPPerLevel = Convert.ToInt32(v), "XP per enemy level"),
                new EditableVariable("Progression.EnemyGoldBase", () => config.Progression.EnemyGoldBase, v => config.Progression.EnemyGoldBase = Convert.ToInt32(v), "Base gold from enemies"),
                new EditableVariable("Progression.EnemyGoldPerLevel", () => config.Progression.EnemyGoldPerLevel, v => config.Progression.EnemyGoldPerLevel = Convert.ToInt32(v), "Gold per enemy level"),
                new EditableVariable("ExperienceSystem.LevelCap", () => config.ExperienceSystem.LevelCap, v => config.ExperienceSystem.LevelCap = Convert.ToInt32(v), "Maximum level"),
                new EditableVariable("ExperienceSystem.StatPointsPerLevel", () => config.ExperienceSystem.StatPointsPerLevel, v => config.ExperienceSystem.StatPointsPerLevel = Convert.ToInt32(v), "Stat points per level"),
                new EditableVariable("ExperienceSystem.SkillPointsPerLevel", () => config.ExperienceSystem.SkillPointsPerLevel, v => config.ExperienceSystem.SkillPointsPerLevel = Convert.ToInt32(v), "Skill points per level"),
                new EditableVariable("ExperienceSystem.AttributeCap", () => config.ExperienceSystem.AttributeCap, v => config.ExperienceSystem.AttributeCap = Convert.ToInt32(v), "Maximum attribute value"),
            };
            variablesByCategory["Progression"] = progressionVars;

            // Status Effects
            var statusVars = new List<EditableVariable>
            {
                new EditableVariable("StatusEffects.Bleed.DamagePerTick", () => config.StatusEffects.Bleed.DamagePerTick, v => config.StatusEffects.Bleed.DamagePerTick = Convert.ToInt32(v), "Bleed damage per tick"),
                new EditableVariable("StatusEffects.Bleed.TickInterval", () => config.StatusEffects.Bleed.TickInterval, v => config.StatusEffects.Bleed.TickInterval = Convert.ToDouble(v), "Bleed tick interval"),
                new EditableVariable("StatusEffects.Bleed.MaxStacks", () => config.StatusEffects.Bleed.MaxStacks, v => config.StatusEffects.Bleed.MaxStacks = Convert.ToInt32(v), "Bleed max stacks"),
                new EditableVariable("StatusEffects.Poison.DamagePerTick", () => config.StatusEffects.Poison.DamagePerTick, v => config.StatusEffects.Poison.DamagePerTick = Convert.ToInt32(v), "Poison damage per tick"),
                new EditableVariable("StatusEffects.Poison.TickInterval", () => config.StatusEffects.Poison.TickInterval, v => config.StatusEffects.Poison.TickInterval = Convert.ToDouble(v), "Poison tick interval"),
                new EditableVariable("StatusEffects.Burn.DamagePerTick", () => config.StatusEffects.Burn.DamagePerTick, v => config.StatusEffects.Burn.DamagePerTick = Convert.ToInt32(v), "Burn damage per tick"),
                new EditableVariable("StatusEffects.Burn.TickInterval", () => config.StatusEffects.Burn.TickInterval, v => config.StatusEffects.Burn.TickInterval = Convert.ToDouble(v), "Burn tick interval"),
                new EditableVariable("StatusEffects.Stun.SkipTurns", () => config.StatusEffects.Stun.SkipTurns, v => config.StatusEffects.Stun.SkipTurns = Convert.ToInt32(v), "Stun skip turns"),
                new EditableVariable("StatusEffects.Stun.Duration", () => config.StatusEffects.Stun.Duration, v => config.StatusEffects.Stun.Duration = Convert.ToDouble(v), "Stun duration"),
            };
            variablesByCategory["StatusEffects"] = statusVars;

            // Dungeon Generation
            var dungeonVars = new List<EditableVariable>
            {
                new EditableVariable("DungeonScaling.RoomCountBase", () => config.DungeonScaling.RoomCountBase, v => config.DungeonScaling.RoomCountBase = Convert.ToInt32(v), "Base room count"),
                new EditableVariable("DungeonScaling.RoomCountPerLevel", () => config.DungeonScaling.RoomCountPerLevel, v => config.DungeonScaling.RoomCountPerLevel = Convert.ToDouble(v), "Room count per level"),
                new EditableVariable("DungeonScaling.EnemyCountPerRoom", () => config.DungeonScaling.EnemyCountPerRoom, v => config.DungeonScaling.EnemyCountPerRoom = Convert.ToInt32(v), "Enemies per room"),
                new EditableVariable("DungeonScaling.BossRoomChance", () => config.DungeonScaling.BossRoomChance, v => config.DungeonScaling.BossRoomChance = Convert.ToDouble(v), "Boss room chance"),
                new EditableVariable("DungeonScaling.TrapRoomChance", () => config.DungeonScaling.TrapRoomChance, v => config.DungeonScaling.TrapRoomChance = Convert.ToDouble(v), "Trap room chance"),
                new EditableVariable("DungeonScaling.TreasureRoomChance", () => config.DungeonScaling.TreasureRoomChance, v => config.DungeonScaling.TreasureRoomChance = Convert.ToDouble(v), "Treasure room chance"),
            };
            variablesByCategory["Dungeon"] = dungeonVars;

            // Loot System
            var lootVars = new List<EditableVariable>
            {
                new EditableVariable("LootSystem.BaseDropChance", () => config.LootSystem.BaseDropChance, v => config.LootSystem.BaseDropChance = Convert.ToDouble(v), "Base drop chance"),
                new EditableVariable("LootSystem.DropChancePerLevel", () => config.LootSystem.DropChancePerLevel, v => config.LootSystem.DropChancePerLevel = Convert.ToDouble(v), "Drop chance per level"),
                new EditableVariable("LootSystem.MaxDropChance", () => config.LootSystem.MaxDropChance, v => config.LootSystem.MaxDropChance = Convert.ToDouble(v), "Maximum drop chance"),
                new EditableVariable("LootSystem.GuaranteedLootChance", () => config.LootSystem.GuaranteedLootChance, v => config.LootSystem.GuaranteedLootChance = Convert.ToDouble(v), "Guaranteed loot chance"),
                new EditableVariable("LootSystem.MagicFindEffectiveness", () => config.LootSystem.MagicFindEffectiveness, v => config.LootSystem.MagicFindEffectiveness = Convert.ToDouble(v), "Magic find effectiveness"),
                new EditableVariable("LootSystem.GoldDropMultiplier", () => config.LootSystem.GoldDropMultiplier, v => config.LootSystem.GoldDropMultiplier = Convert.ToDouble(v), "Gold drop multiplier"),
            };
            variablesByCategory["Loot"] = lootVars;

            // Combo System
            var comboVars = new List<EditableVariable>
            {
                new EditableVariable("ComboSystem.ComboAmplifierAtTech5", () => config.ComboSystem.ComboAmplifierAtTech5, v => config.ComboSystem.ComboAmplifierAtTech5 = Convert.ToDouble(v), "Combo amplifier at technique 5"),
                new EditableVariable("ComboSystem.ComboAmplifierMax", () => config.ComboSystem.ComboAmplifierMax, v => config.ComboSystem.ComboAmplifierMax = Convert.ToDouble(v), "Maximum combo amplifier"),
                new EditableVariable("ComboSystem.ComboAmplifierMaxTech", () => config.ComboSystem.ComboAmplifierMaxTech, v => config.ComboSystem.ComboAmplifierMaxTech = Convert.ToInt32(v), "Technique for max combo amplifier"),
            };
            variablesByCategory["Combo"] = comboVars;

            // Game Settings (User Preferences)
            var gameSettingsVars = new List<EditableVariable>
            {
                new EditableVariable("GameSettings.NarrativeBalance", () => gameSettings.NarrativeBalance, v => gameSettings.NarrativeBalance = Convert.ToDouble(v), "Narrative balance (0.0-1.0)"),
                new EditableVariable("GameSettings.CombatSpeed", () => gameSettings.CombatSpeed, v => gameSettings.CombatSpeed = Convert.ToDouble(v), "Combat speed multiplier"),
                new EditableVariable("GameSettings.EnemyHealthMultiplier", () => gameSettings.EnemyHealthMultiplier, v => gameSettings.EnemyHealthMultiplier = Convert.ToDouble(v), "Enemy health multiplier (difficulty)"),
                new EditableVariable("GameSettings.EnemyDamageMultiplier", () => gameSettings.EnemyDamageMultiplier, v => gameSettings.EnemyDamageMultiplier = Convert.ToDouble(v), "Enemy damage multiplier (difficulty)"),
                new EditableVariable("GameSettings.PlayerHealthMultiplier", () => gameSettings.PlayerHealthMultiplier, v => gameSettings.PlayerHealthMultiplier = Convert.ToDouble(v), "Player health multiplier"),
                new EditableVariable("GameSettings.PlayerDamageMultiplier", () => gameSettings.PlayerDamageMultiplier, v => gameSettings.PlayerDamageMultiplier = Convert.ToDouble(v), "Player damage multiplier"),
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
        /// Save changes to TuningConfig.json and gamesettings.json
        /// </summary>
        public bool SaveChanges()
        {
            try
            {
                // Save GameConfiguration to TuningConfig.json
                bool configSaved = config.SaveToFile();
                
                // Save GameSettings to gamesettings.json
                gameSettings.SaveSettings();
                
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

