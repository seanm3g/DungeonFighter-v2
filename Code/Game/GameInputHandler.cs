namespace RPGGame
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles all user input routing based on game state.
    /// This manager centralizes input handling logic extracted from Game.cs.
    /// 
    /// Responsibilities:
    /// - Route input based on current game state
    /// - Validate input appropriateness
    /// - Provide structured input handling callbacks
    /// - Support async and sync input processing
    /// 
    /// Design: Event-driven coordinator pattern
    /// Usage: Subscribe to input events, Game.cs processes them
    /// </summary>
    public class GameInputHandler
    {
        private GameStateManager stateManager;

        // Input events that Game.cs subscribes to
        public delegate Task InputProcessorAsync(string input);
        public delegate void InputProcessor(string input);

        public event InputProcessorAsync? OnMainMenuInput;
        public event InputProcessorAsync? OnWeaponSelectionInput;
        public event InputProcessorAsync? OnCharacterCreationInput;
        public event InputProcessor? OnInventoryInput;
        public event InputProcessor? OnCharacterInfoInput;
        public event InputProcessor? OnSettingsInput;
        public event InputProcessor? OnTestingInput;
        public event InputProcessorAsync? OnGameLoopInput;
        public event InputProcessorAsync? OnDungeonSelectionInput;
        public event InputProcessorAsync? OnDungeonCompletionInput;
        public event InputProcessorAsync? OnDeathScreenInput;
        public event InputProcessorAsync? OnEscapeKey;
        public event InputProcessor? OnCombatScrollInput;

        public GameInputHandler(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        /// <summary>
        /// Main input handler - routes input based on current game state.
        /// This is the entry point for all input processing.
        /// </summary>
        /// <param name="input">The input string from the user.</param>
        public async Task HandleInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return;

            try
            {
                // Route input based on current state
                switch (stateManager.CurrentState)
                {
                    case GameState.MainMenu:
                        await RaiseMainMenuInput(input);
                        break;

                    case GameState.WeaponSelection:
                        await RaiseWeaponSelectionInput(input);
                        break;

                    case GameState.CharacterCreation:
                        await RaiseCharacterCreationInput(input);
                        break;

                    case GameState.Inventory:
                        RaiseInventoryInput(input);
                        break;

                    case GameState.CharacterInfo:
                        RaiseCharacterInfoInput(input);
                        break;

                    case GameState.Settings:
                        RaiseSettingsInput(input);
                        break;

                    case GameState.Testing:
                        RaiseTestingInput(input);
                        break;

                    case GameState.GameLoop:
                        await RaiseGameLoopInput(input);
                        break;

                    case GameState.DungeonSelection:
                        await RaiseDungeonSelectionInput(input);
                        break;

                    case GameState.DungeonCompletion:
                        await RaiseDungeonCompletionInput(input);
                        break;

                    case GameState.Death:
                        await RaiseDeathScreenInput(input);
                        break;

                    case GameState.Dungeon:
                    case GameState.Combat:
                        // During dungeon/combat, allow scrolling with arrow keys
                        // Other input is handled automatically
                        if (input == "up" || input == "down" || input == "pageup" || input == "pagedown")
                        {
                            // Allow scrolling - will be handled by Game.cs
                            RaiseCombatScrollInput(input);
                        }
                        break;

                    default:
                        // Unknown state - no handler
                        break;
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error handling input: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles escape key press from any state.
        /// Raises escape key event for Game.cs to process.
        /// </summary>
        public async Task HandleEscapeKey()
        {
            try
            {
                if (OnEscapeKey != null)
                {
                    await OnEscapeKey("");
                }
            }
            catch (Exception ex)
            {
                UIManager.WriteSystemLine($"Error handling escape key: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that input is appropriate for current state.
        /// </summary>
        public bool ValidateInputForState(string input, GameState state)
        {
            if (string.IsNullOrEmpty(input)) return false;

            // Can add state-specific validation here
            return true;
        }

        /// <summary>
        /// Gets a description of valid input for current state.
        /// </summary>
        public string GetInputHelpText(GameState state)
        {
            return state switch
            {
                GameState.MainMenu => "1=New Game, 2=Load Game, 3=Settings, 0=Quit",
                GameState.Inventory => "1-7=Action, ESC=Back",
                GameState.Settings => "Adjust settings or press ESC to back",
                GameState.GameLoop => "1=Dungeon, 2=Inventory, 3=Character, 4=Settings, 0=Quit",
                _ => "Enter input"
            };
        }

        // Private helper methods to raise events

        private async Task RaiseMainMenuInput(string input)
        {
            if (OnMainMenuInput != null)
                await OnMainMenuInput(input);
        }

        private async Task RaiseWeaponSelectionInput(string input)
        {
            if (OnWeaponSelectionInput != null)
                await OnWeaponSelectionInput(input);
        }

        private async Task RaiseCharacterCreationInput(string input)
        {
            if (OnCharacterCreationInput != null)
                await OnCharacterCreationInput(input);
        }

        private void RaiseInventoryInput(string input)
        {
            OnInventoryInput?.Invoke(input);
        }

        private void RaiseCharacterInfoInput(string input)
        {
            OnCharacterInfoInput?.Invoke(input);
        }

        private void RaiseSettingsInput(string input)
        {
            OnSettingsInput?.Invoke(input);
        }

        private void RaiseTestingInput(string input)
        {
            OnTestingInput?.Invoke(input);
        }

        private async Task RaiseGameLoopInput(string input)
        {
            if (OnGameLoopInput != null)
                await OnGameLoopInput(input);
        }

        private async Task RaiseDungeonSelectionInput(string input)
        {
            if (OnDungeonSelectionInput != null)
                await OnDungeonSelectionInput(input);
        }

        private async Task RaiseDungeonCompletionInput(string input)
        {
            if (OnDungeonCompletionInput != null)
                await OnDungeonCompletionInput(input);
        }

        private async Task RaiseDeathScreenInput(string input)
        {
            if (OnDeathScreenInput != null)
                await OnDeathScreenInput(input);
        }

        private void RaiseCombatScrollInput(string input)
        {
            OnCombatScrollInput?.Invoke(input);
        }

        /// <summary>
        /// Gets a string representation of the input handler.
        /// </summary>
        public override string ToString()
        {
            return $"GameInputHandler (routes {stateManager.CurrentState} input)";
        }
    }
}
