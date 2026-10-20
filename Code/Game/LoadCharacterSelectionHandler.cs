namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Avalonia.Threading;
    using RPGGame.Audio;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// One page of the Load Character menu, as handed to the renderer.
    /// </summary>
    public sealed class LoadCharacterMenuView
    {
        public List<(int number, string characterName, int level)> Entries { get; init; } = new();
        public int Page { get; init; }
        public int PageCount { get; init; }
        public int TotalCount { get; init; }
        public bool HasPreviousPage => Page > 0;
        public bool HasNextPage => Page < PageCount - 1;
        /// <summary>Delete-mode / confirm prompt shown under the list, or null for the default help line.</summary>
        public string? Prompt { get; init; }
        public bool PromptIsWarning { get; init; }
    }

    /// <summary>
    /// Load Character menu: lists every saved character on disk (most recently saved first), nine per page.
    /// 1-9 load, N/P page, D deletes a save (with confirmation), 0 returns to the main menu.
    /// </summary>
    public class LoadCharacterSelectionHandler
    {
        public const int PageSize = 9;

        private GameStateManager stateManager;
        private IUIManager? customUIManager;
        private GameInitializer gameInitializer;

        // Listing every save deserializes each file, so cache it while the menu is open.
        private List<(string characterId, string characterName, int level)> savedCharacters = new();
        private int currentPage;
        private bool deleteMode;
        private int? pendingDeleteIndex;

        // Delegates
        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        public delegate void OnShowMessage(string message);
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnShowMessage? ShowMessageEvent;

        public LoadCharacterSelectionHandler(
            GameStateManager stateManager,
            IUIManager? customUIManager,
            GameInitializer gameInitializer)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.gameInitializer = gameInitializer ?? throw new ArgumentNullException(nameof(gameInitializer));
        }

        public int CurrentPage => currentPage;
        public int PageCount => Math.Max(1, (savedCharacters.Count + PageSize - 1) / PageSize);
        public bool IsInDeleteMode => deleteMode || pendingDeleteIndex.HasValue;

        /// <summary>
        /// Shows the load character selection menu and transitions to LoadCharacterSelection state
        /// </summary>
        public async Task ShowLoadCharacterSelection()
        {
            try
            {
                stateManager.TransitionToState(GameState.LoadCharacterSelection);

                currentPage = 0;
                ClearDeleteState();
                ReloadSavedCharacters();

                if (savedCharacters.Count == 0)
                    ShowMessageEvent?.Invoke("No saved characters found.");

                Render();
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error showing character selection: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in ShowLoadCharacterSelection: {ex}");
                stateManager.TransitionToState(GameState.MainMenu);
                ShowMainMenuEvent?.Invoke();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Handles input for load character selection menu
        /// </summary>
        public async Task HandleLoadCharacterSelectionInput(string input)
        {
            // Input can arrive before Show (tests, MCP); list lazily so the first keypress still works.
            if (savedCharacters.Count == 0)
                ReloadSavedCharacters();

            string key = (input ?? "").Trim().ToLowerInvariant();

            if (pendingDeleteIndex.HasValue)
            {
                HandleDeleteConfirm(key);
                return;
            }

            switch (key)
            {
                case "0":
                case "escape":
                    if (deleteMode)
                    {
                        ClearDeleteState();
                        Render();
                        return;
                    }
                    ReturnToMainMenu();
                    return;
                case "n":
                case "right":
                case "pagedown":
                    ChangePage(+1);
                    return;
                case "p":
                case "left":
                case "pageup":
                    ChangePage(-1);
                    return;
                case "d":
                    if (savedCharacters.Count == 0)
                    {
                        ShowMessageEvent?.Invoke("No saved characters to delete.");
                        return;
                    }
                    deleteMode = !deleteMode;
                    AudioCues.Trigger(AudioCue.Menu_Confirm);
                    Render();
                    return;
            }

            if (!int.TryParse(key, out int choice))
            {
                AudioCues.Trigger(AudioCue.Menu_Invalid);
                ShowMessageEvent?.Invoke("Invalid input. Enter a number, N/P to change page, D to delete, or 0 to return.");
                return;
            }

            int index = currentPage * PageSize + (choice - 1);
            if (choice < 1 || choice > PageSize || index >= savedCharacters.Count)
            {
                AudioCues.Trigger(AudioCue.Menu_Invalid);
                ShowMessageEvent?.Invoke(savedCharacters.Count == 0 ? "Invalid choice. Press 0 to return." : "Invalid choice.");
                return;
            }

            if (deleteMode)
            {
                deleteMode = false;
                pendingDeleteIndex = index;
                Render();
                return;
            }

            var selected = savedCharacters[index];
            if (StableManager.Instance.Find(selected.characterId) is { } fighter)
            {
                TakeFromStable(fighter);
                return;
            }
            await LoadCharacter(selected.characterId, selected.characterName);
        }

        /// <summary>
        /// The hero is already loaded in the Stable: play that same instance (taken in hand) rather than a second copy
        /// from disk, which would overwrite each other's saves. Refused while it is out in a dungeon.
        /// </summary>
        private void TakeFromStable(StableFighter fighter)
        {
            var stable = StableManager.Instance;
            if (fighter.Status != StableFighterStatus.InHand && !stable.CheckOut(fighter))
            {
                AudioCues.Trigger(AudioCue.Menu_Invalid);
                ShowMessageEvent?.Invoke($"{fighter.Character.Name} is busy in the Stable. Open the Stable from the main menu.");
                return;
            }

            var hero = fighter.Character;
            stateManager.AddCharacter(hero, fighter.CharacterId);
            stateManager.SetCurrentPlayer(hero);
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RestoreDisplayBufferRendering();
                canvasUI.SetCharacter(hero);
                canvasUI.RefreshCharacterPanel();
            }
            ShowMessageEvent?.Invoke($"Took {hero.Name} from the Stable. Press 0 at the hub to send them back.");
            stateManager.TransitionToState(GameState.GameLoop);
            ShowGameLoopEvent?.Invoke();
        }

        private void HandleDeleteConfirm(string key)
        {
            int index = pendingDeleteIndex!.Value;
            ClearDeleteState();

            if (key == "y" && index < savedCharacters.Count)
            {
                var (characterId, characterName, level) = savedCharacters[index];
                // Legacy single-file saves (character_save.json) map to the default filename.
                string? filename = characterId.EndsWith("_legacy")
                    ? null
                    : CharacterSaveManager.GetCharacterSaveFilename(characterId);
                CharacterSaveManager.DeleteSaveFile(filename);
                ShowMessageEvent?.Invoke($"Deleted {characterName} (Level {level}).");

                ReloadSavedCharacters();
                currentPage = Math.Min(currentPage, PageCount - 1);
            }
            else
            {
                ShowMessageEvent?.Invoke("Delete cancelled.");
            }

            Render();
        }

        private void ChangePage(int delta)
        {
            int target = currentPage + delta;
            if (target < 0 || target >= PageCount)
            {
                AudioCues.Trigger(AudioCue.Menu_Invalid);
                return;
            }
            currentPage = target;
            pendingDeleteIndex = null;
            Render();
        }

        private void ReturnToMainMenu()
        {
            ClearDeleteState();
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RestoreDisplayBufferRendering();
            }
            stateManager.TransitionToState(GameState.MainMenu);
            ShowMainMenuEvent?.Invoke();
        }

        private void ClearDeleteState()
        {
            deleteMode = false;
            pendingDeleteIndex = null;
        }

        private void ReloadSavedCharacters()
        {
            savedCharacters = CharacterSaveManager.ListAllSavedCharacters()
                ?? new List<(string characterId, string characterName, int level)>();
        }

        /// <summary>Builds the visible page (also used by tests).</summary>
        public LoadCharacterMenuView BuildView()
        {
            int start = currentPage * PageSize;
            var entries = savedCharacters
                .Skip(start)
                .Take(PageSize)
                .Select((c, i) => (i + 1, c.characterName, c.level))
                .ToList();

            string? prompt = null;
            bool warning = false;
            if (pendingDeleteIndex.HasValue && pendingDeleteIndex.Value < savedCharacters.Count)
            {
                var c = savedCharacters[pendingDeleteIndex.Value];
                prompt = $"Delete {c.characterName} (Level {c.level}) permanently? Press Y to confirm, any other key to cancel.";
                warning = true;
            }
            else if (deleteMode)
            {
                prompt = "DELETE MODE: press the number of the character to delete, or D / 0 to cancel.";
                warning = true;
            }

            return new LoadCharacterMenuView
            {
                Entries = entries,
                Page = currentPage,
                PageCount = PageCount,
                TotalCount = savedCharacters.Count,
                Prompt = prompt,
                PromptIsWarning = warning
            };
        }

        private void Render()
        {
            var view = BuildView();
            if (customUIManager is CanvasUICoordinator canvasUI)
                RenderLoadCharacterSelection(canvasUI, view);
            else
                ShowLoadCharacterListConsole(view);
        }

        /// <summary>
        /// Loads a character by character ID and transitions to game loop
        /// </summary>
        private async Task LoadCharacter(string characterId, string characterName)
        {
            try
            {
                ShowMessageEvent?.Invoke($"Loading {characterName}...");

                // Show loading animation
                if (customUIManager is CanvasUICoordinator canvasUI)
                {
                    canvasUI.ShowLoadingAnimation("Loading character...");
                }

                bool isLegacy = characterId.EndsWith("_legacy");
                Character? loadedCharacter = isLegacy
                    ? await Character.LoadCharacterAsync().ConfigureAwait(false)
                    : await Character.LoadCharacterAsync(characterId).ConfigureAwait(false);

                if (loadedCharacter == null)
                {
                    if (customUIManager is CanvasUICoordinator canvasUIClear)
                    {
                        Dispatcher.UIThread.Post(() => canvasUIClear.ClearLoadingStatus());
                    }
                    ShowMessageEvent?.Invoke($"Failed to load character: {characterName}");
                    return;
                }

                // Register under the save's own ID so later saves overwrite this file instead of
                // writing a fresh duplicate. Legacy saves get a new ID and migrate to a per-character file.
                stateManager.AddCharacter(loadedCharacter, isLegacy ? null : characterId);
                stateManager.SetCurrentPlayer(loadedCharacter);

                // Apply health multiplier if configured
                var settings = GameSettings.Instance;
                if (settings.PlayerHealthMultiplier != 1.0)
                {
                    loadedCharacter.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);
                }

                var ch = loadedCharacter;
                Dispatcher.UIThread.Post(() =>
                {
                    if (customUIManager is CanvasUICoordinator canvasUIUpdate)
                    {
                        canvasUIUpdate.ClearLoadingStatus();
                        canvasUIUpdate.RestoreDisplayBufferRendering();
                        canvasUIUpdate.SetCharacter(ch);
                        canvasUIUpdate.RefreshCharacterPanel();
                    }
                    ShowMessageEvent?.Invoke($"Loaded {ch.Name} (Level {ch.Level})");
                    stateManager.TransitionToState(GameState.GameLoop);
                    ShowGameLoopEvent?.Invoke();
                });
            }
            catch (Exception ex)
            {
                if (customUIManager is CanvasUICoordinator canvasUIError)
                {
                    Dispatcher.UIThread.Post(() => canvasUIError.ClearLoadingStatus());
                }
                ShowMessageEvent?.Invoke($"Error loading character: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in LoadCharacter: {ex}");
            }
        }

        /// <summary>
        /// Renders load character selection screen for CanvasUI
        /// </summary>
        private void RenderLoadCharacterSelection(CanvasUICoordinator canvasUI, LoadCharacterMenuView view)
        {
            try
            {
                // Clear canvas first like other menus do
                canvasUI.Clear();

                // Suppress display buffer auto-rendering to prevent conflicts
                canvasUI.SuppressDisplayBufferRendering();
                canvasUI.ClearDisplayBufferWithoutRender();

                canvasUI.RenderLoadCharacterSelection(view);
            }
            catch (Exception ex)
            {
                ShowMessageEvent?.Invoke($"Error rendering load character selection: {ex.Message}");
                DebugLogger.Log("LoadCharacterSelectionHandler", $"Error in RenderLoadCharacterSelection: {ex}");
                // Try to restore rendering even on error
                try
                {
                    canvasUI.RestoreDisplayBufferRendering();
                    canvasUI.Refresh();
                }
                catch { }
            }
        }

        /// <summary>
        /// Shows saved character list in console mode
        /// </summary>
        private void ShowLoadCharacterListConsole(LoadCharacterMenuView view)
        {
            UIManager.WriteLine("=== LOAD CHARACTER ===");
            UIManager.WriteBlankLine();

            if (view.TotalCount == 0)
            {
                UIManager.WriteMenuLine("No saved characters found.");
                UIManager.WriteBlankLine();
                UIManager.WriteMenuLine("0. Back to Main Menu");
                UIManager.WriteBlankLine();
                UIManager.WriteSystemLine("Press 0 to return.");
                return;
            }

            foreach (var (number, characterName, level) in view.Entries)
            {
                UIManager.WriteMenuLine($"{number}. {characterName} - Level {level}");
            }

            UIManager.WriteBlankLine();
            if (view.PageCount > 1)
                UIManager.WriteMenuLine($"Page {view.Page + 1}/{view.PageCount}  (N) Next  (P) Previous");
            UIManager.WriteMenuLine("D. Delete a character");
            UIManager.WriteMenuLine("0. Back to Main Menu");
            UIManager.WriteBlankLine();
            UIManager.WriteSystemLine(view.Prompt ?? "Select a character number to load, or press 0 to return.");
        }
    }
}
