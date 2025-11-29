using System.Threading.Tasks;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for decreasing a character stat.
    /// </summary>
    public class DecreaseStatCommand : MenuCommand
    {
        private readonly string statName;

        public DecreaseStatCommand(string statName)
        {
            this.statName = statName;
        }

        protected override string CommandName => "DecreaseStat";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep($"Decreasing {statName}");
            
            if (context?.StateManager?.CurrentPlayer != null)
            {
                var player = context.StateManager.CurrentPlayer;
                
                switch (statName.ToLower())
                {
                    case "strength":
                        if (player.Strength > 1) player.Strength--;
                        break;
                    case "agility":
                        if (player.Agility > 1) player.Agility--;
                        break;
                    case "technique":
                        if (player.Technique > 1) player.Technique--;
                        break;
                    case "intelligence":
                        if (player.Intelligence > 1) player.Intelligence--;
                        break;
                    case "health":
                        if (player.MaxHealth > 10)
                        {
                            player.MaxHealth -= 10;
                            if (player.CurrentHealth > player.MaxHealth)
                                player.CurrentHealth = player.MaxHealth;
                        }
                        break;
                }
                
                LogStep($"{statName} decreased");
            }
            
            await Task.CompletedTask;
        }
    }
}

