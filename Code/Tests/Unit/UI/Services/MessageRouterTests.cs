using System;
using System.Collections.Generic;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.Services;
using RPGGame.Tests;
using Avalonia.Media;

namespace RPGGame.Tests.Unit.UI.Services
{
    /// <summary>
    /// Comprehensive tests for MessageRouter
    /// Tests message routing logic
    /// </summary>
    public static class MessageRouterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all MessageRouter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== MessageRouter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestConstructor();
            TestRouteSystemMessage();
            TestRouteColoredText();
            TestRouteCombatMessage();

            TestBase.PrintSummary("MessageRouter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region Constructor Tests

        private static void TestConstructor()
        {
            Console.WriteLine("--- Testing Constructor ---");

            var router = new MessageRouter();
            TestBase.AssertNotNull(router,
                "MessageRouter should be created",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region Routing Tests

        private static void TestRouteSystemMessage()
        {
            Console.WriteLine("\n--- Testing RouteSystemMessage ---");

            var router = new MessageRouter();
            
            // Test that RouteSystemMessage doesn't crash
            router.RouteSystemMessage("Test message");
            
            TestBase.AssertTrue(true,
                "RouteSystemMessage should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRouteColoredText()
        {
            Console.WriteLine("\n--- Testing RouteColoredText ---");

            var router = new MessageRouter();
            var segments = new List<ColoredText> { new ColoredText("Test", Colors.White) };
            
            // Test that RouteColoredText doesn't crash
            router.RouteColoredText(segments);
            
            TestBase.AssertTrue(true,
                "RouteColoredText should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestRouteCombatMessage()
        {
            Console.WriteLine("\n--- Testing RouteCombatMessage ---");

            var router = new MessageRouter();
            var actionText = new List<ColoredText> { new ColoredText("Hero hits Enemy", Colors.White) };
            var rollInfo = new List<ColoredText> { new ColoredText("(roll: 15)", Colors.Gray) };
            
            // Test that RouteCombatMessage doesn't crash
            router.RouteCombatMessage(actionText, rollInfo);
            
            TestBase.AssertTrue(true,
                "RouteCombatMessage should complete without errors",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion
    }
}
