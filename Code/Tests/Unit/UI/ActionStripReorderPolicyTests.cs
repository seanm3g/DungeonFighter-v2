using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Handlers;

namespace RPGGame.Tests.Unit.UI
{
    public static class ActionStripReorderPolicyTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionStripReorderPolicy Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            var dummyEnemy = new Enemy(
                name: "Test",
                level: 1,
                maxHealth: 50,
                strength: 8,
                agility: 6,
                technique: 4,
                intelligence: 4,
                armor: 0);

            TestBase.AssertTrue(!ActionStripReorderPolicy.AllowsReorder(GameState.Combat, null),
                "Combat disallows strip reorder", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.Inventory, null),
                "Inventory allows strip reorder (no enemy)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.Inventory, dummyEnemy),
                "Inventory allows strip reorder even if enemy arg non-null", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.Dungeon, null),
                "Dungeon allows strip reorder when exploring (combat uses GameState.Combat + combat display lock)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.Dungeon, dummyEnemy),
                "Dungeon allows strip reorder regardless of enemy arg", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.DungeonCompletion, null),
                "DungeonCompletion allows strip reorder", ref run, ref passed, ref failed);
            TestBase.AssertTrue(!ActionStripReorderPolicy.AllowsReorder(GameState.MainMenu, null),
                "MainMenu disallows strip reorder", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.GameLoop, null),
                "GameLoop allows strip reorder (hub / actions menu)", ref run, ref passed, ref failed);
            TestBase.AssertTrue(ActionStripReorderPolicy.AllowsReorder(GameState.CharacterInfo, null),
                "CharacterInfo allows strip reorder", ref run, ref passed, ref failed);

            TestBase.PrintSummary("ActionStripReorderPolicy Tests", run, passed, failed);
        }
    }
}
