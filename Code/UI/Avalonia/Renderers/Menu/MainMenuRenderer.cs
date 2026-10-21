using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.UI.Avalonia.Renderers.Menu
{
    /// <summary>
    /// Renders the main menu screen
    /// </summary>
    public class MainMenuRenderer
    {
        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private readonly ICanvasTextManager textManager;
        private readonly MenuOptionRenderer menuOptionRenderer;
        private readonly CharacterSelectionRenderer characterSelectionRenderer;
        private readonly StableScreenRenderer stableScreenRenderer;
        
        public MainMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.menuOptionRenderer = new MenuOptionRenderer(canvas);
            this.characterSelectionRenderer = new CharacterSelectionRenderer(canvas, clickableElements, menuOptionRenderer);
            this.stableScreenRenderer = new StableScreenRenderer(canvas, clickableElements);
        }
        
        /// <summary>
        /// Renders the main menu content within the center panel (centered horizontally and vertically)
        /// </summary>
        public int RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, int savedFighterCount, string? newGameConfirmPrompt = null)
        {
            clickableElements.Clear();
            int currentLineCount = 0;
            
            string loadGameTextForWidth = hasSavedGame
                ? UIConstants.Formats.LoadCharactersWithCount(savedFighterCount)
                : UIConstants.MenuOptions.LoadGame;
            
            int maxOptionLength = Math.Max(
                MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame).Length,
                Math.Max(
                    MenuOptionFormatter.Format(2, loadGameTextForWidth).Length,
                    Math.Max(
                        MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings).Length,
                        Math.Max(
                            MenuOptionFormatter.Format(4, UIConstants.MenuOptions.Stable).Length,
                            Math.Max(
                                MenuOptionFormatter.Format(5, UIConstants.MenuOptions.History).Length,
                                MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit).Length
                            )
                        )
                    )
                )
            );

            const int menuLineCount = 6;
            const int footerReserve = 4;
            var promptLines = WrapNewGameConfirmPrompt(newGameConfirmPrompt, width);
            // Blank row plus the prompt, so the block stays centered with the options instead of
            // sitting on the bottom rows that ClearLoadingStatus wipes after this paint.
            int promptSlots = promptLines.Count == 0 ? 0 : promptLines.Count + 1;
            int layoutHeight = Math.Max(height - footerReserve, menuLineCount + promptSlots + 2);
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateCenteredMenu(x, y, width, layoutHeight, menuLineCount + promptSlots, maxOptionLength);
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var coolWhiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            // Render [1] New Game - Cyan
            int currentY = menuStartY;
            int optionHitHeight = CenterColumnTextZoom.LineStride;
            string newGameText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame);
            var newGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = newGameText
            };
            clickableElements.Add(newGameOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
            currentLineCount++;
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            
            // Render [2] Load Character(s) - with the saved fighter count highlighted
            string loadGameText = loadGameTextForWidth;
            
            string loadGameDisplayText = MenuOptionFormatter.Format(2, loadGameText);
            var loadGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = loadGameDisplayText
            };
            clickableElements.Add(loadGameOption);
            
            if (hasSavedGame)
            {
                // Render with the fighter count highlighted
                menuOptionRenderer.RenderColoredLoadGameOption(menuStartX, currentY, 2, savedFighterCount, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
            }
            currentLineCount++;
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
            
            // Render [3] Settings - Orange
            string settingsText = MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings);
            var settingsOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "3",
                DisplayText = settingsText
            };
            clickableElements.Add(settingsOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);

            // Render [4] Stable - Gold (several heroes running side by side)
            string stableText = MenuOptionFormatter.Format(4, UIConstants.MenuOptions.Stable);
            var stableOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "4",
                DisplayText = stableText
            };
            clickableElements.Add(stableOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 4, UIConstants.MenuOptions.Stable, goldColor, stableOption.IsHovered);
            currentLineCount++;
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);

            // Render [5] History - Cyan (fallen fighters and records; screen coming soon)
            string historyText = MenuOptionFormatter.Format(5, UIConstants.MenuOptions.History);
            var historyOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "5",
                DisplayText = historyText
            };
            clickableElements.Add(historyOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 5, UIConstants.MenuOptions.History, cyanColor, historyOption.IsHovered);
            currentLineCount++;
            currentY = MenuLayoutCalculator.NextMenuLineY(currentY);

            // Note: Characters option (C) is hidden from UI but still accessible via keyboard

            // Render [0] Quit - Cool white
            string quitText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit);
            var quitOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = optionHitHeight,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = quitText
            };
            clickableElements.Add(quitOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;

            if (promptLines.Count > 0)
            {
                var promptColor = AsciiArtAssets.Colors.Yellow;
                currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
                foreach (var line in promptLines)
                {
                    currentY = MenuLayoutCalculator.NextMenuLineY(currentY);
                    int lineX = MenuLayoutCalculator.CalculateCenteredZoomedTextX(x, width, line.Length);
                    canvas.AddText(lineX, currentY, line, promptColor);
                    currentLineCount++;
                }
            }
            else
            {
                RenderMenuFooter(x, y, width, height);
            }
            
            return currentLineCount;
        }

        /// <summary>
        /// Splits the overwrite prompt into lines that fit the menu width. The first visual gap
        /// is added by the caller so the question sits under Quit.
        /// </summary>
        private static List<string> WrapNewGameConfirmPrompt(string? newGameConfirmPrompt, int width)
        {
            if (string.IsNullOrWhiteSpace(newGameConfirmPrompt))
                return new List<string>();

            int wrapWidth = Math.Max(24, CenterColumnTextZoom.ScaleWrapWidth(Math.Max(24, width - 4)));
            return TextWrapper.WrapText(newGameConfirmPrompt, wrapWidth);
        }

        /// <summary>
        /// Usual click/number hint along the bottom of the menu.
        /// </summary>
        private void RenderMenuFooter(int x, int y, int width, int height)
        {
            string instructionText = UIConstants.Messages.ClickOrPressNumber;
            int instructionY = y + height - 3;
            int textWidth = Math.Min(instructionText.Length, Math.Max(1, width));
            int instructionX = instructionText.Length >= width
                ? x
                : MenuLayoutCalculator.CalculateCenteredTextX(x, width, textWidth);
            canvas.AddText(instructionX, instructionY, instructionText, AsciiArtAssets.Colors.Gray);
        }
        
        /// <summary>
        /// Renders the main menu with saved game info if available (legacy method)
        /// NOTE: This method is legacy and may not be used. Canvas clearing should be handled by the caller.
        /// </summary>
        public int RenderMainMenu(bool hasSavedGame, int savedFighterCount, string? newGameConfirmPrompt = null)
        {
            // Note: Canvas clearing removed - should be handled by caller or CanvasTitleLayoutCoordinator / PersistentLayoutRenderCoordinator
            clickableElements.Clear();
            int currentLineCount = 0;
            
            // Title
            canvas.AddTitle(18, "MAIN MENU", AsciiArtAssets.Colors.White);
            currentLineCount = 20; // Title is at line 18 + 2 for spacing
            
            // Color palette for menu items
            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var coolWhiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);
            
            // Find the longest menu option for centering
            string loadGameText = hasSavedGame
                ? UIConstants.Formats.LoadCharactersWithCount(savedFighterCount)
                : UIConstants.MenuOptions.LoadGame;
            
            int maxOptionLength = Math.Max(
                MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame).Length,
                Math.Max(
                    MenuOptionFormatter.Format(2, loadGameText).Length,
                    Math.Max(
                        MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings).Length,
                        MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit).Length
                    )
                )
            );
            
            // Center the menu options around the live grid center (tracks Ctrl+/- column count)
            int menuStartX = MenuLayoutCalculator.SCREEN_CENTER - (maxOptionLength / 2);
            int menuStartY = 20;
            int currentY = menuStartY;
            
            // Render [1] New Game - Cyan
            string newGameText = MenuOptionFormatter.Format(1, UIConstants.MenuOptions.NewGame);
            var newGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "1",
                DisplayText = newGameText
            };
            clickableElements.Add(newGameOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 1, UIConstants.MenuOptions.NewGame, cyanColor, newGameOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Render [2] Load Character(s) - with the saved fighter count highlighted
            string loadGameDisplayText = MenuOptionFormatter.Format(2, loadGameText);
            var loadGameOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "2",
                DisplayText = loadGameDisplayText
            };
            clickableElements.Add(loadGameOption);
            
            if (hasSavedGame)
            {
                // Render with the fighter count highlighted
                menuOptionRenderer.RenderColoredLoadGameOption(menuStartX, currentY, 2, savedFighterCount, orangeColor, loadGameOption.IsHovered);
            }
            else
            {
                // Render plain load game option
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 2, UIConstants.MenuOptions.LoadGame, goldColor, loadGameOption.IsHovered);
            }
            currentLineCount++;
            currentY++;
            
            // Render [3] Settings - Orange
            string settingsText = MenuOptionFormatter.Format(3, UIConstants.MenuOptions.Settings);
            var settingsOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "3",
                DisplayText = settingsText
            };
            clickableElements.Add(settingsOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 3, UIConstants.MenuOptions.Settings, orangeColor, settingsOption.IsHovered);
            currentLineCount++;
            currentY++;
            
            // Note: Characters option (4 or C) is hidden from UI but still accessible via keyboard
            
            // Render [0] Quit - Cool white
            string quitText = MenuOptionFormatter.Format(0, UIConstants.MenuOptions.Quit);
            var quitOption = new ClickableElement
            {
                X = menuStartX,
                Y = currentY,
                Width = maxOptionLength,
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = quitText
            };
            clickableElements.Add(quitOption);
            menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, 0, UIConstants.MenuOptions.Quit, coolWhiteColor, quitOption.IsHovered);
            currentLineCount++;
            
            // Instructions
            string instructions = string.IsNullOrWhiteSpace(newGameConfirmPrompt)
                ? UIConstants.Messages.ClickOrPressNumber
                : newGameConfirmPrompt;
            var instructionColor = string.IsNullOrWhiteSpace(newGameConfirmPrompt)
                ? AsciiArtAssets.Colors.White
                : AsciiArtAssets.Colors.Yellow;
            int instructionsX = MenuLayoutCalculator.CalculateCenteredTextX(0, LayoutConstants.SCREEN_WIDTH, instructions.Length);
            canvas.AddText(instructionsX, currentY + 2, instructions, instructionColor);
            currentLineCount += 3; // +2 for spacing, +1 for instruction line
            
            canvas.Refresh();
            return currentLineCount;
        }
        
        /// <summary>
        /// Renders the character selection menu content
        /// </summary>
        public int RenderCharacterSelectionContent(int x, int y, int width, int height, List<Character> characters, string? activeCharacterName, Dictionary<string, string> characterStatuses)
        {
            return characterSelectionRenderer.RenderCharacterSelectionContent(x, y, width, height, characters, activeCharacterName, characterStatuses);
        }

        /// <summary>Renders the full-screen Stable (see <see cref="StableScreenRenderer"/>).</summary>
        public void RenderStableScreen(StableMenuView view) => stableScreenRenderer.Render(view);

        /// <summary>
        /// Renders one page of the load character menu (saved characters from disk).
        /// </summary>
        public int RenderLoadCharacterSelectionContent(int x, int y, int width, int height, LoadCharacterMenuView view)
        {
            clickableElements.Clear();
            var (menuStartX, menuStartY) = MenuLayoutCalculator.CalculateTopLeftMenu(x, y);
            int startY = menuStartY;

            var cyanColor = ColorPalette.Cyan.GetColor();
            var goldColor = ColorPalette.Gold.GetColor();
            var orangeColor = ColorPalette.Orange.GetColor();
            var whiteColor = ColorLayerSystem.GetWhite(WhiteTemperature.Cool);

            int currentY = menuStartY;

            void AddOption(string value, int number, string text, Color color)
            {
                string formatted = MenuOptionFormatter.Format(number, text);
                var option = new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = formatted.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = value,
                    DisplayText = formatted
                };
                clickableElements.Add(option);
                menuOptionRenderer.RenderColoredMenuOption(menuStartX, currentY, number, text, color, option.IsHovered);
                currentY++;
            }

            void AddLetterOption(string key, string text, Color color)
            {
                string formatted = $"[{key}] {text}";
                clickableElements.Add(new ClickableElement
                {
                    X = menuStartX,
                    Y = currentY,
                    Width = formatted.Length,
                    Height = 1,
                    Type = ElementType.MenuOption,
                    Value = key.ToLowerInvariant(),
                    DisplayText = formatted
                });
                canvas.AddText(menuStartX, currentY, formatted, color);
                currentY++;
            }

            if (view.TotalCount == 0)
            {
                canvas.AddText(menuStartX, currentY, "No saved characters found.", whiteColor);
                currentY += 2;
                AddOption("0", 0, "Back to Main Menu", goldColor);
                currentY++;
                canvas.AddText(menuStartX, currentY, "Press 0 to return to main menu.", whiteColor);
                currentY++;
                canvas.Refresh();
                return currentY - startY;
            }

            string header = view.PageCount > 1
                ? $"{view.TotalCount} saved characters  -  page {view.Page + 1} of {view.PageCount}"
                : $"{view.TotalCount} saved character{(view.TotalCount == 1 ? "" : "s")}";
            canvas.AddText(menuStartX, currentY, header, whiteColor);
            currentY += 2;

            foreach (var (number, characterName, level) in view.Entries)
                AddOption(number.ToString(), number, $"{characterName} - Level {level}", cyanColor);

            currentY++;
            if (view.HasNextPage)
                AddLetterOption("N", "Next page", whiteColor);
            if (view.HasPreviousPage)
                AddLetterOption("P", "Previous page", whiteColor);
            AddLetterOption("D", "Delete a character", orangeColor);
            AddOption("0", 0, "Back to Main Menu", goldColor);
            currentY++;

            string prompt = view.Prompt ?? "Select a character number to load, or press 0 to return.";
            var promptColor = view.PromptIsWarning ? orangeColor : whiteColor;
            foreach (var line in TextWrapper.WrapText(prompt, Math.Max(20, width - (menuStartX - x) - 2)))
            {
                canvas.AddText(menuStartX, currentY, line, promptColor);
                currentY++;
            }

            canvas.Refresh();
            return currentY - startY;
        }
    }
}
