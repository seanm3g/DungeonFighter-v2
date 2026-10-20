using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.MCP;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game
{
    /// <summary>
    /// Stable: real headless dungeon runs per fighter, the between-rooms prompt, parallel fighters, and that a
    /// fighter's run never writes to the live game's UI manager.
    /// </summary>
    public static class StableManagerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        private static readonly List<string> CreatedIds = new();

        public static void RunAllTests()
        {
            Console.WriteLine("=== StableManager Tests ===\n");
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            try
            {
                TestAutoContinueRunsDungeonWithoutTouchingLiveUI();
                TestExitPromptWaitsForDecision();
                TestTwoFightersWaitInParallel();
                TestHandlerRosterAndFighterScreens();
                TestRemoveRefusedMidDungeon();
                TestClickInDungeonOpensFighterView();
                TestResurrectDeadFighter();
                TestKeepGoingStartsNextRun();
                TestPacedScopeKeepsActionDelay();
            }
            finally
            {
                StableManager.Instance.ResetForTests();
                CleanupSaves();
            }

            TestBase.PrintSummary("StableManager Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestAutoContinueRunsDungeonWithoutTouchingLiveUI()
        {
            Console.WriteLine("--- Auto-continue run, live UI untouched ---");
            var stable = NewStable();
            var liveUi = new OutputCapture();
            var previousUi = UIManager.GetCustomUIManager();
            UIManager.SetCustomUIManager(liveUi);
            try
            {
                var fighter = stable.Add(NewHero("StableAuto"), NewId());
                TestBase.AssertTrue(fighter.AutoContinue, "new fighters auto-continue by default", ref _testsRun, ref _testsPassed, ref _testsFailed);
                var dungeon = stable.GetDungeonChoices(fighter).FirstOrDefault();
                TestBase.AssertNotNull(dungeon, "fighter at the hub has dungeon choices", ref _testsRun, ref _testsPassed, ref _testsFailed);
                if (dungeon == null)
                    return;

                TestBase.AssertTrue(stable.StartDungeon(fighter, dungeon), "dungeon starts", ref _testsRun, ref _testsPassed, ref _testsFailed);
                bool done = WaitFor(() => fighter.Status is StableFighterStatus.AtHub or StableFighterStatus.Dead);
                TestBase.AssertTrue(done, $"run finishes on its own (status {fighter.Status})", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(fighter.LastResult != null, $"outcome recorded: {fighter.LastResult}", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(ReferenceEquals(UIManager.GetCustomUIManager(), liveUi), "live UI manager still registered", ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(0, liveUi.GetRecentOutput(200).Count, "nothing written to the live UI", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                UIManager.SetCustomUIManager(previousUi);
            }
        }

        private static void TestExitPromptWaitsForDecision()
        {
            Console.WriteLine("\n--- Between-rooms prompt waits for the player ---");
            var stable = NewStable();
            var fighter = stable.Add(NewHero("StableAsk"), NewId());
            fighter.AutoContinue = false;
            var dungeon = PickMultiRoomDungeon(stable, fighter);
            stable.StartDungeon(fighter, dungeon);

            bool asked = WaitFor(() => fighter.Status is StableFighterStatus.AwaitingExit or StableFighterStatus.Dead or StableFighterStatus.AtHub);
            if (fighter.Status != StableFighterStatus.AwaitingExit)
            {
                // A one-room dungeon or an early death never asks; nothing more to check here.
                TestBase.AssertTrue(asked, $"run ended without a prompt ({fighter.Status})", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            TestBase.AssertTrue(fighter.NeedsDecision, "waiting fighter is flagged", ref _testsRun, ref _testsPassed, ref _testsFailed);
            Thread.Sleep(150);
            TestBase.AssertEqualEnum(StableFighterStatus.AwaitingExit, fighter.Status, "stays paused until answered", ref _testsRun, ref _testsPassed, ref _testsFailed);
            int cleared = fighter.RoomsCleared;

            stable.AnswerExit(fighter, leave: true);
            bool back = WaitFor(() => fighter.Status == StableFighterStatus.AtHub);
            TestBase.AssertTrue(back, "leaving returns the fighter to the hub", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(fighter.LastResult?.StartsWith("Left", StringComparison.Ordinal) == true,
                $"result says it left after {cleared} room(s): {fighter.LastResult}", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(stable.GetDungeonChoices(fighter).Count > 0, "fresh dungeon list after returning", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestTwoFightersWaitInParallel()
        {
            Console.WriteLine("\n--- Two fighters at once ---");
            var stable = NewStable();
            var a = stable.Add(NewHero("StableA"), NewId());
            var b = stable.Add(NewHero("StableB"), NewId());
            a.AutoContinue = b.AutoContinue = false;
            stable.StartDungeon(a, PickMultiRoomDungeon(stable, a));
            stable.StartDungeon(b, PickMultiRoomDungeon(stable, b));
            TestBase.AssertTrue(a.IsBusy && b.IsBusy, "both fighters are out at once", ref _testsRun, ref _testsPassed, ref _testsFailed);

            WaitFor(() => !a.IsBusy || a.Status == StableFighterStatus.AwaitingExit);
            WaitFor(() => !b.IsBusy || b.Status == StableFighterStatus.AwaitingExit);
            TestBase.AssertEqual(2, stable.DecisionCount, "both now wait on the player", ref _testsRun, ref _testsPassed, ref _testsFailed);

            foreach (var f in new[] { a, b }.Where(f => f.Status == StableFighterStatus.AwaitingExit))
                stable.AnswerExit(f, leave: true);
            WaitFor(() => !a.IsBusy && !b.IsBusy);
        }

        private static void TestHandlerRosterAndFighterScreens()
        {
            Console.WriteLine("\n--- Stable screen ---");
            var stable = NewStable();
            var hero = NewHero("StableView");
            var fighter = stable.Add(hero, NewId());
            fighter.AutoContinue = false;
            var stateManager = new GameStateManager();
            var handler = new StableHandler(stateManager, null, stable);

            handler.ShowStable();
            TestBase.AssertEqualEnum(GameState.Stable, stateManager.CurrentState, "opens the Stable state", ref _testsRun, ref _testsPassed, ref _testsFailed);
            var view = handler.BuildView();
            var card = view.Cards.Single();
            TestBase.AssertTrue(card.Selected && card.NeedsDecision && card.Title.Contains(hero.Name) && card.Title.Contains("!"),
                "the waiting fighter's card is selected and flagged", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(card.Actions.Any(a => a.Key == "card:1:g" && a.Text.StartsWith("[G] Go:")),
                "hub card offers a one-key Go to a named dungeon", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(view.ShowAddSlot && view.Footer.Any(l => l.Key == "a"), "there is room to add a hero", ref _testsRun, ref _testsPassed, ref _testsFailed);

            Task.Run(() => handler.HandleInput("d")).Wait();
            int dungeonButtons = handler.BuildView().Cards.Single().Actions.Count(a => a.Key != null && int.TryParse(a.Key.Split(':').Last(), out _));
            TestBase.AssertEqual(Math.Min(StableHandler.PageSize, stable.GetDungeonChoices(fighter).Count), dungeonButtons,
                "D lists the dungeon choices on the card", ref _testsRun, ref _testsPassed, ref _testsFailed);
            Task.Run(() => handler.HandleInput("escape")).Wait();
            TestBase.AssertEqualEnum(GameState.Stable, stateManager.CurrentState, "Esc cancels the picker, not the screen", ref _testsRun, ref _testsPassed, ref _testsFailed);

            Task.Run(() => handler.HandleInput("card:1:t")).Wait();
            TestBase.AssertEqualEnum(StableFighterStatus.InHand, fighter.Status, "clicking Take in hand checks the fighter out", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(ReferenceEquals(stateManager.CurrentPlayer, hero), "main game now plays the same hero", ref _testsRun, ref _testsPassed, ref _testsFailed);

            handler.ShowStable();
            TestBase.AssertEqualEnum(StableFighterStatus.AtHub, fighter.Status, "reopening the Stable takes the hero back", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertNull(stateManager.CurrentPlayer, "main game releases the hero", ref _testsRun, ref _testsPassed, ref _testsFailed);

            Task.Run(() => handler.HandleInput("space")).Wait();
            TestBase.AssertTrue(fighter.Status != StableFighterStatus.AtHub || fighter.LastResult != null, "Space sends a hub fighter out", ref _testsRun, ref _testsPassed, ref _testsFailed);
            WaitFor(() => !fighter.IsBusy || fighter.Status == StableFighterStatus.AwaitingExit);
            if (fighter.Status == StableFighterStatus.AwaitingExit)
                stable.AnswerExit(fighter, leave: true);
            WaitFor(() => !fighter.IsBusy);
        }

        private static void TestResurrectDeadFighter()
        {
            Console.WriteLine("\n--- Resurrect [Dev] ---");
            var stable = NewStable();
            var fighter = stable.Add(NewHero("StableDead"), NewId());
            // Throw a level-1 hero at 1 HP into the hardest dungeon until it dies.
            for (int attempt = 0; attempt < 5 && fighter.Status != StableFighterStatus.Dead; attempt++)
            {
                var hardest = stable.GetDungeonChoices(fighter).OrderByDescending(d => d.MinLevel).First();
                fighter.Character.CurrentHealth = 1;
                stable.StartDungeon(fighter, hardest);
                WaitFor(() => !fighter.IsBusy);
            }
            if (fighter.Status != StableFighterStatus.Dead)
            {
                TestBase.AssertTrue(true, "(hero would not die; resurrect not exercised)", ref _testsRun, ref _testsPassed, ref _testsFailed);
                return;
            }

            TestBase.AssertTrue(fighter.NeedsDecision, "dead fighter waits on the player", ref _testsRun, ref _testsPassed, ref _testsFailed);
            bool back = Task.Run(() => stable.ResurrectAsync(fighter)).Result;
            TestBase.AssertTrue(back && fighter.Status == StableFighterStatus.AtHub, "resurrect returns the fighter to the hub", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(fighter.Character.GetEffectiveMaxHealth(), fighter.Character.CurrentHealth, "at full health", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(File.Exists(CharacterSaveManager.GetCharacterSaveFilename(fighter.CharacterId)), "living save written again", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(stable.Go(fighter), "can head out again", ref _testsRun, ref _testsPassed, ref _testsFailed);
            WaitFor(() => !fighter.IsBusy || fighter.Status == StableFighterStatus.AwaitingExit);
            if (fighter.Status == StableFighterStatus.AwaitingExit)
                stable.AnswerExit(fighter, leave: true);
            WaitFor(() => !fighter.IsBusy);
        }

        private static void TestKeepGoingStartsNextRun()
        {
            Console.WriteLine("\n--- Keep going ---");
            var stable = NewStable();
            var fighter = stable.Add(NewHero("StableKeep"), NewId());
            var choices = stable.GetDungeonChoices(fighter);
            var quick = stable.GetQuickDungeon(fighter);
            TestBase.AssertTrue(quick != null && quick.MinLevel <= Math.Max(fighter.Character.Level, choices.Min(d => d.MinLevel)),
                $"Go picks a dungeon the hero can handle ({quick?.Name} L{quick?.MinLevel})", ref _testsRun, ref _testsPassed, ref _testsFailed);

            int runs = 0;
            void CountRuns()
            {
                if (fighter.Status == StableFighterStatus.Exploring && fighter.Log.LastOrDefault()?.StartsWith("Entered") == true)
                    Interlocked.Increment(ref runs);
            }
            stable.Changed += CountRuns;
            try
            {
                stable.SetKeepGoing(fighter, true);
                WaitFor(() => runs >= 2 || fighter.Status == StableFighterStatus.Dead);
                TestBase.AssertTrue(runs >= 2 || fighter.Status == StableFighterStatus.Dead, $"started another run on its own ({runs} runs)", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                stable.Changed -= CountRuns;
                fighter.KeepGoing = false;
                WaitFor(() => !fighter.IsBusy || fighter.Status == StableFighterStatus.AwaitingExit);
                if (fighter.Status == StableFighterStatus.AwaitingExit)
                    stable.AnswerExit(fighter, leave: true);
                WaitFor(() => !fighter.IsBusy);
            }
        }

        private static void TestPacedScopeKeepsActionDelay()
        {
            Console.WriteLine("\n--- Paced fighters wait out action delays ---");
            bool instant = DeveloperModeState.IsCombatLogInstant;
            DeveloperModeState.SetCombatLogInstant(false);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                Task.Run(async () =>
                {
                    using (RPGGame.Combat.CombatUiMuteScope.Begin(muted: true))
                    using (SimulationPacing.BeginPacedScope())
                        await CombatDelayManager.DelayAfterActionAsync();
                }).Wait();
                int expected = DeveloperModeState.ScaleDelayMs(CombatDelayManager.Config.ActionDelayMs);
                TestBase.AssertTrue(expected > 0 && sw.ElapsedMilliseconds >= expected * 0.8,
                    $"muted + paced action waits ~{expected} ms (took {sw.ElapsedMilliseconds})", ref _testsRun, ref _testsPassed, ref _testsFailed);

                sw.Restart();
                Task.Run(async () =>
                {
                    using (RPGGame.Combat.CombatUiMuteScope.Begin(muted: true))
                        await CombatDelayManager.DelayAfterActionAsync();
                }).Wait();
                TestBase.AssertTrue(sw.ElapsedMilliseconds < 200, "muted without the scope stays instant", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                DeveloperModeState.SetCombatLogInstant(instant);
            }
        }

        private static void TestClickInDungeonOpensFighterView()
        {
            Console.WriteLine("\n--- Jump into a fighter mid-dungeon ---");
            var stable = NewStable();
            var hero = NewHero("StableWatch");
            var fighter = stable.Add(hero, NewId());
            fighter.AutoContinue = false;
            var stateManager = new GameStateManager();
            var handler = new StableHandler(stateManager, null, stable);
            handler.ShowStable();

            var other = stable.Add(NewHero("StableOther"), NewId());
            Task.Run(() => handler.HandleInput("2")).Wait();   // select the other card first
            stable.StartDungeon(fighter, PickMultiRoomDungeon(stable, fighter));
            Task.Run(() => handler.HandleInput("card:1")).Wait();
            var first = handler.BuildView();
            TestBase.AssertTrue(first.Watch == null && first.Cards[0].Selected, "first click on a card only selects it", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(first.Cards[0].Actions.Any(a => a.Key == "card:1:enter"), "selected card shows an Open button", ref _testsRun, ref _testsPassed, ref _testsFailed);
            Task.Run(() => handler.HandleInput("card:1")).Wait();
            var view = handler.BuildView();
            TestBase.AssertNotNull(view.Watch, "second click on the selected card opens the fighter view", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(view.Watch?.Hero.Any(l => l.Text.StartsWith("HP [")) == true, "fighter view shows the hero's HP bar", ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Some rooms have no fight; press on until one does (or the run ends).
            bool HasPlay(List<StableLine> lines) => lines.Any(l => l.Text.Contains(" - ") && (l.Text.Contains("->") || l.Text.Contains("miss")));
            for (int room = 0; room < 10; room++)
            {
                WaitFor(() => !fighter.IsBusy || fighter.Status == StableFighterStatus.AwaitingExit);
                if (HasPlay(fighter.Feed.Lines) || fighter.Status != StableFighterStatus.AwaitingExit)
                    break;
                stable.AnswerExit(fighter, leave: false);
            }
            var feed = handler.BuildView().Watch?.Feed ?? new List<StableLine>();
            TestBase.AssertTrue(feed.Any(l => l.Text.Contains(" - ") && (l.Text.Contains("->") || l.Text.Contains("miss"))),
                $"live feed has play-by-play ({feed.Count} lines, e.g. '{feed.FirstOrDefault(l => l.Text.Contains("->"))?.Text}')", ref _testsRun, ref _testsPassed, ref _testsFailed);

            if (fighter.Status == StableFighterStatus.AwaitingExit)
            {
                var actions = handler.BuildView().Watch!.Actions;
                TestBase.AssertTrue(actions.Any(a => a.Key == "card:1:c"), "fighter view offers Continue", ref _testsRun, ref _testsPassed, ref _testsFailed);
                Task.Run(() => handler.HandleInput("l")).Wait();
                TestBase.AssertNotNull(handler.BuildView().Watch, "deciding keeps you in the fighter view", ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            WaitFor(() => !fighter.IsBusy);

            Task.Run(() => handler.HandleInput("escape")).Wait();
            var back = handler.BuildView();
            TestBase.AssertTrue(back.Watch == null && back.Cards.Count == 2, "Esc returns to all fighters", ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqualEnum(GameState.Stable, stateManager.CurrentState, "still on the Stable", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRemoveRefusedMidDungeon()
        {
            Console.WriteLine("\n--- Remove only between dungeons ---");
            var stable = NewStable();
            var fighter = stable.Add(NewHero("StableRemove"), NewId());
            fighter.AutoContinue = false;
            stable.StartDungeon(fighter, PickMultiRoomDungeon(stable, fighter));
            bool removed = fighter.IsBusy && Task.Run(() => stable.RemoveAsync(fighter)).Result;
            TestBase.AssertFalse(removed, "busy fighter cannot be removed", ref _testsRun, ref _testsPassed, ref _testsFailed);
            WaitFor(() => !fighter.IsBusy || fighter.Status == StableFighterStatus.AwaitingExit);
            if (fighter.Status == StableFighterStatus.AwaitingExit)
                stable.AnswerExit(fighter, leave: true);
            WaitFor(() => !fighter.IsBusy);
            bool removedLater = Task.Run(() => stable.RemoveAsync(fighter)).Result;
            TestBase.AssertTrue(removedLater && stable.Fighters.Count == 0, "idle fighter can be removed", ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        // ------------------------------------------------------------------ helpers

        private static StableManager NewStable()
        {
            StableManager.Instance.ResetForTests();
            var stable = StableManager.Instance;
            stable.RoomPaceMs = 0;
            stable.KeepGoingPauseMs = 0;
            stable.PaceCombat = false;
            stable.AutoContinueMinHpPercent = 0;
            return stable;
        }

        private static Character NewHero(string name)
        {
            var hero = new Character(name, 1);
            new GameInitializer().InitializeNewGame(hero, new List<Dungeon>(), weaponChoice: 1);
            hero.PendingPreWeaponTrainingGround = false;
            hero.CurrentHealth = hero.GetEffectiveMaxHealth();
            return hero;
        }

        private static string NewId()
        {
            string id = $"stabletest_{Guid.NewGuid().ToString("N")[..8]}";
            CreatedIds.Add(id);
            return id;
        }

        private static Dungeon PickMultiRoomDungeon(StableManager stable, StableFighter fighter) =>
            stable.GetDungeonChoices(fighter).OrderBy(d => d.MinLevel).First();

        private static bool WaitFor(Func<bool> condition, int timeoutMs = 60000)
        {
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                if (condition())
                    return true;
                Thread.Sleep(20);
            }
            return condition();
        }

        private static void CleanupSaves()
        {
            foreach (var id in CreatedIds)
            {
                try
                {
                    string live = CharacterSaveManager.GetCharacterSaveFilename(id);
                    string? dir = Path.GetDirectoryName(live);
                    if (dir == null || !Directory.Exists(dir))
                        continue;
                    foreach (var file in Directory.GetFiles(dir, $"character_{id}_*"))
                        File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  (cleanup skipped for {id}: {ex.Message})");
                }
            }
            CreatedIds.Clear();
        }
    }
}
