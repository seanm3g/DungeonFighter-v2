using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;
using RPGGame.UI.Avalonia.Renderers;
using RPGGame.Utils;

namespace RPGGame.UI.Avalonia
{
    /// <summary>
    /// Manages the persistent layout structure where character info is always visible
    /// and only the center content area changes
    /// </summary>
    public class PersistentLayoutManager
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private string lastRenderedTitle = "";
        
        // Layout constants for persistent panels
        private const int SCREEN_WIDTH = 210;
        private const int SCREEN_HEIGHT = 52;  // Reduced from 60 to make panels less tall
        private const int SCREEN_CENTER = SCREEN_WIDTH / 2;  // The center point for menus
        
        // Left panel (Character Info) - wider for better visibility
        private const int LEFT_PANEL_X = 0;
        private const int LEFT_PANEL_Y = 2;
        private const int LEFT_PANEL_WIDTH = 30;  // Increased from 27
        private const int LEFT_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Center panel (Dynamic Content) - narrower to give more space to side panels
        private const int CENTER_PANEL_X = 31;     // After left panel + 1 space
        private const int CENTER_PANEL_Y = 2;
        private const int CENTER_PANEL_WIDTH = 136; // Further reduced from 148 to prevent right panel cutoff
        private const int CENTER_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Right panel (Dungeon/Enemy Info) - wider for better visibility, positioned after center panel
        private const int RIGHT_PANEL_X = 168;     // After center panel (31 + 142 + 1 gap)
        private const int RIGHT_PANEL_Y = 2;
        private const int RIGHT_PANEL_WIDTH = 30;  // Increased from 27
        private const int RIGHT_PANEL_HEIGHT = 48; // 52 - 4 margins (reduced from 56)
        
        // Top bar for title
        private const int TITLE_Y = 0;
        
        public PersistentLayoutManager(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            this.textWriter = new ColoredTextWriter(canvas);
        }
        
        /// <summary>
        /// Renders the complete persistent layout with character info and dynamic content
        /// </summary>
        /// <param name="clearCanvas">Whether to clear the canvas before rendering. Set to false to preserve existing content when transitioning to combat.</param>
        public void RenderLayout(Character? character, Action<int, int, int, int> renderCenterContent, string title = "DUNGEON FIGHTER", Enemy? enemy = null, string? dungeonName = null, string? roomName = null, bool clearCanvas = true)
        {
            RenderLayout(character, renderCenterContent, title, enemy, dungeonName, roomName, clearCanvas, character);
        }
        
        /// <summary>
        /// Internal method that handles the actual rendering
        /// </summary>
        private void RenderLayout(Character? character, Action<int, int, int, int> renderCenterContent, string title, Enemy? enemy, string? dungeonName, string? roomName, bool clearCanvas, Character? characterForRightPanel)
        {
            // Clear canvas if title changed - this ensures clean transitions when title changes
            bool titleChanged = title != lastRenderedTitle;
            if (titleChanged)
            {
                canvas.Clear();
                clearCanvas = true; // Force full render when title changes
            }
            
            if (clearCanvas)
            {
                // Always clear the entire title line before rendering new title to prevent overlay
                // This is critical when transitioning between screens with different title lengths
                // Clear a wider range to ensure we remove any partial text from longer titles
                canvas.ClearTextInRange(TITLE_Y, TITLE_Y);
                
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
                
                // Render center panel border (only when clearing)
                canvas.AddBorder(CENTER_PANEL_X, CENTER_PANEL_Y, CENTER_PANEL_WIDTH, CENTER_PANEL_HEIGHT, AsciiArtAssets.Colors.Cyan);
                
                // Explicitly clear the center panel content area to ensure clean rendering
                // This prevents old content from showing when transitioning between screens
                int centerContentX = CENTER_PANEL_X + 1;
                int centerContentY = CENTER_PANEL_Y + 1;
                int centerContentWidth = CENTER_PANEL_WIDTH - 2;
                int centerContentHeight = CENTER_PANEL_HEIGHT - 2;
                // Clear with height + 1 to ensure we clear the full area (endY is exclusive)
                canvas.ClearTextInArea(centerContentX, centerContentY, centerContentWidth, centerContentHeight + 1);
                canvas.ClearProgressBarsInArea(centerContentX, centerContentY, centerContentWidth, centerContentHeight);
            }
            else
            {
                // When not clearing, only update title if it changed, and update panels that need updating
                // Don't re-render center panel border - preserve existing content
                // Clear the title line before updating to prevent overlay from different title lengths
                if (titleChanged)
                {
                    canvas.ClearTextInRange(TITLE_Y, TITLE_Y);
                }
                canvas.AddTitle(TITLE_Y, title, AsciiArtAssets.Colors.Gold);
                
                // Update left panel (Character Info) - may have changed
                if (character != null)
                {
                    RenderCharacterPanel(character);
                }
                
                // Note: We do NOT clear the center content area here when clearCanvas=false
                // The DisplayRenderer (or other content renderer) is responsible for clearing
                // its own content area before rendering. This avoids redundant clearing and
                // ensures proper separation of concerns.
            }
            
            // Track the last rendered title
            lastRenderedTitle = title;
            
            // Always call the content renderer for the center area
            // When clearCanvas is false, this will render from display buffer which contains all content
            int centerX = CENTER_PANEL_X + 1;
            int centerY = CENTER_PANEL_Y + 1;
            int centerW = CENTER_PANEL_WIDTH - 2;
            int centerH = CENTER_PANEL_HEIGHT - 2;
            ScrollDebugLogger.Log($"PersistentLayoutManager: About to invoke renderCenterContent with x={centerX}, y={centerY}, width={centerW}, height={centerH}");
            renderCenterContent?.Invoke(centerX, centerY, centerW, centerH);
            ScrollDebugLogger.Log($"PersistentLayoutManager: renderCenterContent invoked");
            
            // Render right panel (Dungeon/Enemy Info or Inventory Actions) - always update
            RenderRightPanel(enemy, dungeonName, roomName, title, characterForRightPanel);
            
            // Ensure refresh happens on UI thread and after all rendering is complete
            if (Dispatcher.UIThread.CheckAccess())
            {
                canvas.Refresh();
            }
            else
            {
                Dispatcher.UIThread.Post(() => canvas.Refresh());
            }
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
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Hero), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            canvas.AddText(x, y, character.Name, ColorPalette.Player.GetColor());
            y++;
            canvas.AddText(x, y, $"Lvl {character.Level}", AsciiArtAssets.Colors.Yellow);
            y++;
            canvas.AddText(x, y, character.GetCurrentClass(), AsciiArtAssets.Colors.Cyan);
            y += 2;
            
            // Health bar
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Health), AsciiArtAssets.Colors.Gold);
            y += 2;
            canvas.AddText(x, y, "HP:", AsciiArtAssets.Colors.White);
            y++;
            
            // Clear the health bar and HP value area before redrawing to prevent text overlap
            int healthBarWidth = LEFT_PANEL_WIDTH - 8;
            int healthBarY = y;
            int hpValueY = y + 1;
            canvas.ClearProgressBarsInArea(x, healthBarY, healthBarWidth, 1);
            canvas.ClearTextInArea(x, hpValueY, healthBarWidth, 1);
            
            canvas.AddHealthBar(x, y, healthBarWidth, character.CurrentHealth, character.GetEffectiveMaxHealth());
            canvas.AddText(x, y + 1, $"{character.CurrentHealth}/{character.GetEffectiveMaxHealth()}", AsciiArtAssets.Colors.White);
            y += 3;
            
            // Stats section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Stats), AsciiArtAssets.Colors.Gold);
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
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Gear), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            // Render all equipment slots with increased spacing for text wrapping
            RenderEquipmentSlot(x, ref y, "Weapon", character.Weapon, 2);
            RenderEquipmentSlot(x, ref y, "Head", character.Head, 2);
            RenderEquipmentSlot(x, ref y, "Body", character.Body, 2);
            RenderEquipmentSlot(x, ref y, "Feet", character.Feet, 1);
        }
        
        /// <summary>
        /// Helper method to render a single equipment slot with consistent formatting and text wrapping
        /// Uses colored text system to show item colors based on type and modifiers
        /// </summary>
        private void RenderEquipmentSlot(int x, ref int y, string slotName, Item? item, int spacingAfter = 1)
        {
            canvas.AddText(x, y, $"{slotName}:", AsciiArtAssets.Colors.Gray);
            y++;
            
            if (item != null)
            {
                // Get colored item name segments
                var itemNameSegments = ItemDisplayColoredText.FormatFullItemName(item);
                
                // Wrap text if it's too long (max width of 17 characters)
                const int maxWidth = 17;
                var wrappedLines = textWriter.WrapColoredSegments(itemNameSegments, maxWidth);
                
                // Render each wrapped line with proper colors
                foreach (var lineSegments in wrappedLines)
                {
                    if (lineSegments.Count > 0)
                    {
                        textWriter.RenderSegments(lineSegments, x, y);
                    }
                    y++;
                }
            }
            else
            {
                // Empty slot - show "None" in gray
                canvas.AddText(x, y, "None", AsciiArtAssets.Colors.Gray);
                y++;
            }
            
            y += spacingAfter;
        }
        
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Shows combo sequence and action pool when on inventory page, otherwise shows location/enemy info
        /// </summary>
        private void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName, string title, Character? character)
        {
            // Clear the right panel content area before rendering to prevent text overlap
            int contentX = RIGHT_PANEL_X + 2;
            int contentY = RIGHT_PANEL_Y + 1;
            int contentWidth = RIGHT_PANEL_WIDTH - 4;  // Account for left and right borders
            int contentHeight = RIGHT_PANEL_HEIGHT - 2; // Account for top and bottom borders
            
            canvas.ClearTextInArea(contentX, contentY, contentWidth, contentHeight);
            canvas.ClearProgressBarsInArea(contentX, contentY, contentWidth, contentHeight);
            
            // Main border for right panel
            canvas.AddBorder(RIGHT_PANEL_X, RIGHT_PANEL_Y, RIGHT_PANEL_WIDTH - 2, RIGHT_PANEL_HEIGHT, AsciiArtAssets.Colors.Purple);
            
            int y = RIGHT_PANEL_Y + 1;
            int x = RIGHT_PANEL_X + 2;
            
            // Check if we're on the inventory page
            if (title == "INVENTORY" && character != null)
            {
                // Render combo sequence and action pool for inventory page
                RenderInventoryRightPanel(x, y, character);
            }
            else
            {
                // Render location and enemy info for other pages
                RenderLocationEnemyPanel(x, y, enemy, dungeonName, roomName);
            }
        }
        
        /// <summary>
        /// Renders combo sequence and action pool for inventory page
        /// </summary>
        private void RenderInventoryRightPanel(int x, int y, Character character)
        {
            // Combo Sequence section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("COMBO SEQUENCE"), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var comboActions = character.GetComboActions();
            if (comboActions.Count > 0)
            {
                int currentStepInSequence = (character.ComboStep % comboActions.Count) + 1;
                canvas.AddText(x, y, $"Step: {currentStepInSequence}/{comboActions.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Show combo sequence (limit to fit in panel)
                int maxDisplay = Math.Min(comboActions.Count, 8); // Limit to 8 to fit in panel
                for (int i = 0; i < maxDisplay; i++)
                {
                    var action = comboActions[i];
                    string currentStep = (character.ComboStep % comboActions.Count == i) ? " ←" : "";
                    string actionName = action.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    canvas.AddText(x, y, $"{i + 1}. {actionName}{currentStep}", AsciiArtAssets.Colors.White);
                    y++;
                }
                
                if (comboActions.Count > maxDisplay)
                {
                    canvas.AddText(x, y, $"... +{comboActions.Count - maxDisplay} more", AsciiArtAssets.Colors.Gray);
                    y++;
                }
            }
            else
            {
                canvas.AddText(x, y, "(No combo set)", AsciiArtAssets.Colors.DarkGray);
                y += 2;
            }
            
            y += 1; // Spacing
            
            // Action Pool section
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader("ACTION POOL"), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var actionPool = character.GetActionPool();
            if (actionPool.Count > 0)
            {
                canvas.AddText(x, y, $"Total: {actionPool.Count}", AsciiArtAssets.Colors.White);
                y += 2;
                
                // Group actions by name and show unique actions
                var uniqueActions = actionPool.GroupBy(a => a.Name)
                    .Select(g => new { Name = g.Key, Count = g.Count() })
                    .OrderBy(a => a.Name)
                    .Take(6) // Limit to 6 to fit in panel
                    .ToList();
                
                foreach (var actionGroup in uniqueActions)
                {
                    string actionName = actionGroup.Name;
                    if (actionName.Length > 20)
                        actionName = actionName.Substring(0, 17) + "...";
                    string countText = actionGroup.Count > 1 ? $" x{actionGroup.Count}" : "";
                    canvas.AddText(x, y, $"{actionName}{countText}", AsciiArtAssets.Colors.Cyan);
                    y++;
                }
                
                if (actionPool.Count > uniqueActions.Sum(a => a.Count))
                {
                    int remaining = actionPool.Count - uniqueActions.Sum(a => a.Count);
                    canvas.AddText(x, y, $"... +{remaining} more", AsciiArtAssets.Colors.Gray);
                }
            }
            else
            {
                canvas.AddText(x, y, "(No actions)", AsciiArtAssets.Colors.DarkGray);
            }
        }
        
        /// <summary>
        /// Renders location and enemy information panel
        /// </summary>
        private void RenderLocationEnemyPanel(int x, int y, Enemy? enemy, string? dungeonName, string? roomName)
        {
            // Location section - always shown
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Location), AsciiArtAssets.Colors.Gold);
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
            canvas.AddText(x, y, AsciiArtAssets.UIText.CreateHeader(UIConstants.Headers.Enemy), AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (enemy != null)
            {
                string enemyName = enemy.Name;
                if (enemyName.Length > 20)
                    enemyName = enemyName.Substring(0, 17) + "...";
                
                canvas.AddText(x, y, enemyName, ColorPalette.Enemy.GetColor());
                y++;
                canvas.AddText(x, y, $"Lvl {enemy.Level}", AsciiArtAssets.Colors.Yellow);
                y += 2;
                
                // Enemy health bar
                canvas.AddText(x, y, "HP:", AsciiArtAssets.Colors.White);
                y++;
                
                int enemyHealthBarWidth = RIGHT_PANEL_WIDTH - 6;
                canvas.AddHealthBar(x, y, enemyHealthBarWidth, enemy.CurrentHealth, enemy.MaxHealth, AsciiArtAssets.Colors.Green, AsciiArtAssets.Colors.DarkGreen);
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
        /// Returns the same coordinates that RenderLayout uses for consistency
        /// </summary>
        public (int x, int y, int width, int height) GetCenterContentArea()
        {
            return (CENTER_PANEL_X + 1, CENTER_PANEL_Y + 1, CENTER_PANEL_WIDTH - 2, CENTER_PANEL_HEIGHT - 2);
        }
    }
}

