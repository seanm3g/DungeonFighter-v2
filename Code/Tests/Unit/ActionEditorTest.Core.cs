using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Editors;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Core functionality tests for ActionEditor
    /// Tests create, update, delete, get operations
    /// </summary>
    public static class ActionEditorTest_Core
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionEditor Core Functionality Tests ===\n");
            Console.WriteLine("NOTE: These tests work with the actual Actions.json file.");
            Console.WriteLine("Test actions use 'TEST_' prefix and are cleaned up after tests.\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            try
            {
                TestCreateAction();
                TestUpdateAction();
                TestGetAction();
                TestGetActions();
                TestActionPersistence();
                TestDeleteAction();
                
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
                var testActionNames = new[] { "TEST_ACTION_1", "TEST_ACTION_2", "NEW_TEST_ACTION", "PERSISTENCE_TEST" };
                
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
        
        private static void TestCreateAction()
        {
            Console.WriteLine("--- Testing Create Action ---");
            
            try
            {
                var editor = CreateTestEditor();
                
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
                
                var existing = editor.GetAction("TEST_ACTION_1");
                if (existing == null)
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
                
                var updatedAction = new ActionData
                {
                    Name = "TEST_ACTION_1",
                    Type = "Attack",
                    TargetType = "SingleTarget",
                    Description = "Updated test action 1",
                    DamageMultiplier = 2.0,
                    Length = 1.5,
                    Cooldown = 1,
                    Tags = new List<string> { "test", "updated" }
                };
                
                bool result = editor.UpdateAction("TEST_ACTION_1", updatedAction);
                TestHarnessBase.AssertTrue(result, "UpdateAction should return true for existing action");
                
                var retrieved = editor.GetAction("TEST_ACTION_1");
                TestHarnessBase.AssertNotNull(retrieved, "Updated action should be retrievable");
                if (retrieved != null)
                {
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
                
                var existing = editor.GetAction("TEST_ACTION_2");
                if (existing == null)
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
                
                var beforeDelete = editor.GetAction("TEST_ACTION_2");
                TestHarnessBase.AssertNotNull(beforeDelete, "Action should exist before deletion");
                
                bool result = editor.DeleteAction("TEST_ACTION_2");
                TestHarnessBase.AssertTrue(result, "DeleteAction should return true for existing action");
                
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
                
                var existing = editor.GetAction("TEST_ACTION_1");
                if (existing == null)
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
                
                var action = editor.GetAction("TEST_ACTION_1");
                TestHarnessBase.AssertNotNull(action, "GetAction should return action for existing name");
                if (action != null)
                {
                    TestHarnessBase.AssertEqual("TEST_ACTION_1", action.Name, "Action name should match");
                }
                
                var nonExistent = editor.GetAction("NON_EXISTENT_ACTION_XYZ");
                TestHarnessBase.AssertNull(nonExistent, "GetAction should return null for non-existent action");
                
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
        
        private static void TestActionPersistence()
        {
            Console.WriteLine("\n--- Testing Action Persistence ---");
            
            try
            {
                var editor1 = CreateTestEditor();
                
                var newAction = new ActionData
                {
                    Name = "PERSISTENCE_TEST",
                    Type = "Buff",
                    TargetType = "Self",
                    Description = "Persistence test action",
                    DamageMultiplier = 1.0,
                    Length = 2.0,
                    Cooldown = 3,
                    Tags = new List<string> { "test", "persistence" }
                };
                
                bool createResult = editor1.CreateAction(newAction);
                TestHarnessBase.AssertTrue(createResult, "Action should be created successfully");
                
                var editor2 = CreateTestEditor();
                
                var retrieved = editor2.GetAction("PERSISTENCE_TEST");
                TestHarnessBase.AssertNotNull(retrieved, "Action should persist after editor reload");
                if (retrieved != null)
                {
                    TestHarnessBase.AssertEqual("PERSISTENCE_TEST", retrieved.Name, "Persisted action name should match");
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
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Core Functionality Test Summary ===");
            Console.WriteLine($"Tests Run: {_testsRun}");
            Console.WriteLine($"Tests Passed: {_testsPassed}");
            Console.WriteLine($"Tests Failed: {_testsFailed}");
            Console.WriteLine($"Success Rate: {(_testsRun > 0 ? (_testsPassed * 100.0 / _testsRun) : 0):F1}%");
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All core tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed.");
            }
        }
    }
}

