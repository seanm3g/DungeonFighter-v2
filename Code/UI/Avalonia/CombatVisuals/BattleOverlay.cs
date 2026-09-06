using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI.Avalonia.CombatVisuals;

public static class BattleOverlay
{
    public static readonly Color Ink = Color.Parse("#111517"), Bone = Color.Parse("#EEEAD7"), Sulfur = Color.Parse("#D8E653"), Crimson = Color.Parse("#FF3455");
    private static void Text(DrawingContext c, string value, Point point, double width, Color color, double size = 11)
    {
        if (width <= 0) return;
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), size, new SolidColorBrush(color)) { MaxTextWidth = width, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis };
        c.DrawText(text, point);
    }
    public static void Draw(DrawingContext c, Rect stage, BattlePresentation data, CombatVisualDirector director, double now, bool effects)
    {
        using var clip = c.PushClip(stage);
        var panel = new SolidColorBrush(Color.Parse("#EF111517"));
        c.FillRectangle(panel, new Rect(stage.X, stage.Y, stage.Width, 76));
        c.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#46514A")), 1), new Point(stage.X+8,stage.Y+35), new Point(stage.Right-8,stage.Y+35));
        Text(c, CombatPlaybackControls.Timeline, stage.TopLeft + new Vector(8, 42), stage.Width - 16, Bone, 10);
        Text(c, $"{(CombatPlaybackControls.Paused ? "PAUSED" : "P: pause")} · PgUp/PgDn: speed {DeveloperModeState.CombatSpeedMultiplier}x",
            stage.TopLeft + new Vector(8, 56), stage.Width - 16, Sulfur, 10);
        var cue = CombatVisualPlayback.Current;
        if (cue?.Phase.StartsWith("prepare") == true && cue.Action.SourceIsEnemy)
        {
            c.FillRectangle(new SolidColorBrush(Color.Parse("#EF301A21")), new Rect(stage.X+8,stage.Y+80,stage.Width-16,22));
            Text(c, $"ENEMY PREPARES: {cue.Action.Name} · {cue.Action.Intent}", stage.TopLeft + new Vector(14, 85), stage.Width - 28, Crimson, 11);
        }
        Color gearColor = data.EquipmentRarity.ToLowerInvariant() switch
        {
            "legendary" => Color.Parse("#FFCF66"), "epic" => Color.Parse("#CEA0EE"),
            "rare" => Color.Parse("#80CADE"), "uncommon" => Sulfur, _ => Bone
        };
        Text(c, data.Gear, stage.TopLeft + new Vector(8, 13), stage.Width * .65, gearColor);
        Text(c, data.Rank.ToUpperInvariant() + " · " + data.EnemyRole, stage.TopLeft + new Vector(stage.Width*.67, 13), stage.Width*.32, Crimson);
        if (!string.Equals(data.Rank, "Common", StringComparison.OrdinalIgnoreCase))
            c.DrawEllipse(null, new Pen(new SolidColorBrush(Sulfur), 2), new Point(stage.X+stage.Width*.69, stage.Y+stage.Height*.7), stage.Width*.06, stage.Height*.025);
        Badges(c, stage, data.HeroStatuses, false, effects, now);
        Badges(c, stage, data.EnemyStatuses, true, effects, now);
        c.FillRectangle(panel, new Rect(stage.X, stage.Bottom-60, stage.Width, 60));
        Text(c, "COMBO  " + (string.IsNullOrEmpty(data.Combo) ? "Equip actions to build your sequence" : data.Combo), new Point(stage.X+8,stage.Bottom-56),stage.Width-16,Sulfur);
        string advice = data.Advice.Length == 0 ? "" : data.Advice[0];
        Text(c, advice, new Point(stage.X+8,stage.Bottom-39),stage.Width-16,Bone);
        Text(c, data.Advice.Length > 1 ? data.Advice[1] : "Reorder and compare equipment between encounters.", new Point(stage.X+8,stage.Bottom-22),stage.Width-16,Bone);
        if (director.Enemy.Clip == "death" || director.Hero.Clip == "death")
        {
            bool won = director.Enemy.Clip == "death" && director.Hero.Clip != "death";
            Text(c, won ? "VICTORY" : "DEFEAT", new Point(stage.X+stage.Width*.4,stage.Y+92),stage.Width*.4,won ? Sulfur : Crimson,20);
            Text(c,$"Shown strikes: dealt {director.DamageDealt} · taken {director.DamageTaken}", new Point(stage.X+stage.Width*.20,stage.Y+116),stage.Width*.78,Bone);
            if (effects && won)
                for (int i=0;i<12;i++)
                {
                    double t=Math.Clamp((now-director.Enemy.Started)/1.2,0,1);
                    var p=new Point(stage.X+stage.Width*(.25+i*.045),stage.Y+stage.Height*(.65-t*.35));
                    using var fade=c.PushOpacity(1-t);
                    c.DrawEllipse(new SolidColorBrush(Sulfur),null,p,2,2);
                }
        }
    }
    private static void Badges(DrawingContext c, Rect stage, BattleStatus[] statuses, bool enemy, bool effects, double now)
    {
        double x=stage.X+stage.Width*(enemy ? .53 : .02), y=stage.Bottom-101;
        int shown=Math.Min(4,statuses.Length);
        for(int i=0;i<shown;i++)
        {
            var s=statuses[i]; double bx=x+(i%2)*stage.Width*.225, by=y+(i/2)*17;
            c.FillRectangle(new SolidColorBrush(Color.Parse("#E0111517")),new Rect(bx,by,stage.Width*.22,16));
            c.DrawEllipse(new SolidColorBrush(Color.Parse(s.Color)),null,new Point(bx+6,by+8),3,3);
            Text(c,s.Text,new Point(bx+13,by+1),stage.Width*.22-15,Color.Parse(s.Color),9);
            if(effects)
            {
                double t=(now*.8+i*.27)%1;
                var center=new Point(stage.X+stage.Width*(enemy ? .69 : .30),stage.Y+stage.Height*.59);
                if(s.Key is "poison" or "burn" or "bleed" or "acid")
                    c.DrawEllipse(new SolidColorBrush(Color.Parse(s.Color)),null,center+new Vector(Math.Sin(i+t*6)*15,(s.Key=="bleed"?1:-1)*t*26),2,3);
                else if(s.Key is "shield" or "reflect") c.DrawEllipse(null,new Pen(new SolidColorBrush(Color.Parse(s.Color)),1),center,24,38);
                else if(s.Key=="stun") Text(c,"*  *  *",center+new Vector(-20,-40),70,Sulfur);
                else if(s.Key=="break") { c.DrawLine(new Pen(new SolidColorBrush(Crimson),2),center+new Vector(-12,-15),center+new Vector(5,15)); }
            }
        }
        if(statuses.Length>shown) Text(c,$"+{statuses.Length-shown} effects: see status panel",new Point(x,y-14),stage.Width*.45,Bone,9);
    }
}



