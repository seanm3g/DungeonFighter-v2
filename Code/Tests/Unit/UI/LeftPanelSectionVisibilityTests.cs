using System;
using RPGGame;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Layout;

namespace RPGGame.Tests.Unit.UI
{
    public static class LeftPanelSectionVisibilityTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== LeftPanelSectionVisibility Tests ===\n");
            int run = 0, passed = 0, failed = 0;

            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowGear(GameState.Combat, inDungeonRun: true),
                "GEAR hidden in Combat during dungeon run",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowGear(GameState.Dungeon, inDungeonRun: true),
                "GEAR hidden in Dungeon after select",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowGear(GameState.Inventory, inDungeonRun: true),
                "GEAR hidden in Inventory while still in dungeon run",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(GameState.ActionInteractionLab, inDungeonRun: false),
                "GEAR shown in Action Lab",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(GameState.ActionInteractionLab, inDungeonRun: true),
                "GEAR shown in Action Lab even if dungeon flag set",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(GameState.GameLoop, inDungeonRun: false),
                "GEAR shown in GameLoop outside dungeon",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(GameState.Inventory, inDungeonRun: false),
                "GEAR shown in Inventory outside dungeon",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(GameState.DungeonSelection, inDungeonRun: false),
                "GEAR shown on dungeon selection before pick",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowGear(null, inDungeonRun: false),
                "GEAR shown when state unknown outside dungeon",
                ref run, ref passed, ref failed);

            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowStatusEffects(GameState.Combat, inDungeonRun: true),
                "STATUS EFFECTS shown in Combat during dungeon run",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowStatusEffects(GameState.Dungeon, inDungeonRun: true),
                "STATUS EFFECTS shown while exploring dungeon",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowStatusEffects(GameState.Inventory, inDungeonRun: true),
                "STATUS EFFECTS shown in Inventory while still in dungeon run",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(LeftPanelSectionVisibility.ShowStatusEffects(GameState.ActionInteractionLab, inDungeonRun: false),
                "STATUS EFFECTS shown in Action Lab",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowStatusEffects(GameState.GameLoop, inDungeonRun: false),
                "STATUS EFFECTS hidden in GameLoop after leaving dungeon",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowStatusEffects(GameState.Inventory, inDungeonRun: false),
                "STATUS EFFECTS hidden in Inventory outside dungeon",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowStatusEffects(GameState.DungeonSelection, inDungeonRun: false),
                "STATUS EFFECTS hidden on dungeon selection before pick",
                ref run, ref passed, ref failed);
            TestBase.AssertTrue(!LeftPanelSectionVisibility.ShowStatusEffects(null, inDungeonRun: false),
                "STATUS EFFECTS hidden when state unknown outside dungeon",
                ref run, ref passed, ref failed);

            Console.WriteLine($"\nLeftPanelSectionVisibility: {passed}/{run} passed.");
            if (failed > 0)
                throw new Exception($"{failed} LeftPanelSectionVisibility test(s) failed.");
        }
    }
}
