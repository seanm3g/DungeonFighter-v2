using System;

namespace RPGGame.ActionInteractionLab
{
    public sealed partial class ActionInteractionLabSession
    {
        /// <summary>First visible index into the sorted catalog name list for the right panel.</summary>
        public int CatalogScrollOffset { get; set; }

        /// <summary>First visible index into the sorted enemy type list for the lab panel.</summary>
        public int EnemyCatalogScrollOffset { get; set; }

        /// <summary>
        /// Fallback visible enemy-type rows when the catalog window has not rendered yet (scroll math).
        /// Live row count comes from <see cref="LastEnemyCatalogVisibleRowCount"/> after layout.
        /// </summary>
        public const int EnemyCatalogVisibleRowCount = 10;

        /// <summary>Fallback visible catalog rows when layout has not rendered yet (scroll math).</summary>
        public const int LabCatalogVisibleNameRows = 4;

        /// <summary>Set each frame by the catalog renderer; action rows shown between ▲/▼.</summary>
        public int LastCatalogVisibleRowCount { get; set; }

        /// <summary>Set each frame by the catalog renderer; foe-type rows shown between ▲/▼.</summary>
        public int LastEnemyCatalogVisibleRowCount { get; set; }

        /// <summary>Inclusive grid X bounds for wheel-scrolling the action catalog in the catalog window; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMinGridX { get; set; } = -1;

        /// <summary>Inclusive grid X bounds for wheel-scrolling the action catalog in the catalog window; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMaxGridX { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds (▲ more through ▼ more) for wheel-scrolling the catalog; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMinGridY { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds for wheel-scrolling the catalog; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMaxGridY { get; set; } = -1;

        /// <summary>Inclusive grid X bounds for wheel-scrolling the enemy type list in the catalog window; <c>-1</c> when unset.</summary>
        public int LastEnemyCatalogWheelMinGridX { get; set; } = -1;

        /// <summary>Inclusive grid X bounds for wheel-scrolling the enemy type list in the catalog window; <c>-1</c> when unset.</summary>
        public int LastEnemyCatalogWheelMaxGridX { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds (▲ types through ▼ types) for wheel-scrolling the enemy list; <c>-1</c> when unset.</summary>
        public int LastEnemyCatalogWheelMinGridY { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds for wheel-scrolling the enemy list; <c>-1</c> when unset.</summary>
        public int LastEnemyCatalogWheelMaxGridY { get; set; } = -1;

        /// <summary>Batch size for Action Lab encounter simulation (wheel over the sim row steps 1 / 10 / 100 / 1000, clamped at ends).</summary>
        public int EncounterSimulationBatchCount { get; set; } = ActionLabEncounterSimulator.DefaultBatchEncounterCount;

        /// <summary>Visible snapshot rows in the tools panel list.</summary>
        public const int SnapshotListVisibleRowCount = 3;

        /// <summary>First visible index into <see cref="CharacterLabSnapshotService.ListNames"/>.</summary>
        public int SnapshotScrollOffset { get; set; }

        /// <summary>Currently highlighted snapshot display name (for Load).</summary>
        public string? SelectedSnapshotName { get; set; }

        /// <summary>Status line shown under the Snapshots block (load/save feedback).</summary>
        public string SnapshotStatusMessage { get; set; } = "";

        /// <summary>
        /// When true, batch encounter simulation uses parallel workers (<c>maxDegreeOfParallelism: -1</c> in <see cref="ActionLabEncounterSimulator.RunBatchAsync"/>).
        /// When false, encounters run sequentially (<c>maxDegreeOfParallelism: 1</c>).
        /// </summary>
        public bool UseParallelEncounterSimulation { get; set; } = true;

        /// <summary>
        /// When false (default), lab gear equip and combo-strip removal enforce attribute and weapon-basic requirements like the live game.
        /// When true, those gates are bypassed for sandbox testing.
        /// </summary>
        public bool IgnoreActionRequirements { get; set; }

        private static readonly int[] EncounterSimulationBatchTiers = { 1, 10, 100, 1000 };

        /// <summary>Inclusive grid X bounds for wheel-changing simulation batch count; <c>-1</c> when unset.</summary>
        public int LastSimBatchWheelMinGridX { get; set; } = -1;

        /// <summary>Inclusive grid X bounds for wheel-changing simulation batch count; <c>-1</c> when unset.</summary>
        public int LastSimBatchWheelMaxGridX { get; set; } = -1;

        /// <summary>Inclusive grid Y for the sim button row; <c>-1</c> when unset.</summary>
        public int LastSimBatchWheelGridY { get; set; } = -1;

        /// <summary>
        /// Moves <see cref="EncounterSimulationBatchCount"/> one tier in the 1 / 10 / 100 / 1000 sequence
        /// (<paramref name="direction"/> &gt; 0 toward larger counts), clamped at 1 and 1000 (no wrap).
        /// </summary>
        public void CycleEncounterSimulationBatchCount(int direction)
        {
            if (direction == 0)
                return;
            int sign = direction > 0 ? 1 : -1;
            int idx = Array.IndexOf(EncounterSimulationBatchTiers, EncounterSimulationBatchCount);
            if (idx < 0)
                idx = Array.IndexOf(EncounterSimulationBatchTiers, ActionLabEncounterSimulator.DefaultBatchEncounterCount);
            if (idx < 0)
                idx = 0;
            int newIdx = Math.Clamp(idx + sign, 0, EncounterSimulationBatchTiers.Length - 1);
            EncounterSimulationBatchCount = EncounterSimulationBatchTiers[newIdx];
        }
    }
}
