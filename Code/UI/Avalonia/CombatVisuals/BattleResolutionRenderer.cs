using System;
using System.Globalization;
using Avalonia;
using Avalonia.Media;
using RPGGame.Combat.Sequence;

namespace RPGGame.UI.Avalonia.CombatVisuals;

public static class BattleResolutionRenderer
{
    private static void Text(DrawingContext c, string value, double x, double y, double width, double size, Color color)
    {
        if (width <= 0) return;
        c.DrawText(new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), size, new SolidColorBrush(color))
            { MaxTextWidth = width, MaxLineCount = 1, Trimming = TextTrimming.CharacterEllipsis }, new Point(x,y));
    }
    public static void Draw(DrawingContext c, Rect r)
    {
        if (r.Width < 80 || r.Height < 32) return;
        var frame = CombatResolutionState.Current;
        Color actorColor = frame.Visual?.SourceIsEnemy == true ? BattleOverlay.Crimson : BattleOverlay.Sulfur;
        Color resultColor = frame.Outcome.Contains("MISS") ? Color.Parse("#C1CAC8") : frame.Outcome.Contains("CRIT") ? Color.Parse("#FFCD75") : actorColor;
        using var clip = c.PushClip(r);
        c.FillRectangle(new SolidColorBrush(BattleOverlay.Ink), r);
        c.DrawLine(new Pen(new SolidColorBrush(actorColor),2), r.TopLeft, r.TopRight);
        double small = Math.Clamp(r.Height * .12, 9, 12), large = Math.Clamp(r.Height * .26, 16, 28);
        double die = Math.Min(76, r.Height - 22), left = r.X + die + 24;
        Text(c, $"{(CombatPlaybackControls.Paused ? "PAUSED · " : "")}{frame.Phase}  /  {frame.Actor}  /  {frame.Action}", r.X+10,r.Y+5,r.Width-20,small,actorColor);
        var dieBox = new Rect(r.X+10, r.Y+small+13, die, Math.Min(die,Math.Max(22,r.Height-small-26)));
        c.DrawRectangle(new SolidColorBrush(Color.Parse("#252E31")),new Pen(new SolidColorBrush(resultColor),1),dieBox);
        Text(c,frame.Roll,dieBox.X+7,dieBox.Y+5,dieBox.Width-14,frame.Roll.Length > 5 ? small : large,resultColor);
        Text(c,frame.Headline,left,r.Y+small+13,r.Right-left-10,large,resultColor);
        Text(c,frame.Detail,left,r.Y+small+large+19,r.Right-left-10,small,BattleOverlay.Bone);
        if (r.Height >= 95 && frame.Visual is { HitThreshold: int hit, ComboThreshold: int combo, CritThreshold: int crit, CritMissThreshold: int miss } visual)
        {
            // Critical evaluation can exclude bonuses included in the displayed total.
            // Show the snapshotted rules, never classify that total a second time here.
            Text(c,$"Hit > {hit} · Combo ≥ {combo} · Crit check ≥ {crit} · Crit miss ≤ {miss}",
                left,r.Bottom-18,r.Right-left-10,small,BattleOverlay.Bone);
        }
    }
}
