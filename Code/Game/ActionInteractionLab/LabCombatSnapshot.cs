using System;
using System.Collections.Generic;
using RPGGame.BattleStatistics;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Immutable capture of lab hero, enemy, strip, and catalog pick for encounter batch simulation.
    /// </summary>
    public sealed class LabCombatSnapshot
    {
        public LabCombatSnapshot(
            string initialPlayerJson,
            int labPanelStrDelta,
            int labPanelAgiDelta,
            int labPanelTecDelta,
            int labPanelIntDelta,
            int labPanelLevelDelta,
            int labPanelArmorDelta,
            string? sessionEnemyLoaderType,
            int enemyLevel,
            IReadOnlyList<string> comboStripActionNames,
            string selectedCatalogActionName)
        {
            InitialPlayerJson = initialPlayerJson ?? throw new ArgumentNullException(nameof(initialPlayerJson));
            LabPanelStrDelta = labPanelStrDelta;
            LabPanelAgiDelta = labPanelAgiDelta;
            LabPanelTecDelta = labPanelTecDelta;
            LabPanelIntDelta = labPanelIntDelta;
            LabPanelLevelDelta = labPanelLevelDelta;
            LabPanelArmorDelta = labPanelArmorDelta;
            SessionEnemyLoaderType = sessionEnemyLoaderType;
            EnemyLevel = enemyLevel;
            ComboStripActionNames = comboStripActionNames ?? throw new ArgumentNullException(nameof(comboStripActionNames));
            SelectedCatalogActionName = selectedCatalogActionName ?? "";
        }

        public string InitialPlayerJson { get; }
        public int LabPanelStrDelta { get; }
        public int LabPanelAgiDelta { get; }
        public int LabPanelTecDelta { get; }
        public int LabPanelIntDelta { get; }
        public int LabPanelLevelDelta { get; }
        public int LabPanelArmorDelta { get; }
        public string? SessionEnemyLoaderType { get; }
        public int EnemyLevel { get; }
        public IReadOnlyList<string> ComboStripActionNames { get; }
        public string SelectedCatalogActionName { get; }

        /// <summary>Matches <see cref="ActionInteractionLabSession"/> default dummy when no loader enemy is set.</summary>
        public static BattleConfiguration DefaultTestEnemyBattleConfig { get; } = new()
        {
            PlayerDamage = 10,
            PlayerAttackSpeed = 1.0,
            PlayerArmor = 0,
            PlayerHealth = 100,
            EnemyDamage = 10,
            EnemyAttackSpeed = 0.65,
            EnemyArmor = 5,
            EnemyHealth = 150
        };
    }
}
