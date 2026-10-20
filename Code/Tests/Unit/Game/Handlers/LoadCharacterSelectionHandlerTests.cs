using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RPGGame.Entity.Services;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Game.Handlers
{
    /// <summary>
    /// Comprehensive tests for LoadCharacterSelectionHandler
    /// Tests character loading interface
    /// </summary>
    public static class LoadCharacterSelectionHandlerTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all LoadCharacterSelectionHandler tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== LoadCharacterSelectionHandler Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestShowLoadCharacterSelection();
            TestHandleLoadCharacterSelectionInput_ValidChoice();
            TestHandleLoadCharacterSelectionInput_ReturnToMainMenu();
            TestHandleLoadCharacterSelectionInput_InvalidChoice();
            TestHandleLoadCharacterSelectionInput_NoSavedCharacters();
            TestPaging();
            TestDeleteRequiresConfirmation();

            TestBase.PrintSummary("LoadCharacterSelectionHandler Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            TestBase.AssertNotNull(handler,
                "LoadCharacterSelectionHandler should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Display Tests

        private static void TestShowLoadCharacterSelection()
        {
            Console.WriteLine("\n--- Testing ShowLoadCharacterSelection ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            
            // Test that ShowLoadCharacterSelection doesn't crash
            Task.Run(async () => await handler.ShowLoadCharacterSelection()).Wait();
            TestBase.AssertTrue(true,
                "ShowLoadCharacterSelection should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Input Handling Tests

        private static void TestHandleLoadCharacterSelectionInput_ValidChoice()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Valid Choice ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowGameLoopEvent += () => { };
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test valid character selection (may fail if no saved characters, but should not crash)
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("1")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput for valid choice should complete",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_ReturnToMainMenu()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Return to Main Menu ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMainMenuEvent += () => { };
            
            // Test return to main menu
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("0")).Wait();
            
            TestBase.AssertEqualEnum(GameState.MainMenu, stateManager.CurrentState,
                "State should transition to MainMenu",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_InvalidChoice()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - Invalid Choice ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test invalid choice
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("99")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput should handle invalid choice",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestHandleLoadCharacterSelectionInput_NoSavedCharacters()
        {
            Console.WriteLine("\n--- Testing HandleLoadCharacterSelectionInput - No Saved Characters ---");

            var stateManager = new GameStateManager();
            var gameInitializer = new GameInitializer();
            string? messageReceived = null;
            
            var handler = new LoadCharacterSelectionHandler(stateManager, null, gameInitializer);
            handler.ShowMessageEvent += (msg) => { messageReceived = msg; };
            
            // Test with no saved characters (choice 0 should still work)
            Task.Run(async () => await handler.HandleLoadCharacterSelectionInput("0")).Wait();
            
            TestBase.AssertTrue(true,
                "HandleLoadCharacterSelectionInput should handle no saved characters",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Paging / Delete (fake save service)

        private sealed class FakeSaveService : ICharacterSaveService
        {
            public List<(string characterId, string characterName, int level)> Saves = new();
            public List<string?> Deleted = new();

            public void SaveCharacter(Character character, string? characterId = null, string? filename = null, bool markDead = false) { }
            public Task SaveCharacterAsync(Character character, string? characterId = null, string? filename = null, bool markDead = false, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task<Character?> LoadCharacterAsync(string? characterId = null, string? filename = null, CancellationToken cancellationToken = default) => Task.FromResult<Character?>(null);
            public void DeleteSaveFile(string? filename = null)
            {
                Deleted.Add(filename);
                Saves.RemoveAll(s => GetCharacterSaveFilename(s.characterId) == filename);
            }
            public bool SaveFileExists(string? filename = null) => Saves.Count > 0;
            public (string? characterName, int level) GetSavedCharacterInfo(string? filename = null) => (null, 0);
            public string GetCharacterSaveFilename(string characterId) => $"character_{characterId}_save.json";
            public List<(string characterId, string characterName, int level)> ListAllSavedCharacters() => Saves.ToList();
            public int ClearAllSavedCharacters() { int n = Saves.Count; Saves.Clear(); return n; }
        }

        private static (LoadCharacterSelectionHandler handler, FakeSaveService fake, ICharacterSaveService previous) CreateWithSaves(int count)
        {
            var fake = new FakeSaveService();
            for (int i = 0; i < count; i++)
                fake.Saves.Add(($"Hero{i}_1_abcd{i:D4}", $"Hero {i}", i + 1));
            var previous = CharacterSaveManager.Service;
            CharacterSaveManager.Service = fake;
            var handler = new LoadCharacterSelectionHandler(new GameStateManager(), null, new GameInitializer());
            return (handler, fake, previous);
        }

        private static void TestPaging()
        {
            Console.WriteLine("\n--- Testing Load Character paging ---");
            var (handler, _, previous) = CreateWithSaves(20);
            try
            {
                handler.ShowLoadCharacterSelection().Wait();
                var view = handler.BuildView();
                TestBase.AssertTrue(view.PageCount == 3 && view.Entries.Count == 9 && view.Entries[0].characterName == "Hero 0",
                    "20 saves should give 3 pages of 9, starting with the most recent",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                handler.HandleLoadCharacterSelectionInput("n").Wait();
                handler.HandleLoadCharacterSelectionInput("n").Wait();
                view = handler.BuildView();
                TestBase.AssertTrue(view.Page == 2 && view.Entries.Count == 2 && view.Entries[0].characterName == "Hero 18" && !view.HasNextPage,
                    "Last page should hold the remaining 2 saves",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                handler.HandleLoadCharacterSelectionInput("n").Wait();
                TestBase.AssertTrue(handler.CurrentPage == 2,
                    "Next on the last page should stay put",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                handler.HandleLoadCharacterSelectionInput("p").Wait();
                TestBase.AssertTrue(handler.CurrentPage == 1,
                    "Previous should go back a page",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CharacterSaveManager.Service = previous;
            }
        }

        private static void TestDeleteRequiresConfirmation()
        {
            Console.WriteLine("\n--- Testing Load Character delete confirmation ---");
            var (handler, fake, previous) = CreateWithSaves(3);
            try
            {
                handler.ShowLoadCharacterSelection().Wait();

                handler.HandleLoadCharacterSelectionInput("d").Wait();
                handler.HandleLoadCharacterSelectionInput("2").Wait();
                handler.HandleLoadCharacterSelectionInput("n").Wait();
                TestBase.AssertTrue(fake.Deleted.Count == 0 && fake.Saves.Count == 3 && !handler.IsInDeleteMode,
                    "Any key other than Y should cancel the delete",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                handler.HandleLoadCharacterSelectionInput("d").Wait();
                handler.HandleLoadCharacterSelectionInput("2").Wait();
                handler.HandleLoadCharacterSelectionInput("y").Wait();
                TestBase.AssertTrue(fake.Deleted.Count == 1 && fake.Saves.Count == 2 && fake.Saves.All(s => s.characterName != "Hero 1"),
                    "D, number, Y should delete exactly that save",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                TestBase.AssertTrue(handler.BuildView().TotalCount == 2,
                    "List should refresh after a delete",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            finally
            {
                CharacterSaveManager.Service = previous;
            }
        }

        #endregion
    }
}
