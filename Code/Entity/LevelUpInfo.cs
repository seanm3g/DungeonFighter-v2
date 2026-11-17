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
        public int ActionSlotIncrease { get; set; }
        public bool HasWeapon { get; set; }
        /// <summary>Available Skill Points on the weapon path after this level-up award.</summary>
        public int SkillPointsAvailable { get; set; }
        public bool AwardedSkillPoint { get; set; }

        /// <summary>
        /// Returns true if this represents a valid level-up (has level information)
        /// </summary>
        public bool IsValid => NewLevel > 0;
        
    }
}
