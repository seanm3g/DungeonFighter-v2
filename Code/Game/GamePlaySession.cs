using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RPGGame.MCP;
using RPGGame.MCP.Models;
using RPGGame.MCP.Tools;

namespace RPGGame.Game
{
    /// <summary>
    /// Manages a single game play session
    /// Handles initialization, tool calls, state management, and cleanup
    /// </summary>
    public class GamePlaySession
    {
        private GameWrapper? _gameWrapper;
        private GameStateSnapshot? _currentState;
        private List<string> _actionHistory = new();
        private int _turnCount = 0;
        private bool _isInitialized = false;

        /// <summary>
        /// Gets the current game state snapshot
        /// </summary>
        public GameStateSnapshot? CurrentState => _currentState;

        /// <summary>
        /// Gets the current turn number
        /// </summary>
        public int TurnCount => _turnCount;

        /// <summary>
        /// Gets the history of actions taken
        /// </summary>
        public IReadOnlyList<string> ActionHistory => _actionHistory.AsReadOnly();

        /// <summary>
        /// Gets whether the session is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initialize the session with a game wrapper and set up MCP tool state
        /// </summary>
        public async Task Initialize()
        {
            if (_isInitialized)
                throw new InvalidOperationException("Session is already initialized");

            _gameWrapper = new GameWrapper();
            McpToolState.GameWrapper = _gameWrapper;
            _actionHistory.Clear();
            _turnCount = 0;
            _isInitialized = true;

            await Task.CompletedTask;
        }

        /// <summary>
        /// Start a new game (shows main menu)
        /// </summary>
        public async Task StartNewGame()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await GameControlTools.StartNewGame();
                _currentState = ParseGameStateResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to start new game", ex);
            }
        }

        /// <summary>
        /// Execute a game action and update the current state
        /// </summary>
        public async Task ExecuteAction(string action)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            _actionHistory.Add(action);
            _turnCount++;

            try
            {
                var response = await NavigationTools.HandleInput(action);
                _currentState = ParseGameStateResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to execute action '{action}'", ex);
            }
        }

        /// <summary>
        /// Get the list of available actions for the current game state
        /// </summary>
        public async Task<List<string>> GetAvailableActions()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await NavigationTools.GetAvailableActions();
                return ParseAvailableActionsResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get available actions", ex);
            }
        }

        /// <summary>
        /// Get the current game state
        /// </summary>
        public async Task<GameStateSnapshot?> GetGameState()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await InformationTools.GetGameState();
                return ParseGameStateResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get game state", ex);
            }
        }

        /// <summary>
        /// Get player statistics
        /// </summary>
        public async Task<dynamic?> GetPlayerStats()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await InformationTools.GetPlayerStats();
                return JsonSerializer.Deserialize<dynamic>(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get player stats", ex);
            }
        }

        /// <summary>
        /// Get current dungeon information
        /// </summary>
        public async Task<dynamic?> GetCurrentDungeon()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await InformationTools.GetCurrentDungeon();
                return JsonSerializer.Deserialize<dynamic>(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get current dungeon", ex);
            }
        }

        /// <summary>
        /// Get recent game output messages
        /// </summary>
        public async Task<List<string>> GetRecentOutput(int count = 10)
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await InformationTools.GetRecentOutput(count);
                return ParseRecentOutputResponse(response);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to get recent output", ex);
            }
        }

        /// <summary>
        /// Save the current game
        /// </summary>
        public async Task SaveGame()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Session must be initialized first");

            try
            {
                var response = await GameControlTools.SaveGame();
                // Parse response to verify success
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save game", ex);
            }
        }

        /// <summary>
        /// Check if the game is over based on current state
        /// </summary>
        public bool IsGameOver()
        {
            if (_currentState == null)
                return false;

            // Check for game over states
            return _currentState.CurrentState switch
            {
                "GameOver" => true,
                "PlayerDefeated" => true,
                "VictoryScreen" => true,
                _ => false
            };
        }

        /// <summary>
        /// Check if the player won
        /// </summary>
        public bool IsPlayerVictory()
        {
            if (_currentState == null)
                return false;

            return _currentState.CurrentState == "VictoryScreen";
        }

        /// <summary>
        /// Dispose the session and clean up resources
        /// </summary>
        public void Dispose()
        {
            if (_gameWrapper != null)
            {
                _gameWrapper.DisposeGame();
                _gameWrapper = null;
            }

            McpToolState.GameWrapper = null;
            _isInitialized = false;
        }

        /// <summary>
        /// Parse game state response from tool
        /// </summary>
        private GameStateSnapshot? ParseGameStateResponse(string response)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                return JsonSerializer.Deserialize<GameStateSnapshot>(response, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse game state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parse available actions response from tool
        /// </summary>
        private List<string> ParseAvailableActionsResponse(string response)
        {
            try
            {
                var result = JsonSerializer.Deserialize<dynamic>(response);
                if (result is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
                {
                    if (elem.TryGetProperty("actions", out var actions))
                    {
                        var list = new List<string>();
                        foreach (var action in actions.EnumerateArray())
                        {
                            list.Add(action.GetString() ?? "");
                        }
                        return list;
                    }
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse available actions: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Parse recent output response from tool
        /// </summary>
        private List<string> ParseRecentOutputResponse(string response)
        {
            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(response);
                if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("messages", out var messages))
                {
                    var list = new List<string>();
                    foreach (var message in messages.EnumerateArray())
                    {
                        list.Add(message.GetString() ?? "");
                    }
                    return list;
                }
                return new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to parse recent output: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
