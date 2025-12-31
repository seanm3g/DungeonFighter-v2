using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Commands
{
    /// <summary>
    /// Command for randomizing character stats.
    /// </summary>
    public class RandomizeCharacterCommand : MenuCommand
    {
        protected override string CommandName => "RandomizeCharacter";

        protected override async Task ExecuteCommand(IMenuContext? context)
        {
            LogStep("Randomizing character");
            
            if (context?.StateManager?.CurrentPlayer != null)
            {
                var player = context.StateManager.CurrentPlayer;
                
                // Randomize stats (keeping them within reasonable bounds)
                player.Strength = RandomUtility.Next(5, 16);
                player.Agility = RandomUtility.Next(5, 16);
                player.Technique = RandomUtility.Next(5, 16);
                player.Intelligence = RandomUtility.Next(5, 16);
                
                // Randomize health (base 100, +/- 50)
                int baseHealth = 100;
                int healthVariation = RandomUtility.Next(-50, 51);
                player.MaxHealth = baseHealth + healthVariation;
                // Heal to full effective max health (including equipment bonuses)
                player.CurrentHealth = player.GetEffectiveMaxHealth();
                
                LogStep("Character randomized");
            }
            
            await Task.CompletedTask;
        }
    }
}

