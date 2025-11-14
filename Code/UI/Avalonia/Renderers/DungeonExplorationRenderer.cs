using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Specialized renderer for dungeon exploration screens
    /// </summary>
    public class DungeonExplorationRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ICanvasInteractionManager interactionManager;
        
        public DungeonExplorationRenderer(GameCanvasControl canvas, ICanvasInteractionManager interactionManager)
        {
            this.canvas = canvas;
            this.interactionManager = interactionManager;
        }
        
        /// <summary>
        /// Renders the dungeon exploration screen
        /// </summary>
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents, CanvasContext context)
        {
            RenderWithLayout(player, "DUNGEON EXPLORATION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderDungeonContent(contentX, contentY, contentWidth, contentHeight, currentLocation, availableActions, recentEvents);
            }, context);
        }
        
        /// <summary>
        /// Renders the dungeon exploration content with location, events, and actions
        /// </summary>
        private void RenderDungeonContent(int x, int y, int width, int height, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            int startY = y; // Store initial Y position for bottom calculations
            
            // Current location
            canvas.AddText(x + 2, y, "═══ CURRENT LOCATION ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            canvas.AddText(x + 2, y, currentLocation, AsciiArtAssets.Colors.White);
            y += 3;
            
            // Recent events
            canvas.AddText(x + 2, y, "═══ RECENT EVENTS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            if (recentEvents.Count == 0)
            {
                canvas.AddText(x + 2, y, "You stand ready for adventure...", AsciiArtAssets.Colors.White);
                y++;
            }
            else
            {
                foreach (var evt in recentEvents.TakeLast(6))
                {
                    // Use full width for event text
                    string eventText = evt;
                    if (eventText.Length > width - 6)
                        eventText = eventText.Substring(0, width - 9) + "...";
                    canvas.AddText(x + 2, y, eventText, AsciiArtAssets.Colors.White);
                    y++;
                }
            }
            y += 2;
            
            // Available actions
            canvas.AddText(x + 2, y, "═══ AVAILABLE ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            for (int i = 0; i < Math.Min(availableActions.Count, 5); i++)
            {
                var action = availableActions[i];
                var actionElement = interactionManager.CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] {action}");
                interactionManager.AddClickableElement(actionElement);
                
                canvas.AddMenuOption(x + 2, y, i + 1, action, AsciiArtAssets.Colors.White, actionElement.IsHovered);
                y++;
            }
            
            // Quick actions at bottom
            y = startY + height - 4;
            canvas.AddText(x + 2, y, "═══ QUICK ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var inventoryButton = interactionManager.CreateButton(x + 2, y, 15, "inventory", "[I] Inventory");
            interactionManager.AddClickableElement(inventoryButton);
            
            canvas.AddText(x + 2, y, "[I] Inventory", AsciiArtAssets.Colors.White);
        }
        
        /// <summary>
        /// Helper method to render with layout context
        /// </summary>
        private void RenderWithLayout(Character character, string title, Action<int, int, int, int> renderContent, CanvasContext context)
        {
            interactionManager.ClearClickableElements();
            // Render content directly without layout manager
            renderContent(0, 0, (int)canvas.Width, (int)canvas.Height);
        }
    }
}
