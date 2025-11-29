using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for canceling current menu operation.
    /// </summary>
    public class CancelCommand : MenuCommand
    {
        private readonly string sourceMenu;

        public CancelCommand(string sourceMenu)
        {
            this.sourceMenu = sourceMenu;
        }

        protected override string CommandName => "Cancel";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Canceling from {sourceMenu}");
            await Task.CompletedTask;
        }
    }
}

