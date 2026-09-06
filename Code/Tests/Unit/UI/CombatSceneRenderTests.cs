using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RPGGame.Tests;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.CombatVisuals;

namespace RPGGame.Tests.Unit.UI;

/// <summary>Opt-in native offscreen rendering check; never opens a game window.</summary>
public static class CombatSceneRenderTests
{
    public static void RunAllTests()
    {
        int run = 0, passed = 0, failed = 0;
        if (Application.Current == null)
            AppBuilder.Configure<Application>().UsePlatformDetect().SetupWithoutStarting();
        foreach (string asset in new[] { "arena-background", "fighter-layer", "demon-layer", "bat-layer", "skeleton-layer" })
        {
            var uri = new Uri($"avares://DF/Visuals/{asset}.png");
            TestBase.AssertTrue(AssetLoader.Exists(uri), $"{asset} embedded for deployment", ref run, ref passed, ref failed);
            using var stream = AssetLoader.Open(uri);
            using var bitmap = new Bitmap(stream);
            TestBase.AssertEqual(new PixelSize(1600, 900), bitmap.PixelSize, "layer dimensions match", ref run, ref passed, ref failed);
        }
        var canvas = new GameCanvasControl();
        var visualSettings = GameConfiguration.Instance.UICustomization;
        bool priorInstant = DeveloperModeState.IsCombatLogInstant;
        bool priorAnimation = visualSettings.AnimateCombat, priorEffects = visualSettings.CombatVisualEffects;
        bool priorReduced = visualSettings.ReducedCombatMotion;
        var rosterPath = JsonLoader.FindGameDataFile(GameConstants.EnemiesJson)
            ?? GameConstants.TryGetExistingGameDataFilePath(GameConstants.EnemiesJson);
        TestBase.AssertTrue(rosterPath != null, "live enemy roster is available for coverage check", ref run, ref passed, ref failed);
        if (rosterPath != null)
        {
            using var roster = JsonDocument.Parse(File.ReadAllText(rosterPath));
            foreach (var enemy in roster.RootElement.EnumerateArray())
            {
                string name = enemy.GetProperty("name").GetString()!;
                TestBase.AssertTrue(EnemyVisualCatalog.Resolve(name) != null, $"{name} has authored artwork", ref run, ref passed, ref failed);
            }
        }
        foreach (string actor in EnemyVisualCatalog.Entries.Values.Append("fighter").Append("demon").Distinct())
        {
            using var clips = new RPGGame.UI.Avalonia.CombatVisuals.SpriteClipSet(actor);
            TestBase.AssertTrue(true, $"{actor} animation atlases load and validate", ref run, ref passed, ref failed);
            using var png = AssetLoader.Open(new Uri($"avares://DF/Visuals/{actor}-layer.png"));
            using var still = new Bitmap(png);
            TestBase.AssertEqual(new PixelSize(1600, 900), still.PixelSize, $"{actor} static fallback deploys", ref run, ref passed, ref failed);
        }
        TestBase.AssertEqual(3, SpriteClipSet.SelectFrame(10, 12, 5, false, true, 100, .6), "anticipation never reaches contact before reveal", ref run, ref passed, ref failed);
        TestBase.AssertEqual(5, SpriteClipSet.SelectFrame(10, 12, 5, false, false, .32, .6), "contact frame holds briefly on reveal", ref run, ref passed, ref failed);
        TestBase.AssertEqual(9, SpriteClipSet.SelectFrame(10, 12, 5, false, false, 1, .6), "one-shot animation settles on final frame", ref run, ref passed, ref failed);
        foreach (string weapon in new[] { "sword", "dagger", "mace", "wand", "unarmed" })
            foreach (string armor in new[] { "cloth", "plate" })
            {
                using var gearClips = new SpriteClipSet($"fighter-{weapon}-{armor}");
                TestBase.AssertTrue(true, $"{weapon}/{armor} equipment animations deploy", ref run, ref passed, ref failed);
            }
        foreach (string arena in new[] { "forest", "volcanic", "desert", "mountain", "wetland", "crypt", "cavern" })
            TestBase.AssertTrue(AssetLoader.Exists(new Uri($"avares://DF/Visuals/arena-{arena}.png")), $"{arena} stage deploys", ref run, ref passed, ref failed);
        var sampleHero = new Character("Visual test", 1) { Weapon = new WeaponItem("Test wand", weaponType: WeaponType.Wand), PoisonPercentOfMaxHealth = 3, IsStunned = true, StunTurnsRemaining = 2 };
        var sampleEnemy = new Enemy("Wolf", 1, 100, 8, 6, 4, 4, 2);
        var snapshot = BattlePresentation.Capture(sampleHero, sampleEnemy);
        TestBase.AssertEqual("fighter-wand-cloth", snapshot.HeroAsset, "equipped weapon controls hero model", ref run, ref passed, ref failed);
        sampleHero.EquipItem(new ChestItem("Plate", armor: 5), "Body");
        TestBase.AssertEqual("fighter-wand-plate", BattlePresentation.Capture(sampleHero, sampleEnemy).HeroAsset, "equipping armor changes model silhouette", ref run, ref passed, ref failed);
        TestBase.AssertEqual("desert", EnemyVisualCatalog.Arena("Mirage Hunter"), "authored biome overrides shared wolf family", ref run, ref passed, ref failed);
        TestBase.AssertTrue(snapshot.HeroStatuses.Any(s => s.Text == "STUN 2t") && snapshot.HeroStatuses.Any(s => s.Text == "POISON 3%"), "statuses show actual counters, not invented durations", ref run, ref passed, ref failed);
        using (var scene = new RPGGame.UI.Avalonia.CombatVisuals.CombatSceneRenderer())
        {
            scene.Configure(true, "Bat", false);
            TestBase.AssertEqual("bat-layer", scene.EnemyAssetKey, "bat resolves in normal combat", ref run, ref passed, ref failed);
            scene.Configure(true, "Skeleton", false);
            TestBase.AssertEqual("skeleton-layer", scene.EnemyAssetKey, "changing enemy replaces bat artwork", ref run, ref passed, ref failed);
            scene.Configure(true, " BAT ", true);
            TestBase.AssertEqual("bat-layer", scene.EnemyAssetKey, "mapped enemy overrides lab preview", ref run, ref passed, ref failed);
            scene.Configure(true, "Combatant", false);
            TestBase.AssertTrue(scene.EnemyAssetKey == null, "unknown name does not substring-match bat", ref run, ref passed, ref failed);
        }
        try
        {
            DeveloperModeState.SetCombatLogInstant(false);
            visualSettings.AnimateCombat = visualSettings.CombatVisualEffects = true;
            visualSettings.ReducedCombatMotion = false;
            canvas.Measure(new Size(1600, 900));
            canvas.Arrange(new Rect(0, 0, 1600, 900));
            canvas.ConfigureCombatScene(true, "Prototype demon", true);
            CombatArenaHudLayout.GetResolveStackBand(out int x, out int y, out int w, out int h);
            canvas.AddBorder(x, y, w, h, Colors.White);
            canvas.AddText(x + 2, y + 1, "ACTION RESOLUTION", Colors.White);
            using var target = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
            target.Render(canvas);
            var output = System.Environment.GetEnvironmentVariable("DEMON_FIGHTER_RENDER_CHECK");
            if (!string.IsNullOrWhiteSpace(output))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
                target.Save(output);
                foreach (string name in new[] { "Bat", "Skeleton", "Wolf", "Treant", "Sun Lance Scorpion", "Lich", "Lion Fish" })
                {
                    canvas.ConfigureCombatScene(true, name, false);
                    using var enemyTarget = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
                    enemyTarget.Render(canvas);
                    enemyTarget.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, name.ToLowerInvariant() + "-integration.png"));
                }
            }
                canvas.ConfigureCombatScene(true, "Bat", false, 101, 202);
                var visual = new RPGGame.Combat.Sequence.CombatVisualAction(999, 101, 202, true, true, 24, 0, false, "attack");
                RPGGame.Combat.Sequence.CombatVisualPlayback.Publish(visual, "impact", 600);
                canvas.Refresh();
                using var impactTarget = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
                impactTarget.Render(canvas);
                TestBase.AssertTrue(canvas.ActiveCombatEffect?.Action.Id == 999, "impact reaches scene renderer", ref run, ref passed, ref failed);
                if (!string.IsNullOrWhiteSpace(output))
                    impactTarget.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "impact-integration.png"));
                DeveloperModeState.SetCombatLogInstant(true);
                canvas.Refresh();
                using var instantTarget = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
                instantTarget.Render(canvas);
                TestBase.AssertTrue(canvas.ActiveCombatEffect == null, "instant mode suppresses animation and VFX", ref run, ref passed, ref failed);
                DeveloperModeState.SetCombatLogInstant(false);
                RPGGame.Combat.Sequence.CombatVisualPlayback.Clear();
                canvas.SetBattlePresentation(snapshot with { Rank = "Rare" });
                canvas.ConfigureCombatScene(true, "Wolf", false, 101, 202);
                canvas.Refresh();
                using var polished = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
                polished.Render(canvas);
                if (!string.IsNullOrWhiteSpace(output)) polished.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "polished-encounter.png"));
                FighterResolveActionStackState.Clear();
                var demoAction = new RPGGame.Action();
                demoAction.Name = "SLAM";
                demoAction.Description = "A heavy mace strike that crushes the target with overwhelming force.";
                for (int i = 0; i < 3; i++)
                {
                    FighterResolveActionStackState.BeginResolve(sampleHero, demoAction);
                    FighterResolveActionStackState.RevealCurrent();
                    FighterResolveActionStackState.ArchiveCurrent();
                }
                demoAction.Name = "BITE";
                demoAction.Description = "Lunge forward and tear at the fighter with sharp fangs.";
                FighterResolveActionStackState.BeginResolve(sampleEnemy, demoAction);
                FighterResolveActionStackState.RevealCurrent();
                FighterResolveActionStackState.ArchiveCurrent();
                FighterResolveActionStackState.BeginResolve(sampleEnemy, demoAction);
                FighterResolveActionStackState.RevealCurrent();
                FighterResolveActionStackState.MarkCurrentMissed();
                FighterResolveActionStackState.ApplyVisualResult(new RPGGame.Combat.Sequence.CombatVisualAction(900,
                    RPGGame.Combat.Sequence.CombatVisualPlayback.ActorId(sampleEnemy), RPGGame.Combat.Sequence.CombatVisualPlayback.ActorId(sampleHero),
                    true, false, 18, 0, false, "attack", Name: "BITE", Blocked: 5, ActionUsed: false), "impact");
                FighterResolveActionStackRenderer.Render(canvas);
                using var piles = new RenderTargetBitmap(new PixelSize(1600, 900), new Vector(96, 96));
                piles.Render(canvas);
                if (!string.IsNullOrWhiteSpace(output)) piles.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "action-piles.png"));
                foreach (var size in new[] { new PixelSize(1280, 800), new PixelSize(1920, 1080) })
                {
                    canvas.Measure(new Size(size.Width, size.Height));
                    canvas.Arrange(new Rect(0, 0, size.Width, size.Height));
                    using var resized = new RenderTargetBitmap(size, new Vector(96,96));
                    resized.Render(canvas); // update responsive grid before rebuilding cards
                    FighterResolveActionStackRenderer.Render(canvas);
                    resized.Render(canvas);
                    var cardBounds = FighterResolveActionStackRenderer.ActiveCardBounds;
                    TestBase.AssertTrue(cardBounds.Right * canvas.GetCharWidth() <= size.Width + 1 &&
                        cardBounds.Bottom * canvas.GetCharHeight() <= size.Height + 1,
                        $"active card stays inside {size.Width}x{size.Height}", ref run, ref passed, ref failed);
                    if (!string.IsNullOrWhiteSpace(output)) resized.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, $"battle-{size.Width}.png"));
                }
                canvas.Measure(new Size(1600,900));
                canvas.Arrange(new Rect(0,0,1600,900));
                FighterResolveActionStackState.Clear();
                canvas.SetBattlePresentation(null);
                canvas.ConfigureCombatScene(false, null, false);
                canvas.Clear();
                var itemRenderer = new RPGGame.UI.Avalonia.Renderers.Inventory.ItemComparisonRenderer(canvas, new RPGGame.UI.Avalonia.Renderers.ColoredTextWriter(canvas), new System.Collections.Generic.List<RPGGame.UI.Avalonia.ClickableElement>());
                itemRenderer.RenderItemComparison(4, 4, 110, 48, sampleHero, new WeaponItem("Tempered sword", tier: 2, baseDamage: 25), sampleHero.Weapon, "weapon");
                using var gearPreview = new RenderTargetBitmap(new PixelSize(1600,900), new Vector(96,96));
                gearPreview.Render(canvas);
                if (!string.IsNullOrWhiteSpace(output)) gearPreview.Save(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(output))!, "equipment-comparison.png"));
                canvas.ConfigureCombatScene(true, "Wolf", false, 101, 202);
            TestBase.AssertTrue(CombatArenaHudLayout.IllustratedSceneVisible, "scene survives layout and render", ref run, ref passed, ref failed);
            canvas.Clear();
            TestBase.AssertTrue(!CombatArenaHudLayout.IllustratedSceneVisible, "clear removes scene for menus", ref run, ref passed, ref failed);
            canvas.ConfigureCombatScene(true, "Other enemy", false);
            canvas.ConfigureCombatScene(false, null, false);
            TestBase.AssertTrue(!CombatArenaHudLayout.IllustratedSceneVisible, "disable releases scene", ref run, ref passed, ref failed);
        }
        finally
        {
            canvas.ConfigureCombatScene(false, null, false);
            DeveloperModeState.SetCombatLogInstant(priorInstant);
            visualSettings.AnimateCombat = priorAnimation;
            visualSettings.CombatVisualEffects = priorEffects;
            visualSettings.ReducedCombatMotion = priorReduced;
            RPGGame.Combat.Sequence.CombatVisualPlayback.Clear();
        }
        TestBase.PrintSummary("CombatSceneRenderTests", run, passed, failed);
    }
}



