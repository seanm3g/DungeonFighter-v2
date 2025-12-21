using System;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Edge case tests for ActionEditor
    /// Tests error handling for non-existent actions and boundary conditions
    /// </summary>
    public static class ActionEditorTest_EdgeCases
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionEditor Edge Case Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            try
            {
                TestUpdateNonExistentAction();
                TestDeleteNonExistentAction();
                TestGetNonExistentAction();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            PrintSummary();
        }
        
        private static ActionEditor CreateTestEditor()
        {
            return new ActionEditor();
        }
        
        private static void TestUpdateNonExistentAction()
        {
            Console.WriteLine("--- Testing Update Non-Existent Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "NON_EXISTENT",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                };
                
                bool result = editor.UpdateAction("NON_EXISTENT", action);
                TestHarnessBase.AssertFalse(result, "UpdateAction should return false for non-existent action");
                
                _testsPassed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
                _testsFailed++;
            }
            finally
            {
                _testsRun++;
            }
        }
        
        private static void TestDeleteNonExistentAction()
        {
            Console.WriteLine("\n--- Testing Delete Non-Existent Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                bool result = editor.DeleteAction("NON_EXISTENT_ACTION");
                TestHarnessBase.AssertFalse(result, "DeleteAction should return false for non-existent action");
                
                _testsPassed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
                _testsFailed++;
            }
            finally
            {
                _testsRun++;
            }
        }
        
        private static void TestGetNonExistentAction()
        {
            Console.WriteLine("\n--- Testing Get Non-Existent Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                var action = editor.GetAction("NON_EXISTENT_ACTION");
                TestHarnessBase.AssertNull(action, "GetAction should return null for non-existent action");
                
                _testsPassed++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  FAILED: {ex.Message}");
                _testsFailed++;
            }
            finally
            {
                _testsRun++;
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Edge Case Test Summary ===");
            Console.WriteLine($"Tests Run: {_testsRun}");
            Console.WriteLine($"Tests Passed: {_testsPassed}");
            Console.WriteLine($"Tests Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100.0 / _testsRun) : 0):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All edge case tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed.");
            }
        }
    }
}

