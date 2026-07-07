using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.BattleStatistics;
using RPGGame.Data;
using RPGGame.Entity.Services;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.ActionInteractionLab
{
    /// <summary>
    /// Sandbox combat session for stepped play with forced actions and fixed d20.
    /// Does not mutate the real save character; uses an in-memory clone for <see cref="LabPlayer"/>.
    /// </summary>
    public sealed partial class ActionInteractionLabSession
    {
        private static ActionInteractionLabSession? _current;

        /// <summary>Active session while in <see cref="GameState.ActionInteractionLab"/>.</summary>
        public static ActionInteractionLabSession? Current => _current;

        private readonly CombatManager _combatManager;
        private readonly System.Action _refreshCombatUi;
        /// <summary>Clear/reseed center panel before rebuilding log during undo replay (optional).</summary>
        private readonly System.Action? _prepareLabHistoryReplay;
        private string _initialPlayerJson;
        private readonly List<LabStep> _history = new();
        /// <summary>Sum of per-encounter turn counts from completed batch simulations (off-thread fights).</summary>
        private int _simulatedCombatTurnAccumulator;
        /// <summary>Serializes Step / Undo / replay so two UI actions cannot interleave async combat (shared speed system).</summary>
        private readonly SemaphoreSlim _turnGate = new(1, 1);

        private Character _labPlayer = null!;
        private Enemy _labEnemy = null!;
        private Environment _labRoom = null!;

        /// <summary>When non-null, undo/replay restores this loader type instead of the default test dummy.</summary>
        private string? _sessionEnemyLoaderType;

        private ICanvasContextManager? _restoreTarget;
        private CanvasContextSnapshot? _contextSnapshot;

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

        /// <summary>
        /// Lab tools panel counter: interactive <see cref="StepAsync"/> advances match <see cref="History"/> length;
        /// each completed encounter batch adds that encounter's resolved turn count (see <see cref="RecordEncounterSimulationTurns"/>).
        /// Resets when lab step history is cleared (gear, enemy swap, enemy level rebuild, or <see cref="ResetLabEncounterAsync"/>).
        /// </summary>
        public int LabTotalActionTicks => _history.Count + _simulatedCombatTurnAccumulator;

        /// <summary>Adds simulated encounter turn totals after <see cref="ActionLabEncounterSimulator.RunBatchAsync"/> completes.</summary>
        public void RecordEncounterSimulationTurns(ActionLabEncounterSimulationReport report)
        {
            if (report == null)
                return;
            foreach (var e in report.Encounters)
                _simulatedCombatTurnAccumulator += e.Turns;
        }

        private void ResetSimulatedCombatTurnAccumulator() => _simulatedCombatTurnAccumulator = 0;

        /// <summary>Catalog action name for the next Step (full list from <see cref="ActionLoader"/>).</summary>
        public string SelectedCatalogActionName { get; set; } = "";

        /// <summary>Chosen natural d20 (1–20) used when <see cref="UseRandomD20PerStep"/> is false.</summary>
        public int SelectedD20 { get; set; } = 16;

        /// <summary>When true, each Step uses a fresh random 1–20; when false, <see cref="SelectedD20"/> is used.</summary>
        public bool UseRandomD20PerStep { get; set; }

        /// <summary>Returns the d20 value for the next Step (random or fixed per <see cref="UseRandomD20PerStep"/>).</summary>
        public int ResolveD20ForNextStep() =>
            UseRandomD20PerStep ? Random.Shared.Next(1, 21) : SelectedD20;

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

            var labEnemy = TestCharacterFactory.CreateTestEnemy(LabCombatSnapshot.DefaultTestEnemyBattleConfig, 0, level: 1);
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
            TryBeginWithDefaultCatalogEnemy(session);
            session.EncounterSimulationBatchCount = ActionLabEncounterSimulator.DefaultBatchEncounterCount;
            session.ResetSimulatedCombatTurnAccumulator();
            session.SyncLabHeroFromTuning();
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
        /// Aligns the catalog pick with the lab hero's combo strip and <see cref="Character.ComboStep"/> so the next Step
        /// uses the correct slot in the player's sequence (including after the enemy's turn when the enemy has its own strip).
        /// </summary>
        public void SyncCatalogSelectionToUpcomingActor()
        {
            var name = ActionLabCatalogSync.ComputeSelectedCatalogName(GetNextActorToAct(), _labPlayer, _labEnemy);
            if (!string.IsNullOrEmpty(name))
                SelectedCatalogActionName = name;
        }

        /// <summary>
        /// Action Lab Reset control: clears the center combat log and step/sim tick counters, restores full HP on both
        /// fighters, clears status/temp combat effects, and returns both combo steps to the start of their strips.
        /// Preserves equipped items, combo strip contents, and the current lab enemy (identity/level).
        /// </summary>
        public async Task ResetLabEncounterAsync()
        {
            await _turnGate.WaitAsync().ConfigureAwait(true);
            try
            {
                ResetLabEncounterCore();
            }
            finally
            {
                _turnGate.Release();
            }
        }

        /// <summary>
        /// Reloads game data from disk (actions, enemies, tuning config), rebuilds lab entities with fresh definitions,
        /// and resets the current encounter (clears step history). Preserves combo strip order, gear, and lab panel deltas.
        /// </summary>
        public async Task RefreshGameDataAsync()
        {
            await _turnGate.WaitAsync().ConfigureAwait(true);
            try
            {
                // Catalog/strip edits live on the in-memory lab clone; RebuildCharacterActions only restores
                // combo slots that exist in the action pool with IsComboAction. ReapplyLabHeroComboStrip
                // matches ReplayHistoryAsync and resolves names from pool or ActionLoader.
                var comboSnapshot = _labPlayer.GetComboActions().Select(a => a.Name).ToList();
                ReloadLabGameDataFromDisk();
                CharacterSerializer.RebuildCharacterActions(_labPlayer, preserveComboSequence: false);
                ReapplyLabHeroComboStrip(comboSnapshot);
                ApplyLabPanelDeltasToLabHero();
                BuildLabEnemyFromPanelState();
                SyncLabEnemyToCanvasContext();
                EnsureValidCatalogSelection();
                ResetLabEncounterCore();
            }
            finally
            {
                _turnGate.Release();
            }
        }

        private static void ReloadLabGameDataFromDisk()
        {
            ActionLoader.ReloadActions();
            GameDataSheetsPullService.ReloadRuntimeCachesAfterPull();
            try
            {
                GameConfiguration.Instance.Reload();
            }
            catch
            {
                // best-effort: tuning file may be missing or invalid during dev
            }
        }

        private void EnsureValidCatalogSelection()
        {
            var names = ActionLoader.GetAllActionNames();
            if (names.Count == 0)
            {
                SelectedCatalogActionName = "";
                return;
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(SelectedCatalogActionName)
                || !names.Any(n => string.Equals(n, SelectedCatalogActionName, StringComparison.OrdinalIgnoreCase)))
            {
                SelectedCatalogActionName = names[0];
            }
        }

        private void ResetLabEncounterCore()
        {
            _history.Clear();
            ResetSimulatedCombatTurnAccumulator();
            _prepareLabHistoryReplay?.Invoke();

            _labPlayer.Facade.ClearAllTempEffects();
            _labEnemy.Facade.ClearAllTempEffects();

            _labPlayer.CurrentHealth = _labPlayer.GetEffectiveMaxHealth();
            _labEnemy.CurrentHealth = _labEnemy.GetEffectiveMaxHealth();

            GameTicker.Instance.Reset();
            _combatManager.StartBattleNarrative(
                _labPlayer.Name,
                _labEnemy.Name,
                _labRoom.Name,
                _labPlayer.CurrentHealth,
                _labEnemy.CurrentHealth);
            _combatManager.InitializeCombatEntities(_labPlayer, _labEnemy, _labRoom, playerGetsFirstAttack: true, enemyGetsFirstAttack: false);
            _labRoom.ResetForNewFight();
            ActionSelector.ClearStoredRolls();
            Dice.ClearTestRoll();

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

        private Character ClonePlayerFromInitialJson() => LabCombatEntityFactory.ClonePlayerFromJson(_initialPlayerJson);

        /// <summary>Replaces lab entities from <see cref="_initialPlayerJson"/> and enemy baseline; does not bootstrap combat.</summary>
        private void RestoreLabEntitiesFromInitialBaseline()
        {
            _labPlayer = ClonePlayerFromInitialJson();
            BuildLabEnemyFromPanelState();
            _labRoom = TestCharacterFactory.CreateTestEnvironment();
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
                _labPanelActionSlotDelta,
                _sessionEnemyLoaderType,
                enemyLevel: Math.Clamp(_labEnemy.Level, 1, 99),
                strip,
                SelectedCatalogActionName ?? "");
        }

        /// <summary>Who will act next (peek). Null if the speed system must advance time first.</summary>
        public Actor? GetNextActorToAct() => _combatManager.GetNextEntityToAct();

        /// <summary>
        /// False once either fighter dies in the lab combat log, while history is replaying, or during batch sim.
        /// Use <see cref="UndoLastStepAsync"/> or <see cref="ResetLabEncounterAsync"/> to step again.
        /// </summary>
        public bool CanStepForward =>
            !IsReplayingHistory
            && !IsEncounterSimulationRunning
            && _labPlayer.IsAlive
            && _labEnemy.IsAlive;

        /// <summary>Runs one lab turn with the chosen catalog action name and d20.</summary>
        public async Task<CombatSingleTurnResult> StepAsync(int d20, string forcedActionName)
        {
            await _turnGate.WaitAsync().ConfigureAwait(true);
            try
            {
                if (!CanStepForward)
                {
                    if (!_labPlayer.IsAlive)
                        return CombatSingleTurnResult.PlayerDefeated;
                    if (!_labEnemy.IsAlive)
                        return CombatSingleTurnResult.EnemyDefeated;
                    return CombatSingleTurnResult.Advanced;
                }

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
        /// Note: <see cref="Dice.SetTestRoll"/> forces the primary d20 for action selection and the first die
        /// in 2d20 luck/unluck; the second 2d20 die uses <see cref="Dice.RollUnforced"/> so Action Lab picks
        /// do not duplicate as 4/4. Other <see cref="Dice.Roll"/> calls in the turn may still use the test value.
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
