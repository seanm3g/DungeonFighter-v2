using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for confirming character creation.
    /// </summary>
    public class ConfirmCharacterCommand : MenuCommand
    {
        protected override string CommandName => "ConfirmCharacter";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Confirming character creation");
            
            if (context?.StateManager?.CurrentPlayer != null)
            {
                // Character is already created, just need to finalize
                // Additional initialization would happen here if needed
                LogStep("Character confirmed");
            }
            
            await Task.CompletedTask;
        }
    }
}

