using RPGGame;

namespace RPGGame.MCP.Tools
{
    /// <summary>
    /// Manages shared state for MCP tools
    /// </summary>
    public static class McpToolState
    {
        private static GameWrapper? _gameWrapper;
        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? _lastTestResult;
        private static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? _baselineTestResult;
        private static RPGGame.Editors.VariableEditor? _variableEditor;

        public static GameWrapper? GameWrapper
        {
            get => _gameWrapper;
            set => _gameWrapper = value;
        }

        public static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? LastTestResult
        {
            get => _lastTestResult;
            set => _lastTestResult = value;
        }

        public static BattleStatisticsRunner.ComprehensiveWeaponEnemyTestResult? BaselineTestResult
        {
            get => _baselineTestResult;
            set => _baselineTestResult = value;
        }

        public static RPGGame.Editors.VariableEditor GetVariableEditor()
        {
            if (_variableEditor == null)
            {
                _variableEditor = new RPGGame.Editors.VariableEditor();
            }
            return _variableEditor;
        }
    }
}
