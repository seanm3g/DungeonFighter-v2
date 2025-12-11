using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.MCP.Models;

namespace RPGGame.MCP
{
    /// <summary>
    /// Wraps the game instance and provides a clean API for MCP server
    /// Manages headless game instance with output capture
    /// </summary>
    public class GameWrapper
    {
        private Game? _game;
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
        public Game? Game => _game;

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

            _game = new Game(); // Headless mode - no UI manager
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

            _game = new Game(character);
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
        /// Disposes the game instance
        /// </summary>
        public void DisposeGame()
        {
            _game = null;
            _outputCapture.Clear();
        }
    }
}

