using System;
using System.IO;
using System.Text.Json;

namespace RPGGame
{
    public class GameSettings
    {
        // Narrative Settings
        public double NarrativeBalance { get; set; } = 0.8; // 0.0 = Event-driven, 1.0 = Poetic narrative
        public bool EnableNarrativeEvents { get; set; } = true;
        public bool EnableInformationalSummaries { get; set; } = true;
        
        // Combat Settings
        public double CombatSpeed { get; set; } = 1.0; // 0.5 = Slow, 2.0 = Fast
        public bool ShowIndividualActionMessages { get; set; } = false;
        public bool EnableComboSystem { get; set; } = true;
        public bool EnableTextDisplayDelays { get; set; } = true; // Only apply delays when text is displayed
        public bool FastCombat { get; set; } = false; // When enabled, sets combat delay to zero
        
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
        
        // Appearance/Color Settings
        public string PanelBackgroundColor { get; set; } = "#FFFFFF"; // White
        public string PanelBorderColor { get; set; } = "#404040"; // Dark gray
        public string PanelTextColor { get; set; } = "#000000"; // Black
        public string SettingsBackgroundColor { get; set; } = "#1A1A1A"; // Dark gray
        public string SettingsTitleColor { get; set; } = "#FFD700"; // Gold
        public string ListBoxSelectedColor { get; set; } = "#FFD700"; // Gold
        public string ListBoxSelectedBackgroundColor { get; set; } = "#2A2A2A"; // Dark gray
        public string ListBoxHoverBackgroundColor { get; set; } = "#353535"; // Medium gray
        public string ButtonPrimaryColor { get; set; } = "#0078D4"; // Blue
        public string ButtonSecondaryColor { get; set; } = "#555555"; // Gray
        public string ButtonBackColor { get; set; } = "#404040"; // Dark gray
        public string TextBoxTextColor { get; set; } = "#FFFFFF"; // White
        public string TextBoxBackgroundColor { get; set; } = "#2A2A2A"; // Dark gray
        public string TextBoxHoverBackgroundColor { get; set; } = "#353535"; // Medium gray
        public string TextBoxBorderColor { get; set; } = "#555555"; // Gray
        public string TextBoxFocusBorderColor { get; set; } = "#0078D4"; // Blue
        
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
                    if (string.IsNullOrEmpty(json))
                    {
                        return new GameSettings();
                    }
                    var settings = JsonSerializer.Deserialize<GameSettings>(json);
                    if (settings != null)
                    {
                        // Validate loaded settings and fix any invalid values
                        settings.ValidateAndFix();
                        return settings;
                    }
                }
            }
            catch (JsonException ex)
            {
                // Corrupted JSON file - backup and create new
                ErrorHandler.LogError(ex, "GameSettings.LoadSettings", "Settings file is corrupted");
                try
                {
                    string backupPath = SettingsFilePath + ".corrupted." + DateTime.Now.ToString("yyyyMMddHHmmss");
                    if (File.Exists(SettingsFilePath))
                    {
                        File.Copy(SettingsFilePath, backupPath, true);
                        File.Delete(SettingsFilePath);
                    }
                }
                catch { /* Ignore backup errors */ }
            }
            catch (Exception ex)
            {
                ErrorHandler.LogError(ex, "GameSettings.LoadSettings", "Could not load settings file");
            }
            
            // Return default settings if file doesn't exist or is invalid
            return new GameSettings();
        }
        
        /// <summary>
        /// Validates all settings values and fixes any that are out of range
        /// </summary>
        public void ValidateAndFix()
        {
            // Narrative Settings
            NarrativeBalance = Math.Clamp(NarrativeBalance, 0.0, 1.0);
            
            // Combat Settings
            CombatSpeed = Math.Clamp(CombatSpeed, 0.5, 2.0);
            
            // Difficulty Settings
            EnemyHealthMultiplier = Math.Clamp(EnemyHealthMultiplier, 0.5, 3.0);
            EnemyDamageMultiplier = Math.Clamp(EnemyDamageMultiplier, 0.5, 3.0);
            PlayerHealthMultiplier = Math.Clamp(PlayerHealthMultiplier, 0.5, 3.0);
            PlayerDamageMultiplier = Math.Clamp(PlayerDamageMultiplier, 0.5, 3.0);
            
            // Gameplay Settings
            AutoSaveInterval = Math.Max(1, AutoSaveInterval);
        }
        
        public void SaveSettings()
        {
            // Validate settings before saving
            ValidateAndFix();
            
            ErrorHandler.TrySaveJson(() =>
            {
                // Write to temporary file first, then replace (atomic operation)
                string tempPath = SettingsFilePath + ".tmp";
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(tempPath, json);
                
                // Replace original file atomically
                if (File.Exists(SettingsFilePath))
                {
                    File.Replace(tempPath, SettingsFilePath, SettingsFilePath + ".bak");
                }
                else
                {
                    File.Move(tempPath, SettingsFilePath);
                }
            }, "GameSettings.json");
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
            FastCombat = false;
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
            PanelBackgroundColor = "#FFFFFF";
            PanelBorderColor = "#404040";
            PanelTextColor = "#000000";
            SettingsBackgroundColor = "#1A1A1A";
            SettingsTitleColor = "#FFD700";
            ListBoxSelectedColor = "#FFD700";
            ListBoxSelectedBackgroundColor = "#2A2A2A";
            ListBoxHoverBackgroundColor = "#353535";
            ButtonPrimaryColor = "#0078D4";
            ButtonSecondaryColor = "#555555";
            ButtonBackColor = "#404040";
            TextBoxTextColor = "#FFFFFF";
            TextBoxBackgroundColor = "#2A2A2A";
            TextBoxHoverBackgroundColor = "#353535";
            TextBoxBorderColor = "#555555";
            TextBoxFocusBorderColor = "#0078D4";
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
        
        public string GetFastCombatDescription()
        {
            return FastCombat ? "Enabled (Zero combat delays)" : "Disabled (Normal combat delays)";
        }
    }
} 