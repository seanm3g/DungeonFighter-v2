using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RPGGame.Combat.Sequence;

/// <summary>Immutable presentation data. No renderer types, RNG or gameplay writes.</summary>
public sealed record CombatVisualAction(long Id, long SourceId, long TargetId,
    bool Hit, bool Critical, int Damage, int Heal, bool TargetDied, string Style,
    string Delivery = "melee", string Name = "", int? Blocked = null, string Effects = "", bool ActionUsed = true,
    bool SourceIsEnemy = false, string Intent = "attack", int? RollTotal = null,
    int? HitThreshold = null, int? ComboThreshold = null, int? CritThreshold = null, int? CritMissThreshold = null,
    string DamageBreakdown = "");
public sealed record CombatVisualCue(CombatVisualAction Action, string Phase, long Timestamp, int DurationMs);

public static class CombatVisualPlayback
{
    private sealed class Identity { public long Value { get; } = Interlocked.Increment(ref nextIdentity); }
    private static long nextIdentity, nextAction;
    private static readonly ConditionalWeakTable<Actor, Identity> identities = new();
    private static CombatVisualCue? current;
    public static CombatVisualCue? Current => Volatile.Read(ref current);
    public static long ActorId(Actor? actor) => actor == null ? 0 : identities.GetValue(actor, _ => new Identity()).Value;
    public static long NextActionId() => Interlocked.Increment(ref nextAction);
    public static void Clear() => Volatile.Write(ref current, null);
    public static void Publish(CombatVisualAction action, string phase, int durationMs) =>
        Volatile.Write(ref current, new CombatVisualCue(action, phase, Stopwatch.GetTimestamp(), Math.Clamp(durationMs, 40, 600)));
}
