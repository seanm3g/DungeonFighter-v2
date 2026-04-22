using RPGGame;
using RPGGame.ActionInteractionLab;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Managers;
using System;

namespace RPGGame.UI.Avalonia.Renderers.Validators
{
    /// <summary>
    /// Validates whether combat rendering should proceed based on character activity.
    /// Extracts character activity validation logic from CanvasRenderer to improve Single Responsibility Principle compliance.
    /// </summary>
    public class CombatRenderingValidator
    {
        private readonly ICanvasContextManager contextManager;
        private GameStateManager? gameStateManager;

        public CombatRenderingValidator(ICanvasContextManager contextManager, GameStateManager? gameStateManager = null)
        {
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
            this.gameStateManager = gameStateManager;
        }

        /// <summary>
        /// Wires game state so menu-only states (e.g. Death) block stale debounced combat repaints.
        /// </summary>
        public void SetGameStateManager(GameStateManager? stateManager)
        {
            this.gameStateManager = stateManager;
        }

        /// <summary>
        /// Checks if the given character is currently active and should be rendered.
        /// </summary>
        /// <param name="character">The character to check</param>
        /// <returns>True if the character is active and should be rendered, false otherwise</returns>
        public bool IsCharacterActive(Character? character)
        {
            if (character == null)
                return false;

            var lab = ActionInteractionLabSession.Current;
            if (lab != null && ReferenceEquals(character, lab.LabPlayer))
                return true;

            // Menu / hub states paint their own center content; a queued combat debounce must not repaint over them.
            if (gameStateManager != null &&
                DisplayStateCoordinator.ShouldSuppressRendering(gameStateManager.CurrentState, gameStateManager))
                return false;

            Character? activePlayer = contextManager.GetCurrentCharacter();
            return activePlayer == character;
        }

        /// <summary>
        /// Validates that the character is active before rendering.
        /// Throws an exception if validation fails (for use in early-return patterns).
        /// </summary>
        /// <param name="character">The character to validate</param>
        /// <returns>True if valid, false if character is not active</returns>
        public bool ValidateCharacterActive(Character? character)
        {
            return IsCharacterActive(character);
        }
    }
}

