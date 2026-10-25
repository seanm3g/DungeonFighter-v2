using System.Collections.Generic;

namespace RPGGame.Data.ActionModel
{
    // Draft action schema: every action is a strike chassis plus three primitive lists.
    // Not wired into runtime; ActionModelConverter reads Actions.json rows into it so the
    // shape can be checked against the live data before anything depends on it.

    /// <summary>Who an effect lands on.</summary>
    public enum ModTarget { Self, Enemy }

    /// <summary>How long an effect lasts. Instant = applied once and gone (heals, one-shot trigger grants).</summary>
    public enum ScopeKind { Instant, Hit, Action, Turn, Fight, Dungeon }

    /// <summary>
    /// The number a <see cref="Modifier"/> changes. Hero/enemy side comes from <see cref="Modifier.Target"/>,
    /// so there is one entry per quantity rather than heroX / enemyX pairs.
    /// </summary>
    public enum Quantity
    {
        // Attributes
        STR, AGI, TECH, INT,
        // Roll thresholds
        Accuracy, Hit, Combo, Crit, CritMiss,
        // Per-action multipliers (percent points)
        DamagePercent, SpeedPercent, AmpPercent,
        // Flat
        Hits, WeaponDamage, WeaponSpeed, DamageFlat, Health,
        // Statuses: per-stack magnitude lives in code, so these use ModOp.Stack
        Weaken, Slow, Vulnerability, Expose, Harden, Pierce, Confuse,
    }

    /// <summary>Stack = apply N stacks of a status whose per-stack strength is configured in code.</summary>
    public enum ModOp { Add, Mul, Set, Stack }

    public enum RollOpKind { Advantage, Disadvantage, ReplaceNext, CritFaceMin, Explode, SalvageMiss }

    public enum StripOpKind
    {
        Jump, Skip, Repeat, Loop, Stop, Random, Shuffle, ReplaceNext, Disable,
        RetriggerNext, RetriggerOpener, RetriggerFinisher, RetriggerSlot,
        /// <summary>Reset the target's combo (JAB, BLUDGEON); a strip op aimed at the enemy.</summary>
        Disrupt,
    }

    public enum IssueSeverity { Loss, Note }

    /// <summary>The base swing: what every action does before any effect.</summary>
    public sealed record Chassis(double DamagePercent, double Speed, int Hits, double Block, ModTarget StrikeTarget);

    /// <summary>Scope plus how many of it (e.g. Turn × 3 = next 3 turns).</summary>
    public sealed record ActionScope(ScopeKind Kind, int Count = 1)
    {
        public static readonly ActionScope Instant = new(ScopeKind.Instant);
        public static readonly ActionScope Hit = new(ScopeKind.Hit);
        public override string ToString() => Count == 1 ? Kind.ToString() : $"{Kind}x{Count}";
    }

    /// <summary>
    /// Gate on an effect. Event is the canonical WHEN token (ONCRITICAL, ONKILL, ONROLLVALUE, …);
    /// Arg is its operand (roll face, room count); Filters are AND'd conditions (IFSOURCEHEALTHBELOW:50).
    /// </summary>
    public sealed record When(string Event, string? Arg, IReadOnlyList<string> Filters)
    {
        public override string ToString()
        {
            string s = Arg == null ? Event : $"{Event}:{Arg}";
            return Filters.Count == 0 ? s : $"{s} & {string.Join(" & ", Filters)}";
        }
    }

    /// <summary>Change one number, on one side, for some scope, optionally gated and scaled.</summary>
    public sealed record Modifier(
        Quantity Quantity,
        ModOp Op,
        double Value,
        ModTarget Target,
        ActionScope Scope,
        When? When = null,
        string? ScaleFrom = null)
    {
        public override string ToString()
        {
            string s = $"{Target} {Quantity} {Op} {Value:0.##} [{Scope}]";
            if (ScaleFrom != null) s += $" x{ScaleFrom}";
            if (When != null) s += $" when {When}";
            return s;
        }
    }

    /// <summary>Changes the die's distribution rather than adding to it.</summary>
    public sealed record RollOp(RollOpKind Kind, int? Value, ActionScope Scope, When? When = null)
    {
        public override string ToString() =>
            $"Roll {Kind}{(Value.HasValue ? ":" + Value : "")} [{Scope}]{(When != null ? " when " + When : "")}";
    }

    /// <summary>Control flow on a combo strip.</summary>
    public sealed record StripOp(StripOpKind Kind, string? Arg, ModTarget Target, When? When = null)
    {
        public override string ToString() =>
            $"Strip {Kind}{(Arg != null ? ":" + Arg : "")} ({Target}){(When != null ? " when " + When : "")}";
    }

    public sealed record ActionModel(
        string Name,
        Chassis Chassis,
        IReadOnlyList<Modifier> Modifiers,
        IReadOnlyList<RollOp> RollOps,
        IReadOnlyList<StripOp> StripOps,
        IReadOnlyList<string> Tags,
        bool Opener,
        bool Finisher)
    {
        public bool IsChassisOnly => Modifiers.Count == 0 && RollOps.Count == 0 && StripOps.Count == 0 && !Opener && !Finisher;
    }

    /// <summary>Loss = information in the row the model can't hold (or copies that disagree); Note = worth a look, nothing dropped.</summary>
    public sealed record ConversionIssue(IssueSeverity Severity, string Field, string Message)
    {
        public override string ToString() => $"{Severity.ToString().ToUpperInvariant()} {Field}: {Message}";
    }
}
