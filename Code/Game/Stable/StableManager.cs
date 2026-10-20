namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using RPGGame.Combat;
    using RPGGame.Utils;

    /// <summary>
    /// The Stable: several saved heroes running dungeons at once, each in its own headless <see cref="GameCoordinator"/>.
    /// Fighters pause and raise <see cref="Changed"/> whenever the player has a call to make (pick a dungeon, continue
    /// or leave between rooms, acknowledge a death).
    ///
    /// Each fighter's async flow is scoped so the live game is untouched: its UI manager is the fighter's own
    /// capture (<see cref="UIManager.BeginScopedUIManager"/>), combat UI and pacing delays are muted
    /// (<see cref="CombatUiMuteScope"/>), and the between-rooms prompt waits on the Stable
    /// (<see cref="DungeonExitChoiceHandler.BeginScopedExitDecider"/>). Call from the UI thread so fighter turns
    /// interleave with the live game there; combat itself already runs on the thread pool, as in a normal run.
    /// </summary>
    public sealed class StableManager
    {
        public const int MaxFighters = 9;

        public static StableManager Instance { get; } = new();

        private readonly List<StableFighter> fighters = new();

        // Read by combat-bus handlers on the thread pool; replaced (never mutated) whenever the roster changes.
        private volatile StableFighter[] roster = System.Array.Empty<StableFighter>();

        /// <summary>Pause after each room so progress is watchable (ms). 0 in tests.</summary>
        public int RoomPaceMs { get; set; } = 1200;

        /// <summary>
        /// When true (default), fighters take as long as a watched fight: the normal per-action and room delays run
        /// even though their combat text is never shown. Off in tests.
        /// </summary>
        public bool PaceCombat { get; set; } = true;

        /// <summary>Pause at the hub before "keep going" starts the next dungeon (ms).</summary>
        public int KeepGoingPauseMs { get; set; } = 2500;

        /// <summary>Auto-continue keeps going between rooms only at or above this HP %.</summary>
        public int AutoContinueMinHpPercent { get; set; } = 50;

        /// <summary>Raised after any fighter changes (status, room, outcome). May fire off the UI thread.</summary>
        public event System.Action? Changed;

        public IReadOnlyList<StableFighter> Fighters => fighters;

        public int DecisionCount => fighters.Count(f => f.NeedsDecision);

        public StableFighter? Find(string characterId) =>
            fighters.FirstOrDefault(f => string.Equals(f.CharacterId, characterId, StringComparison.Ordinal));

        public StableFighter? Find(Character character) =>
            fighters.FirstOrDefault(f => ReferenceEquals(f.Character, character));

        /// <summary>Loads a save into the Stable. Returns null (with a reason) if it cannot join.</summary>
        public async Task<(StableFighter? fighter, string? error)> AddFromSaveAsync(string characterId)
        {
            if (Find(characterId) != null)
                return (null, "That hero is already in the Stable.");
            if (fighters.Count >= MaxFighters)
                return (null, $"The Stable is full ({MaxFighters} fighters).");

            bool isLegacy = characterId.EndsWith("_legacy", StringComparison.Ordinal);
            var character = isLegacy
                ? await Character.LoadCharacterAsync()
                : await Character.LoadCharacterAsync(characterId);
            if (character == null)
                return (null, "Could not load that save.");

            var settings = GameSettings.Instance;
            if (settings.PlayerHealthMultiplier != 1.0)
                character.ApplyHealthMultiplier(settings.PlayerHealthMultiplier);

            // Legacy single-file saves get a fresh ID and migrate to a per-character file on first save.
            return (Add(character, isLegacy ? null : characterId), null);
        }

        /// <summary>
        /// Adds a hero that is already in memory (e.g. one just created in the main game). A null ID gets a fresh one.
        /// </summary>
        public StableFighter Add(Character character, string? characterId)
        {
            var existing = Find(character) ?? (characterId == null ? null : Find(characterId));
            if (existing != null)
                return existing;

            var output = new RPGGame.MCP.OutputCapture();
            var game = new GameCoordinator(character, characterId, output);
            var fighter = new StableFighter(character, game.StateManager.GetCharacterId(character) ?? characterId ?? "", output)
            {
                Game = game,
                AutoContinue = true
            };
            fighter.AddLog($"Joined the Stable at level {character.Level}.");
            fighters.Add(fighter);
            RosterChanged();
            PrepareHub(fighter);
            RaiseChanged();
            return fighter;
        }

        /// <summary>Dungeons the fighter can enter now (utility rows such as custom level are left out).</summary>
        public List<Dungeon> GetDungeonChoices(StableFighter fighter) =>
            fighter.Game?.AvailableDungeons
                .Where(d => !GameConstants.IsDungeonSelectionUtilityOption(d.Name))
                .ToList()
            ?? new List<Dungeon>();

        /// <summary>
        /// The dungeon "Go" picks: the last one run if it is still offered, else the hardest one at or below the hero's level.
        /// </summary>
        public Dungeon? GetQuickDungeon(StableFighter fighter)
        {
            var choices = GetDungeonChoices(fighter);
            if (choices.Count == 0)
                return null;
            var repeat = choices.FirstOrDefault(d => d.Name == fighter.LastDungeonName);
            if (repeat != null)
                return repeat;
            int level = fighter.Character.Level;
            return choices.Where(d => d.MinLevel <= level).OrderByDescending(d => d.MinLevel).FirstOrDefault()
                ?? choices.OrderBy(d => d.MinLevel).First();
        }

        /// <summary>Starts <see cref="GetQuickDungeon"/>.</summary>
        public bool Go(StableFighter fighter) =>
            GetQuickDungeon(fighter) is { } dungeon && StartDungeon(fighter, dungeon);

        public void SetKeepGoing(StableFighter fighter, bool on)
        {
            fighter.KeepGoing = on;
            RaiseChanged();
            if (on && fighter.Status == StableFighterStatus.AtHub)
                Go(fighter);
        }

        /// <summary>Dev: brings a dead fighter back at full health with no penalty and re-saves it as living.</summary>
        public async Task<bool> ResurrectAsync(StableFighter fighter)
        {
            if (fighter.Status != StableFighterStatus.Dead || fighter.Game == null)
                return false;
            var state = fighter.Game.StateManager;
            state.SetCurrentDungeon(null);
            state.SetCurrentRoom(null);
            CharacterResurrectionService.ResurrectDevNoPenalty(fighter.Character);
            state.SetCurrentPlayer(fighter.Character);
            await SaveAsync(fighter);
            fighter.Status = StableFighterStatus.AtHub;
            fighter.LastResult = "Resurrected [Dev].";
            fighter.AddLog(fighter.LastResult);
            PrepareHub(fighter);
            RaiseChanged();
            return true;
        }

        /// <summary>Sends the fighter into one of <see cref="GetDungeonChoices"/>. Returns false if it cannot go now.</summary>
        public bool StartDungeon(StableFighter fighter, Dungeon dungeon)
        {
            if (fighter.Status != StableFighterStatus.AtHub || fighter.Game == null)
                return false;
            int menuIndex = fighter.Game.AvailableDungeons.IndexOf(dungeon);
            if (menuIndex < 0)
                return false;

            fighter.Status = StableFighterStatus.Exploring;
            fighter.DungeonName = dungeon.Name;
            fighter.LastDungeonName = dungeon.Name;
            fighter.DungeonLevel = dungeon.MinLevel;
            fighter.LeftByChoice = false;
            fighter.RoomsCleared = 0;
            fighter.TotalRooms = 0;
            fighter.LastResult = null;
            fighter.AddLog($"Entered {dungeon.Name} (L{dungeon.MinLevel}).");
            RaiseChanged();

            EnsureFeedSubscribed();
            _ = RunDungeonAsync(fighter, menuIndex);
            return true;
        }

        /// <summary>Answers the between-rooms prompt.</summary>
        public void AnswerExit(StableFighter fighter, bool leave)
        {
            var pending = fighter.PendingExit;
            if (fighter.Status != StableFighterStatus.AwaitingExit || pending == null)
                return;
            fighter.PendingExit = null;
            fighter.Status = StableFighterStatus.Exploring;
            fighter.LeftByChoice = leave;
            fighter.AddLog(leave ? "Leaving the dungeon." : "Pressing on.");
            RaiseChanged();
            pending.TrySetResult(leave);
        }

        /// <summary>Saves (if alive) and removes the fighter. Not allowed mid-dungeon.</summary>
        public async Task<bool> RemoveAsync(StableFighter fighter)
        {
            if (fighter.IsBusy || !fighters.Contains(fighter))
                return false;
            if (fighter.Status != StableFighterStatus.Dead && fighter.Status != StableFighterStatus.InHand)
                await SaveAsync(fighter);
            fighters.Remove(fighter);
            RosterChanged();
            fighter.Game = null;
            RaiseChanged();
            return true;
        }

        /// <summary>
        /// Hands an idle fighter to the main game for direct play. The fighter shows as "in hand" until
        /// <see cref="ReturnFromHand"/>.
        /// </summary>
        public bool CheckOut(StableFighter fighter)
        {
            if (fighter.Status != StableFighterStatus.AtHub)
                return false;
            fighter.Status = StableFighterStatus.InHand;
            fighter.AddLog("Taken in hand.");
            RaiseChanged();
            return true;
        }

        /// <summary>Back from direct play: dead heroes stay dead, others return to the hub with a fresh dungeon list.</summary>
        public void ReturnFromHand(StableFighter fighter)
        {
            if (fighter.Status != StableFighterStatus.InHand)
                return;
            if (!fighter.Character.IsAlive)
            {
                fighter.Status = StableFighterStatus.Dead;
                fighter.LastResult = "Died while in hand.";
            }
            else
            {
                fighter.Status = StableFighterStatus.AtHub;
                fighter.AddLog($"Back in the Stable at level {fighter.Character.Level}.");
                PrepareHub(fighter);
            }
            RaiseChanged();
        }

        public void SetAutoContinue(StableFighter fighter, bool on)
        {
            fighter.AutoContinue = on;
            // Turning auto on while it is waiting answers the prompt the same way it would have.
            if (on && fighter.Status == StableFighterStatus.AwaitingExit && ShouldAutoContinue(fighter))
                AnswerExit(fighter, leave: false);
            else
                RaiseChanged();
        }

        /// <summary>Tests: drop every fighter without saving.</summary>
        internal void ResetForTests()
        {
            foreach (var f in fighters)
                f.PendingExit?.TrySetResult(true);
            fighters.Clear();
            RosterChanged();
        }

        private async Task RunDungeonAsync(StableFighter fighter, int menuIndex)
        {
            var game = fighter.Game!;
            var hero = fighter.Character;
            int levelBefore = hero.Level;
            int itemsBefore = hero.Inventory?.Count ?? 0;

            using var ui = UIManager.BeginScopedUIManager(fighter.Output);
            using var mute = CombatUiMuteScope.Begin(muted: true);
            using var exit = DungeonExitChoiceHandler.BeginScopedExitDecider((_, room, total) => DecideExitAsync(fighter, room, total));
            using var paced = PaceCombat ? SimulationPacing.BeginPacedScope() : null;
            using var feed = CombatDelayManager.BeginAfterActionHook(() =>
            {
                fighter.Feed.AfterAction();
                RaiseChanged();
            });

            try
            {
                await game.HandleInput((menuIndex + 1).ToString());
            }
            catch (Exception ex)
            {
                DebugLogger.Log("StableManager", $"{hero.Name} dungeon run failed: {ex}");
                fighter.LastResult = $"Run failed: {ex.Message}";
            }

            switch (game.CurrentState)
            {
                case GameState.Death:
                    fighter.Status = StableFighterStatus.Dead;
                    fighter.LastResult = $"Died in {fighter.DungeonName}, room {fighter.CurrentRoomNumber} of {fighter.TotalRooms}.";
                    fighter.AddLog(fighter.LastResult);
                    RaiseChanged();
                    return;
                case GameState.DungeonCompletion:
                    fighter.LastResult = DescribeClear(fighter, levelBefore, itemsBefore);
                    break;
                default:
                    fighter.LastResult ??= $"Left {fighter.DungeonName} after {fighter.RoomsCleared} of {fighter.TotalRooms} rooms.";
                    break;
            }

            fighter.AddLog(fighter.LastResult!);
            await SaveAsync(fighter);
            fighter.Status = StableFighterStatus.AtHub;
            PrepareHub(fighter);
            RaiseChanged();

            // Keep going repeats after a clear or an automatic stop, not after the player chose to leave.
            if (fighter.KeepGoing && !fighter.LeftByChoice && fighters.Contains(fighter))
            {
                if (KeepGoingPauseMs > 0)
                    await Task.Delay(KeepGoingPauseMs);
                if (fighter.KeepGoing && fighter.Status == StableFighterStatus.AtHub && fighters.Contains(fighter))
                    Go(fighter);
            }
        }

        private async Task<bool> DecideExitAsync(StableFighter fighter, int roomCleared, int totalRooms)
        {
            fighter.RoomsCleared = roomCleared;
            fighter.TotalRooms = totalRooms;
            var hero = fighter.Character;
            fighter.AddLog($"Room {roomCleared}/{totalRooms} cleared - HP {hero.CurrentHealth}/{hero.GetEffectiveMaxHealth()}.");
            RaiseChanged();

            if (RoomPaceMs > 0)
                await Task.Delay(RoomPaceMs);

            if (fighter.AutoContinue && ShouldAutoContinue(fighter))
                return false;

            var pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            fighter.PendingExit = pending;
            fighter.Status = StableFighterStatus.AwaitingExit;
            RaiseChanged();
            return await pending.Task;
        }

        private bool ShouldAutoContinue(StableFighter fighter)
        {
            var hero = fighter.Character;
            int max = Math.Max(1, hero.GetEffectiveMaxHealth());
            return hero.CurrentHealth * 100 >= AutoContinueMinHpPercent * max;
        }

        private static string DescribeClear(StableFighter fighter, int levelBefore, int itemsBefore)
        {
            var hero = fighter.Character;
            var parts = new List<string> { $"Cleared {fighter.DungeonName}" };
            if (hero.Level > levelBefore)
                parts.Add($"level {levelBefore} -> {hero.Level}");
            int found = (hero.Inventory?.Count ?? 0) - itemsBefore;
            if (found > 0)
                parts.Add($"{found} item{(found == 1 ? "" : "s")} found");
            return string.Join(", ", parts) + ".";
        }

        /// <summary>Puts the fighter's game on dungeon selection with a list rolled for its current level.</summary>
        private static void PrepareHub(StableFighter fighter)
        {
            var selection = fighter.Game?.GetDungeonSelectionHandler();
            if (selection == null)
                return;
            using var ui = UIManager.BeginScopedUIManager(fighter.Output);
            using var mute = CombatUiMuteScope.Begin(muted: true);
            selection.ShowDungeonSelection().GetAwaiter().GetResult();
        }

        private static async Task SaveAsync(StableFighter fighter)
        {
            try
            {
                await fighter.Character.SaveCharacterAsync(fighter.CharacterId);
            }
            catch (Exception ex)
            {
                DebugLogger.Log("StableManager", $"Save failed for {fighter.Character.Name}: {ex.Message}");
                fighter.AddLog($"Save failed: {ex.Message}");
            }
        }

        private void RaiseChanged() => Changed?.Invoke();

        private void RosterChanged()
        {
            roster = fighters.ToArray();
        }

        private static readonly RPGGame.Combat.Events.CombatEventType[] FeedEvents =
        {
            RPGGame.Combat.Events.CombatEventType.ActionExecuted,
            RPGGame.Combat.Events.CombatEventType.ActionHit,
            RPGGame.Combat.Events.CombatEventType.ActionMiss,
            RPGGame.Combat.Events.CombatEventType.EnemyDied
        };

        /// <summary>
        /// New games clear the combat bus (and tests reset it), so check before every run and re-subscribe on Cleared.
        /// </summary>
        private void EnsureFeedSubscribed()
        {
            var bus = RPGGame.Combat.Events.CombatEventBus.Instance;
            bus.Cleared -= OnBusCleared;
            bus.Cleared += OnBusCleared;
            foreach (var type in FeedEvents)
            {
                if (!bus.HasSubscriber(type, OnCombatEvent))
                    bus.Subscribe(type, OnCombatEvent);
            }
        }

        private void OnBusCleared()
        {
            var bus = RPGGame.Combat.Events.CombatEventBus.Instance;
            foreach (var type in FeedEvents)
                bus.Subscribe(type, OnCombatEvent);
        }

        /// <summary>Routes bus events that involve a fighter's hero into that fighter's feed (others are the live game's).</summary>
        private void OnCombatEvent(RPGGame.Combat.Events.CombatEvent evt)
        {
            foreach (var f in roster)
            {
                if (!f.IsBusy)
                    continue;
                if (ReferenceEquals(evt.Source, f.Character) || ReferenceEquals(evt.Target, f.Character)
                    || (f.Status == StableFighterStatus.Exploring && evt.Source is Enemy && ReferenceEquals(f.CurrentEnemy, evt.Source)))
                {
                    f.Feed.OnEvent(evt, f.Character);
                    return;
                }
            }
        }
    }
}
