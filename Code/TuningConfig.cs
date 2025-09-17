using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    public class TuningConfig
    {
        private static TuningConfig? _instance;
        private static readonly object _lock = new object();
        
        public CharacterConfig Character { get; set; } = new();
        public AttributesConfig Attributes { get; set; } = new();
        public CombatConfig Combat { get; set; } = new();
        public PoisonConfig Poison { get; set; } = new();
        public EquipmentConfig Equipment { get; set; } = new();
        public ProgressionConfig Progression { get; set; } = new();
        public LootConfig Loot { get; set; } = new();
        public RollSystemConfig RollSystem { get; set; } = new();
        public ComboSystemConfig ComboSystem { get; set; } = new();
        public EnemyScalingConfig EnemyScaling { get; set; } = new();
        public UIConfig UI { get; set; } = new();
        public GameSpeedConfig GameSpeed { get; set; } = new();

        public static TuningConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TuningConfig();
                            _instance.LoadFromFile();
                        }
                    }
                }
                return _instance;
            }
        }

        private void LoadFromFile()
        {
            try
            {
                // Try multiple possible paths for the config file
                string[] possiblePaths = {
                    Path.Combine("GameData", "TuningConfig.json"),
                    Path.Combine("..", "GameData", "TuningConfig.json"),
                    Path.Combine("..", "..", "GameData", "TuningConfig.json")
                };
                
                string? configPath = null;
                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        configPath = path;
                        break;
                    }
                }
                
                if (configPath != null)
                {
                    string jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<TuningConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (config != null)
                    {
                        Character = config.Character;
                        Attributes = config.Attributes;
                        Combat = config.Combat;
                        Equipment = config.Equipment;
                        Progression = config.Progression;
                        Loot = config.Loot;
                        RollSystem = config.RollSystem;
                        ComboSystem = config.ComboSystem;
                        EnemyScaling = config.EnemyScaling;
                        UI = config.UI;
                    }
                }
                else
                {
                    Console.WriteLine($"Warning: TuningConfig.json not found, using default values. Tried paths: {string.Join(", ", possiblePaths)}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading tuning config: {ex.Message}");
                Console.WriteLine("Using default values");
            }
        }

        public void Reload()
        {
            LoadFromFile();
        }
    }

    public class CharacterConfig
    {
        public int PlayerBaseHealth { get; set; } = 50;
        public int HealthPerLevel { get; set; } = 3;
        public int EnemyHealthPerLevel { get; set; } = 3;
    }

    public class AttributesConfig
    {
        public AttributeSet PlayerBaseAttributes { get; set; } = new();
        public int PlayerAttributesPerLevel { get; set; } = 2;
        public int EnemyAttributesPerLevel { get; set; } = 2;
        public int EnemyPrimaryAttributeBonus { get; set; } = 1;
        public int IntelligenceRollBonusPer { get; set; } = 10;
    }

    public class AttributeSet
    {
        public int Strength { get; set; } = 8;
        public int Agility { get; set; } = 6;
        public int Technique { get; set; } = 4;
        public int Intelligence { get; set; } = 4;
    }

    public class CombatConfig
    {
        public int CriticalHitThreshold { get; set; } = 20;
        public double CriticalHitMultiplier { get; set; } = 2.0;
        public int MinimumDamage { get; set; } = 1;
        public double BaseAttackTime { get; set; } = 10.0;
        public double AgilitySpeedReduction { get; set; } = 0.1;
        public double MinimumAttackTime { get; set; } = 1.0;
    }

    public class PoisonConfig
    {
        public double TickInterval { get; set; } = 10.0;
        public int DamagePerTick { get; set; } = 3;
        public int StacksPerApplication { get; set; } = 3;
    }

    public class EquipmentConfig
    {
        public MinMaxConfig BonusDamageRange { get; set; } = new();
        public MinMaxConfig BonusAttackSpeedRange { get; set; } = new();
    }

    public class MinMaxConfig
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class ProgressionConfig
    {
        public int BaseXPToLevel2 { get; set; } = 100;
        public double XPScalingFactor { get; set; } = 1.5;
        public int EnemyXPBase { get; set; } = 10;
        public int EnemyXPPerLevel { get; set; } = 5;
        public int EnemyGoldBase { get; set; } = 5;
        public int EnemyGoldPerLevel { get; set; } = 3;
    }

    public class LootConfig
    {
        public double LootChanceBase { get; set; } = 0.3;
        public double LootChancePerLevel { get; set; } = 0.05;
        public double MaximumLootChance { get; set; } = 0.8;
    }

    public class RollSystemConfig
    {
        public MinMaxConfig MissThreshold { get; set; } = new() { Min = 1, Max = 5 };
        public MinMaxConfig BasicAttackThreshold { get; set; } = new() { Min = 6, Max = 13 };
        public MinMaxConfig ComboThreshold { get; set; } = new() { Min = 14, Max = 20 };
        public int CriticalThreshold { get; set; } = 20;
    }

    public class ComboSystemConfig
    {
        public double ComboAmplifierAtTech5 { get; set; } = 1.05;
        public double ComboAmplifierMax { get; set; } = 2.0;
        public int ComboAmplifierMaxTech { get; set; } = 100;
    }

    public class EnemyScalingConfig
    {
        public double EnemyHealthMultiplier { get; set; } = 1.0;
        public double EnemyDamageMultiplier { get; set; } = 1.0;
        public int EnemyLevelVariance { get; set; } = 1;
    }

    public class UIConfig
    {
        public bool EnableTextDelays { get; set; } = true;
        public int BaseDelayPerAction { get; set; } = 400;
        public int MinimumDelay { get; set; } = 50;
        public double CombatSpeedMultiplier { get; set; } = 1.0;
        public int CombatLogDelay { get; set; } = 500;
        public int MainMenuDelay { get; set; } = 500;
        public int DungeonEntryDelay { get; set; } = 1000;
        public int RoomEntryDelay { get; set; } = 1000;
        public int EnemyEncounterDelay { get; set; } = 1000;
    }
    
    public class GameSpeedConfig
    {
        public double GameTickerInterval { get; set; } = 1.0;
        public double GameSpeedMultiplier { get; set; } = 1.0;
    }
}
