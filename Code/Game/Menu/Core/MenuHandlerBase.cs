using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Abstract base class for all menu handlers.
    /// Provides common functionality for input processing, validation, and error handling.
    /// All menu handlers should inherit from this class.
    /// </summary>
    public abstract class MenuHandlerBase : IMenuHandler
    {
        /// <summary>
        /// Gets the state manager for commands to access game state.
        /// </summary>
        protected GameStateManager? StateManager { get; set; }

        /// <summary>
        /// Gets the game state this handler is responsible for.
        /// Subclasses must override this property.
        /// </summary>
        public abstract GameState TargetState { get; }

        /// <summary>
        /// Gets the name of this handler (for logging).
        /// </summary>
        protected abstract string HandlerName { get; }

        /// <summary>
        /// Processes user input with error handling and logging.
        /// This method handles the input processing flow and logs all steps.
        /// </summary>
        public async Task<MenuInputResult> HandleInput(string input)
        {
            try
            {
                LogStep($"Input received: '{input}'");

                // 1. Validate input
                if (string.IsNullOrWhiteSpace(input))
                {
                    LogError("Input is null or empty");
                    return MenuInputResult.Failure("Input cannot be empty");
                }

                LogStep($"Input is valid, parsing...");

                // 2. Parse input into command
                var command = ParseInput(input);
                if (command == null)
                {
                    LogError($"Failed to parse input: '{input}'");
                    return MenuInputResult.Failure("Invalid input option");
                }

                LogStep($"Input parsed successfully, executing command...");

                // 3. Execute command and get result
                var nextState = await ExecuteCommand(command);

                LogStep($"Command executed, returning result");

                // 4. Return result with optional state transition
                return MenuInputResult.Success(nextState, command);
            }
            catch (Exception ex)
            {
                LogError($"Exception during input handling: {ex.Message}");
                return MenuInputResult.Failure("Error processing input");
            }
        }

        /// <summary>
        /// Parse the input string into a menu command.
        /// Subclasses must override this to implement their specific input parsing logic.
        /// </summary>
        /// <param name="input">The input string to parse</param>
        /// <returns>The parsed command, or null if input is invalid</returns>
        protected abstract IMenuCommand? ParseInput(string input);

        /// <summary>
        /// Execute a command and return the next game state (if any).
        /// Subclasses must override this to implement their specific command execution logic.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The next game state, or null if no state change</returns>
        protected abstract Task<GameState?> ExecuteCommand(IMenuCommand command);

        /// <summary>
        /// Helper method for safe logging of handler steps.
        /// </summary>
        protected void LogStep(string step)
        {
            DebugLogger.Log(HandlerName, step);
        }

        /// <summary>
        /// Helper method for error logging.
        /// </summary>
        protected void LogError(string error)
        {
            DebugLogger.Log(HandlerName, $"ERROR: {error}");
        }
    }
}

