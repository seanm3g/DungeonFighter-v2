using System;
using System.Threading.Tasks;

namespace RPGGame.Game
{
    /// <summary>
    /// Plays via GamePlaySession until Death. Run: dotnet run -- PLAYTODEATH
    /// </summary>
    public static class PlayUntilDeath
    {
        public static async Task RunAsync()
        {
            var session = new GamePlaySession();
            await session.Initialize();
            await session.StartNewGame();

            int run = 0;
            int maxActions = 200;

            for (int i = 0; i < maxActions; i++)
            {
                var state = session.CurrentState;
                var current = state?.CurrentState ?? "";
                if (current == "Death" || session.IsGameOver())
                    break;

                string action = PickAction(current);
                if (current == "DungeonSelection" && action is "1" or "2" or "3")
                    run++;

                await session.ExecuteAction(action);

                if (session.CurrentState?.CurrentState == "Death")
                    break;
            }

            var final = session.CurrentState;
            Console.WriteLine("\n=== PLAY UNTIL DEATH ===");
            Console.WriteLine($"Dungeon runs attempted: {run}");
            Console.WriteLine($"Final state: {final?.CurrentState}");
            if (final?.Player != null)
            {
                var p = final.Player;
                Console.WriteLine($"Character: {p.Name} Lvl {p.Level} HP {p.CurrentHealth}/{p.MaxHealth}");
                Console.WriteLine($"Weapon: {p.Weapon?.Name ?? "none"}");
                Console.WriteLine($"Inventory items: {p.Inventory.Count}");
            }

            session.Dispose();
            if (final?.CurrentState != "Death")
                throw new InvalidOperationException($"Did not reach Death (ended in {final?.CurrentState}).");
        }

        private static string PickAction(string state) => state switch
        {
            "MainMenu" => "1",
            "TrainingGroundOffer" => "2",
            "PreWeaponPathIntro" => "1",
            "WeaponSelection" => "1",
            "CharacterCreation" => "1",
            "GameLoop" => "1",
            "DungeonCompletion" => "1",
            "DungeonSelection" => "2", // prefer slightly harder option when available
            "Death" => "1",
            _ => "1"
        };
    }
}
