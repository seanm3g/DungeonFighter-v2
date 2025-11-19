using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Generic command for toggling settings/options.
    /// Can be used in Settings menu and similar.
    /// </summary>
    public class ToggleOptionCommand : MenuCommand
    {
        private readonly string optionName;

        public ToggleOptionCommand(string option)
        {
            optionName = option;
        }

        protected override string CommandName => $"ToggleOption({optionName})";

        protected override async Task ExecuteCommand(IMenuContext context)
        {
            LogStep($"Toggling option: {optionName}");
            
            // TODO: When integrating with Game.cs:
            // 1. Get current option value
            // 2. Toggle the value
            // 3. Save setting
            
            LogStep($"{optionName} toggled");
            await Task.CompletedTask;
        }
    }
}

