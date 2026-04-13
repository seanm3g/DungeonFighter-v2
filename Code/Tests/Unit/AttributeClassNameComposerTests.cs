using RPGGame.Tests;

namespace RPGGame.Tests.Unit
{
    public static class AttributeClassNameComposerTests
    {
        public static void RunAllTests()
        {
            int run = 0, passed = 0, failed = 0;

            ZeroPathsUsesDefault(ref run, ref passed, ref failed);
            SoloNoDisciplineSuffix(ref run, ref passed, ref failed);
            DuoNoDisciplineSuffix(ref run, ref passed, ref failed);
            MultiPathUsesTopTwoDuoPlusVeil(ref run, ref passed, ref failed);
            MultiPathTopTwoByPointsChoosesDuoCore(ref run, ref passed, ref failed);
            QuadAllPathsUsesTopTwoPlusVeil(ref run, ref passed, ref failed);
            ThirdPathSuffixUsesConfig(ref run, ref passed, ref failed);
            ThirdPathSuffixRequiresFirstGate(ref run, ref passed, ref failed);
            TierPrefixAlignsWithSoloTrioBands(ref run, ref passed, ref failed);

            TestBase.PrintSummary("AttributeClassNameComposerTests", run, passed, failed);
        }

        private static ClassPresentationConfig TestPresentation() =>
            new ClassPresentationConfig
            {
                TierThresholds = new[] { 10, 25, 45, 70 },
                DefaultNoPointsClassName = "Fighter"
            }.EnsureNormalized();

        private static CharacterProgression Prog(int mace, int sword, int dagger, int wand)
        {
            var p = new CharacterProgression();
            p.BarbarianPoints = mace;
            p.WarriorPoints = sword;
            p.RoguePoints = dagger;
            p.WizardPoints = wand;
            return p;
        }

        private static void ZeroPathsUsesDefault(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            string r = AttributeClassNameComposer.ComposeDisplayClass(Prog(0, 0, 0, 0), cfg);
            TestBase.AssertEqual("Fighter", r, "no class points -> default", ref run, ref passed, ref failed);
            string r2 = AttributeClassNameComposer.ComposeDisplayClass(null, cfg);
            TestBase.AssertEqual("Fighter", r2, "null progression -> default", ref run, ref passed, ref failed);
        }

        private static void SoloNoDisciplineSuffix(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(0, 30, 0, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Blooded Warrior", r, "solo: prefix slot matches tier band vs thresholds (30 pts → band 2)", ref run, ref passed, ref failed);
        }

        private static void DuoNoDisciplineSuffix(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(32, 30, 0, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Blooded Warbrute", r, "duo: prefix from highest path tier band", ref run, ref passed, ref failed);
        }

        private static void MultiPathUsesTopTwoDuoPlusVeil(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(12, 11, 10, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Scarred Warbrute of the Veil", r, "3 paths: max pts band 1 → band-1 prefix; duo core + third-highest suffix", ref run, ref passed, ref failed);
        }

        private static void MultiPathTopTwoByPointsChoosesDuoCore(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(5, 25, 20, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Blooded Duelist", r, "3 paths: third-highest below first gate → no suffix", ref run, ref passed, ref failed);
        }

        private static void QuadAllPathsUsesTopTwoPlusVeil(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(75, 75, 75, 75);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Abyssal Warbrute of the Veil", r, "4 paths: same rule — top two (Mace+Sword tie-break) + veil suffix", ref run, ref passed, ref failed);
        }

        private static void TierPrefixAlignsWithSoloTrioBands(ref int run, ref int passed, ref int failed)
        {
            var cfg = new ClassPresentationConfig
            {
                TierThresholds = new[] { 2, 20, 60, 100 },
                AttributeSoloTrioTierPrefixes = new[] { "Lesser", "Blooded", "Dread", "Abyssal" },
                MaceClassDisplayName = "Barbarian"
            }.EnsureNormalized();
            string one = AttributeClassNameComposer.ComposeDisplayClass(Prog(1, 0, 0, 0), cfg);
            TestBase.AssertEqual("Fighter", one, "below first tier gate → default shown label", ref run, ref passed, ref failed);
            string two = AttributeClassNameComposer.ComposeDisplayClass(Prog(2, 0, 0, 0), cfg);
            TestBase.AssertEqual("Lesser Barbarian", two, "2 pts on highest path → band 1 → Band 1 word (Lesser)", ref run, ref passed, ref failed);
            string twenty = AttributeClassNameComposer.ComposeDisplayClass(Prog(20, 0, 0, 0), cfg);
            TestBase.AssertEqual("Blooded Barbarian", twenty, "20 pts → band 2 → Band 2 word (Blooded)", ref run, ref passed, ref failed);
        }

        private static void ThirdPathSuffixRequiresFirstGate(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            var p = Prog(30, 25, 5, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Blooded Warbrute", r, "third path 5 pts < first gate 10 → duo hybrid only", ref run, ref passed, ref failed);
        }

        private static void ThirdPathSuffixUsesConfig(ref int run, ref int passed, ref int failed)
        {
            var cfg = TestPresentation();
            cfg.AttributeModifierDagger = "of the Beyond";
            cfg = cfg.EnsureNormalized();
            var p = Prog(12, 11, 10, 0);
            string r = AttributeClassNameComposer.ComposeDisplayClass(p, cfg);
            TestBase.AssertEqual("Scarred Warbrute of the Beyond", r, "3 paths: Dagger third-highest → Dagger modifier row", ref run, ref passed, ref failed);
        }
    }
}
