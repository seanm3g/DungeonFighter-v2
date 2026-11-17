using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using RPGGame;
using RPGGame.UI.Avalonia.Helpers;
using RPGGame.UI.Avalonia.Managers.Settings;
using System;
using System.Threading.Tasks;
using ActionDelegate = System.Action;

namespace RPGGame.UI.Avalonia.Managers
{
    /// <summary>
    /// Unified event wiring system for SettingsPanel controls.
    /// Consolidates SettingsEventWiring and SettingsEventManager for better maintainability.
    /// </summary>
    public class SettingsEventWiring
    {
        private readonly Func<int, Task>? runBattleTest;
        private readonly Func<Task>? runWeaponTypeTests;
        private readonly Func<Task>? runComprehensiveWeaponEnemyTests;
        private readonly ActionDelegate? onSave;
        private readonly ActionDelegate? onReset;
        private readonly ActionDelegate? onBack;

        public SettingsEventWiring(
            Func<int, Task>? runBattleTest,
            Func<Task>? runWeaponTypeTests,
            Func<Task>? runComprehensiveWeaponEnemyTests,
            ActionDelegate? onSave,
            ActionDelegate? onReset,
            ActionDelegate? onBack)
        {
            this.runBattleTest = runBattleTest;
            this.runWeaponTypeTests = runWeaponTypeTests;
            this.runComprehensiveWeaponEnemyTests = runComprehensiveWeaponEnemyTests;
            this.onSave = onSave;
            this.onReset = onReset;
            this.onBack = onBack;
        }

    }
}

