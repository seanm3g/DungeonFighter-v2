using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.ActionInteractionLab;
using RPGGame.Entity.Services;
using RPGGame.Tuning.Profiles;

namespace RPGGame.Tuning.LabBalance
{
    public sealed class LabBalanceScenarioOverrides
    {
        public string? WeaponType { get; init; }
        public string? EnemyType { get; init; }
        public int? PlayerLevel { get; init; }
        public int? EnemyLevel { get; init; }
        public string? ForcedCatalogAction { get; init; }
        public bool? StripNonWeaponGear { get; init; }
    }

    public sealed class LabBalanceScenarioSummary
    {
        public LabBalanceProcessLayer Layer { get; init; }
        public string WeaponType { get; init; } = "";
        public string EnemyType { get; init; } = "";
        public int PlayerLevel { get; init; }
        public int EnemyLevel { get; init; }
        public string ForcedCatalogAction { get; init; } = "";
        public bool WeaponOnly { get; init; }
        public string Description { get; init; } = "";
    }

    /// <summary>
    /// Applies a process-layer recipe to the live Action Lab session (hero, foe, forced action).
    /// </summary>
    public static class LabBalanceScenarioApplicator
    {
        public static LabBalanceScenarioSummary Apply(
            ActionInteractionLabSession session,
            LabBalanceProcessLayer layer,
            LabBalanceScenarioOverrides? overrides = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            var def = LabBalanceLayerDefinition.Get(layer);
            if (def.StatsMode == LabBalanceStatsMode.DeepLinkOnly)
            {
                session.ActiveBalanceProcessLayer = layer;
                return new LabBalanceScenarioSummary
                {
                    Layer = layer,
                    Description = def.Description,
                    WeaponType = overrides?.WeaponType ?? def.DefaultWeaponType,
                    EnemyType = overrides?.EnemyType ?? def.DefaultEnemyType,
                    PlayerLevel = overrides?.PlayerLevel ?? def.DefaultPlayerLevel,
                    EnemyLevel = overrides?.EnemyLevel ?? def.DefaultEnemyLevel,
                    WeaponOnly = false
                };
            }

            bool strip = overrides?.StripNonWeaponGear ?? def.StripNonWeaponGear;
            string weaponType = overrides?.WeaponType ?? def.DefaultWeaponType;
            string enemyType = overrides?.EnemyType ?? def.DefaultEnemyType;
            int playerLevel = Math.Clamp(overrides?.PlayerLevel ?? def.DefaultPlayerLevel, 1, 99);
            int enemyLevel = Math.Clamp(overrides?.EnemyLevel ?? def.DefaultEnemyLevel, 1, 99);

            ActionLoader.ReloadActions();
            EnemyLoader.LoadEnemies();

            if (layer == LabBalanceProcessLayer.GearInjection && !strip)
            {
                // Keep current gear; only retarget levels + enemy if needed.
                session.ApplyBalanceLevelAndEnemy(enemyType, enemyLevel, playerLevel);
            }
            else
            {
                var snapshot = BuildFundamentalsStyleSnapshot(
                    weaponType, enemyType, playerLevel, enemyLevel, overrides?.ForcedCatalogAction);
                session.ApplyBalanceScenarioFromSnapshot(snapshot, stripArmorSlots: strip);
            }

            if (layer == LabBalanceProcessLayer.DungeonAttrition)
            {
                if (string.IsNullOrWhiteSpace(session.LabDungeonCatalogKey))
                {
                    var names = ActionLabDungeonFactory.ListCatalogDungeonNames();
                    if (names.Count > 0)
                        session.LabDungeonCatalogKey = names[0];
                }

                session.LabDungeonLevelDelta = 0;
                try
                {
                    session.GenerateLabDungeon();
                }
                catch
                {
                    /* dungeon catalog may be empty in tests */
                }
            }

            session.ActiveBalanceProcessLayer = layer;

            return new LabBalanceScenarioSummary
            {
                Layer = layer,
                WeaponType = weaponType,
                EnemyType = session.SessionEnemyLoaderType ?? enemyType,
                PlayerLevel = Math.Clamp(session.LabPlayer.Level, 1, 99),
                EnemyLevel = Math.Clamp(session.LabEnemy.Level, 1, 99),
                ForcedCatalogAction = session.SelectedCatalogActionName ?? "",
                WeaponOnly = strip,
                Description = def.Description
            };
        }

        /// <summary>Builds the same style of snapshot as <see cref="FundamentalsCombatSetup"/> for applicator/tests.</summary>
        public static LabCombatSnapshot BuildFundamentalsStyleSnapshot(
            string weaponType,
            string enemyType,
            int playerLevel,
            int enemyLevel,
            string? forcedCatalogAction = null)
        {
            var config = new SimulationProfileConfig
            {
                Mode = "fundamentals_encounter",
                PlayerLevel = playerLevel,
                EnemyLevel = enemyLevel,
                WeaponType = weaponType,
                EnemyType = enemyType,
                ForcedCatalogAction = forcedCatalogAction
            };
            return FundamentalsCombatSetup.BuildSnapshot(config);
        }

        public static IReadOnlyList<CombatTuningParameter> GetKnobsForLayer(LabBalanceProcessLayer layer)
        {
            var def = LabBalanceLayerDefinition.Get(layer);
            if (def.RegistryLayers.Count == 0)
                return Array.Empty<CombatTuningParameter>();

            return CombatTuningParameterRegistry.All
                .Where(p => def.RegistryLayers.Contains(p.Layer) && p.IsImplemented)
                .ToList();
        }
    }
}
