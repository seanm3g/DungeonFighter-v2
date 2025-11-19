using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Represents the result of processing menu input.
    /// Provides a consistent way to communicate success/failure and next actions.
    /// </summary>
    public class MenuInputResult
    {
        /// <summary>
        /// Gets whether the input was processed successfully.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the message (error, status, etc.)
        /// </summary>
        public string? Message { get; private set; }

        /// <summary>
        /// Gets the next game state to transition to (if any).
        /// Null means no state change.
        /// </summary>
        public GameState? NextState { get; private set; }

        /// <summary>
        /// Gets the command to execute (if any).
        /// Null means no command execution needed.
        /// </summary>
        public IMenuCommand? Command { get; private set; }

        /// <summary>
        /// Private constructor - use factory methods instead
        /// </summary>
        private MenuInputResult(
            bool success,
            string? message = null,
            GameState? nextState = null,
            IMenuCommand? command = null)
        {
            IsSuccess = success;
            Message = message;
            NextState = nextState;
            Command = command;
        }

        /// <summary>
        /// Factory method for successful results with optional state transition.
        /// </summary>
        public static MenuInputResult Success(
            GameState? nextState = null,
            IMenuCommand? command = null,
            string? message = null)
        {
            return new MenuInputResult(
                success: true,
                message: message,
                nextState: nextState,
                command: command);
        }

        /// <summary>
        /// Factory method for failed results with error message.
        /// </summary>
        public static MenuInputResult Failure(string errorMessage)
        {
            return new MenuInputResult(
                success: false,
                message: errorMessage);
        }

        /// <summary>
        /// Factory method for results with just a state transition (no error/message).
        /// </summary>
        public static MenuInputResult StateTransition(GameState nextState)
        {
            return new MenuInputResult(
                success: true,
                nextState: nextState);
        }

        /// <summary>
        /// Factory method for results with command execution.
        /// </summary>
        public static MenuInputResult WithCommand(IMenuCommand command)
        {
            return new MenuInputResult(
                success: true,
                command: command);
        }
    }
}

