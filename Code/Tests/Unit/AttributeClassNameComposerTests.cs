using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class AttributeClassNameComposerTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            ZeroMeaningfulUsesDefault(ref run, ref passed, ref failed);
            DuoWithModifier(ref run, ref passed, ref failed);
            TrioUsesLowestCoreModifier(ref run, ref passed, ref failed);
            QuadUsesTierWord(ref run, ref passed, ref failed);
            QuadTierWordUsesConfig(ref run, ref passed, ref failed);

            TestBase.PrintSummary("AttributeClassNameComposerTests", run, passed, failed);
        }

        /// <summary>Same four gates as legacy attribute tests; display tier now uses class points vs these.</summary>
        private static ClassPresentationConfig TestPresentation() =>
            new ClassPresentationConfig
            {
                MeaningfulAttributeMinimum = 8,
                TierThresholds = new[] { 10, 25, 45, 70 },
                DefaultNoPointsClassName = "Fighter"
            }.EnsureNormalized();

        private static CharacterStats Stats(int str, int agi, int tec, int intel)
        {
            var s = new CharacterStats(1);
            s.Strength = str;
            s.Agility = agi;
            s.Technique = tec;
            s.Intelligence = intel;
            return s;
        }

        private static CharacterProgression ProgSword(int warriorPoints)
        {
            var p = new CharacterProgression();
            p.WarriorPoints = warriorPoints;
            return p;
        }

        private static void ZeroMeaningfulUsesDefault(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var s = Stats(3, 3, 3, 3);
            string r = AttributeClassNameComposer.ComposeDisplayClass(s, null, cfg);
            TestBase.AssertEqual("Fighter", r, "below meaningful floor -> default", ref run, ref passed, ref failed);
        }

        private static void DuoWithModifier(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var s = Stats(30, 25, 7, 5);
            var p = ProgSword(30);
            string r = AttributeClassNameComposer.ComposeDisplayClass(s, p, cfg);
            TestBase.AssertEqual("Dread Warbrute of the Veil", r, "duo tier from class points + modifier", ref run, ref passed, ref failed);
        }

        private static void TrioUsesLowestCoreModifier(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var s = Stats(20, 20, 20, 5);
            var p = ProgSword(20);
            string r = AttributeClassNameComposer.ComposeDisplayClass(s, p, cfg);
            TestBase.AssertEqual("Blooded Dreadstalker of Fury", r, "trio tier from class points", ref run, ref passed, ref failed);
        }

        private static void QuadUsesTierWord(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var s = Stats(50, 50, 50, 50);
            var p = ProgSword(75);
            string r = AttributeClassNameComposer.ComposeDisplayClass(s, p, cfg);
            TestBase.AssertEqual("Eternal", r, "quad tier word from class points band 4", ref run, ref passed, ref failed);
        }

        private static void QuadTierWordUsesConfig(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            cfg.AttributeQuadTierNames = new[] { "A", "B", "CustomQuad", "D" };
            cfg = cfg.EnsureNormalized();
            var s = Stats(44, 44, 44, 44);
            var p = ProgSword(50);
            string r = AttributeClassNameComposer.ComposeDisplayClass(s, p, cfg);
            TestBase.AssertEqual("CustomQuad", r, "quad band 3 from class points uses configured names", ref run, ref passed, ref failed);
        }
    }
}
