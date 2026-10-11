using System;
using System.Collections.Generic;
using RPGGame.Tests;
using RPGGame.UI.Avalonia.Effects;

namespace RPGGame.Tests.Unit.UI
{
    public static class ActionTextEmphasisTriggerTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== ActionTextEmphasisTrigger Tests ===\n");
            _run = _passed = _failed = 0;

            TestQueuesAfterForceRenderAndRetriesUntilFound();
            TestStopsWhenFoundOnFirstBackgroundTick();
            TestIgnoresBlankWord();

            TestBase.PrintSummary("ActionTextEmphasisTrigger Tests", _run, _passed, _failed);
        }

        private static void TestQueuesAfterForceRenderAndRetriesUntilFound()
        {
            Console.WriteLine("--- ForceRender then retry until locate succeeds ---");
            int forceCount = 0;
            int tryCount = 0;
            var queue = new Queue<System.Action>();

            ActionTextEmphasisTrigger.QueueAfterPaint(
                forceRender: () => forceCount++,
                tryTrigger: _ =>
                {
                    tryCount++;
                    return tryCount >= 3;
                },
                word: "SLAM",
                postBackground: a => queue.Enqueue(a),
                maxAttempts: 4);

            TestBase.AssertEqual(1, forceCount, "forceRender once up front", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(1, queue.Count, "one Background attempt queued", ref _run, ref _passed, ref _failed);

            while (queue.Count > 0)
                queue.Dequeue().Invoke();

            TestBase.AssertEqual(3, tryCount, "retried until third success", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, queue.Count, "queue drained", ref _run, ref _passed, ref _failed);
        }

        private static void TestStopsWhenFoundOnFirstBackgroundTick()
        {
            Console.WriteLine("\n--- Stops when first Background locate hits ---");
            int tryCount = 0;
            var queue = new Queue<System.Action>();

            ActionTextEmphasisTrigger.QueueAfterPaint(
                forceRender: () => { },
                tryTrigger: _ =>
                {
                    tryCount++;
                    return true;
                },
                word: "SLAM",
                postBackground: a => queue.Enqueue(a),
                maxAttempts: 4);

            while (queue.Count > 0)
                queue.Dequeue().Invoke();

            TestBase.AssertEqual(1, tryCount, "single successful try", ref _run, ref _passed, ref _failed);
        }

        private static void TestIgnoresBlankWord()
        {
            Console.WriteLine("\n--- Blank word is a no-op ---");
            int forceCount = 0;
            var queue = new Queue<System.Action>();

            ActionTextEmphasisTrigger.QueueAfterPaint(
                forceRender: () => forceCount++,
                tryTrigger: _ => true,
                word: "  ",
                postBackground: a => queue.Enqueue(a));

            TestBase.AssertEqual(0, forceCount, "no forceRender", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0, queue.Count, "no queue work", ref _run, ref _passed, ref _failed);
        }
    }
}
