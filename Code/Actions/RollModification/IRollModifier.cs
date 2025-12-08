namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Interface for roll modifiers that can modify dice rolls
    /// </summary>
    public interface IRollModifier
    {
        /// <summary>
        /// Modifies a roll value
        /// </summary>
        /// <param name="baseRoll">The original roll value</param>
        /// <param name="context">Context about the roll (source, target, action, etc.)</param>
        /// <returns>The modified roll value</returns>
        int ModifyRoll(int baseRoll, RollModificationContext context);
        
        /// <summary>
        /// Gets the priority of this modifier (higher priority modifiers are applied first)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// Gets the name/identifier of this modifier
        /// </summary>
        string Name { get; }
    }
}

