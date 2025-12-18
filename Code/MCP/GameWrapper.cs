using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.MCP.Models;
using RPGGame.UI;
using RPGGame.Combat;

namespace RPGGame.MCP
{
    /// <summary>
    /// Wraps the game instance and provides a clean API for MCP server
    /// Manages headless game instance with output capture
    /// </summary>
    public class GameWrapper
    {
        private GameCoordinator? _game;
        private OutputCapture _outputCapture;

        public GameWrapper()
        {
            _outputCapture = new OutputCapture();
        }

        /// <summary>
        /// Gets whether a game is currently initialized
        /// </summary>
        public bool IsGameInitialized => _game != null;

        /// <summary>
        /// Gets the current game instance
        /// </summary>
        public GameCoordinator? Game => _game;

        /// <summary>
        /// Gets the output capture instance
        /// </summary>
        public OutputCapture OutputCapture => _outputCapture;

        /// <summary>
        /// Initializes a new headless game instance
        /// </summary>
        public void InitializeGame()
        {
            if (_game != null)
            {
                throw new InvalidOperationException("Game is already initialized. Call DisposeGame() first.");
            }

            // Disable all delays for MCP mode
            DisableAllDelays();

            _game = new GameCoordinator(); // Headless mode - no UI manager
            _game.SetUIManager(_outputCapture);
        }

        /// <summary>
        /// Initializes game with an existing character
        /// </summary>
        public void InitializeGame(Character character)
        {
            if (_game != null)
            {
                throw new InvalidOperationException("Game is already initialized. Call DisposeGame() first.");
            }

            // Disable all delays for MCP mode
            DisableAllDelays();

            _game = new GameCoordinator(character);
            _game.SetUIManager(_outputCapture);
        }

        /// <summary>
        /// Handles input to the game
        /// </summary>
        public async Task<GameStateSnapshot> HandleInput(string input)
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Game is not initialized. Call InitializeGame() first.");
            }

            await _game.HandleInput(input);
            return GameStateSerializer.SerializeGameState(_game, _outputCapture, includeRecentOutput: false);
        }

        /// <summary>
        /// Gets the current game state snapshot
        /// </summary>
        public GameStateSnapshot GetGameState()
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Game is not initialized. Call InitializeGame() first.");
            }

            return GameStateSerializer.SerializeGameState(_game, _outputCapture, includeRecentOutput: false);
        }

        /// <summary>
        /// Gets available actions for the current game state
        /// </summary>
        public List<string> GetAvailableActions()
        {
            if (_game == null)
            {
                return new List<string>();
            }

            return GameStateSerializer.SerializeGameState(_game, _outputCapture, includeRecentOutput: false).AvailableActions;
        }

        /// <summary>
        /// Gets recent game output
        /// </summary>
        public List<string> GetRecentOutput(int count = 50)
        {
            return _outputCapture.GetRecentOutput(count);
        }

        /// <summary>
        /// Clears the output buffer
        /// </summary>
        public void ClearOutput()
        {
            _outputCapture.Clear();
        }

        /// <summary>
        /// Shows the main menu
        /// </summary>
        public void ShowMainMenu()
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Game is not initialized. Call InitializeGame() first.");
            }

            _game.ShowMainMenu();
        }

        /// <summary>
        /// Saves the current game
        /// </summary>
        public void SaveGame()
        {
            if (_game == null)
            {
                throw new InvalidOperationException("Game is not initialized. Call InitializeGame() first.");
            }

            _game.SaveGame();
        }

        /// <summary>
        /// Disables all delays in the game for MCP mode
        /// </summary>
        private void DisableAllDelays()
        {
            // Set MCP mode flag
            MCPMode.IsActive = true;
            
            // Disable UI delays
            UIManager.EnableDelays = false;
            
            // Disable combat UI output (which also disables combat delays)
            CombatManager.DisableCombatUIOutput = true;
            
            // Note: Combat delays are automatically disabled by DisableCombatUIOutput
            // and UIManager.EnableDelays = false above. The old UpdateConfig method
            // is obsolete and configuration should be managed via TextDelayConfig.json
            
            // Disable text display delays in game settings
            var settings = GameSettings.Instance;
            settings.EnableTextDisplayDelays = false;
            settings.FastCombat = true;
        }

        /// <summary>
        /// Disposes the game instance
        /// </summary>
        public void DisposeGame()
        {
            // Reset MCP mode flag
            MCPMode.IsActive = false;
            
            _game = null;
            _outputCapture.Clear();
        }
    }
}

