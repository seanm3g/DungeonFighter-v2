using System;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Combat.Narrative;
using RPGGame.Config;
using RPGGame.Services.Llm;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Tests for victory limerick orchestration (mocked LLM; no network).</summary>
    public static class VictoryLimerickServiceTests
    {
        private static int _testsRun;
        private static int _testsPassed;
        private static int _testsFailed;

        private static readonly string ValidLimerick =
            "There once was the tara-weaving wanderer\n" +
            "who fought through a crypt with a plan\n" +
            "The Wight took a swing\n" +
            "but steel had its sting\n" +
            "and silence returned to the land.";

        private static readonly string LimerickWithHeroName =
            "There once was a hero named Taran\n" +
            "who fought through a crypt with a plan\n" +
            "The Wight took a swing\n" +
            "but steel had its sting\n" +
            "and silence returned to the land.";

        public static void RunAllTests()
        {
            Console.WriteLine("=== VictoryLimerickService Tests ===\n");
            _testsRun = _testsPassed = _testsFailed = 0;

            TestDisabledReturnsNull().GetAwaiter().GetResult();
            TestEnabledMockClientReturnsLimerick().GetAwaiter().GetResult();
            TestApiFailureFallsBackToFlavorText().GetAwaiter().GetResult();
            TestNameLeakFallsBackToFlavorText().GetAwaiter().GetResult();
            TestHeroLossReturnsNull().GetAwaiter().GetResult();
            TestFlavorTextFallbackDirect();
            TestPrefetchThenDeliverUsesPrefetchResult().GetAwaiter().GetResult();
            TestBeginPrefetchNoOpsWhenDisabled();
            TestClearPrefetchDropsInFlight();
            TestBeginPrefetchNoOpsUnlessFinaleRoom();

            TestBase.PrintSummary("VictoryLimerickService Tests", _testsRun, _testsPassed, _testsFailed);
        }

        private static async Task TestDisabledReturnsNull()
        {
            Console.WriteLine("--- Disabled returns null ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = false,
                ApiKey = "sk-test"
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            string? result = await svc.GenerateAsync(SamplePacket(heroWon: true));
            TestBase.AssertTrue(result == null, "disabled feature returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static async Task TestEnabledMockClientReturnsLimerick()
        {
            Console.WriteLine("--- Enabled mock client returns limerick ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            string? result = await svc.GenerateAsync(SamplePacket(heroWon: true));
            TestBase.AssertTrue(result != null, "mock client yields limerick",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(5, VictoryLimerickValidator.SplitToDisplayLines(result!).Count,
                "result is five lines",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static async Task TestApiFailureFallsBackToFlavorText()
        {
            Console.WriteLine("--- API failure falls back to FlavorText ---");
            FlavorText.Reload();

            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(null)); // simulate API failure / empty

            string? result = await svc.GenerateAsync(SamplePacket(heroWon: true));
            TestBase.AssertTrue(result != null, "FlavorText fallback produces limerick when bank present",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (result != null)
            {
                TestBase.AssertFalse(result.Contains("Taran", StringComparison.OrdinalIgnoreCase),
                    "fallback must not print literal hero name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertEqual(5, VictoryLimerickValidator.SplitToDisplayLines(result).Count,
                    "fallback is five lines",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static async Task TestNameLeakFallsBackToFlavorText()
        {
            Console.WriteLine("--- Name leak from model falls back to FlavorText ---");
            FlavorText.Reload();

            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(LimerickWithHeroName));

            string? result = await svc.GenerateAsync(SamplePacket(heroWon: true));
            TestBase.AssertTrue(result != null, "FlavorText used when model prints hero name",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (result != null)
            {
                TestBase.AssertFalse(result.Contains("Taran", StringComparison.OrdinalIgnoreCase),
                    "delivered limerick must not contain hero name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static async Task TestHeroLossReturnsNull()
        {
            Console.WriteLine("--- Hero loss returns null ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test"
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            string? result = await svc.GenerateAsync(SamplePacket(heroWon: false));
            TestBase.AssertTrue(result == null, "loss packet returns null",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestFlavorTextFallbackDirect()
        {
            Console.WriteLine("--- FlavorText fallback direct ---");
            FlavorText.Reload();
            string? result = VictoryLimerickService.TryFlavorTextFallback(SamplePacket(heroWon: true));
            TestBase.AssertTrue(result != null, "direct fallback works when bank loaded",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            if (result != null)
            {
                TestBase.AssertFalse(result.Contains("Taran", StringComparison.OrdinalIgnoreCase),
                    "direct fallback omits literal hero name",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static async Task TestPrefetchThenDeliverUsesPrefetchResult()
        {
            Console.WriteLine("--- Prefetch then deliver uses prefetch result ---");
            int completeCalls = 0;
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new CountingStubLlmClient(ValidLimerick, () => { completeCalls++; }));

            svc.SetFinaleRoomPrefetchAllowed(true);
            svc.BeginPrefetch(SamplePacket(heroWon: true));
            TestBase.AssertTrue(svc.HasPrefetch, "prefetch outstanding after BeginPrefetch",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string? delivered = null;
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            svc.DeliverPrefetchOrRequest(SamplePacket(heroWon: true), (System.Action<string>)(text =>
            {
                delivered = text;
                done.TrySetResult(true);
            }));

            await Task.WhenAny(done.Task, Task.Delay(5000));
            TestBase.AssertTrue(delivered != null, "deliver receives limerick",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertEqual(1, completeCalls, "LLM called once via prefetch (not again on deliver)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertFalse(svc.HasPrefetch, "prefetch cleared after deliver",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBeginPrefetchNoOpsWhenDisabled()
        {
            Console.WriteLine("--- BeginPrefetch no-ops when disabled ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = false,
                ApiKey = "sk-test"
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            svc.SetFinaleRoomPrefetchAllowed(true);
            svc.BeginPrefetch(SamplePacket(heroWon: true));
            TestBase.AssertFalse(svc.HasPrefetch, "disabled feature does not prefetch",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestClearPrefetchDropsInFlight()
        {
            Console.WriteLine("--- ClearPrefetch drops in-flight handle ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            svc.SetFinaleRoomPrefetchAllowed(true);
            svc.BeginPrefetch(SamplePacket(heroWon: true));
            svc.ClearPrefetch();
            TestBase.AssertFalse(svc.HasPrefetch, "ClearPrefetch nulls outstanding task",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBeginPrefetchNoOpsUnlessFinaleRoom()
        {
            Console.WriteLine("--- BeginPrefetch no-ops unless finale room ---");
            var prefs = new OpenAiPreferences
            {
                VictoryLimerickEnabled = true,
                ApiKey = "sk-test",
                TimeoutSeconds = 10
            };
            var svc = new VictoryLimerickService(
                () => prefs,
                _ => new StubLlmClient(ValidLimerick));

            svc.SetFinaleRoomPrefetchAllowed(false);
            svc.BeginPrefetch(SamplePacket(heroWon: true));
            TestBase.AssertFalse(svc.HasPrefetch, "mid-dungeon rooms do not prefetch",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            svc.SetFinaleRoomPrefetchAllowed(true);
            svc.BeginPrefetch(SamplePacket(heroWon: true));
            TestBase.AssertTrue(svc.HasPrefetch, "finale room allows prefetch",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            svc.ClearPrefetch();
        }

        private static BattleLimerickPacket SamplePacket(bool heroWon) => new()
        {
            HeroName = "Taran",
            EnemyName = "Crypt Wight",
            Location = "Bone Crypt",
            HeroWon = heroWon,
            HeroHpLeft = 40,
            HeroMaxHp = 100,
            Highlights = new[] { "a critical slam" }
        };

        private sealed class StubLlmClient : ILlmTextClient
        {
            private readonly string? _response;

            public StubLlmClient(string? response) => _response = response;

            public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
                => Task.FromResult(_response);
        }

        private sealed class CountingStubLlmClient : ILlmTextClient
        {
            private readonly string? _response;
            private readonly System.Action _onComplete;

            public CountingStubLlmClient(string? response, System.Action onComplete)
            {
                _response = response;
                _onComplete = onComplete;
            }

            public Task<string?> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
            {
                _onComplete.Invoke();
                return Task.FromResult(_response);
            }
        }
    }
}
