using System;

namespace RPGGame.Combat.Sequence;

/// <summary>Bounded, clock-driven visual state. Cues replace cosmetics rather than queueing combat.</summary>
public sealed class CombatVisualDirector
{
    public sealed record Pose(string Clip, double Started, double Duration);
    private CombatVisualCue? seen;
    private long hero, enemy;
    public Pose Hero { get; private set; } = new("idle", 0, 0);
    public Pose Enemy { get; private set; } = new("idle", 0, 0);
    public CombatVisualCue? Effect { get; private set; }
    public CombatVisualAction? Action { get; private set; }
    public int DamageDealt { get; private set; }
    public int DamageTaken { get; private set; }
    private long resolvedAction;

    public void Bind(long heroId, long enemyId)
    {
        if (hero == heroId && enemy == enemyId) return;
        hero = heroId; enemy = enemyId; Reset();
        seen = CombatVisualPlayback.Current;
    }
    public void Reset()
    {
        Hero = new("idle", 0, 0); Enemy = new("idle", 0, 0);
        Effect = null; Action = null; seen = null;
        DamageDealt = DamageTaken = 0; resolvedAction = 0;
    }
    public void Suspend() { Reset(); seen = CombatVisualPlayback.Current; }
    public void Accept(CombatVisualCue? cue, double now)
    {
        if (cue == null) { if (seen != null) Reset(); return; }
        if (ReferenceEquals(cue, seen)) return;
        seen = cue;
        var a = cue.Action;
        if ((a.SourceId != 0 && a.SourceId != hero && a.SourceId != enemy)
            || (a.TargetId != 0 && a.TargetId != hero && a.TargetId != enemy)) return;
        double duration = cue.DurationMs / 1000d;
        Action = a;
        if (cue.Phase == "impact" && resolvedAction != a.Id)
        {
            resolvedAction = a.Id;
            if (a.TargetId == enemy) DamageDealt += a.Damage;
            if (a.TargetId == hero) DamageTaken += a.Damage;
        }
        void Set(long id, string clip, double offset = 0)
        {
            if (id == 0) return;
            var pose = new Pose(clip, now - offset, duration);
            if (id == hero && Hero.Clip != "death") Hero = pose;
            if (id == enemy && Enemy.Clip != "death") Enemy = pose;
        }
        switch (cue.Phase)
        {
            case "attack": case "cast":
                Set(a.SourceId, a.Damage > 0 || a.Heal > 0 ? "prepare-" + cue.Phase : cue.Phase);
                break;
            case "miss": Set(a.SourceId, "attack"); Set(a.TargetId, "evade"); Effect = cue; break;
            case "guard": Set(a.SourceId, "attack", duration * .5); Set(a.TargetId, "guard"); Effect = cue; break;
            case "impact":
                Set(a.SourceId, a.Style, duration * .5);
                Set(a.TargetId, a.TargetDied ? "death" : "hit"); Effect = cue; break;
            case "heal": case "effect": Set(a.SourceId, "cast", duration * .5); Effect = cue; break;
        }
    }
    public Pose GetPose(bool isEnemy, double now, bool hold = false)
    {
        var pose = isEnemy ? Enemy : Hero;
        if (pose.Clip == "death") return pose;
        if (pose.Clip.StartsWith("prepare-", StringComparison.Ordinal)) return pose;
        if (hold && pose.Clip != "idle") return pose;
        if (now - pose.Started < pose.Duration) return pose;
        if (!isEnemy && Enemy.Clip == "death" && now - Enemy.Started >= Enemy.Duration)
            return new("victory", Enemy.Started + Enemy.Duration, .6);
        return new("idle", 0, 0);
    }
}
