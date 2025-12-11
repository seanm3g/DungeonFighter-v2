using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ModelContextProtocol.Server;
using RPGGame.MCP;

namespace RPGGame.MCP
{
    /// <summary>
    /// MCP Server for DungeonFighter game
    /// Uses Microsoft.Extensions.Hosting to set up the MCP server
    /// </summary>
    public class DungeonFighterMCPServer
    {
        private readonly GameWrapper _gameWrapper;
        private IHost? _host;

        public DungeonFighterMCPServer()
        {
            _gameWrapper = new GameWrapper();
            McpTools.SetGameWrapper(_gameWrapper);
        }

        /// <summary>
        /// Starts the MCP server with stdio transport
        /// </summary>
        public async Task RunAsync()
        {
            var builder = Host.CreateApplicationBuilder();

            // Configure logging to stderr (required for MCP stdio transport)
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // All logs must go to stderr to avoid interfering with MCP protocol on stdout
                consoleLogOptions.LogToStandardErrorThreshold = Microsoft.Extensions.Logging.LogLevel.Trace;
            });

            // Configure MCP server with stdio transport and tools from assembly
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            _host = builder.Build();
            await _host.RunAsync();
        }

        /// <summary>
        /// Stops the MCP server
        /// </summary>
        public void Stop()
        {
            _host?.StopAsync().Wait();
            _gameWrapper.DisposeGame();
        }
    }
}

