using System;
using System.Threading.Tasks;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.Narrative;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Live ComfyUI combat-scene JSON feed.</summary>
    public static class CombatSceneLiveFeedTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatSceneLiveFeed Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestPublishScenesBuildsEnvelope();
            TestHttpRoutes();
            TestWaitReturnsWhenRevisionAdvances();
            TestMutedCombatDoesNotBind();
            TestPathAndQueryParsing();

            TestBase.PrintSummary("CombatSceneLiveFeed Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static void TestPublishScenesBuildsEnvelope()
        {
            Console.WriteLine("--- PublishScenes builds revisioned envelope ---");
            CombatSceneLiveFeed.ResetForTests();
            try
            {
                var establishing = CombatScenePacketBuilder.BuildEstablishingShot("Coral Garden");
                var beat = new CombatScenePacket
                {
                    Character = "Basil Flameweaver",
                    Action = "lunging mace slam",
                    Target = "Boar",
                    Environment = "Coral Garden",
                    Emotion = "focused and aggressive",
                    Camera = CombatScenePacketBuilder.DefaultCamera,
                    Composition = CombatScenePacketBuilder.CornerActionComposition,
                    Effects = new[] { "dust" }
                };

                CombatSceneLiveFeed.PublishScenes(new[] { establishing, beat }, fightId: "fight-test", status: "active");
                var snap = CombatSceneLiveFeed.Current;

                TestBase.AssertEqual(1L, snap.Revision, "first publish is revision 1",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(2, snap.BeatCount, "establishing + beat",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(1, snap.BeatIndex, "latest is last scene",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("fight-test", snap.FightId, "fight id preserved",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual("Basil Flameweaver", snap.Latest?.Character, "latest character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(CombatSceneLiveFeed.EnvelopeJson.Contains("\"revision\": 1", StringComparison.Ordinal)
                        || CombatSceneLiveFeed.EnvelopeJson.Contains("\"revision\":1", StringComparison.Ordinal),
                    "envelope JSON includes revision",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(CombatSceneLiveFeed.LatestJson.Contains("lunging mace slam", StringComparison.Ordinal),
                    "latest JSON is the action beat",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(CombatSceneLiveFeed.ScenesJson.TrimStart().StartsWith("[", StringComparison.Ordinal),
                    "scenes JSON is an array",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatSceneLiveFeed.ResetForTests();
            }
        }

        private static void TestHttpRoutes()
        {
            Console.WriteLine("--- HTTP router serves envelope / latest / scenes / health ---");
            CombatSceneLiveFeed.ResetForTests();
            try
            {
                var scenes = new[]
                {
                    CombatScenePacketBuilder.BuildEstablishingShot("crypt"),
                    new CombatScenePacket
                    {
                        Character = "Hero",
                        Action = "strike",
                        Target = "Rat",
                        Environment = "crypt",
                        Emotion = "focused",
                        Camera = CombatScenePacketBuilder.DefaultCamera,
                        Composition = CombatScenePacketBuilder.CornerActionComposition
                    }
                };
                CombatSceneLiveFeed.PublishScenes(scenes, fightId: "http-test");

                var health = CombatSceneLiveFeedHttp.Handle("/health", 7877);
                TestBase.AssertEqual(200, health.StatusCode, "health status",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(health.Body.Contains("dungeonfighter-combat-scene", StringComparison.Ordinal),
                    "health names service",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var envelope = CombatSceneLiveFeedHttp.Handle("/combat-scene", 7877);
                TestBase.AssertEqual(200, envelope.StatusCode, "envelope status",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(envelope.Body.Contains("\"scenes\"", StringComparison.Ordinal),
                    "envelope has scenes",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var latest = CombatSceneLiveFeedHttp.Handle("/combat-scene/latest", 7877);
                TestBase.AssertTrue(latest.Body.Contains("Hero", StringComparison.Ordinal),
                    "latest has character",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var array = CombatSceneLiveFeedHttp.Handle("/combat-scene/scenes", 7877);
                TestBase.AssertTrue(array.Body.TrimStart().StartsWith("[", StringComparison.Ordinal),
                    "scenes path returns array",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var missing = CombatSceneLiveFeedHttp.Handle("/nope", 7877);
                TestBase.AssertEqual(404, missing.StatusCode, "unknown path 404",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatSceneLiveFeed.ResetForTests();
            }
        }

        private static void TestWaitReturnsWhenRevisionAdvances()
        {
            Console.WriteLine("--- Wait returns after a newer revision ---");
            CombatSceneLiveFeed.ResetForTests();
            try
            {
                CombatSceneLiveFeed.PublishScenes(
                    new[] { CombatScenePacketBuilder.BuildEstablishingShot("hall") },
                    fightId: "wait-1");
                long since = CombatSceneLiveFeed.Current.Revision;

                var publishTask = Task.Run(async () =>
                {
                    await Task.Delay(50);
                    CombatSceneLiveFeed.PublishScenes(
                        new[]
                        {
                            CombatScenePacketBuilder.BuildEstablishingShot("hall"),
                            new CombatScenePacket
                            {
                                Character = "A",
                                Action = "jab",
                                Target = "B",
                                Environment = "hall",
                                Emotion = "fierce",
                                Camera = CombatScenePacketBuilder.DefaultCamera
                            }
                        },
                        fightId: "wait-1");
                });

                var response = CombatSceneLiveFeedHttp.Handle(
                    $"/combat-scene/wait?since={since}&timeoutMs=2000",
                    7877);
                publishTask.Wait(3000);

                TestBase.AssertEqual(200, response.StatusCode, "wait status",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(CombatSceneLiveFeed.Current.Revision > since,
                    "revision advanced past since",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(response.Body.Contains("jab", StringComparison.Ordinal),
                    "wait body includes new beat",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatSceneLiveFeed.ResetForTests();
            }
        }

        private static void TestMutedCombatDoesNotBind()
        {
            Console.WriteLine("--- Muted combat does not bind the live feed ---");
            CombatSceneLiveFeed.ResetForTests();
            try
            {
                using (CombatUiMuteScope.Begin(muted: true))
                {
                    var hero = new Character("Mute Hero", 1);
                    var enemy = new Enemy("Mute Rat", 1, 10, 5, 5, 5, 5);
                    CombatSceneLiveFeed.BindFight(
                        hero,
                        enemy,
                        room: null,
                        dungeon: null,
                        narrativeProvider: () => null);
                    CombatSceneLiveFeed.NotifyChanged();
                }

                TestBase.AssertEqual(0L, CombatSceneLiveFeed.Current.Revision,
                    "muted bind/publish leaves revision at 0",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CombatSceneLiveFeed.ResetForTests();
            }
        }

        private static void TestPathAndQueryParsing()
        {
            Console.WriteLine("--- Path/query helpers normalize URLs ---");
            CombatSceneLiveFeedHttp.SplitUrl(
                "http://127.0.0.1:7877/combat-scene/wait?since=3&timeoutMs=1000",
                out string path,
                out string query);
            TestBase.AssertEqual("/combat-scene/wait",
                CombatSceneLiveFeedHttp.NormalizePath(path),
                "absolute URL path",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            var map = CombatSceneLiveFeedHttp.ParseQuery(query);
            TestBase.AssertEqual("3", map["since"], "since query",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("1000", map["timeoutMs"], "timeout query",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual("/combat-scene/latest",
                CombatSceneLiveFeedHttp.NormalizePath("/combat-scene/latest/"),
                "trailing slash stripped",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }
    }
}
