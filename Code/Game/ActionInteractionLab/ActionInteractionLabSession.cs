using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.BattleStatistics;
using RPGGame.Entity.Services;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Sandbox combat session for stepped play with forced actions and fixed d20.
    /// Does not mutate the real save character; uses an in-memory clone for <see cref="LabPlayer"/>.
    /// </summary>
    public sealed class ActionInteractionLabSession
    {
        private static ActionInteractionLabSession? _current;

        /// <summary>Active session while in <see cref="GameState.ActionInteractionLab"/>.</summary>
        public static ActionInteractionLabSession? Current => _current;

        private static readonly BattleConfiguration LabEnemyConfig = new()
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

        private readonly CombatManager _combatManager;
        private readonly System.Action _refreshCombatUi;
        /// <summary>Clear/reseed center panel before rebuilding log during undo replay (optional).</summary>
        private readonly System.Action? _prepareLabHistoryReplay;
        private string _initialPlayerJson;
        private readonly List<LabStep> _history = new();
        /// <summary>Serializes Step / Undo / replay so two UI actions cannot interleave async combat (shared speed system).</summary>
        private readonly SemaphoreSlim _turnGate = new(1, 1);

        private Character _labPlayer = null!;
        private Enemy _labEnemy = null!;
        private Environment _labRoom = null!;

        /// <summary>When non-null, undo/replay restores this loader type instead of the default test dummy.</summary>
        private string? _sessionEnemyLoaderType;

        private ICanvasContextManager? _restoreTarget;
        private CanvasContextSnapshot? _contextSnapshot;

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

        /// <summary>Enemy level from the last loader pick (or 1 for the default lab dummy).</summary>
        private int _labEnemyBaseLevel = 1;

        /// <summary>Cumulative right-panel enemy level tweaks (effective = clamp(base + delta, 1–99)).</summary>
        private int _labPanelEnemyLevelDelta;

        public Character LabPlayer => _labPlayer;
        public Enemy LabEnemy => _labEnemy;
        public Environment LabRoom => _labRoom;

        /// <summary>GUI forward steps use no artificial block delays in the lab.</summary>
        public bool ZeroDisplayDelays { get; set; } = true;

        public bool IsReplayingHistory { get; private set; }

        /// <summary>True while the encounter batch simulator is running (UI shows a busy state).</summary>
        public bool IsEncounterSimulationRunning { get; private set; }

        /// <summary>Sets <see cref="IsEncounterSimulationRunning"/> (used by Action Lab tools UI).</summary>
        public void SetEncounterSimulationRunning(bool value) => IsEncounterSimulationRunning = value;

        public IReadOnlyList<LabStep> History => _history;

        /// <summary>Catalog action name for the next Step (full list from <see cref="ActionLoader"/>).</summary>
        public string SelectedCatalogActionName { get; set; } = "";

        /// <summary>Chosen natural d20 (1–20) used when <see cref="UseRandomD20PerStep"/> is false.</summary>
        public int SelectedD20 { get; set; } = 16;

        /// <summary>When true, each Step uses a fresh random 1–20; when false, <see cref="SelectedD20"/> is used.</summary>
        public bool UseRandomD20PerStep { get; set; }

        /// <summary>Returns the d20 value for the next Step (random or fixed per <see cref="UseRandomD20PerStep"/>).</summary>
        public int ResolveD20ForNextStep() =>
            UseRandomD20PerStep ? Random.Shared.Next(1, 21) : SelectedD20;

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

        private ActionInteractionLabSession(
            CombatManager combatManager,
            Character labPlayer,
            Enemy labEnemy,
            Environment labRoom,
            string initialPlayerJson,
            System.Action refreshCombatUi,
            System.Action? prepareLabHistoryReplay)
        {
            _combatManager = combatManager;
            _labPlayer = labPlayer;
            _labEnemy = labEnemy;
            _labRoom = labRoom;
            _initialPlayerJson = initialPlayerJson;
            _refreshCombatUi = refreshCombatUi;
            _prepareLabHistoryReplay = prepareLabHistoryReplay;
        }

        /// <summary>Begins a lab fight from the active character (cloned). Call from UI thread.</summary>
        /// <param name="canvasContext">When set, snapshots and replaces canvas context for combat routing; restored in <see cref="EndSession"/>.</param>
        /// <param name="prepareLabHistoryReplay">Optional: clear center panel and lab intro line before undo replay rebuilds the combat log.</param>
        public static ActionInteractionLabSession Begin(
            Character activePlayer,
            CombatManager combatManager,
            System.Action refreshCombatUi,
            ICanvasContextManager? canvasContext = null,
            System.Action? prepareLabHistoryReplay = null)
        {
            EndSession();

            ActionLoader.ReloadActions();
            var serializer = new CharacterSerializer();
            string json = serializer.Serialize(activePlayer);
            var data = serializer.Deserialize(json) ?? throw new InvalidOperationException("Lab: failed to deserialize character clone payload.");
            var labPlayer = serializer.CreateCharacterFromSaveData(data);

            var labEnemy = TestCharacterFactory.CreateTestEnemy(LabEnemyConfig, 0, level: 1);
            var labRoom = TestCharacterFactory.CreateTestEnvironment();

            var session = new ActionInteractionLabSession(combatManager, labPlayer, labEnemy, labRoom, json, refreshCombatUi, prepareLabHistoryReplay);
            _current = session;
            session._sessionEnemyLoaderType = null;
            session._labEnemyBaseLevel = 1;
            session._labPanelEnemyLevelDelta = 0;
            var names = ActionLoader.GetAllActionNames().OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            if (names.Count > 0)
                session.SelectedCatalogActionName = names[0];
            session.SelectedD20 = 16;
            session.UseRandomD20PerStep = false;
            session.CatalogScrollOffset = 0;
            session.EnemyCatalogScrollOffset = 0;
            TryBeginWithRandomCatalogEnemy(session);
            session.EncounterSimulationBatchCount = ActionLabEncounterSimulator.DefaultBatchEncounterCount;
            session._restoreTarget = canvasContext;
            if (canvasContext != null)
            {
                session._contextSnapshot = CanvasContextSnapshot.Capture(canvasContext);
                session.ApplyLabToCanvasContext(canvasContext);
            }
            session.BootstrapCombatState();
            session.SyncCatalogSelectionToUpcomingActor();
            refreshCombatUi();
            return session;
        }

        /// <summary>
        /// When <see cref="EnemyLoader"/> has at least one enemy type, replaces the default lab dummy with a random
        /// level-1 loader enemy and scrolls the enemy type list so that type is visible.
        /// </summary>
        private static void TryBeginWithRandomCatalogEnemy(ActionInteractionLabSession session)
        {
            EnemyLoader.LoadEnemies();
            var enemyTypes = EnemyLoader.GetAllEnemyTypes();
            if (enemyTypes.Count == 0)
                return;

            enemyTypes.Sort(StringComparer.OrdinalIgnoreCase);
            int pickIdx = Random.Shared.Next(enemyTypes.Count);
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
        /// Equips lab gear for <paramref name="slot"/> (<c>weapon</c>, <c>head</c>, <c>body</c>, <c>feet</c>), clears step history, and re-bases undo snapshot.
        /// </summary>
        public void ApplyLabGear(Item item, string slot)
        {
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
                case "feet":
                    if (item is not FeetItem)
                        throw new ArgumentException("Item must be a FeetItem for feet slot.", nameof(item));
                    break;
                default:
                    throw new ArgumentException("Slot must be weapon, head, body, or feet.", nameof(slot));
            }

            _labPlayer.EquipItem(item, s);
            _history.Clear();
            ResetLabPanelDeltas();
            BootstrapCombatState();
            var serializer = new CharacterSerializer();
            _initialPlayerJson = serializer.Serialize(_labPlayer);
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
        }

        /// <summary>
        /// Unequips the lab hero in <paramref name="slot"/> (<c>weapon</c>, <c>head</c>, <c>body</c>, <c>feet</c>),
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
                case "feet":
                    _labPlayer.UnequipItem(s);
                    break;
                default:
                    throw new ArgumentException("Slot must be weapon, head, body, or feet.", nameof(slot));
            }

            _history.Clear();
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
            if (_labPanelLevelDelta != 0)
                _labPlayer.ApplyActionLabLevelDelta(_labPanelLevelDelta);
            if (_labPanelStrDelta != 0)
                _labPlayer.Stats.Strength = Math.Max(1, _labPlayer.Stats.Strength + _labPanelStrDelta);
            if (_labPanelAgiDelta != 0)
                _labPlayer.Stats.Agility = Math.Max(1, _labPlayer.Stats.Agility + _labPanelAgiDelta);
            if (_labPanelTecDelta != 0)
                _labPlayer.Stats.Technique = Math.Max(1, _labPlayer.Stats.Technique + _labPanelTecDelta);
            if (_labPanelIntDelta != 0)
                _labPlayer.Stats.Intelligence = Math.Max(1, _labPlayer.Stats.Intelligence + _labPanelIntDelta);
            if (_labPanelArmorDelta != 0)
                _labPlayer.ActionLabArmorBonus = Math.Max(0, _labPlayer.ActionLabArmorBonus + _labPanelArmorDelta);
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
            BootstrapCombatState();
            SyncCatalogSelectionToUpcomingActor();
            SyncLabEnemyToCanvasContext();
            _refreshCombatUi();
        }

        private void BuildLabEnemyFromPanelState()
        {
            int effectiveLevel = Math.Clamp(_labEnemyBaseLevel + _labPanelEnemyLevelDelta, 1, 99);
            if (!string.IsNullOrEmpty(_sessionEnemyLoaderType))
            {
                EnemyLoader.LoadEnemies();
                _labEnemy = EnemyLoader.CreateEnemy(_sessionEnemyLoaderType, effectiveLevel)
                    ?? TestCharacterFactory.CreateTestEnemy(LabEnemyConfig, 0, effectiveLevel);
            }
            else
                _labEnemy = TestCharacterFactory.CreateTestEnemy(LabEnemyConfig, 0, effectiveLevel);
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

        private sealed class CanvasContextSnapshot
        {
            public Character? Character { get; init; }
            public Enemy? Enemy { get; init; }
            public string? DungeonName { get; init; }
            public string? RoomName { get; init; }

            public static CanvasContextSnapshot Capture(ICanvasContextManager ctx) => new()
            {
                Character = ctx.GetCurrentCharacter(),
                Enemy = ctx.GetCurrentEnemy(),
                DungeonName = ctx.GetDungeonName(),
                RoomName = ctx.GetRoomName(),
            };

            public void Restore(ICanvasContextManager ctx)
            {
                ctx.SetCurrentCharacter(Character);
                if (Enemy != null)
                    ctx.SetCurrentEnemy(Enemy);
                else
                    ctx.ClearCurrentEnemy();
                ctx.SetDungeonName(DungeonName);
                ctx.SetRoomName(RoomName);
            }
        }

        /// <summary>
        /// Aligns the catalog pick with the lab hero's combo strip and <see cref="Character.ComboStep"/> so the next Step
        /// uses the correct slot in the player's sequence (including after the enemy's turn when the enemy has its own strip).
        /// </summary>
        public void SyncCatalogSelectionToUpcomingActor()
        {
            var name = ActionLabCatalogSync.ComputeSelectedCatalogName(GetNextActorToAct(), _labPlayer, _labEnemy);
            if (!string.IsNullOrEmpty(name))
                SelectedCatalogActionName = name;
        }

        /// <summary>Resets combo step for both lab participants and refreshes catalog selection. Does not clear action bonuses.</summary>
        public void ResetLabCombo()
        {
            _labPlayer.ComboStep = 0;
            _labEnemy.ComboStep = 0;
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
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
            var action = ActionLoader.GetAction(SelectedCatalogActionName);
            if (action == null) return;
            if (!action.IsComboAction)
                action.IsComboAction = true;
            _labPlayer.AddToCombo(action);
            SyncCatalogSelectionToUpcomingActor();
            _refreshCombatUi();
        }

        private void BootstrapCombatState()
        {
            GameTicker.Instance.Reset();
            _labPlayer.ComboStep = 0;
            _labEnemy.ComboStep = 0;
            _combatManager.StartBattleNarrative(_labPlayer.Name, _labEnemy.Name, _labRoom.Name, _labPlayer.CurrentHealth, _labEnemy.CurrentHealth);
            _combatManager.InitializeCombatEntities(_labPlayer, _labEnemy, _labRoom, playerGetsFirstAttack: true, enemyGetsFirstAttack: false);
            _labRoom.ResetForNewFight();
            ActionSelector.ClearStoredRolls();
            Dice.ClearTestRoll();
        }

        private Character ClonePlayerFromInitialJson()
        {
            var serializer = new CharacterSerializer();
            var data = serializer.Deserialize(_initialPlayerJson) ?? throw new InvalidOperationException("Lab: invalid initial player json.");
            return serializer.CreateCharacterFromSaveData(data);
        }

        /// <summary>Replaces lab entities from <see cref="_initialPlayerJson"/> and enemy baseline; does not bootstrap combat.</summary>
        private void RestoreLabEntitiesFromInitialBaseline()
        {
            _labPlayer = ClonePlayerFromInitialJson();
            BuildLabEnemyFromPanelState();
            _labRoom = TestCharacterFactory.CreateTestEnvironment();
        }

        /// <summary>
        /// Restores the lab hero combo strip after baseline replay from
        /// <see cref="_initialPlayerJson"/>, which does not include catalog/strip edits made during the session.
        /// Uses action pool entries when present, otherwise <see cref="ActionLoader"/> (same as catalog add).
        /// </summary>
        private void ReapplyLabHeroComboStrip(IReadOnlyList<string> orderedActionNames)
        {
            foreach (var a in _labPlayer.GetComboActions().ToList())
                _labPlayer.RemoveFromCombo(a);

            int nextSlot = 1;
            foreach (var name in orderedActionNames)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;
                var poolEntry = _labPlayer.ActionPool.FirstOrDefault(item =>
                    item.action != null && string.Equals(item.action.Name, name, StringComparison.OrdinalIgnoreCase));
                Action? act = poolEntry.action;
                if (act == null)
                    act = ActionLoader.GetAction(name);
                if (act == null)
                    continue;
                if (!act.IsComboAction)
                    act.IsComboAction = true;
                // Match <see cref="ComboSequenceManager.RestoreComboFromActionNames"/> so opener/middle ordering matches snapshot order.
                act.ComboOrder = nextSlot++;
                _labPlayer.AddToCombo(act);
            }
        }

        /// <summary>Replay history to match <paramref name="steps"/> after undo. Clears center log, re-runs turns with UI to rebuild it.</summary>
        public async Task ReplayHistoryAsync(IReadOnlyList<LabStep> steps)
        {
            _prepareLabHistoryReplay?.Invoke();

            bool prevUi = CombatManager.DisableCombatUIOutput;
            IsReplayingHistory = true;
            // Allow combat UI while replaying so the center log matches state. (DisableCombatUIOutput was not
            // honored by all display paths, which left stale lines and appended duplicate blocks.)
            CombatManager.DisableCombatUIOutput = false;
            try
            {
                // Strip edits (catalog add/remove/reorder) live only on the in-memory lab clone; the baseline JSON
                // is from session start (or gear change). Preserve the current strip across entity restore.
                var comboSnapshot = _labPlayer.GetComboActions().Select(a => a.Name).ToList();
                RestoreLabEntitiesFromInitialBaseline();
                ApplyLabPanelDeltasToLabHero();
                BootstrapCombatState();
                ReapplyLabHeroComboStrip(comboSnapshot);
                foreach (var step in steps)
                {
                    await ExecuteOneStepCoreAsync(step.D20, step.ForcedActionName, silent: false).ConfigureAwait(true);
                }
            }
            finally
            {
                CombatManager.DisableCombatUIOutput = prevUi;
                IsReplayingHistory = false;
                SyncCatalogSelectionToUpcomingActor();
                _refreshCombatUi();
            }
        }

        /// <summary>
        /// Captures hero baseline JSON, lab stat deltas, enemy loader selection, combo strip, and catalog pick
        /// for <see cref="ActionLabEncounterSimulator"/> without mutating session entities.
        /// </summary>
        public LabCombatSnapshot CaptureSimulationSnapshot()
        {
            var strip = _labPlayer.GetComboActions().Select(a => a.Name).ToList();
            return new LabCombatSnapshot(
                _initialPlayerJson,
                _labPanelStrDelta,
                _labPanelAgiDelta,
                _labPanelTecDelta,
                _labPanelIntDelta,
                _labPanelLevelDelta,
                _labPanelArmorDelta,
                _sessionEnemyLoaderType,
                enemyLevel: Math.Clamp(_labEnemy.Level, 1, 99),
                strip,
                SelectedCatalogActionName ?? "");
        }

        /// <summary>Who will act next (peek). Null if the speed system must advance time first.</summary>
        public Actor? GetNextActorToAct() => _combatManager.GetNextEntityToAct();

        /// <summary>Runs one lab turn with the chosen catalog action name and d20.</summary>
        public async Task<CombatSingleTurnResult> StepAsync(int d20, string forcedActionName)
        {
            await _turnGate.WaitAsync().ConfigureAwait(true);
            try
            {
                if (d20 < 1 || d20 > 20)
                    throw new ArgumentOutOfRangeException(nameof(d20), "d20 must be 1–20.");

                var step = new LabStep(d20, forcedActionName);
                var result = await ExecuteOneStepCoreAsync(step.D20, step.ForcedActionName, silent: false).ConfigureAwait(true);
                if (result == CombatSingleTurnResult.Advanced
                    || result == CombatSingleTurnResult.PlayerDefeated
                    || result == CombatSingleTurnResult.EnemyDefeated)
                {
                    _history.Add(step);
                }
                SyncCatalogSelectionToUpcomingActor();
                _refreshCombatUi();
                return result;
            }
            finally
            {
                _turnGate.Release();
            }
        }

        /// <summary>
        /// Note: <see cref="Dice.SetTestRoll"/> forces the same value for every <see cref="Dice.Roll"/> in the turn
        /// (e.g. secondary random picks). Primary d20 selection is the intended use.
        /// </summary>
        private async Task<CombatSingleTurnResult> ExecuteOneStepCoreAsync(int d20, string forcedActionName, bool silent)
        {
            var forced = ActionLoader.GetAction(forcedActionName);
            if (forced == null)
                throw new InvalidOperationException($"Lab: unknown action '{forcedActionName}'.");

            // Match catalog click behavior (MouseInteractionHandler lab_act): forced steps must be combo-eligible
            // so roll threshold, ONCOMBO effects, and amplification use the same rules as the strip.
            if (!forced.IsComboAction)
                forced.IsComboAction = true;

            Dice.SetTestRoll(d20);
            ActionSelector.SetStoredActionRoll(_labPlayer, d20);
            ActionSelector.SetStoredActionRoll(_labEnemy, d20);

            bool prevUiOut = CombatManager.DisableCombatUIOutput;
            if (silent)
                CombatManager.DisableCombatUIOutput = true;

            try
            {
                // Only force the catalog combo action when this d20 would select a combo (same as main combat).
                // Otherwise pass null so ActionSelector picks the unnamed normal attack — avoids "RAGE" etc. on low rolls.
                Action? forcedForPlayer = ActionSelector.WouldNaturalRollSelectComboAction(_labPlayer, d20)
                    ? forced
                    : null;
                return await _combatManager.AdvanceSingleTurnAsync(_labPlayer, _labEnemy, _labRoom, forcedForPlayer).ConfigureAwait(true);
            }
            finally
            {
                if (silent)
                    CombatManager.DisableCombatUIOutput = prevUiOut;
                Dice.ClearTestRoll();
            }
        }

        /// <summary>Removes the last step and restores state by replaying the remaining prefix.</summary>
        public async Task UndoLastStepAsync()
        {
            await _turnGate.WaitAsync().ConfigureAwait(true);
            try
            {
                if (_history.Count == 0)
                    return;
                _history.RemoveAt(_history.Count - 1);
                await ReplayHistoryAsync(_history).ConfigureAwait(true);
            }
            finally
            {
                _turnGate.Release();
            }
        }

        public static void EndSession()
        {
            if (_current == null)
                return;
            var session = _current;
            try
            {
                session._combatManager.EndBattleNarrative(session._labPlayer, session._labEnemy);
            }
            catch
            {
                // best-effort cleanup
            }
            Dice.ClearTestRoll();
            ActionSelector.ClearStoredRolls();
            var restore = session._restoreTarget;
            var snap = session._contextSnapshot;
            session.SetEncounterSimulationRunning(false);
            _current = null;
            if (restore != null && snap != null)
            {
                try
                {
                    snap.Restore(restore);
                }
                catch
                {
                    /* best-effort */
                }
            }
        }
    }
}
