using RPGGame;
using RPGGame.UI.Avalonia.Renderers.Helpers;
using RPGGame.UI.Avalonia.Renderers.Layout;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Coordinates rendering of all statistics-related screens.
    /// Extracted from CanvasRenderer to reduce size and improve organization.
    /// </summary>
    public class StatisticsRendererCoordinator
    {
        private readonly MenuScreenRenderingHelper menuScreenHelper;
        private readonly MenuRenderer menuRenderer;
        
        public StatisticsRendererCoordinator(MenuScreenRenderingHelper menuScreenHelper, MenuRenderer menuRenderer)
        {
            this.menuScreenHelper = menuScreenHelper;
            this.menuRenderer = menuRenderer;
        }
        
        public void RenderBattleStatisticsMenu(BattleStatisticsRunner.StatisticsResult? results, bool isRunning)
        {
            menuScreenHelper.RenderMenuScreen("BATTLE STATISTICS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsMenuContent(x, y, w, h, results, isRunning));
        }
        
        public void RenderBattleStatisticsResults(BattleStatisticsRunner.StatisticsResult results)
        {
            menuScreenHelper.RenderMenuScreen("BATTLE STATISTICS RESULTS", 
                (x, y, w, h) => menuRenderer.RenderBattleStatisticsResultsContent(x, y, w, h, results));
        }
        
        public void RenderWeaponTestResults(List<BattleStatisticsRunner.WeaponTestResult> results)
        {
            menuScreenHelper.RenderMenuScreen("WEAPON TYPE TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderWeaponTestResultsContent(x, y, w, h, results));
        }
        
        public void RenderComprehensiveWeaponEnemyResults(BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult results)
        {
            menuScreenHelper.RenderMenuScreen("COMPREHENSIVE WEAPON-ENEMY TEST RESULTS", 
                (x, y, w, h) => menuRenderer.RenderComprehensiveWeaponEnemyResultsContent(x, y, w, h, results));
        }
    }
}
