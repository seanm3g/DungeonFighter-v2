using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for opening settings menu.
    /// </summary>
    public class SettingsCommand : MenuCommand
    {
        protected override string CommandName => "Settings";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Opening settings menu");
            await Task.CompletedTask;
        }
    }
}

