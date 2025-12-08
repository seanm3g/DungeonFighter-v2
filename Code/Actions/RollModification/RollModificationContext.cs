using RPGGame;

namespace RPGGame.Actions.RollModification
{
    /// <summary>
    /// Context information for roll modifications
    /// </summary>
    public class RollModificationContext
    {
        public Actor Source { get; set; }
        public Actor? Target { get; set; }
        public Action? Action { get; set; }
        public int OriginalRoll { get; set; }
        public int RollBonus { get; set; }
        public bool IsCombo { get; set; }
        public bool IsCritical { get; set; }
        public bool IsMiss { get; set; }
        
        public RollModificationContext(Actor source, Actor? target = null, Action? action = null)
        {
            Source = source;
            Target = target;
            Action = action;
        }
    }
}

