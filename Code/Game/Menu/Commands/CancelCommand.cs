using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Generic command for canceling/going back in menus.
    /// Transitions to a previous or parent menu state.
    /// </summary>
    public class CancelCommand : MenuCommand
    {
        private readonly string menuName;

        public CancelCommand(string menu = "Menu")
        {
            menuName = menu;
        }

        protected override string CommandName => $"Cancel({menuName})";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep($"Canceling {menuName}");
            
            // TODO: When integrating with Game.cs:
            // 1. Cancel current operation
            // 2. Return to previous state
            
            LogStep($"{menuName} canceled");
            await Task.CompletedTask;
        }
    }
}

