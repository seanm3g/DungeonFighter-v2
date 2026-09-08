using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Combat.Sequence;

/// <summary>A single, reveal-safe snapshot shared by the UI. Never rolls dice or predicts an outcome.</summary>
public sealed record CombatResolutionFrame(string Actor, string Action, string Phase, string Roll,
    string Headline, string Detail, string Outcome, CombatVisualAction? Visual, bool RollComplete)
{
    public static readonly CombatResolutionFrame Empty = new("", "", "READY", "—", "Awaiting action", "The next roll will appear here.", "", null, false);
    public static CombatResolutionFrame Capture(IReadOnlyList<CombatSequenceStep> steps, int current, bool revealed, IReadOnlyList<ColoredText> visible)
    {
        if (current < 0 || steps.Count == 0) return Empty;
        int at = Math.Min(current, steps.Count - 1), start = at, end = steps.Count;
        while (start > 0 && steps[start].Kind != CombatSequenceStepKind.Attacker) start--;
        for (int i = start + 1; i < steps.Count; i++)
            if (steps[i].Kind == CombatSequenceStepKind.Attacker) { end = i; break; }
        string Text(IEnumerable<ColoredText> text) => string.Concat(text.Select(t => t.Text));
        string Shown(int i) => i < current ? Text(steps[i].Result) : i == current && revealed ? Text(visible) : "";
        bool Complete(int i) => i < current || i == current && revealed && Text(visible) == Text(steps[i].Result);
        int Find(CombatSequenceStepKind kind) { for (int i = start; i < end; i++) if (steps[i].Kind == kind) return i; return -1; }
        string Value(CombatSequenceStepKind kind) { int i = Find(kind); return i < 0 ? "" : Shown(i); }
        int rollIndex = Find(CombatSequenceStepKind.Roll), outcomeIndex = Find(CombatSequenceStepKind.Outcome);
        bool rollDone = rollIndex >= 0 && Complete(rollIndex);
        string outcome = outcomeIndex >= 0 && Complete(outcomeIndex) ? Shown(outcomeIndex) : "";
        var visual = steps[start].VisualAction;
        string roll = Value(CombatSequenceStepKind.Roll);
        if (rollDone && visual?.RollTotal is int total) roll = total.ToString();
        var active = steps[at];
        string phase = current >= end ? "RESULT" : active.Kind switch
        {
            CombatSequenceStepKind.Attacker => "PREPARE", CombatSequenceStepKind.Roll => "ROLL",
            CombatSequenceStepKind.Outcome => "OUTCOME", CombatSequenceStepKind.Action => "ACTION",
            CombatSequenceStepKind.Defense => "DEFENSE", CombatSequenceStepKind.Effect => "CONSEQUENCES", _ => "IMPACT"
        };
        string headline = outcome.Length > 0 ? outcome : phase == "ROLL" ? "Rolling…" : phase == "OUTCOME" ? "Checking thresholds…" : "Preparing action";
        foreach (var kind in new[] { CombatSequenceStepKind.Damage, CombatSequenceStepKind.Heal })
        {
            int i = Find(kind);
            if (i >= 0 && Complete(i)) headline = (outcome.Length > 0 ? outcome + " · " : "") + Shown(i) + (kind == CombatSequenceStepKind.Heal ? " HEALED" : " DAMAGE");
        }
        if (current < end && active.Kind == CombatSequenceStepKind.Defense) headline = "DEFENSE · " + (outcome.Length > 0 ? outcome : "Resolving");
        int defense = Find(CombatSequenceStepKind.Defense), damage = Find(CombatSequenceStepKind.Damage);
        if (defense >= 0 && Complete(defense) && visual is { Damage: 0, Heal: 0, Hit: true, Blocked: > 0 }) headline = "BLOCKED · 0 DAMAGE";
        string detail = current < end && revealed ? Text(visible) : "";
        if (current >= end) detail = string.Join("  ·  ", new[] { Value(CombatSequenceStepKind.Action), Value(CombatSequenceStepKind.Defense), Value(CombatSequenceStepKind.Effect) }.Where(s => s.Length > 0));
        if (detail.Length == 0) detail = outcome.Length > 0 ? Value(CombatSequenceStepKind.Action) : "Waiting for the next reveal";
        if (damage >= 0 && Complete(damage) && !string.IsNullOrEmpty(visual?.DamageBreakdown))
            detail = visual.DamageBreakdown + (Value(CombatSequenceStepKind.Effect) is { Length: > 0 } effect ? " · " + effect : "");
        string actionLabel = Value(CombatSequenceStepKind.Action) is { Length: > 0 } action ? action : visual?.Name ?? "Action";
        if (actionLabel == "N/A") actionLabel = "Basic attack";
        if (current >= end && outcome.Length == 0 && damage < 0 && Find(CombatSequenceStepKind.Heal) < 0)
            headline = "Resolved";
        return new(Text(steps[start].Result), actionLabel,
            phase, roll.Length > 0 ? roll : "—", headline, detail, outcome, visual, rollDone);
    }
}

public static class CombatResolutionState
{
    private static CombatResolutionFrame current = CombatResolutionFrame.Empty;
    public static CombatResolutionFrame Current => Volatile.Read(ref current);
    internal static void Update() => Volatile.Write(ref current, CombatResolutionFrame.Capture(CombatSequenceHudState.Steps,
        CombatSequenceHudState.CurrentIndex, CombatSequenceHudState.ResultRevealed, CombatSequenceHudState.VisibleResult));
}
