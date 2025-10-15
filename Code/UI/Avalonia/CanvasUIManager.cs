using Avalonia.Media;
using Avalonia.Threading;
using RPGGame;
using RPGGame.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RPGGame.UI.Avalonia
{
    // Supporting classes for mouse interaction
    public enum ElementType
    {
        MenuOption,
        Item,
        Button,
        Text
    }

    public class ClickableElement
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public ElementType Type { get; set; }
        public string Value { get; set; } = "";
        public string DisplayText { get; set; } = "";
        public bool IsHovered { get; set; } = false;
        
        /// <summary>
        /// Checks if the given coordinates are within this element's bounds
        /// </summary>
        public bool Contains(int x, int y)
        {
            return x >= X && x < X + Width && y >= Y && y < Y + Height;
        }
    }

    /// <summary>
    /// Canvas-based UI manager that implements IUIManager to render to ASCII canvas instead of console
    /// </summary>
    public class CanvasUIManager : IUIManager
    {
        private readonly GameCanvasControl canvas;
        private readonly PersistentLayoutManager layoutManager;
        private readonly Renderers.MenuRenderer menuRenderer;
        private readonly Renderers.ColoredTextWriter textWriter;
        private readonly Renderers.CombatRenderer combatRenderer;
        private readonly Renderers.InventoryRenderer inventoryRenderer;
        private readonly Renderers.DungeonRenderer dungeonRenderer;
        private readonly int maxLines = 100; // Maximum lines to display (increased for larger resolution)
        private readonly List<string> displayBuffer = new();
        private readonly List<ClickableElement> clickableElements = new();
        private int hoverX = -1;
        private int hoverY = -1;
        private System.Action? closeAction = null;
        
        // Layout constants
        private const int LEFT_MARGIN = 2;
        private const int RIGHT_MARGIN = 2;
        private const int TOP_MARGIN = 2;
        private const int BOTTOM_MARGIN = 2;
        private const int CONTENT_WIDTH = 206; // 210 - 4 margins
        private const int CONTENT_HEIGHT = 56; // 60 - 4 margins
        
        // Screen dimensions and center point
        private const int SCREEN_WIDTH = 210;  // Total character width of the screen
        private const int SCREEN_CENTER = SCREEN_WIDTH / 2;  // The center point for menus
        
        // Current character reference for persistent display
        private Character? currentCharacter = null;
        
        // Animation state for dungeon selection
        private bool isDungeonSelectionActive = false;
        private Character? dungeonSelectionPlayer = null;
        private List<Dungeon>? dungeonSelectionList = null;
        private System.Threading.Timer? undulationTimer = null;
        private System.Threading.Timer? brightnessMaskTimer = null;

        public CanvasUIManager(GameCanvasControl canvas)
        {
            this.canvas = canvas;
            this.layoutManager = new PersistentLayoutManager(canvas);
            this.textWriter = new Renderers.ColoredTextWriter(canvas);
            this.menuRenderer = new Renderers.MenuRenderer(canvas, clickableElements);
            this.combatRenderer = new Renderers.CombatRenderer(canvas, textWriter);
            this.inventoryRenderer = new Renderers.InventoryRenderer(canvas, textWriter, clickableElements);
            this.dungeonRenderer = new Renderers.DungeonRenderer(canvas, textWriter, clickableElements);
            
            // Start undulation animation timer (speed configured in UIConfiguration.json)
            int undulationInterval = UIManager.UIConfig.UndulationTimerMs;
            undulationTimer = new System.Threading.Timer(UpdateUndulation, null, undulationInterval, undulationInterval);
            
            // Start brightness mask timer (separate speed for cloud movement)
            int brightnessMaskInterval = UIManager.UIConfig.BrightnessMask.UpdateIntervalMs;
            brightnessMaskTimer = new System.Threading.Timer(UpdateBrightnessMask, null, brightnessMaskInterval, brightnessMaskInterval);
        }
        
        /// <summary>
        /// Sets the close action for the UI manager
        /// </summary>
        public void SetCloseAction(System.Action action)
        {
            closeAction = action;
        }
        
        /// <summary>
        /// Gets the center X coordinate for centering text
        /// </summary>
        public int CenterX => canvas.CenterX;
        
        /// <summary>
        /// Closes the UI window
        /// </summary>
        public void Close()
        {
            closeAction?.Invoke();
        }
        
        /// <summary>
        /// Sets the current character for persistent display
        /// </summary>
        public void SetCharacter(Character? character)
        {
            currentCharacter = character;
        }
        
        /// <summary>
        /// Helper method to render content with persistent layout
        /// </summary>
        private void RenderWithLayout(Character character, string title, Action<int, int, int, int> renderContent, Enemy? enemy = null, string? dungeonName = null, string? roomName = null)
        {
            currentCharacter = character;
            ClearClickableElements();
            layoutManager.RenderLayout(character, renderContent, title, enemy, dungeonName, roomName);
        }
        
        /// <summary>
        /// Renders the opening animation on the canvas with proper centering and color markup
        /// </summary>
        /// <summary>
        /// Renders the opening title screen animation
        /// </summary>
        public void RenderOpeningAnimation()
        {
            canvas.Clear();
            
            // Use title art from AsciiArtAssets
            string[] asciiArt = AsciiArtAssets.TitleArt.DungeonFighterTitle;
            
            // Use absolute positioning - no centering
            // This allows precise control of ASCII art placement
            int startX = 0;  // Absolute X position (left edge)
            int startY = 5;  // Absolute Y position (from top)
            
            // Render each line at absolute position
            for (int i = 0; i < asciiArt.Length; i++)
            {
                string line = asciiArt[i];
                if (!string.IsNullOrEmpty(line))
                {
                    // Render line with color markup at absolute position
                    WriteLineColored(line, startX, startY + i);
                }
            }
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Adds the "Press any key to continue" message below the opening animation
        /// </summary>
        public void ShowPressKeyMessage()
        {
            // Center the message horizontally
            int startY = 48; // Absolute Y position (8 lines below title)
            string message = "&Y[Press any key to continue]";
            
            // Calculate visible length (excluding markup) for centering around CenterX
            int visibleLength = ColorParser.GetDisplayLength(message);
            int startX = Math.Max(0, canvas.CenterX - (visibleLength / 2));
            
            WriteLineColored(message, startX, startY);
            canvas.Refresh();
        }
        
        /// <summary>
        /// Clears the canvas for rendering
        /// </summary>
        public void Clear()
        {
            canvas.Clear();
        }
        
        /// <summary>
        /// Refreshes the canvas to display all rendered content
        /// </summary>
        public void Refresh()
        {
            canvas.Refresh();
        }
        
        public void WriteLine(string message, UIMessageType messageType = UIMessageType.System)
        {
            AddToDisplayBuffer(message, messageType);
            ApplyDelay(messageType);
        }

        /// <summary>
        /// Adds colored text to canvas with color markup support
        /// </summary>
        public void WriteLineColored(string message, int x, int y)
        {
            textWriter.WriteLineColored(message, x, y);
        }

        /// <summary>
        /// Adds colored text to canvas with color markup support and text wrapping
        /// Returns the number of lines rendered
        /// </summary>
        public int WriteLineColoredWrapped(string message, int x, int y, int maxWidth)
        {
            return textWriter.WriteLineColoredWrapped(message, x, y, maxWidth);
        }

        public void Write(string message)
        {
            // For canvas, we'll treat Write as WriteLine for simplicity
            WriteLine(message);
        }

        public void WriteSystemLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }

        public void WriteMenuLine(string message)
        {
            AddToDisplayBuffer(message, UIMessageType.Menu);
            ApplyProgressiveMenuDelay();
        }

        public void WriteTitleLine(string message)
        {
            WriteLine(message, UIMessageType.Title);
        }

        public void WriteDungeonLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }

        public void WriteRoomLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }

        public void WriteEnemyLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }

        public void WriteRoomClearedLine(string message)
        {
            WriteLine(message, UIMessageType.System);
        }

        public void WriteEffectLine(string message)
        {
            WriteLine(message, UIMessageType.EffectMessage);
        }

        public void WriteBlankLine()
        {
            AddToDisplayBuffer("", UIMessageType.System);
        }
        
        public void WriteChunked(string message, UI.ChunkedTextReveal.RevealConfig? config = null)
        {
            // For GUI, we'll display text chunk by chunk with delays
            // But since we're rendering to a canvas, we need to handle this differently
            // For now, we'll split into chunks and add them to the display buffer with delays
            
            config ??= UI.ChunkedTextReveal.DefaultConfig;
            
            // If chunked reveal is disabled, just write normally
            if (!config.Enabled)
            {
                WriteLine(message);
                return;
            }
            
            // Split text into chunks
            var chunks = SplitIntoChunks(message, config.Strategy);
            
            // Add each chunk to display buffer with delays
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                
                // Add chunk to display buffer
                AddToDisplayBuffer(chunk, UIMessageType.System);
                
                // Add blank line if configured
                if (config.AddBlankLineBetweenChunks && i < chunks.Count - 1)
                {
                    AddToDisplayBuffer("", UIMessageType.System);
                }
                
                // Apply delay if not the last chunk
                if (i < chunks.Count - 1)
                {
                    int delay = CalculateChunkDelay(chunk, config);
                    Thread.Sleep(delay);
                }
            }
        }
        
        /// <summary>
        /// Splits text into chunks based on strategy (helper for WriteChunked)
        /// </summary>
        private List<string> SplitIntoChunks(string text, UI.ChunkedTextReveal.ChunkStrategy strategy)
        {
            // Use reflection to call the private method in ChunkedTextReveal
            // Or we can duplicate the logic here for simplicity
            var chunks = new List<string>();
            
            switch (strategy)
            {
                case UI.ChunkedTextReveal.ChunkStrategy.Sentence:
                    // Split by sentences
                    chunks = System.Text.RegularExpressions.Regex.Split(text, @"(?<=[.!?])\s+(?=[A-Z\n])")
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Paragraph:
                    // Split by paragraphs
                    chunks = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Trim())
                        .Where(p => !string.IsNullOrEmpty(p))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Line:
                    // Split by lines
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
                    
                case UI.ChunkedTextReveal.ChunkStrategy.Semantic:
                    // Split by semantic sections (simple version for now)
                    chunks = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrEmpty(l))
                        .ToList();
                    break;
            }
            
            return chunks;
        }
        
        /// <summary>
        /// Calculates delay for a chunk based on its length
        /// </summary>
        private int CalculateChunkDelay(string chunk, UI.ChunkedTextReveal.RevealConfig config)
        {
            // Get display length (excluding color markup)
            int displayLength = ColorParser.GetDisplayLength(chunk);
            
            // Calculate delay: base delay per character * number of characters
            int calculatedDelay = displayLength * config.BaseDelayPerCharMs;
            
            // Clamp to min/max
            int delay = Math.Max(config.MinDelayMs, Math.Min(config.MaxDelayMs, calculatedDelay));
            
            return delay;
        }

        // Store dungeon context separately so it doesn't get cleared during combat
        private List<string> dungeonContext = new List<string>();
        private Enemy? currentEnemy = null;
        private string? currentDungeonName = null;
        private string? currentRoomName = null;
        
        public void SetDungeonContext(List<string> context)
        {
            dungeonContext = new List<string>(context);
        }
        
        public void SetCurrentEnemy(Enemy enemy)
        {
            currentEnemy = enemy;
        }
        
        public void SetDungeonName(string? dungeonName)
        {
            currentDungeonName = dungeonName;
        }
        
        public void SetRoomName(string? roomName)
        {
            currentRoomName = roomName;
        }
        
        public void ResetForNewBattle()
        {
            // Clear combat actions but keep dungeon context
            displayBuffer.Clear();
            // Add dungeon context back to display buffer
            foreach (var line in dungeonContext)
            {
                displayBuffer.Add(line);
            }
        }
        
        public void ClearCurrentEnemy()
        {
            currentEnemy = null;
            currentDungeonName = null;
            currentRoomName = null;
        }
        
        /// <summary>
        /// Adds a victory message to the combat log without clearing the screen
        /// </summary>
        public void AddVictoryMessage(Enemy enemy, BattleNarrative? battleNarrative)
        {
            // Add blank line for separation
            WriteLine("", UIMessageType.Combat);
            
            // Add victory message
            string victoryMsg = string.Format(AsciiArtAssets.UIText.VictoryPrefix, enemy.Name);
            WriteLine($"&G{victoryMsg}", UIMessageType.Combat);
            WriteLine(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            // Add battle narrative highlights if available
            if (battleNarrative != null)
            {
                var narrativeLines = battleNarrative.GetTriggeredNarratives();
                if (narrativeLines != null && narrativeLines.Count > 0)
                {
                    WriteLine("", UIMessageType.Combat);
                    WriteLine("&C" + AsciiArtAssets.UIText.CreateHeader(AsciiArtAssets.UIText.BattleHighlightsHeader), UIMessageType.Combat);
                    foreach (var line in narrativeLines.Take(3))
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            WriteLine($"&C  {line}", UIMessageType.Combat);
                        }
                    }
                }
            }
            
            WriteLine("", UIMessageType.Combat);
        }
        
        /// <summary>
        /// Adds a defeat message to the combat log without clearing the screen
        /// </summary>
        public void AddDefeatMessage()
        {
            WriteLine("", UIMessageType.Combat);
            WriteLine($"&R{AsciiArtAssets.UIText.DefeatMessage}", UIMessageType.Combat);
            WriteLine(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            WriteLine("", UIMessageType.Combat);
        }
        
        /// <summary>
        /// Adds a room cleared message to the combat log without clearing the screen
        /// </summary>
        public void AddRoomClearedMessage()
        {
            WriteLine("", UIMessageType.Combat);
            WriteLine($"&G{AsciiArtAssets.UIText.RoomClearedMessage}", UIMessageType.Combat);
            WriteLine(AsciiArtAssets.UIText.Divider, UIMessageType.Combat);
            
            if (currentCharacter != null)
            {
                string healthMsg = string.Format(AsciiArtAssets.UIText.RemainingHealth, 
                    currentCharacter.CurrentHealth, currentCharacter.GetEffectiveMaxHealth());
                WriteLine($"&W{healthMsg}", UIMessageType.Combat);
            }
            
            WriteLine("", UIMessageType.Combat);
        }

        public void ResetMenuDelayCounter()
        {
            // Reset menu delay counter
            // This would be implemented if we had menu delay logic
        }

        public int GetConsecutiveMenuLineCount()
        {
            // Return consecutive menu line count
            return 0; // Simplified for now
        }

        public int GetBaseMenuDelay()
        {
            // Return base menu delay
            return 0; // Simplified for now
        }

        private void AddToDisplayBuffer(string message, UIMessageType messageType)
        {
            // Truncate message if too long (use display length to handle color markup)
            if (ColorParser.GetDisplayLength(message) > CONTENT_WIDTH)
            {
                // Strip markup before truncating to avoid cutting markup codes in the middle
                string strippedMessage = ColorParser.StripColorMarkup(message);
                message = strippedMessage.Substring(0, Math.Min(strippedMessage.Length, CONTENT_WIDTH - 3)) + "...";
            }

            // Prevent consecutive duplicate messages
            if (displayBuffer.Count > 0 && displayBuffer[displayBuffer.Count - 1] == message)
            {
                return; // Skip duplicate
            }

            displayBuffer.Add(message);
            
            // Keep only the last maxLines
            if (displayBuffer.Count > maxLines)
            {
                displayBuffer.RemoveAt(0);
            }
            
            RenderDisplayBuffer();
        }

        private void ApplyDelay(UIMessageType messageType)
        {
            // Don't apply delays for canvas UI - they block the UI thread
            // Visual pacing is handled by the canvas refresh rate instead
        }

        private void ApplyProgressiveMenuDelay()
        {
            // Apply progressive menu delay
            // Simplified for now
        }

        private int GetDelayForMessageType(UIMessageType messageType)
        {
            return messageType switch
            {
                UIMessageType.System => 100,
                UIMessageType.Menu => 50,
                UIMessageType.Title => 200,
                UIMessageType.EffectMessage => 150,
                _ => 0
            };
        }

        private void RenderDisplayBuffer()
        {
            // Dispatch to UI thread to avoid cross-thread issues
            Dispatcher.UIThread.Post(() =>
            {
                canvas.Clear();
                
                // If we have a character (in combat or during gameplay), use persistent layout
                if (currentCharacter != null)
                {
                    layoutManager.RenderLayout(currentCharacter, (contentX, contentY, contentWidth, contentHeight) =>
                    {
                        // Render display buffer in the center content area with color parsing
                        int y = contentY;
                        int availableWidth = contentWidth - 4; // 2 chars padding on each side
                        
                        foreach (var line in displayBuffer.TakeLast(maxLines))
                        {
                            if (y < contentY + contentHeight - 1)
                            {
                                // Parse and render color markup with text wrapping
                                int linesRendered = WriteLineColoredWrapped(line, contentX + 2, y, availableWidth);
                                y += linesRendered;
                            }
                        }
                    }, "COMBAT", currentEnemy, currentDungeonName, currentRoomName); // Pass current enemy, dungeon, and room info
                }
                else
                {
                    // Fallback to simple rendering when no character is set
                    // Add title
                    //canvas.AddTitle(0, "DUNGEON FIGHTER v2 - ASCII EDITION", AsciiArtAssets.Colors.Gold);
                    
                    // Add main content area border
                    canvas.AddBorder(LEFT_MARGIN, TOP_MARGIN + 2, CONTENT_WIDTH, CONTENT_HEIGHT, AsciiArtAssets.Colors.White);
                    
                    // Render display buffer with color parsing
                    int y = TOP_MARGIN + 3;
                    foreach (var line in displayBuffer.TakeLast(maxLines))
                    {
                        if (y < TOP_MARGIN + CONTENT_HEIGHT - 1)
                        {
                            // Parse and render color markup
                            WriteLineColored(line, LEFT_MARGIN + 1, y);
                            y++;
                        }
                    }
                }
                
                canvas.Refresh();
            }, DispatcherPriority.Background);
        }

        // Specialized rendering methods for different game displays
        public void RenderMainMenu()
        {
            RenderMainMenu(false, null, 0);
        }

        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            // Delegate to MenuRenderer
            menuRenderer.RenderMainMenu(hasSavedGame, characterName, characterLevel);
        }

        public void RenderInventory(Character character, List<Item> inventory)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderInventoryContent(contentX, contentY, contentWidth, contentHeight, character, inventory);
            });
        }
        
        private void RenderInventoryContent(int x, int y, int width, int height, Character character, List<Item> inventory)
        {
            // Delegate to InventoryRenderer
            inventoryRenderer.RenderInventory(x, y, width, height, character, inventory);
        }
        
        public void RenderItemSelectionPrompt(Character character, List<Item> inventory, string promptMessage, string actionType)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderItemSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character, inventory, promptMessage, actionType);
            });
        }
        
        public void RenderSlotSelectionPrompt(Character character)
        {
            RenderWithLayout(character, "INVENTORY", (contentX, contentY, contentWidth, contentHeight) =>
            {
                inventoryRenderer.RenderSlotSelectionPrompt(contentX, contentY, contentWidth, contentHeight, character);
            });
        }

        public void RenderCombat(Character player, Enemy enemy, List<string> combatLog)
        {
            RenderWithLayout(player, "COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderCombatContent(contentX, contentY, contentWidth, contentHeight, player, enemy, combatLog);
            }, enemy);
        }
        
        private void RenderCombatContent(int x, int y, int width, int height, Character player, Enemy enemy, List<string> combatLog)
        {
            // Delegate to CombatRenderer
            combatRenderer.RenderCombat(x, y, width, height, player, enemy, combatLog);
        }

        public void ClearDisplay()
        {
            canvas.Clear();
            displayBuffer.Clear();
            canvas.Refresh();
        }

        public void ShowMessage(string message, Color color = default)
        {
            if (color == default) color = AsciiArtAssets.Colors.White;
            
            canvas.Clear();
            canvas.AddCenteredText(20, message, color);
            canvas.Refresh();
        }

        public void ShowError(string error)
        {
            ShowMessage($"ERROR: {error}", AsciiArtAssets.Colors.Red);
        }

        public void ShowSuccess(string message)
        {
            ShowMessage(message, AsciiArtAssets.Colors.Green);
        }

        // Additional methods for ASCII UI functionality
        private bool showHelp = false;

        public void ToggleHelp()
        {
            showHelp = !showHelp;
            if (showHelp)
            {
                RenderHelp();
            }
            else
            {
                RenderMainMenu();
            }
        }

        public void UpdateStatus(string message)
        {
            // Add status message to the bottom of the display
            canvas.AddText(LEFT_MARGIN + 2, CONTENT_HEIGHT - 2, message, AsciiArtAssets.Colors.Gray);
            canvas.Refresh();
        }

        public void RenderHelp()
        {
            canvas.Clear();
            
            // Title
            canvas.AddTitle(2, "HELP - DUNGEON FIGHTER", AsciiArtAssets.Colors.White);
            
            // Help content
            canvas.AddText(LEFT_MARGIN + 2, 6, "Controls:", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 8, "1-6: Select menu option", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 9, "H: Toggle this help screen", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 10, "ESC: Go back/Exit", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 11, "Arrow Keys: Navigate menus", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 12, "Enter: Confirm selection", AsciiArtAssets.Colors.White);
            
            canvas.AddText(LEFT_MARGIN + 2, 15, "Game Features:", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 17, "• Enter Dungeon: Start exploring", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 18, "• View Inventory: Check items", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 19, "• Character Info: View stats", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 20, "• Tuning Console: Game settings", AsciiArtAssets.Colors.White);
            canvas.AddText(LEFT_MARGIN + 4, 21, "• Save Game: Save progress", AsciiArtAssets.Colors.White);
            
            canvas.AddText(LEFT_MARGIN + 2, 24, "Press H to return to main menu", AsciiArtAssets.Colors.White);
            
            canvas.Refresh();
        }

        // Mouse interaction methods
        public ClickableElement? GetElementAt(int x, int y)
        {
            return clickableElements.FirstOrDefault(element => 
                x >= element.X && x < element.X + element.Width &&
                y >= element.Y && y < element.Y + element.Height);
        }

        public void SetHoverPosition(int x, int y)
        {
            bool hoverChanged = false;
            
            // Update hover state for all elements
            foreach (var element in clickableElements)
            {
                bool wasHovered = element.IsHovered;
                element.IsHovered = (x >= element.X && x < element.X + element.Width &&
                                   y >= element.Y && y < element.Y + element.Height);
                
                if (wasHovered != element.IsHovered)
                {
                    hoverChanged = true;
                }
            }
            
            hoverX = x;
            hoverY = y;
            
            // Only refresh if hover state changed
            if (hoverChanged)
            {
                // Re-render the current screen with updated hover states
                ReRenderCurrentScreen();
            }
        }

        private void ReRenderCurrentScreen()
        {
            // This is a simplified approach - in a more complex system,
            // you'd want to track the current screen type and re-render accordingly
            // For now, we'll just refresh the canvas
            canvas.Refresh();
        }

        public void ClearClickableElements()
        {
            clickableElements.Clear();
        }

        private void AddClickableElement(int x, int y, int width, int height, ElementType type, string value, string displayText)
        {
            clickableElements.Add(new ClickableElement
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Type = type,
                Value = value,
                DisplayText = displayText
            });
        }
        
        /// <summary>
        /// Helper method to create a clickable menu option
        /// </summary>
        private ClickableElement CreateMenuOption(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement 
            { 
                X = x, 
                Y = y, 
                Width = width, 
                Height = 1, 
                Type = ElementType.MenuOption, 
                Value = value, 
                DisplayText = displayText 
            };
        }
        
        /// <summary>
        /// Helper method to create a clickable button
        /// </summary>
        private ClickableElement CreateButton(int x, int y, int width, string value, string displayText)
        {
            return new ClickableElement 
            { 
                X = x, 
                Y = y, 
                Width = width, 
                Height = 1, 
                Type = ElementType.Button, 
                Value = value, 
                DisplayText = displayText 
            };
        }

        // Weapon selection screen
        public void RenderWeaponSelection(List<StartingWeapon> weapons)
        {
            ClearClickableElements();
            // Use null character to show blank left panel
            layoutManager.RenderLayout(null, (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderWeaponSelectionContent(contentX, contentY, contentWidth, contentHeight, weapons);
            }, "WEAPON SELECTION");
        }
        
        private void RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
        {
            // Center the content vertically
            int centerY = y + (height / 2) - (weapons.Count * 3) / 2 - 2;
            
            // Instructions
            string instructions = "Choose your starting weapon:";
            int instructionX = x + (width / 2) - (instructions.Length / 2);
            canvas.AddText(instructionX, centerY, instructions, AsciiArtAssets.Colors.White);
            centerY += 3;
            
            // Find max weapon display text length for centering
            int maxLength = 0;
            foreach (var weapon in weapons)
            {
                string displayText = $"[{weapons.IndexOf(weapon) + 1}] {weapon.name}";
                if (displayText.Length > maxLength)
                    maxLength = displayText.Length;
            }
            
            // Render weapon options centered
            foreach (var weapon in weapons)
            {
                int weaponNum = weapons.IndexOf(weapon) + 1;
                string displayText = $"[{weaponNum}] {weapon.name}";
                string coloredName = $"{{{{common|{weapon.name}}}}}";
                
                // Center each weapon option
                int optionX = x + (width / 2) - (maxLength / 2);
                
                // Add clickable element (relative to canvas, not content area)
                clickableElements.Add(new ClickableElement
                {
                    X = optionX,
                    Y = centerY,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = weaponNum.ToString(),
                    DisplayText = displayText
                });
                
                // Display weapon name with color
                WriteLineColored($"[{weaponNum}] {coloredName}", optionX, centerY);
                centerY++;
                
                // Weapon stats (indented slightly)
                string stats = $"    Damage: {weapon.damage:F1}, Attack Speed: {weapon.attackSpeed:F2}s";
                int statsX = x + (width / 2) - (stats.Length / 2);
                canvas.AddText(statsX, centerY, stats, AsciiArtAssets.Colors.Gray);
                centerY += 2;
            }
            
            // Instructions at bottom
            centerY += 2;
            string bottomInstructions = "Press the number key or click to select your weapon";
            int bottomX = x + (width / 2) - (bottomInstructions.Length / 2);
            canvas.AddText(bottomX, centerY, bottomInstructions, AsciiArtAssets.Colors.White);
        }
        
        // Character creation screen
        public void RenderCharacterCreation(Character character)
        {
            RenderWithLayout(character, "CHARACTER CREATION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderCharacterCreationContent(contentX, contentY, contentWidth, contentHeight, character);
            });
        }
        
        private void RenderCharacterCreationContent(int x, int y, int width, int height, Character character)
        {
            int startY = y; // Store initial Y position for bottom calculations
            int centerY = y + (height / 2) - 8;
            
            // Welcome message
            canvas.AddText(x + (width / 2) - 20, centerY, "═══ YOUR HERO HAS BEEN CREATED! ═══", AsciiArtAssets.Colors.Gold);
            centerY += 3;
            
            canvas.AddText(x + 4, centerY, $"Welcome, {character.Name}!", AsciiArtAssets.Colors.White);
            centerY += 2;
            canvas.AddText(x + 4, centerY, "Your adventure in the dungeons begins now.", AsciiArtAssets.Colors.White);
            centerY += 2;
            canvas.AddText(x + 4, centerY, "Check your stats and equipment on the left.", AsciiArtAssets.Colors.White);
            centerY += 3;
            
            // Starting equipment summary
            canvas.AddText(x + 4, centerY, "Starting Equipment:", AsciiArtAssets.Colors.Gold);
            centerY++;
            string weaponDisplay = character.Weapon != null ? ItemDisplayFormatter.GetColoredItemName(character.Weapon) : "Bare Fists";
            WriteLineColored($"• {weaponDisplay}", x + 6, centerY);
            centerY++;
            if (character.Head != null)
            {
                WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Head)}", x + 6, centerY);
                centerY++;
            }
            if (character.Body != null)
            {
                WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Body)}", x + 6, centerY);
                centerY++;
            }
            if (character.Feet != null)
            {
                WriteLineColored($"• {ItemDisplayFormatter.GetColoredItemName(character.Feet)}", x + 6, centerY);
                centerY++;
            }
            
            // Action buttons at bottom
            y = startY + height - 6;
            canvas.AddText(x + 2, y, "═══ ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var startButton = CreateButton(x + 4, y, 20, "1", "[1] Start Adventure");
            clickableElements.Add(startButton);
            
            canvas.AddMenuOption(x + 4, y, 1, "Start Adventure", AsciiArtAssets.Colors.White, startButton.IsHovered);
        }

        // Settings screen
        public void RenderSettings()
        {
            // Delegate to MenuRenderer
            menuRenderer.RenderSettings();
        }
        
        // Track if delete confirmation is pending
        private bool deleteConfirmationPending = false;
        
        /// <summary>
        /// Resets the delete confirmation state
        /// </summary>
        public void ResetDeleteConfirmation()
        {
            deleteConfirmationPending = false;
        }
        
        /// <summary>
        /// Sets the delete confirmation pending state
        /// </summary>
        public void SetDeleteConfirmationPending(bool pending)
        {
            deleteConfirmationPending = pending;
        }

        // Loading animation
        public void ShowLoadingAnimation(string message = "Loading...")
        {
            canvas.Clear();
            
            // Center the loading message
            canvas.AddCenteredText(18, message, AsciiArtAssets.Colors.White);
            
            // Simple loading animation with dots
            string dots = "....";
            for (int i = 0; i < 4; i++)
            {
                canvas.AddCenteredText(20, dots.Substring(0, i + 1), AsciiArtAssets.Colors.Yellow);
                canvas.Refresh();
                Thread.Sleep(200);
            }
        }

        // Enhanced error display
        public void ShowError(string error, string suggestion = "")
        {
            canvas.Clear();
            
            // Error title
            canvas.AddCenteredText(15, "ERROR", AsciiArtAssets.Colors.Red);
            
            // Error message
            canvas.AddCenteredText(18, error, AsciiArtAssets.Colors.White);
            
            // Suggestion if provided
            if (!string.IsNullOrEmpty(suggestion))
            {
                canvas.AddCenteredText(20, suggestion, AsciiArtAssets.Colors.Yellow);
            }
            
            // Instructions
            canvas.AddCenteredText(25, "Press any key to continue...", AsciiArtAssets.Colors.Gray);
            
            canvas.Refresh();
        }

        // Game loop - Dungeon exploration
        public void RenderDungeonExploration(Character player, string currentLocation, List<string> availableActions, List<string> recentEvents)
        {
            RenderWithLayout(player, "DUNGEON EXPLORATION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderDungeonContent(contentX, contentY, contentWidth, contentHeight, currentLocation, availableActions, recentEvents);
            });
        }
        
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
                var actionElement = CreateButton(x + 2, y, width - 4, (i + 1).ToString(), $"[{i + 1}] {action}");
                clickableElements.Add(actionElement);
                
                canvas.AddMenuOption(x + 2, y, i + 1, action, AsciiArtAssets.Colors.White, actionElement.IsHovered);
                y++;
            }
            
            // Quick actions at bottom
            y = startY + height - 4;
            canvas.AddText(x + 2, y, "═══ QUICK ACTIONS ═══", AsciiArtAssets.Colors.Gold);
            y += 2;
            
            var inventoryButton = CreateButton(x + 2, y, 15, "inventory", "[I] Inventory");
            clickableElements.Add(inventoryButton);
            
            canvas.AddText(x + 2, y, "[I] Inventory", AsciiArtAssets.Colors.White);
        }

        // Game menu (main game loop menu)
        public void RenderGameMenu(Character player, List<Item> inventory)
        {
            RenderWithLayout(player, $"WELCOME, {player.Name.ToUpper()}!", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderGameMenuContent(contentX, contentY, contentWidth, contentHeight);
            });
        }
        
        private void RenderGameMenuContent(int x, int y, int width, int height)
        {
            // Delegate to MenuRenderer
            menuRenderer.RenderGameMenu(x, y, width, height);
        }

        // Dungeon selection screen
        public void RenderDungeonSelection(Character player, List<Dungeon> dungeons)
        {
            // Store state for animation
            isDungeonSelectionActive = true;
            dungeonSelectionPlayer = player;
            dungeonSelectionList = dungeons;
            
            RenderWithLayout(player, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderDungeonSelectionContent(contentX, contentY, contentWidth, contentHeight, dungeons);
            });
        }
        
        private void RenderDungeonSelectionContent(int x, int y, int width, int height, List<Dungeon> dungeons)
        {
            // Delegate to DungeonRenderer
            dungeonRenderer.RenderDungeonSelection(x, y, width, height, dungeons);
        }
        
        /// <summary>
        /// Stops the dungeon selection animation
        /// Call this when leaving the dungeon selection screen
        /// </summary>
        public void StopDungeonSelectionAnimation()
        {
            isDungeonSelectionActive = false;
            dungeonSelectionPlayer = null;
            dungeonSelectionList = null;
        }
        
        /// <summary>
        /// Update callback for undulation animation
        /// </summary>
        private void UpdateUndulation(object? state)
        {
            if (isDungeonSelectionActive && dungeonSelectionPlayer != null && dungeonSelectionList != null)
            {
                // Update the undulation offset
                dungeonRenderer.UpdateUndulation();
                
                // Re-render the dungeon selection
                Dispatcher.UIThread.Post(() =>
                {
                    RenderWithLayout(dungeonSelectionPlayer, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
                    {
                        RenderDungeonSelectionContent(contentX, contentY, contentWidth, contentHeight, dungeonSelectionList);
                    });
                });
            }
        }
        
        /// <summary>
        /// Update callback for brightness mask animation (separate from undulation)
        /// </summary>
        private void UpdateBrightnessMask(object? state)
        {
            if (isDungeonSelectionActive && dungeonSelectionPlayer != null && dungeonSelectionList != null)
            {
                // Update the brightness mask offset
                dungeonRenderer.UpdateBrightnessMask();
                
                // Re-render the dungeon selection
                Dispatcher.UIThread.Post(() =>
                {
                    RenderWithLayout(dungeonSelectionPlayer, "DUNGEON SELECTION", (contentX, contentY, contentWidth, contentHeight) =>
                    {
                        RenderDungeonSelectionContent(contentX, contentY, contentWidth, contentHeight, dungeonSelectionList);
                    });
                });
            }
        }

        // Dungeon start screen
        public void RenderDungeonStart(Dungeon dungeon, Character player)
        {
            RenderWithLayout(player, $"ENTERING DUNGEON: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderDungeonStartContent(contentX, contentY, contentWidth, contentHeight, dungeon);
            }, null, dungeon.Name, null);
        }
        
        private void RenderDungeonStartContent(int x, int y, int width, int height, Dungeon dungeon)
        {
            // Delegate to DungeonRenderer
            dungeonRenderer.RenderDungeonStart(x, y, width, height, dungeon);
        }

        // Room entry screen
        public void RenderRoomEntry(Environment room, Character player, string? dungeonName = null)
        {
            RenderWithLayout(player, $"ENTERING ROOM: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderRoomEntryContent(contentX, contentY, contentWidth, contentHeight, room);
            }, null, dungeonName, room.Name);
        }
        
        private void RenderRoomEntryContent(int x, int y, int width, int height, Environment room)
        {
            // Delegate to DungeonRenderer
            dungeonRenderer.RenderRoomEntry(x, y, width, height, room);
        }
        
        /// <summary>
        /// Wraps text to fit within maxWidth, preserving leading whitespace (indentation)
        /// </summary>
        private List<string> WrapText(string text, int maxWidth)
        {
            return textWriter.WrapText(text, maxWidth);
        }

        // Enemy encounter screen - now shows accumulated dungeon log
        public void RenderEnemyEncounter(Enemy enemy, Character player, List<string> dungeonLog, string? dungeonName = null, string? roomName = null)
        {
            RenderWithLayout(player, "PREPARING FOR COMBAT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderEnemyEncounterContent(contentX, contentY, contentWidth, contentHeight, dungeonLog);
            }, enemy, dungeonName, roomName);
        }
        
        /// <summary>
        /// Renders the enemy encounter screen showing dungeon/room/enemy context
        /// </summary>
        private void RenderEnemyEncounterContent(int x, int y, int width, int height, List<string> dungeonLog)
        {
            // Delegate to CombatRenderer
            combatRenderer.RenderEnemyEncounter(x, y, width, height, dungeonLog);
        }

        // Combat result screen
        public void RenderCombatResult(bool playerSurvived, Character player, Enemy enemy, BattleNarrative? battleNarrative = null, string? dungeonName = null, string? roomName = null)
        {
            RenderWithLayout(player, "COMBAT RESULT", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderCombatResultContent(contentX, contentY, contentWidth, contentHeight, playerSurvived, enemy, battleNarrative);
            }, enemy, dungeonName, roomName);
        }
        
        /// <summary>
        /// Renders the combat result screen (victory/defeat)
        /// </summary>
        private void RenderCombatResultContent(int x, int y, int width, int height, bool playerSurvived, Enemy enemy, BattleNarrative? battleNarrative)
        {
            // Delegate to CombatRenderer
            combatRenderer.RenderCombatResult(x, y, width, height, playerSurvived, enemy, battleNarrative);
        }

        // Room completion screen
        public void RenderRoomCompletion(Environment room, Character player, string? dungeonName = null)
        {
            RenderWithLayout(player, $"ROOM CLEARED: {room.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderRoomCompletionContent(contentX, contentY, contentWidth, contentHeight, room, player);
            }, null, dungeonName, room.Name);
        }
        
        /// <summary>
        /// Renders the room completion screen
        /// </summary>
        private void RenderRoomCompletionContent(int x, int y, int width, int height, Environment room, Character character)
        {
            // Delegate to DungeonRenderer
            dungeonRenderer.RenderRoomCompletion(x, y, width, height, room, character);
        }

        // Dungeon completion screen
        public void RenderDungeonCompletion(Dungeon dungeon, Character player)
        {
            RenderWithLayout(player, $"DUNGEON COMPLETED: {dungeon.Name.ToUpper()}", (contentX, contentY, contentWidth, contentHeight) =>
            {
                RenderDungeonCompletionContent(contentX, contentY, contentWidth, contentHeight, dungeon);
            }, null, dungeon.Name, null);
        }
        
        private void RenderDungeonCompletionContent(int x, int y, int width, int height, Dungeon dungeon)
        {
            // Delegate to DungeonRenderer
            dungeonRenderer.RenderDungeonCompletion(x, y, width, height, dungeon);
        }
    }
}
