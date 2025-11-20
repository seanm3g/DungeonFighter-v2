using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for confirming character creation.
    /// Finalizes character and transitions to game loop.
    /// </summary>
    public class ConfirmCharacterCommand : MenuCommand
    {
        protected override string CommandName => "ConfirmCharacter";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Confirming character creation");
            
            // TODO: When integrating with Game.cs:
            // 1. Validate character is complete
            // 2. Save character to context
            // 3. Initialize game systems
            
            LogStep("Character confirmed and ready to play");
            await Task.CompletedTask;
        }
    }
}


