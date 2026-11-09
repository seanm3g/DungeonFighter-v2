using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using RPGGame.UI.Avalonia.Renderers.Helpers;
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
        /// Main menu as a fighter roster: one card per saved fighter (most recently saved first, click to load), a "+"
        /// card for a new fighter, "Fighter Overview (Stable)" top left, Settings / Quit bottom right.
        /// Keys: 1-9 load the fighter cards in order; N new, A all saved fighters, F overview, S settings, Q / 0 quit.
        /// </summary>
        public int RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, int savedFighterCount, string? newGameConfirmPrompt = null)
        {
            clickableElements.Clear();
            var gold = ColorPalette.Gold.GetColor();

            List<(string characterId, string characterName, int level)> saved;
            try { saved = CharacterSaveManager.ListAllSavedCharacters() ?? new(); }
            catch { saved = new(); }
            saved = saved.FindAll(c => !string.IsNullOrWhiteSpace(c.characterName));

            int left = x + Math.Max(2, width * 13 / 100);
            int right = x + width - Math.Max(2, width * 8 / 100);

            // Fighter Overview (Stable), top left
            string overviewLabel = $"[{MainMenuHandler.OverviewKey.ToUpperInvariant()}] Fighter Overview (Stable)";
            BoxButton.Draw(canvas, clickableElements, left, y + Math.Max(1, height * 6 / 100), overviewLabel.Length + 4, 3, overviewLabel, MainMenuHandler.OverviewKey);

            // Fighter cards + the add card in one row (wraps to a second row when there are many fighters).
            const int cardW = 28;
            const int cardH = 10;
            const int gap = 3;
            int perRow = Math.Max(2, (right - left + gap) / (cardW + gap));
            int maxRows = height >= 40 ? 2 : 1;
            int capacity = perRow * maxRows;
            // Last slot is the "+" card; if fighters overflow, the one before it opens the full saved list.
            // Fighter cards are hotkeyed 1-9, so at most nine get a card of their own.
            int fighterCapacity = Math.Min(capacity - 1, MainMenuHandler.MaxFighterHotkeys);
            bool overflow = saved.Count > fighterCapacity;
            int fighterCards = overflow ? Math.Min(capacity - 2, MainMenuHandler.MaxFighterHotkeys) : saved.Count;

            int cardsTop = y + Math.Max(5, height * 24 / 100);
            int slot = 0;
            void NextSlot(out int cx, out int cy)
            {
                cx = left + (slot % perRow) * (cardW + gap);
                cy = cardsTop + (slot / perRow) * (cardH + 2);
                slot++;
            }

            for (int i = 0; i < fighterCards; i++)
            {
                var (_, name, level) = saved[i];
                NextSlot(out int cx, out int cy);
                var lines = new List<string> { $"[{i + 1}]" };
                lines.AddRange(WrapName(name, cardW - 4));
                lines.Add("");
                lines.Add($"Level {level}");
                BoxButton.Draw(canvas, clickableElements, cx, cy, cardW, cardH, lines[0], MainMenuFighterKey(i + 1), gold, 1, lines.GetRange(1, lines.Count - 1));
            }

            if (overflow)
            {
                NextSlot(out int mx, out int my);
                BoxButton.Draw(canvas, clickableElements, mx, my, cardW, cardH, $"[{MainMenuHandler.AllFightersKey.ToUpperInvariant()}]", MainMenuHandler.AllFightersKey, gold, 1,
                    new[] { $"+{saved.Count - fighterCards} more", "", "all saved fighters" });
            }

            NextSlot(out int ax, out int ay);
            BoxButton.Draw(canvas, clickableElements, ax, ay, cardW, cardH, $"[{MainMenuHandler.NewFighterKey.ToUpperInvariant()}]", MainMenuHandler.NewFighterKey, gold, 1,
                new[] { "+", "", "new fighter" });

            // Settings / Quit, bottom right
            const int sideW = 22;
            int sideX = right - sideW;
            int quitY = y + height - Math.Max(6, height * 14 / 100);
            BoxButton.Draw(canvas, clickableElements, sideX, quitY - 4, sideW, 3,
                $"[{MainMenuHandler.SettingsKey.ToUpperInvariant()}] {UIConstants.MenuOptions.Settings}", MainMenuHandler.SettingsKey);
            BoxButton.Draw(canvas, clickableElements, sideX, quitY, sideW, 3,
                $"[{MainMenuHandler.QuitKey.ToUpperInvariant()}] {UIConstants.MenuOptions.Quit}", MainMenuHandler.QuitKey);

            int rowsUsed = (slot + perRow - 1) / perRow;
            int belowCards = cardsTop + rowsUsed * (cardH + 2) + 1;
            var promptLines = WrapNewGameConfirmPrompt(newGameConfirmPrompt, Math.Max(24, right - left));
            if (promptLines.Count > 0)
            {
                foreach (var line in promptLines)
                    canvas.AddText(left, belowCards++, line, AsciiArtAssets.Colors.Yellow);
            }
            else if (saved.Count == 0)
            {
                canvas.AddText(left, belowCards, "No fighters yet. Click + or press N to create one.", AsciiArtAssets.Colors.Gray);
            }

            return slot;
        }

        /// <summary>Input sent by the Nth fighter card (1-based, save list order).</summary>
        public static string MainMenuFighterKey(int number) => MainMenuHandler.FighterCardPrefix + number;

        private static List<string> WrapName(string name, int width)
        {
            var lines = TextWrapper.WrapText(name ?? "", Math.Max(4, width));
            if (lines.Count > 2)
                lines = lines.GetRange(0, 2);
            return lines.Count == 0 ? new List<string> { "" } : lines;
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
