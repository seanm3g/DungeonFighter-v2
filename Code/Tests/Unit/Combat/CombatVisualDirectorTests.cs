using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using System;
using System.Linq;

namespace RPGGame.Tests.Unit.Combat;

public static class CombatVisualDirectorTests
{
    public static void RunAllTests()
    {
        int run=0, passed=0, failed=0;
        var director=new CombatVisualDirector();
        CombatVisualPlayback.Clear();
        director.Bind(1,2);
        var action=new CombatVisualAction(1,1,2,true,true,24,0,false,"attack");
        TestBase.AssertEqual(1d, RPGGame.UI.Avalonia.CombatVisuals.BattleMotion.Approach(action,1,"prepare-attack",1,.6), "melee reaches contact distance during anticipation",ref run,ref passed,ref failed);
        TestBase.AssertEqual(0d, RPGGame.UI.Avalonia.CombatVisuals.BattleMotion.Approach(action with { Delivery="projectile" },1,"prepare-attack",1,.6), "ranged attacks keep distance",ref run,ref passed,ref failed);
        TestBase.AssertEqual(0d, RPGGame.UI.Avalonia.CombatVisuals.BattleMotion.Approach(action,1,"attack",1,.6), "melee returns after recovery",ref run,ref passed,ref failed);
        TestBase.AssertEqual(0d, RPGGame.UI.Avalonia.CombatVisuals.BattleMotion.Approach(action with { TargetId=1 },1,"prepare-attack",1,.6), "self-target actions never lunge",ref run,ref passed,ref failed);
        var attack=new CombatVisualCue(action,"attack",0,300);
        director.Accept(attack,10);
        TestBase.AssertEqual("prepare-attack",director.GetPose(false,20).Clip,"anticipation waits for damage reveal",ref run,ref passed,ref failed);
        var impact=new CombatVisualCue(action,"impact",0,300);
        director.Accept(impact,20);
        TestBase.AssertEqual("attack",director.GetPose(false,20).Clip,"strike starts at impact",ref run,ref passed,ref failed);
        TestBase.AssertEqual("hit",director.GetPose(true,20).Clip,"target reacts at impact",ref run,ref passed,ref failed);
        director.Accept(impact,21);
        TestBase.AssertEqual("idle",director.GetPose(true,21).Clip,"same cue does not restart reaction",ref run,ref passed,ref failed);
        director.Accept(new CombatVisualCue(action with { Id=2, TargetDied=true },"impact",0,300),22);
        TestBase.AssertEqual("death",director.GetPose(true,24).Clip,"death holds its final frame",ref run,ref passed,ref failed);
        TestBase.AssertEqual("victory",director.GetPose(false,24).Clip,"victory follows enemy death",ref run,ref passed,ref failed);
        director.Accept(null,25);
        TestBase.AssertEqual("idle",director.GetPose(true,25).Clip,"cancel clears death and held poses",ref run,ref passed,ref failed);
        director.Bind(1,3);
        director.Accept(impact,26);
        TestBase.AssertTrue(director.Effect==null,"old encounter target is rejected",ref run,ref passed,ref failed);
        director.Bind(1,2);
        director.Accept(new CombatVisualCue(action with { Hit=false },"miss",0,200),27);
        TestBase.AssertEqual("evade",director.GetPose(true,27).Clip,"miss uses evade rather than injury",ref run,ref passed,ref failed);
        TestBase.AssertEqual("evade",director.GetPose(true,30,hold:true).Clip,"manual stepping holds pose",ref run,ref passed,ref failed);
        director.Accept(new CombatVisualCue(action with { SourceId=2,TargetId=1,Damage=0,Heal=12 },"heal",0,200),31);
        TestBase.AssertEqual(1L,director.Effect!.Action.TargetId,"healing preserves recipient identity",ref run,ref passed,ref failed);

        var hero=TestDataBuilders.CreateTestCharacter("VisualHero",1);
        var enemy=TestDataBuilders.Enemy().WithName("Bat").Build();
        var result=new ActionExecutionResult { SelectedAction=new RPGGame.Action { Name="Hit",Type=ActionType.Attack }, Hit=true,Damage=9,VisualTargetHealthAfter=10 };
        result.NestedRetriggerResults.Add(new ActionExecutionResult { SelectedAction=new RPGGame.Action { Name="Again",Type=ActionType.Attack }, Hit=true,Damage=4,VisualTargetHealthAfter=0 });
        var visuals=CombatSequenceBuilder.From(result,hero,enemy).Select(s=>s.VisualAction).Where(v=>v!=null).Distinct().ToArray();
        TestBase.AssertEqual(2,visuals.Length,"retrigger keeps a separate visual action",ref run,ref passed,ref failed);
        TestBase.AssertTrue(!visuals[0]!.TargetDied && visuals[1]!.TargetDied,"death belongs to lethal retrigger only",ref run,ref passed,ref failed);
        result.SelectedAction.Target=TargetType.Self;
        result.NestedRetriggerResults.Clear();
        var self=CombatSequenceBuilder.From(result,hero,enemy)[0].VisualAction!;
        TestBase.AssertEqual(self.SourceId,self.TargetId,"self-target effect stays on source",ref run,ref passed,ref failed);
        CombatVisualPlayback.Clear();
        TestBase.PrintSummary("CombatVisualDirectorTests",run,passed,failed);
    }
}
