using System;
using System.Runtime.CompilerServices;
namespace RPGGame;

public static class DungeonRestChoice
{
    private sealed class Pending { public bool Ambush; }
    private static readonly ConditionalWeakTable<Character, Pending> pending = new();
    public static void Rest(Character hero)
    {
        hero.Heal(Math.Max(1, hero.GetEffectiveMaxHealth() / 10));
        pending.GetOrCreateValue(hero).Ambush = true;
    }
    public static bool Consume(Character hero)
    {
        if (!pending.TryGetValue(hero, out var state) || !state.Ambush) return false;
        state.Ambush = false;
        return true;
    }
    public static void Clear(Character hero) => pending.Remove(hero);
}
