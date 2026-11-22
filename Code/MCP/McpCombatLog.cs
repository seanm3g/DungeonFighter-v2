using System;
using System.Collections.Generic;
using RPGGame.Combat.Events;

namespace RPGGame.MCP
{
    /// <summary>
    /// Compact combat log for headless (MCP) play. The canvas draws combat itself, so without it a dungeon run
    /// reaches an agent as a few room lines. This listens to <see cref="CombatEventBus"/> and writes one line per
    /// swing (who, which action, roll, outcome, both health bars after it) plus statuses, kills and room clears
    /// into <see cref="OutputCapture"/>.
    /// </summary>
    public sealed class McpCombatLog
    {
        private readonly OutputCapture _output;
        private readonly Func<Character?> _hero;
        private readonly Action<CombatEvent> _onExecuted, _onHit, _onMiss, _onStatus, _onDied, _onRoomCleared;
        private Pending? _pending;
        private readonly List<string> _statuses = new();
        private readonly List<string> _ticks = new();

        private sealed class Pending
        {
            public Actor Source = null!;
            public Actor? Target;
            public string ActionName = "";
            public int Roll, Natural;
            public bool Combo, Crit, Hit, Miss, ChainBreak;
        }

        /// <summary>Rooms cleared since the last <see cref="ResetRoomCount"/> (one dungeon run).</summary>
        public int RoomsCleared { get; private set; }

        public void ResetRoomCount() => RoomsCleared = 0;

        public McpCombatLog(OutputCapture output, Func<Character?> hero)
        {
            _output = output;
            _hero = hero;
            _onExecuted = OnExecuted;
            _onHit = e => { if (Matches(e)) { _pending!.Hit = true; _pending.Combo |= e.IsCombo; _pending.Crit |= e.IsCritical; if (_pending.Natural == 0) _pending.Natural = e.NaturalRollValue; } };
            _onMiss = e => { if (Matches(e)) { _pending!.Miss = true; _pending.ChainBreak |= e.IsChainBreak; } };
            _onStatus = OnStatus;
            // The game already writes "<enemy> has been defeated!"; just close the last swing first.
            _onDied = _ => Flush();
            _onRoomCleared = e => { Flush(); RoomsCleared++; _output.WriteLine("  Room cleared."); };
            // Bleed / Poison / Burn ticks: named on the swing line they land in, so HP never drops with no cause shown.
            RPGGame.CombatEffectsSimplified.DoTTicked += (actor, kind, damage) =>
            {
                string who = actor is Enemy ? actor.Name : actor is Character ? "Hero" : actor?.Name ?? "?";
                _ticks.Add($"{kind} -{damage} on {who}");
            };
        }

        /// <summary>
        /// Re-subscribes before each input: starting a new game calls <c>CombatEventBus.Instance.Clear()</c>, which drops
        /// handlers without replacing the bus.
        /// </summary>
        public void EnsureSubscribed()
        {
            var bus = CombatEventBus.Instance;
            bus.Unsubscribe(CombatEventType.ActionExecuted, _onExecuted);
            bus.Unsubscribe(CombatEventType.ActionHit, _onHit);
            bus.Unsubscribe(CombatEventType.ActionMiss, _onMiss);
            bus.Unsubscribe(CombatEventType.StatusEffectApplied, _onStatus);
            bus.Unsubscribe(CombatEventType.EnemyDied, _onDied);
            bus.Unsubscribe(CombatEventType.RoomCleared, _onRoomCleared);
            bus.Subscribe(CombatEventType.ActionExecuted, _onExecuted);
            bus.Subscribe(CombatEventType.ActionHit, _onHit);
            bus.Subscribe(CombatEventType.ActionMiss, _onMiss);
            bus.Subscribe(CombatEventType.StatusEffectApplied, _onStatus);
            bus.Subscribe(CombatEventType.EnemyDied, _onDied);
            bus.Subscribe(CombatEventType.RoomCleared, _onRoomCleared);
        }

        private bool Matches(CombatEvent e) =>
            _pending != null && ReferenceEquals(e.Source, _pending.Source) && string.Equals(e.Action?.Name ?? "", _pending.ActionName, StringComparison.Ordinal);

        private void OnExecuted(CombatEvent e)
        {
            // A swing's hit/miss events can come before or after this one; group them by source and action, and write the
            // line when the next swing starts (health is then final for this one).
            if (_pending != null && Matches(e))
            {
                _pending.Roll = e.RollValue;
                if (e.NaturalRollValue > 0) _pending.Natural = e.NaturalRollValue;
                _pending.Combo |= e.IsCombo;
                _pending.Crit |= e.IsCritical;
                _pending.Target ??= e.Target;
                return;
            }
            Flush();
            _pending = new Pending
            {
                Source = e.Source,
                Target = e.Target,
                ActionName = string.IsNullOrWhiteSpace(e.Action?.Name) ? "" : e.Action!.Name,
                Roll = e.RollValue,
                Natural = e.NaturalRollValue,
                Combo = e.IsCombo,
                Crit = e.IsCritical
            };
        }

        private void OnStatus(CombatEvent e)
        {
            if (string.IsNullOrEmpty(e.StatusEffectType))
                return;
            string on = e.Target?.Name ?? e.Source?.Name ?? "?";
            _statuses.Add($"{e.StatusEffectType} on {on}");
        }

        /// <summary>Writes the pending swing line (call at the end of each input as well).</summary>
        public void Flush()
        {
            var p = _pending;
            _pending = null;
            if (p == null)
                return;

            var hero = _hero();
            string who = p.Source is Enemy ? p.Source.Name : p.Source is Character ? "Hero" : $"[Room] {p.Source?.Name}";
            string outcome = p.Miss && !p.Hit ? (p.ChainBreak ? "chain ends" : "miss") : p.Crit ? "CRIT" : p.Combo ? "COMBO" : p.Hit ? "hit" : "";
            // The die face, then the total when bonuses move it ("roll 1 (total 9)"): a bare total read as a natural 1 that hit.
            string roll = p.Natural > 0
                ? $" roll {p.Natural}{(p.Roll != p.Natural ? $" (total {p.Roll})" : "")}"
                : p.Roll > 0 ? $" roll {p.Roll}" : "";
            string actionName = p.ActionName.Length > 0 ? p.ActionName : "attack";
            var parts = new List<string> { $"{who}: {actionName}{roll}{(outcome.Length > 0 ? " " + outcome : "")}" };

            Enemy? foe = p.Source as Enemy ?? p.Target as Enemy;
            if (foe != null)
                parts.Add($"{foe.Name} {Math.Max(0, foe.CurrentHealth)}/{foe.MaxHealth}");
            if (hero != null)
                parts.Add($"Hero {Math.Max(0, hero.CurrentHealth)}/{hero.GetEffectiveMaxHealth()}");
            if (_statuses.Count > 0)
            {
                parts.Add("applied " + string.Join(", ", _statuses));
                _statuses.Clear();
            }
            if (_ticks.Count > 0)
            {
                parts.Add(string.Join(", ", _ticks));
                _ticks.Clear();
            }
            _output.WriteLine("  " + string.Join(" | ", parts));
        }
    }
}
