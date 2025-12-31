using RPGGame;

namespace RPGGame.UI.Avalonia.Display
{
    /// <summary>
    /// Centralized coordinator for display state management.
    /// Single source of truth for menu state detection, character validation,
    /// display mode determination, and rendering suppression logic.
    /// </summary>
    public static class DisplayStateCoordinator
    {
        /// <summary>
        /// Determines if the given game state is a menu state.
        /// Menu states suppress display buffer rendering as they handle their own rendering.
        /// </summary>
        public static bool IsMenuState(GameState? state)
        {
            if (state == null)
                return false;
            
            return state == GameState.MainMenu ||
                   state == GameState.GameLoop ||
                   state == GameState.WeaponSelection ||
                   state == GameState.CharacterCreation ||
                   state == GameState.CharacterSelection ||
                   state == GameState.LoadCharacterSelection ||
                   state == GameState.Inventory ||
                   state == GameState.CharacterInfo ||
                   state == GameState.Settings ||
                   state == GameState.Testing ||
                   state == GameState.DeveloperMenu ||
                   state == GameState.BattleStatistics ||
                   state == GameState.VariableEditor ||
                   state == GameState.TuningParameters ||
                   state == GameState.ActionEditor ||
                   state == GameState.CreateAction ||
                   state == GameState.EditAction ||
                   state == GameState.ViewAction ||
                   state == GameState.DeleteActionConfirmation ||
                   state == GameState.DungeonSelection ||
                   state == GameState.DungeonCompletion ||
                   state == GameState.Death;
        }
        
        /// <summary>
        /// Determines if a character is currently the active character.
        /// Used to prevent background combat from affecting the display.
        /// </summary>
        public static bool IsCharacterActive(Character? character, GameStateManager? stateManager)
        {
            if (character == null || stateManager == null)
                return false;
            
            var activeCharacter = stateManager.GetActiveCharacter();
            return character == activeCharacter;
        }
        
        /// <summary>
        /// Determines the appropriate display mode based on the current game state.
        /// </summary>
        public static DisplayMode DetermineDisplayMode(GameState? state)
        {
            if (state == null)
                return new StandardDisplayMode();
            
            if (state == GameState.Combat)
                return new CombatDisplayMode();
            
            if (IsMenuState(state))
                return new MenuDisplayMode();
            
            return new StandardDisplayMode();
        }
        
        /// <summary>
        /// Determines if display buffer rendering should be suppressed.
        /// Suppresses rendering when in menu states or when state manager is null (title screen).
        /// </summary>
        public static bool ShouldSuppressRendering(GameState? state, GameStateManager? stateManager)
        {
            // Title screen (stateManager is null) should suppress rendering
            if (stateManager == null)
                return true;
            
            // Menu states should suppress rendering
            if (IsMenuState(state))
                return true;
            
            return false;
        }
    }
}

