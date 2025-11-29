using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for increasing a character stat.
    /// </summary>
    public class IncreaseStatCommand : MenuCommand
    {
        private readonly string statName;

        public IncreaseStatCommand(string statName)
        {
            this.statName = statName;
        }

        protected override string CommandName => "IncreaseStat";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Increasing {statName}");
            
            if (context?.StateManager?.CurrentPlayer != null)
            {
                var player = context.StateManager.CurrentPlayer;
                
                switch (statName.ToLower())
                {
                    case "strength":
                        player.Strength++;
                        break;
                    case "agility":
                        player.Agility++;
                        break;
                    case "technique":
                        player.Technique++;
                        break;
                    case "intelligence":
                        player.Intelligence++;
                        break;
                    case "health":
                        player.MaxHealth += 10;
                        player.CurrentHealth += 10;
                        break;
                }
                
                LogStep($"{statName} increased");
            }
            
            await Task.CompletedTask;
        }
    }
}

