using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Entity.Services;
using RPGGame.Tuning;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.ActionInteractionLab
{
    public sealed partial class ActionInteractionLabSession
    {
        /// <summary>
        /// Cumulative left-panel lab tweaks since <see cref="_initialPlayerJson"/> was last written (Begin / ApplyLabGear).
        /// Undo replay reapplies these on the cloned hero so combat rewind does not reset lab edits.
        /// </summary>
        private int _labPanelStrDelta;
        private int _labPanelAgiDelta;
        private int _labPanelTecDelta;
        private int _labPanelIntDelta;
        private int _labPanelLevelDelta;
        private int _labPanelArmorDelta;
        private int _labPanelActionSlotDelta;

        /// <summary>Enemy level from the last loader pick (or 1 for the default lab dummy).</summary>
        private int _labEnemyBaseLevel = 1;

        /// <summary>Cumulative right-panel enemy level tweaks (effective = clamp(base + delta, 1–99)).</summary>
        private int _labPanelEnemyLevelDelta;

        /// <summary>Default loader enemy when Action Lab opens (must match a name in Enemies.json).</summary>
        private const string DefaultCatalogEnemyType = "Sandstorm Flanker";

        /// <summary>
        /// When <see cref="EnemyLoader"/> contains <see cref="DefaultCatalogEnemyType"/>, replaces the default lab
        /// dummy with that level-1 loader enemy and scrolls the enemy type list so that type is visible.
        /// </summary>
        private static void TryBeginWithDefaultCatalogEnemy(ActionInteractionLabSession session)
        {
            EnemyLoader.LoadEnemies();
            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            if (enemyTypes.Count == 0)
                return;

            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
            int pickIdx = enemyTypes.FindIndex(t => string.Equals(t, DefaultCatalogEnemyType, StringComparison.OrdinalIgnoreCase));
            if (pickIdx < 0)
                return;

            string pick = enemyTypes[pickIdx];
            var created = EnemyLoader.CreateEnemy(pick, level: 1);
            if (created == null)
                return;

            session._labEnemy = created;
            session._sessionEnemyLoaderType = pick;
            session._labEnemyBaseLevel = 1;
            session._labPanelEnemyLevelDelta = 0;
            int maxScroll = Math.Max(0, enemyTypes.Count - EnemyCatalogVisibleRowCount);
            session.EnemyCatalogScrollOffset = Math.Clamp(pickIdx, 0, maxScroll);
        }

        /// <summary>
        /// Equips a new weapon on the lab hero (from <see cref="ActionLabWeaponFactory"/>), clears step history, and re-bases undo snapshot.
        /// </summary>
        public void ApplyLabWeapon(WeaponItem weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));
            ApplyLabGear(weapon, "weapon");
        }

        /// <summary>
        /// Equips lab gear for <paramref name="slot"/> (<c>weapon</c>, <c>head</c>, <c>body</c>, <c>legs</c>, <c>feet</c>), clears step history, and re-bases undo snapshot.
        /// </summary>
        /// <returns><c>true</c> if gear was applied; <c>false</c> if attribute requirements block equip (no lab state change).</returns>
        public bool TryApplyLabGear(Item item, string slot, out string? failureReason)
        {
            failureReason = null;
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (string.IsNullOrWhiteSpace(slot))
                throw new ArgumentException("Slot is required.", nameof(slot));

            string s = slot.Trim().ToLowerInvariant();
            switch (s)
            {
                case "weapon":
                    if (item is not WeaponItem)
                        throw new ArgumentException("Item must be a WeaponItem for weapon slot.", nameof(item));
                    break;
                case "head":
                    if (item is not HeadItem)
                        throw new ArgumentException("Item must be a HeadItem for head slot.", nameof(item));
                    break;
                case "body":
                    if (item is not ChestItem)
                        throw new ArgumentException("Item must be a ChestItem for body slot.", nameof(item));
                    break;
                case "legs":
                    if (item is not LegsItem)
                        throw new ArgumentException("Item must be a LegsItem for legs slot.", nameof(item));
                    break;
                case "feet":
                    if (item is not FeetItem)
                        throw new ArgumentException("Item must be a FeetItem for feet slot.", nameof(item));
                    break;
                default:
                    throw new ArgumentException("Slot must be weapon, head, body, legs, or feet.", nameof(slot));
            }

            if (!_labPlayer.TryEquipItem(item, s, out _, out var reqFail, IgnoreActionRequirements))
            {
                failureReason = reqFail ?? "Cannot equip item (attribute requirements).";
                return false;
            }

            _history.Clear();
            ResetSimulatedCombatTurnAccumulator();
            ResetLabPanelDeltas();
            BootstrapCombatState();
            var serializer = new CharacterSerializer();
            _initialPlayerJson = serializer.Serialize(_labPlayer);
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
            return true;
        }

        /// <summary>
        /// Removes an action from the lab hero combo strip, honoring <see cref="IgnoreActionRequirements"/>.
        /// </summary>
        public bool TryRemoveFromLabCombo(Action action) =>
            _labPlayer.RemoveFromCombo(action, ignoreWeaponRequirement: IgnoreActionRequirements);

        /// <summary>
        /// Same as <see cref="TryApplyLabGear"/> but throws <see cref="InvalidOperationException"/> when requirements block equip.
        /// </summary>
        public void ApplyLabGear(Item item, string slot)
        {
            if (!TryApplyLabGear(item, slot, out var err))
                throw new InvalidOperationException(err ?? "Cannot equip item in Action Lab.");
        }

        /// <summary>
        /// Unequips the lab hero in <paramref name="slot"/> (<c>weapon</c>, <c>head</c>, <c>body</c>, <c>legs</c>, <c>feet</c>),
        /// clears step history, and re-bases undo snapshot (same bookkeeping as <see cref="ApplyLabGear"/>).
        /// </summary>
        public void ClearLabGear(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                throw new ArgumentException("Slot is required.", nameof(slot));

            string s = slot.Trim().ToLowerInvariant();
            switch (s)
            {
                case "weapon":
                case "head":
                case "body":
                case "legs":
                case "feet":
                    _labPlayer.UnequipItem(s);
                    break;
                default:
                    throw new ArgumentException("Slot must be weapon, head, body, legs, or feet.", nameof(slot));
            }

            _history.Clear();
            ResetSimulatedCombatTurnAccumulator();
            ResetLabPanelDeltas();
            BootstrapCombatState();
            var serializer = new CharacterSerializer();
            _initialPlayerJson = serializer.Serialize(_labPlayer);
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
        }

        private void ResetLabPanelDeltas()
        {
            _labPanelStrDelta = 0;
            _labPanelAgiDelta = 0;
            _labPanelTecDelta = 0;
            _labPanelIntDelta = 0;
            _labPanelLevelDelta = 0;
            _labPanelArmorDelta = 0;
            _labPanelActionSlotDelta = 0;
        }

        /// <summary>Called from <see cref="ActionLabLeftPanelStatAdjustment"/> when the user changes a stat row.</summary>
        internal void RecordLabPanelStatDelta(string statKey, int delta)
        {
            if (delta == 0) return;
            switch (statKey)
            {
                case "str":
                case "damage":
                    _labPanelStrDelta += delta;
                    break;
                case "agi":
                case "speed":
                    _labPanelAgiDelta += delta;
                    break;
                case "tec":
                    _labPanelTecDelta += delta;
                    break;
                case "int":
                    _labPanelIntDelta += delta;
                    break;
                case "armor":
                    _labPanelArmorDelta += delta;
                    break;
                case "actionslots":
                    _labPanelActionSlotDelta += delta;
                    break;
            }
        }

        /// <summary>Called from <see cref="ActionLabLeftPanelStatAdjustment"/> when the user changes hero level.</summary>
        internal void RecordLabPanelLevelDelta(int delta)
        {
            if (delta != 0)
                _labPanelLevelDelta += delta;
        }

        /// <summary>Re-applies cumulative left-panel deltas onto the lab hero after cloning from <see cref="_initialPlayerJson"/>.</summary>
        private void ApplyLabPanelDeltasToLabHero()
        {
            LabCombatEntityFactory.ApplyPanelDeltas(
                _labPlayer,
                _labPanelLevelDelta,
                _labPanelStrDelta,
                _labPanelAgiDelta,
                _labPanelTecDelta,
                _labPanelIntDelta,
                _labPanelArmorDelta,
                _labPanelActionSlotDelta);
            SyncLabHeroFromTuning();
        }

        /// <summary>
        /// Recomputes lab hero max health from <see cref="GameConfiguration"/> (settings / balance patches).
        /// Does not reset base attributes so left-panel stat deltas are preserved.
        /// </summary>
        private void SyncLabHeroFromTuning()
        {
            PlayerTuningApplier.ApplyMaxHealthFromTuning(_labPlayer);
        }

        /// <summary>When Action Lab is active, reapplies combat tuning to the sandbox hero (e.g. after settings save).</summary>
        public static void ApplyTuningToActiveLabHeroIfAny()
        {
            if (_current == null)
                return;
            _current.SyncLabHeroFromTuning();
            _current._refreshCombatUi();
        }

        /// <summary>Replace the lab enemy from <see cref="EnemyLoader"/> data (level 1 by default). Clears step history.</summary>
        public void SetLabEnemyFromLoader(string enemyType, int level = 1)
        {
            if (string.IsNullOrWhiteSpace(enemyType))
                throw new ArgumentException("Enemy type is required.", nameof(enemyType));

            EnemyLoader.LoadEnemies();
            var created = EnemyLoader.CreateEnemy(enemyType.Trim(), level)
                ?? throw new InvalidOperationException($"Lab: could not create enemy '{enemyType}'.");

            _sessionEnemyLoaderType = enemyType.Trim();
            _labEnemyBaseLevel = Math.Clamp(level, 1, 99);
            _labPanelEnemyLevelDelta = 0;
            _labEnemy = created;
            _history.Clear();
            ResetSimulatedCombatTurnAccumulator();
            BootstrapCombatState();
            SyncCatalogSelectionToUpcomingActor();
            SyncLabEnemyToCanvasContext();
            _refreshCombatUi();
        }

        /// <summary>
        /// Action Lab: change enemy level from the right panel (rebuilds enemy, clears step history, re-bootstraps combat).
        /// </summary>
        public void ApplyLabEnemyLevelDelta(int delta)
        {
            if (delta == 0)
                return;
            int before = Math.Clamp(_labEnemyBaseLevel + _labPanelEnemyLevelDelta, 1, 99);
            int next = Math.Clamp(before + delta, 1, 99);
            if (next == before)
                return;
            _labPanelEnemyLevelDelta = next - _labEnemyBaseLevel;
            BuildLabEnemyFromPanelState();
            _history.Clear();
            ResetSimulatedCombatTurnAccumulator();
            BootstrapCombatState();
            SyncCatalogSelectionToUpcomingActor();
            SyncLabEnemyToCanvasContext();
            _refreshCombatUi();
        }

        private void BuildLabEnemyFromPanelState()
        {
            int effectiveLevel = Math.Clamp(_labEnemyBaseLevel + _labPanelEnemyLevelDelta, 1, 99);
            _labEnemy = LabCombatEntityFactory.BuildLabEnemy(
                _sessionEnemyLoaderType,
                effectiveLevel,
                LabCombatSnapshot.DefaultTestEnemyBattleConfig);
        }

        private void ApplyLabToCanvasContext(ICanvasContextManager ctx)
        {
            ctx.SetCurrentCharacter(_labPlayer);
            ctx.SetCurrentEnemy(_labEnemy);
            ctx.SetDungeonName("Action lab");
            ctx.SetRoomName(_labRoom.Name);
        }

        private void SyncLabEnemyToCanvasContext()
        {
            if (_restoreTarget == null) return;
            _restoreTarget.SetCurrentEnemy(_labEnemy);
        }

        /// <summary>Moves <see cref="LabPlayer"/> combo strip pointer by <paramref name="delta"/> slots; clamps to first/last slot (no wrap).</summary>
        public void NudgeLabPlayerComboStep(int delta)
        {
            if (delta == 0) return;
            var actions = _labPlayer.GetComboActions();
            if (actions.Count == 0) return;
            int n = actions.Count;
            int cycleBase = (_labPlayer.ComboStep / n) * n;
            int slot = _labPlayer.ComboStep - cycleBase;
            int newSlot = Math.Clamp(slot + delta, 0, n - 1);
            _labPlayer.ComboStep = cycleBase + newSlot;
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
        }

        /// <summary>Adds the current <see cref="SelectedCatalogActionName"/> to the lab hero combo strip (combo-eligible).</summary>
        public void AddSelectedCatalogActionToComboStrip()
        {
            if (string.IsNullOrWhiteSpace(SelectedCatalogActionName)) return;
            var poolEntry = _labPlayer.ActionPool.FirstOrDefault(item =>
                item.action != null && string.Equals(item.action.Name, SelectedCatalogActionName, StringComparison.OrdinalIgnoreCase));
            var action = poolEntry.action ?? ActionLoader.GetAction(SelectedCatalogActionName);
            if (action == null) return;
            if (!action.IsComboAction)
                action.IsComboAction = true;
            // Fresh ActionLoader instances keep JSON ComboOrder (often 0), which ReorderComboSequence sorts to the front.
            // Append at the end of the current strip, matching ReapplyComboStrip / RestoreComboFromActionNames.
            var existing = _labPlayer.GetComboActions();
            int nextSlot = existing.Count > 0 ? existing.Max(a => a.ComboOrder) + 1 : 1;
            action.ComboOrder = nextSlot;
            _labPlayer.AddToCombo(action);
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
        }

        /// <summary>
        /// Restores the lab hero combo strip after baseline replay from
        /// <see cref="_initialPlayerJson"/>, which does not include catalog/strip edits made during the session.
        /// Uses action pool entries when present, otherwise <see cref="ActionLoader"/> (same as catalog add).
        /// </summary>
        private void ReapplyLabHeroComboStrip(IReadOnlyList<string> orderedActionNames)
        {
            LabCombatEntityFactory.ReapplyComboStrip(_labPlayer, orderedActionNames, preferActionPool: true);
        }
    }
}
