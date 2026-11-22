using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RPGGame.MCP.Models;
using RPGGame.MCP.Tools;
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
        private readonly McpCombatLog _combatLog;
        private DungeonRunBaseline? _pendingRunBaseline;

        private sealed class DungeonRunBaseline
        {
            public int Level { get; init; }
            public int Health { get; init; }
            public int Xp { get; init; }
            public int InventoryCount { get; init; }
            public string? SelectedDungeonName { get; init; }
        }

        public GameWrapper()
        {
            _outputCapture = new OutputCapture();
            _combatLog = new McpCombatLog(_outputCapture, () => _game?.CurrentPlayer);
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

            // Quit on the main menu ends the process, which would kill the MCP server mid-session.
            if (_game.CurrentState == GameState.MainMenu && IsQuitInput(input))
            {
                _outputCapture.WriteSystemLine("Quit is disabled over MCP (it would stop the server). Use reset_game for a fresh start.");
                return GameStateSerializer.SerializeGameState(_game, _outputCapture, includeRecentOutput: false);
            }

            var inputContext = AgentChoiceBuilder.ResolveInputContext(_game);
            if (IsLikelyDungeonEntry(_game, input, inputContext))
            {
                _pendingRunBaseline = CaptureRunBaseline(_game, input, inputContext);
                _combatLog.ResetRoomCount();
            }

            _combatLog.EnsureSubscribed();
            try
            {
                await _game.HandleInput(input);
            }
            finally
            {
                _combatLog.Flush();
            }

            var snapshot = GameStateSerializer.SerializeGameState(_game, _outputCapture, includeRecentOutput: false);

            if (_pendingRunBaseline != null &&
                (_game.CurrentState == GameState.DungeonCompletion || _game.CurrentState == GameState.Death))
            {
                McpToolState.LastRunSummary = BuildRunSummary(_game, _pendingRunBaseline, _combatLog.RoomsCleared);
                _pendingRunBaseline = null;
            }
            // A new hero (or the main menu) shouldn't show the previous hero's run.
            if (_game.CurrentState is GameState.MainMenu or GameState.WeaponSelection or GameState.TrainingGroundOffer)
                McpToolState.LastRunSummary = null;

            return snapshot;
        }

        /// <summary>Most NewEvents lines returned by one handle_input (a full dungeon run logs far more).</summary>
        public const int MaxNewEventsPerInput = 150;

        /// <summary>
        /// Sends input and returns the agent view after it: screen, body, labeled choices, plus the lines this input
        /// produced (combat log, loot, messages). This is what the handle_input tool returns.
        /// </summary>
        public async Task<AgentContextSnapshot> HandleInputForAgent(string input, int recentEventCount = 0)
        {
            if (_game == null)
                throw new InvalidOperationException("Game is not initialized. Call InitializeGame() first.");

            long mark = _outputCapture.TotalMessages;
            await HandleInput(input);
            var events = _outputCapture.GetMessagesSince(mark);

            var context = AgentContextBuilder.Build(_game, _outputCapture, Math.Max(0, recentEventCount));
            if (recentEventCount <= 0)
                context.RecentEvents = new List<string>();
            int omitted = Math.Max(0, events.Count - MaxNewEventsPerInput);
            context.NewEvents = omitted > 0 ? events.GetRange(omitted, MaxNewEventsPerInput) : events;
            context.NewEventsOmitted = omitted > 0 ? omitted : null;
            return context;
        }

        private static bool IsQuitInput(string input)
        {
            string t = (input ?? "").Trim();
            return t == "0" || string.Equals(t, MainMenuHandler.QuitKey, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyDungeonEntry(GameCoordinator game, string input, McpGameplayInputContext inputContext)
        {
            if (game.CurrentState != GameState.DungeonSelection)
                return false;

            if (input == "0")
                return false;

            if (inputContext.PendingInputMode == AgentChoiceBuilder.ModeCustomDungeonLevel)
                return input.Equals("enter", StringComparison.OrdinalIgnoreCase);

            if (int.TryParse(input, out int index) && index > 0)
                return game.AvailableDungeons != null && index <= game.AvailableDungeons.Count;

            return false;
        }

        private static DungeonRunBaseline CaptureRunBaseline(
            GameCoordinator game,
            string input,
            McpGameplayInputContext inputContext)
        {
            var player = game.CurrentPlayer;
            string? dungeonName = null;

            if (inputContext.PendingInputMode == AgentChoiceBuilder.ModeCustomDungeonLevel)
            {
                dungeonName = $"Custom level {inputContext.CustomLevelBuffer}";
            }
            else if (int.TryParse(input, out int index) && game.AvailableDungeons != null && index > 0 && index <= game.AvailableDungeons.Count)
            {
                dungeonName = game.AvailableDungeons[index - 1].Name;
            }

            return new DungeonRunBaseline
            {
                Level = player?.Level ?? 0,
                Health = player?.CurrentHealth ?? 0,
                Xp = player?.XP ?? 0,
                InventoryCount = player?.Inventory.Count ?? 0,
                SelectedDungeonName = dungeonName
            };
        }

        private static DungeonRunSummary BuildRunSummary(GameCoordinator game, DungeonRunBaseline baseline, int roomsClearedSeen)
        {
            var player = game.CurrentPlayer;
            bool died = game.CurrentState == GameState.Death;
            int totalRooms = game.CurrentDungeon?.Rooms?.Count ?? 0;
            // The dungeon is often gone by the time the run ends, so count the RoomCleared events this run produced.
            int roomsCleared = roomsClearedSeen;
            if (!died && totalRooms == 0)
                totalRooms = roomsCleared;
            else if (!died)
                roomsCleared = Math.Max(roomsCleared, totalRooms);

            return new DungeonRunSummary
            {
                Outcome = died ? "Death" : "Completed",
                DungeonName = game.CurrentDungeon?.Name ?? baseline.SelectedDungeonName,
                RoomsCleared = roomsCleared,
                TotalRooms = totalRooms > 0 ? totalRooms : null,
                LevelBefore = baseline.Level,
                LevelAfter = player?.Level ?? baseline.Level,
                HealthBefore = baseline.Health,
                HealthAfter = player?.CurrentHealth ?? 0,
                XpBefore = baseline.Xp,
                XpAfter = player?.XP ?? baseline.Xp,
                InventoryCountBefore = baseline.InventoryCount,
                InventoryCountAfter = player?.Inventory.Count ?? baseline.InventoryCount
            };
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
            SimulationPacing.EnableFastMode();
            RPGGame.Combat.Narrative.FernisFightSession.McpPlay = true;
        }

        /// <summary>
        /// Disposes the game instance
        /// </summary>
        public void DisposeGame()
        {
            // Reset MCP mode flag
            MCPMode.IsActive = false;
            
            _game = null;
            _pendingRunBaseline = null;
            _outputCapture.Clear();
        }
    }
}

