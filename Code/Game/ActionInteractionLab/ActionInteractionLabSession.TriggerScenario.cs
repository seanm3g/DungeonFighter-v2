using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Entity.Services;
using RPGGame.Items.ItemTriggerScenario;

namespace RPGGame.ActionInteractionLab
{
    public sealed partial class ActionInteractionLabSession
    {
        /// <summary>Visible trigger-identity rows in the tools panel.</summary>
        public const int TriggerListVisibleRowCount = 4;

        /// <summary>First visible index into <see cref="ItemTriggerIdentityCatalog.Identities"/>.</summary>
        public int TriggerScrollOffset { get; set; }

        /// <summary>Selected catalog identity index (Triggers.json id).</summary>
        public int? SelectedTriggerIdentityIndex { get; set; }

        /// <summary>Status line under the Triggers block.</summary>
        public string TriggerStatusMessage { get; set; } = "";

        /// <summary>Last scenario report from Load/Run (shown compact in tools panel).</summary>
        public ItemTriggerScenarioReport? LastTriggerScenarioReport { get; set; }

        /// <summary>Optional filter substring for the Triggers list (name / WHEN / mechanics).</summary>
        public string TriggerListFilter { get; set; } = "";

        public IReadOnlyList<ItemTriggerIdentityCatalog.Identity> GetFilteredTriggerIdentities()
        {
            var all = ItemTriggerIdentityCatalog.Identities;
            if (string.IsNullOrWhiteSpace(TriggerListFilter))
                return all;
            return all.Where(i => ItemTriggerScenarioRunner.MatchesFilter(i, TriggerListFilter)).ToList();
        }

        public ItemTriggerIdentityCatalog.Identity? GetSelectedTriggerIdentity()
        {
            if (SelectedTriggerIdentityIndex is not int idx)
                return null;
            try
            {
                return ItemTriggerIdentityCatalog.Get(idx);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Auto-stages the selected trigger: for attacker combat WHENs, replaces lab hero/enemy/strip and sets d20.
        /// Equip / room-clear / take-hit identities are noted for <see cref="RunSelectedTriggerScenario"/>.
        /// </summary>
        public bool TryLoadSelectedTriggerScenario(out string? error)
        {
            error = null;
            var identity = GetSelectedTriggerIdentity();
            if (identity == null)
            {
                error = "Select a trigger identity first";
                return false;
            }

            string when = ActionTriggerGate.NormalizeToken(identity.When ?? "");
            bool combatAttacker = !identity.IsEquipEffect
                                  && when is not ("WHILEEQUIPPED" or "ONEQUIP" or "ONROOMSCLEARED" or "ONROOMCLEARED"
                                      or "ONTAKEHIT" or "ONHEROHURT");

            if (!combatAttacker)
            {
                TriggerStatusMessage = $"#{identity.Index} {identity.Name}: use [ Run ] (path={when})";
                LastTriggerScenarioReport = null;
                _refreshCombatUi();
                return true;
            }

            try
            {
                var setup = ItemTriggerScenarioRunner.PrepareAttackerSetup(identity);
                ApplyTriggerScenarioSetup(setup);
                TriggerStatusMessage =
                    $"Loaded #{identity.Index} d20={setup.ForcedD20} WHEN={identity.When}";
                LastTriggerScenarioReport = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                TriggerStatusMessage = $"Load failed: {ex.Message}";
                return false;
            }
        }

        /// <summary>Runs the selected identity through <see cref="ItemTriggerScenarioRunner"/> and stores the report.</summary>
        public ItemTriggerScenarioReport RunSelectedTriggerScenario()
        {
            var identity = GetSelectedTriggerIdentity()
                           ?? throw new InvalidOperationException("Select a trigger identity first");
            var report = ItemTriggerScenarioRunner.Run(identity);
            LastTriggerScenarioReport = report;
            TriggerStatusMessage = report.Passed
                ? $"PASS #{report.IdentityIndex}: {Truncate(report.Finding, 40)}"
                : $"FAIL #{report.IdentityIndex}: {Truncate(report.Finding, 40)}";

            // After a successful combat attacker run, stage the lab to match for visual follow-up
            string when = ActionTriggerGate.NormalizeToken(identity.When ?? "");
            bool combatAttacker = !identity.IsEquipEffect
                                  && when is not ("WHILEEQUIPPED" or "ONEQUIP" or "ONROOMSCLEARED" or "ONROOMCLEARED"
                                      or "ONTAKEHIT" or "ONHEROHURT");
            if (combatAttacker && report.Passed)
            {
                try
                {
                    var setup = ItemTriggerScenarioRunner.PrepareAttackerSetup(identity);
                    ApplyTriggerScenarioSetup(setup);
                }
                catch
                {
                    // Report still valid even if re-stage fails
                }
            }

            _refreshCombatUi();
            return report;
        }

        /// <summary>Runs all filtered identities; returns batch report text.</summary>
        public ItemTriggerScenarioBatchResult RunAllFilteredTriggerScenarios()
        {
            string? filter = string.IsNullOrWhiteSpace(TriggerListFilter) ? null : TriggerListFilter;
            var batch = ItemTriggerScenarioRunner.RunAll(filter);
            LastTriggerScenarioReport = batch.Reports.LastOrDefault();
            TriggerStatusMessage = $"Batch {batch.Passed}/{batch.Total} passed";
            _refreshCombatUi();
            return batch;
        }

        private void ApplyTriggerScenarioSetup(ItemTriggerScenarioSetup setup)
        {
            _labPlayer = setup.Hero;
            _labEnemy = setup.Enemy;
            _sessionEnemyLoaderType = null;
            _labEnemyBaseLevel = setup.Enemy.Level;
            _labPanelEnemyLevelDelta = 0;

            UseRandomD20PerStep = false;
            UseSeededD20 = false;
            SelectedD20 = setup.ForcedD20;
            SelectedCatalogActionName = setup.Swing.Name ?? "TrigSwing";

            ClearStepHistoryAndSnapshots();
            ResetSimulatedCombatTurnAccumulator();
            ResetLabPanelDeltas();
            BootstrapCombatState();
            var serializer = new CharacterSerializer();
            _initialPlayerJson = serializer.Serialize(_labPlayer);
            SyncCatalogSelectionToUpcomingActor();
            SyncLabEnemyToCanvasContext();
            if (_restoreTarget != null)
                ApplyLabToCanvasContext(_restoreTarget);
            _refreshCombatUi();
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, Math.Max(0, max - 1)) + "…";
        }
    }
}
