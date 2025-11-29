using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Character Creation Menu Handler using direct method calls.
    /// Handles stat modification and character confirmation.
    /// </summary>
    public class CharacterCreationMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.CharacterCreation;
        protected override string HandlerName => "CharacterCreation";

        /// <summary>
        /// Handle input directly and return next game state.
        /// </summary>
        protected override async Task<GameState?> HandleInputDirect(string input)
        {
            string cleaned = input.Trim().ToLower();

            if (cleaned.Length != 1)
                return null;

            return cleaned switch
            {
                "1" => await ModifyStat("Strength", true),
                "2" => await ModifyStat("Strength", false),
                "3" => await ModifyStat("Agility", true),
                "4" => await ModifyStat("Agility", false),
                "5" => await ModifyStat("Technique", true),
                "6" => await ModifyStat("Technique", false),
                "7" => await ModifyStat("Intelligence", true),
                "8" => await ModifyStat("Intelligence", false),
                "9" => await ModifyStat("Health", true),
                "r" => await RandomizeCharacter(),
                "c" => await ConfirmCharacter(),
                "0" => GameState.MainMenu,
                _ => null
            };
        }

        private Task<GameState?> ModifyStat(string statName, bool increase)
        {
            if (StateManager?.CurrentPlayer == null)
            {
                LogError("StateManager or CurrentPlayer is null");
                return Task.FromResult<GameState?>(null);
            }

            var player = StateManager.CurrentPlayer;
            var action = increase ? "Increasing" : "Decreasing";
            LogStep($"{action} {statName}");

            switch (statName.ToLower())
            {
                case "strength":
                    if (increase) player.Strength++;
                    else if (player.Strength > 1) player.Strength--;
                    break;
                case "agility":
                    if (increase) player.Agility++;
                    else if (player.Agility > 1) player.Agility--;
                    break;
                case "technique":
                    if (increase) player.Technique++;
                    else if (player.Technique > 1) player.Technique--;
                    break;
                case "intelligence":
                    if (increase) player.Intelligence++;
                    else if (player.Intelligence > 1) player.Intelligence--;
                    break;
                case "health":
                    if (increase)
                    {
                        player.MaxHealth += 10;
                        player.CurrentHealth += 10;
                    }
                    else if (player.MaxHealth > 10)
                    {
                        player.MaxHealth -= 10;
                        if (player.CurrentHealth > player.MaxHealth)
                            player.CurrentHealth = player.MaxHealth;
                    }
                    break;
            }

            return Task.FromResult<GameState?>(null); // Stay in character creation
        }

        private Task<GameState?> RandomizeCharacter()
        {
            if (StateManager?.CurrentPlayer == null)
            {
                LogError("StateManager or CurrentPlayer is null");
                return Task.FromResult<GameState?>(null);
            }

            LogStep("Randomizing character");
            var player = StateManager.CurrentPlayer;

            // Randomize stats (keeping them within reasonable bounds)
            player.Strength = RandomUtility.Next(5, 16);
            player.Agility = RandomUtility.Next(5, 16);
            player.Technique = RandomUtility.Next(5, 16);
            player.Intelligence = RandomUtility.Next(5, 16);

            // Randomize health (base 100, +/- 50)
            int baseHealth = 100;
            int healthVariation = RandomUtility.Next(-50, 51);
            player.MaxHealth = baseHealth + healthVariation;
            player.CurrentHealth = player.MaxHealth;

            LogStep("Character randomized");
            return Task.FromResult<GameState?>(null); // Stay in character creation
        }

        private Task<GameState?> ConfirmCharacter()
        {
            LogStep("Confirming character creation");
            // Character is already created, just transition to game loop
            return Task.FromResult<GameState?>(GameState.GameLoop);
        }
    }
}

