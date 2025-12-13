using System;
using System.Threading.Tasks;
using RPGGame.MCP;

namespace RPGGame.MCP
{
    /// <summary>
    /// Entry point for the MCP server
    /// Run this as a separate executable to start the MCP server
    /// Usage: dotnet run -- MCP
    /// </summary>
    public class MCPServerProgram
    {
        // This Main method is an alternative entry point when run with "MCP" argument
        // The primary Program.Main in Game/Program.cs is the default entry point
        public static async Task RunMCPServer(string[] args)
        {
            // Check if MCP mode is requested
            if (args.Length == 0 || args[0] != "MCP")
            {
                // Not MCP mode, run normal game
                return;
            }

            var server = new DungeonFighterMCPServer();

            // Handle graceful shutdown
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                server.Stop();
            };

            try
            {
                await server.RunAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"MCP Server Error: {ex.Message}");
                Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
                System.Environment.Exit(1);
            }
        }
    }
}

