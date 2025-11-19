using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for opening the settings menu.
    /// Transitions to settings menu state.
    /// </summary>
    public class SettingsCommand : MenuCommand
    {
        protected override string CommandName => "Settings";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep("Transitioning to settings menu");
            
            // No special logic needed - state transition handled by handler
            
            LogStep("Settings menu opened");
            await Task.CompletedTask;
        }
    }
}

