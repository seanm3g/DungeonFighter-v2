using System.Collections.Generic;

namespace RPGGame
{
    /// <summary>
    /// Stores level-up information to be displayed on the dungeon completion screen
    /// instead of immediately in the combat log
    /// </summary>
    public class LevelUpInfo
    {
        public int NewLevel { get; set; }
        public string? ClassName { get; set; }
        public string? StatIncreaseMessage { get; set; }
        public string? CurrentClass { get; set; }
        public string? FullNameWithQualifier { get; set; }
        public string? ClassPointsInfo { get; set; }
        public string? ClassUpgradeInfo { get; set; }
        public bool HasWeapon { get; set; }
        
        /// <summary>
        /// Returns true if this represents a valid level-up (has level information)
        /// </summary>
        public bool IsValid => NewLevel > 0;
        
        /// <summary>
        /// Gets formatted level-up messages for display
        /// </summary>
        public List<string> GetDisplayMessages()
        {
            var messages = new List<string>();
            
            if (!IsValid) return messages;
            
            messages.Add("*** LEVEL UP! ***");
            messages.Add($"You reached level {NewLevel}!");
            
            if (HasWeapon && !string.IsNullOrEmpty(ClassName))
            {
                messages.Add($"Gained +1 {ClassName} class point!");
                if (!string.IsNullOrEmpty(StatIncreaseMessage))
                {
                    messages.Add($"Stats increased: {StatIncreaseMessage}");
                }
                if (!string.IsNullOrEmpty(CurrentClass))
                {
                    messages.Add($"Current class: {CurrentClass}");
                }
                if (!string.IsNullOrEmpty(FullNameWithQualifier))
                {
                    messages.Add($"You are now known as: {FullNameWithQualifier}");
                }
                if (!string.IsNullOrEmpty(ClassPointsInfo))
                {
                    messages.Add($"Class Points: {ClassPointsInfo}");
                }
                if (!string.IsNullOrEmpty(ClassUpgradeInfo))
                {
                    messages.Add($"Next Upgrades: {ClassUpgradeInfo}");
                }
            }
            else
            {
                messages.Add("No weapon equipped - equal stat increases (+2 all stats)");
            }
            
            return messages;
        }
    }
}

