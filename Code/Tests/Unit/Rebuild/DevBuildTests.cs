using System;
using System.Linq;
using RPGGame.UI.Avalonia.Managers.Settings;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 0.2: the 11 developer states, the dev settings panels and the clone are dev-build only.</summary>
    public static class DevBuildTests
    {
        private static int _run, _passed, _failed;

        private static readonly GameState[] DevStates =
        {
            GameState.Testing, GameState.DeveloperMenu, GameState.BattleStatistics, GameState.VariableEditor,
            GameState.TuningParameters, GameState.ActionEditor, GameState.CreateAction, GameState.EditAction,
            GameState.ViewAction, GameState.DeleteActionConfirmation, GameState.ActionInteractionLab
        };

        public static void RunAllTests()
        {
            Console.WriteLine("=== DevBuild Tests ===\n");
            _run = _passed = _failed = 0;
            try
            {
                Exactly_Eleven_Dev_States();
                Player_Build_Refuses_Dev_States();
                Dev_Build_Allows_Dev_States();
                Player_Build_Hides_Dev_Settings_Panels();
                Clone_Is_Dev_Only();
            }
            finally
            {
                DevBuild.SetOverride(null);
            }
            TestBase.PrintSummary("DevBuild Tests", _run, _passed, _failed);
        }

        private static void Exactly_Eleven_Dev_States()
        {
            int count = Enum.GetValues<GameState>().Count(DevBuild.IsDevOnlyState);
            TestBase.AssertEqual(11, count, "Exactly the 11 developer states are dev-only", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(DevStates.All(DevBuild.IsDevOnlyState), "Each listed developer state is dev-only", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(DevBuild.IsDevOnlyState(GameState.Settings), "Settings is a player state", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(DevBuild.IsDevOnlyState(GameState.Stable), "The Stable is a player state (D1.2)", ref _run, ref _passed, ref _failed);
        }

        private static void Player_Build_Refuses_Dev_States()
        {
            DevBuild.SetOverride(false);
            var manager = new GameStateManager();
            foreach (var state in DevStates)
            {
                bool ok = manager.TransitionToState(state);
                TestBase.AssertFalse(ok, $"Player build refuses {state}", ref _run, ref _passed, ref _failed);
            }
            TestBase.AssertTrue(manager.TransitionToState(GameState.Settings), "Player build still opens Settings", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqualEnum(GameState.Settings, manager.CurrentState, "State is Settings", ref _run, ref _passed, ref _failed);
        }

        private static void Dev_Build_Allows_Dev_States()
        {
            DevBuild.SetOverride(true);
            var manager = new GameStateManager();
            TestBase.AssertTrue(manager.TransitionToState(GameState.DeveloperMenu), "Dev build opens the developer menu", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(manager.TransitionToState(GameState.ActionInteractionLab), "Dev build opens the action lab", ref _run, ref _passed, ref _failed);
        }

        private static void Player_Build_Hides_Dev_Settings_Panels()
        {
            DevBuild.SetOverride(false);
            var groups = SettingsPanelCatalog.GetPanelsForSidebar().Select(p => p.SidebarGroup).Distinct().ToList();
            TestBase.AssertFalse(groups.Contains(SettingsSidebarGroups.Developer), "Player build hides Developer panels", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(groups.Contains(SettingsSidebarGroups.Testing), "Player build hides the Testing panel", ref _run, ref _passed, ref _failed);
            TestBase.AssertFalse(groups.Contains(SettingsSidebarGroups.Balance), "Player build hides Balance panels", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(groups.Contains(SettingsSidebarGroups.Player), "Player build keeps Player settings", ref _run, ref _passed, ref _failed);

            DevBuild.SetOverride(true);
            var devGroups = SettingsPanelCatalog.GetPanelsForSidebar().Select(p => p.SidebarGroup).Distinct().ToList();
            TestBase.AssertTrue(devGroups.Contains(SettingsSidebarGroups.Testing), "Dev build shows the Testing panel", ref _run, ref _passed, ref _failed);
        }

        private static void Clone_Is_Dev_Only()
        {
            DevBuild.SetOverride(false);
            TestBase.AssertFalse(DevBuild.AllowsClone, "Player build has no clone (D7.9)", ref _run, ref _passed, ref _failed);
            DevBuild.SetOverride(true);
            TestBase.AssertTrue(DevBuild.AllowsClone, "Dev build keeps the clone", ref _run, ref _passed, ref _failed);
        }
    }
}
