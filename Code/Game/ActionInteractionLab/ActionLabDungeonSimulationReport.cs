using System;
using System.Collections.Generic;

namespace RPGGame.ActionInteractionLab
{
    public sealed class ActionLabDungeonRunMetrics
    {
        public bool ClearedDungeon { get; set; }
        public int RoomsCleared { get; set; }
        public int HostileRoomCount { get; set; }
        public int DeathRoomIndex { get; set; } = -1;
        public int PlayerFinalHealth { get; set; }
        public string? ErrorMessage { get; set; }
        public int SeedUsed { get; set; }
    }

    public sealed class ActionLabDungeonSimulationReport
    {
        public List<ActionLabDungeonRunMetrics> Runs { get; } = new();
        public int ClearedRuns { get; set; }
        public double ClearRate { get; set; }
        public double AverageRoomsCleared { get; set; }
        public int ErroredRuns { get; set; }
        public Dictionary<int, int> DeathsByRoomIndex { get; } = new();
        public TimeSpan SimulationWallElapsed { get; set; }
        public string CatalogKey { get; set; } = "";
        public int DungeonLevel { get; set; }
        public int BaseSeed { get; set; }
    }
}
