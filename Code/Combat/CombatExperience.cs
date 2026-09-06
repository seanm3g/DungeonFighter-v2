using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RPGGame;

/// <summary>Encounter health accounting; records applied HP changes, including DoT and reflection.</summary>
public static class EncounterReport
{
    public sealed class Totals
    {
        public long Lost, Restored;
        public bool Active;
    }
    private static readonly ConditionalWeakTable<Character, Totals> totals = new();
    public static void Begin(Character actor) { var t = totals.GetOrCreateValue(actor); lock (t) { t.Lost = t.Restored = 0; t.Active = true; } }
    public static void End(Character actor) { if (totals.TryGetValue(actor, out var t)) lock (t) t.Active = false; }
    public static void Record(Character actor, int before, int after)
    {
        if (!totals.TryGetValue(actor, out var t)) return;
        lock (t) { if (!t.Active) return; t.Lost += Math.Max(0, before - after); t.Restored += Math.Max(0, after - before); }
    }
    public static (long Lost, long Restored) Read(Character actor)
    {
        if (!totals.TryGetValue(actor, out var t)) return default;
        lock (t) return (t.Lost, t.Restored);
    }
    public static string Summary(Character hero, Enemy enemy)
    {
        var h = Read(hero); var e = Read(enemy);
        return $"BATTLE REPORT | Enemy HP lost {e.Lost} | Fighter HP lost {h.Lost} | Healing: fighter {h.Restored}, enemy {e.Restored} (includes status effects)";
    }
}

/// <summary>Live playback pause. Headless runs bypass the gate.</summary>
public static class CombatPlaybackControls
{
    private static int paused;
    public static bool Paused => Volatile.Read(ref paused) != 0;
    public static string Timeline { get; private set; } = "Readiness pending";
    public static void Toggle() => Interlocked.Exchange(ref paused, Paused ? 0 : 1);
    public static void Resume() => Interlocked.Exchange(ref paused, 0);
    public static async Task WaitAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        while (Paused && !SimulationPacing.ShouldSkipDelays)
            await Task.Delay(40, token);
    }
    public static void SetTimeline(string value) => Timeline = value;
}

