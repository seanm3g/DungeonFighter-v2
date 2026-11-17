using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Defines color preferences for a specific character
    /// </summary>
    public class CharacterColorProfile
    {
        public string CharacterName { get; set; } = "";
        public ColorPalette PrimaryColor { get; set; } = ColorPalette.White;
        public ColorPalette SecondaryColor { get; set; } = ColorPalette.Gray;
        public ColorPalette AccentColor { get; set; } = ColorPalette.Cyan;
        public ColorPalette DamageColor { get; set; } = ColorPalette.Damage;
        public ColorPalette HealingColor { get; set; } = ColorPalette.Healing;
        public ColorPalette CriticalColor { get; set; } = ColorPalette.Critical;
        public ColorPalette MissColor { get; set; } = ColorPalette.Miss;
        public ColorPalette BlockColor { get; set; } = ColorPalette.Block;
        public ColorPalette DodgeColor { get; set; } = ColorPalette.Dodge;
        
        // Custom pattern overrides
        private readonly Dictionary<string, ColorPalette> _customPatterns = new Dictionary<string, ColorPalette>();
        
        public CharacterColorProfile(string characterName)
        {
            CharacterName = characterName;
        }
        
        /// <summary>
        /// Gets the color for a specific pattern, using character-specific overrides if available
        /// </summary>
        public Color GetColorForPattern(string pattern)
        {
            // Check for character-specific override first
            if (_customPatterns.TryGetValue(pattern.ToLowerInvariant(), out var customColor))
            {
                return customColor.GetColor();
            }
            
            // Use character-specific colors for common patterns
            return pattern.ToLowerInvariant() switch
            {
                "damage" => DamageColor.GetColor(),
                "healing" => HealingColor.GetColor(),
                "critical" => CriticalColor.GetColor(),
                "miss" => MissColor.GetColor(),
                "block" => BlockColor.GetColor(),
                "dodge" => DodgeColor.GetColor(),
                "primary" => PrimaryColor.GetColor(),
                "secondary" => SecondaryColor.GetColor(),
                "accent" => AccentColor.GetColor(),
                _ => ColorPatterns.GetColorForPattern(pattern)
            };
        }

    }
    
    /// <summary>
    /// Manages color profiles for different characters
    /// </summary>
    public static class CharacterColorManager
    {
        private static readonly Dictionary<string, CharacterColorProfile> _profiles = new Dictionary<string, CharacterColorProfile>();
        private static CharacterColorProfile? _defaultProfile;
        
        /// <summary>
        /// Gets or creates a color profile for a character
        /// </summary>
        public static CharacterColorProfile GetProfile(string characterName)
        {
            if (string.IsNullOrEmpty(characterName))
                return GetDefaultProfile();
                
            if (!_profiles.TryGetValue(characterName, out var profile))
            {
                profile = CreateDefaultProfile(characterName);
                _profiles[characterName] = profile;
            }
            
            return profile;
        }

        /// <summary>
        /// Gets the default color profile
        /// </summary>
        public static CharacterColorProfile GetDefaultProfile()
        {
            if (_defaultProfile == null)
            {
                _defaultProfile = new CharacterColorProfile("Default");
            }
            return _defaultProfile;
        }

        /// <summary>
        /// Creates a default profile for a character
        /// </summary>
        private static CharacterColorProfile CreateDefaultProfile(string characterName)
        {
            var profile = new CharacterColorProfile(characterName);
            
            // Set character-specific defaults based on name or type
            if (characterName.Contains("Player", StringComparison.OrdinalIgnoreCase) || 
                characterName.Contains("Hero", StringComparison.OrdinalIgnoreCase))
            {
                profile.PrimaryColor = ColorPalette.Cyan;
                profile.SecondaryColor = ColorPalette.Blue;
                profile.AccentColor = ColorPalette.Gold;
            }
            else if (characterName.Contains("Enemy", StringComparison.OrdinalIgnoreCase) || 
                     characterName.Contains("Monster", StringComparison.OrdinalIgnoreCase))
            {
                profile.PrimaryColor = ColorPalette.Red;
                profile.SecondaryColor = ColorPalette.DarkRed;
                profile.AccentColor = ColorPalette.Orange;
            }
            else if (characterName.Contains("Boss", StringComparison.OrdinalIgnoreCase))
            {
                profile.PrimaryColor = ColorPalette.Purple;
                profile.SecondaryColor = ColorPalette.DarkMagenta;
                profile.AccentColor = ColorPalette.Gold;
            }
            else if (characterName.Contains("NPC", StringComparison.OrdinalIgnoreCase) || 
                     characterName.Contains("Merchant", StringComparison.OrdinalIgnoreCase))
            {
                profile.PrimaryColor = ColorPalette.Green;
                profile.SecondaryColor = ColorPalette.DarkGreen;
                profile.AccentColor = ColorPalette.Gold;
            }
            
            return profile;
        }
        
    }
}
