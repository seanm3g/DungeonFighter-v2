namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Avalonia.Threading;
    using RPGGame.Audio;
    using RPGGame.UI.Avalonia;
    using RPGGame.Utils;

    /// <summary>
    /// Full-screen Stable: a card per fighter in <see cref="StableManager"/>, live status, and the decisions each one is
    /// waiting on. One card is selected; action keys apply to it, and clicking a card's action selects it first.
    /// Clicking a card selects it; clicking it again, its Open button, or Enter jumps into that fighter: a full-screen
    /// view with its live fight and the same actions.
    ///
    /// Grid: 1-9 select, Enter open fighter, Tab next fighter waiting, Space does the obvious thing (continue / go) and moves on,
    /// G go (repeat or best dungeon), D pick a dungeon (then 1-9), C continue, L leave, U auto-continue, K keep going,
    /// T take in hand, R resurrect [Dev], X remove / lay to rest (Y confirms), A add a saved hero, N new hero,
    /// Esc/0 cancel or main menu. Add list: 1-9 add, N/P page, D delete (Y confirms), Esc/0 back.
    /// </summary>
    public class StableHandler
    {
        public const int PageSize = 9;

        /// <summary>Clickable card actions send <c>card:{number}:{key}</c>.</summary>
        public const string CardKeyPrefix = "card:";

        private enum Mode { Grid, Watch, AddHero }

        private readonly GameStateManager stateManager;
        private readonly IUIManager? customUIManager;
        private readonly StableManager stable;

        private Mode mode = Mode.Grid;
        private StableFighter? selected;
        private bool pickingDungeon;
        private bool confirmRemove;
        private string? notice;
        private bool noticeIsWarning;

        // Add-hero list (saves not already in the Stable)
        private List<(string characterId, string characterName, int level)> saves = new();
        private int addPage;
        private bool deleteMode;
        private int? pendingDeleteIndex;

        // The next hero made with N joins the Stable when the player comes back here.
        private bool adoptNextNewHero;

        private DispatcherTimer? refreshTimer;
        private bool renderQueued;
        private int lastDecisionCount;

        public delegate void OnShowGameLoop();
        public delegate void OnShowMainMenu();
        public delegate Task OnStartNewHero();
        public delegate void OnShowMessage(string message);
        public event OnShowGameLoop? ShowGameLoopEvent;
        public event OnShowMainMenu? ShowMainMenuEvent;
        public event OnStartNewHero? StartNewHeroEvent;
        public event OnShowMessage? ShowMessageEvent;

        public StableHandler(GameStateManager stateManager, IUIManager? customUIManager, StableManager? stable = null)
        {
            this.stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            this.customUIManager = customUIManager;
            this.stable = stable ?? StableManager.Instance;
        }

        // Every GameCoordinator builds handlers (fighters' headless games too, and the live game again on
        // SetUIManager), so only the handler that last opened the Stable listens for changes.
        private static StableHandler? listening;

        /// <summary>Opens the Stable, first taking back any fighter the main game was playing.</summary>
        public void ShowStable()
        {
            if (!ReferenceEquals(listening, this))
            {
                if (listening != null)
                    listening.stable.Changed -= listening.OnStableChanged;
                stable.Changed += OnStableChanged;
                listening = this;
            }

            ReclaimFromMainGame();
            mode = Mode.Grid;
            pickingDungeon = false;
            confirmRemove = false;
            ClearDeleteState();
            if (selected == null || !stable.Fighters.Contains(selected))
                selected = stable.Fighters.FirstOrDefault(f => f.NeedsDecision) ?? stable.Fighters.FirstOrDefault();
            stateManager.TransitionToState(GameState.Stable);
            StartRefreshTimer();
            Render();
        }

        public async Task HandleInput(string input)
        {
            string raw = (input ?? "").Trim();
            notice = null;

            // Clicked a card's action: select that card, then act on it.
            if (raw.StartsWith(CardKeyPrefix, StringComparison.Ordinal))
            {
                var parts = raw.Substring(CardKeyPrefix.Length).Split(':', 2);
                if (int.TryParse(parts[0], out int n) && n >= 1 && n <= stable.Fighters.Count)
                {
                    bool wasSelected = ReferenceEquals(selected, stable.Fighters[n - 1]);
                    if (!wasSelected)
                    {
                        selected = stable.Fighters[n - 1];
                        pickingDungeon = false;
                        confirmRemove = false;
                    }
                    if (parts.Length > 1)
                        await HandleGridInput(parts[1].ToLowerInvariant());
                    else if (wasSelected && mode == Mode.Grid)
                        OpenWatch(selected);   // second click on the selected card goes in
                    else
                        Render();              // first click selects it and shows its options
                }
                return;
            }

            string key = raw.ToLowerInvariant();
            if (mode == Mode.AddHero)
                await HandleAddInput(key);
            else
                await HandleGridInput(key);
        }

        private async Task HandleGridInput(string key)
        {
            var f = selected != null && stable.Fighters.Contains(selected) ? selected : null;

            if (confirmRemove && f != null)
            {
                confirmRemove = false;
                if (key == "y")
                {
                    string name = f.Character.Name;
                    bool dead = f.Status == StableFighterStatus.Dead;
                    if (await stable.RemoveAsync(f))
                    {
                        Notify(dead ? $"{name} is laid to rest." : $"{name} left the Stable (saved).");
                        selected = NextWaiting(null) ?? stable.Fighters.FirstOrDefault();
                    }
                }
                else
                {
                    Notify("Cancelled.");
                }
                Render();
                return;
            }

            if (pickingDungeon && f != null)
            {
                var dungeons = stable.GetDungeonChoices(f);
                if (int.TryParse(key, out int d) && d >= 1 && d <= Math.Min(PageSize, dungeons.Count))
                {
                    pickingDungeon = false;
                    if (stable.StartDungeon(f, dungeons[d - 1]))
                        AfterDecision();
                    else
                        Render();
                    return;
                }
                if (key is "0" or "escape" or "d")
                {
                    pickingDungeon = false;
                    Render();
                    return;
                }
                Invalid("Pick a dungeon number, or Esc to cancel.");
                return;
            }

            switch (key)
            {
                case "0":
                case "escape":
                    if (mode == Mode.Watch)
                        BackToGrid();
                    else
                        LeaveToMainMenu();
                    return;
                case "enter":
                    if (mode == Mode.Grid && f != null)
                        OpenWatch(f);
                    return;
                case "a":
                    OpenAddList();
                    return;
                case "n":
                    adoptNextNewHero = true;
                    StopRefreshTimer();
                    if (StartNewHeroEvent != null)
                        await StartNewHeroEvent.Invoke();
                    return;
                case "tab":
                    var next = NextWaiting(f);
                    if (next == null)
                    {
                        Notify("Nobody is waiting on you.");
                    }
                    else
                    {
                        selected = next;
                    }
                    Render();
                    return;
                case "left":
                case "up":
                    MoveSelection(-1);
                    return;
                case "right":
                case "down":
                    MoveSelection(+1);
                    return;
            }

            if (int.TryParse(key, out int pick) && pick >= 1 && pick <= stable.Fighters.Count)
            {
                selected = stable.Fighters[pick - 1];
                if (mode == Mode.Watch)
                    OpenWatch(selected);
                else
                    Render();
                return;
            }

            if (f == null)
            {
                Invalid("Add a hero with A, or create one with N.");
                return;
            }

            await HandleFighterAction(f, key);
        }

        private async Task HandleFighterAction(StableFighter f, string key)
        {
            switch (key)
            {
                case "space":
                    if (f.Status == StableFighterStatus.AwaitingExit)
                        Decide(() => stable.AnswerExit(f, leave: false));
                    else if (f.Status == StableFighterStatus.AtHub)
                        Decide(() => stable.Go(f));
                    else
                        Invalid(f.Status == StableFighterStatus.Dead ? "R resurrects [Dev]; X lays them to rest." : "Nothing to decide right now.");
                    return;
                case "u":
                    if (f.Status == StableFighterStatus.Dead)
                        break;
                    stable.SetAutoContinue(f, !f.AutoContinue);
                    Notify(f.AutoContinue
                        ? $"Auto-continue on: keeps going between rooms while HP is at least {stable.AutoContinueMinHpPercent}%."
                        : "Auto-continue off: asks after every room.");
                    Render();
                    return;
                case "k":
                    if (f.Status == StableFighterStatus.Dead)
                        break;
                    stable.SetKeepGoing(f, !f.KeepGoing);
                    Notify(f.KeepGoing
                        ? "Keep going on: starts the next dungeon after each run, unless you choose to leave."
                        : "Keep going off: waits at the hub after each run.");
                    Render();
                    return;
            }

            switch (f.Status)
            {
                case StableFighterStatus.AtHub:
                    switch (key)
                    {
                        case "g":
                            Decide(() => stable.Go(f));
                            return;
                        case "d":
                            pickingDungeon = true;
                            Render();
                            return;
                        case "t":
                            TakeInHand(f);
                            return;
                        case "x":
                            confirmRemove = true;
                            Render();
                            return;
                    }
                    break;
                case StableFighterStatus.AwaitingExit:
                    if (key is "c" or "l")
                    {
                        Decide(() => stable.AnswerExit(f, leave: key == "l"));
                        return;
                    }
                    break;
                case StableFighterStatus.Dead:
                    if (key == "r")
                    {
                        await stable.ResurrectAsync(f);
                        AudioCues.Trigger(AudioCue.Menu_Confirm);
                        Notify($"{f.Character.Name} is back on their feet [Dev].");
                        Render();
                        return;
                    }
                    if (key == "x")
                    {
                        confirmRemove = true;
                        Render();
                        return;
                    }
                    break;
            }

            Invalid("That isn't available for this fighter right now.");
        }

        /// <summary>Runs a decision, then moves the selection to the next fighter waiting.</summary>
        private void Decide(System.Action decision)
        {
            decision();
            AfterDecision();
        }

        private void AfterDecision()
        {
            AudioCues.Trigger(AudioCue.Menu_Confirm);
            // On the grid the selection moves on to the next fighter waiting; inside a fighter's view you stay put.
            if (mode == Mode.Grid && NextWaiting(selected) is { } next)
                selected = next;
            Render();
        }

        private void OpenWatch(StableFighter? f)
        {
            if (f == null)
                return;
            selected = f;
            mode = Mode.Watch;
            pickingDungeon = false;
            confirmRemove = false;
            Render();
        }

        /// <summary>Next fighter waiting on the player after <paramref name="from"/>, wrapping; null if none.</summary>
        private StableFighter? NextWaiting(StableFighter? from)
        {
            var list = stable.Fighters;
            if (list.Count == 0)
                return null;
            int start = from == null ? -1 : list.ToList().IndexOf(from);
            for (int i = 1; i <= list.Count; i++)
            {
                var f = list[(start + i + list.Count) % list.Count];
                if (f.NeedsDecision && !ReferenceEquals(f, from))
                    return f;
            }
            return null;
        }

        private void MoveSelection(int delta)
        {
            var list = stable.Fighters;
            if (list.Count == 0)
                return;
            int index = selected == null ? 0 : list.ToList().IndexOf(selected);
            selected = list[((index < 0 ? 0 : index) + delta + list.Count) % list.Count];
            pickingDungeon = false;
            confirmRemove = false;
            Render();
        }

        private async Task HandleAddInput(string key)
        {
            if (pendingDeleteIndex.HasValue)
            {
                int index = pendingDeleteIndex.Value;
                ClearDeleteState();
                if (key == "y" && index < saves.Count)
                {
                    var (characterId, characterName, level) = saves[index];
                    string? filename = characterId.EndsWith("_legacy", StringComparison.Ordinal)
                        ? null
                        : CharacterSaveManager.GetCharacterSaveFilename(characterId);
                    CharacterSaveManager.DeleteSaveFile(filename);
                    Notify($"Deleted {characterName} (Level {level}).");
                    ReloadSaves();
                    addPage = Math.Min(addPage, AddPageCount - 1);
                }
                else
                {
                    Notify("Delete cancelled.");
                }
                Render();
                return;
            }

            switch (key)
            {
                case "0":
                case "escape":
                    if (deleteMode)
                    {
                        ClearDeleteState();
                        Render();
                        return;
                    }
                    BackToGrid();
                    return;
                case "n":
                case "right":
                case "pagedown":
                    ChangeAddPage(+1);
                    return;
                case "p":
                case "left":
                case "pageup":
                    ChangeAddPage(-1);
                    return;
                case "d":
                    if (saves.Count == 0)
                    {
                        Invalid("No saved heroes to delete.");
                        return;
                    }
                    deleteMode = !deleteMode;
                    Render();
                    return;
            }

            int choice = int.TryParse(key, out int c) ? c : 0;
            int idx = addPage * PageSize + (choice - 1);
            if (choice < 1 || choice > PageSize || idx >= saves.Count)
            {
                Invalid("Pick a hero number, N/P to page, D to delete, or Esc to go back.");
                return;
            }

            if (deleteMode)
            {
                deleteMode = false;
                pendingDeleteIndex = idx;
                Render();
                return;
            }

            var pick = saves[idx];
            var (fighter, error) = await stable.AddFromSaveAsync(pick.characterId);
            if (fighter == null)
            {
                Invalid(error ?? "Could not add that hero.");
                return;
            }
            AudioCues.Trigger(AudioCue.Menu_Confirm);
            Notify($"{fighter.Character.Name} joined the Stable.");
            selected = fighter;
            BackToGrid();
        }

        private void TakeInHand(StableFighter f)
        {
            if (!stable.CheckOut(f))
                return;
            StopRefreshTimer();

            stateManager.AddCharacter(f.Character, f.CharacterId);
            stateManager.SetCurrentPlayer(f.Character);
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                canvasUI.RestoreDisplayBufferRendering();
                canvasUI.SetCharacter(f.Character);
                canvasUI.RefreshCharacterPanel();
            }
            ShowMessageEvent?.Invoke($"{f.Character.Name} is in your hands. Press 0 at the hub to return to the Stable.");
            stateManager.TransitionToState(GameState.GameLoop);
            ShowGameLoopEvent?.Invoke();
        }

        /// <summary>
        /// Takes back fighters the main game was playing (and a hero just made with N), so the same character is
        /// never run by both games at once.
        /// </summary>
        private void ReclaimFromMainGame()
        {
            var mainHero = stateManager.CurrentPlayer;
            if (adoptNextNewHero && mainHero != null && mainHero.IsAlive && mainHero.Weapon != null && stable.Find(mainHero) == null)
            {
                adoptNextNewHero = false;
                var fighter = stable.Add(mainHero, stateManager.GetCharacterId(mainHero));
                fighter.Status = StableFighterStatus.InHand;
                selected = fighter;
            }

            foreach (var f in stable.Fighters.Where(f => f.Status == StableFighterStatus.InHand).ToList())
            {
                if (ReferenceEquals(stateManager.CurrentPlayer, f.Character))
                {
                    var id = stateManager.GetCharacterId(f.Character);
                    if (!string.IsNullOrEmpty(id))
                        stateManager.RemoveCharacter(id);
                    stateManager.SetCurrentPlayer(null);
                }
                stable.ReturnFromHand(f);
            }
        }

        private void BackToGrid()
        {
            mode = Mode.Grid;
            pickingDungeon = false;
            confirmRemove = false;
            ClearDeleteState();
            Render();
        }

        private void OpenAddList()
        {
            if (stable.Fighters.Count >= StableManager.MaxFighters)
            {
                Invalid($"The Stable is full ({StableManager.MaxFighters} fighters).");
                return;
            }
            mode = Mode.AddHero;
            addPage = 0;
            ClearDeleteState();
            ReloadSaves();
            Render();
        }

        private void LeaveToMainMenu()
        {
            StopRefreshTimer();
            if (customUIManager is CanvasUICoordinator canvasUI)
                canvasUI.RestoreDisplayBufferRendering();
            stateManager.TransitionToState(GameState.MainMenu);
            ShowMainMenuEvent?.Invoke();
        }

        private int AddPageCount => Math.Max(1, (saves.Count + PageSize - 1) / PageSize);

        private void ChangeAddPage(int delta)
        {
            int target = addPage + delta;
            if (target < 0 || target >= AddPageCount)
            {
                AudioCues.Trigger(AudioCue.Menu_Invalid);
                return;
            }
            addPage = target;
            pendingDeleteIndex = null;
            Render();
        }

        private void ReloadSaves()
        {
            saves = (CharacterSaveManager.ListAllSavedCharacters() ?? new List<(string, string, int)>())
                .Where(s => stable.Find(s.characterId) == null)
                .ToList();
        }

        private void ClearDeleteState()
        {
            deleteMode = false;
            pendingDeleteIndex = null;
        }

        private void Notify(string message, bool warning = false)
        {
            notice = message;
            noticeIsWarning = warning;
        }

        private void Invalid(string message)
        {
            AudioCues.Trigger(AudioCue.Menu_Invalid);
            Notify(message, warning: true);
            Render();
        }

        // ---------------------------------------------------------------- view

        /// <summary>Builds the current screen (also used by tests).</summary>
        public StableMenuView BuildView()
        {
            var noticeLine = notice == null ? null : new StableLine(notice, noticeIsWarning ? StableLineTone.Bad : StableLineTone.Good);
            if (mode == Mode.AddHero)
            {
                return new StableMenuView
                {
                    Title = "THE STABLE",
                    Summary = new StableLine("Add a saved hero", StableLineTone.Header),
                    PanelTitle = "ADD A HERO",
                    Panel = BuildAddLines(),
                    Footer = new List<StableLine>
                    {
                        new("[1-9] Add", StableLineTone.Option),
                        new("[N/P] Page", StableLineTone.Option),
                        new("[D] Delete", StableLineTone.Bad, "d"),
                        new("[Esc] Back", StableLineTone.Option, "escape")
                    },
                    Notice = noticeLine
                };
            }

            var fighters = stable.Fighters;
            int waiting = stable.DecisionCount;
            if (mode == Mode.Watch && selected != null && fighters.Contains(selected))
                return BuildWatchView(selected, waiting, noticeLine);

            var summary = fighters.Count == 0
                ? new StableLine("No fighters yet. Add saved heroes and run them side by side.", StableLineTone.Header)
                : waiting > 0
                    ? new StableLine($"{fighters.Count} fighter{(fighters.Count == 1 ? "" : "s")}  -  {waiting} waiting on you", StableLineTone.Alert)
                    : new StableLine($"{fighters.Count} fighter{(fighters.Count == 1 ? "" : "s")}  -  all busy", StableLineTone.Header);

            var footer = new List<StableLine>();
            if (waiting > 0)
                footer.Add(new StableLine("[Tab] Next waiting", StableLineTone.Alert, "tab"));
            footer.Add(new StableLine("[Space] Do it", StableLineTone.Option, "space"));
            if (fighters.Count > 0)
                footer.Add(new StableLine("[Enter] Open fighter", StableLineTone.Option, "enter"));
            if (fighters.Count < StableManager.MaxFighters)
                footer.Add(new StableLine("[A] Add hero", StableLineTone.Option, "a"));
            footer.Add(new StableLine("[N] New hero", StableLineTone.Option, "n"));
            footer.Add(new StableLine("[Esc] Main menu", StableLineTone.Option, "escape"));

            return new StableMenuView
            {
                Summary = summary,
                Cards = fighters.Select((f, i) => BuildCard(f, i + 1)).ToList(),
                ShowAddSlot = fighters.Count < StableManager.MaxFighters,
                Footer = footer,
                Notice = noticeLine
            };
        }

        private StableCardView BuildCard(StableFighter f, int number)
        {
            var hero = f.Character;
            bool isSelected = ReferenceEquals(f, selected);
            string Key(string k) => $"{CardKeyPrefix}{number}:{k}";

            var lines = new List<StableLine>
            {
                new(HpText(hero), HpTone(hero)),
                new(StatusText(f), RowTone(f))
            };
            if (f.Status == StableFighterStatus.Exploring && f.CurrentEnemy is { } enemy)
                lines.Add(new StableLine($"vs {enemy.Name} L{enemy.Level}  {Bar(enemy.CurrentHealth, enemy.GetEffectiveMaxHealth(), 10)} {Math.Max(0, enemy.CurrentHealth)}/{enemy.GetEffectiveMaxHealth()}", StableLineTone.Normal));
            else if (f.LastResult != null && f.Status is StableFighterStatus.AtHub or StableFighterStatus.Dead)
                lines.Add(new StableLine(f.LastResult, StableLineTone.Dim));


            var actions = BuildActions(f, isSelected, Key);
            if (isSelected && !confirmRemove && !pickingDungeon)
                actions.Insert(0, new StableLine("[Enter] Open", StableLineTone.Header, Key("enter")));

            return new StableCardView
            {
                Number = number,
                Selected = isSelected,
                NeedsDecision = f.NeedsDecision,
                BorderTone = isSelected ? StableLineTone.Selected : RowTone(f),
                Title = $"[{number}]{(f.NeedsDecision ? " !" : "")} {hero.Name}  L{hero.Level} {hero.GetCurrentClass()}",
                SelectKey = $"{CardKeyPrefix}{number}",
                Lines = lines,
                Log = f.Log.Reverse().Select(l => new StableLine(l, StableLineTone.Dim)).ToList(),
                Actions = actions
            };
        }

        /// <summary>The fighter's buttons for its current status (shared by the card and the fighter view).</summary>
        private List<StableLine> BuildActions(StableFighter f, bool isSelected, Func<string, string> Key)
        {
            var hero = f.Character;
            var actions = new List<StableLine>();
            void AddToggles()
            {
                actions.Add(new StableLine($"[U] Auto {(f.AutoContinue ? "ON" : "off")}", f.AutoContinue ? StableLineTone.Good : StableLineTone.Option, Key("u")));
                actions.Add(new StableLine($"[K] Keep going {(f.KeepGoing ? "ON" : "off")}", f.KeepGoing ? StableLineTone.Good : StableLineTone.Option, Key("k")));
            }

            if (confirmRemove && isSelected)
            {
                actions.Add(new StableLine(f.Status == StableFighterStatus.Dead
                    ? $"Lay {hero.Name} to rest? [Y] yes"
                    : $"Remove {hero.Name}? Saved first. [Y] yes", StableLineTone.Bad, Key("y")));
                actions.Add(new StableLine("[Esc] cancel", StableLineTone.Option, Key("escape")));
            }
            else if (pickingDungeon && isSelected)
            {
                var dungeons = stable.GetDungeonChoices(f);
                for (int i = 0; i < Math.Min(PageSize, dungeons.Count); i++)
                {
                    var d = dungeons[i];
                    actions.Add(new StableLine($"[{i + 1}] {d.Name} L{d.MinLevel}", StableLineTone.Option, Key((i + 1).ToString())));
                }
                actions.Add(new StableLine("[Esc] cancel", StableLineTone.Option, Key("escape")));
            }
            else
            {
                switch (f.Status)
                {
                    case StableFighterStatus.AtHub:
                        var quick = stable.GetQuickDungeon(f);
                        if (quick != null)
                            actions.Add(new StableLine($"[G] Go: {quick.Name} L{quick.MinLevel}", f.NeedsDecision ? StableLineTone.Alert : StableLineTone.Option, Key("g")));
                        actions.Add(new StableLine("[D] Pick dungeon", StableLineTone.Option, Key("d")));
                        actions.Add(new StableLine("[T] Take in hand", StableLineTone.Option, Key("t")));
                        AddToggles();
                        actions.Add(new StableLine("[X] Remove", StableLineTone.Option, Key("x")));
                        break;
                    case StableFighterStatus.AwaitingExit:
                        actions.Add(new StableLine("[C] Continue", StableLineTone.Alert, Key("c")));
                        actions.Add(new StableLine("[L] Leave (full heal, no rewards)", StableLineTone.Option, Key("l")));
                        AddToggles();
                        break;
                    case StableFighterStatus.Exploring:
                        AddToggles();
                        break;
                    case StableFighterStatus.Dead:
                        actions.Add(new StableLine("[R] Resurrect [Dev]", StableLineTone.Alert, Key("r")));
                        actions.Add(new StableLine("[X] Lay to rest", StableLineTone.Option, Key("x")));
                        break;
                    case StableFighterStatus.InHand:
                        actions.Add(new StableLine("In your hands in the main game", StableLineTone.Dim));
                        break;
                }
            }
            return actions;
        }

        private StableMenuView BuildWatchView(StableFighter f, int waiting, StableLine? noticeLine)
        {
            var hero = f.Character;
            int number = stable.Fighters.ToList().IndexOf(f) + 1;
            string Key(string k) => $"{CardKeyPrefix}{number}:{k}";

            var heroLines = new List<StableLine>
            {
                new($"Level {hero.Level} {hero.GetCurrentClass()}  -  {hero.Weapon?.Name ?? "no weapon"}", StableLineTone.Normal),
                new($"HP {Bar(hero.CurrentHealth, hero.GetEffectiveMaxHealth(), 30)} {Math.Clamp(hero.CurrentHealth, 0, Math.Max(1, hero.GetEffectiveMaxHealth()))}/{hero.GetEffectiveMaxHealth()}", HpTone(hero)),
                new($"XP {hero.XP}    Bag {hero.Inventory?.Count ?? 0}", StableLineTone.Dim),
                new(StatusText(f), RowTone(f))
            };

            var enemyLines = new List<StableLine>();
            if (f.CurrentEnemy is { } enemy)
            {
                enemyLines.Add(new StableLine($"{enemy.Name}  L{enemy.Level}", StableLineTone.Bad));
                enemyLines.Add(new StableLine($"HP {Bar(enemy.CurrentHealth, enemy.GetEffectiveMaxHealth(), 30)} {Math.Max(0, enemy.CurrentHealth)}/{enemy.GetEffectiveMaxHealth()}", StableLineTone.Normal));
            }
            else
            {
                enemyLines.Add(new StableLine(f.Status switch
                {
                    StableFighterStatus.Exploring => "Moving to the next room...",
                    StableFighterStatus.AwaitingExit => "Room cleared.",
                    StableFighterStatus.Dead => "-",
                    _ => "No fight."
                }, StableLineTone.Dim));
            }

            var dungeonLines = new List<StableLine>();
            if (f.IsBusy && f.DungeonName != null)
            {
                int room = f.Status == StableFighterStatus.AwaitingExit ? f.RoomsCleared : Math.Max(1, f.CurrentRoomNumber);
                dungeonLines.Add(new StableLine($"{f.DungeonName}  L{f.DungeonLevel}", StableLineTone.Header));
                dungeonLines.Add(new StableLine($"Room {room} of {f.TotalRooms}  {Bar(f.RoomsCleared, Math.Max(1, f.TotalRooms), 20)}", StableLineTone.Normal));
            }
            else
            {
                dungeonLines.Add(new StableLine(f.LastResult ?? "At the hub.", StableLineTone.Dim));
            }

            var summary = waiting > 0
                ? new StableLine($"{waiting} fighter{(waiting == 1 ? "" : "s")} waiting on you", StableLineTone.Alert)
                : new StableLine("nobody else is waiting", StableLineTone.Dim);
            var footer = new List<StableLine>();
            if (waiting > 0 && !(waiting == 1 && f.NeedsDecision))
                footer.Add(new StableLine("[Tab] Next waiting", StableLineTone.Alert, "tab"));
            footer.Add(new StableLine($"[1-{stable.Fighters.Count}] Other fighters", StableLineTone.Option));
            footer.Add(new StableLine("[Esc] All fighters", StableLineTone.Option, "escape"));

            return new StableMenuView
            {
                Title = $"THE STABLE  >  {hero.Name.ToUpperInvariant()}",
                Summary = summary,
                Footer = footer,
                Notice = noticeLine,
                Watch = new StableWatchView
                {
                    Name = $"[{number}]{(f.NeedsDecision ? " !" : "")} {hero.Name}",
                    NameTone = f.NeedsDecision ? StableLineTone.Alert : StableLineTone.Header,
                    Hero = heroLines,
                    Enemy = enemyLines,
                    Dungeon = dungeonLines,
                    Feed = f.Feed.Lines,
                    Actions = BuildActions(f, isSelected: true, Key)
                }
            };
        }

        private List<StableLine> BuildAddLines()
        {
            var lines = new List<StableLine>();
            if (saves.Count == 0)
            {
                lines.Add(new StableLine("No saved heroes outside the Stable."));
                return lines;
            }

            lines.Add(new StableLine(AddPageCount > 1
                ? $"{saves.Count} saved heroes  -  page {addPage + 1} of {AddPageCount}"
                : $"{saves.Count} saved hero{(saves.Count == 1 ? "" : "es")}", StableLineTone.Header));
            lines.Add(StableLine.Blank);

            int start = addPage * PageSize;
            for (int i = 0; i < PageSize && start + i < saves.Count; i++)
            {
                var s = saves[start + i];
                lines.Add(new StableLine($"[{i + 1}] {s.characterName} - Level {s.level}", StableLineTone.Option, (i + 1).ToString()));
            }

            lines.Add(StableLine.Blank);
            if (addPage < AddPageCount - 1)
                lines.Add(new StableLine("[N] Next page", StableLineTone.Normal, "n"));
            if (addPage > 0)
                lines.Add(new StableLine("[P] Previous page", StableLineTone.Normal, "p"));

            if (pendingDeleteIndex.HasValue && pendingDeleteIndex.Value < saves.Count)
            {
                var s = saves[pendingDeleteIndex.Value];
                lines.Add(StableLine.Blank);
                lines.Add(new StableLine($"Delete {s.characterName} (Level {s.level}) permanently? Press Y to confirm, any other key to cancel.", StableLineTone.Bad));
            }
            else if (deleteMode)
            {
                lines.Add(StableLine.Blank);
                lines.Add(new StableLine("DELETE MODE: press the number of the hero to delete, or D / Esc to cancel.", StableLineTone.Bad));
            }
            return lines;
        }

        internal static string Bar(int value, int max, int width)
        {
            max = Math.Max(1, max);
            int v = Math.Clamp(value, 0, max);
            int filled = (int)Math.Round((double)width * v / max);
            return $"[{new string('#', filled)}{new string('-', width - filled)}]";
        }

        internal static string HpText(Character hero)
        {
            int max = Math.Max(1, hero.GetEffectiveMaxHealth());
            return $"HP {Bar(hero.CurrentHealth, max, 20)} {Math.Clamp(hero.CurrentHealth, 0, max)}/{max}";
        }

        private static StableLineTone HpTone(Character hero)
        {
            int max = Math.Max(1, hero.GetEffectiveMaxHealth());
            int pct = hero.CurrentHealth * 100 / max;
            return pct >= 60 ? StableLineTone.Good : pct >= 30 ? StableLineTone.Alert : StableLineTone.Bad;
        }

        internal static string StatusText(StableFighter f) => f.Status switch
        {
            StableFighterStatus.AtHub => f.NeedsDecision ? "At the hub - waiting for orders" : "At the hub - heading out again",
            StableFighterStatus.Exploring => $"{f.DungeonName} L{f.DungeonLevel} - room {Math.Max(1, f.CurrentRoomNumber)}/{f.TotalRooms}",
            StableFighterStatus.AwaitingExit => $"{f.DungeonName} - room {f.RoomsCleared}/{f.TotalRooms} cleared. Press on?",
            StableFighterStatus.Dead => "DEAD",
            StableFighterStatus.InHand => "In your hands (main game)",
            _ => f.Status.ToString()
        };

        private static StableLineTone RowTone(StableFighter f) => f.Status switch
        {
            StableFighterStatus.Dead => StableLineTone.Bad,
            StableFighterStatus.InHand => StableLineTone.Dim,
            _ when f.NeedsDecision => StableLineTone.Alert,
            _ => StableLineTone.Normal
        };

        // ---------------------------------------------------------------- rendering / refresh

        private void Render()
        {
            if (stateManager.CurrentState != GameState.Stable)
                return;
            var view = BuildView();
            if (customUIManager is CanvasUICoordinator canvasUI)
            {
                try
                {
                    canvasUI.SuppressDisplayBufferRendering();
                    canvasUI.ClearDisplayBufferWithoutRender();
                    canvasUI.RenderStable(view);
                }
                catch (Exception ex)
                {
                    DebugLogger.Log("StableHandler", $"Render failed: {ex}");
                }
            }
            else
            {
                UIManager.WriteLine($"=== {view.Title} ===");
                foreach (var line in view.AllLines())
                    UIManager.WriteMenuLine(line.Text);
            }
        }

        private void OnStableChanged()
        {
            if (customUIManager is not CanvasUICoordinator canvasUI)
                return;
            Dispatcher.UIThread.Post(() =>
            {
                int waiting = stable.DecisionCount;
                bool newDecision = waiting > lastDecisionCount;
                lastDecisionCount = waiting;

                if (stateManager.CurrentState == GameState.Stable)
                {
                    QueueRender();
                }
                else if (newDecision && waiting > 0)
                {
                    // Playing someone else in hand: a quiet nudge that the Stable needs a call.
                    canvasUI.UpdateStatus($"Stable: {waiting} fighter{(waiting == 1 ? "" : "s")} waiting on you");
                }
            });
        }

        private void QueueRender()
        {
            if (renderQueued)
                return;
            renderQueued = true;
            Dispatcher.UIThread.Post(() =>
            {
                renderQueued = false;
                Render();
            }, DispatcherPriority.Background);
        }

        /// <summary>Live HP / room / enemy while the Stable is open (fights resolve between change events).</summary>
        private void StartRefreshTimer()
        {
            if (customUIManager is not CanvasUICoordinator || refreshTimer != null)
                return;
            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            refreshTimer.Tick += (_, _) =>
            {
                if (stateManager.CurrentState != GameState.Stable)
                {
                    StopRefreshTimer();
                    return;
                }
                if (stable.Fighters.Any(f => f.Status == StableFighterStatus.Exploring))
                    QueueRender();
            };
            refreshTimer.Start();
        }

        private void StopRefreshTimer()
        {
            refreshTimer?.Stop();
            refreshTimer = null;
        }
    }
}
