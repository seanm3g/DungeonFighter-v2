using System;
using System.Collections.Generic;
using Avalonia.Media;
using RPGGame.Utils;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Unit tests for ObjectPool
    /// </summary>
    public static class ObjectPoolTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== ObjectPool Tests ===\n");
            
            TestBasicPool();
            TestResetAction();
            TestMaxSize();
            TestColoredTextPool();
            
            Console.WriteLine("\n✓ All ObjectPool tests passed!");
        }

        private static void TestBasicPool()
        {
            var pool = new ObjectPool<List<int>>(maxSize: 10);
            
            var list1 = pool.Get();
            list1.Add(1);
            pool.Return(list1);
            
            var list2 = pool.Get();
            TestHarnessBase.AssertTrue(list2.Count == 0, "Returned list should be reset");
            
            pool.Clear();
        }

        private static void TestResetAction()
        {
            var pool = new ObjectPool<List<int>>(
                resetAction: list => list.Clear(),
                maxSize: 10
            );
            
            var list = pool.Get();
            list.Add(1);
            list.Add(2);
            pool.Return(list);
            
            var list2 = pool.Get();
            TestHarnessBase.AssertEqual(0, list2.Count, "Reset action should clear list");
            
            pool.Clear();
        }

        private static void TestMaxSize()
        {
            var pool = new ObjectPool<List<int>>(maxSize: 2);
            
            var list1 = pool.Get();
            var list2 = pool.Get();
            var list3 = pool.Get();
            
            pool.Return(list1);
            pool.Return(list2);
            pool.Return(list3); // Should not be added (max size reached)
            
            TestHarnessBase.AssertEqual(2, pool.Count, "Pool should respect max size");
            
            pool.Clear();
        }

        private static void TestColoredTextPool()
        {
            var segments = ColoredTextPool.Get();
            segments.Add(new ColoredText("Test", Colors.White));
            ColoredTextPool.Return(segments);
            
            var segments2 = ColoredTextPool.Get();
            TestHarnessBase.AssertEqual(0, segments2.Count, "Returned segments should be cleared");
        }
    }
}
