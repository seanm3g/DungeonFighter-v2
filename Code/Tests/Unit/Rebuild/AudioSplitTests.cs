using System;
using System.IO;
using RPGGame.Actions.Execution;
using RPGGame.Audio;

namespace RPGGame.Tests.Unit.Rebuild
{
    /// <summary>Rebuild 1.2 (D9.6): the enemy's chain is thuds (you being hit), quieter and shorter in escalation.</summary>
    public static class AudioSplitTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== AudioSplit Tests ===\n");
            _run = _passed = _failed = 0;

            var enemy = TestDataBuilders.Enemy().WithName("Chainer").Build();
            enemy.ActionLabActionSlotBonus = 6;
            for (int i = 0; i < 5; i++)
                enemy.AddToCombo(new Action($"LINK{i}", comboOrder: i, damageMultiplier: 1.0, length: 1.0, isComboAction: true));
            int n = enemy.GetComboActions().Count;
            TestBase.AssertTrue(n >= 4, $"Enemy strip built ({n} slots)", ref _run, ref _passed, ref _failed);

            AudioCue Link(int step) { enemy.ComboStep = step; return ActionEventPublisher.ResolveEnemyChainCue(enemy); }
            TestBase.AssertTrue(Link(0) == AudioCue.Combat_EnemyChainStep1, "Link 1 is thud 1", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Link(1) == AudioCue.Combat_EnemyChainStep2, "Link 2 is thud 2", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Link(2) == AudioCue.Combat_EnemyChainStep3, "Link 3 is thud 3", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Link(3) == AudioCue.Combat_EnemyChainStep3, "Link 4 holds at thud 3 (quieter escalation)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(Link(0) == AudioCue.Combat_EnemyChainStep1, "A new chain starts at thud 1", ref _run, ref _passed, ref _failed);

            TestBase.AssertTrue(AudioCue.Combat_EnemyChainStep2.GetFallback() == AudioCue.Combat_HeroHurt, "Unbound thuds fall back to the hero-hurt sound", ref _run, ref _passed, ref _failed);

            foreach (var cue in new[] { AudioCue.Combat_EnemyChainStep1, AudioCue.Combat_EnemyChainStep2, AudioCue.Combat_EnemyChainStep3 })
            {
                var b = AudioConfig.DefaultBindingFor(cue);
                TestBase.AssertTrue(!string.IsNullOrEmpty(b.File) && b.Volume < 1.0f, $"{cue} ships bound and quieter ({b.File}, {b.Volume})", ref _run, ref _passed, ref _failed);
                string path = AudioConfig.ResolveAssetPath(b.File);
                TestBase.AssertTrue(File.Exists(path), $"{cue} file exists ({path})", ref _run, ref _passed, ref _failed);
            }

            TestBase.PrintSummary("AudioSplit Tests", _run, _passed, _failed);
        }
    }
}
