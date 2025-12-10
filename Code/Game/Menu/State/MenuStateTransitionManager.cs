using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RPGGame;

namespace DungeonFighter.Game.Menu.State
{
    /// <summary>
    /// Centralized manager for all state transitions in the menu system.
    /// Enforces valid state transitions, validates conditions, and logs all state changes.
    /// 
    /// This replaces scattered state transitions throughout the codebase with a single,
    /// auditable state machine.
    /// </summary>
    public class MenuStateTransitionManager
    {
        private readonly GameStateManager stateManager;
        private readonly List<StateTransitionRule> transitionRules;
        private GameState currentState;

        // Events for state changes
        public event EventHandler<StateChangeEventArgs>? OnBeforeStateChange;
        public event EventHandler<StateChangeEventArgs>? OnAfterStateChange;
        public event EventHandler<InvalidTransitionEventArgs>? OnInvalidTransition;

        /// <summary>
        /// Creates a new MenuStateTransitionManager.
        /// </summary>
        public MenuStateTransitionManager(GameStateManager stateManager)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.currentState = stateManager.CurrentState;
            this.transitionRules = new List<StateTransitionRule>();
            
            // Initialize with default valid transitions
            InitializeDefaultTransitions();
        }

        /// <summary>
        /// Attempts to transition to a new state.
        /// Validates the transition against rules, fires events, and updates state.
        /// </summary>
        public Task<bool> TransitionToAsync(GameState newState, string? reason = null)
        {
            return Task.FromResult(TransitionToInternal(newState, reason));
        }

        /// <summary>
        /// Internal implementation of state transition.
        /// </summary>
        private bool TransitionToInternal(GameState newState, string? reason = null)
        {

            // Check if transition is valid
            var isValid = IsTransitionValid(currentState, newState);
            
            if (!isValid)
            {
                HandleInvalidTransition(currentState, newState, reason);
                return false;
            }

            // Fire before-change event
            var eventArgs = new StateChangeEventArgs(currentState, newState, reason);
            OnBeforeStateChange?.Invoke(this, eventArgs);

            // Store previous state before attempting transition
            var previousState = currentState;
            
            try
            {
                // Update state
                currentState = newState;
                stateManager.TransitionToState(newState);
                // Fire after-change event
                OnAfterStateChange?.Invoke(this, eventArgs);

                return true;
            }
            catch (Exception ex)
            {
                // Attempt to revert to previous state
                currentState = previousState;
                return false;
            }
        }

        /// <summary>
        /// Synchronous version of TransitionToAsync.
        /// </summary>
        public bool TransitionTo(GameState newState, string? reason = null)
        {
            return TransitionToAsync(newState, reason).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Checks if a transition from one state to another is valid.
        /// </summary>
        public bool IsTransitionValid(GameState from, GameState to)
        {
            // Self-transitions are always allowed (except for special states)
            if (from == to)
            {
                return !(from == GameState.Combat || from == GameState.Dungeon);
            }

            // Check against defined rules
            var rule = transitionRules.FirstOrDefault(r => r.Matches(from, to));
            
            if (rule == null)
            {
                return false;
            }

            // Check condition if present
            if (!rule.IsValid())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Registers a state transition rule.
        /// </summary>
        public void RegisterTransition(StateTransitionRule rule)
        {
            transitionRules.Add(rule);
        }

        /// <summary>
        /// Initialize default valid state transitions.
        /// This defines the state machine's allowed transitions.
        /// </summary>
        private void InitializeDefaultTransitions()
        {
            // Main Menu transitions
            RegisterTransition(new StateTransitionRule(
                GameState.MainMenu, GameState.WeaponSelection, "New Game"));
            RegisterTransition(new StateTransitionRule(
                GameState.MainMenu, GameState.GameLoop, "Load Game"));
            RegisterTransition(new StateTransitionRule(
                GameState.MainMenu, GameState.Settings, "Settings"));
            RegisterTransition(new StateTransitionRule(
                GameState.MainMenu, GameState.Testing, "Testing (Debug)"));

            // Character Creation transitions
            RegisterTransition(new StateTransitionRule(
                GameState.CharacterCreation, GameState.GameLoop, "Character Confirmed"));
            RegisterTransition(new StateTransitionRule(
                GameState.CharacterCreation, GameState.MainMenu, "Character Cancelled"));

            // Weapon Selection transitions
            RegisterTransition(new StateTransitionRule(
                GameState.WeaponSelection, GameState.CharacterCreation, "Weapon Selected"));
            RegisterTransition(new StateTransitionRule(
                GameState.WeaponSelection, GameState.MainMenu, "Weapon Selection Cancelled"));

            // Game Loop transitions
            RegisterTransition(new StateTransitionRule(
                GameState.GameLoop, GameState.DungeonSelection, "Start Dungeon"));
            RegisterTransition(new StateTransitionRule(
                GameState.GameLoop, GameState.Inventory, "Open Inventory"));
            RegisterTransition(new StateTransitionRule(
                GameState.GameLoop, GameState.CharacterInfo, "View Character"));
            RegisterTransition(new StateTransitionRule(
                GameState.GameLoop, GameState.Settings, "Settings"));
            RegisterTransition(new StateTransitionRule(
                GameState.GameLoop, GameState.MainMenu, "Return to Menu"));

            // Inventory transitions
            RegisterTransition(new StateTransitionRule(
                GameState.Inventory, GameState.GameLoop, "Inventory Closed"));
            RegisterTransition(new StateTransitionRule(
                GameState.Inventory, GameState.MainMenu, "Exit to Menu"));

            // Character Info transitions
            RegisterTransition(new StateTransitionRule(
                GameState.CharacterInfo, GameState.GameLoop, "Character Info Closed"));

            // Settings transitions
            RegisterTransition(new StateTransitionRule(
                GameState.Settings, GameState.MainMenu, "Settings Closed"));
            RegisterTransition(new StateTransitionRule(
                GameState.Settings, GameState.GameLoop, "Settings Closed (In Game)"));
            RegisterTransition(new StateTransitionRule(
                GameState.Settings, GameState.Testing, "Testing (Debug)"));

            // Dungeon Selection transitions
            RegisterTransition(new StateTransitionRule(
                GameState.DungeonSelection, GameState.Dungeon, "Dungeon Selected"));
            RegisterTransition(new StateTransitionRule(
                GameState.DungeonSelection, GameState.GameLoop, "Dungeon Selection Cancelled"));

            // Dungeon transitions
            RegisterTransition(new StateTransitionRule(
                GameState.Dungeon, GameState.Combat, "Combat Started"));
            RegisterTransition(new StateTransitionRule(
                GameState.Dungeon, GameState.DungeonCompletion, "Dungeon Completed"));

            // Combat transitions
            RegisterTransition(new StateTransitionRule(
                GameState.Combat, GameState.Dungeon, "Combat Finished"));

            // Dungeon Completion transitions
            RegisterTransition(new StateTransitionRule(
                GameState.DungeonCompletion, GameState.GameLoop, "Return to Game"));
            RegisterTransition(new StateTransitionRule(
                GameState.DungeonCompletion, GameState.DungeonSelection, "Continue Dungeons"));

            // Testing transitions
            RegisterTransition(new StateTransitionRule(
                GameState.Testing, GameState.Settings, "Exit Testing"));
            RegisterTransition(new StateTransitionRule(
                GameState.Testing, GameState.MainMenu, "Exit Testing to Menu"));
        }

        /// <summary>
        /// Handle an invalid transition attempt.
        /// </summary>
        private void HandleInvalidTransition(GameState from, GameState to, string? reason)
        {
            var error = $"Invalid transition: {from} → {to}";
            if (reason != null) error += $" ({reason})";
            OnInvalidTransition?.Invoke(this, 
                new InvalidTransitionEventArgs(from, to, error));
        }

        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameState CurrentState => currentState;

        /// <summary>
        /// Gets all registered transition rules (for debugging/logging).
        /// </summary>
        public IReadOnlyList<StateTransitionRule> TransitionRules => transitionRules.AsReadOnly();
    }

    /// <summary>
    /// Event arguments for state change events.
    /// </summary>
    public class StateChangeEventArgs : EventArgs
    {
        public GameState FromState { get; }
        public GameState ToState { get; }
        public string? Reason { get; }
        public DateTime Timestamp { get; }

        public StateChangeEventArgs(GameState from, GameState to, string? reason = null)
        {
            FromState = from;
            ToState = to;
            Reason = reason;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for invalid transition attempts.
    /// </summary>
    public class InvalidTransitionEventArgs : EventArgs
    {
        public GameState FromState { get; }
        public GameState ToState { get; }
        public string ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public InvalidTransitionEventArgs(GameState from, GameState to, string errorMessage)
        {
            FromState = from;
            ToState = to;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.UtcNow;
        }
    }
}

