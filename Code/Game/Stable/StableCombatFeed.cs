namespace RPGGame
{
    using System;
    using System.Collections.Generic;
    using RPGGame.Combat.Events;

    /// <summary>
    /// A fighter's live combat log, built from <see cref="CombatEventBus"/> events (its combat text is never rendered).
    /// Hit events fire before damage lands, so each action is held until the after-action hook and written with the
    /// HP the target actually lost. Thread-safe: combat runs on the thread pool, the Stable screen reads on the UI thread.
    /// </summary>
    public sealed class StableCombatFeed
    {
        public const int Capacity = 60;

        private readonly object gate = new();
        private readonly List<StableLine> lines = new();
        private Pending? pending;
        private Actor? lastEnemy;

        private sealed class Pending
        {
            public string Source = "";
            public string ActionName = "";
            public Actor? Target;
            public int TargetHpBefore;
            public bool HeroActing;
            public bool Hit;
            public bool Miss;
            public bool Critical;
            public bool CriticalMiss;
            public bool Combo;
        }

        /// <summary>Snapshot, oldest first.</summary>
        public List<StableLine> Lines
        {
            get { lock (gate) return new List<StableLine>(lines); }
        }

        public void Add(string text, StableLineTone tone = StableLineTone.Dim)
        {
            lock (gate)
            {
                lines.Add(new StableLine(text, tone));
                if (lines.Count > Capacity)
                    lines.RemoveAt(0);
            }
        }

        /// <summary>Records one bus event for this fighter's fight.</summary>
        internal void OnEvent(CombatEvent evt, Character hero)
        {
            lock (gate)
            {
                switch (evt.Type)
                {
                    case CombatEventType.ActionExecuted:
                        var enemy = ReferenceEquals(evt.Source, hero) ? evt.Target : evt.Source;
                        if (enemy is Enemy e && !ReferenceEquals(e, lastEnemy))
                        {
                            lastEnemy = e;
                            AddLocked($"{e.Name} (L{e.Level}) steps up.", StableLineTone.Header);
                        }
                        pending = new Pending
                        {
                            Source = evt.Source.Name,
                            ActionName = evt.Action?.Name ?? "acts",
                            Target = evt.Target,
                            TargetHpBefore = (evt.Target as Character)?.CurrentHealth ?? 0,
                            HeroActing = ReferenceEquals(evt.Source, hero),
                            Critical = evt.IsCritical,
                            Combo = evt.IsCombo
                        };
                        break;
                    case CombatEventType.ActionHit:
                        if (pending != null)
                        {
                            pending.Hit = true;
                            pending.Critical |= evt.IsCritical;
                            pending.Combo |= evt.IsCombo;
                        }
                        break;
                    case CombatEventType.ActionMiss:
                        if (pending != null)
                        {
                            pending.Miss = true;
                            pending.CriticalMiss = evt.IsCriticalMiss;
                        }
                        break;
                    case CombatEventType.EnemyDied:
                        FlushLocked();
                        if (evt.Target != null)
                            AddLocked($"{evt.Target.Name} falls!", StableLineTone.Good);
                        break;
                }
            }
        }

        /// <summary>End of an action: damage has landed, write the line.</summary>
        internal void AfterAction()
        {
            lock (gate)
                FlushLocked();
        }

        private void FlushLocked()
        {
            var p = pending;
            pending = null;
            if (p == null)
                return;

            string tags = (p.Critical ? " CRIT" : "") + (p.Combo ? " combo" : "");
            if (p.Miss)
            {
                AddLocked($"{p.Source} - {p.ActionName}: {(p.CriticalMiss ? "critical miss" : "miss")}", StableLineTone.Dim);
                return;
            }

            int after = (p.Target as Character)?.CurrentHealth ?? p.TargetHpBefore;
            int delta = p.TargetHpBefore - after;
            string targetName = p.Target?.Name ?? "";
            string text = delta > 0
                ? $"{p.Source} - {p.ActionName} -> {targetName} -{delta}{tags}"
                : delta < 0
                    ? $"{p.Source} - {p.ActionName} -> {targetName} +{-delta}{tags}"
                    : $"{p.Source} - {p.ActionName}{(targetName.Length > 0 && !ReferenceEquals(p.Target, null) ? $" -> {targetName}" : "")}{tags}";
            var tone = p.HeroActing
                ? (p.Critical ? StableLineTone.Alert : StableLineTone.Normal)
                : (delta > 0 ? StableLineTone.Bad : StableLineTone.Dim);
            AddLocked(text, tone);
        }

        private void AddLocked(string text, StableLineTone tone)
        {
            lines.Add(new StableLine(text, tone));
            if (lines.Count > Capacity)
                lines.RemoveAt(0);
        }
    }
}
