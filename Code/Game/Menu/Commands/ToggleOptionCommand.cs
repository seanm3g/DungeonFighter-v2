using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for toggling a settings option.
    /// </summary>
    public class ToggleOptionCommand : MenuCommand
    {
        private readonly string optionName;

        public ToggleOptionCommand(string optionName)
        {
            this.optionName = optionName;
        }

        protected override string CommandName => "ToggleOption";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Toggling {optionName} option");
            
            // Settings toggle logic would be handled by SettingsManager
            // This command just marks the action
            
            await Task.CompletedTask;
        }
    }
}

