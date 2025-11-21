using Avalonia.Media;
using RPGGame;
using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Manages the persistent layout structure where character info is always visible
    /// and only the center content area changes
    /// </summary>
    public class PersistentLayoutManager
    {
        private readonly GameCanvasControl canvas;
        
        // Layout constants for persistent panels
        private const int SCREEN_WIDTH = 210;
        private const int SCREEN_HEIGHT = 60;
        private const int SCREEN_CENTER = SCREEN_WIDTH / 2;  // The center point for menus
        
        // Left panel (Character Info) - 13% of width
        private const int LEFT_PANEL_X = 0;
        private const int LEFT_PANEL_Y = 2;
        private const int LEFT_PANEL_WIDTH = 27;  // 13% of 210
        private const int LEFT_PANEL_HEIGHT = 56; // 60 - 4 margins
        
        // Center panel (Dynamic Content) - 74% of width
        private const int CENTER_PANEL_X = 28;     // After left panel + 1 space
        private const int CENTER_PANEL_Y = 2;
        private const int CENTER_PANEL_WIDTH = 154; // Middle space (wider center)
        private const int CENTER_PANEL_HEIGHT = 56; // 60 - 4 margins
        
        // Right panel (Dungeon/Enemy Info) - 13% of width
        private const int RIGHT_PANEL_X = 183;     // 210 - 27
        private const int RIGHT_PANEL_Y = 2;
        private const int RIGHT_PANEL_WIDTH = 27;  // Same as left panel
        private const int RIGHT_PANEL_HEIGHT = 56; // 60 - 4 margins
        
        // Top bar for title
        private const int TITLE_Y = 0;
        
        public PersistentLayoutManager(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Renders the complete persistent layout with character info and dynamic content
        /// </summary>
        /// <param name="clearCanvas">Whether to clear the canvas before rendering. Set to false to preserve existing content when transitioning to combat.</param>
        public void RenderLayout(Character? character, Action<int, int, int, int> renderCenterContent, string title = "DUNGEON FIGHTER", Enemy? enemy = null, string? dungeonName = null, string? roomName = null, bool clearCanvas = true)
        {
            if (clearCanvas)
            {
                canvas.Clear();
            }
            
            // Render title bar
            canvas.AddTitle(TITLE_Y, title, AsciiArtAssets.Colors.Gold);
            
            // Render left panel (Character Info) - Always visible
            if (character != null)
            {
                RenderCharacterPanel(character);
            }
            else
            {
                RenderEmptyCharacterPanel();
            }
            
            // Render center panel (Dynamic Content)
            canvas.AddBorder(CENTER_PANEL_X, CENTER_PANEL_Y, CENTER_PANEL_WIDTH, CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
            
            // Call the content renderer for the center area
            renderCenterContent?.Invoke(CENTER_PANEL_X + 1, CENTER_PANEL_Y + 1, CENTER_PANEL_WIDTH - 2, CENTER_PANEL_HEIGHT - 2);
            
            // Render right panel (Dungeon/Enemy Info)
            RenderRightPanel(enemy, dungeonName, roomName);
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Renders the character information panel (left side)
        /// </summary>
        private void RenderCharacterPanel(Character character)
        {
            // Main border for character panel
            canvas.AddBorder(LEFT_PANEL_X + 2, LEFT_PANEL_Y, LEFT_PANEL_WIDTH - 2, LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Blue);
            
            int y = LEFT_PANEL_Y + 1;
            int x = LEFT_PANEL_X + 4;
            
            // Character name and level
            canvas.AddText(x, y, "═══ HERO ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            canvas.AddText(x, y, character.Name, AsciiArtAssets.Colors.White);
            y++;
            canvas.AddText(x, y, $"Lvl {character.Level}", AsciiArtAssets.Colors.Yellow);
            y++;
            canvas.AddText(x, y, character.GetCurrentClass(), AsciiArtAssets.Colors.Cyan);
            y += 2;
            
            // Health bar
            canvas.AddText(x, y, "══ HEALTH ══", AsciiArtAssets.Colors.Gold);
            y += 2;
            canvas.AddText(x, y, "HP:", AsciiArtAssets.Colors.White);
            y++;
            canvas.AddHealthBar(x, y, LEFT_PANEL_WIDTH - 8, character.CurrentHealth, character.GetEffectiveMaxHealth());
            canvas.AddText(x, y + 1, $"{character.CurrentHealth}/{character.GetEffectiveMaxHealth()}", AsciiArtAssets.Colors.White);
            y += 3;
            
            // Stats section
            canvas.AddText(x, y, "══ STATS ══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Determine primary stat based on class points
            string primaryStat = GetPrimaryStatForCharacter(character);
            
            // All stats are white by default, primary stat is purple
            canvas.AddCharacterStat(x, y, "STR", character.GetEffectiveStrength(), 0, 
                primaryStat == "STR" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "AGI", character.GetEffectiveAgility(), 0, 
                primaryStat == "AGI" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "TEC", character.GetEffectiveTechnique(), 0, 
                primaryStat == "TEC" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y++;
            canvas.AddCharacterStat(x, y, "INT", character.GetEffectiveIntelligence(), 0, 
                primaryStat == "INT" ? AsciiArtAssets.Colors.Purple : AsciiArtAssets.Colors.White);
            y += 2;
            
            // Equipment section
            canvas.AddText(x, y, "══ GEAR ══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Render all equipment slots with increased spacing for text wrapping
            RenderEquipmentSlot(x, ref y, "Weapon", character.Weapon, 2);
            RenderEquipmentSlot(x, ref y, "Head", character.Head, 2);
            RenderEquipmentSlot(x, ref y, "Body", character.Body, 2);
            RenderEquipmentSlot(x, ref y, "Feet", character.Feet, 1);
        }
        
        /// <summary>
        /// Helper method to render a single equipment slot with consistent formatting and text wrapping
        /// </summary>
        private void RenderEquipmentSlot(int x, ref int y, string slotName, Item? item, int spacingAfter = 1)
        {
            string displayName = item?.Name ?? "None";
            Color color = item != null ? AsciiArtAssets.GetRarityColor(item.Rarity) : AsciiArtAssets.Colors.Gray;
            
            canvas.AddText(x, y, $"{slotName}:", AsciiArtAssets.Colors.Gray);
            y++;
            
            // Wrap text if it's too long (max width of 17 characters)
            const int maxWidth = 17;
            List<string> wrappedLines = WrapText(displayName, maxWidth);
            
            foreach (string line in wrappedLines)
            {
                canvas.AddText(x, y, line, color);
                y++;
            }
            
            y += spacingAfter;
        }
        
        /// <summary>
        /// Wraps text to fit within a specified width, breaking at spaces when possible
        /// Handles color markup properly by using display length instead of raw length
        /// </summary>
        private List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();
            
            if (string.IsNullOrEmpty(text))
            {
                lines.Add(text ?? "");
                return lines;
            }
            
            // Use display length (excluding markup) for comparison
            if (ColorParser.GetDisplayLength(text) <= maxWidth)
            {
                lines.Add(text);
                return lines;
            }
            
            // Break text into words
            string[] words = text.Split(' ');
            string currentLine = "";
            int currentLineDisplayLength = 0;
            
            foreach (string word in words)
            {
                int wordDisplayLength = ColorParser.GetDisplayLength(word);
                
                // If adding this word would exceed the max width
                if (currentLineDisplayLength > 0 && (currentLineDisplayLength + 1 + wordDisplayLength) > maxWidth)
                {
                    // Add the current line and start a new one
                    lines.Add(currentLine);
                    currentLine = word;
                    currentLineDisplayLength = wordDisplayLength;
                }
                else
                {
                    // Add the word to the current line
                    if (currentLineDisplayLength > 0)
                    {
                        currentLine += " " + word;
                        currentLineDisplayLength += 1 + wordDisplayLength;
                    }
                    else
                    {
                        currentLine = word;
                        currentLineDisplayLength = wordDisplayLength;
                    }
                }
                
                // If a single word is longer than maxWidth, we need to break it
                // Note: This is a simplification - proper character-level breaking with markup is complex
                if (currentLineDisplayLength > maxWidth)
                {
                    lines.Add(currentLine);
                    currentLine = "";
                    currentLineDisplayLength = 0;
                }
            }
            
            // Add the last line if there's anything left
            if (currentLineDisplayLength > 0)
            {
                lines.Add(currentLine);
            }
            
            return lines;
        }
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Always shows all sections (Location, Room, Enemy) even when empty
        /// </summary>
        private void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName)
        {
            // Main border for right panel
            canvas.AddBorder(RIGHT_PANEL_X, RIGHT_PANEL_Y, RIGHT_PANEL_WIDTH - 2, RIGHT_PANEL_HEIGHT, AsciiArtAssets.Colors.Purple);
            
            int y = RIGHT_PANEL_Y + 1;
            int x = RIGHT_PANEL_X + 2;
            
            // Location section - always shown
            canvas.AddText(x, y, "══ LOCATION ══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Dungeon - always shown
            canvas.AddText(x, y, "Dungeon:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(dungeonName))
            {
                string displayDungeon = dungeonName;
                if (displayDungeon.Length > 20)
                    displayDungeon = displayDungeon.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayDungeon, AsciiArtAssets.Colors.Cyan);
            }
            else
            {
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
            }
            y += 2;
            
            // Room - always shown
            canvas.AddText(x, y, "Room:", AsciiArtAssets.Colors.Gray);
            y++;
            if (!string.IsNullOrEmpty(roomName))
            {
                string displayRoom = roomName;
                if (displayRoom.Length > 20)
                    displayRoom = displayRoom.Substring(0, 17) + "...";
                canvas.AddText(x, y, displayRoom, AsciiArtAssets.Colors.Yellow);
            }
            else
            {
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
            }
            y += 2;
            
            // Enemy section - always shown
            canvas.AddText(x, y, "══ ENEMY ══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (enemy != null)
            {
                string enemyName = enemy.Name;
                if (enemyName.Length > 20)
                    enemyName = enemyName.Substring(0, 17) + "...";
                
                canvas.AddText(x, y, enemyName, AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"Lvl {enemy.Level}", AsciiArtAssets.Colors.Yellow);
                y += 2;
                
                // Enemy health bar
                canvas.AddText(x, y, "HP:", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddHealthBar(x, y, RIGHT_PANEL_WIDTH - 6, enemy.CurrentHealth, enemy.MaxHealth, AsciiArtAssets.Colors.Green, AsciiArtAssets.Colors.DarkGreen);
                canvas.AddText(x, y + 1, $"{enemy.CurrentHealth}/{enemy.MaxHealth}", AsciiArtAssets.Colors.White);
                y += 3;
                
                // Enemy stats
                canvas.AddText(x, y, "Armor:", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"{enemy.Armor}", AsciiArtAssets.Colors.Yellow);
                y += 2;
                
                canvas.AddText(x, y, "Attack:", AsciiArtAssets.Colors.White);
                y++;
                canvas.AddText(x, y, $"STR {enemy.Strength}", AsciiArtAssets.Colors.Yellow);
                y++;
                canvas.AddText(x, y, $"AGI {enemy.Agility}", AsciiArtAssets.Colors.Yellow);
                y++;
                canvas.AddText(x, y, $"TEC {enemy.Technique}", AsciiArtAssets.Colors.Yellow);
                y++;
                canvas.AddText(x, y, $"INT {enemy.Intelligence}", AsciiArtAssets.Colors.Yellow);
            }
            else
            {
                // Show empty enemy state
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.DarkGray);
                y += 2;
                canvas.AddText(x, y, "No active", AsciiArtAssets.Colors.DarkGray);
                y++;
                canvas.AddText(x, y, "combat", AsciiArtAssets.Colors.DarkGray);
            }
        }
        
        /// <summary>
        /// Renders an empty character panel when no character is loaded
        /// </summary>
        private void RenderEmptyCharacterPanel()
        {
            canvas.AddBorder(LEFT_PANEL_X + 2, LEFT_PANEL_Y, LEFT_PANEL_WIDTH - 2, LEFT_PANEL_HEIGHT, AsciiArtAssets.Colors.Gray);
            
            int y = LEFT_PANEL_Y + LEFT_PANEL_HEIGHT / 2;
            int x = LEFT_PANEL_X + 6;
            
            canvas.AddText(x, y, "No Character", AsciiArtAssets.Colors.Gray);
            canvas.AddText(x, y + 1, "Loaded", AsciiArtAssets.Colors.Gray);
        }
        
        /// <summary>
        /// Determines the primary stat for a character based on their class points
        /// </summary>
        private string GetPrimaryStatForCharacter(Character character)
        {
            // Get the highest class points to determine primary stat
            int barbarianPoints = character.BarbarianPoints;
            int warriorPoints = character.WarriorPoints;
            int roguePoints = character.RoguePoints;
            int wizardPoints = character.WizardPoints;
            
            // Find the class with the most points
            var classes = new List<(string stat, int points)>
            {
                ("STR", barbarianPoints),  // Barbarian - Strength
                ("AGI", warriorPoints),    // Warrior - Agility
                ("TEC", roguePoints),      // Rogue - Technique
                ("INT", wizardPoints)      // Wizard - Intelligence
            };
            
            classes.Sort((a, b) => b.points.CompareTo(a.points));
            
            // If no class points, no primary stat (all white)
            if (classes[0].points == 0)
                return "";
            
            return classes[0].stat;
        }
        
        /// <summary>
        /// Gets the center content area dimensions
        /// </summary>
        public (int x, int y, int width, int height) GetCenterContentArea()
        {
            return (CENTER_PANEL_X + 2, CENTER_PANEL_Y + 2, CENTER_PANEL_WIDTH - 4, CENTER_PANEL_HEIGHT - 4);
        }
    }
}

