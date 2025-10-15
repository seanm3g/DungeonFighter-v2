using Avalonia.Media;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia
{
    public static class AsciiArtAssets
    {
        // Color definitions
        public static class Colors
        {
            public static readonly Color White = Color.FromRgb(255, 255, 255);
            public static readonly Color Gray = Color.FromRgb(128, 128, 128);
            public static readonly Color DarkGray = Color.FromRgb(64, 64, 64);
            public static readonly Color Black = Color.FromRgb(0, 0, 0);
            public static readonly Color Red = Color.FromRgb(255, 0, 0);
            public static readonly Color DarkRed = Color.FromRgb(139, 0, 0);
            public static readonly Color Green = Color.FromRgb(0, 255, 0);
            public static readonly Color DarkGreen = Color.FromRgb(0, 100, 0);
            public static readonly Color Blue = Color.FromRgb(0, 0, 255);
            public static readonly Color DarkBlue = Color.FromRgb(0, 0, 139);
            public static readonly Color Yellow = Color.FromRgb(255, 255, 0);
            public static readonly Color Orange = Color.FromRgb(255, 165, 0);
            public static readonly Color Purple = Color.FromRgb(128, 0, 128);
            public static readonly Color Cyan = Color.FromRgb(0, 255, 255);
            public static readonly Color Magenta = Color.FromRgb(255, 0, 255);
            public static readonly Color Gold = Color.FromRgb(255, 215, 0);
            public static readonly Color Silver = Color.FromRgb(192, 192, 192);
            public static readonly Color Bronze = Color.FromRgb(205, 127, 50);
        }

        // Equipment Icons
        public static class EquipmentIcons
        {
            public const string Sword = "⚔";
            public const string Shield = "🛡";
            public const string Bow = "🏹";
            public const string Wand = "🔮";
            public const string Staff = "⛏";
            public const string Mace = "🔨";
            public const string Dagger = "🗡";
            public const string Armor = "🛡";
            public const string Helmet = "⛑";
            public const string Boots = "👢";
            public const string Ring = "💍";
            public const string Amulet = "📿";
            public const string Potion = "🧪";
            public const string Scroll = "📜";
            public const string Gem = "💎";
        }

        // Status Effect Icons
        public static class StatusIcons
        {
            public const string Burn = "🔥";
            public const string Freeze = "❄";
            public const string Poison = "💀";
            public const string Stun = "⚡";
            public const string Bleed = "🩸";
            public const string Heal = "💚";
            public const string Shield = "🛡";
            public const string Speed = "💨";
            public const string Strength = "💪";
            public const string Magic = "✨";
            public const string Weak = "😵";
            public const string Confused = "😵‍💫";
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

        // Combat Elements
        public static class CombatIcons
        {
            public const string Player = "👤";
            public const string Enemy = "👹";
            public const string Boss = "👑";
            public const string Damage = "💥";
            public const string Critical = "💢";
            public const string Miss = "💨";
            public const string Block = "🛡";
            public const string Dodge = "💨";
            public const string Parry = "⚔";
            public const string Combo = "⚡";
            public const string Magic = "✨";
            public const string Heal = "💚";
            public const string Death = "💀";
            public const string Victory = "🏆";
            public const string Defeat = "💔";
        }

        // Dungeon Elements
        public static class DungeonIcons
        {
            public const string Room = "🏠";
            public const string Door = "🚪";
            public const string Chest = "📦";
            public const string Trap = "⚠";
            public const string Secret = "❓";
            public const string Exit = "🚪";
            public const string Stairs = "🪜";
            public const string Portal = "🌀";
            public const string Altar = "⛩";
            public const string Fountain = "⛲";
            public const string Fire = "🔥";
            public const string Ice = "❄";
            public const string Lava = "🌋";
            public const string Water = "💧";
            public const string Forest = "🌲";
            public const string Desert = "🏜";
            public const string Mountain = "⛰";
            public const string Cave = "🕳";
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

        // Get equipment icon by weapon type
        public static string GetWeaponIcon(string weaponType)
        {
            return weaponType.ToLower() switch
            {
                "sword" => EquipmentIcons.Sword,
                "bow" => EquipmentIcons.Bow,
                "wand" => EquipmentIcons.Wand,
                "staff" => EquipmentIcons.Staff,
                "mace" => EquipmentIcons.Mace,
                "dagger" => EquipmentIcons.Dagger,
                _ => EquipmentIcons.Sword
            };
        }

        // Get armor icon by armor type
        public static string GetArmorIcon(string armorType)
        {
            return armorType.ToLower() switch
            {
                "helmet" or "head" => EquipmentIcons.Helmet,
                "armor" or "body" or "chest" => EquipmentIcons.Armor,
                "boots" or "feet" => EquipmentIcons.Boots,
                "ring" => EquipmentIcons.Ring,
                "amulet" or "necklace" => EquipmentIcons.Amulet,
                _ => EquipmentIcons.Armor
            };
        }

        // Get status effect icon
        public static string GetStatusIcon(string statusEffect)
        {
            return statusEffect.ToLower() switch
            {
                "burn" or "burning" => StatusIcons.Burn,
                "freeze" or "frozen" => StatusIcons.Freeze,
                "poison" or "poisoned" => StatusIcons.Poison,
                "stun" or "stunned" => StatusIcons.Stun,
                "bleed" or "bleeding" => StatusIcons.Bleed,
                "heal" or "healing" => StatusIcons.Heal,
                "shield" or "protected" => StatusIcons.Shield,
                "speed" or "haste" => StatusIcons.Speed,
                "strength" or "strong" => StatusIcons.Strength,
                "magic" or "enchanted" => StatusIcons.Magic,
                "weak" or "weakened" => StatusIcons.Weak,
                "confused" or "confusion" => StatusIcons.Confused,
                _ => "?"
            };
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
            string rarityPrefix = string.IsNullOrEmpty(rarity) ? "" : $"[{rarity.ToUpper()}] ";
            string statsSuffix = string.IsNullOrEmpty(stats) ? "" : $" - {stats}";
            return $"[{number}] {rarityPrefix}{name}{statsSuffix}";
        }

        // Title Screen ASCII Art
        public static class TitleArt
        {
            // Main title screen with DUNGEON FIGHTER logo
            // 75 chars wide (including border)
            public static readonly string[] DungeonFighterTitle = new string[]
            {
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "&G                                                                                  ",
                "&G                                                                                    ",
                "&G                                                                          &W██████╗  ██╗   ██╗███╗   ██╗ ██████╗ ███████╗ ██████╗ ███╗   ██╗",
                "&G                                                                          &W██╔═══██╗██║   ██║████╗  ██║██╔════╝ ██╔════╝██╔═══██╗████╗  ██║",
                "&G                                                                          &W██║   ██║██║   ██║██╔██╗ ██║██║  ███╗█████╗  ██║   ██║██╔██╗ ██║",
                "&G                                                                          &W██║   ██║██║   ██║██║╚██╗██║██║   ██║██╔══╝  ██║   ██║██║╚██╗██║",
                "&G                                                                          &W╚██████╔╝╚██████╔╝██║ ╚████║╚██████╔╝███████╗╚██████╔╝██║ ╚████║",
                "&G                                                                           &W╚═════╝  ╚═════╝ ╚═╝  ╚═══╝ ╚═════╝ ╚══════╝ ╚═════╝ ╚═╝  ╚═══╝",
                "&G                                                                                                                                                        ",
                "&G                                                                                                               ◈━━━━━━━━━━━━━━━◈                     ",
                "&G                                                                                                                                                        ",
                "&G                                                                                      &R███████╗██╗ ██████╗ ██╗  ██╗████████╗███████╗██████╗     ",
                "&G                                                                                      &R██╔════╝██║██╔════╝ ██║  ██║╚══██╔══╝██╔════╝██╔══██╗    ",
                "&G                                                                                      &R█████╗  ██║██║  ███╗███████║   ██║   █████╗  ██████╔╝     ",
                "&G                                                                                      &R██╔══╝  ██║██║   ██║██╔══██║   ██║   ██╔══╝  ██╔══██╗     ",
                "&G                                                                                      &R██║     ██║╚██████╔╝██║  ██║   ██║   ███████╗██║  ██║     ",
                "&G                                                                                      &R╚═╝     ╚═╝ ╚═════╝ ╚═╝  ╚═╝   ╚═╝   ╚══════╝╚═╝  ╚═╝     ",
                "&G                                                                                                                                                       ",
                "&G                                                                                 ",
                "&G                                                                                                                                                        ",
                "&G                                                                                    &C◈ Enter the depths. Face the darkness. Claim your glory. ◈       ",
                "&G                                                                                                                                                        ",
                "&G                                                                                 ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                                                                                        ",
                "&G                                                                                              &Y[ Press any key to continue ]       ",
                "&G                                                                                                                                                        ",
                "",
            };
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
            public const string EnemyAttackFormat = "             Attack: STR {0}, AGI {1}, TEC {2}, INT {3}";
            public const string EncounteredFormat = "Encountered [{0}]{1}!";
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
