using RPGGame;
using RPGGame.MCP.Models;
using RPGGame.Tuning;

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
        private static MultiLevelSimulationResult? _lastMultiLevelResult;
        private static RPGGame.Editors.VariableEditor? _variableEditor;
        private static string? _agentDirective;
        private static DungeonRunSummary? _lastRunSummary;

        public static GameWrapper? GameWrapper
        {
            get => _gameWrapper;
            set => _gameWrapper = value;
        }

        /// <summary>Session-scoped strategy from set_agent_directive, echoed in get_agent_context.</summary>
        public static string? AgentDirective
        {
            get => _agentDirective;
            set => _agentDirective = value;
        }

        /// <summary>Summary of the most recent atomic dungeon run (completion or death).</summary>
        public static DungeonRunSummary? LastRunSummary
        {
            get => _lastRunSummary;
            set => _lastRunSummary = value;
        }

        public static void ClearGameplaySessionState()
        {
            _agentDirective = null;
            _lastRunSummary = null;
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

        public static MultiLevelSimulationResult? LastMultiLevelResult
        {
            get => _lastMultiLevelResult;
            set => _lastMultiLevelResult = value;
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
