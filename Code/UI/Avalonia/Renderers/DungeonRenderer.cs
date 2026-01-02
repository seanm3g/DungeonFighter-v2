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
        
        // Animation updates are now handled by centralized state - no methods needed here
        
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
            if (dungeons == null)
            {
                return;
            }
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
                if (textManager == null)
                    return;
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area using efficient ClearTextInArea method
                // Use height + 1 to ensure full area is cleared (endY is exclusive)
                canvas.ClearTextInArea(x, y, width, height + 1);
                
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
                if (textManager == null)
                    return;
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area using efficient ClearTextInArea method
                // Use height + 1 to ensure full area is cleared (endY is exclusive)
                canvas.ClearTextInArea(x, y, width, height + 1);
                
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
        /// <param name="player">The character whose combat is being rendered (used to get the correct display manager)</param>
        public void RenderCombatScreen(int x, int y, int width, int height, Dungeon? dungeon, Environment? room, Enemy enemy, ICanvasTextManager textManager, Character? player = null, List<string>? dungeonContext = null)
        {
            // Use the display buffer system which already handles scrolling and message ordering
            // The display buffer contains all messages in order: dungeon info, room info, enemy encounter, combat log
            // Messages are added to the bottom and the buffer automatically scrolls when content exceeds viewport
            if (textManager is CanvasTextManager canvasTextManager)
            {
                // CRITICAL: Use GetDisplayManagerForCharacter(player) instead of DisplayManager
                // DisplayManager returns the active character's manager, but we need the combat player's manager
                // This ensures we render the correct character's buffer even when another character is active
                var displayManager = canvasTextManager.GetDisplayManagerForCharacter(player);
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
                if (textManager == null)
                    return;
                var displayBuffer = textManager.DisplayBuffer;
                int currentY = y;
                int availableWidth = width - 2;
                int textX = x + 1;
                
                // Clear the content area using efficient ClearTextInArea method
                // Use height + 1 to ensure full area is cleared (endY is exclusive)
                canvas.ClearTextInArea(x, y, width, height + 1);
                
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
        public void RenderDungeonCompletion(int x, int y, int width, int height, Dungeon dungeon, Character player, int xpGained, Item? lootReceived, List<LevelUpInfo> levelUpInfos, List<Item> itemsFoundDuringRun, List<string>? dungeonContext = null)
        {
            currentLineCount = dungeonCompletionRenderer.RenderDungeonCompletion(x, y, width, height, dungeon, player, xpGained, lootReceived, levelUpInfos ?? new List<LevelUpInfo>(), itemsFoundDuringRun ?? new List<Item>(), dungeonContext);
        }
        
        /// <summary>
        /// Renders the death screen with run statistics (condensed version)
        /// </summary>
        public void RenderDeathScreen(int x, int y, int width, int height, Character player, string defeatSummary)
        {
            int currentY = y + 1;
            int startX = x + 2;
            
            // ===== COMPACT HEADER =====
            string headerText = "═══ YOU DIED ═══";
            int headerX = x + (width / 2) - (headerText.Length / 2);
            canvas.AddText(headerX, currentY, headerText, AsciiArtAssets.Colors.Red);
            currentY += 2;
            
            // ===== PARSE AND DISPLAY STATISTICS (CONDENSED) =====
            string[] summaryLines = defeatSummary.Split('\n');
            Color sectionColor = AsciiArtAssets.Colors.White;
            bool inSection = false;
            bool skipNextEmpty = false;
            
            // Stats to skip (less relevant - only skip if zero or meaningless)
            var statsToSkip = new HashSet<string>
            {
                "Total Healing Received: 0",
                "Combo Success Rate: ∞%",
                "Combo Success Rate: NaN%",
                "Rare Items: 0",
                "Epic Items: 0",
                "Legendary Items: 0",
                "Dungeons Completed: 0",
                "Rooms Explored: 0",
                "Encounters Survived: 0"
            };
            
            foreach (string line in summaryLines)
            {
                string trimmedLine = line.Trim();
                
                // Skip old header/footer lines
                if (trimmedLine == "═══════════════════════════════════════" || 
                    trimmedLine == "DEFEAT STATISTICS" ||
                    trimmedLine == "YOU DIED" ||
                    trimmedLine == "Better luck next time!")
                {
                    continue;
                }
                
                // Skip empty lines (but allow one between sections)
                if (string.IsNullOrWhiteSpace(trimmedLine))
                {
                    if (inSection && !skipNextEmpty)
                    {
                        skipNextEmpty = true;
                        continue; // Skip this empty line
                    }
                    skipNextEmpty = false;
                    continue;
                }
                
                // Skip less relevant stats
                bool shouldSkip = false;
                foreach (var skipPattern in statsToSkip)
                {
                    if (trimmedLine.Contains(skipPattern))
                    {
                        shouldSkip = true;
                        break;
                    }
                }
                if (shouldSkip) continue;
                
                skipNextEmpty = false;
                
                // Detect and render section headers (compact, no separator lines)
                if (trimmedLine.Contains("CHARACTER PROGRESSION"))
                {
                    sectionColor = AsciiArtAssets.Colors.Cyan;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("COMBAT PERFORMANCE"))
                {
                    sectionColor = AsciiArtAssets.Colors.Red;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("DAMAGE RECORDS"))
                {
                    // Merge into combat section - skip header, just show stats
                    sectionColor = AsciiArtAssets.Colors.Orange;
                    inSection = true;
                    continue; // Skip the header, just show the stats
                }
                else if (trimmedLine.Contains("COMBO MASTERY"))
                {
                    sectionColor = AsciiArtAssets.Colors.Yellow;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("ITEM COLLECTION"))
                {
                    sectionColor = AsciiArtAssets.Colors.Magenta;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                else if (trimmedLine.Contains("EXPLORATION"))
                {
                    // Skip exploration if all zeros
                    inSection = false;
                    sectionColor = AsciiArtAssets.Colors.White;
                    continue;
                }
                else if (trimmedLine.Contains("ACHIEVEMENTS UNLOCKED"))
                {
                    sectionColor = AsciiArtAssets.Colors.Gold;
                    inSection = true;
                    canvas.AddText(startX, currentY, $"  {trimmedLine}", sectionColor);
                    currentY++;
                    continue;
                }
                
                // Render stat lines
                Color lineColor = AsciiArtAssets.Colors.White;
                if (trimmedLine.Contains("✓"))
                {
                    lineColor = AsciiArtAssets.Colors.Green;
                }
                else if (inSection && sectionColor != AsciiArtAssets.Colors.White)
                {
                    lineColor = AsciiArtAssets.Colors.White;
                }
                
                // Compact formatting - remove extra indentation
                string formattedLine = trimmedLine;
                if (formattedLine.StartsWith("   "))
                {
                    formattedLine = "    " + formattedLine.TrimStart();
                }
                
                canvas.AddText(startX, currentY, formattedLine, lineColor);
                currentY++;
            }
            
            currentY += 1;
            
            // ===== COMPACT FOOTER =====
            string footerText = "Better luck next time!";
            int footerX = x + (width / 2) - (footerText.Length / 2);
            canvas.AddText(footerX, currentY, footerText, AsciiArtAssets.Colors.Yellow);
            currentY += 2;
            
            // ===== CONTINUE BUTTON =====
            string continueText = UIConstants.MenuOptions.ReturnToMainMenu;
            string continueDisplayText = MenuOptionFormatter.Format(0, continueText);
            string buttonText = $"[ {continueDisplayText} ]";
            int buttonX = x + (width / 2) - (buttonText.Length / 2);
            
            var continueButton = new ClickableElement
            {
                X = buttonX,
                Y = currentY,
                Width = buttonText.Length,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "enter",
                DisplayText = continueDisplayText
            };
            
            clickableElements.Add(continueButton);
            canvas.AddText(buttonX, currentY, buttonText, AsciiArtAssets.Colors.Yellow);
            
            // Compact prompt
            currentY += 1;
            string promptText = "Press any key to continue...";
            int promptX = x + (width / 2) - (promptText.Length / 2);
            canvas.AddText(promptX, currentY, promptText, AsciiArtAssets.Colors.Gray);
        }
    }
}
