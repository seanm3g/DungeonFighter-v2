using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Conditional;
using RPGGame.Combat.Events;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.Tuning;

namespace RPGGame.Tests.Unit
{
    /// <summary>The single trigger event vocabulary (<see cref="TriggerEvents"/>) and the parsers routed through it.</summary>
    public static class TriggerEventsTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Trigger Events Tests ===\n");
            _run = _passed = _failed = 0;

            TestTokensAndAliasesAreUnique();
            TestAliasesResolveToCanonical();
            TestGateKeepsFilterAliases();
            TestEveryMaterialSynthesisWhenIsAnEvent();
            TestEveryTriggerRowWhenIsAnEvent();
            TestLabelsAndDescriptions();
            TestReservedEventsAreNotStoredAsConditions();
            TestSynergyWhenKeysCanonicalize();
            TestSynergyBotWhenKeysAreEvents();

            TestBase.PrintSummary("Trigger Events Tests", _run, _passed, _failed);
        }

        private static void TestTokensAndAliasesAreUnique()
        {
            TestBase.SetCurrentTestName(nameof(TestTokensAndAliasesAreUnique));
            var names = TriggerEvents.All.SelectMany(d => new[] { d.Token }.Concat(d.Aliases)).ToList();
            TestBase.AssertEqual(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                "no token or alias is listed twice", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(TriggerEvents.All.All(d => TriggerEvents.NormalizeToken(d.Token) == d.Token),
                "every canonical token normalizes to itself", ref _run, ref _passed, ref _failed);
        }

        private static void TestAliasesResolveToCanonical()
        {
            TestBase.SetCurrentTestName(nameof(TestAliasesResolveToCanonical));
            var cases = new (string Raw, string Expected)[]
            {
                ("ON_RECEIVE_HIT", "ONTAKEHIT"),
                ("ON_TAKE_HIT", "ONTAKEHIT"),
                ("ONHEROHURT", "ONTAKEHIT"),
                ("ON_ENEMY_HEALTH_THRESHOLD", "ONHEALTHTHRESHOLD"),
                ("ON_COMBO_END", "ONCOMBOEND"),
                ("on combo ended", "ONCOMBOEND"),
                ("ONCRIT", "ONCRITICAL"),
                ("ON_CRITICAL", "ONCRITICAL"),
                ("ONFIRSTBLOOD", "ONFIRSTHIT"),
                ("ON_FIRST_HIT", "ONFIRSTHIT"),
                ("ON_AFTER_MISS", "ONAFTERMISS"),
                ("ONANYHIT", "ONCONNECT"),
                ("ON_CONSECUTIVE_ATTACK", "ONCONSECUTIVEATTACK"),
                ("WHILE_EQUIPPED", "WHILEEQUIPPED"),
                ("ON-KILL", "ON-KILL"), // the generic normalizer keeps hyphens (filter args may use them)
            };
            foreach (var (raw, expected) in cases)
                TestBase.AssertEqual(expected, TriggerEvents.NormalizeToken(raw), $"{raw} → {expected}", ref _run, ref _passed, ref _failed);

            TestBase.AssertEqual("ONKILL", MaterialBuildData.NormalizeWhenToken("ON-KILL"),
                "material synthesis drops hyphens", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(TriggerEvents.IsTakeHit("ON_RECEIVE_HIT") && TriggerEvents.IsTakeHit("ONHEROHURT")
                && !TriggerEvents.IsTakeHit("ONCONNECT"), "IsTakeHit accepts every take-hit spelling", ref _run, ref _passed, ref _failed);
        }

        private static void TestGateKeepsFilterAliases()
        {
            TestBase.SetCurrentTestName(nameof(TestGateKeepsFilterAliases));
            TestBase.AssertEqual("IFSAMESACTION", ActionTriggerGate.NormalizeToken("IFMIRROR"), "IFMIRROR alias", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("IFDIFFERENTACTION", ActionTriggerGate.NormalizeToken("IF_SWITCH_UP"), "IFSWITCHUP alias", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("IFCLUTCH", ActionTriggerGate.NormalizeToken("IFCLUTCH"), "unknown filter passes through", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ONTAKEHIT", ActionTriggerGate.NormalizeToken("ON_RECEIVE_HIT"), "gate resolves events via registry", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(ActionTriggerGate.KnownTokens.Contains("ONRECEIVEHIT") && ActionTriggerGate.KnownTokens.Contains("IFCLUTCH"),
                "KnownTokens lists event aliases and filters", ref _run, ref _passed, ref _failed);
        }

        private static void TestEveryMaterialSynthesisWhenIsAnEvent()
        {
            TestBase.SetCurrentTestName(nameof(TestEveryMaterialSynthesisWhenIsAnEvent));
            var builds = MaterialBuildsLoader.GetAll();
            TestBase.AssertTrue(builds.Count >= 18, "material builds loaded", ref _run, ref _passed, ref _failed);
            foreach (var build in builds)
            {
                bool ok = TriggerEvents.TryGet(build.WhenToken, out var def)
                          && def.Token == build.WhenToken
                          && def.Emitted
                          && def.Family != TriggerEventFamily.Equip;
                TestBase.AssertTrue(ok, $"{build.Material} synthesis '{build.Synthesis}' → canonical emitted event ({build.WhenToken})",
                    ref _run, ref _passed, ref _failed);
            }
        }

        private static void TestEveryTriggerRowWhenIsAnEvent()
        {
            TestBase.SetCurrentTestName(nameof(TestEveryTriggerRowWhenIsAnEvent));
            var unknown = TriggersLoader.GetAll()
                .Where(t => !string.IsNullOrWhiteSpace(t.When) && !TriggerEvents.IsEvent(t.When))
                .Select(t => $"{t.Name}:{t.When}")
                .ToList();
            TestBase.AssertTrue(unknown.Count == 0, "every Triggers.json WHEN is an event" + (unknown.Count > 0 ? " — " + string.Join(", ", unknown) : ""),
                ref _run, ref _passed, ref _failed);
        }

        private static void TestLabelsAndDescriptions()
        {
            TestBase.SetCurrentTestName(nameof(TestLabelsAndDescriptions));
            TestBase.AssertEqual("On take hit", ItemTriggerBundleDisplay.FormatWhen("ON_RECEIVE_HIT"), "take-hit alias label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("On natural 7", ItemTriggerBundleDisplay.FormatWhen("ONNATURALROLL:7"), "arg label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("On room clear #3", ItemTriggerBundleDisplay.FormatWhen("ONROOMSCLEARED:3"), "room-clear arg label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("On roll total 15", ItemTriggerBundleDisplay.FormatWhen("ONROLLVALUE:15"), "roll-value arg label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("While equipped", ItemTriggerBundleDisplay.FormatWhen("WHILE_EQUIPPED"), "equip label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("On normal hit", ItemTriggerBundleDisplay.FormatWhen("ONHIT"), "normal hit label", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("each time an enemy hits you", MaterialBuildData.DescribeWhen("ON_RECEIVE_HIT"), "describe alias", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("its set trigger", MaterialBuildData.DescribeWhen(""), "describe empty", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ONSOMETHINGNEW", MaterialBuildData.DescribeWhen("ON_SOMETHING_NEW"), "describe unknown falls back to token", ref _run, ref _passed, ref _failed);
        }

        private static void TestReservedEventsAreNotStoredAsConditions()
        {
            TestBase.SetCurrentTestName(nameof(TestReservedEventsAreNotStoredAsConditions));
            foreach (var reserved in TriggerEvents.All.Where(d => !d.Emitted))
            {
                var parsed = ActionTriggerGate.ParseTriggerConditionList(reserved.Token);
                TestBase.AssertTrue(parsed.Count == 0, $"{reserved.Token} not stored until combat fires it", ref _run, ref _passed, ref _failed);
            }
            var takeHit = ActionTriggerGate.ParseTriggerConditionList("ON_TAKE_HIT, ON_CRIT");
            TestBase.AssertTrue(takeHit.Contains("ONTAKEHIT") && takeHit.Contains("ONCRITICAL"), "aliases parse to canonical conditions",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestSynergyWhenKeysCanonicalize()
        {
            TestBase.SetCurrentTestName(nameof(TestSynergyWhenKeysCanonicalize));
            var a = new Dictionary<string, double> { ["heal"] = 1 };
            var b = new Dictionary<string, double> { ["heal"] = 2 };

            var aliasOnly = RulesSynergy.CanonicalizeWhenKeys(new Dictionary<string, Dictionary<string, double>> { ["when:ONRECEIVEHIT"] = a, ["node:x"] = b });
            TestBase.AssertTrue(aliasOnly.TryGetValue("when:ONTAKEHIT", out var got) && ReferenceEquals(got, a), "alias key renamed", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(aliasOnly.ContainsKey("node:x"), "non-when keys kept", ref _run, ref _passed, ref _failed);

            var both = RulesSynergy.CanonicalizeWhenKeys(new Dictionary<string, Dictionary<string, double>> { ["when:ONTAKEHIT"] = b, ["when:ON_RECEIVE_HIT"] = a });
            TestBase.AssertTrue(both.Count == 1 && ReferenceEquals(both["when:ONTAKEHIT"], b), "canonical key wins over alias", ref _run, ref _passed, ref _failed);
        }

        private static void TestSynergyBotWhenKeysAreEvents()
        {
            TestBase.SetCurrentTestName(nameof(TestSynergyBotWhenKeysAreEvents));
            var whenKeys = RulesSynergy.Data.Rules.Keys.Where(k => k.StartsWith("when:", StringComparison.OrdinalIgnoreCase)).ToList();
            TestBase.AssertTrue(whenKeys.Count > 0, "synergy-bot.json has when: rules", ref _run, ref _passed, ref _failed);
            foreach (string key in whenKeys)
                TestBase.AssertTrue(TriggerEvents.IsEvent(key[5..]), $"{key} is an event", ref _run, ref _passed, ref _failed);

            // Every formed material set must find its weights (Bone = ONTAKEHIT, Iron = ONHEALTHTHRESHOLD after the merge).
            foreach (var build in MaterialBuildsLoader.GetAll().Where(m => m.Material is "Bone" or "Iron"))
                TestBase.AssertTrue(RulesSynergy.Data.Rules.ContainsKey("when:" + build.WhenToken),
                    $"{build.Material} set ({build.WhenToken}) keeps its synergy weights", ref _run, ref _passed, ref _failed);
        }
    }
}
