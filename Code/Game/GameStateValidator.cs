namespace RPGGame
{
    /// <summary>
    /// Validates game state transitions.
    /// Extracted from GameStateManager to separate validation logic from state management.
    /// </summary>
    public class GameStateValidator
    {
        /// <summary>
        /// Validates whether a state transition is allowed.
        /// Currently allows all transitions; can be enhanced with business logic.
        /// </summary>
        /// <param name=""from"">Current state.</param>
        /// <param name=""to"">Target state.</param>
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
    }
}