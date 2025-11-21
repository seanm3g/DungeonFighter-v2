using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace RPGGame.UI.ColorSystem
{
    /// <summary>
    /// Library of color templates for different themes and effects
    /// </summary>
    public static class ColorTemplateLibrary
    {
        /// <summary>
        /// Creates fiery-themed colored text
        /// </summary>
        public static List<ColoredText> Fiery(string text)
        {
            var colors = new[] { ColorPalette.Orange.GetColor(), ColorPalette.Red.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates icy-themed colored text
        /// </summary>
        public static List<ColoredText> Icy(string text)
        {
            var colors = new[] { ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.White.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates toxic-themed colored text
        /// </summary>
        public static List<ColoredText> Toxic(string text)
        {
            var colors = new[] { ColorPalette.Green.GetColor(), ColorPalette.Lime.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates crystalline-themed colored text
        /// </summary>
        public static List<ColoredText> Crystalline(string text)
        {
            var colors = new[] { ColorPalette.Silver.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates golden-themed colored text
        /// </summary>
        public static List<ColoredText> Golden(string text)
        {
            var colors = new[] { ColorPalette.Gold.GetColor(), ColorPalette.Yellow.GetColor(), ColorPalette.Orange.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates holy-themed colored text
        /// </summary>
        public static List<ColoredText> Holy(string text)
        {
            var colors = new[] { ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.Yellow.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates shadow-themed colored text
        /// Uses dark but visible colors (no pure black)
        /// </summary>
        public static List<ColoredText> Shadow(string text)
        {
            // Use dark purple/gray instead of black to ensure visibility
            var colors = new[] { 
                ColorValidator.EnsureVisible(ColorPalette.DarkGray.GetColor()), 
                ColorValidator.EnsureVisible(ColorPalette.DarkMagenta.GetColor()), 
                ColorPalette.Purple.GetColor() 
            };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates electric-themed colored text
        /// </summary>
        public static List<ColoredText> Electric(string text)
        {
            var colors = new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Blue.GetColor() };
            return CreateMultiColorText(text, colors);
        }
        
        /// <summary>
        /// Creates multi-color text by alternating colors for each character
        /// </summary>
        private static List<ColoredText> CreateMultiColorText(string text, Color[] colors)
        {
            if (string.IsNullOrEmpty(text))
                return new List<ColoredText>();
            
            var result = new List<ColoredText>();
            var colorIndex = 0;
            
            foreach (char c in text)
            {
                if (char.IsWhiteSpace(c))
                {
                    // Keep whitespace in default color
                    result.Add(new ColoredText(c.ToString(), Colors.White));
                }
                else
                {
                    // Use next color in sequence
                    var color = colors[colorIndex % colors.Length];
                    result.Add(new ColoredText(c.ToString(), color));
                    colorIndex++;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Creates a template with a single color
        /// </summary>
        public static List<ColoredText> SingleColor(string text, Color color)
        {
            return new List<ColoredText> { new ColoredText(text, color) };
        }
        
        /// <summary>
        /// Creates a template with a color from the palette
        /// </summary>
        public static List<ColoredText> PaletteColor(string text, ColorPalette palette)
        {
            return new List<ColoredText> { new ColoredText(text, palette.GetColor()) };
        }
        
        /// <summary>
        /// Creates rarity-themed colored text
        /// </summary>
        public static List<ColoredText> Common(string text)
        {
            return SingleColor(text, Colors.White);
        }
        
        public static List<ColoredText> Uncommon(string text)
        {
            return SingleColor(text, ColorPalette.Green.GetColor());
        }
        
        public static List<ColoredText> Rare(string text)
        {
            return SingleColor(text, ColorPalette.Blue.GetColor());
        }
        
        public static List<ColoredText> Epic(string text)
        {
            return SingleColor(text, ColorPalette.Purple.GetColor());
        }
        
        public static List<ColoredText> Legendary(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Orange.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.Orange.GetColor() });
        }
        
        public static List<ColoredText> Mythic(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Purple.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.Purple.GetColor() });
        }
        
        public static List<ColoredText> Transcendent(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), ColorPalette.Purple.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.Purple.GetColor(), ColorPalette.White.GetColor() });
        }
        
        /// <summary>
        /// Creates item type themed colored text
        /// </summary>
        public static List<ColoredText> WeaponClass(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Red.GetColor(), ColorPalette.Orange.GetColor(), ColorPalette.Yellow.GetColor() });
        }
        
        public static List<ColoredText> HeadArmor(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.White.GetColor() });
        }
        
        public static List<ColoredText> ChestArmor(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Blue.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() });
        }
        
        public static List<ColoredText> FeetArmor(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Green.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() });
        }
        
        /// <summary>
        /// Creates weapon type themed colored text
        /// </summary>
        public static List<ColoredText> SwordWeapon(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor() });
        }
        
        public static List<ColoredText> DaggerWeapon(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor() });
        }
        
        public static List<ColoredText> MaceWeapon(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Orange.GetColor(), ColorPalette.Red.GetColor(), ColorPalette.Orange.GetColor() });
        }
        
        public static List<ColoredText> WandWeapon(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Purple.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Purple.GetColor() });
        }
        
        /// <summary>
        /// Creates natural-themed colored text
        /// </summary>
        public static List<ColoredText> Natural(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Green.GetColor(), ColorPalette.Lime.GetColor(), ColorPalette.Brown.GetColor(), ColorPalette.Lime.GetColor(), ColorPalette.Green.GetColor() });
        }
        
        /// <summary>
        /// Creates arcane-themed colored text
        /// </summary>
        public static List<ColoredText> Arcane(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Purple.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Purple.GetColor() });
        }
        
        /// <summary>
        /// Creates critical hit themed colored text (red-orange-yellow sequence)
        /// </summary>
        public static List<ColoredText> Critical(string text)
        {
            return CreateMultiColorText(text, new[] { ColorPalette.Red.GetColor(), ColorPalette.Orange.GetColor(), ColorPalette.Yellow.GetColor(), ColorPalette.Orange.GetColor(), ColorPalette.Red.GetColor() });
        }
        
        /// <summary>
        /// Creates miss themed colored text (yellow/gold)
        /// </summary>
        public static List<ColoredText> Miss(string text)
        {
            return SingleColor(text, ColorPalette.Gold.GetColor());
        }
        
        /// <summary>
        /// Converts color code strings to Color array
        /// </summary>
        private static Color[] ConvertColorCodesToColors(string[] colorCodes)
        {
            return colorCodes.Select(code => ConvertColorCodeToColor(code)).ToArray();
        }
        
        /// <summary>
        /// Converts a single color code to a Color
        /// Ensures the color is visible on black background
        /// </summary>
        private static Color ConvertColorCodeToColor(string colorCode)
        {
            if (string.IsNullOrEmpty(colorCode))
                return Colors.White;

            Color color = colorCode switch
            {
                "R" => ColorPalette.Red.GetColor(),
                "r" => ColorPalette.DarkRed.GetColor(),
                "G" => ColorPalette.Green.GetColor(),
                "g" => ColorPalette.DarkGreen.GetColor(),
                "B" => ColorPalette.Blue.GetColor(),
                "b" => ColorPalette.DarkBlue.GetColor(),
                "C" => ColorPalette.Cyan.GetColor(),
                "c" => ColorPalette.DarkCyan.GetColor(),
                "M" => ColorPalette.Magenta.GetColor(),
                "m" => ColorPalette.DarkMagenta.GetColor(),
                "O" => ColorPalette.Orange.GetColor(),
                "o" => Color.FromRgb(200, 100, 0), // Dark orange
                "W" => ColorPalette.Gold.GetColor(),
                "w" => ColorPalette.Brown.GetColor(),
                "Y" => Colors.White,
                "y" => ColorPalette.Gray.GetColor(),
                "K" => ColorPalette.DarkGray.GetColor(),
                "k" => Colors.Black, // Will be lightened by ColorValidator
                _ => Colors.White
            };
            
            // Ensure color is visible on black background
            return ColorValidator.EnsureVisible(color);
        }
        
        /// <summary>
        /// Checks if a template name exists in the library
        /// </summary>
        public static bool HasTemplate(string templateName)
        {
            return templateName.ToLower() switch
            {
                "fiery" => true,
                "icy" => true,
                "toxic" => true,
                "crystalline" => true,
                "golden" => true,
                "holy" => true,
                "shadow" => true,
                "electric" => true,
                "common" => true,
                "uncommon" => true,
                "rare" => true,
                "epic" => true,
                "legendary" => true,
                "mythic" => true,
                "transcendent" => true,
                "weapon_class" => true,
                "head_armor" => true,
                "chest_armor" => true,
                "feet_armor" => true,
                "sword_weapon" => true,
                "dagger_weapon" => true,
                "mace_weapon" => true,
                "wand_weapon" => true,
                "natural" => true,
                "arcane" => true,
                // Combat templates
                "critical" => true,
                "miss" => true,
                // Modification templates
                "worn" => true,
                "dull" => true,
                "sturdy" => true,
                "balanced" => true,
                "sharp" => true,
                "swift" => true,
                "precise" => true,
                "reinforced" => true,
                "keen" => true,
                "agile" => true,
                "lucky" => true,
                "vampiric" => true,
                "brutal" => true,
                "lightning" => true,
                "blessed" => true,
                "venomous" => true,
                "masterwork" => true,
                "godlike" => true,
                "enchanted" => true,
                "annihilation" => true,
                "timewarp" => true,
                "perfect" => true,
                "divine" => true,
                "realitybreaker" => true,
                "omnipotent" => true,
                "infinite" => true,
                "cosmic" => true,
                // Dungeon theme templates
                "forest" => true,
                "lava" => true,
                "crypt" => true,
                "crystal" => true,
                "temple" => true,
                "ice" => true,
                "steampunk" => true,
                "swamp" => true,
                "astral" => true,
                "underground" => true,
                "storm" => true,
                "nature" => true,
                "desert" => true,
                "volcano" => true,
                "ruins" => true,
                "ocean" => true,
                "mountain" => true,
                "temporal" => true,
                "dream" => true,
                "void" => true,
                "dimensional" => true,
                "generic" => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Gets a template by name (case-insensitive)
        /// </summary>
        public static List<ColoredText> GetTemplate(string templateName, string text)
        {
            return templateName.ToLower() switch
            {
                "fiery" => Fiery(text),
                "icy" => Icy(text),
                "toxic" => Toxic(text),
                "crystalline" => Crystalline(text),
                "golden" => Golden(text),
                "holy" => Holy(text),
                "shadow" => Shadow(text),
                "electric" => Electric(text),
                "common" => Common(text),
                "uncommon" => Uncommon(text),
                "rare" => Rare(text),
                "epic" => Epic(text),
                "legendary" => Legendary(text),
                "mythic" => Mythic(text),
                "transcendent" => Transcendent(text),
                "weapon_class" => WeaponClass(text),
                "head_armor" => HeadArmor(text),
                "chest_armor" => ChestArmor(text),
                "feet_armor" => FeetArmor(text),
                "sword_weapon" => SwordWeapon(text),
                "dagger_weapon" => DaggerWeapon(text),
                "mace_weapon" => MaceWeapon(text),
                "wand_weapon" => WandWeapon(text),
                "natural" => Natural(text),
                "arcane" => Arcane(text),
                // Combat templates
                "critical" => Critical(text),
                "miss" => Miss(text),
                // Modification templates - use rank-based colors for now
                "worn" => SingleColor(text, ColorPalette.DarkRed.GetColor()),
                "dull" => SingleColor(text, Colors.Gray),
                "sturdy" => SingleColor(text, Colors.Gray),
                "balanced" => SingleColor(text, Colors.Gray),
                "sharp" => CreateMultiColorText(text, new[] { Colors.Gray, ColorPalette.White.GetColor() }),
                "swift" => CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), Colors.Gray }),
                "precise" => CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), Colors.Gray }),
                "reinforced" => CreateMultiColorText(text, new[] { ColorPalette.Brown.GetColor(), Colors.Gray }),
                "keen" => CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() }),
                "agile" => CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor() }),
                "lucky" => CreateMultiColorText(text, new[] { ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor() }),
                "vampiric" => CreateMultiColorText(text, new[] { ColorPalette.DarkRed.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.DarkRed.GetColor() }),
                "brutal" => CreateMultiColorText(text, new[] { ColorPalette.Red.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Red.GetColor() }),
                "lightning" => CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() }),
                "blessed" => CreateMultiColorText(text, new[] { ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.White.GetColor() }),
                "venomous" => CreateMultiColorText(text, new[] { ColorPalette.DarkGreen.GetColor(), ColorPalette.Green.GetColor(), ColorPalette.White.GetColor() }),
                "masterwork" => CreateMultiColorText(text, new[] { ColorPalette.Orange.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor() }),
                "godlike" => CreateMultiColorText(text, new[] { ColorPalette.Magenta.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.Magenta.GetColor() }),
                "enchanted" => CreateMultiColorText(text, new[] { ColorPalette.Purple.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() }),
                "annihilation" => CreateMultiColorText(text, new[] { ColorPalette.Red.GetColor(), ColorPalette.Orange.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Red.GetColor() }),
                "timewarp" => CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.Cyan.GetColor() }),
                "perfect" => CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.White.GetColor() }),
                "divine" => CreateMultiColorText(text, new[] { ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor() }),
                "realitybreaker" => CreateMultiColorText(text, new[] { ColorPalette.Magenta.GetColor(), ColorPalette.Red.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.Magenta.GetColor() }),
                "omnipotent" => CreateMultiColorText(text, new[] { ColorPalette.White.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Gold.GetColor(), ColorPalette.White.GetColor() }),
                "infinite" => CreateMultiColorText(text, new[] { ColorPalette.Cyan.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Blue.GetColor() }),
                "cosmic" => CreateMultiColorText(text, new[] { ColorPalette.Magenta.GetColor(), ColorPalette.Blue.GetColor(), ColorPalette.White.GetColor(), ColorPalette.Cyan.GetColor(), ColorPalette.Magenta.GetColor(), ColorPalette.White.GetColor() }),
                // Dungeon theme templates (from ColorTemplates.json)
                "forest" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "g", "G", "w" })),
                "lava" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "r", "R", "O" })),
                "crypt" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "y", "m", "Y" })),
                "crystal" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "m", "M", "C" })),
                "temple" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "W", "Y", "w" })),
                "ice" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "C", "B", "Y" })),
                "steampunk" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "w", "o", "y" })),
                "swamp" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "g", "w", "y" })),
                "astral" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "M", "B", "C" })),
                "underground" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "w", "y", "Y" })),
                "storm" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "C", "Y", "B" })),
                "nature" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "g", "G", "Y" })),
                "desert" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "W", "O", "Y" })),
                "volcano" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "R", "O", "r" })),
                "ruins" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "w", "y", "Y" })),
                "ocean" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "b", "B", "C" })),
                "mountain" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "y", "Y", "C" })),
                "temporal" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "C", "M", "Y" })),
                "dream" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "M", "C", "Y" })),
                "void" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "m", "M", "y" })),
                "dimensional" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "M", "C", "B" })),
                "generic" => CreateMultiColorText(text, ConvertColorCodesToColors(new[] { "y", "Y" })),
                _ => SingleColor(text, Colors.White)
            };
        }
    }
}
