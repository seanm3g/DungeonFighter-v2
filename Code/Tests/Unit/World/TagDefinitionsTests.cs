using System.Linq;
using RPGGame;
using RPGGame.World.Tags;

namespace RPGGame.Tests.Unit.World
{
    public static class TagDefinitionsTests
    {
        private static int _run;
        private static int _pass;
        private static int _fail;

        public static void RunAll(ref int testsRun, ref int testsPassed, ref int testsFailed)
        {
            _run = _pass = _fail = 0;

            TestKnownTagCount(ref _run, ref _pass, ref _fail);
            TestEnemyArchetypes(ref _run, ref _pass, ref _fail);
            TestValidateEnemyTags(ref _run, ref _pass, ref _fail);
            TestElementTags(ref _run, ref _pass, ref _fail);
            TestRegistrySeeded(ref _run, ref _pass, ref _fail);

            testsRun += _run;
            testsPassed += _pass;
            testsFailed += _fail;
        }

        private static void TestKnownTagCount(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestKnownTagCount));
            TestBase.AssertEqual(59, TagDefinitions.AllRegistryTags.Count(), "59 registry tags", ref run, ref pass, ref fail);
        }

        private static void TestEnemyArchetypes(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestEnemyArchetypes));
            TestBase.AssertEqual(10, TagDefinitions.ValidEnemyArchetypes.Count, "10 archetypes", ref run, ref pass, ref fail);
            TestBase.AssertTrue(TagDefinitions.IsValidEnemyArchetype("Knight"), "Knight valid", ref run, ref pass, ref fail);
            TestBase.AssertTrue(TagDefinitions.IsValidEnemyArchetype("sage"), "sage case-insensitive", ref run, ref pass, ref fail);
            TestBase.AssertFalse(TagDefinitions.IsValidEnemyArchetype("Guardian"), "Guardian removed", ref run, ref pass, ref fail);
            TestBase.AssertTrue(TagDefinitions.TryParseEnemyArchetype("Warlord", out var wl) && wl == EnemyArchetype.Warlord,
                "parse Warlord", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Trickster", TagDefinitions.CanonicalizeEnemyArchetype("trickster"), "canonicalize", ref run, ref pass, ref fail);
        }

        private static void TestValidateEnemyTags(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestValidateEnemyTags));
            var warnings = TagDefinitions.ValidateTagList(TagEntityScope.Enemy, new[] { "undead", "trickster" });
            TestBase.AssertTrue(warnings.Any(w => w.Contains("hero subclass")), "subclass on enemy warns", ref run, ref pass, ref fail);
            TestBase.AssertTrue(TagDefinitions.ValidateTagList(TagEntityScope.Enemy, new[] { "not_a_tag" }).Count > 0,
                "unknown tag warns", ref run, ref pass, ref fail);
        }

        private static void TestElementTags(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestElementTags));
            TestBase.AssertTrue(TagDefinitions.IsKnownTag("fire"), "fire", ref run, ref pass, ref fail);
            TestBase.AssertFalse(TagDefinitions.IsKnownTag("ice"), "ice not in registry", ref run, ref pass, ref fail);
            TestBase.AssertFalse(TagDefinitions.IsKnownTag("lightning"), "lightning not in registry", ref run, ref pass, ref fail);
        }

        private static void TestRegistrySeeded(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(TestRegistrySeeded));
            var registry = TagRegistry.Instance;
            TestBase.AssertTrue(registry.IsTagRegistered("environment"), "environment registered", ref run, ref pass, ref fail);
            TestBase.AssertTrue(registry.IsTagRegistered("modtrade"), "modtrade registered", ref run, ref pass, ref fail);
            TestBase.AssertFalse(registry.IsTagRegistered("TRANSCENDENT"), "legacy removed", ref run, ref pass, ref fail);
        }
    }
}
