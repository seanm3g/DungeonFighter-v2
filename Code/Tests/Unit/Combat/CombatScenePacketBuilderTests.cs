using System;
using System.IO;
using System.Linq;
using RPGGame;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Combat-log scene JSON packets saved on right-click.</summary>
    public static class CombatScenePacketBuilderTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatScenePacketBuilder Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestAggressiveSwordSlashShape();
            TestCloakOnlyWhenLegsLookLikeCloak();
            TestEstablishingShotIsRoomOnly();
            TestLogUsesEveryBattleEvent();
            TestEnemyBeatDoesNotInheritHeroActionOrGear();
            TestSnapshotWhenNoEvents();
            TestSerializeSingleObjectAndArray();
            TestWriterCreatesFile();
            TestExportRejectsEmptyState();

            TestBase.PrintSummary("CombatScenePacketBuilder Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestAggressiveSwordSlashShape()
        {
            Console.WriteLine("--- Aggressive sword slash scene matches export schema ---");
            var hero = CreateHero();
            var enemy = new Enemy("skeleton knight", 5, 80, 10, 10, 10, 10);
            enemy.PoisonPercentOfMaxHealth = 5;
            var slash = new Action("SLASH") { BlockPercent = 0 };
            var evt = new BattleEvent
            {
                Actor = hero.Name,
                Target = enemy.Name,
                Action = "SLASH",
                IsSuccess = true,
                IsCombo = true,
                Damage = 12
            };

            var packet = CombatScenePacketBuilder.BuildFromEvent(
                evt, hero, enemy, "haunted crypt", slash);

            TestBase.AssertEqual("Rhys Duskstar", packet.Character, "character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("lunging sword slash", packet.Action, "action phrase",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("skeleton knight", packet.Target, "target",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("haunted crypt", packet.Environment, "environment",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("focused and aggressive", packet.Emotion, "emotion",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.DefaultCamera, packet.Camera, "camera",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.CornerActionComposition, packet.Composition,
                "composition locks room + corner fighters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var gear = packet.Gear;
            TestBase.AssertTrue(gear != null, "equipped hero has gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Ironthorn Helm", gear!.Helmet, "helmet",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Wolfhide Mantle", gear.Cloak, "cloak from mantle legs",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Ashplate Armor", gear.Armor, "armor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Emberfang", gear.WeaponMain, "weapon_main",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(gear.WeaponOffhand == null, "weapon_offhand is null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Effects.Contains("dust"), "crypt dust",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Effects.Contains("faint green necrotic glow"), "necrotic effect",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestCloakOnlyWhenLegsLookLikeCloak()
        {
            Console.WriteLine("--- Legs that are not a cloak stay off the cloak field ---");
            var hero = CreateHero();
            hero.Equipment.Legs = new LegsItem("Iron Greaves", 1, 2);
            var evt = new BattleEvent
            {
                Actor = hero.Name,
                Target = "foe",
                Action = "STRIKE",
                IsSuccess = true
            };

            var packet = CombatScenePacketBuilder.BuildFromEvent(evt, hero, null, "forest");
            TestBase.AssertTrue(packet.Gear?.Cloak == null, "pants are not a cloak",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEstablishingShotIsRoomOnly()
        {
            Console.WriteLine("--- Establishing shot is environment-only with wide camera ---");
            var packet = CombatScenePacketBuilder.BuildEstablishingShot("Crumbling Hall");
            TestBase.AssertTrue(packet.Character == null, "no character on establishing shot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Action == null, "no action on establishing shot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Target == null, "no target on establishing shot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("Crumbling Hall", packet.Environment, "environment is the subject",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("weathered and still", packet.Emotion, "room atmosphere",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.EstablishingCamera, packet.Camera,
                "ultra-wide empty-room establishing camera",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.CornerActionComposition, packet.Composition,
                "composition present on establishing shot for ComfyUI lock",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Gear == null, "no gear on establishing shot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Effects.Contains("dust"), "crumbling hall dust",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestLogUsesEveryBattleEvent()
        {
            Console.WriteLine("--- BuildLog emits establishing shot then one scene per battle event ---");
            var hero = CreateHero();
            var enemy = new Enemy("skeleton knight", 5, 80, 10, 10, 10, 10);
            var narrative = new BattleNarrative(hero.Name, enemy.Name, "haunted crypt", 100, 80);
            narrative.AddEvent(new BattleEvent
            {
                Actor = hero.Name,
                Target = enemy.Name,
                Action = "STRIKE",
                IsSuccess = true
            });
            narrative.AddEvent(new BattleEvent
            {
                Actor = enemy.Name,
                Target = hero.Name,
                Action = "BONE SLASH",
                IsSuccess = true
            });

            var log = CombatScenePacketBuilder.BuildLog(hero, enemy, null, null, narrative);
            TestBase.AssertEqual(3, log.Count, "establishing plus two events",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(log[0].Character == null, "first row is room-only establishing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.EstablishingCamera, log[0].Camera,
                "first camera is establishing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(hero.Name, log[1].Character, "second actor is hero",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(enemy.Name, log[2].Character, "third actor is enemy",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("haunted crypt", log[0].Environment, "narrative location",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestEnemyBeatDoesNotInheritHeroActionOrGear()
        {
            Console.WriteLine("--- Enemy beat keeps its own action; no placeholder Bludgeon gear ---");
            var hero = CreateHero();
            var enemy = new Enemy("Rats", 1, 40, 2, 3, 2, 3);
            enemy.Equipment.Weapon = new WeaponItem("Bludgeon", 1, 6, 0.0, WeaponType.Mace);
            var slam = new Action("SLAM") { BlockPercent = 0 };
            var evt = new BattleEvent
            {
                Actor = enemy.Name,
                Target = hero.Name,
                Action = "RAT SWARM",
                IsSuccess = false,
                NaturalRoll = 4
            };

            var packet = CombatScenePacketBuilder.BuildFromEvent(
                evt, hero, enemy, "Trap Room", lastPlayerAction: slam);

            TestBase.AssertEqual("Rats", packet.Character, "enemy is actor",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("rat swarm", packet.Action, "uses event action, not hero SLAM",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            string enemyAction = packet.Action ?? "";
            TestBase.AssertFalse(
                enemyAction.Contains("mace", StringComparison.OrdinalIgnoreCase) ||
                enemyAction.Contains("slam", StringComparison.OrdinalIgnoreCase),
                "does not invent lunging mace slam from hero last action / enemy math weapon",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(packet.Gear == null, "enemy placeholder weapon omitted from gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.MissCamera, packet.Camera,
                "miss keeps ultra-wide corner framing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.CornerActionComposition, packet.Composition,
                "composition on enemy beat",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSnapshotWhenNoEvents()
        {
            Console.WriteLine("--- Empty narrative still opens with establishing then fighter snapshot ---");
            var hero = CreateHero();
            var enemy = new Enemy("skeleton knight", 5, 80, 10, 10, 10, 10);
            var room = new Environment("haunted crypt", "bones", true, "crypt");
            var log = CombatScenePacketBuilder.BuildLog(hero, enemy, room, null, narrative: null);
            TestBase.AssertEqual(2, log.Count, "establishing plus snapshot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(CombatScenePacketBuilder.EstablishingCamera, log[0].Camera,
                "opens with establishing shot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("haunted crypt", log[0].Environment, "room name on establishing",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("haunted crypt", log[1].Environment, "room name on snapshot",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestSerializeSingleObjectAndArray()
        {
            Console.WriteLine("--- Serialize one scene as object, many as array ---");
            var hero = CreateHero();
            var one = CombatScenePacketBuilder.BuildFromEvent(
                new BattleEvent { Actor = hero.Name, Target = "foe", Action = "STRIKE", IsSuccess = true },
                hero, null, "crypt");
            string single = CombatScenePacketBuilder.Serialize(new[] { one });
            TestBase.AssertTrue(single.TrimStart().StartsWith("{", StringComparison.Ordinal),
                "single scene is a JSON object",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(single.Contains("\"character\"", StringComparison.Ordinal),
                "has character key",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(single.Contains(": null", StringComparison.Ordinal),
                "null properties are omitted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(single.Contains("\"weapon_offhand\"", StringComparison.Ordinal),
                "null weapon_offhand is omitted",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string establishing = CombatScenePacketBuilder.Serialize(
                new[] { CombatScenePacketBuilder.BuildEstablishingShot("Crumbling Hall") });
            TestBase.AssertFalse(establishing.Contains("\"character\"", StringComparison.Ordinal),
                "establishing omits character",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(establishing.Contains("\"action\"", StringComparison.Ordinal),
                "establishing omits action",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(establishing.Contains("\"target\"", StringComparison.Ordinal),
                "establishing omits target",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(establishing.Contains("\"gear\"", StringComparison.Ordinal),
                "establishing omits empty gear",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(establishing.Contains("\"composition\"", StringComparison.Ordinal),
                "establishing includes composition lock",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(establishing.Contains(": null", StringComparison.Ordinal),
                "establishing has no null values",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string many = CombatScenePacketBuilder.Serialize(new[] { one, one });
            TestBase.AssertTrue(many.TrimStart().StartsWith("[", StringComparison.Ordinal),
                "multiple scenes are a JSON array",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestWriterCreatesFile()
        {
            Console.WriteLine("--- Writer writes timestamped JSON under a folder ---");
            var hero = CreateHero();
            var packet = CombatScenePacketBuilder.BuildFromEvent(
                new BattleEvent { Actor = hero.Name, Target = "foe", Action = "STRIKE", IsSuccess = true },
                hero, null, "crypt");
            string dir = Path.Combine(Path.GetTempPath(), "df-combat-logs-" + Guid.NewGuid().ToString("N"));
            try
            {
                string path = CombatSceneLogWriter.Write(
                    new[] { packet },
                    dir,
                    new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc));
                TestBase.AssertTrue(File.Exists(path), "file exists",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(path.Contains("Rhys-Duskstar", StringComparison.OrdinalIgnoreCase),
                    "filename uses character token",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                string json = File.ReadAllText(path);
                TestBase.AssertTrue(json.Contains("Rhys Duskstar", StringComparison.Ordinal),
                    "file contains character name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                try { Directory.Delete(dir, recursive: true); } catch { /* ignore temp cleanup */ }
            }

            TestBase.AssertEqual("Rhys-Duskstar", CombatSceneLogWriter.SanitizeFileToken("Rhys Duskstar"),
                "sanitize spaces to dashes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestExportRejectsEmptyState()
        {
            Console.WriteLine("--- Export requires a scene ---");
            bool ok = CombatSceneLogExport.TryBuild(
                null, null, null, null, null, null, out _, out _);
            TestBase.AssertFalse(ok, "empty state cannot export",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static Character CreateHero()
        {
            var hero = new Character("Rhys Duskstar", 5);
            hero.Equipment.Head = new HeadItem("Ironthorn Helm", 1, 1);
            hero.Equipment.Body = new ChestItem("Ashplate Armor", 1, 2);
            hero.Equipment.Legs = new LegsItem("Wolfhide Mantle", 1, 1);
            hero.Equipment.Weapon = new WeaponItem("Emberfang", 1, 10, 0.05, WeaponType.Sword);
            return hero;
        }
    }
}
