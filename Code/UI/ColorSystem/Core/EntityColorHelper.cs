using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame;
using RPGGame.Data;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.Avalonia;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Helper class for getting entity colors with override support
    /// </summary>
    public static class EntityColorHelper
    {
        /// <summary>
        /// Gets the color for an enemy, checking for color override first, then name-based mapping, then falling back to defaults
        /// </summary>
        public static Color GetEnemyColor(Enemy enemy)
        {
            if (enemy == null)
                return ColorPalette.White.GetColor();
            
            // Check for color override first (highest priority)
            var colorOverride = GetColorOverride(enemy);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                {
                    return overrideColor.Value;
                }
            }
            
            return GetEnemyColorByName(enemy.Name);
        }
        
        /// <summary>
        /// Gets a color for an enemy based on its name (string version, for cases where Enemy object is not available)
        /// Maps each enemy type to a thematically appropriate color
        /// </summary>
        public static Color GetEnemyColorByName(string enemyName)
        {
            var shaded = AnimalEnemyNameColoredText.TryBuildSegments(enemyName);
            if (shaded != null && shaded.Count > 0)
                return shaded[0].Color;

            var color = GetEnemyColorByNameInternal(enemyName);
            if (color.HasValue)
                return color.Value;

            return ColorPalette.Enemy.GetColor();
        }

        /// <summary>
        /// Right-panel LOCATION line: same coloring as the combat log but uses the ellipsized <paramref name="lineText"/>
        /// so the glyph count matches the monospace row (creature keywords re-evaluated on the truncated string).
        /// </summary>
        public static List<ColoredText> BuildEnemyNamePanelLineSegments(Enemy enemy, string lineText)
        {
            if (enemy == null)
                return new List<ColoredText>();

            var colorOverride = GetColorOverride(enemy);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                    return new List<ColoredText> { new ColoredText(lineText, overrideColor.Value) };
            }

            var animal = AnimalEnemyNameColoredText.TryBuildSegments(lineText);
            if (animal != null)
                return animal;

            return new List<ColoredText> { new ColoredText(lineText, GetEnemyColor(enemy)) };
        }

        /// <summary>
        /// Enemy name for HUD / combat log: multi-shade creature keywords, otherwise one segment using catalog colors.
        /// </summary>
        public static List<ColoredText> BuildEnemyNameDisplaySegments(Enemy enemy)
        {
            if (enemy == null)
                return new List<ColoredText>();

            var colorOverride = GetColorOverride(enemy);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                    return new List<ColoredText> { new ColoredText(enemy.Name, overrideColor.Value) };
            }

            var animal = AnimalEnemyNameColoredText.TryBuildSegments(enemy.Name);
            if (animal != null)
                return animal;

            return new List<ColoredText> { new ColoredText(enemy.Name, GetEnemyColorByName(enemy.Name)) };
        }

        /// <summary>Appends enemy display name segments (creature shading when applicable).</summary>
        public static void AppendEnemyNameColored(ColoredTextBuilder builder, Enemy enemy)
        {
            builder.AddRange(BuildEnemyNameDisplaySegments(enemy));
        }

        /// <summary>Creature shading when the string matches keywords; otherwise solid enemy color.</summary>
        public static void AppendEnemyNameByString(ColoredTextBuilder builder, string enemyName)
        {
            var shaded = AnimalEnemyNameColoredText.TryBuildSegments(enemyName);
            if (shaded != null)
                builder.AddRange(shaded);
            else
                builder.Add(enemyName, GetEnemyColorByName(enemyName));
        }

        /// <summary>Uses shaded creature names for enemies; other actors use existing actor color rules.</summary>
        public static void AppendActorNameColored(ColoredTextBuilder builder, Actor actor)
        {
            if (actor is Enemy enemy)
                AppendEnemyNameColored(builder, enemy);
            else
                builder.Add(actor.Name, GetActorColor(actor));
        }
        
        /// <summary>
        /// Gets a color for an enemy based on its name
        /// Maps each enemy type to a thematically appropriate color
        /// </summary>
        private static Color? GetEnemyColorByNameInternal(string enemyName)
        {
            if (string.IsNullOrEmpty(enemyName))
                return null;
            
            // Normalize the name for comparison (case-insensitive, trim whitespace)
            var normalizedName = enemyName.Trim();
            
            // Map enemy names to appropriate colors
            return normalizedName.ToLowerInvariant() switch
            {
                "goblin" => ColorPalette.Green.GetColor(),           // Classic green goblin
                "fire elemental" => ColorPalette.Red.GetColor(),     // Red for fire
                "lava golem" => ColorPalette.Orange.GetColor(),      // Orange for lava
                "skeleton" => ColorPalette.LightGray.GetColor(),     // Light gray/white for bones
                "zombie" => ColorPalette.DarkGreen.GetColor(),       // Dark green for rotting undead
                "wraith" => ColorPalette.Purple.GetColor(),          // Purple for spectral being
                _ => null  // Unknown enemy, return null to fall back to default
            };
        }
        
        /// <summary>
        /// Gets the color for an actor (Character or Enemy), using enemy-specific colors for enemies
        /// For characters: gold for default (Fighter) display, class color for leveled classes
        /// </summary>
        public static Color GetActorColor(Actor actor)
        {
            if (actor == null)
                return ColorPalette.White.GetColor();
            
            if (actor is Enemy enemy)
            {
                return GetEnemyColor(enemy);
            }
            
            // For characters, check their class and use appropriate color
            if (actor is Character character)
            {
                return GetCharacterColor(character);
            }
            
            // Fallback to player color for other actor types
            return ColorPalette.Player.GetColor();
        }
        
        /// <summary>
        /// Gets the color for a character based on their class
        /// Gold for default Fighter display, class-specific color for leveled classes
        /// </summary>
        private static Color GetCharacterColor(Character character)
        {
            if (character == null)
                return ColorPalette.White.GetColor();

            var cfg = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            string currentClass = character.GetCurrentClass();
            if (string.Equals(currentClass, cfg.DefaultNoPointsClassName, StringComparison.OrdinalIgnoreCase))
                return ColorPalette.Gold.GetColor();

            var wt = character.Progression.GetPrimaryClassWeaponType();
            if (wt == null)
                return ColorPalette.Gold.GetColor();
            return GetWeaponPathColor(wt.Value);
        }
        
        /// <summary>
        /// Path color for the character's primary weapon class (Mace/Sword/Dagger/Wand).
        /// Character panel uses <see cref="Character.Progression"/>'s primary path, not parsed display strings.
        /// </summary>
        private static Color GetWeaponPathColor(WeaponType weaponType) => weaponType switch
        {
            WeaponType.Mace => ColorConfigurationLoader.GetColorCode("C"),
            WeaponType.Sword => ColorConfigurationLoader.GetColorCode("W"),
            WeaponType.Dagger => ColorConfigurationLoader.GetColorCode("M"),
            WeaponType.Wand => ColorConfigurationLoader.GetColorCode("m"),
            _ => ColorPalette.White.GetColor()
        };

        
        /// <summary>
        /// Gets the color for a class name (public method for use in UI rendering).
        /// Default Fighter display uses gold; leveled classes use weapon-path colors.
        /// </summary>
        public static Color GetClassColorForDisplay(Character character)
        {
            if (character == null)
                return ColorPalette.White.GetColor();

            var cfg = GameConfiguration.Instance.ClassPresentation.EnsureNormalized();
            string currentClass = character.GetCurrentClass();
            if (string.Equals(currentClass, cfg.DefaultNoPointsClassName, StringComparison.OrdinalIgnoreCase))
                return ColorPalette.Gold.GetColor();

            var wt = character.Progression.GetPrimaryClassWeaponType();
            if (wt == null)
                return ColorPalette.Gold.GetColor();
            return GetWeaponPathColor(wt.Value);
        }
        
        /// <summary>
        /// Gets the color for a dungeon, checking for color override first, then falling back to theme color
        /// </summary>
        public static Color GetDungeonColor(Dungeon dungeon)
        {
            if (dungeon == null)
                return ColorPalette.White.GetColor();
            
            // Check for color override
            var colorOverride = GetDungeonColorOverride(dungeon);
            if (colorOverride != null)
            {
                var overrideColor = ResolveColorOverride(colorOverride);
                if (overrideColor.HasValue)
                {
                    return overrideColor.Value;
                }
            }
            
            // Fallback to theme color
            return DungeonThemeColors.GetThemeColor(dungeon.Theme);
        }
        
        /// <summary>
        /// Gets the color override from an enemy using reflection
        /// </summary>
        private static ColorOverride? GetColorOverride(Enemy enemy)
        {
            var colorOverrideProperty = typeof(Enemy).GetProperty("ColorOverride", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (colorOverrideProperty != null)
            {
                return colorOverrideProperty.GetValue(enemy) as ColorOverride;
            }
            return null;
        }
        
        /// <summary>
        /// Gets the color override from a dungeon
        /// </summary>
        private static ColorOverride? GetDungeonColorOverride(Dungeon dungeon)
        {
            return dungeon.ColorOverride;
        }
        
        /// <summary>
        /// Resolves a color override to an actual Color value
        /// </summary>
        private static Color? ResolveColorOverride(ColorOverride? colorOverride)
        {
            if (colorOverride == null || string.IsNullOrEmpty(colorOverride.Type))
                return null;
            
            var type = colorOverride.Type.ToLowerInvariant();
            
            switch (type)
            {
                case "template":
                    // Template name - return first color from template
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        var template = ColorConfigurationLoader.GetTemplate(colorOverride.Value);
                        if (template != null && template.Colors != null && template.Colors.Count > 0)
                        {
                            return ColorCodeLoader.GetColor(template.Colors[0]);
                        }
                    }
                    break;
                    
                case "palette":
                    // Palette name - return palette color
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        if (Enum.TryParse<ColorPalette>(colorOverride.Value, true, out var palette))
                        {
                            return palette.GetColor();
                        }
                    }
                    break;
                    
                case "colorcode":
                case "color_code":
                    // Color code - return color code color
                    if (!string.IsNullOrEmpty(colorOverride.Value))
                    {
                        return ColorCodeLoader.GetColor(colorOverride.Value);
                    }
                    break;
                    
                case "rgb":
                    // Direct RGB values
                    if (colorOverride.Rgb != null && colorOverride.Rgb.Length == 3)
                    {
                        int r = Math.Clamp(colorOverride.Rgb[0], 0, 255);
                        int g = Math.Clamp(colorOverride.Rgb[1], 0, 255);
                        int b = Math.Clamp(colorOverride.Rgb[2], 0, 255);
                        return Color.FromRgb((byte)r, (byte)g, (byte)b);
                    }
                    break;
            }
            
            return null;
        }
    }
}

