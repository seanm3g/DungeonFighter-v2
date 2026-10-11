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

                for (int i = 0; i < maxActionsPerRun; i++)
                {
                    var state = session.CurrentState;
                    var current = state?.CurrentState ?? "";
                    if (current == "Death" || session.IsGameOver())
                        break;

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
