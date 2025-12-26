using RPGGame;
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

        public CombatRenderingValidator(ICanvasContextManager contextManager)
        {
            this.contextManager = contextManager ?? throw new ArgumentNullException(nameof(contextManager));
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

