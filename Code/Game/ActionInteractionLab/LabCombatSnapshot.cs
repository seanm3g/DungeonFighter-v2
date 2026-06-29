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
            string selectedCatalogActionName,
            BattleConfiguration? labEnemyBattleConfig = null)
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
            LabEnemyBattleConfig = labEnemyBattleConfig;
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
        /// <summary>When set, overrides <see cref="DefaultTestEnemyBattleConfig"/> for batch sim dummies.</summary>
        public BattleConfiguration? LabEnemyBattleConfig { get; }

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

        /// <summary>
        /// Legacy tuned dummy HP pool. Fundamentals tuning sims use a loader enemy
        /// (<see cref="FundamentalsCombatSetup.DefaultFundamentalsEnemyType"/>) so global health multipliers apply.
        /// </summary>
        public static BattleConfiguration FundamentalsTestEnemyBattleConfig { get; } = new()
        {
            PlayerDamage = 10,
            PlayerAttackSpeed = 1.0,
            PlayerArmor = 0,
            PlayerHealth = 100,
            EnemyDamage = 8,
            EnemyAttackSpeed = 0.65,
            EnemyArmor = 18,
            EnemyHealth = 560
        };
    }
}
