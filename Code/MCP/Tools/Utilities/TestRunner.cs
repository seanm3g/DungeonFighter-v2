using System;
using System.Text;
using System.Threading.Tasks;
using RPGGame.MCP;
using RPGGame.Tests.Unit;

namespace RPGGame.MCP.Tools.Utilities
{
    /// <summary>
    /// Helper for running unit tests with console output capture
    /// </summary>
    public static class TestRunner
    {
        /// <summary>
        /// Runs a test action and captures its console output
        /// </summary>
        public static async Task<string> RunTestWithCapture(System.Action testAction)
        {
            return await Task.Run(() =>
            {
                using var capture = new ConsoleOutputCapture();
                try
                {
                    testAction();
                    return capture.GetOutput();
                }
                catch (Exception ex)
                {
                    return $"Error running test: {ex.Message}\n{ex.StackTrace}";
                }
            });
        }
        
        /// <summary>
        /// Runs ComboDiceRollTests and captures output
        /// </summary>
        public static async Task<string> RunComboDiceRollTests()
        {
            return await RunTestWithCapture(() => ComboDiceRollTests.RunAllTests());
        }
        
        /// <summary>
        /// Runs ActionAndSequenceTests and captures output
        /// </summary>
        public static async Task<string> RunActionSequenceTests()
        {
            return await RunTestWithCapture(() => ActionAndSequenceTests.RunAllTests());
        }
        
        /// <summary>
        /// Runs CombatSystemTests and captures output
        /// </summary>
        public static async Task<string> RunCombatSystemTests()
        {
            return await RunTestWithCapture(() => CombatSystemTests.RunAllTests());
        }
        
        /// <summary>
        /// Runs multiple test actions and combines their output
        /// </summary>
        public static async Task<string> RunMultipleTests(params System.Action[] testActions)
        {
            var output = new StringBuilder();
            
            foreach (var testAction in testActions)
            {
                string result = await RunTestWithCapture(testAction);
                output.AppendLine(result);
                output.AppendLine();
            }
            
            return output.ToString();
        }
    }
}

