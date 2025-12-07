namespace RPGGame.UI.Avalonia.Layout
{
    using System;
    using System.Linq;
    using RPGGame;
    using RPGGame.UI;
    using RPGGame.UI.ColorSystem;

    /// <summary>
    /// Renders the right panel (location/enemy info or inventory actions)
    /// </summary>
    public class RightPanelRenderer
    {
        private readonly GameCanvasControl canvas;
        
        public RightPanelRenderer(GameCanvasControl canvas)
        {
            this.canvas = canvas;
        }
        
        /// <summary>
        /// Renders the dungeon and enemy information panel (right side)
        /// Shows combo sequence and action pool when on inventory page or combo management, otherwise shows location/enemy info
        /// </summary>
        public void RenderRightPanel(Enemy? enemy, string? dungeonName, string? roomName, string title, Character? character)
        {
            // Clear the right panel content area before rendering to prevent text overlap
            int contentX = LayoutConstants.RIGHT_PANEL_X + 2;
            int contentY = LayoutConstants.RIGHT_PANEL_Y + 1;
            int contentWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 4;  // Account for left and right borders
            int contentHeight = LayoutConstants.RIGHT_PANEL_HEIGHT - 2; // Account for top and bottom borders
            
            canvas.ClearTextInArea(contentX, contentY, contentWidth, contentHeight);
            canvas.ClearProgressBarsInArea(contentX, contentY, contentWidth, contentHeight);
            
            // Main border for right panel
            canvas.AddBorder(LayoutConstants.RIGHT_PANEL_X, LayoutConstants.RIGHT_PANEL_Y, LayoutConstants.RIGHT_PANEL_WIDTH - 2, LayoutConstants.RIGHT_PANEL_HEIGHT, AsciiArtAssets.Colors.Purple);
            
            int y = LayoutConstants.RIGHT_PANEL_Y + 1;
            int x = LayoutConstants.RIGHT_PANEL_X + 2;
            
            // Check if we're on the inventory page or combo management page
            if ((title == "INVENTORY" || title == "COMBO MANAGEMENT") && character != null)
            {
                // Render combo sequence and action pool for inventory page and combo management
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
                
                int enemyHealthBarWidth = LayoutConstants.RIGHT_PANEL_WIDTH - 6;
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
    }
}

