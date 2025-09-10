using System;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    public class GameSettings
    {
        // Narrative Settings
        public double NarrativeBalance { get; set; } = 0.7; // 0.0 = Event-driven, 1.0 = Poetic narrative
        public bool EnableNarrativeEvents { get; set; } = true;
        public bool EnableInformationalSummaries { get; set; } = true;
        
        // Combat Settings
        public double CombatSpeed { get; set; } = 1.0; // 0.5 = Slow, 2.0 = Fast
        public bool ShowIndividualActionMessages { get; set; } = false;
        public bool EnableComboSystem { get; set; } = true;
        public bool EnableTextDisplayDelays { get; set; } = true; // Only apply delays when text is displayed
        
        // Gameplay Settings
        public bool EnableAutoSave { get; set; } = true;
        public int AutoSaveInterval { get; set; } = 5; // Save every 5 encounters
        public bool ShowDetailedStats { get; set; } = true;
        public bool EnableSoundEffects { get; set; } = false; // For future implementation
        
        // Difficulty Settings
        public double EnemyHealthMultiplier { get; set; } = 1.0;
        public double EnemyDamageMultiplier { get; set; } = 1.0;
        public double PlayerHealthMultiplier { get; set; } = 1.0;
        public double PlayerDamageMultiplier { get; set; } = 1.0;
        
        // UI Settings
        public bool ShowHealthBars { get; set; } = true;
        public bool ShowDamageNumbers { get; set; } = true;
        public bool ShowComboProgress { get; set; } = true;
        
        private static readonly string SettingsFilePath = "gamesettings.json";
        private static GameSettings? _instance;
        
        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = LoadSettings();
                }
                return _instance;
            }
        }
        
        public GameSettings() { }
        
        public static GameSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<GameSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not load settings file: {ex.Message}");
            }
            
            // Return default settings if file doesn't exist or is invalid
            return new GameSettings();
        }
        
        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not save settings file: {ex.Message}");
            }
        }
        
        public void ResetToDefaults()
        {
            NarrativeBalance = 0.5;
            EnableNarrativeEvents = true;
            EnableInformationalSummaries = true;
            CombatSpeed = 1.0;
            ShowIndividualActionMessages = false;
            EnableComboSystem = true;
            EnableTextDisplayDelays = true;
            EnableAutoSave = true;
            AutoSaveInterval = 5;
            ShowDetailedStats = true;
            EnableSoundEffects = false;
            EnemyHealthMultiplier = 1.0;
            EnemyDamageMultiplier = 1.0;
            PlayerHealthMultiplier = 1.0;
            PlayerDamageMultiplier = 1.0;
            ShowHealthBars = true;
            ShowDamageNumbers = true;
            ShowComboProgress = true;
        }
        
        public string GetNarrativeBalanceDescription()
        {
            if (NarrativeBalance <= 0.2)
                return "Event-Driven (Minimal narrative, focus on facts)";
            else if (NarrativeBalance <= 0.4)
                return "Mostly Event-Driven (Some narrative for significant events)";
            else if (NarrativeBalance <= 0.6)
                return "Balanced (Equal mix of events and narrative)";
            else if (NarrativeBalance <= 0.8)
                return "Mostly Poetic (Rich narrative with some events)";
            else
                return "Poetic (Full narrative experience)";
        }
        
        public string GetCombatSpeedDescription()
        {
            if (CombatSpeed <= 0.5)
                return "Very Slow";
            else if (CombatSpeed <= 0.8)
                return "Slow";
            else if (CombatSpeed <= 1.2)
                return "Normal";
            else if (CombatSpeed <= 1.5)
                return "Fast";
            else
                return "Very Fast";
        }
        
        public string GetDifficultyDescription()
        {
            double avgMultiplier = (EnemyHealthMultiplier + EnemyDamageMultiplier) / 2.0;
            if (avgMultiplier <= 0.7)
                return "Easy";
            else if (avgMultiplier <= 0.9)
                return "Below Normal";
            else if (avgMultiplier <= 1.1)
                return "Normal";
            else if (avgMultiplier <= 1.3)
                return "Above Normal";
            else
                return "Hard";
        }
        
        public string GetTextDisplayDelayDescription()
        {
            return EnableTextDisplayDelays ? "Enabled (Delays match action length)" : "Disabled (Fast background calculations)";
        }
    }
} 