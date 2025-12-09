namespace RPGGame
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Manages all game state including current game state, player, inventory, and dungeon progression.
    /// This manager centralizes state management that was previously scattered throughout Game.cs.
    /// 
    /// Responsibilities:
    /// - Track current game state (MainMenu, Combat, Dungeon, etc.)
    /// - Manage player character reference
    /// - Manage inventory
    /// - Manage available dungeons
    /// - Track current dungeon and room
    /// - Validate state transitions
    /// - Provide clean API for state operations
    /// - Notify subscribers of state changes via events
    /// </summary>
    public class GameStateManager
    {
        // State fields
        private GameState currentState = GameState.MainMenu;
        private Character? currentPlayer;
        private List<Item> currentInventory = new();
        private List<Dungeon> availableDungeons = new();
        private Dungeon? currentDungeon;
        private Environment? currentRoom;
        
        /// <summary>
        /// Event fired when game state transitions occur.
        /// Allows systems (like UI animations) to react to state changes.
        /// </summary>
        public event EventHandler<StateChangedEventArgs>? StateChanged;

        /// <summary>
        /// Gets or sets the current game state.
        /// </summary>
        public GameState CurrentState
        {
            get => currentState;
            private set => currentState = value;
        }

        /// <summary>
        /// Gets or sets the current player character.
        /// When set, also updates the inventory reference if player has inventory.
        /// </summary>
        public Character? CurrentPlayer
        {
            get => currentPlayer;
            private set => currentPlayer = value;
        }

        /// <summary>
        /// Gets the current inventory.
        /// </summary>
        public List<Item> CurrentInventory
        {
            get => currentInventory;
            private set => currentInventory = value;
        }

        /// <summary>
        /// Gets the list of available dungeons.
        /// </summary>
        public List<Dungeon> AvailableDungeons
        {
            get => availableDungeons;
            private set => availableDungeons = value;
        }

        /// <summary>
        /// Gets or sets the current dungeon being explored.
        /// </summary>
        public Dungeon? CurrentDungeon
        {
            get => currentDungeon;
            private set => currentDungeon = value;
        }

        /// <summary>
        /// Gets or sets the current room within a dungeon.
        /// </summary>
        public Environment? CurrentRoom
        {
            get => currentRoom;
            private set => currentRoom = value;
        }

        /// <summary>
        /// Convenience property to check if a player exists.
        /// </summary>
        public bool HasPlayer => CurrentPlayer != null;

        /// <summary>
        /// Convenience property to check if a dungeon is active.
        /// </summary>
        public bool HasCurrentDungeon => CurrentDungeon != null;

        /// <summary>
        /// Convenience property to check if a room is active.
        /// </summary>
        public bool HasCurrentRoom => CurrentRoom != null;

        /// <summary>
        /// Transitions the game to a new state.
        /// Validates the transition before applying and fires StateChanged event.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        /// <returns>True if transition was successful, false if invalid.</returns>
        public bool TransitionToState(GameState newState)
        {
            if (ValidateStateTransition(currentState, newState))
            {
                var previousState = currentState;
                CurrentState = newState;
                
                // Fire state change event to notify subscribers (e.g., UI animations)
                StateChanged?.Invoke(this, new StateChangedEventArgs(previousState, newState));
                
                return true;
            }

            ErrorHandler.LogWarning($"Invalid state transition from {currentState} to {newState}");
            return false;
        }

        /// <summary>
        /// Validates whether a state transition is allowed.
        /// Currently allows all transitions; can be enhanced with business logic.
        /// </summary>
        /// <param name="from">Current state.</param>
        /// <param name="to">Target state.</param>
        /// <returns>True if transition is valid, false otherwise.</returns>
        public bool ValidateStateTransition(GameState from, GameState to)
        {
            // Define invalid transitions based on game logic
            // For now, we allow all transitions as the original Game.cs does
            // This can be enhanced later with specific validation rules
            
            // Example rules that could be added:
            // - Can't go from MainMenu to Combat directly
            // - Can only go to Combat from Dungeon
            // - Can't leave Combat without completing it
            
            return true; // All transitions valid for now
        }

        /// <summary>
        /// Sets the current player and updates inventory reference.
        /// </summary>
        /// <param name="player">The player character to set, or null to clear.</param>
        public void SetCurrentPlayer(Character? player)
        {
            CurrentPlayer = player;
            if (player != null && player.Inventory != null)
            {
                CurrentInventory = player.Inventory;
            }
            else
            {
                CurrentInventory.Clear();
            }
        }

        /// <summary>
        /// Sets the current dungeon and clears the current room.
        /// </summary>
        /// <param name="dungeon">The dungeon to set, or null to clear.</param>
        public void SetCurrentDungeon(Dungeon? dungeon)
        {
            CurrentDungeon = dungeon;
            CurrentRoom = null; // Reset room when entering new dungeon
        }

        /// <summary>
        /// Sets the current room within the active dungeon.
        /// </summary>
        /// <param name="room">The room to set, or null to clear.</param>
        public void SetCurrentRoom(Environment? room)
        {
            CurrentRoom = room;
        }

        /// <summary>
        /// Updates the list of available dungeons.
        /// </summary>
        /// <param name="dungeons">New list of dungeons.</param>
        public void SetAvailableDungeons(List<Dungeon> dungeons)
        {
            AvailableDungeons = new List<Dungeon>(dungeons ?? new List<Dungeon>());
        }

        /// <summary>
        /// Resets all game state to initial values.
        /// Used when starting a new game or returning to main menu.
        /// </summary>
        public void ResetGameState()
        {
            CurrentState = GameState.MainMenu;
            CurrentPlayer = null;
            CurrentInventory.Clear();
            AvailableDungeons.Clear();
            CurrentDungeon = null;
            CurrentRoom = null;
        }

        /// <summary>
        /// Resets only the dungeon-related state.
        /// Used when completing a dungeon or returning to game loop.
        /// </summary>
        public void ResetDungeonState()
        {
            CurrentDungeon = null;
            CurrentRoom = null;
        }

        /// <summary>
        /// Gets a string representation of the current game state for debugging.
        /// </summary>
        /// <returns>Formatted string showing current state information.</returns>
        public override string ToString()
        {
            return $"GameState: {CurrentState}, " +
                   $"Player: {CurrentPlayer?.Name ?? "None"}, " +
                   $"Dungeon: {CurrentDungeon?.Theme ?? "None"}, " +
                   $"Room: {CurrentRoom?.Name ?? "None"}";
        }
    }
    
    /// <summary>
    /// Event arguments for state change events.
    /// </summary>
    public class StateChangedEventArgs : EventArgs
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }
        
        public StateChangedEventArgs(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}

