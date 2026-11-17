using System.Collections.Generic;

namespace RPGGame.MCP.Models
{
    public class AgentContextSnapshot
    {
        public AgentScreenInfo Screen { get; set; } = new();
        public string Summary { get; set; } = "";
        public string SuggestedFocus { get; set; } = "";
        public string PendingInputMode { get; set; } = "Normal";
        public string? CustomLevelBuffer { get; set; }
        public List<string> Hints { get; set; } = new();
        public List<AgentChoice> Choices { get; set; } = new();
        public AgentPlayerBrief? Player { get; set; }
        public List<DungeonOptionSnapshot> Dungeons { get; set; } = new();
        public List<string> RecentEvents { get; set; } = new();
        /// <summary>Lines the last handle_input produced (combat log, loot, messages). Set only on handle_input replies.</summary>
        public List<string>? NewEvents { get; set; }
        /// <summary>How many earlier NewEvents lines were left out (long dungeon runs); read them with get_recent_output.</summary>
        public int? NewEventsOmitted { get; set; }
        public string? UserDirective { get; set; }
        public DungeonRunSummary? LastRunSummary { get; set; }
        public string CurrentState { get; set; } = "";
    }

    public class AgentScreenInfo
    {
        public string State { get; set; } = "";
        public string Title { get; set; } = "";
        /// <summary>Open prompt inside the screen (EquipPicker, Compare, ActionSequence, ...), or null.</summary>
        public string? SubScreen { get; set; }
        /// <summary>What the screen shows, as text (bag, comparison, sequence, skill nodes, regions...).</summary>
        public List<string> Body { get; set; } = new();
    }

    public class AgentChoice
    {
        public string Input { get; set; } = "";
        public string Label { get; set; } = "";
        public bool Recommended { get; set; }
    }

    public class AgentPlayerBrief
    {
        public string Name { get; set; } = "";
        public int Level { get; set; }
        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }
        public double HealthPercentage { get; set; }
        public int XP { get; set; }
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Technique { get; set; }
        public int Intelligence { get; set; }
        public string? WeaponName { get; set; }
        public int? WeaponTier { get; set; }
        public string? HeadArmor { get; set; }
        public string? BodyArmor { get; set; }
        public string? FeetArmor { get; set; }
        public string? LegsArmor { get; set; }
        public string? Charm { get; set; }
        public string? Region { get; set; }
        public List<string> ActionSequence { get; set; } = new();
        public int ActionSequenceCapacity { get; set; }
        public int UnspentSkillPoints { get; set; }
        public List<ItemSnapshot> Inventory { get; set; } = new();
    }

    public class DungeonOptionSnapshot
    {
        public int Index { get; set; }
        public string Name { get; set; } = "";
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public string Theme { get; set; } = "";
        public bool IsCustomLevelEntry { get; set; }
        public bool IsResetDifficultyEntry { get; set; }
    }

    public class DungeonRunSummary
    {
        public string Outcome { get; set; } = "";
        public string? DungeonName { get; set; }
        public int? RoomsCleared { get; set; }
        public int? TotalRooms { get; set; }
        public int LevelBefore { get; set; }
        public int LevelAfter { get; set; }
        public int HealthBefore { get; set; }
        public int HealthAfter { get; set; }
        public int XpBefore { get; set; }
        public int XpAfter { get; set; }
        public int InventoryCountBefore { get; set; }
        public int InventoryCountAfter { get; set; }
    }

    public class McpGameplayInputContext
    {
        public string PendingInputMode { get; set; } = "Normal";
        public string? CustomLevelBuffer { get; set; }
    }
}
