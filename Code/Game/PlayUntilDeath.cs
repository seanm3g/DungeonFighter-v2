using System;
using System.Threading.Tasks;
using RPGGame.Tuning;

namespace RPGGame.Game
{
    /// <summary>
    /// Plays via GamePlaySession until Death. Run: dotnet run -- PLAYTODEATH
    /// </summary>
    public static class PlayUntilDeath
    {
        public const int DefaultMaxActions = 500;

        public static async Task RunAsync(int weaponMenuSlot = 1, int maxActions = DefaultMaxActions)
        {
            var classes = ClassPlaythroughClassResolver.ResolveAllClasses();
            if (weaponMenuSlot < 1 || weaponMenuSlot > classes.Count)
                throw new ArgumentOutOfRangeException(nameof(weaponMenuSlot), "Weapon menu slot must be 1-4.");

            var (weaponType, slot, displayName) = classes[weaponMenuSlot - 1];
            var result = await ClassPlaythroughRunner.RunAsync(weaponType, slot, displayName, maxActions);

            Console.WriteLine("\n=== PLAY UNTIL DEATH ===");
            Console.WriteLine($"Class: {result.ClassDisplayName} ({result.WeaponType})");
            Console.WriteLine($"Dungeon runs attempted: {result.DungeonsAttempted}");
            Console.WriteLine($"Dungeon runs completed: {result.DungeonsCompleted}");
            Console.WriteLine($"Turns: {result.TurnCount}");
            Console.WriteLine($"Final state: {result.FinalState}");
            Console.WriteLine($"Stop reason: {result.StopReason}");
            Console.WriteLine($"Character Lvl {result.FinalLevel} HP {result.FinalHealth}/{result.FinalMaxHealth}");
            Console.WriteLine($"Inventory items: {result.InventoryCount}");

            if (!result.ReachedDeath)
                throw new InvalidOperationException($"Did not reach Death (ended in {result.FinalState}, reason {result.StopReason}).");
        }
    }
}
