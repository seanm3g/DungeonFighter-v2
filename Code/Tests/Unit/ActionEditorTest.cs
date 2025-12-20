using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for ActionEditor and ActionEditorHandler functionality
    /// Tests create, edit, delete, validation, and error handling
    /// </summary>
    public static class ActionEditorTest
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        /// <summary>
        /// Runs all ActionEditor tests
        /// NOTE: These tests work with the actual Actions.json file.
        /// They create test actions with "TEST_" prefix and clean them up afterward.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionEditor Tests ===\n");
            Console.WriteLine("NOTE: These tests work with the actual Actions.json file.");
            Console.WriteLine("Test actions use 'TEST_' prefix and are cleaned up after tests.\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            try
            {
                // Validation tests (don't require file I/O)
                TestValidationEmptyName();
                TestValidationEmptyType();
                TestValidationEmptyTargetType();
                TestValidationInvalidType();
                TestValidationInvalidTargetType();
                TestValidationNegativeValues();
                
                // Core functionality tests (require file I/O)
                // These use test actions with "TEST_" prefix
                TestCreateAction();
                TestUpdateAction();
                TestGetAction();
                TestGetActions();
                TestValidationDuplicateName(); // After creating test action
                TestValidationNameChangeConflict(); // After creating test actions
                TestActionPersistence();
                TestDeleteAction(); // Delete at the end
                
                // Edge cases
                TestUpdateNonExistentAction();
                TestDeleteNonExistentAction();
                TestGetNonExistentAction();
                
                // Cleanup: Remove any remaining test actions
                CleanupTestActions();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nFATAL ERROR: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Always try to clean up
                CleanupTestActions();
            }
            
            // Print summary
            PrintSummary();
        }
        
        #region Setup and Cleanup
        
        private static void CleanupTestActions()
        {
            try
            {
                var editor = new ActionEditor();
                var testActionNames = new[] { "TEST_ACTION_1", "TEST_ACTION_2", "NEW_TEST_ACTION", "PERSISTENCE_TEST", 
                    "VALID_NAME_1", "VALID_NAME_2", "VALID_NAME_3" };
                
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
        
        private static ActionEditor CreateTestEditor()
        {
            return new ActionEditor();
        }
        
        #endregion
        
        #region Core Functionality Tests
        
        private static void TestCreateAction()
        {
            Console.WriteLine("--- Testing Create Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                // Clean up if it already exists
                var existing = editor.GetAction("NEW_TEST_ACTION");
                if (existing != null)
                {
                    editor.DeleteAction("NEW_TEST_ACTION");
                }
                
                var newAction = new ActionData
                {
                    Name = "NEW_TEST_ACTION",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 15,
                    Description = "New test action",
                    DamageMultiplier = 1.5,
                    Length = 1.0,
                    Cooldown = 1,
                    Tags = new List<string> { "new", "test" }
                };
                
                bool result = editor.CreateAction(newAction);
                TestHarnessBase.AssertTrue(result, "CreateAction should return true for valid action");
                
                var retrieved = editor.GetAction("NEW_TEST_ACTION");
                TestHarnessBase.AssertNotNull(retrieved, "Created action should be retrievable");
                if (retrieved != null)
                {
                    TestHarnessBase.AssertEqual("NEW_TEST_ACTION", retrieved.Name, "Action name should match");
                    TestHarnessBase.AssertEqual(15, retrieved.BaseValue, "Base value should match");
                    TestHarnessBase.AssertEqual(1.5, retrieved.DamageMultiplier, "Damage multiplier should match");
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
        
        private static void TestUpdateAction()
        {
            Console.WriteLine("\n--- Testing Update Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                // Ensure test action exists
                var existing = editor.GetAction("TEST_ACTION_1");
                if (existing == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_1",
                        Type = "Attack",
                        TargetType = "SingleTarget",
                        BaseValue = 10,
                        Description = "Test action 1",
                        DamageMultiplier = 1.0,
                        Length = 1.0,
                        Cooldown = 0,
                        Tags = new List<string> { "test" }
                    });
                }
                
                var updatedAction = new ActionData
                {
                    Name = "TEST_ACTION_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 20, // Changed from 10
                    Description = "Updated test action 1", // Changed
                    DamageMultiplier = 2.0, // Changed from 1.0
                    Length = 1.5, // Changed from 1.0
                    Cooldown = 1, // Changed from 0
                    Tags = new List<string> { "test", "updated" }
                };
                
                bool result = editor.UpdateAction("TEST_ACTION_1", updatedAction);
                TestHarnessBase.AssertTrue(result, "UpdateAction should return true for existing action");
                
                var retrieved = editor.GetAction("TEST_ACTION_1");
                TestHarnessBase.AssertNotNull(retrieved, "Updated action should be retrievable");
                if (retrieved != null)
                {
                    TestHarnessBase.AssertEqual(20, retrieved.BaseValue, "Base value should be updated");
                    TestHarnessBase.AssertEqual("Updated test action 1", retrieved.Description, "Description should be updated");
                    TestHarnessBase.AssertEqual(2.0, retrieved.DamageMultiplier, "Damage multiplier should be updated");
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
        
        private static void TestDeleteAction()
        {
            Console.WriteLine("\n--- Testing Delete Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                // Ensure test action exists
                var existing = editor.GetAction("TEST_ACTION_2");
                if (existing == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_2",
                        Type = "Heal",
                        TargetType = "Self",
                        BaseValue = 5,
                        Description = "Test action 2",
                        DamageMultiplier = 0.0,
                        Length = 1.0,
                        Cooldown = 2,
                        Tags = new List<string>()
                    });
                }
                
                // Verify action exists before deletion
                var beforeDelete = editor.GetAction("TEST_ACTION_2");
                TestHarnessBase.AssertNotNull(beforeDelete, "Action should exist before deletion");
                
                bool result = editor.DeleteAction("TEST_ACTION_2");
                TestHarnessBase.AssertTrue(result, "DeleteAction should return true for existing action");
                
                // Verify action no longer exists
                var afterDelete = editor.GetAction("TEST_ACTION_2");
                TestHarnessBase.AssertNull(afterDelete, "Action should not exist after deletion");
                
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
        
        private static void TestGetAction()
        {
            Console.WriteLine("\n--- Testing Get Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                // Ensure test action exists
                var existing = editor.GetAction("TEST_ACTION_1");
                if (existing == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_1",
                        Type = "Attack",
                        TargetType = "SingleTarget",
                        BaseValue = 10,
                        Description = "Test action 1",
                        DamageMultiplier = 1.0,
                        Length = 1.0,
                        Cooldown = 0,
                        Tags = new List<string> { "test" }
                    });
                }
                
                var action = editor.GetAction("TEST_ACTION_1");
                TestHarnessBase.AssertNotNull(action, "GetAction should return action for existing name");
                if (action != null)
                {
                    TestHarnessBase.AssertEqual("TEST_ACTION_1", action.Name, "Action name should match");
                }
                
                var nonExistent = editor.GetAction("NON_EXISTENT_ACTION_XYZ");
                TestHarnessBase.AssertNull(nonExistent, "GetAction should return null for non-existent action");
                
                // Test case-insensitive
                var caseInsensitive = editor.GetAction("test_action_1");
                TestHarnessBase.AssertNotNull(caseInsensitive, "GetAction should be case-insensitive");
                
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
        
        private static void TestGetActions()
        {
            Console.WriteLine("\n--- Testing Get Actions ---");
            
            try
            {
                var editor = CreateTestEditor();
                
                var actions = editor.GetActions();
                TestHarnessBase.AssertNotNull(actions, "GetActions should return a list");
                TestHarnessBase.AssertTrue(actions.Count >= 1, "GetActions should return at least one action");
                
                // Verify we can find our test actions
                bool foundTest1 = actions.Any(a => a.Name.Equals("TEST_ACTION_1", StringComparison.OrdinalIgnoreCase));
                TestHarnessBase.AssertTrue(foundTest1, "GetActions should include TEST_ACTION_1");
                
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
        
        #endregion
        
        #region Validation Tests
        
        private static void TestValidationEmptyName()
        {
            Console.WriteLine("\n--- Testing Validation: Empty Name ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10
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
                    BaseValue = 10
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
                    BaseValue = 10
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
                
                // Ensure test action exists
                var existing = editor.GetAction("TEST_ACTION_1");
                if (existing == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_1",
                        Type = "Attack",
                        TargetType = "SingleTarget",
                        BaseValue = 10,
                        Description = "Test action 1",
                        DamageMultiplier = 1.0,
                        Length = 1.0,
                        Cooldown = 0,
                        Tags = new List<string> { "test" }
                    });
                }
                
                var action = new ActionData
                {
                    Name = "TEST_ACTION_1", // Already exists
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10
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
                    BaseValue = 10
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
                    BaseValue = 10
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
                
                // Test negative damage multiplier
                var action1 = new ActionData
                {
                    Name = "VALID_NAME_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10,
                    DamageMultiplier = -1.0
                };
                
                var result1 = editor.ValidateAction(action1);
                TestHarnessBase.AssertNotNull(result1, "Validation should return error for negative damage multiplier");
                if (result1 != null)
                {
                    TestHarnessBase.AssertTrue(result1.Contains("negative", StringComparison.OrdinalIgnoreCase), 
                        "Error message should mention negative value");
                }
                
                // Test negative length
                var action2 = new ActionData
                {
                    Name = "VALID_NAME_2",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10,
                    Length = -1.0
                };
                
                var result2 = editor.ValidateAction(action2);
                TestHarnessBase.AssertNotNull(result2, "Validation should return error for negative length");
                
                // Test negative cooldown
                var action3 = new ActionData
                {
                    Name = "VALID_NAME_3",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10,
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
                
                // Ensure both test actions exist
                var existing1 = editor.GetAction("TEST_ACTION_1");
                if (existing1 == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_1",
                        Type = "Attack",
                        TargetType = "SingleTarget",
                        BaseValue = 10,
                        Description = "Test action 1",
                        DamageMultiplier = 1.0,
                        Length = 1.0,
                        Cooldown = 0,
                        Tags = new List<string> { "test" }
                    });
                }
                
                var existing2 = editor.GetAction("TEST_ACTION_2");
                if (existing2 == null)
                {
                    editor.CreateAction(new ActionData
                    {
                        Name = "TEST_ACTION_2",
                        Type = "Heal",
                        TargetType = "Self",
                        BaseValue = 5,
                        Description = "Test action 2",
                        DamageMultiplier = 0.0,
                        Length = 1.0,
                        Cooldown = 2,
                        Tags = new List<string>()
                    });
                }
                
                // Try to update TEST_ACTION_1 to have name TEST_ACTION_2 (which exists)
                var updatedAction = new ActionData
                {
                    Name = "TEST_ACTION_2", // Trying to rename to existing name
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10
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
        
        #endregion
        
        #region Edge Cases
        
        private static void TestUpdateNonExistentAction()
        {
            Console.WriteLine("\n--- Testing Update Non-Existent Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                var action = new ActionData
                {
                    Name = "NON_EXISTENT",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    BaseValue = 10
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
        
        private static void TestActionPersistence()
        {
            Console.WriteLine("\n--- Testing Action Persistence ---");
            
            try
            {
                var editor1 = CreateTestEditor();
                
                // Create a new action
                var newAction = new ActionData
                {
                    Name = "PERSISTENCE_TEST",
                    Type = "Buff",
                    TargetType = "Self",
                    BaseValue = 25,
                    Description = "Persistence test action",
                    DamageMultiplier = 1.0,
                    Length = 2.0,
                    Cooldown = 3,
                    Tags = new List<string> { "test", "persistence" }
                };
                
                bool createResult = editor1.CreateAction(newAction);
                TestHarnessBase.AssertTrue(createResult, "Action should be created successfully");
                
                // Create a new editor instance (simulating reload)
                var editor2 = CreateTestEditor();
                
                // Verify action persists
                var retrieved = editor2.GetAction("PERSISTENCE_TEST");
                TestHarnessBase.AssertNotNull(retrieved, "Action should persist after editor reload");
                if (retrieved != null)
                {
                    TestHarnessBase.AssertEqual("PERSISTENCE_TEST", retrieved.Name, "Persisted action name should match");
                    TestHarnessBase.AssertEqual(25, retrieved.BaseValue, "Persisted action base value should match");
                    TestHarnessBase.AssertEqual("Persistence test action", retrieved.Description, "Persisted action description should match");
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
        
        #endregion
        
        #region Summary
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Test Summary ===");
            Console.WriteLine($"Tests Run: {_testsRun}");
            Console.WriteLine($"Tests Passed: {_testsPassed}");
            Console.WriteLine($"Tests Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100.0 / _testsRun) : 0):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed.");
            }
        }
        
        #endregion
    }
}

