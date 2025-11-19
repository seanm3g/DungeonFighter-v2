using Avalonia.Media;
using RPGGame;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Handles rendering of all menu screens (main menu, settings, game menu, weapon selection)
    /// </summary>
    public class MenuRenderer : IInteractiveRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly ICanvasInteractionManager interactionManager;
        private int currentLineCount;
        
        // Screen dimensions
        private const int SCREEN_WIDTH = 210;
        private const int SCREEN_CENTER = SCREEN_WIDTH / 2;
        private const int LEFT_MARGIN = 2;
        private const int CONTENT_WIDTH = 206;
        
        public MenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager, ICanvasInteractionManager interactionManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.interactionManager = interactionManager;
            this.currentLineCount = 0;
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
        /// Renders the main menu content within the center panel (top-left justified)
        /// </summary>
        public void RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, string? characterName, int characterLevel)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Build menu options dynamically
            string loadGameText = hasSavedGame && characterName != null 
                ? $"Load Game - *{characterName} - lvl {characterLevel}*"
                : "Load Game";
            
            // Warm white to cold white gradient using ColorLayerSystem
            var menuConfig = new[]
            {
                (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm)),
                (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2)),
                (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3)),
                (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool))
            };
            
            // Position menu at top-left of center panel
            int menuStartX = x + 2; // Small margin from left edge
            int menuStartY = y + 2; // Small margin from top edge
            
            // Render menu options with colors
            for (int i = 0; i < menuConfig.Length; i++)
            {
                var (number, text, color) = menuConfig[i];
                string displayText = $"[{number}] {text}";
                
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY + i,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                canvas.AddMenuOption(menuStartX, menuStartY + i, number, text, color, option.IsHovered);
                currentLineCount++;
            }
            
            // Add instruction text at bottom of center panel
            int instructionY = y + height - 3;
            canvas.AddText(x + 2, instructionY, "Click on options or press number keys. Press H for help", AsciiArtAssets.Colors.Gray);
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available (legacy method)
        /// </summary>
        public void RenderMainMenu(bool hasSavedGame, string? characterName, int characterLevel)
        {
            canvas.Clear();
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Title
            canvas.AddTitle(18, "MAIN MENU", AsciiArtAssets.Colors.White);
            currentLineCount = 20; // Title is at line 18 + 2 for spacing
            
            // Build menu options dynamically
            string loadGameText = hasSavedGame && characterName != null 
                ? $"Load Game - *{characterName} - lvl {characterLevel}*"
                : "Load Game";
            
            // Warm white to cold white gradient using ColorLayerSystem
            // This respects the WhiteTemperatureIntensity configuration parameter
            var menuConfig = new[]
            {
                (1, "New Game", ColorLayerSystem.GetWhite(WhiteTemperature.Warm)),
                (2, loadGameText, ColorLayerSystem.GetWhiteByDepth(2)),
                (3, "Settings", ColorLayerSystem.GetWhiteByDepth(3)),
                (0, "Quit", ColorLayerSystem.GetWhite(WhiteTemperature.Cool))
            };
            
            // Find the longest menu option for centering
            int maxOptionLength = menuConfig.Max(m => $"[{m.Item1}] {m.Item2}".Length);
            
            // Center the menu options around screen center
            int menuStartX = SCREEN_CENTER - (maxOptionLength / 2);
            int menuStartY = 20;
            
            // Render menu options with colors
            for (int i = 0; i < menuConfig.Length; i++)
            {
                var (number, text, color) = menuConfig[i];
                string displayText = $"[{number}] {text}";
                
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY + i,
                    Width = maxOptionLength,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                
                // Render with gradient color (double render for bold effect)
                canvas.AddText(menuStartX, menuStartY + i, displayText, color);
                canvas.AddText(menuStartX, menuStartY + i, displayText, color);
                currentLineCount++;
            }
            
            // Instructions
            string instructions = "Click on options or press number keys. Press H for help";
            int instructionsX = SCREEN_CENTER - (instructions.Length / 2);
            canvas.AddText(instructionsX, menuStartY + menuConfig.Length + 2, instructions, AsciiArtAssets.Colors.White);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
        }
        
        /// <summary>
        /// Renders the settings screen using the 3-panel layout
        /// </summary>
        public void RenderSettings()
        {
            // Use the persistent layout system for consistent 3-panel design
            var layoutManager = new PersistentLayoutManager(canvas);
            
            // Get saved character info for display
            var (characterName, characterLevel) = CharacterSaveManager.GetSavedCharacterInfo();
            bool hasSavedCharacter = characterName != null;
            
            // Render the layout with settings content
            layoutManager.RenderLayout(
                character: null, // No character in settings
                renderCenterContent: (x, y, width, height) => RenderSettingsContent(x, y, width, height, hasSavedCharacter, characterName, characterLevel),
                title: "SETTINGS",
                enemy: null,
                dungeonName: null,
                roomName: null
            );
        }
        
        /// <summary>
        /// Renders the simplified settings content with only functional options
        /// </summary>
        private void RenderSettingsContent(int x, int y, int width, int height, bool hasSavedCharacter, string? characterName, int characterLevel)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Simple centered menu layout
            int menuStartX = x + (width / 2) - 15; // Center the menu
            int menuStartY = y + (height / 2) - 3; // Center vertically
            
            // Title
            canvas.AddText(menuStartX, menuStartY, "=== SETTINGS ===", AsciiArtAssets.Colors.Gold);
            menuStartY += 3;
            
            // Menu options - only functional ones
            var menuOptions = new[]
            {
                (1, "Testing", AsciiArtAssets.Colors.White),
                (0, "Back to Main Menu", AsciiArtAssets.Colors.White)
            };
            
            foreach (var (number, text, color) in menuOptions)
            {
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = menuStartY,
                    Width = text.Length + 4,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = number.ToString(),
                    DisplayText = $"[{number}] {text}"
                };
                clickableElements.Add(option);
                
                canvas.AddText(menuStartX, menuStartY, $"[{number}] {text}", color);
                menuStartY++;
            }
            
            // Show saved character info if available
            if (hasSavedCharacter && characterName != null)
            {
                menuStartY += 2;
                canvas.AddText(menuStartX, menuStartY, $"Saved Character: {characterName} (Level {characterLevel})", AsciiArtAssets.Colors.Cyan);
                menuStartY++;
                canvas.AddText(menuStartX, menuStartY, "Use 'Delete Saved Character' in game menu to remove", AsciiArtAssets.Colors.Gray);
            }
        }
        
        
        /// <summary>
        /// Renders the testing menu screen using the 3-panel layout
        /// </summary>
        public void RenderTestingMenu(int x, int y, int width, int height)
        {
            clickableElements.Clear();
            currentLineCount = 0;
            
            // Test description panel (top section)
            int panelHeight = (height - 4) / 3; // Divide available height into 3 sections with spacing
            int currentY = y;
            
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Blue);
            canvas.AddTitle(currentY + 1, "TEST DESCRIPTION", AsciiArtAssets.Colors.Blue);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            int textY = currentY + 3;
            canvas.AddText(x + 2, textY, "These tests verify all game systems:", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Character, Combat, Inventory, Dungeon", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Data Loading, UI, Save/Load, Actions", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Color System, Performance, Integration", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "• Combat UI Fixes and System Validation", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Test options panel (middle section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Green);
            canvas.AddTitle(currentY + 1, "TEST OPTIONS", AsciiArtAssets.Colors.Green);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            canvas.AddText(x + 2, textY, "[1] Run All Tests (Complete Suite)", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[2] Character System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[3] Combat System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[4] Inventory System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[5] Dungeon System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[6] Data System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[7] UI System Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[8] Combat UI Fixes", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[9] Integration Tests", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "[0] Back to Settings", AsciiArtAssets.Colors.White);
            currentLineCount++;
            
            // Instructions panel (bottom section)
            currentY += panelHeight + 1;
            canvas.AddBorder(x, currentY, width, panelHeight, AsciiArtAssets.Colors.Yellow);
            canvas.AddTitle(currentY + 1, "INSTRUCTIONS", AsciiArtAssets.Colors.Yellow);
            currentLineCount = currentY + 3; // Border + title + spacing
            
            textY = currentY + 3;
            canvas.AddText(x + 2, textY, "Select a test option to run. Results will be displayed", AsciiArtAssets.Colors.White);
            textY++;
            currentLineCount++;
            canvas.AddText(x + 2, textY, "in the center panel after test completion.", AsciiArtAssets.Colors.White);
            currentLineCount++;
        }
        
        /// <summary>
        /// Renders the in-game menu (after character creation)
        /// </summary>
        public void RenderGameMenu(int x, int y, int width, int height)
        {
            currentLineCount = 0;
            
            // Welcome message
            int centerY = y + (height / 2) - 5;
            canvas.AddText(x + (width / 2) - 20, centerY, "═══ WHAT WOULD YOU LIKE TO DO? ═══", AsciiArtAssets.Colors.Gold);
            currentLineCount++;
            centerY += 3;
            
            // Game menu options
            int menuX = x + (width / 2) - 10;
            
            var option1 = new ClickableElement
            {
                X = menuX,
                Y = centerY,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = "[1] Go to Dungeon"
            };
            
            var option2 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 1,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = "[2] Show Inventory"
            };
            
            var option3 = new ClickableElement
            {
                X = menuX,
                Y = centerY + 2,
                Width = 20,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = "[0] Save & Exit"
            };
            
            clickableElements.AddRange(new[] { option1, option2, option3 });
            
            canvas.AddMenuOption(menuX, centerY, 1, "Go to Dungeon", AsciiArtAssets.Colors.White, option1.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 1, 2, "Show Inventory", AsciiArtAssets.Colors.White, option2.IsHovered);
            currentLineCount++;
            canvas.AddMenuOption(menuX, centerY + 2, 0, "Save & Exit", AsciiArtAssets.Colors.White, option3.IsHovered);
            currentLineCount++;
        }

        /// <summary>
        /// Renders the weapon selection content with centered layout (merged from WeaponSelectionRenderer)
        /// </summary>
        public void RenderWeaponSelectionContent(int x, int y, int width, int height, List<StartingWeapon> weapons)
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
                
                // Center each weapon option
                int optionX = x + (width / 2) - (maxLength / 2);
                
                // Add clickable element
                var option = new ClickableElement
                {
                    X = optionX,
                    Y = centerY,
                    Width = displayText.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = weaponNum.ToString(),
                    DisplayText = displayText
                };
                clickableElements.Add(option);
                interactionManager.AddClickableElement(option);
                
                // Display weapon option using canvas.AddMenuOption (same as main menu)
                canvas.AddMenuOption(optionX, centerY, weaponNum, weapon.name, AsciiArtAssets.Colors.White, option.IsHovered);
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
    }
}

