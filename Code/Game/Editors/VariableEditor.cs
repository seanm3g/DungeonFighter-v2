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

            // Combat timing, attributes, equipment combat knobs → Settings → Combat Tuning panel.
            variablesByCategory["CombatTiming"] = new List<EditableVariable>();
            variablesByCategory["Attributes"] = new List<EditableVariable>();

            // Items/Equipment (non-combat loot affix tuning only)
            var itemVars = new List<EditableVariable>
            {
                new EditableVariable("LootSystem.MagicFindEffectiveness", () => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness, v => GameConfiguration.Instance.LootSystem.MagicFindEffectiveness = Convert.ToDouble(v), "Magic find effectiveness"),
                new EditableVariable("LootSystem.RarityUpgrade.Enabled", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.Enabled = Convert.ToBoolean(v), "Enable rarity upgrades"),
                new EditableVariable("LootSystem.RarityUpgrade.BaseUpgradeChance", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.BaseUpgradeChance, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.BaseUpgradeChance = Convert.ToDouble(v), "Base upgrade chance (0-1)"),
                new EditableVariable("LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier", () => GameConfiguration.Instance.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier, v => GameConfiguration.Instance.LootSystem.RarityUpgrade.UpgradeChanceDecayPerTier = Convert.ToDouble(v), "Upgrade decay per tier"),
            };
            variablesByCategory["Items"] = itemVars;

            // Progression/XP (combat XP/gold knobs → Combat Tuning Rewards tab)
            var progressionVars = new List<EditableVariable>
            {
                new EditableVariable("Progression.BaseXPToLevel2", () => GameConfiguration.Instance.Progression.BaseXPToLevel2, v => GameConfiguration.Instance.Progression.BaseXPToLevel2 = Convert.ToInt32(v), "L1→2 XP bar; 0 = one tier-1 dungeon completion; >0 scales whole curve to that first bar"),
                new EditableVariable("Progression.XPScalingFactor", () => GameConfiguration.Instance.Progression.XPScalingFactor, v => GameConfiguration.Instance.Progression.XPScalingFactor = Convert.ToDouble(v), "Multiplies every XP bar; ≤0 = 1.0 (dungeon-paced curve: 1, 1.5, 2, 3… completions/level)"),
                new EditableVariable("ExperienceSystem.LevelCap", () => GameConfiguration.Instance.ExperienceSystem.LevelCap, v => GameConfiguration.Instance.ExperienceSystem.LevelCap = Convert.ToInt32(v), "Maximum level"),
                new EditableVariable("ExperienceSystem.StatPointsPerLevel", () => GameConfiguration.Instance.ExperienceSystem.StatPointsPerLevel, v => GameConfiguration.Instance.ExperienceSystem.StatPointsPerLevel = Convert.ToInt32(v), "Stat points per level"),
                new EditableVariable("ExperienceSystem.SkillPointsPerLevel", () => GameConfiguration.Instance.ExperienceSystem.SkillPointsPerLevel, v => GameConfiguration.Instance.ExperienceSystem.SkillPointsPerLevel = Convert.ToInt32(v), "Skill points per level"),
                new EditableVariable("ExperienceSystem.AttributeCap", () => GameConfiguration.Instance.ExperienceSystem.AttributeCap, v => GameConfiguration.Instance.ExperienceSystem.AttributeCap = Convert.ToInt32(v), "Maximum attribute value"),
            };
            variablesByCategory["Progression"] = progressionVars;

            // Status effects → Combat Tuning Status Effects tab
            variablesByCategory["StatusEffects"] = new List<EditableVariable>();

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

            // Loot (combat drop rates → Combat Tuning Rewards tab)
            var lootVars = new List<EditableVariable>
            {
                new EditableVariable("LootSystem.DropChancePerLevel", () => GameConfiguration.Instance.LootSystem.DropChancePerLevel, v => GameConfiguration.Instance.LootSystem.DropChancePerLevel = Convert.ToDouble(v), "Drop chance per level"),
                new EditableVariable("LootSystem.GuaranteedLootChance", () => GameConfiguration.Instance.LootSystem.GuaranteedLootChance, v => GameConfiguration.Instance.LootSystem.GuaranteedLootChance = Convert.ToDouble(v), "Guaranteed loot chance"),
                new EditableVariable("LootSystem.AffixMagicFindMaxWeightBoost", () => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxWeightBoost, v => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxWeightBoost = Convert.ToDouble(v), "MF 100: max affix-line tier weight boost (prefix/suffix pools)"),
                new EditableVariable("LootSystem.AffixMagicFindMaxExtraChanceBoost", () => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxExtraChanceBoost, v => GameConfiguration.Instance.LootSystem.AffixMagicFindMaxExtraChanceBoost = Convert.ToDouble(v), "MF 100: max multiplier on optional affix extra chances"),
            };
            variablesByCategory["Loot"] = lootVars;

            // Game Settings (combat speed/multipliers → Combat Tuning panel)
            var gameSettingsVars = new List<EditableVariable>
            {
                new EditableVariable("GameSettings.NarrativeBalance", () => gameSettings.NarrativeBalance, v => gameSettings.NarrativeBalance = Convert.ToDouble(v), "Narrative balance (0.0-1.0)"),
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

