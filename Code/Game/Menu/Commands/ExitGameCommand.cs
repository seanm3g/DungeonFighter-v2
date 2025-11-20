using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for exiting the game.
    /// Closes the game application.
    /// </summary>
    public class ExitGameCommand : MenuCommand
    {
        protected override string CommandName => "ExitGame";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Exiting game");
            
            // TODO: When integrating with Game.cs:
            // 1. Save any necessary data
            // 2. Clean up resources
            // 3. Exit application
            
            LogStep("Game exit initiated");
            await Task.CompletedTask;
        }
    }
}


