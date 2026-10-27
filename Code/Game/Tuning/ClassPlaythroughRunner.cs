using System;
using System.Threading.Tasks;
using RPGGame.Game;
using RPGGame.MCP.Models;

namespace RPGGame.Tuning
{
    /// <summary>
    /// Runs one headless full-game playthrough until Death or max actions.
    /// </summary>
    public static class ClassPlaythroughRunner
    {
        /// <summary>When true (default), the bot equips bag upgrades at the hub between dungeons.</summary>
        public static bool EquipGear { get; set; } = true;

        /// <summary>When true, the bot uses <see cref="PlaythroughSmartPolicy"/> (skills, gear and sequence chosen by simulation).</summary>
        public static bool SmartPolicy { get; set; }

        /// <summary>When &gt; 0, a run stops successfully once the hero reaches this level.</summary>
        public static int StopAtLevel { get; set; }

        /// <summary>
        /// When &gt; 0, the bot leaves a dungeon between rooms once HP is below this % of max (leaving fully heals;
        /// the dungeon counts as not cleared). 0 = never leave.
        /// </summary>
        public static int RetreatBelowHpPercent { get; set; }

        /// <summary>
        /// When &gt; 1, the hero is raised to this level at the first hub screen (normal level-up rules, no banners) and
        /// handed the loot a player would have collected on the way (<see cref="StartLootLevels"/> levels of
        /// <see cref="StartLootPerLevel"/> drops). Lets survival / clear rate be measured from mid-game without playing from L1.
        /// </summary>
        public static int StartAtLevel { get; set; }

        /// <summary>When true, XP is reset at every dungeon entry so the hero stays at its level (DUNGEONSIM).</summary>
        public static bool HoldLevel { get; set; }

        /// <summary>When &gt; 0, a run stops successfully after this many dungeons (DUNGEONSIM).</summary>
        public static int MaxDungeons { get; set; }

        public static int StartLootLevels { get; set; } = 8;
        /// <summary>
        /// Drops per level in the starting bag; 0 = by level, max(4, level / 2). Calibrated against heroes who played up
        /// from L1 (2026-10-12: 4 matches L10, 8 matches L15; unverified above L15).
        /// </summary>
        public static int StartLootPerLevel { get; set; }

        public static int ResolveStartLootPerLevel(int level) =>
            StartLootPerLevel > 0 ? StartLootPerLevel : Math.Max(4, (int)Math.Round(level / 2.0));

        /// <summary>Raises the hero to <see cref="StartAtLevel"/> and fills the bag with drops from the levels just below it.</summary>
        private static void ApplyStartLevel(Character hero)
        {
            if (hero.Level < StartAtLevel)
                hero.ApplyActionLabLevelDelta(StartAtLevel - hero.Level);
            // A hero at this level would have travelled: use the region whose level band holds it (themes, enemy pools).
            var region = new TravelRegionCatalog().GetAllRegions().FirstOrDefault(r => r.ContainsLevel(StartAtLevel));
            if (region != null)
                hero.CurrentRegionId = region.Id;
            for (int lvl = Math.Max(1, StartAtLevel - StartLootLevels); lvl < StartAtLevel; lvl++)
            {
                for (int n = 0; n < ResolveStartLootPerLevel(StartAtLevel); n++)
                {
                    var item = LootGenerator.GenerateLoot(lvl, lvl, null, guaranteedLoot: true);
                    if (item != null)
                        hero.AddToInventory(item);
                }
            }
            hero.CurrentHealth = hero.GetEffectiveMaxHealth();
        }

        public static async Task<PlaythroughRunResult> RunAsync(
            WeaponType weaponType,
            int weaponMenuSlot,
            string classDisplayName,
            int maxActionsPerRun = 500,
            Action<string>? trace = null)
        {
            var result = new PlaythroughRunResult
            {
                WeaponType = weaponType,
                WeaponMenuSlot = weaponMenuSlot,
                ClassDisplayName = classDisplayName
            };

            GamePlaySession? session = null;
            using var recorder = new PlaythroughFightRecorder(trace);
            var smart = SmartPolicy ? new PlaythroughSmartPolicy(trace) : null;
            try
            {
                // DUNGEONSIM measures one level: offer its dungeons at exactly that level, not anywhere in the region band.
                if (HoldLevel && StartAtLevel > 1)
                    DungeonManagerWithRegistry.HeadlessFixedDungeonLevel = StartAtLevel;
                if (RetreatBelowHpPercent > 0)
                {
                    DungeonExitChoiceHandler.HeadlessExitPolicy = hero =>
                    {
                        bool leave = hero != null && hero.CurrentHealth * 100 < RetreatBelowHpPercent * hero.GetEffectiveMaxHealth();
                        if (leave)
                        {
                            result.DungeonsRetreated++;
                            recorder.MarkRetreat();
                            trace?.Invoke($"    retreat at {hero!.CurrentHealth}/{hero.GetEffectiveMaxHealth()} HP");
                        }
                        return leave;
                    };
                }
                session = new GamePlaySession();
                await session.Initialize();
                await session.StartNewGame();

                int dungeonsAttempted = 0;
                int dungeonsCompleted = 0;
                var levelTurnTracker = new PlaythroughLevelTurnTracker();
                var death = new PlaythroughDeathContext();
                string lastTraceKey = "";
                string lastEnemyKey = "";
                string lastDungeonKey = "";
                string lastGearState = "";
                bool startLevelApplied = StartAtLevel <= 1;

                for (int i = 0; i < maxActionsPerRun; i++)
                {
                    var state = session.CurrentState;
                    var current = state?.CurrentState ?? "";
                    if (current == "Death" || session.IsGameOver())
                        break;

                    if (MaxDungeons > 0 && dungeonsAttempted >= MaxDungeons
                        && current is "GameLoop" or "DungeonSelection" or "DungeonCompletion")
                    {
                        result.StopReason = PlaythroughStopReason.ReachedLevel;
                        break;
                    }

                    int levelAtAction = state?.Player?.Level ?? 1;
                    if (StopAtLevel > 0 && levelAtAction >= StopAtLevel)
                    {
                        result.StopReason = PlaythroughStopReason.ReachedLevel;
                        break;
                    }
                    levelTurnTracker.RecordAction(levelAtAction);
                    TrackDeathContext(state, death, ref lastEnemyKey, ref lastDungeonKey, dungeonsCompleted);
                    if (trace != null)
                    {
                        string line = FormatTraceLine(state);
                        string key = line;
                        if (key != lastTraceKey)
                            trace(line);
                        lastTraceKey = key;
                    }

                    if (!startLevelApplied && (current == "GameLoop" || current == "DungeonSelection")
                        && session.Player is { } startHero && startHero.Weapon != null)
                    {
                        ApplyStartLevel(startHero);
                        startLevelApplied = true;
                        trace?.Invoke($"    start level L{startHero.Level}: bag {startHero.Inventory.Count} items");
                    }

                    // Between dungeons the bot goes Completion -> Selection without visiting the hub, so check gear on all three screens.
                    if (EquipGear && (current == "GameLoop" || current == "DungeonSelection") && current != lastGearState)
                    {
                        int n = 0;
                        if (smart != null)
                            await smart.OptimizeAsync(session.Player, weaponType);
                        else
                            n = PlaythroughGearPolicy.EquipUpgrades(session.Player, weaponType);
                        result.UpgradesEquipped += n;
                        if (n > 0)
                            trace?.Invoke($"    equipped {n} upgrade(s)");
                        var hero = session.Player;
                        if (trace != null && hero != null)
                            trace($"    gear: W[{hero.Weapon?.Name} {Fmt(hero.Weapon)}] H[{Fmt(hero.Head)}] B[{Fmt(hero.Body)}] L[{Fmt(hero.Legs)}] F[{Fmt(hero.Feet)}] | bag {hero.Inventory.Count}");
                    }

                    lastGearState = current;
                    string action = ClassPlaythroughPolicy.PickAction(current, weaponMenuSlot);
                    bool enteringDungeon = ClassPlaythroughPolicy.IsDungeonEntry(current, action);
                    if (enteringDungeon)
                    {
                        if (HoldLevel && session.Player is { } heldHero)
                            heldHero.XP = 0;
                        dungeonsAttempted++;
                        recorder.BeginDungeon();
                        death.HpAtDungeonEntry = state?.Player?.CurrentHealth ?? 0;
                        death.MaxHpAtDungeonEntry = state?.Player?.MaxHealth ?? 0;
                        death.DungeonsCompletedBefore = dungeonsCompleted;
                    }
                    recorder.EnsureSubscribed();

                    await session.ExecuteAction(action);

                    var after = session.CurrentState?.CurrentState ?? "";
                    if (enteringDungeon && after == "DungeonCompletion")
                        dungeonsCompleted++;

                    if (after == "Death" || session.IsGameOver())
                        break;
                }

                var final = session.CurrentState;
                result.TurnCount = session.TurnCount;
                result.ActionsByLevel = levelTurnTracker.ToDictionary();
                result.DungeonsAttempted = dungeonsAttempted;
                result.DungeonsCompleted = dungeonsCompleted;
                result.FinalState = final?.CurrentState;
                result.ReachedDeath = final?.CurrentState == "Death";
                if (result.StopReason != PlaythroughStopReason.ReachedLevel || result.ReachedDeath)
                    result.StopReason = result.ReachedDeath
                        ? PlaythroughStopReason.Death
                        : PlaythroughStopReason.MaxActions;
                if (result.ReachedDeath)
                {
                    var fatal = recorder.CloseAsDeath();
                    if (fatal != null)
                    {
                        death.EnemyName = fatal.EnemyName;
                        death.EnemyLevel = fatal.EnemyLevel;
                        death.FightsInDungeon = fatal.FightInDungeon;
                        death.RoomNumber = fatal.RoomsClearedInDungeon + 1;
                        death.Level = fatal.HeroLevel;
                    }
                    result.Death = death;
                }
                result.Fights = recorder.Fights;
                if (smart != null)
                {
                    result.UpgradesEquipped = smart.GearChanges;
                    result.SmartSkillsLearned = smart.SkillsLearned;
                    result.SmartSequenceChanges = smart.SequenceChanges;
                    result.SmartEvaluations = smart.Evaluations;
                    result.SequenceDecisions = smart.SequenceDecisions;
                    result.SkillDecisions = smart.SkillDecisions;
                    result.GearDecisions = smart.GearDecisions;
                    result.Milestones = smart.Milestones;
                }
                trace?.Invoke(result.ReachedDeath
                    ? $"DIED at L{death.Level} in room {death.RoomNumber} vs {death.EnemyName} L{death.EnemyLevel} | fight #{death.FightsInDungeon} in this dungeon | entered dungeon at {death.HpAtDungeonEntry}/{death.MaxHpAtDungeonEntry} HP"
                    : $"Stopped ({result.StopReason}) at state {result.FinalState}");

                if (final?.Player != null)
                {
                    var player = final.Player;
                    result.FinalLevel = player.Level;
                    result.FinalHealth = player.CurrentHealth;
                    result.FinalMaxHealth = player.MaxHealth;
                    result.InventoryCount = player.Inventory?.Count ?? 0;
                    result.TotalXp = player.XP;
                }
            }
            catch (Exception ex)
            {
                result.StopReason = PlaythroughStopReason.Error;
                result.ErrorMessage = ex.Message;
            }
            finally
            {
                DungeonExitChoiceHandler.HeadlessExitPolicy = null;
                DungeonManagerWithRegistry.HeadlessFixedDungeonLevel = null;
                session?.Dispose();
            }

            return result;
        }

        private static string Fmt(Item? item) =>
            item == null ? "-" : $"{item.Rarity[0]}T{item.Tier}:{RPGGame.Tuning.Profiles.ItemValueModel.Score(item):F1}";

        private static void TrackDeathContext(
            GameStateSnapshot? state, PlaythroughDeathContext death, ref string lastEnemyKey, ref string lastDungeonKey, int dungeonsCompleted)
        {
            if (state?.Player == null)
                return;
            death.Level = state.Player.Level;
            var dungeon = state.CurrentDungeon;
            if (dungeon != null)
            {
                string dungeonKey = $"{dungeon.Name}|{dungeon.Level}|{dungeon.TotalRooms}";
                if (dungeonKey != lastDungeonKey)
                    lastDungeonKey = dungeonKey;
                death.DungeonName = dungeon.Name;
                death.DungeonLevel = dungeon.Level;
                death.RoomNumber = dungeon.CurrentRoomNumber;
                death.TotalRooms = dungeon.TotalRooms;
            }

            var enemy = state.Combat?.CurrentEnemy;
            if (enemy != null)
            {
                string enemyKey = $"{dungeon?.CurrentRoomNumber}|{enemy.Name}|{enemy.MaxHealth}";
                if (enemyKey != lastEnemyKey)
                {
                    lastEnemyKey = enemyKey;
                    death.FightsInDungeon++;
                }
                death.EnemyName = enemy.Name;
                death.EnemyLevel = enemy.Level;
            }
        }

        private static string FormatTraceLine(GameStateSnapshot? state)
        {
            if (state == null)
                return "(no state)";
            var p = state.Player;
            string hero = p == null ? "" : $"L{p.Level} HP {p.CurrentHealth}/{p.MaxHealth}";
            string dungeon = state.CurrentDungeon == null ? "" :
                $" | {state.CurrentDungeon.Name} L{state.CurrentDungeon.Level} room {state.CurrentDungeon.CurrentRoomNumber}/{state.CurrentDungeon.TotalRooms}";
            string room = state.CurrentRoom == null ? "" : $" ({state.CurrentRoom.EnemyCount} enemies)";
            var e = state.Combat?.CurrentEnemy;
            string enemy = e == null ? "" : $" | vs {e.Name} L{e.Level} {e.CurrentHealth}/{e.MaxHealth}";
            return $"{state.CurrentState,-20} {hero}{dungeon}{room}{enemy}";
        }
    }
}
