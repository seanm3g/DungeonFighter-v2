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
        public string? UserDirective { get; set; }
        public DungeonRunSummary? LastRunSummary { get; set; }
        public string CurrentState { get; set; } = "";
    }

    public class AgentScreenInfo
    {
        public string State { get; set; } = "";
        public string Title { get; set; } = "";
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
