using System;
using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Manages location-specific taunt logic and generation for battle narratives.
    /// Encapsulates taunt generation, location detection, and taunt thresholds.
    /// </summary>
    public class TauntSystem
    {
        private readonly NarrativeTextProvider textProvider;

        public TauntSystem(NarrativeTextProvider textProvider)
        {
            this.textProvider = textProvider;
        }

        /// <summary>
        /// Determines the location type from the environment name
        /// </summary>
        public string GetLocationType(string environmentName)
        {
            if (string.IsNullOrEmpty(environmentName))
                return "generic";

            string lowerEnv = environmentName.ToLower();

            if (lowerEnv.Contains("library") || lowerEnv.Contains("study") || lowerEnv.Contains("archive"))
                return "library";
            else if (lowerEnv.Contains("water") || lowerEnv.Contains("ocean") || lowerEnv.Contains("sea") || lowerEnv.Contains("underwater"))
                return "underwater";
            else if (lowerEnv.Contains("lava") || lowerEnv.Contains("volcano") || lowerEnv.Contains("volcanic")
                     || lowerEnv.Contains("magma") || lowerEnv.Contains("molten") || lowerEnv.Contains("fire"))
                return "lava";
            else if (lowerEnv.Contains("crypt") || lowerEnv.Contains("tomb") || lowerEnv.Contains("grave"))
                return "crypt";
            else if (lowerEnv.Contains("crystal") || lowerEnv.Contains("geode"))
                return "crystal";
            else if (lowerEnv.Contains("temple") || lowerEnv.Contains("shrine") || lowerEnv.Contains("altar"))
                return "temple";
            else if (lowerEnv.Contains("forest") || lowerEnv.Contains("grove"))
                return "forest";
            else if (lowerEnv.Contains("ice") || lowerEnv.Contains("frozen") || lowerEnv.Contains("frost")
                     || lowerEnv.Contains("glacier") || lowerEnv.Contains("glacial"))
                return "ice";
            else if (lowerEnv.Contains("swamp") || lowerEnv.Contains("marsh") || lowerEnv.Contains("bog"))
                return "swamp";
            else
                return "generic";
        }

        /// <summary>
        /// Gets location-specific taunt text based on the current environment
        /// </summary>
        public string GetLocationSpecificTaunt(string taunterType, string taunterName, string targetName, string currentLocation)
        {
            // Determine location type from current location
            string locationType = GetLocationType(currentLocation);

            // Get location-specific taunt
            string tauntKey = $"{taunterType}Taunt_{locationType}";
            string taunt = textProvider.GetRandomNarrative(tauntKey);

            // If no location-specific taunt exists, fall back to generic
            if (taunt == textProvider.GetFallbackNarrative(tauntKey))
            {
                tauntKey = $"{taunterType}Taunt";
                taunt = textProvider.GetRandomNarrative(tauntKey);
            }

            // Replace placeholders
            taunt = taunt.Replace("{name}", taunterName);
            if (taunterType == "player")
            {
                taunt = taunt.Replace("{enemy}", targetName);
            }
            else
            {
                taunt = taunt.Replace("{player}", targetName);
            }

            return taunt;
        }

        /// <summary>
        /// Gets the action threshold for player taunt at a given taunt count
        /// </summary>
        public int GetPlayerTauntThreshold(int tauntCount, GameSettings settings)
        {
            return tauntCount switch
            {
                0 => 8 + (int)(settings.NarrativeBalance * 4),  // First taunt after 8-12 actions
                1 => 15 + (int)(settings.NarrativeBalance * 5), // Second taunt after 15-20 actions
                _ => int.MaxValue // No more taunts
            };
        }

        /// <summary>
        /// Gets the action threshold for enemy taunt at a given taunt count
        /// </summary>
        public int GetEnemyTauntThreshold(int tauntCount, GameSettings settings)
        {
            return tauntCount switch
            {
                0 => 6 + (int)(settings.NarrativeBalance * 4),   // First taunt after 6-10 actions
                1 => 12 + (int)(settings.NarrativeBalance * 6),  // Second taunt after 12-18 actions
                _ => int.MaxValue // No more taunts
            };
        }

        /// <summary>
        /// Checks if a player taunt should trigger and returns the taunt text if so
        /// </summary>
        public (bool shouldTaunt, string tauntText) CheckPlayerTaunt(int playerActionCount, int playerTauntCount, string playerName, string enemyName, string currentLocation, GameSettings settings)
        {
            if (playerTauntCount >= 2)
                return (false, "");

            int threshold = GetPlayerTauntThreshold(playerTauntCount, settings);
            if (playerActionCount >= threshold)
            {
                string taunt = GetLocationSpecificTaunt("player", playerName, enemyName, currentLocation);
                return (true, taunt);
            }

            return (false, "");
        }

        /// <summary>
        /// Checks if an enemy taunt should trigger and returns the taunt text if so
        /// </summary>
        public (bool shouldTaunt, string tauntText) CheckEnemyTaunt(
            int enemyActionCount,
            int enemyTauntCount,
            string enemyName,
            string playerName,
            string currentLocation,
            GameSettings settings,
            string? creatureTier = null)
        {
            if (enemyTauntCount >= 2)
                return (false, "");

            int threshold = GetEnemyTauntThreshold(enemyTauntCount, settings);
            if (enemyActionCount >= threshold)
            {
                if (CreatureTierIds.TryCanonicalize(creatureTier, out var tier)
                    && textProvider.TryGetBankNarrative(CreatureTierIds.SuffixedBank("enemyTaunt", tier), out var tierTaunt))
                {
                    string filled = ApplyTauntPlaceholders(tierTaunt, "enemy", enemyName, playerName);
                    return (true, filled);
                }

                string taunt = GetLocationSpecificTaunt("enemy", enemyName, playerName, currentLocation);
                return (true, taunt);
            }

            return (false, "");
        }

        private static string ApplyTauntPlaceholders(string taunt, string taunterType, string taunterName, string targetName)
        {
            taunt = taunt.Replace("{name}", taunterName);
            if (taunterType == "player")
                return taunt.Replace("{enemy}", targetName);
            return taunt.Replace("{player}", targetName);
        }
    }
}

