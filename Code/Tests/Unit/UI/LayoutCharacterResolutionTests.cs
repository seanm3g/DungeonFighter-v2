using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Display.Render;

namespace RPGGame.Tests.Unit.UI
{
    /// <summary>
    /// Regression: strip/title layout character must prefer registry active character over stale UI context reference.
    /// </summary>
    public static class LayoutCharacterResolutionTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            PreferActiveOverStaleContext(ref run, ref passed, ref failed);
            FallsBackToContextWhenNoActive(ref run, ref passed, ref failed);

            TestBase.PrintSummary("LayoutCharacterResolutionTests", run, passed, failed);
        }

        private static void PreferActiveOverStaleContext(ref int run, ref int passed, ref int failed)
        {
            var active = TestDataBuilders.Character().WithName("ActiveStrip").Build();
            var stale = TestDataBuilders.Character().WithName("StaleContext").Build();
            var stateManager = new GameStateManager();
            stateManager.SetCurrentPlayer(active);

            var state = new RenderState
            {
                CurrentCharacter = stale,
                NeedsRender = true
            };

            var resolved = RenderCoordinator.ResolveLayoutCharacter(state, stateManager);
            TestBase.AssertTrue(
                ReferenceEquals(active, resolved),
                "active character wins over stale context reference",
                ref run, ref passed, ref failed);
        }

        private static void FallsBackToContextWhenNoActive(ref int run, ref int passed, ref int failed)
        {
            var ctxOnly = TestDataBuilders.Character().WithName("ContextOnly").Build();
            var state = new RenderState
            {
                CurrentCharacter = ctxOnly,
                NeedsRender = true
            };

            var resolved = RenderCoordinator.ResolveLayoutCharacter(state, new GameStateManager());
            TestBase.AssertTrue(
                ReferenceEquals(ctxOnly, resolved),
                "falls back to context when no active character is registered",
                ref run, ref passed, ref failed);
        }
    }
}
