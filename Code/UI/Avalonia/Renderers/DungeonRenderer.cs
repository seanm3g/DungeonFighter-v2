using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Consolidated dungeon renderer that handles all dungeon-related screens
    /// Merged from multiple specialized renderers for better maintainability
    /// </summary>
    public class DungeonRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly ColoredTextWriter textWriter;
        private readonly List<ClickableElement> clickableElements;
        private int currentLineCount;
        
        // Specialized renderers (only keep complex ones)
        private readonly DungeonSelectionRenderer selectionRenderer;
        private readonly DungeonCompletionRenderer dungeonCompletionRenderer;
        
        public DungeonRenderer(GameCanvasControl canvas, ColoredTextWriter textWriter, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.textWriter = textWriter;
            this.clickableElements = clickableElements;
            this.currentLineCount = 0;
            
            // Initialize specialized renderers (only complex ones)
            this.selectionRenderer = new DungeonSelectionRenderer(canvas, textWriter, clickableElements);
            this.dungeonCompletionRenderer = new DungeonCompletionRenderer(canvas, textWriter, clickableElements);
        }
        
        // IScreenRenderer implementation
        public void Render()
        {
            // This is a placeholder - specific render methods are called directly
            // Future refactor could use a state machine pattern here
        }
        
        public void Clear()
        {
            clickableElements.Clear();
            currentLineCount = 0;
        }
        
        /// <summary>
        /// Updates the undulation animation for dungeon names
        /// Call this each frame to create a shimmering effect
        /// </summary>
        public void UpdateUndulation()
        {
            selectionRenderer.UpdateUndulation();
        }
        
        /// <summary>
        /// Updates the brightness mask animation (separate from undulation)
        /// </summary>
        public void UpdateBrightnessMask()
        {
            selectionRenderer.UpdateBrightnessMask();
        }
        
        public int GetLineCount()
        {
            return currentLineCount;
        }
        
        // IInteractiveRenderer implementation
        public List<ClickableElement> GetClickableElements()
        {
            return clickableElements;
        }
        
        public void UpdateHoverState(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                element.IsHovered = element.Contains(x, y);
            }
        }
        
        public bool HandleClick(int x, int y)
        {
            foreach (var element in clickableElements)
            {
                if (element.Contains(x, y))
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Renders the dungeon selection screen
        /// </summary>
        public void RenderDungeonSelection(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            currentLineCount = selectionRenderer.RenderDungeonSelection(x, y, width, height, dungeons);
        }
        
        /// <summary>
        /// Renders the dungeon start screen using the display buffer system
        /// All content is rendered from the display buffer for consistency
        /// </summary>
        public void RenderDungeonStart(int x, int y, int width, int height, Dungeon dungeon, ICanvasTextManager textManager, List<string>? dungeonHeaderInfo = null)
        {
            // Use the display buffer system which handles scrolling and message ordering
            // The display buffer contains all messages in order
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Get the display buffer from the display manager
                var displayManager = canvasTextManager.DisplayManager;
                var buffer = displayManager.Buffer;
                
                // Create a display renderer to render the buffer with scrolling support
                var displayRenderer = new DisplayRenderer(textWriter);
                
                // Render the display buffer - it will handle scrolling automatically
                // The DisplayRenderer clears the content area internally and renders with proper scrolling
                displayRenderer.Render(buffer, x, y, width, height);
            }
            else
            {
                // Fallback: render display buffer directly if not using CanvasTextManager
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area
                for (int clearY = y; clearY < y + height; clearY++)
                {
                    canvas.AddText(x, clearY, new string(' ', width), AsciiArtAssets.Colors.White);
                }
                
                // Render all messages from the buffer in order
                foreach (var message in displayBuffer)
                {
                    if (currentY >= y + height)
                        break;
                    
                    int linesRendered = textWriter.WriteLineColoredWrapped(message, textX, currentY, availableWidth);
                    currentY += linesRendered;
                }
            }
        }
        
        /// <summary>
        /// Prepares the room entry screen (pure reactive mode)
        /// In pure reactive mode, this method does NOT render - it only ensures data is ready.
        /// The CenterPanelDisplayManager will handle all rendering reactively when the buffer changes.
        /// </summary>
        /// <param name="startFromBufferIndex">Not used in reactive mode - kept for API compatibility</param>
        public void RenderRoomEntry(int x, int y, int width, int height, Environment room, ICanvasTextManager textManager, int? startFromBufferIndex = null)
        {
            // Pure reactive mode: Do NOT render here
            // The CenterPanelDisplayManager will automatically render when buffer changes
            // This method is kept for API compatibility but is now a no-op for rendering
            // The layout is set up by RenderWithLayout() in CanvasRenderer, and the reactive
            // system (CenterPanelDisplayManager.PerformRender()) will render the buffer
            
            // No rendering code - let the reactive system handle it
        }
        
        /// <summary>
        /// Renders the enemy encounter screen using the display buffer system
        /// All content is rendered from the display buffer for consistency
        /// </summary>
        public void RenderEnemyEncounter(int x, int y, int width, int height, Enemy enemy, ICanvasTextManager textManager, List<string>? dungeonContext = null)
        {
            // Use the display buffer system which handles scrolling and message ordering
            // The display buffer contains all messages in order (dungeon info, room info, enemy encounter, etc.)
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Get the display buffer from the display manager
                var displayManager = canvasTextManager.DisplayManager;
                var buffer = displayManager.Buffer;
                
                // Create a display renderer to render the buffer with scrolling support
                var displayRenderer = new DisplayRenderer(textWriter);
                
                // Render the display buffer - it will handle scrolling automatically
                // The DisplayRenderer clears the content area internally and renders with proper scrolling
                displayRenderer.Render(buffer, x, y, width, height);
            }
            else
            {
                // Fallback: render display buffer directly if not using CanvasTextManager
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area
                for (int clearY = y; clearY < y + height; clearY++)
                {
                    canvas.AddText(x, clearY, new string(' ', width), AsciiArtAssets.Colors.White);
                }
                
                // Render all messages from the buffer in order
                foreach (var message in displayBuffer)
                {
                    if (currentY >= y + height)
                        break;
                    
                    int linesRendered = textWriter.WriteLineColoredWrapped(message, textX, currentY, availableWidth);
                    currentY += linesRendered;
                }
            }
        }
        
        /// <summary>
        /// Renders the complete combat screen by rendering the display buffer
        /// The display buffer already contains all messages in order (dungeon info, room info, enemy encounter, combat log)
        /// and handles scrolling automatically. Messages are added to the bottom and scroll when necessary.
        /// </summary>
        public void RenderCombatScreen(int x, int y, int width, int height, Dungeon? dungeon, Environment? room, Enemy enemy, ICanvasTextManager textManager, List<string>? dungeonContext = null)
        {
            // Use the display buffer system which already handles scrolling and message ordering
            // The display buffer contains all messages in order: dungeon info, room info, enemy encounter, combat log
            // Messages are added to the bottom and the buffer automatically scrolls when content exceeds viewport
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // Get the display buffer from the display manager
                var displayManager = canvasTextManager.DisplayManager;
                var buffer = displayManager.Buffer;
                
                // Create a display renderer to render the buffer with scrolling support
                var displayRenderer = new DisplayRenderer(textWriter);
                
                // Render the display buffer - it will handle scrolling automatically
                // The DisplayRenderer clears the content area internally and renders with proper scrolling
                displayRenderer.Render(buffer, x, y, width, height);
            }
            else
            {
                // Fallback: render display buffer directly if not using CanvasTextManager
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area
                for (int clearY = y; clearY < y + height; clearY++)
                {
                    canvas.AddText(x, clearY, new string(' ', width), AsciiArtAssets.Colors.White);
                }
                
                // Render all messages from the buffer in order
                // This will naturally scroll as content exceeds the viewport
                foreach (var message in displayBuffer)
                {
                    if (currentY >= y + height)
                        break;
                    
                    int linesRendered = textWriter.WriteLineColoredWrapped(message, textX, currentY, availableWidth);
                    currentY += linesRendered;
                }
            }
        }
        
        /// <summary>
        /// Renders the room completion screen (merged from RoomCompletionRenderer)
        /// </summary>
        public void RenderRoomCompletion(int x, int y, int width, int height, Environment room, Character currentCharacter)
        {
            currentLineCount = 0;
            
            // Match original console UI pattern for room completion
            if (currentCharacter != null)
            {
                canvas.AddText(x + 2, y, string.Format(AsciiArtAssets.UIText.RemainingHealth,
                    currentCharacter.CurrentHealth, currentCharacter.GetEffectiveMaxHealth()),
                    AsciiArtAssets.Colors.White);
                y += 2;
                currentLineCount += 2;
            }
            
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.RoomClearedMessage, AsciiArtAssets.Colors.Green);
            y++;
            currentLineCount++;
            canvas.AddText(x + 2, y, AsciiArtAssets.UIText.Divider, AsciiArtAssets.Colors.Green);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders the dungeon completion screen with detailed statistics and menu choices
        /// </summary>
        public void RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived)
        {
            currentLineCount = dungeonCompletionRenderer.RenderDungeonCompletion(x, y, width, height, dungeon, player, xpGained, lootReceived);
        }
        
        /// <summary>
        /// Renders the death screen with run statistics
        /// </summary>
        public void RenderDeathScreen(int x, int y, int width, int height, Character player, string defeatSummary)
        {
            int currentY = y + 2;
            int startX = x + 4;
            
            // Death header
            canvas.AddText(x + (width / 2) - 15, currentY, "═══════════════════════════════════════", AsciiArtAssets.Colors.Red);
            currentY += 1;
            canvas.AddText(x + (width / 2) - 7, currentY, "              YOU DIED", AsciiArtAssets.Colors.Red);
            currentY += 1;
            canvas.AddText(x + (width / 2) - 15, currentY, "═══════════════════════════════════════", AsciiArtAssets.Colors.Red);
            currentY += 3;
            
            // Display defeat summary line by line
            string[] summaryLines = defeatSummary.Split('\n');
            foreach (string line in summaryLines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    // Determine color based on line content
                    Color lineColor = AsciiArtAssets.Colors.White;
                    if (line.Contains("YOU DIED") || line.Contains("DEFEAT"))
                    {
                        lineColor = AsciiArtAssets.Colors.Red;
                    }
                    else if (line.Contains("═══") || line.Contains("STATISTICS"))
                    {
                        lineColor = AsciiArtAssets.Colors.Yellow;
                    }
                    else if (line.Contains("ACHIEVEMENTS"))
                    {
                        lineColor = AsciiArtAssets.Colors.Gold;
                    }
                    else if (line.Contains("✓"))
                    {
                        lineColor = AsciiArtAssets.Colors.Green;
                    }
                    
                    canvas.AddText(startX, currentY, line, lineColor);
                    currentY++;
                }
            }
            
            currentY += 2;
            
            // Prompt to continue
            canvas.AddText(x + (width / 2) - 20, currentY, "Press any key to return to main menu...", AsciiArtAssets.Colors.Yellow);
        }
    }
}
