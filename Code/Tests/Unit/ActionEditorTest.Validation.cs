using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Validation tests for ActionEditor
    /// Tests input validation, duplicate detection, and constraint checking
    /// </summary>
    public static class ActionEditorTest_Validation
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionEditor Validation Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            try
            {
                TestValidationEmptyName();
                TestValidationEmptyType();
                TestValidationEmptyTargetType();
                TestValidationInvalidType();
                TestValidationInvalidTargetType();
                TestValidationNegativeValues();
                
                // These require test actions to exist
                EnsureTestActionsExist();
                TestValidationDuplicateName();
                TestValidationNameChangeConflict();
                
                CleanupTestActions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                CleanupTestActions();
            }
            
            PrintSummary();
        }
        
        private static void CleanupTestActions()
        {
            try
            {
                var editor = new ActionEditor();
                var testActionNames = new[] { "TEST_ACTION_1", "TEST_ACTION_2", "VALID_NAME_1", "VALID_NAME_2", "VALID_NAME_3" };
                
                foreach (var name in testActionNames)
                {
                    var action = editor.GetAction(name);
                    if (action != null)
                    {
                        editor.DeleteAction(name);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        
        private static void EnsureTestActionsExist()
        {
            var editor = new ActionEditor();
            
            if (editor.GetAction("TEST_ACTION_1") == null)
            {
                editor.CreateAction(new ActionData
                {
                    Name = "TEST_ACTION_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    Description = "Test action 1",
                    DamageMultiplier = 1.0,
                    Length = 1.0,
                    Cooldown = 0,
                    Tags = new List<string> { "test" }
                });
            }
            
            if (editor.GetAction("TEST_ACTION_2") == null)
            {
                editor.CreateAction(new ActionData
                {
                    Name = "TEST_ACTION_2",
                    Type = "Heal",
                    TargetType = "Self",
                    Description = "Test action 2",
                    DamageMultiplier = 0.0,
                    Length = 1.0,
                    Cooldown = 2,
                    Tags = new List<string>()
                });
            }
        }
        
        private static ActionEditor CreateTestEditor()
        {
            return new ActionEditor();
        }
        
        private static void TestValidationEmptyName()
        {
            Console.WriteLine("--- Testing Validation: Empty Name ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for empty name");
                TestHarnessBase.AssertTrue(result!.Contains("name cannot be empty", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention empty name");
                
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
        
        private static void TestValidationEmptyType()
        {
            Console.WriteLine("\n--- Testing Validation: Empty Type ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "VALID_NAME",
                    Type = "",
                    TargetType = "SingleTarget",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for empty type");
                TestHarnessBase.AssertTrue(result!.Contains("type cannot be empty", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention empty type");
                
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
        
        private static void TestValidationEmptyTargetType()
        {
            Console.WriteLine("\n--- Testing Validation: Empty Target Type ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "VALID_NAME",
                    Type = "Attack",
                    TargetType = "",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for empty target type");
                TestHarnessBase.AssertTrue(result!.Contains("target type cannot be empty", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention empty target type");
                
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
        
        private static void TestValidationDuplicateName()
        {
            Console.WriteLine("\n--- Testing Validation: Duplicate Name ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                var action = new ActionData
                {
                    Name = "TEST_ACTION_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for duplicate name");
                TestHarnessBase.AssertTrue(result!.Contains("already exists", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention duplicate name");
                
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
        
        private static void TestValidationInvalidType()
        {
            Console.WriteLine("\n--- Testing Validation: Invalid Type ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "VALID_NAME",
                    Type = "InvalidType",
                    TargetType = "SingleTarget",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for invalid type");
                TestHarnessBase.AssertTrue(result!.Contains("Invalid action type", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention invalid type");
                
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
        
        private static void TestValidationInvalidTargetType()
        {
            Console.WriteLine("\n--- Testing Validation: Invalid Target Type ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "VALID_NAME",
                    Type = "Attack",
                    TargetType = "InvalidTargetType",
                };
                
                var result = editor.ValidateAction(action);
                TestHarnessBase.AssertNotNull(result, "Validation should return error for invalid target type");
                TestHarnessBase.AssertTrue(result!.Contains("Invalid target type", StringComparison.OrdinalIgnoreCase), 
                    "Error message should mention invalid target type");
                
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
        
        private static void TestValidationNegativeValues()
        {
            Console.WriteLine("\n--- Testing Validation: Negative Values ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                var action1 = new ActionData
                {
                    Name = "VALID_NAME_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    DamageMultiplier = -1.0
                };
                
                var result1 = editor.ValidateAction(action1);
                TestHarnessBase.AssertNotNull(result1, "Validation should return error for negative damage multiplier");
                if (result1 != null)
                {
                    TestHarnessBase.AssertTrue(result1.Contains("negative", StringComparison.OrdinalIgnoreCase), 
                        "Error message should mention negative value");
                }
                
                var action2 = new ActionData
                {
                    Name = "VALID_NAME_2",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    Length = -1.0
                };
                
                var result2 = editor.ValidateAction(action2);
                TestHarnessBase.AssertNotNull(result2, "Validation should return error for negative length");
                
                var action3 = new ActionData
                {
                    Name = "VALID_NAME_3",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    Cooldown = -1
                };
                
                var result3 = editor.ValidateAction(action3);
                TestHarnessBase.AssertNotNull(result3, "Validation should return error for negative cooldown");
                
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
        
        private static void TestValidationNameChangeConflict()
        {
            Console.WriteLine("\n--- Testing Validation: Name Change Conflict ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                var updatedAction = new ActionData
                {
                    Name = "TEST_ACTION_2",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                };
                
                var result = editor.ValidateAction(updatedAction, "TEST_ACTION_1");
                TestHarnessBase.AssertNotNull(result, "Validation should return error for name change conflict");
                if (result != null)
                {
                    TestHarnessBase.AssertTrue(result.Contains("already exists", StringComparison.OrdinalIgnoreCase), 
                        "Error message should mention name already exists");
                }
                
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
            Console.WriteLine("\n=== Validation Test Summary ===");
            Console.WriteLine($"Tests Run: {_testsRun}");
            Console.WriteLine($"Tests Passed: {_testsPassed}");
            Console.WriteLine($"Tests Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100.0 / _testsRun) : 0):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All validation tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed.");
            }
        }
    }
}

