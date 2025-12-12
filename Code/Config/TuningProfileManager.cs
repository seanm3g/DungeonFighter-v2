using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using RPGGame.Utils;

namespace RPGGame.Config
{
    /// <summary>
    /// Manages local tuning profiles (not for sharing)
    /// </summary>
    public class TuningProfileManager
    {
        private static readonly string ProfilesDirectory = Path.Combine("GameData", "TuningProfiles");

        /// <summary>
        /// Profile metadata
        /// </summary>
        public class ProfileMetadata
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string CreatedDate { get; set; } = "";
            public string LastModified { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        /// <summary>
        /// Complete profile structure
        /// </summary>
        public class TuningProfile
        {
            public ProfileMetadata Metadata { get; set; } = new();
            public GameConfiguration TuningConfig { get; set; } = new();
        }

        /// <summary>
        /// Ensure profiles directory exists
        /// </summary>
        private static void EnsureProfilesDirectory()
        {
            if (!Directory.Exists(ProfilesDirectory))
            {
                Directory.CreateDirectory(ProfilesDirectory);
            }
        }

        /// <summary>
        /// Create profile from current configuration
        /// </summary>
        public static TuningProfile CreateProfile(string name, string description = "", string notes = "")
        {
            var profile = new TuningProfile
            {
                Metadata = new ProfileMetadata
                {
                    Name = name,
                    Description = description,
                    CreatedDate = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    LastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                    Notes = notes
                },
                TuningConfig = GameConfiguration.Instance
            };

            return profile;
        }

        /// <summary>
        /// Save profile to profiles directory
        /// </summary>
        public static bool SaveProfile(TuningProfile profile)
        {
            try
            {
                EnsureProfilesDirectory();
                
                string fileName = SanitizeFileName(profile.Metadata.Name) + ".json";
                string filePath = Path.Combine(ProfilesDirectory, fileName);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(profile, options);
                File.WriteAllText(filePath, json);

                ScrollDebugLogger.Log($"TuningProfileManager: Saved profile '{profile.Metadata.Name}' to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningProfileManager: Error saving profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load profile from name
        /// </summary>
        public static TuningProfile? LoadProfile(string profileName)
        {
            try
            {
                EnsureProfilesDirectory();
                string fileName = SanitizeFileName(profileName) + ".json";
                string filePath = Path.Combine(ProfilesDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    ScrollDebugLogger.Log($"TuningProfileManager: Profile file not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var profile = JsonSerializer.Deserialize<TuningProfile>(json, options);
                
                if (profile != null)
                {
                    ScrollDebugLogger.Log($"TuningProfileManager: Loaded profile '{profile.Metadata.Name}' from {filePath}");
                }

                return profile;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningProfileManager: Error loading profile: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Apply profile to current game configuration
        /// </summary>
        public static bool ApplyProfile(TuningProfile profile)
        {
            try
            {
                if (profile.TuningConfig == null)
                {
                    ScrollDebugLogger.Log($"TuningProfileManager: Profile has no tuning configuration");
                    return false;
                }

                // Apply the tuning config from profile
                var config = GameConfiguration.Instance;
                var profileConfig = profile.TuningConfig;

                // Copy all configuration sections
                config.Character = profileConfig.Character;
                config.Attributes = profileConfig.Attributes;
                config.Combat = profileConfig.Combat;
                config.CombatBalance = profileConfig.CombatBalance;
                config.RollSystem = profileConfig.RollSystem;
                config.EnemySystem = profileConfig.EnemySystem;
                config.WeaponScaling = profileConfig.WeaponScaling;
                config.EquipmentScaling = profileConfig.EquipmentScaling;
                config.Progression = profileConfig.Progression;
                config.StatusEffects = profileConfig.StatusEffects;
                config.ComboSystem = profileConfig.ComboSystem;
                config.LootSystem = profileConfig.LootSystem;
                config.DungeonScaling = profileConfig.DungeonScaling;
                config.DungeonGeneration = profileConfig.DungeonGeneration;
                config.ModificationRarity = profileConfig.ModificationRarity;
                config.GameSpeed = profileConfig.GameSpeed;
                config.GameData = profileConfig.GameData;
                config.Debug = profileConfig.Debug;
                config.BalanceAnalysis = profileConfig.BalanceAnalysis;
                config.BalanceValidation = profileConfig.BalanceValidation;
                config.DifficultySettings = profileConfig.DifficultySettings;
                config.UICustomization = profileConfig.UICustomization;

                // Save to file
                config.SaveToFile();

                ScrollDebugLogger.Log($"TuningProfileManager: Applied profile '{profile.Metadata.Name}'");
                return true;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningProfileManager: Error applying profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List all available profiles
        /// </summary>
        public static List<TuningProfile> ListProfiles()
        {
            var profiles = new List<TuningProfile>();
            
            try
            {
                EnsureProfilesDirectory();

                if (!Directory.Exists(ProfilesDirectory))
                {
                    return profiles;
                }

                var files = Directory.GetFiles(ProfilesDirectory, "*.json");
                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        var profile = JsonSerializer.Deserialize<TuningProfile>(json, options);
                        if (profile != null)
                        {
                            profiles.Add(profile);
                        }
                    }
                    catch
                    {
                        // Skip invalid files
                    }
                }
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningProfileManager: Error listing profiles: {ex.Message}");
            }

            return profiles;
        }

        /// <summary>
        /// Delete profile
        /// </summary>
        public static bool DeleteProfile(string profileName)
        {
            try
            {
                EnsureProfilesDirectory();
                string fileName = SanitizeFileName(profileName) + ".json";
                string filePath = Path.Combine(ProfilesDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    ScrollDebugLogger.Log($"TuningProfileManager: Deleted profile '{profileName}'");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ScrollDebugLogger.Log($"TuningProfileManager: Error deleting profile: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sanitize file name
        /// </summary>
        private static string SanitizeFileName(string name)
        {
            string sanitized = name.ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_");
            
            // Remove special characters
            sanitized = new string(sanitized.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
            
            return sanitized;
        }
    }
}

