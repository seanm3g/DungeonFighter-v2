using Avalonia.Media;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    public static class AsciiArtAssets
    {
        // Color definitions - Now delegates to ColorPalette for consistency
        // Maintains backward compatibility while using centralized color system
        public static class Colors
        {
            public static Color White => ColorPalette.White.GetColor();
            public static Color Gray => ColorPalette.Gray.GetColor();
            public static Color DarkGray => ColorPalette.DarkGray.GetColor();
            public static Color Black => ColorPalette.Black.GetColor();
            public static Color Red => ColorPalette.Red.GetColor();
            public static Color DarkRed => ColorPalette.DarkRed.GetColor();
            public static Color Green => ColorPalette.Green.GetColor();
            public static Color DarkGreen => ColorPalette.DarkGreen.GetColor();
            public static Color Blue => ColorPalette.Blue.GetColor();
            public static Color DarkBlue => ColorPalette.DarkBlue.GetColor();
            public static Color Yellow => ColorPalette.Yellow.GetColor();
            public static Color Orange => ColorPalette.Orange.GetColor();
            public static Color Purple => ColorPalette.Purple.GetColor();
            public static Color Cyan => ColorPalette.Cyan.GetColor();
            public static Color Magenta => ColorPalette.Magenta.GetColor();
            public static Color Gold => ColorPalette.Gold.GetColor();
            public static Color Silver => ColorPalette.Silver.GetColor();
            public static Color Bronze => ColorPalette.Bronze.GetColor();
        }

        // Equipment Icons - Now delegates to IconRegistry
        // Maintains backward compatibility
        public static class EquipmentIcons
        {
            public const string Sword = IconRegistry.Sword;
            public const string Shield = IconRegistry.Shield;
            public const string Bow = IconRegistry.Bow;
            public const string Wand = IconRegistry.Wand;
            public const string Staff = IconRegistry.Staff;
            public const string Mace = IconRegistry.Mace;
            public const string Dagger = IconRegistry.Dagger;
            public const string Armor = IconRegistry.Armor;
            public const string Helmet = IconRegistry.Helmet;
            public const string Boots = IconRegistry.Boots;
            public const string Ring = IconRegistry.Ring;
            public const string Amulet = IconRegistry.Amulet;
            public const string Potion = IconRegistry.Potion;
            public const string Scroll = IconRegistry.Scroll;
            public const string Gem = IconRegistry.Gem;
        }

        // Status Effect Icons - Now delegates to IconRegistry
        // Maintains backward compatibility
        public static class StatusIcons
        {
            public const string Burn = IconRegistry.Burn;
            public const string Freeze = IconRegistry.Freeze;
            public const string Poison = IconRegistry.Poison;
            public const string Stun = IconRegistry.Stun;
            public const string Bleed = IconRegistry.Bleed;
            public const string Heal = IconRegistry.Heal;
            public const string Shield = IconRegistry.Shield;
            public const string Speed = IconRegistry.Speed;
            public const string Strength = IconRegistry.Strength;
            public const string Magic = IconRegistry.Magic;
            public const string Weak = IconRegistry.Weak;
            public const string Confused = IconRegistry.Confused;
        }

        // UI Elements
        public static class UIElements
        {
            public const string BorderTopLeft = "┌";
            public const string BorderTopRight = "┐";
            public const string BorderBottomLeft = "└";
            public const string BorderBottomRight = "┘";
            public const string BorderHorizontal = "─";
            public const string BorderVertical = "│";
            public const string BorderCross = "┼";
            public const string BorderTopT = "┬";
            public const string BorderBottomT = "┴";
            public const string BorderLeftT = "├";
            public const string BorderRightT = "┤";
            
            public const string ProgressBarFull = "█";
            public const string ProgressBarEmpty = "░";
            public const string ProgressBarHalf = "▓";
            public const string ProgressBarQuarter = "▒";
            
            public const string ArrowUp = "▲";
            public const string ArrowDown = "▼";
            public const string ArrowLeft = "◄";
            public const string ArrowRight = "►";
            public const string ArrowUpDown = "↕";
            public const string ArrowLeftRight = "↔";
            
            public const string Checkmark = "✓";
            public const string X = "✗";
            public const string Star = "★";
            public const string Heart = "♥";
            public const string Diamond = "♦";
            public const string Spade = "♠";
            public const string Club = "♣";
        }

        // Combat Elements - Now delegates to IconRegistry
        // Maintains backward compatibility
        public static class CombatIcons
        {
            public const string Player = IconRegistry.Player;
            public const string Enemy = IconRegistry.Enemy;
            public const string Boss = IconRegistry.Boss;
            public const string Damage = IconRegistry.Damage;
            public const string Critical = IconRegistry.Critical;
            public const string Miss = IconRegistry.Miss;
            public const string Block = IconRegistry.Block;
            public const string Dodge = IconRegistry.Dodge;
            public const string Parry = IconRegistry.Parry;
            public const string Combo = IconRegistry.Combo;
            public const string Magic = IconRegistry.Magic;
            public const string Heal = IconRegistry.Heal;
            public const string Death = IconRegistry.Death;
            public const string Victory = IconRegistry.Victory;
            public const string Defeat = IconRegistry.Defeat;
        }

        // Dungeon Elements - Now delegates to IconRegistry
        // Maintains backward compatibility
        public static class DungeonIcons
        {
            public const string Room = IconRegistry.Room;
            public const string Door = IconRegistry.Door;
            public const string Chest = IconRegistry.Chest;
            public const string Trap = IconRegistry.Trap;
            public const string Secret = IconRegistry.Secret;
            public const string Exit = IconRegistry.Exit;
            public const string Stairs = IconRegistry.Stairs;
            public const string Portal = IconRegistry.Portal;
            public const string Altar = IconRegistry.Altar;
            public const string Fountain = IconRegistry.Fountain;
            public const string Fire = IconRegistry.Fire;
            public const string Ice = IconRegistry.Ice;
            public const string Lava = IconRegistry.Lava;
            public const string Water = IconRegistry.Water;
            public const string Forest = IconRegistry.Forest;
            public const string Desert = IconRegistry.Desert;
            public const string Mountain = IconRegistry.Mountain;
            public const string Cave = IconRegistry.Cave;
        }

        // Rarity Colors
        public static class RarityColors
        {
            public static readonly Color Common = Colors.White;
            public static readonly Color Uncommon = Colors.Green;
            public static readonly Color Rare = Colors.Blue;
            public static readonly Color Epic = Colors.Purple;
            public static readonly Color Legendary = Colors.Gold;
            public static readonly Color Mythic = Colors.Magenta;
            public static readonly Color Transcendent = Colors.Cyan;
        }

        // Get equipment icon by weapon type - Delegates to IconRegistry
        public static string GetWeaponIcon(string weaponType)
        {
            return IconRegistry.GetWeaponIcon(weaponType);
        }

        // Get armor icon by armor type - Delegates to IconRegistry
        public static string GetArmorIcon(string armorType)
        {
            return IconRegistry.GetArmorIcon(armorType);
        }

        // Get status effect icon - Delegates to IconRegistry
        public static string GetStatusIcon(string statusEffect)
        {
            return IconRegistry.GetStatusIcon(statusEffect);
        }

        // Get rarity color
        public static Color GetRarityColor(string rarity)
        {
            return rarity.ToLower() switch
            {
                "common" => RarityColors.Common,
                "uncommon" => RarityColors.Uncommon,
                "rare" => RarityColors.Rare,
                "epic" => RarityColors.Epic,
                "legendary" => RarityColors.Legendary,
                "mythic" => RarityColors.Mythic,
                "transcendent" => RarityColors.Transcendent,
                _ => RarityColors.Common
            };
        }

        // Create a box border
        public static string[] CreateBox(int width, int height, string title = "")
        {
            var lines = new List<string>();
            
            // Top border
            string topLine = UIElements.BorderTopLeft + new string(UIElements.BorderHorizontal[0], width - 2) + UIElements.BorderTopRight;
            if (!string.IsNullOrEmpty(title) && title.Length <= width - 4)
            {
                int titleStart = (width - title.Length - 2) / 2;
                topLine = topLine.Substring(0, titleStart) + $" {title} " + topLine.Substring(titleStart + title.Length + 2);
            }
            lines.Add(topLine);
            
            // Middle lines
            for (int i = 1; i < height - 1; i++)
            {
                lines.Add(UIElements.BorderVertical + new string(' ', width - 2) + UIElements.BorderVertical);
            }
            
            // Bottom border
            lines.Add(UIElements.BorderBottomLeft + new string(UIElements.BorderHorizontal[0], width - 2) + UIElements.BorderBottomRight);
            
            return lines.ToArray();
        }

        // Create a progress bar
        public static string CreateProgressBar(int width, double progress, char fullChar = '█', char emptyChar = '░')
        {
            int filledWidth = (int)(width * progress);
            int emptyWidth = width - filledWidth;
            
            return new string(fullChar, filledWidth) + new string(emptyChar, emptyWidth);
        }

        // Create a health bar with text
        public static string CreateHealthBar(int current, int max, int width = 20)
        {
            double progress = (double)current / max;
            string bar = CreateProgressBar(width, progress);
            return $"[{bar}] {current}/{max}";
        }

        // Create a menu option
        public static string CreateMenuOption(int number, string text, bool selected = false)
        {
            string prefix = selected ? "► " : "  ";
            return $"{prefix}[{number}] {text}";
        }

        // Create an item display
        public static string CreateItemDisplay(int number, string name, string stats = "", string rarity = "")
        {
            // TrimEnd() to ensure no trailing spaces before the closing bracket
            string rarityPrefix = string.IsNullOrEmpty(rarity) ? "" : $"[{rarity.TrimEnd().ToUpper()}] ";
            string statsSuffix = string.IsNullOrEmpty(stats) ? "" : $" - {stats}";
            return $"[{number}] {rarityPrefix}{name}{statsSuffix}";
        }

        // UI Text Constants
        public static class UIText
        {
            // Header decorations
            public const string HeaderPrefix = "═══";
            public const string HeaderSuffix = "═══";
            public const string Divider = "====================================";
            
            // Combat messages
            public const string CombatLogHeader = "COMBAT LOG";
            public const string BattleCompleteHeader = "BATTLE COMPLETE";
            public const string BattleHighlightsHeader = "BATTLE HIGHLIGHTS";
            
            // Room messages
            public const string EnteringDungeonHeader = "ENTERING DUNGEON";
            public const string EnteringRoomHeader = "ENTERING ROOM";
            public const string RoomClearedMessage = "Room cleared!";
            
            // Combat status
            public const string EnemyHeader = "ENEMY";
            public const string PreparingForCombat = "PREPARING FOR COMBAT";
            
            // Victory/Defeat
            public const string VictoryPrefix = "[{0}] has been defeated!";
            public const string DefeatMessage = "You have been defeated!";
            
            // Stats display
            public const string RemainingHealth = "Remaining Health: {0}/{1}";
            public const string EnemyStatsFormat = "Enemy Stats - Health: {0}/{1}, Armor: {2}";
            public const string EnemyAttackFormat = "STR {0}, AGI {1}, TEC {2}, INT {3}";
            public const string EncounteredFormat = "A {0}{1} appears.";
            public const string WeaponSuffix = " with {0}";
            
            // Helper methods
            public static string CreateHeader(string text) => $"{HeaderPrefix} {text} {HeaderSuffix}";
            public static string FormatEnemyStats(int currentHealth, int maxHealth, int armor) 
                => string.Format(EnemyStatsFormat, currentHealth, maxHealth, armor);
            public static string FormatEnemyAttack(int str, int agi, int tec, int intel) 
                => string.Format(EnemyAttackFormat, str, agi, tec, intel);
        }
    }
}
