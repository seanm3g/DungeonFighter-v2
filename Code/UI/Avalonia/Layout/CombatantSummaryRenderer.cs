using System;
using Avalonia.Media;
using RPGGame.Combat.UI;
using RPGGame.UI.Avalonia.CombatVisuals;

namespace RPGGame.UI.Avalonia.Layout;

/// <summary>Compact combat summaries free the center for animation and resolution.</summary>
public static class CombatantSummaryRenderer
{
    public static void Render(GameCanvasControl canvas, Character actor, bool enemy, string? dungeon = null, string? room = null)
    {
        int px=enemy ? LayoutConstants.RIGHT_PANEL_X : LayoutConstants.LEFT_PANEL_X;
        int pw=enemy ? LayoutConstants.RIGHT_PANEL_WIDTH : LayoutConstants.LEFT_PANEL_WIDTH;
        int x=px+2,w=pw-4,y=1,bottom=CombatArenaHudLayout.SideCardY-1;
        canvas.ClearTextInArea(px,0,pw,LayoutConstants.SCREEN_HEIGHT+1);
        canvas.ClearProgressBarsInArea(px,0,pw,LayoutConstants.SCREEN_HEIGHT+1);
        canvas.ClearSegmentedBarsInArea(px,0,pw,LayoutConstants.SCREEN_HEIGHT+1);
        canvas.ClearBoxesInArea(px,0,pw,LayoutConstants.SCREEN_HEIGHT+1);
        canvas.ClearLinesInArea(px,0,pw,LayoutConstants.SCREEN_HEIGHT+1);
        Color accent=enemy ? BattleOverlay.Crimson : BattleOverlay.Sulfur;
        void Line(string value, Color? color=null) { if(y<bottom) canvas.AddText(x,y++,FighterResolveActionStackRenderer.Fit(value,w),color??BattleOverlay.Bone); }
        Line((enemy?"ENEMY":"FIGHTER")+" / "+actor.Name,accent);
        Line($"Lv {actor.Level} · "+(actor is Enemy foe ? $"{foe.Rarity} {foe.Archetype}" : actor.GetCurrentClass()));
        int hp=HealthBarDisplayHold.Resolve(HealthBarEntityId.ForActor(actor)??$"player_{actor.Name}",actor.CurrentHealth);
        int maxHp = actor.GetEffectiveMaxHealth();
        canvas.AddHealthBar(x,y++,w,hp,maxHp,entityId:HealthBarEntityId.ForActor(actor));
        Line($"HP {hp}/{maxHp} · Armor {(actor is Enemy armored ? armored.Armor : actor.GetMaxArmor())}");
        if(enemy) { Line(dungeon??"Dungeon"); Line(room??"Encounter"); }
        else { Line(actor.Weapon?.Name??"Unarmed"); Line(actor.Body?.Name??"No body armor"); }
        Line($"STR {actor.GetEffectiveStrength()} · AGI {actor.GetEffectiveAgility()}");
        Line($"TECH {actor.GetEffectiveTechnique()} · INT {actor.GetEffectiveIntelligence()}");
        if (y + 4 < bottom) y = DiceRollThresholdRowsRenderer.RenderRows(canvas,x,y,actor);
        var statuses=StatusEffectDisplayLines.Build(actor,actor);
        int shown = Math.Min(statuses.Count, Math.Max(0,bottom-y-2));
        for(int i=0;i<shown;i++) Line(statuses[i]);
        if(statuses.Count>shown) Line($"+{statuses.Count-shown} more effects");
        if(statuses.Count==0) Line("No active effects",Color.Parse("#A8B5B1"));
        if(actor is Enemy e && !e.IsLiving) Line("Immune: Bleed, Poison",Color.Parse("#A8B5B1"));
    }
}
