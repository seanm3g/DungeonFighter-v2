using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat;

public static class CombatExperienceTests
{
    public static void RunAllTests()
    {
        int run = 0, passed = 0, failed = 0;
        var hero = TestDataBuilders.CreateTestCharacter("Experience");
        hero.CurrentHealth = hero.GetEffectiveMaxHealth();
        EncounterReport.Begin(hero);
        int start = hero.CurrentHealth;
        hero.CurrentHealth -= 20;
        hero.Heal(7);
        var report = EncounterReport.Read(hero);
        TestBase.AssertTrue(report.Lost == 20 && report.Restored == 7, "applied health accounting includes direct and healing paths", ref run, ref passed, ref failed);
        EncounterReport.End(hero);
        hero.Heal(5);
        TestBase.AssertEqual(report, EncounterReport.Read(hero), "post-battle recovery does not change report", ref run, ref passed, ref failed);
        hero.CurrentHealth = start / 2;
        DungeonRestChoice.Rest(hero);
        TestBase.AssertEqual(Math.Min(start, start / 2 + Math.Max(1, start / 10)), hero.CurrentHealth, "rest grants advertised recovery", ref run, ref passed, ref failed);
        TestBase.AssertTrue(DungeonRestChoice.Consume(hero) && !DungeonRestChoice.Consume(hero), "rest initiative penalty consumed once", ref run, ref passed, ref failed);
        var speed = new ActionSpeedSystem();
        var enemy = MockFactories.CreateMockEnemy("Wolf");
        speed.AddEntity(hero, 1); speed.AddEntity(enemy, 1);
        double now = GameTicker.Instance.GetCurrentGameTime();
        speed.SetEntityActionTime(hero, now + 8); speed.SetEntityActionTime(enemy, now + 2);
        TestBase.AssertTrue(speed.DescribeReadiness(hero).IndexOf("Enemy", StringComparison.Ordinal) < speed.DescribeReadiness(hero).IndexOf("Fighter", StringComparison.Ordinal), "timeline follows scheduled readiness", ref run, ref passed, ref failed);
        var breaker = TestDataBuilders.CreateMockAction("Break"); breaker.CausesArmorBreak = true;
        var heavy = TestDataBuilders.CreateMockAction("Heavy"); heavy.DamageMultiplier = 1.5;
        hero.ActionLabActionSlotBonus = 10; breaker.IsComboAction = heavy.IsComboAction = true; hero.AddToCombo(breaker); hero.AddToCombo(heavy);
        TestBase.AssertTrue(BuildIdentity.Describe(hero).StartsWith("BREAKER"), "equipped setup and payoff identify breaker build", ref run, ref passed, ref failed);
        hero.Weapon = new WeaponItem("Bleeding blade");
        hero.Weapon.Modifications.Add(new Modification { Effect = "weaponBleed", RolledValue = 1, TriggerWhen = "ONCRITICAL" });
        TestBase.AssertTrue(BuildIdentity.Describe(hero).Contains("BLEED WEAPON"), "identity recognizes item-based DoT mechanics", ref run, ref passed, ref failed);
        bool cancelled = false;
        try { CombatPlaybackControls.WaitAsync(new CancellationToken(true)).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { cancelled = true; }
        TestBase.AssertTrue(cancelled, "pause gate honors encounter cancellation", ref run, ref passed, ref failed);
        CombatPlaybackControls.Resume();
        CombatPlaybackControls.Toggle();
        TestBase.AssertTrue(CombatPlaybackControls.Paused, "pause toggles", ref run, ref passed, ref failed);
        CombatPlaybackControls.Resume();
        TestBase.AssertTrue(!CombatPlaybackControls.Paused, "resume clears pause", ref run, ref passed, ref failed);
        TestBase.PrintSummary("CombatExperience", run, passed, failed);
    }
}


