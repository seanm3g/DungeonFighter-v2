using System;
using System.Threading.Tasks;
using RPGGame;

namespace DungeonFighter.Game.Menu.Core
{
    /// <summary>
    /// Base class for all menu commands.
    /// Provides common functionality for command execution, error handling, and logging.
    /// </summary>
    public abstract class MenuCommand : IMenuCommand
    {
        /// <summary>
        /// Gets the name of this command (for logging).
        /// </summary>
        protected abstract string CommandName { get; }

        /// <summary>
        /// Executes the command with error handling and logging.
        /// </summary>
        public async Task Execute(IMenuContext? context)
        {
            try
            {
                DebugLogger.Log("MenuCommand", $"Executing: {CommandName}");
                if (context != null)
                {
                    await ExecuteCommand(context);
                }
                DebugLogger.Log("MenuCommand", $"Completed: {CommandName}");
            }
            catch (Exception ex)
            {
                DebugLogger.Log("MenuCommand", $"Error executing {CommandName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Override this method to implement the actual command logic.
        /// </summary>
        protected abstract Task ExecuteCommand(IMenuContext? context);

        /// <summary>
        /// Helper method for safe logging of command execution.
        /// </summary>
        protected void LogStep(string step)
        {
            DebugLogger.Log(CommandName, step);
        }

        /// <summary>
        /// Helper method for error logging.
        /// </summary>
        protected void LogError(string error)
        {
            DebugLogger.Log(CommandName, error);
        }
    }
}

