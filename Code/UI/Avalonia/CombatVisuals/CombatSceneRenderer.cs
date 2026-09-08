using System;
using System.Globalization;
using System.Diagnostics;
using RPGGame.Combat.Sequence;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace RPGGame.UI.Avalonia.CombatVisuals;

/// <summary>
/// Optional animated presentation. Owns its resources; no combat subscriptions,
/// timers, RNG, or mutable actor references. Rendered only by the main canvas.
/// </summary>
public sealed class CombatSceneRenderer : IDisposable
{
    private Bitmap? background;
    private Bitmap? fighter;
    private Bitmap? enemyArtwork;
    private string? loadedEnemyAsset;
    private SpriteClipSet? heroClips, enemyClips;
    private bool heroClipsAttempted;
    private BattlePresentation? presentation;
    private string heroAsset = "fighter", arenaAsset = "arena-background";
    public void SetPresentation(BattlePresentation? value) => presentation = value;
    private readonly CombatVisualDirector director = new();
    private long heroId, enemyId;
    public string? EnemyAssetKey => loadedEnemyAsset;
    internal CombatVisualCue? ActiveEffect => director.Effect;
    private bool failed;
    public bool Visible { get; private set; }
    public string EnemyName { get; private set; } = "Enemy";
    public bool PreviewDemon { get; private set; }
    public void Hide() => Visible = false;

    public void Configure(bool visible, string? enemyName, bool previewDemon, long fighterIdentity = 0, long enemyIdentity = 0)
    {
        heroId = fighterIdentity; enemyId = enemyIdentity;
        director.Bind(heroId, enemyId);
        Visible = visible;
        EnemyName = string.IsNullOrWhiteSpace(enemyName) ? "Enemy" : enemyName;
        PreviewDemon = previewDemon;
        if (!visible) Dispose();
        else if (!EnsureAssets()) Visible = false;
        else
        {
            LoadPresentationAssets();
            LoadEnemyAsset(ResolveEnemyAsset(EnemyName, previewDemon));
            if (!heroClipsAttempted)
            {
                heroClipsAttempted = true;
                heroClips = TryLoadClips(heroAsset);
            }
        }
    }

    private static SpriteClipSet? TryLoadClips(string actor)
    {
        try { return new SpriteClipSet(actor); }
        catch (Exception ex)
        {
            DebugLogger.Log("CombatVisuals", $"Static fallback for {actor}: {ex.Message}");
            return null;
        }
    }

    private void LoadPresentationAssets()
    {
        string nextHero = presentation?.HeroAsset ?? "fighter";
        string nextArena = presentation == null ? "arena-background" : "arena-" + presentation.Arena;
        if (nextHero != heroAsset)
        {
            try
            {
                var still = Load(nextHero + "-layer");
                fighter?.Dispose(); fighter = still;
                heroClips?.Dispose(); heroClips = TryLoadClips(nextHero);
                heroAsset = nextHero; heroClipsAttempted = true;
            }
            catch (Exception ex) { DebugLogger.Log("CombatVisuals", $"Equipment fallback: {ex.Message}"); }
        }
        if (nextArena != arenaAsset)
        {
            try { var still = Load(nextArena); background?.Dispose(); background = still; arenaAsset = nextArena; }
            catch (Exception ex) { DebugLogger.Log("CombatVisuals", $"Arena fallback: {ex.Message}"); }
        }
    }

    public static string? ResolveEnemyAsset(string? name, bool previewDemon = false)
    {
        var family = EnemyVisualCatalog.Resolve(name);
        if (family != null) return family + "-layer";
        return previewDemon || string.Equals(name?.Trim(), "demon", StringComparison.OrdinalIgnoreCase)
            ? "demon-layer" : null;
    }

    private void LoadEnemyAsset(string? key)
    {
        if (loadedEnemyAsset == key) return;
        enemyArtwork?.Dispose();
        enemyClips?.Dispose(); enemyClips = null;
        enemyArtwork = null;
        loadedEnemyAsset = key;
        if (key == null) return;
        try
        {
            enemyArtwork = Load(key);
            enemyClips = TryLoadClips(key.Replace("-layer", ""));
        }
        catch (Exception ex) when (ex is System.IO.IOException or ArgumentException or InvalidOperationException)
        {
            DebugLogger.Log("CombatVisuals", $"Enemy artwork unavailable ({key}): {ex.Message}");
        }
    }

    private static Bitmap Load(string name)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://DF/Visuals/{name}.png"));
        return new Bitmap(stream);
    }

    private bool EnsureAssets()
    {
        if (failed) return false;
        if (background != null) return true;
        try
        {
            background = Load("arena-background");
            fighter = Load("fighter-layer");
            return true;
        }
        catch (Exception ex) when (ex is System.IO.IOException or ArgumentException or InvalidOperationException)
        {
            Dispose();
            failed = true;
            DebugLogger.Log("CombatVisuals", $"Stage unavailable: {ex.Message}");
            return false;
        }
    }

    public void Draw(DrawingContext context, Rect bounds)
    {
        if (!Visible || bounds.Width <= 0 || bounds.Height <= 0) return;
        using var clip = context.PushClip(bounds);
        context.FillRectangle(Brushes.Black, bounds);
        if (!EnsureAssets())
        {
            Label(context, "Battle artwork unavailable — combat continues below", bounds.TopLeft + new Vector(8, 8), 12);
            return;
        }
        double scale = Math.Min(bounds.Width / 1600, bounds.Height / 900);
        var destination = new Rect(bounds.X + (bounds.Width - 1600 * scale) / 2,
            bounds.Y + (bounds.Height - 900 * scale) / 2, 1600 * scale, 900 * scale);
        context.DrawImage(background!, destination);
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        var settings = GameConfiguration.Instance.UICustomization;
        bool instant = DeveloperModeState.IsCombatLogInstant;
        if (instant) director.Reset();
        else director.Accept(CombatVisualPlayback.Current, now);
        bool animated = settings.AnimateCombat && !settings.ReducedCombatMotion && !instant;
        if (settings.CombatVisualEffects && !settings.ReducedCombatMotion && !instant && !CombatPlaybackControls.Paused)
            DrawPortalAtmosphere(context, destination, now);
        bool hold = CombatSequencePresenter.IsManualPlaybackWaiting;
        var heroPose = director.GetPose(false, now, hold);
        var enemyPose = director.GetPose(true, now, hold);
        double Elapsed(CombatVisualDirector.Pose pose) => hold
            ? Math.Min(now - pose.Started, pose.Duration * .5) : now - pose.Started;
        double heroTravel = animated ? BattleMotion.Approach(director.Action, heroId, heroPose.Clip, Elapsed(heroPose), heroPose.Duration) : 0;
        double enemyTravel = animated ? BattleMotion.Approach(director.Action, enemyId, enemyPose.Clip, Elapsed(enemyPose), enemyPose.Duration) : 0;
        string? family = EnemyVisualCatalog.Resolve(EnemyName);
        var heroStage = destination.Translate(new Vector(destination.Width * .21 * heroTravel, 0));
        var enemyStage = destination.Translate(new Vector(-destination.Width * .21 * enemyTravel, 0));
        var shadow = new SolidColorBrush(Color.Parse("#70000000"));
        context.DrawEllipse(shadow, null, new Point(heroStage.X + destination.Width * .30, destination.Y + destination.Height * .70),
            destination.Width * .045, destination.Height * .018);
        context.DrawEllipse(shadow, null, new Point(enemyStage.X + destination.Width * .69, destination.Y + destination.Height * .70),
            destination.Width * .045, destination.Height * .018);
        var resolving = CombatResolutionState.Current;
        if (resolving.Phase != "RESULT" && resolving.Visual is { SourceId: not 0 } acting && (acting.SourceId == heroId || acting.SourceId == enemyId))
        {
            bool enemyActing = acting.SourceId == enemyId;
            var actorStage = enemyActing ? enemyStage : heroStage;
            context.DrawEllipse(null, new Pen(new SolidColorBrush(enemyActing ? BattleOverlay.Crimson : BattleOverlay.Sulfur), 2),
                new Point(actorStage.X + destination.Width * (enemyActing ? .69 : .30), destination.Y + destination.Height * .70),
                destination.Width * .05, destination.Height * .02);
        }
        if (!animated || heroClips == null || !heroClips.Draw(context, heroStage, heroPose.Clip, Elapsed(heroPose), heroPose.Duration))
            context.DrawImage(fighter!, heroStage);
        if (enemyArtwork != null)
        {
            double ghostOpacity = animated && enemyPose.Clip == "evade" && BattleMotion.IsSpectral(family)
                ? 1 - .85 * Math.Sin(Math.PI * Math.Clamp(Elapsed(enemyPose) / Math.Max(.04, enemyPose.Duration), 0, 1)) : 1;
            using var ghost = context.PushOpacity(ghostOpacity);
            if (!animated || enemyClips == null || !enemyClips.Draw(context, enemyStage, enemyPose.Clip, Elapsed(enemyPose), enemyPose.Duration))
                context.DrawImage(enemyArtwork, enemyStage);
        }
        else
        {
            var marker = new Rect(destination.X + destination.Width * .62,
                destination.Y + destination.Height * .39, destination.Width * .2, destination.Height * .32);
            context.DrawRectangle(new SolidColorBrush(Color.Parse("#CC101410")),
                new Pen(new SolidColorBrush(Color.Parse("#C8D52D")), 1), marker);
            Label(context, "?", new Point(marker.Center.X - 7, marker.Center.Y - 17), 26);
            string name = EnemyName.Length <= 16 ? EnemyName : EnemyName[..13] + "...";
            using var markerClip = context.PushClip(marker);
            Label(context, name, new Point(marker.X + 5, marker.Bottom - 18), 10);
        }
        if (settings.CombatVisualEffects && !instant)
        {
            if (!settings.ReducedCombatMotion)
            {
                DrawCharge(context, destination, heroPose, false, now);
                DrawCharge(context, destination, enemyPose, true, now);
            }
            DrawEffect(context, destination, now, settings.ReducedCombatMotion);
        }
        if (director.Action?.Delivery == "projectile" && settings.CombatVisualEffects && !instant && !settings.ReducedCombatMotion)
            DrawProjectile(context, destination, director.Action.SourceId == enemyId ? enemyPose : heroPose, now, hold);
        if (presentation != null)
            BattleOverlay.Draw(context, destination, presentation, director, now, settings.CombatVisualEffects && !instant && !settings.ReducedCombatMotion);
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#46514A")), 1), destination.Deflate(.5));
        if (PreviewDemon) Label(context, "ACTION LAB", bounds.TopLeft + new Vector(8, 3), 10);
    }

    private static void DrawPortalAtmosphere(DrawingContext context, Rect stage, double now)
    {
        // A fixed, bounded field of motes; no simulation RNG and no particle backlog.
        for (int i = 0; i < 9; i++)
        {
            double phase = (now * .14 + i * .113) % 1;
            double x = stage.X + stage.Width * (.46 + .10 * Math.Sin(i * 2.4 + phase));
            double y = stage.Y + stage.Height * (.64 - phase * .28);
            using var fade = context.PushOpacity(Math.Sin(phase * Math.PI) * .45);
            context.DrawEllipse(new SolidColorBrush(Color.Parse("#E99D56")), null, new Point(x,y), 1.2, 2.2);
        }
    }

    private void DrawProjectile(DrawingContext context, Rect stage, CombatVisualDirector.Pose pose, double now, bool hold)
    {
        if (!pose.Clip.StartsWith("prepare-", StringComparison.Ordinal) || director.Action is not { } action
            || action.Damage <= 0 || action.SourceId == action.TargetId) return;
        double t = Math.Clamp((now - pose.Started) / Math.Max(.04, pose.Duration), 0, 1);
        if (hold) t = Math.Min(t, .5);
        bool sourceEnemy = action.SourceId == enemyId;
        var start = new Point(stage.X + stage.Width * (sourceEnemy ? .69 : .3), stage.Y + stage.Height * .48);
        var end = new Point(stage.X + stage.Width * (sourceEnemy ? .3 : .69), stage.Y + stage.Height * (sourceEnemy ? .48 : BattleMotion.TargetY(EnemyVisualCatalog.Resolve(EnemyName))));
        double flight = Math.Min(.94, Math.Max(0, (t - .2) / .8));
        var center = start + (end - start) * flight + new Vector(0, -Math.Sin(flight * Math.PI) * stage.Height * .07);
        var brush = new SolidColorBrush(Color.Parse("#F4EAAA"));
        context.DrawLine(new Pen(new SolidColorBrush(Color.Parse("#FF3455")), 4), center - (end-start) * .06, center);
        context.DrawEllipse(brush, null, center, 4, 4);
    }

    private static void DrawCharge(DrawingContext context, Rect stage, CombatVisualDirector.Pose pose, bool enemy, double now)
    {
        if (pose.Clip != "prepare-cast") return;
        double age = Math.Max(0, now - pose.Started);
        double charge = Math.Min(1, age / Math.Max(.04, pose.Duration * .3));
        var center = new Point(stage.X + stage.Width * (enemy ? .69 : .3), stage.Y + stage.Height * .48);
        var brush = new SolidColorBrush(Color.Parse("#DFF28C"));
        double radius = stage.Height * (.035 + .018 * charge);
        context.DrawEllipse(null, new Pen(brush, 1.5), center, radius, radius * .55);
        for (int i = 0; i < 6; i++)
        {
            // Motion stops once charged, including manual-step holds.
            double angle = i * Math.Tau / 6 + charge * 1.5;
            var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
            context.DrawEllipse(brush, null, center + direction * radius * (2 - charge), 2, 2);
        }
    }

    private void DrawEffect(DrawingContext context, Rect stage, double now, bool reducedMotion)
    {
        if (director.Effect is not { } cue) return;
        double age = now - cue.Timestamp / (double)Stopwatch.Frequency;
        double duration = Math.Max(.08, cue.DurationMs / 1000d);
        if (CombatSequencePresenter.IsManualPlaybackWaiting) age = Math.Min(age, duration * .4);
        if (age < 0 || age > duration) return;
        double progress = age / duration;
        var a = cue.Action;
        double targetX = a.TargetId == enemyId ? .69 : .3;
        var center = new Point(stage.X + stage.Width * targetX, stage.Y + stage.Height *
            (a.TargetId == enemyId ? BattleMotion.TargetY(EnemyVisualCatalog.Resolve(EnemyName)) : .48));
        var color = cue.Phase is "heal" or "effect" ? Color.Parse("#DFF28C") : Color.Parse("#FF234B");
        using var opacity = context.PushOpacity(1 - progress);
        string label = cue.Phase switch
        {
            "impact" => (a.Critical ? "CRIT " : "") + a.Damage,
            "heal" => "+" + a.Heal,
            "miss" => "MISS",
            "guard" => "DEFEND",
            _ => "EFFECT"
        };
        if (!reducedMotion)
        {
            double radius = stage.Height * (.05 + progress * .09);
            var pen = new Pen(new SolidColorBrush(color), a.Critical ? 4 : 2);
            if (cue.Phase is "heal" or "effect" or "guard")
                context.DrawEllipse(null, pen, center, radius, radius * .6);
            else if (cue.Phase == "impact")
            {
                double burst = 1 - Math.Pow(1 - progress, 3);
                double reach = stage.Height * (.045 + burst * (a.Critical ? .18 : .12));
                var hot = new SolidColorBrush(Color.Parse("#F6F0BC"));
                if (progress < .18)
                {
                    using var flash = context.PushOpacity((.18 - progress) * 3);
                    context.DrawEllipse(hot, null, center, reach * .8, reach * .8);
                }
                for (int i = 0; i < 14; i++)
                {
                    double angle = i * Math.PI * 2 / 14 + (a.Id % 17) * .17;
                    var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
                    double length = reach * (i % 3 == 0 ? 1.2 : .75);
                    var spark = new Pen(i % 2 == 0 ? hot : new SolidColorBrush(color),
                        Math.Max(1, stage.Height * .007 * (1 - progress)));
                    context.DrawLine(spark, center + direction * length * .6, center + direction * length);
                }
                double slash = a.SourceId == enemyId ? -1 : 1;
                var start = a.Style == "cast"
                    ? new Point(stage.X + stage.Width * (a.SourceId == enemyId ? .69 : .3), center.Y)
                    : center + new Vector(-reach * slash, reach * .7);
                var end = a.Style == "cast" ? center : center + new Vector(reach * slash, -reach * .7);
                context.DrawLine(new Pen(new SolidColorBrush(color), stage.Height * .018), start, end);
                context.DrawLine(new Pen(hot, stage.Height * .005), start, end);
                if (a.Critical)
                    context.DrawEllipse(null, new Pen(hot, 2), center, reach, reach * .45);
            }
        }
        Label(context, label, center + new Vector(-20, -25 - (reducedMotion ? 0 : progress * 25)), 16);
    }

    private static void Label(DrawingContext context, string value, Point point, double size)
    {
        var text = new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Consolas"), size, new SolidColorBrush(Color.Parse("#EEEAD7")));
        context.DrawText(text, point);
    }

    public void Dispose()
    {
        background?.Dispose(); fighter?.Dispose(); enemyArtwork?.Dispose();
        background = fighter = enemyArtwork = null;
        loadedEnemyAsset = null;
        heroAsset = "fighter"; arenaAsset = "arena-background";
        heroClips?.Dispose(); enemyClips?.Dispose();
        heroClips = enemyClips = null;
        heroClipsAttempted = false;
        director.Suspend();
    }
}
