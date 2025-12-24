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
        public async Task StopAsync()
        {
            if (_host != null)
            {
                await _host.StopAsync();
            }
            _gameWrapper.DisposeGame();
        }

        /// <summary>
        /// Stops the MCP server (synchronous version for backward compatibility)
        /// NOTE: This method is deprecated. Use StopAsync instead for proper async handling.
        /// </summary>
        [Obsolete("Use StopAsync instead. This method blocks the calling thread.")]
        public void Stop()
        {
            // For backward compatibility only - callers should migrate to async version
            // Using ConfigureAwait(false) to avoid deadlocks, but this still blocks
            StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}

