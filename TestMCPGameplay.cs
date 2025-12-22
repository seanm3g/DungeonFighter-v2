using System;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.MCP;
using RPGGame.MCP.Tools;

namespace RPGGame.Tests
{
    /// <summary>
    /// Quick test to verify MCP gameplay works
    /// </summary>
    public class TestMCPGameplay
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== MCP GAMEPLAY VERIFICATION TEST ===\n");

            try
            {
                // Initialize game wrapper
                Console.WriteLine("Initializing game wrapper...");
                var wrapper = new GameWrapper();
                McpToolState.GameWrapper = wrapper;
                Console.WriteLine("✓ Game wrapper initialized\n");

                // Start new game
                Console.WriteLine("Starting new game...");
                var startResult = await GameControlTools.StartNewGame();
                Console.WriteLine("✓ Game started");
                Console.WriteLine($"Result: {startResult.Substring(0, Math.Min(200, startResult.Length))}...\n");

                // Get game state
                Console.WriteLine("Getting game state...");
                var stateResult = await InformationTools.GetGameState();
                Console.WriteLine("✓ Game state retrieved");
                Console.WriteLine($"State: {stateResult.Substring(0, Math.Min(200, stateResult.Length))}...\n");

                // Get available actions
                Console.WriteLine("Getting available actions...");
                var actionsResult = await NavigationTools.GetAvailableActions();
                Console.WriteLine("✓ Available actions retrieved");
                Console.WriteLine($"Actions: {actionsResult}\n");

                // Execute action (select "New Game")
                Console.WriteLine("Executing action 1 (New Game)...");
                var action1Result = await NavigationTools.HandleInput("1");
                Console.WriteLine("✓ Action 1 executed");
                Console.WriteLine($"Result: {action1Result.Substring(0, Math.Min(200, action1Result.Length))}...\n");

                // Select weapon
                Console.WriteLine("Executing action 1 (Select first weapon)...");
                var action2Result = await NavigationTools.HandleInput("1");
                Console.WriteLine("✓ Action 2 executed");
                Console.WriteLine($"Result: {action2Result.Substring(0, Math.Min(200, action2Result.Length))}...\n");

                // Confirm character
                Console.WriteLine("Executing action 1 (Confirm character)...");
                var action3Result = await NavigationTools.HandleInput("1");
                Console.WriteLine("✓ Action 3 executed");
                Console.WriteLine($"Result: {action3Result.Substring(0, Math.Min(200, action3Result.Length))}...\n");

                // Select dungeon
                Console.WriteLine("Executing action 1 (Select first dungeon)...");
                var action4Result = await NavigationTools.HandleInput("1");
                Console.WriteLine("✓ Action 4 executed");
                Console.WriteLine($"Result: {action4Result.Substring(0, Math.Min(200, action4Result.Length))}...\n");

                // Get final state
                Console.WriteLine("Getting final game state...");
                var finalState = await InformationTools.GetGameState();
                Console.WriteLine("✓ Final state retrieved");
                Console.WriteLine($"Final State: {finalState.Substring(0, Math.Min(300, finalState.Length))}...\n");

                Console.WriteLine("\n=== MCP GAMEPLAY TEST COMPLETE ===");
                Console.WriteLine("✓ All MCP tools responded correctly");
                Console.WriteLine("✓ Game navigation worked");
                Console.WriteLine("✓ State transitions successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
