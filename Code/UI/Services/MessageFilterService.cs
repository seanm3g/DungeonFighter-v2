using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Services
{
    /// <summary>
    /// Single source of truth for message filtering logic
    /// Consolidates character/state filtering that was duplicated across multiple files
    /// </summary>
    public class MessageFilterService
    {
        /// <summary>
        /// Menu states that should block combat and system messages
        /// </summary>
        private static readonly HashSet<GameState> MenuStates = new HashSet<GameState>
        {
            GameState.MainMenu,
            GameState.Inventory,
            GameState.CharacterInfo,
            GameState.Settings,
            GameState.DeveloperMenu,
            GameState.Testing,
            GameState.DungeonSelection,
            GameState.GameLoop,
            GameState.CharacterCreation,
            GameState.WeaponSelection,
            GameState.DungeonCompletion,
            GameState.Death,
            GameState.BattleStatistics,
            GameState.VariableEditor,
            GameState.TuningParameters,
            GameState.ActionEditor,
            GameState.CreateAction,
            GameState.ViewAction,
            GameState.CharacterSelection
        };

        /// <summary>
        /// Determines if a message should be displayed based on game state, character, and message type
        /// </summary>
        /// <param name="sourceCharacter">The character this message belongs to (null = always display if not in menu)</param>
        /// <param name="messageType">The type of message (Combat, System, etc.)</param>
        /// <param name="stateManager">Game state manager (null = always display for backward compatibility)</param>
        /// <param name="contextManager">Context manager for getting current character (optional)</param>
        /// <param name="performRaceConditionCheck">If true, performs additional race condition check for Dungeon state</param>
        /// <returns>True if message should be displayed, false otherwise</returns>
        public bool ShouldDisplayMessage(
            Character? sourceCharacter,
            UIMessageType messageType,
            GameStateManager? stateManager,
            ICanvasContextManager? contextManager = null,
            bool performRaceConditionCheck = false)
        {
            // No state manager - always display (backward compatibility)
            if (stateManager == null)
            {
                return true;
            }

            var currentState = stateManager.CurrentState;
            bool isMenuState = MenuStates.Contains(currentState);

            // Don't add combat/system messages if we're in a menu state
            if (isMenuState && (messageType == UIMessageType.Combat || messageType == UIMessageType.System))
            {
                return false;
            }

            // If we're in a menu state and it's not a combat/system message, allow it
            if (isMenuState)
            {
                return true;
            }

            // For non-menu states, check if character matches active character (for multi-character support)
            if (sourceCharacter != null)
            {
                var activeCharacter = stateManager.GetActiveCharacter();
                
                // No active character - block the message
                if (activeCharacter == null)
                {
                    return false;
                }

                // Character doesn't match - block the message
                if (activeCharacter != sourceCharacter)
                {
                    return false;
                }

                // Optional race condition check for Dungeon state
                // This ensures that even if the context manager was updated between the check and now,
                // we still block messages from inactive characters
                if (performRaceConditionCheck && currentState == GameState.Dungeon)
                {
                    var recheckActiveCharacter = stateManager.GetActiveCharacter();
                    if (recheckActiveCharacter != sourceCharacter)
                    {
                        return false;
                    }
                }
            }
            else if (contextManager != null)
            {
                // No source character provided, but we have a context manager
                // Check if the current character in context matches the active character
                var currentCharacter = contextManager.GetCurrentCharacter();
                var activeCharacter = stateManager.GetActiveCharacter();

                if (currentCharacter != null)
                {
                    // Character doesn't match - block the message
                    if (currentCharacter != activeCharacter)
                    {
                        return false;
                    }

                    // Optional race condition check for Dungeon state
                    if (performRaceConditionCheck && currentState == GameState.Dungeon)
                    {
                        var recheckActiveCharacter = stateManager.GetActiveCharacter();
                        if (currentCharacter != recheckActiveCharacter)
                        {
                            return false;
                        }
                    }
                }
            }

            // All checks passed - display the message
            return true;
        }


        /// <summary>
        /// Determines if we're currently in a menu state
        /// </summary>
        public bool IsMenuState(GameStateManager? stateManager)
        {
            if (stateManager == null)
            {
                return false;
            }

            return MenuStates.Contains(stateManager.CurrentState);
        }
    }
}

