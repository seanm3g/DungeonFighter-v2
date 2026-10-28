using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Analytics;
using RPGGame.Combat.Events;
using RPGGame.Data;

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
        /// <summary>The hero left the dungeon early after this fight (retreat; the dungeon is not cleared).</summary>
        public bool RetreatedAfter { get; set; }
        /// <summary>Hero combo strip when the fight started.</summary>
        public List<string> Strip { get; set; } = new();
        /// <summary>Hero actions fired in the fight (action name → count).</summary>
        public Dictionary<string, int> ActionUses { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Lowest hero HP during the fight, as a share of max HP.</summary>
        public double HpMinPct { get; set; } = 1;
        /// <summary>Times the lead (hero HP % vs enemy HP %) changed hands.</summary>
        public int LeadChanges { get; set; }
        public int HeroMisses { get; set; }
        public int LongestMissStreak { get; set; }
        /// <summary>Hero hits, and the combo ones among them (hit size and combo lift reads).</summary>
        /// <summary>The final-room boss (dungeonScaling.bossMergesRoom).</summary>
        public bool BossFight { get; set; }
        public int HeroHits { get; set; }
        public int ComboHits { get; set; }
        /// <summary>HP the enemy lost by source: hit, dot, retrigger, reflect (during an enemy action), environment.</summary>
        public Dictionary<string, int> DamageBySource { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>HP the hero lost by source: hit (enemy swing), dot, retrigger, self (during its own action), environment.</summary>
        public Dictionary<string, int> HeroDamageBySource { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Parts of hit damage from crits, combo hits and convert actions (subsets of "hit"; they overlap).</summary>
        public int CritDamage { get; set; }
        public int ComboDamage { get; set; }
        public int ConvertDamage { get; set; }
        /// <summary>Hero swings fired from each strip slot (0-based, the AMP exponent). Completion = last / first.</summary>
        public List<int> SlotSwings { get; set; } = new();
        /// <summary>Enemy HP lost to hero hits, by the strip slot that dealt them (retrigger, DoT and reflect excluded).</summary>
        public List<int> SlotHitDamage { get; set; } = new();
        /// <summary>Hero strip slot max, effective TECH and INT when the fight started.</summary>
        public int SlotMax { get; set; }
        public int HeroTechnique { get; set; }
        public int HeroIntelligence { get; set; }
        /// <summary>
        /// Times a strip under way (step past slot 1) dropped back to slot 1, by cause: "normal hit" / "miss" (the hero's
        /// own swing), "routing" (a combo hit whose routing stopped the chain), "enemy" / "environment" (reset between hero
        /// swings while that side acted, e.g. Disrupt), "other" (reset between hero swings with no action in between).
        /// "strip change" counts swings the recorded strip no longer matches. Finishing the last slot is not a break.
        /// </summary>
        public Dictionary<string, int> StripBreaks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Strips run through the last slot on a combo hit.</summary>
        public int StripCompletions { get; set; }
        /// <summary>The hero's first swing of the fight was past slot 1 (strip carried in from the previous enemy).</summary>
        public bool CarriedInStrip { get; set; }
        /// <summary>Worn loadout at fight start: slot → "Name|Material|T{tier}|Rarity" (empty slots left out).</summary>
        public Dictionary<string, string> Gear { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Material → equipped pieces counting toward it (wildcards included), for materials worn 2+ times.</summary>
        public Dictionary<string, int> Sets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Learned skill node → rank at fight start.</summary>
        public Dictionary<string, int> Skills { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public int HeroStrength { get; set; }
        public int HeroAgility { get; set; }
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
        private readonly Action<Actor, int, string> _onDamage;
        private enum Role { Hero, Enemy, Other }
        private Role _lastRole;
        private bool _hitCrit, _hitCombo, _hitConvert;
        private int _missStreak, _lead;
        private int _slot = -1;
        // Strip-break tracking: the hero's step before its swing, the swing's outcome, the step seen after it, and who
        // acted between hero swings.
        private bool _pendingHero, _heroMissed, _heroHit, _heroCombo;
        private int _heroStepBefore, _heroSlotCount, _stepAfterHero = -1;
        private Role? _between;
        private static readonly Dictionary<string, bool> ConvertActions = new(StringComparer.OrdinalIgnoreCase);

        public List<PlaythroughFightRecord> Fights { get; } = new();

        /// <summary>True while the smart policy runs evaluation fights, so they are not recorded as real fights.</summary>
        public static bool Suspended { get; set; }

        /// <summary>When true, every action event is traced (RUNLOG --verbose).</summary>
        public static bool Verbose { get; set; }

        public PlaythroughFightRecorder(Action<string>? trace)
        {
            _trace = trace;
            _onDamage = OnDamage;
        }

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
            _bus.Subscribe(CombatEventType.ActionHit, OnHit);
            _bus.Subscribe(CombatEventType.ActionMiss, OnMiss);
            DamageLedger.Report = _onDamage;
        }

        /// <summary>Marks a new dungeon: fight and room counters restart.</summary>
        public void BeginDungeon()
        {
            _fightInDungeon = 0;
            _roomsCleared = 0;
        }

        /// <summary>Marks the last finished fight as the one the hero retreated after.</summary>
        public void MarkRetreat()
        {
            if (_current != null)
                Finish();
            if (Fights.Count > 0)
                Fights[^1].RetreatedAfter = true;
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
            _lastRole = evt.Source is not Character ? Role.Other : evt.Source is Enemy ? Role.Enemy : Role.Hero;
            _hitCrit = _hitCombo = _hitConvert = false;
            _slot = -1;
            var (hero, enemy) = Resolve(evt.Source, evt.Target);
            if (hero == null || enemy == null)
                return;
            if (_current != null && ReferenceEquals(hero, _hero))
            {
                ResolveHeroSwing();
                if (_lastRole != Role.Hero && _stepAfterHero > 0)
                    _between ??= _lastRole;
            }

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
                    BossFight = enemy is Enemy { IsBoss: true },
                    EnemyMaxHealth = enemy.MaxHealth,
                    HeroHpStart = hero.CurrentHealth,
                    HeroMaxHp = hero.GetEffectiveMaxHealth(),
                    FightInDungeon = _fightInDungeon,
                    RoomsClearedInDungeon = _roomsCleared,
                    Strip = hero.GetComboActions().Select(a => a.Name).ToList(),
                    SlotMax = ComboSequenceMaxHelper.GetEffectiveMax(hero),
                    HeroTechnique = hero.GetEffectiveTechnique(),
                    HeroIntelligence = hero.GetEffectiveIntelligence(),
                    HeroStrength = hero.GetEffectiveStrength(),
                    HeroAgility = hero.GetEffectiveAgility()
                };
                RecordLoadout(_current, hero);
                _current.SlotSwings = new List<int>(new int[_current.Strip.Count]);
                _current.SlotHitDamage = new List<int>(new int[_current.Strip.Count]);
                _missStreak = 0;
                _lead = 0;
                _pendingHero = false;
                _stepAfterHero = -1;
                _between = null;
                UpdatePacing();
            }

            if (ReferenceEquals(evt.Source, hero))
            {
                _current!.HeroActions++;
                string? name = evt.Action?.Name;
                if (!string.IsNullOrWhiteSpace(name))
                    _current.ActionUses[name] = _current.ActionUses.GetValueOrDefault(name) + 1;
                // Slot as the AMP exponent sees it (strip changed mid-fight: outside the recorded strip, skipped).
                var strip = hero.GetComboActions();
                if (evt.Action != null)
                {
                    int slot = ActionUtilities.TryGetComboActionSlotIndex(evt.Action, strip);
                    if (slot >= 0 && slot < _current.SlotSwings.Count)
                    {
                        _slot = slot;
                        _current.SlotSwings[slot]++;
                    }
                }
                int count = strip.Count;
                if (count != _current.Strip.Count || !strip.Select(a => a.Name).SequenceEqual(_current.Strip))
                    Count(_current.StripBreaks, "strip change");
                if (_current.HeroActions == 1 && count > 0 && hero.ComboStep % count != 0)
                    _current.CarriedInStrip = true;
                // Reset between hero swings (the step seen after the last swing is gone before this one).
                if (_stepAfterHero > 0 && hero.ComboStep == 0)
                    Count(_current.StripBreaks, _between == Role.Enemy ? "enemy" : _between == Role.Other ? "environment" : "other");
                _heroStepBefore = hero.ComboStep;
                _heroSlotCount = count;
                _heroMissed = _heroHit = _heroCombo = false;
                _between = null;
                _pendingHero = true;
            }
            else
                _current!.EnemyActions++;
            if (Verbose)
                _trace?.Invoke($"      {evt.Source?.Name,-14} {evt.Action?.Name,-16} roll {evt.RollValue,2}{(evt.IsCombo ? " combo" : "")}{(evt.IsCritical ? " crit" : "")} | hero {hero.CurrentHealth}/{hero.GetEffectiveMaxHealth()} enemy {enemy.CurrentHealth}/{enemy.MaxHealth} | swing hero {hero.GetTotalAttackSpeed():F2}s enemy {enemy.GetTotalAttackSpeed():F2}s AGI {enemy.GetEffectiveAgility()} lvl {enemy.Level} | heroPenalty {hero.RollPenalty} hitThr {RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetHitThreshold(hero)} comboThr {RPGGame.Actions.RollModification.RollModificationManager.GetThresholdManager().GetComboThreshold(hero)}");
        }

        private void OnHit(CombatEvent evt)
        {
            if (Suspended || _current == null || !ReferenceEquals(evt.Source, _hero))
                return;
            _missStreak = 0;
            _current.HeroHits++;
            if (evt.IsCombo)
                _current.ComboHits++;
            _heroHit = true;
            _heroCombo = evt.IsCombo;
            _hitCrit = evt.IsCritical;
            _hitCombo = evt.IsCombo;
            _hitConvert = IsConvert(evt.Action?.Name);
        }

        private void OnMiss(CombatEvent evt)
        {
            if (Suspended || _current == null || !ReferenceEquals(evt.Source, _hero))
                return;
            _current.HeroMisses++;
            _current.LongestMissStreak = Math.Max(_current.LongestMissStreak, ++_missStreak);
            _heroMissed = true;
        }

        private static void RecordLoadout(PlaythroughFightRecord record, Character hero)
        {
            var eq = hero.Equipment;
            if (eq != null)
            {
                foreach (var (slot, item) in new[] { ("Head", eq.Head), ("Body", eq.Body), ("Legs", eq.Legs), ("Feet", eq.Feet), ("Weapon", eq.Weapon), ("Charm", eq.Charm) })
                {
                    if (item != null)
                        record.Gear[slot] = $"{item.Name}|{item.Material}|T{item.Tier}|{item.Rarity}";
                }
            }
            foreach (var material in MaterialSetController.EnumerateEquipped(hero)
                         .Where(i => i != null && !string.IsNullOrWhiteSpace(i.Material))
                         .Select(i => ItemMaterialRules.RemapLegacyMaterial(i!.Material))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                int n = MaterialSetController.CountEquipped(hero, material);
                if (n >= 2)
                    record.Sets[material] = n;
            }
            foreach (var (id, rank) in hero.Progression.LearnedSkillRanks)
            {
                if (rank > 0)
                    record.Skills[id] = rank;
            }
        }

        /// <summary>
        /// First event after a hero swing: its outcome has resolved, so read where the strip went. A drop to step 0 from a
        /// strip under way is a break (by the swing's outcome) unless the swing was the last slot landing as a combo, which
        /// wraps the strip.
        /// </summary>
        private void ResolveHeroSwing()
        {
            if (!_pendingHero || _current == null || _hero == null)
                return;
            _pendingHero = false;
            int after = _hero.ComboStep;
            int count = _heroSlotCount;
            bool lastSlot = count > 0 && _heroStepBefore % count == count - 1;
            if (lastSlot && _heroHit && _heroCombo && count > 1)
                _current.StripCompletions++;
            else if (_heroStepBefore > 0 && after == 0)
                Count(_current.StripBreaks, _heroMissed ? "miss" : !_heroHit ? "other" : _heroCombo ? "routing" : "normal hit");
            _stepAfterHero = after;
        }

        private static void Count(Dictionary<string, int> counts, string key) =>
            counts[key] = counts.GetValueOrDefault(key) + 1;

        /// <summary>HP actually lost by any actor (<see cref="DamageLedger"/>); attributes enemy HP loss to a source.</summary>
        private void OnDamage(Actor target, int lost, string source)
        {
            if (Suspended || _current == null || lost == 0)
                return;
            if (lost > 0 && ReferenceEquals(target, _currentEnemy))
            {
                string key = source != DamageLedger.Hit ? source
                    : _lastRole == Role.Other ? "environment"
                    : _lastRole == Role.Enemy ? "reflect"
                    : DamageLedger.Hit;
                _current.DamageBySource[key] = _current.DamageBySource.GetValueOrDefault(key) + lost;
                if (key == DamageLedger.Hit)
                {
                    if (_hitCrit) _current.CritDamage += lost;
                    if (_hitCombo) _current.ComboDamage += lost;
                    if (_hitConvert) _current.ConvertDamage += lost;
                    if (_lastRole == Role.Hero && _slot >= 0) _current.SlotHitDamage[_slot] += lost;
                }
            }
            else if (lost > 0 && ReferenceEquals(target, _hero))
            {
                // Hit during the hero's own action = self-damage (costs, reflect); otherwise by who acted last.
                string key = source != DamageLedger.Hit ? source
                    : _lastRole == Role.Other ? "environment"
                    : _lastRole == Role.Hero ? "self"
                    : DamageLedger.Hit;
                _current.HeroDamageBySource[key] = _current.HeroDamageBySource.GetValueOrDefault(key) + lost;
            }
            UpdatePacing();
        }

        /// <summary>Lowest hero HP and lead changes (whose HP share is higher), after every HP change.</summary>
        private void UpdatePacing()
        {
            if (_current == null || _hero == null || _currentEnemy == null)
                return;
            int heroMax = _hero.GetEffectiveMaxHealth();
            double heroPct = heroMax > 0 ? _hero.CurrentHealth / (double)heroMax : 0;
            double enemyPct = _currentEnemy.MaxHealth > 0 ? _currentEnemy.CurrentHealth / (double)_currentEnemy.MaxHealth : 0;
            _current.HpMinPct = Math.Min(_current.HpMinPct, Math.Max(0, heroPct));
            int sign = Math.Sign(heroPct - enemyPct);
            if (sign != 0 && _lead != 0 && sign != _lead)
                _current.LeadChanges++;
            if (sign != 0)
                _lead = sign;
        }

        private static bool IsConvert(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            lock (ConvertActions)
            {
                if (!ConvertActions.TryGetValue(name, out bool isConvert))
                    ConvertActions[name] = isConvert = MaterialBuildsLoader.FindByConvertAction(name) != null;
                return isConvert;
            }
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
            ResolveHeroSwing();
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
            _bus.Unsubscribe(CombatEventType.ActionHit, OnHit);
            _bus.Unsubscribe(CombatEventType.ActionMiss, OnMiss);
            if (DamageLedger.Report == _onDamage)
                DamageLedger.Report = null;
            _bus = null;
        }

        public void Dispose() => Unsubscribe();
    }
}
