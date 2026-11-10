using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly FighterRosterRenderer rosterRenderer;
        
        public MainMenuRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements, ICanvasTextManager textManager)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
            this.textManager = textManager;
            this.menuOptionRenderer = new MenuOptionRenderer(canvas);
            this.characterSelectionRenderer = new CharacterSelectionRenderer(canvas, clickableElements, menuOptionRenderer);
            this.rosterRenderer = new FighterRosterRenderer(canvas, clickableElements);
            this.stableScreenRenderer = new StableScreenRenderer(canvas, clickableElements, rosterRenderer);
        }
        
        /// <summary>
        /// Main menu as a fighter roster (see <see cref="FighterRosterRenderer"/>): the new-fighter card first, then one
        /// card per saved fighter (most recently saved first, click to load), a detail panel for the hovered card,
        /// the Stable mode toggle top left, History / Settings / Quit bottom right. History replaces the roster in place.
        /// Keys: 1-9 load the fighter cards in order; N new, A all saved fighters, F Stable mode, R history, S settings, Q / 0 quit.
        /// </summary>
        public int RenderMainMenuContent(int x, int y, int width, int height, bool hasSavedGame, int savedFighterCount, string? newGameConfirmPrompt = null)
        {
            clickableElements.Clear();
            if (MainMenuViewState.ShowingHistory)
                return RenderHistory(x, y, width, height);

            var gold = ColorPalette.Gold.GetColor();
            var green = ColorPalette.Success.GetColor();
            var gray = AsciiArtAssets.Colors.Gray;

            List<(string characterId, string characterName, int level)> saved;
            try { saved = CharacterSaveManager.ListAllSavedCharacters() ?? new(); }
            catch { saved = new(); }
            saved = saved.FindAll(c => !string.IsNullOrWhiteSpace(c.characterName));

            // New-fighter card first; if fighters overflow, the last slot opens the full saved list.
            // Fighter cards are hotkeyed 1-9, so at most nine get a card of their own.
            int capacity = FighterRosterRenderer.CardCapacity(x, width, height);
            int fighterCapacity = Math.Min(capacity - 1, MainMenuHandler.MaxFighterHotkeys);
            bool overflow = saved.Count > fighterCapacity;
            int fighterCards = overflow ? Math.Min(capacity - 2, MainMenuHandler.MaxFighterHotkeys) : saved.Count;

            var cards = new List<RosterCard>
            {
                new()
                {
                    Key = MainMenuHandler.NewFighterKey,
                    Label = $"[{MainMenuHandler.NewFighterKey.ToUpperInvariant()}]",
                    Face = FighterFace.NewFighter,
                    Name = "NEW FIGHTER",
                    Sub = "start a fresh run",
                    Accent = green,
                    FaceColor = green,
                    Border = 2
                }
            };
            var summaries = new Dictionary<string, SavedFighterSummary?>();
            for (int i = 0; i < fighterCards; i++)
            {
                var (id, name, level) = saved[i];
                var summary = SavedFighterSummary.Load(id);
                string key = MainMenuFighterKey(i + 1);
                summaries[key] = summary;
                cards.Add(new RosterCard
                {
                    Key = key,
                    Label = $"[{i + 1}]",
                    Face = FighterFace.For(name, summary?.IsDead == true),
                    Name = name,
                    Sub = $"Level {level}",
                    Status = summary?.ClassName,
                    StatusColor = gray,
                    Accent = gold
                });
            }
            if (overflow)
            {
                cards.Add(new RosterCard
                {
                    Key = MainMenuHandler.AllFightersKey,
                    Label = $"[{MainMenuHandler.AllFightersKey.ToUpperInvariant()}]",
                    Face = new[] { "", $"+{saved.Count - fighterCards} more", "" },
                    Name = "all saved fighters",
                    Accent = gold,
                    FaceColor = gray
                });
            }

            // Detail panel for the hovered card.
            string hovered = FighterRosterHoverState.Value;
            RosterDetail? detail = null;
            if (summaries.TryGetValue(hovered, out var hoveredSummary)
                && int.TryParse(hovered.Substring(MainMenuHandler.FighterCardPrefix.Length), out int number))
            {
                var (_, name, level) = saved[number - 1];
                detail = BuildFighterDetail(name, level, number, hoveredSummary);
            }
            else if (hovered == MainMenuHandler.NewFighterKey)
            {
                detail = new RosterDetail
                {
                    Face = FighterFace.NewFighter,
                    FaceColor = green,
                    Title = "New fighter",
                    TitleColor = green,
                    BorderColor = green,
                    Header = { new RosterLine("Level 1, random name", gray) },
                    Body =
                    {
                        new RosterLine("Starts at level 1 with an optional Training Ground run, then picks a weapon.", ColorLayerSystem.GetWhite(WhiteTemperature.Neutral)),
                        new RosterLine("Gets its own save file; no existing fighter is replaced.", gray),
                        new RosterLine("", gray),
                        new RosterLine("Click or press N to begin.", gray)
                    }
                };
            }

            var placeholder = new List<RosterLine>();
            foreach (var line in WrapNewGameConfirmPrompt(newGameConfirmPrompt, 60))
                placeholder.Add(new RosterLine(line, AsciiArtAssets.Colors.Yellow));
            if (placeholder.Count == 0)
                placeholder.Add(new RosterLine(saved.Count == 0
                    ? "No fighters yet. Click + or press N to create one."
                    : "Hover a fighter for details.", gray));

            rosterRenderer.Render(x, y, width, height, new RosterScreen
            {
                StableMode = false,
                ToggleKey = MainMenuHandler.OverviewKey,
                Cards = cards,
                Detail = detail,
                DetailAnchorKey = detail != null ? hovered : null,
                Placeholder = placeholder,
                SideButtons = MainMenuSideButtons(prefix: "")
            });
            return cards.Count;
        }

        /// <summary>History / Settings / Quit, bottom right; the Stable prefixes keys so it can hand them to the main menu.</summary>
        public static List<(string Label, string Key)> MainMenuSideButtons(string prefix) => new()
        {
            ($"[{MainMenuHandler.HistoryKey.ToUpperInvariant()}] History", prefix + MainMenuHandler.HistoryKey),
            ($"[{MainMenuHandler.SettingsKey.ToUpperInvariant()}] {UIConstants.MenuOptions.Settings}", prefix + MainMenuHandler.SettingsKey),
            ($"[{MainMenuHandler.QuitKey.ToUpperInvariant()}] {UIConstants.MenuOptions.Quit}", prefix + MainMenuHandler.QuitKey)
        };

        private static RosterDetail BuildFighterDetail(string name, int level, int number, SavedFighterSummary? s)
        {
            var gold = ColorPalette.Gold.GetColor();
            var white = ColorLayerSystem.GetWhite(WhiteTemperature.Neutral);
            var gray = AsciiArtAssets.Colors.Gray;
            var orange = ColorPalette.Orange.GetColor();
            bool dead = s?.IsDead == true;
            var detail = new RosterDetail
            {
                Face = FighterFace.For(name, dead),
                FaceColor = dead ? orange : white,
                Title = name,
                TitleColor = gold,
                BorderColor = gold
            };
            if (s == null)
            {
                detail.Header.Add(new RosterLine($"Level {level}", gray));
                detail.Body.Add(new RosterLine($"Click or press {number} to continue.", gray));
                return detail;
            }

            detail.Header.Add(new RosterLine($"Level {s.Level} {s.ClassName}", white));
            int maxHp = Math.Max(1, s.MaxHealth);
            int pct = Math.Clamp(s.CurrentHealth, 0, maxHp) * 100 / maxHp;
            var hpColor = dead ? orange : pct >= 60 ? ColorPalette.Success.GetColor() : pct >= 30 ? gold : orange;
            detail.Header.Add(new RosterLine(dead ? "Fallen" : $"HP {StableHandler.Bar(s.CurrentHealth, maxHp, 12)} {Math.Max(0, s.CurrentHealth)}/{maxHp}", hpColor));
            detail.Header.Add(new RosterLine($"XP {s.Xp}", gray));

            detail.Body.Add(new RosterLine($"STR {s.Strength}   AGI {s.Agility}   TEC {s.Technique}   INT {s.Intelligence}", white));
            if (!string.IsNullOrEmpty(s.Weapon))
                detail.Body.Add(new RosterLine($"Weapon: {s.Weapon}", white));
            if (!string.IsNullOrEmpty(s.Region))
                detail.Body.Add(new RosterLine($"Region: {Capitalize(s.Region)}", white));
            detail.Body.Add(new RosterLine($"Kills {s.EnemiesDefeated}   Dungeons {s.DungeonsCompleted}   Best hit {s.HighestHit}", gray));
            detail.Body.Add(new RosterLine($"Last played {Ago(s.LastPlayed)}" + (s.PlayMinutes >= 1 ? $"   Played {FormatMinutes(s.PlayMinutes)}" : ""), gray));
            detail.Body.Add(new RosterLine("", gray));
            detail.Body.Add(new RosterLine($"Click or press {number} to continue.", gray));
            return detail;
        }

        /// <summary>History of fallen fighters (<see cref="FighterHistory"/>), newest first, with all-time bests on top.</summary>
        private int RenderHistory(int x, int y, int width, int height)
        {
            var gold = ColorPalette.Gold.GetColor();
            var white = ColorLayerSystem.GetWhite(WhiteTemperature.Neutral);
            var gray = AsciiArtAssets.Colors.Gray;
            var orange = ColorPalette.Orange.GetColor();
            int left = x + Math.Max(2, width * 13 / 100);
            int right = x + width - Math.Max(2, width * 8 / 100);
            int w = Math.Max(30, right - left);
            int top = y + Math.Max(1, height * 6 / 100);

            string back = $"[{MainMenuHandler.HistoryKey.ToUpperInvariant()}] Back";
            int backW = back.Length + 4;
            BoxButton.Draw(canvas, clickableElements, left, top, backW, 3, back, MainMenuHandler.HistoryKey,
                hovered: FighterRosterHoverState.IsOver(new ClickableElement { X = left, Y = top, Width = backW, Height = 3 }));
            canvas.AddText(left + backW + 3, top + 1, "HISTORY  -  fallen fighters", gold);

            var entries = FighterHistory.Load();
            entries.Reverse();
            int row = top + 5;
            if (entries.Count == 0)
            {
                canvas.AddText(left, row, "No fallen fighters yet. Those who die in a dungeon are remembered here.", gray);
                return 0;
            }

            var highest = entries.OrderByDescending(e => e.Level).First();
            var biggest = entries.OrderByDescending(e => e.HighestSingleHitDamage).First();
            var deadliest = entries.Where(e => !string.IsNullOrEmpty(e.KilledBy)).GroupBy(e => e.KilledBy).OrderByDescending(g => g.Count()).FirstOrDefault();
            canvas.AddText(left, row++, Fit($"{entries.Count} fallen   Highest: {highest.Name} L{highest.Level}   Biggest hit: {biggest.HighestSingleHitDamage} ({biggest.Name})"
                + (deadliest != null ? $"   Deadliest foe: {deadliest.Key} x{deadliest.Count()}" : ""), w), white);
            row++;

            int bottom = y + height - 3;
            int visible = Math.Max(1, (bottom - row) / 2);
            int scroll = Math.Clamp(MainMenuViewState.HistoryScroll, 0, Math.Max(0, entries.Count - visible));
            MainMenuViewState.HistoryScroll = scroll;
            for (int i = scroll; i < entries.Count && row + 1 <= bottom; i++)
            {
                var e = entries[i];
                string killer = string.IsNullOrEmpty(e.KilledBy) ? "fell" : $"slain by {e.KilledBy}{(e.KillerLevel > 0 ? $" L{e.KillerLevel}" : "")}";
                string where = string.IsNullOrEmpty(e.Dungeon) ? "" : $" in {e.Dungeon}{(string.IsNullOrEmpty(e.Room) ? "" : $" ({e.Room})")}";
                canvas.AddText(left, row, FighterFace.For(e.Name, dead: true)[1], orange);
                canvas.AddText(left + 9, row, Fit($"{e.Name}  L{e.Level} {e.Class}  -  {killer}{where}", w - 9), white);
                canvas.AddText(left + 9, row + 1, Fit($"{e.DiedAt:MMM d, HH:mm}   Kills {e.EnemiesDefeated}   Dungeons {e.DungeonsCompleted}   Best hit {e.HighestSingleHitDamage}   Best combo {e.HighestComboStep}", w - 9), gray);
                row += 2;
            }
            if (entries.Count > visible)
                canvas.AddText(left, bottom + 1, $"{scroll + 1}-{Math.Min(entries.Count, scroll + visible)} of {entries.Count}   Up/Down to scroll   R or Esc to go back", gray);
            return entries.Count;
        }

        private static string Ago(DateTime when)
        {
            var span = DateTime.Now - when;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalHours < 1) return $"{(int)span.TotalMinutes}m ago";
            if (span.TotalDays < 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
            return when.ToString("MMM d, yyyy");
        }

        private static string FormatMinutes(double minutes) =>
            minutes < 60 ? $"{(int)minutes}m" : $"{(int)(minutes / 60)}h {(int)(minutes % 60)}m";

        private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private static string Fit(string text, int max) =>
            max <= 1 ? "" : text.Length <= max ? text : text.Substring(0, max - 1) + ".";

        /// <summary>Input sent by the Nth fighter card (1-based, save list order).</summary>
        public static string MainMenuFighterKey(int number) => MainMenuHandler.FighterCardPrefix + number;

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

        /// <summary>The Stable grid in the main menu's roster layout.</summary>
        public void RenderStableRoster(int x, int y, int width, int height, StableMenuView view) => stableScreenRenderer.RenderRoster(x, y, width, height, view);

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
