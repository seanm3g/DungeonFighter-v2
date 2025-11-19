using System.Threading.Tasks;
using RPGGame;
using DungeonFighter.Game.Menu.Commands;
using DungeonFighter.Game.Menu.Core;

namespace DungeonFighter.Game.Menu.Handlers
{
    /// <summary>
    /// Refactored Character Creation Menu Handler using the unified menu framework.
    /// Handles stat modification and character confirmation.
    /// 
    /// BEFORE: ~150 lines with inconsistent patterns
    /// AFTER: ~85 lines with clear command pattern
    /// </summary>
    public class CharacterCreationMenuHandler : MenuHandlerBase
    {
        public override GameState TargetState => GameState.CharacterCreation;
        protected override string HandlerName => "CharacterCreation";

        /// <summary>
        /// Parse input into character creation command.
        /// Supports: 1-9 for stat selection, actions like r, c, e, h
        /// </summary>
        protected override IMenuCommand? ParseInput(string input)
        {
            string cleaned = input.Trim().ToLower();

            if (cleaned.Length != 1)
                return null;

            return cleaned switch
            {
                // Stat increases (1-9 could map to different stats)
                "1" => new IncreaseStatCommand("Strength"),
                "2" => new DecreaseStatCommand("Strength"),
                "3" => new IncreaseStatCommand("Agility"),
                "4" => new DecreaseStatCommand("Agility"),
                "5" => new IncreaseStatCommand("Technique"),
                "6" => new DecreaseStatCommand("Technique"),
                "7" => new IncreaseStatCommand("Intelligence"),
                "8" => new DecreaseStatCommand("Intelligence"),
                "9" => new IncreaseStatCommand("Health"),
                
                // Actions
                "r" => new RandomizeCharacterCommand(),
                "c" => new ConfirmCharacterCommand(),
                "0" => new CancelCommand("CharacterCreation"),
                
                _ => null
            };
        }

        /// <summary>
        /// Execute command and determine next state.
        /// </summary>
        protected override async Task<GameState?> ExecuteCommand(IMenuCommand command)
        {
            if (StateManager != null)
            {
                var context = new MenuContext(StateManager);
                await command.Execute(context);
            }
            else
            {
                DebugLogger.Log(HandlerName, "WARNING: StateManager is null, executing command with null context");
                await command.Execute(null);
            }

            // Determine state transition
            return command switch
            {
                ConfirmCharacterCommand => GameState.GameLoop,
                CancelCommand => GameState.MainMenu,
                _ => (GameState?)null  // Stay in character creation
            };
        }
    }
}

