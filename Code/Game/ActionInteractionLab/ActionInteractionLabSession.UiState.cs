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
        /// Visible enemy-type rows between ▲/▼ in the Action Lab tools panel.
        /// Keep in sync with <see cref="RPGGame.UI.Avalonia.ActionInteractionLab.ActionLabControlsRenderer"/> layout.
        /// </summary>
        public const int EnemyCatalogVisibleRowCount = 5;

        /// <summary>Fallback visible catalog rows when layout has not rendered yet (scroll math).</summary>
        public const int LabCatalogVisibleNameRows = 4;

        /// <summary>Set each frame by <see cref="RPGGame.UI.Avalonia.ActionInteractionLab.ActionLabControlsRenderer"/>; rows shown between ▲/▼.</summary>
        public int LastCatalogVisibleRowCount { get; set; }

        /// <summary>Inclusive grid X bounds for wheel-scrolling the action catalog in the tools panel; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMinGridX { get; set; } = -1;

        /// <summary>Inclusive grid X bounds for wheel-scrolling the action catalog in the tools panel; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMaxGridX { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds (▲ more through ▼ more) for wheel-scrolling the catalog; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMinGridY { get; set; } = -1;

        /// <summary>Inclusive grid Y bounds for wheel-scrolling the catalog; <c>-1</c> when unset.</summary>
        public int LastCatalogWheelMaxGridY { get; set; } = -1;

        /// <summary>Batch size for Action Lab encounter simulation (wheel over the sim row steps 1 / 10 / 100 / 1000, clamped at ends).</summary>
        public int EncounterSimulationBatchCount { get; set; } = ActionLabEncounterSimulator.DefaultBatchEncounterCount;

        /// <summary>
        /// When true, batch encounter simulation uses parallel workers (<c>maxDegreeOfParallelism: -1</c> in <see cref="ActionLabEncounterSimulator.RunBatchAsync"/>).
        /// When false, encounters run sequentially (<c>maxDegreeOfParallelism: 1</c>).
        /// </summary>
        public bool UseParallelEncounterSimulation { get; set; } = true;

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
