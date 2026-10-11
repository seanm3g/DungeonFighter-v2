using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.Tuning
{
    /// <summary>One fight seen during a headless playthrough.</summary>
    public sealed class PlaythroughFightRecord
    {
        public int HeroLevel { get; set; }
        public string EnemyName { get; set; } = "";
        public int EnemyLevel { get; set; }
        public int EnemyMaxHealth { get; set; }
        public int HeroHpStart { get; set; }
        public int HeroHpEnd { get; set; }
        public int HeroMaxHp { get; set; }
        public int HeroActions { get; set; }
        public int EnemyActions { get; set; }
        /// <summary>Fights since the run last entered a dungeon (1 = first fight of the dungeon).</summary>
        public int FightInDungeon { get; set; }
        public int RoomsClearedInDungeon { get; set; }
        public bool HeroDied { get; set; }
    }

    /// <summary>
    /// Watches <see cref="CombatEventBus"/> during a playthrough and turns action events into per-fight records,
    /// so a whole dungeon run inside one session action is still visible fight by fight.
    /// </summary>
    public sealed class PlaythroughFightRecorder : IDisposable
    {
        private CombatEventBus? _bus;
        private PlaythroughFightRecord? _current;
        private Enemy? _currentEnemy;
        private Character? _hero;
        private int _fightInDungeon;
        private int _roomsCleared;
        private readonly Action<string>? _trace;

        public List<PlaythroughFightRecord> Fights { get; } = new();

        /// <summary>True while the smart policy runs evaluation fights, so they are not recorded as real fights.</summary>
        public static bool Suspended { get; set; }

        /// <summary>When true, every action event is traced (RUNLOG --verbose).</summary>
        public static bool Verbose { get; set; }

        public PlaythroughFightRecorder(Action<string>? trace) => _trace = trace;

        /// <summary>
        /// Call before each session action. Always re-subscribes: new games call <c>CombatEventBus.Instance.Clear()</c>,
        /// which drops handlers without replacing the singleton.
        /// </summary>
        public void EnsureSubscribed()
        {
            var bus = CombatEventBus.Instance;
            Unsubscribe();
            _bus = bus;
            _bus.Subscribe(CombatEventType.ActionExecuted, OnAction);
            _bus.Subscribe(CombatEventType.EnemyDied, OnEnemyDied);
            _bus.Subscribe(CombatEventType.RoomCleared, OnRoomCleared);
        }

        /// <summary>Marks a new dungeon: fight and room counters restart.</summary>
        public void BeginDungeon()
        {
            _fightInDungeon = 0;
            _roomsCleared = 0;
        }

        /// <summary>Closes an open fight as a hero death (the session reached Death).</summary>
        public PlaythroughFightRecord? CloseAsDeath()
        {
            if (_current == null)
                return Fights.Count > 0 ? Fights[^1] : null;
            _current.HeroDied = true;
            Finish();
            return Fights[^1];
        }

        private void OnAction(CombatEvent evt)
        {
            if (Suspended)
                return;
            var (hero, enemy) = Resolve(evt.Source, evt.Target);
            if (hero == null || enemy == null)
                return;

            if (!ReferenceEquals(enemy, _currentEnemy))
            {
                if (_current != null)
                    Finish();
                _currentEnemy = enemy;
                _hero = hero;
                _fightInDungeon++;
                _current = new PlaythroughFightRecord
                {
                    HeroLevel = hero.Level,
                    EnemyName = enemy.Name,
                    EnemyLevel = enemy.Level,
                    EnemyMaxHealth = enemy.MaxHealth,
                    HeroHpStart = hero.CurrentHealth,
                    HeroMaxHp = hero.MaxHealth,
                    FightInDungeon = _fightInDungeon,
                    RoomsClearedInDungeon = _roomsCleared
                };
            }

            if (ReferenceEquals(evt.Source, hero))
                _current!.HeroActions++;
            else
                _current!.EnemyActions++;
            if (Verbose)
                _trace?.Invoke($"      {evt.Source?.Name,-14} {evt.Action?.Name,-16} roll {evt.RollValue,2}{(evt.IsCombo ? " combo" : "")}{(evt.IsCritical ? " crit" : "")} | hero {hero.CurrentHealth}/{hero.MaxHealth} enemy {enemy.CurrentHealth}/{enemy.MaxHealth} | swing hero {hero.GetTotalAttackSpeed():F2}s enemy {enemy.GetTotalAttackSpeed():F2}s AGI {enemy.GetEffectiveAgility()} lvl {enemy.Level} | heroPenalty {hero.RollPenalty} hitThr {RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetHitThreshold(hero)} comboThr {RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetComboThreshold(hero)}");
        }

        private void OnEnemyDied(CombatEvent evt)
        {
            if (Suspended)
                return;
            if (_current != null && (ReferenceEquals(evt.Source, _currentEnemy) || ReferenceEquals(evt.Target, _currentEnemy)))
                Finish();
        }

        private void OnRoomCleared(CombatEvent evt)
        {
            if (!Suspended)
                _roomsCleared++;
        }

        private void Finish()
        {
            if (_current == null)
                return;
            _current.HeroHpEnd = _hero?.CurrentHealth ?? 0;
            Fights.Add(_current);
            _trace?.Invoke(
                $"    fight #{_current.FightInDungeon} (room {_current.RoomsClearedInDungeon + 1}): {_current.EnemyName} L{_current.EnemyLevel} ({_current.EnemyMaxHealth} HP)"
                + $" | hero L{_current.HeroLevel} {_current.HeroHpStart}->{_current.HeroHpEnd}/{_current.HeroMaxHp}"
                + $" | actions hero {_current.HeroActions} enemy {_current.EnemyActions}"
                + (_current.HeroDied ? " | HERO DIED" : ""));
            _current = null;
            _currentEnemy = null;
        }

        private static (Character? hero, Enemy? enemy) Resolve(Actor? a, Actor? b)
        {
            Character? hero = a is Character ca && a is not Enemy ? ca : b is Character cb && b is not Enemy ? cb : null;
            Enemy? enemy = a as Enemy ?? b as Enemy;
            return (hero, enemy);
        }

        private void Unsubscribe()
        {
            if (_bus == null)
                return;
            _bus.Unsubscribe(CombatEventType.ActionExecuted, OnAction);
            _bus.Unsubscribe(CombatEventType.EnemyDied, OnEnemyDied);
            _bus.Unsubscribe(CombatEventType.RoomCleared, OnRoomCleared);
            _bus = null;
        }

        public void Dispose() => Unsubscribe();
    }
}
