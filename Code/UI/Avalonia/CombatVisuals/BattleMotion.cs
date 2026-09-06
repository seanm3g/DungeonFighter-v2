using System;
using RPGGame.Combat.Sequence;
namespace RPGGame.UI.Avalonia.CombatVisuals;
public static class BattleMotion
{
    public static double Approach(CombatVisualAction? action, long actorId, string clip, double elapsed, double duration)
    {
        if (action == null || action.SourceId != actorId || action.SourceId == action.TargetId || action.Delivery != "melee") return 0;
        double t = Math.Clamp(elapsed / Math.Max(.04, duration), 0, 1);
        if (clip == "prepare-attack") return Smooth(Math.Min(1, t / .3));
        if (clip != "attack") return 0;
        return t < .5 ? Smooth(t / .5) : 1 - Smooth(Math.Max(0, (t - .62) / .38));
    }
    private static double Smooth(double t) => t * t * (3 - 2 * t);
    public static bool IsSpectral(string? family) => family is "wraith" or "sprite" or "flame";
    public static bool IsBeast(string? family) => family is "wolf" or "firehound" or "bear" or "boar" or "rat" or "lizard" or "toad" or "tortoise";
    public static double TargetY(string? family) => IsBeast(family) || family is "spider" or "crab" or "beetle" or "scorpion" ? .63 : .48;
}
