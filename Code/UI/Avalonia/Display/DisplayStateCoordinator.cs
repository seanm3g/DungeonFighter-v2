using RPGGame;
using RPGGame.ActionInteractionLab;

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
        /// Matches by reference first, then by registry character id when both ids are known (avoids stale canvas context when instances differ).
        /// </summary>
        public static bool IsCharacterActive(Character? character, GameStateManager? stateManager)
        {
            if (character == null || stateManager == null)
                return false;

            if (stateManager.CurrentState == GameState.ActionInteractionLab)
            {
                var lab = ActionInteractionLabSession.Current;
                if (lab != null && ReferenceEquals(character, lab.LabPlayer))
                    return true;
            }

            var activeCharacter = stateManager.GetActiveCharacter();
            if (ReferenceEquals(character, activeCharacter))
                return true;

            string? charId = stateManager.GetCharacterId(character);
            string? activeId = stateManager.GetActiveCharacterId();
            return !string.IsNullOrEmpty(charId) && !string.IsNullOrEmpty(activeId) &&
                   string.Equals(charId, activeId, System.StringComparison.Ordinal);
        }
        
        /// <summary>
        /// Determines the appropriate display mode based on the current game state.
        /// </summary>
        public static DisplayMode DetermineDisplayMode(GameState? state)
        {
            if (state == null)
                return new StandardDisplayMode();
            
            if (state == GameState.Combat || state == GameState.ActionInteractionLab)
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

            // Defensive: avoid running display-buffer layout before CurrentState is observable
            if (state == null)
                return true;
            
            // Menu states should suppress rendering
            if (IsMenuState(state))
                return true;
            
            return false;
        }
    }
}

