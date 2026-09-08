using System.Collections.Generic;
using System.Linq;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat;

public static class CombatResolutionTests
{
    public static void RunAllTests()
    {
        int run=0,passed=0,failed=0;
        void Check(bool value,string message) => TestBase.AssertTrue(value,message,ref run,ref passed,ref failed);
        var hero=new Character("Fighter",1);
        var foe=new Enemy("Wolf",1,100,8,6,4,4,2);
        var result=new ActionExecutionResult {
            SelectedAction=new RPGGame.Action { Name="SLAM" }, ModifiedBaseRoll=14, RollBonus=3, AttackRoll=17,
            Hit=true,IsCombo=true,Damage=49,ResolvedHitThreshold=6,ResolvedComboThreshold=14,
            ResolvedCritThreshold=16,ResolvedCritMissThreshold=1,
            StatusEffectMessages=new List<string> { "Bleed applied" }
        };
        var steps=CombatSequenceBuilder.From(result,hero,foe);
        CombatResolutionFrame At(int index,bool revealed=false,IReadOnlyList<ColoredText>? visible=null) =>
            CombatResolutionFrame.Capture(steps,index,revealed,visible??new List<ColoredText>());
        Check(At(0).Roll=="—" && At(0).Outcome=="", "prepare hides future roll and outcome");
        int roll=steps.FindIndex(s=>s.Kind==CombatSequenceStepKind.Roll);
        int outcome=steps.FindIndex(s=>s.Kind==CombatSequenceStepKind.Outcome);
        int damage=steps.FindIndex(s=>s.Kind==CombatSequenceStepKind.Damage);
        var partial=At(roll,true,steps[roll].MathBeats[0]);
        Check(!partial.RollComplete && !partial.Roll.Contains("17"),"base die does not reveal future bonus total");
        var rolled=At(roll,true,steps[roll].Result);
        Check(rolled.Roll=="17" && rolled.Outcome=="", "total reveals before outcome");
        var classified=At(outcome,true,steps[outcome].Result);
        Check(classified.Outcome.Contains("COMBO")&&!classified.Outcome.Contains("CRIT"),"authoritative outcome wins over total exceeding critical threshold");
        Check(!classified.Headline.Contains("49") && !classified.Detail.Contains("Bleed"),"outcome hides future damage and consequences");
        Check(!At(damage).Headline.Contains("49"),"impact anticipation hides final damage");
        Check(At(damage,true,steps[damage].Result).Headline.Contains("49 DAMAGE"),"damage appears at impact reveal");
        CombatSequenceHudState.Begin(steps);
        CombatSequenceHudState.FinishSequence();
        Check(CombatResolutionState.Current.Phase=="RESULT" && CombatResolutionState.Current.Headline.Contains("49 DAMAGE"),"finished result persists");
        result.NestedRetriggerResults.Add(new ActionExecutionResult { SelectedAction=new RPGGame.Action { Name="BITE" },ModifiedBaseRoll=2,AttackRoll=2 });
        steps=CombatSequenceBuilder.From(result,hero,foe);
        int nested=steps.FindLastIndex(s=>s.Kind==CombatSequenceStepKind.Attacker);
        Check(At(nested).Roll=="—"&&At(nested).Outcome=="", "nested swing does not inherit previous roll");
        var hazard=CombatSequenceBuilder.FromEnvironmental("Falling stones",8,null,hero);
        var hazardFrame=CombatResolutionFrame.Capture(hazard,hazard.Count,true,new List<ColoredText>());
        Check(hazardFrame.Roll=="—" && hazardFrame.Headline=="8 DAMAGE", "hazard reports damage without inventing a roll");
        var healSteps=CombatSequenceBuilder.From(new ActionExecutionResult {
            SelectedAction=new RPGGame.Action { Name="MEND",Type=ActionType.Heal }, Hit=true,IsCombo=true,HealAmount=12
        },hero,hero);
        var healed=CombatResolutionFrame.Capture(healSteps,healSteps.Count,true,new List<ColoredText>());
        Check(healed.Headline.Contains("12 HEALED"),"healing gets its own outcome label");
        var blockSteps=CombatSequenceBuilder.From(new ActionExecutionResult {
            SelectedAction=new RPGGame.Action { Name="BITE" }, Hit=true,DefenseFace=20,
            DamageTrace=new CombatSequenceDamageTrace { Block=5,Final=0 }
        },foe,hero);
        var blocked=CombatResolutionFrame.Capture(blockSteps,blockSteps.Count,true,new List<ColoredText>());
        Check(blocked.Headline=="BLOCKED · 0 DAMAGE","fully blocked attack remains readable");
        bool priorInstant=DeveloperModeState.IsCombatLogInstant;
        bool priorMuted=CombatManager.DisableCombatUIOutput;
        try
        {
            DeveloperModeState.SetCombatLogInstant(true);
            CombatManager.DisableCombatUIOutput=false;
            CombatSequencePresenter.SetPending(healSteps);
            CombatSequencePresenter.PlayPendingAsync().GetAwaiter().GetResult();
            Check(CombatResolutionState.Current.Headline.Contains("12 HEALED")&&CombatResolutionState.Current.Phase=="RESULT",
                "instant playback retains completed result without animation");
        }
        finally { DeveloperModeState.SetCombatLogInstant(priorInstant); CombatManager.DisableCombatUIOutput=priorMuted; CombatSequencePresenter.ClearPending(); }
        CombatSequenceHudState.ClearStep();
        Check(CombatResolutionState.Current==CombatResolutionFrame.Empty,"clear removes old combat result");
        TestBase.PrintSummary("CombatResolutionTests",run,passed,failed);
    }
}
