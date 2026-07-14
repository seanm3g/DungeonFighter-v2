using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using RPGGame;
using RPGGame.Combat.Formatting;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;
using RPGGame.UI.ColorSystem.Applications;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// Comprehensive tests for DamageFormatter
    /// Tests damage display formatting, spacing, and color application
    /// </summary>
    public static class DamageFormatterTests
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;

        /// <summary>
        /// Runs all DamageFormatter tests
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== DamageFormatter Tests ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;

            TestAddForAmountUnit();
            TestAddWithAction();
            TestAddUsesAction();
            TestAddHitsTarget();
            TestAddAttackVsArmor();
            TestAddSpeedInfo();
            TestAddAmpInfo();
            TestAddTakesDamageFrom();
            TestAddBracketedActorTakesDamage();
            TestAddActorTakesDamage();
            TestAddActorTakesDamage_UsesStatusEffectTemplateForDamageType();
            TestAddActorTakesDamage_ActorOverload_MatchesCreatureNameShading();
            TestAddActorTakesDamage_MarkupRoundTripPreservesHeroNameAndBleedTemplate();
            TestAddBracketedActorNoLongerAffected();
            TestAddActorNoLongerAffected();
            TestAddEffectStacksRemain();
            TestBleedStatusDetailWordsUseBleedingTemplate();
            TestFormatDamageDisplayColored();

            TestBase.PrintSummary("DamageFormatter Tests", _testsRun, _testsPassed, _testsFailed);
        }

        #region AddForAmountUnit Tests

        private static void TestAddForAmountUnit()
        {
            Console.WriteLine("--- Testing AddForAmountUnit ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddForAmountUnit(builder, "10", ColorPalette.Damage, "damage", ColorPalette.Info);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddForAmountUnit should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Color overload
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddForAmountUnit(builder2, "20", ColorPalette.Damage, "health", Colors.White);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddForAmountUnit (Color overload) should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddWithAction Tests

        private static void TestAddWithAction()
        {
            Console.WriteLine("\n--- Testing AddWithAction ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddWithAction(builder, "JAB", ColorPalette.Green);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddWithAction should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddUsesAction Tests

        private static void TestAddUsesAction()
        {
            Console.WriteLine("\n--- Testing AddUsesAction ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddUsesAction(builder, "HEAL", ColorPalette.Green);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddUsesAction should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddHitsTarget Tests

        private static void TestAddHitsTarget()
        {
            Console.WriteLine("\n--- Testing AddHitsTarget ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder, "Enemy", ColorPalette.Enemy);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddHitsTarget should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with hitsColor
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder2, "Player", ColorPalette.Player, ColorPalette.Success);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddHitsTarget with hitsColor should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test Color overload
            var builder3 = new ColoredTextBuilder();
            DamageFormatter.AddHitsTarget(builder3, "Target", Colors.Red);
            var result3 = builder3.Build();

            TestBase.AssertTrue(result3.Count > 0,
                "AddHitsTarget (Color overload) should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddAttackVsArmor Tests

        private static void TestAddAttackVsArmor()
        {
            Console.WriteLine("\n--- Testing AddAttackVsArmor ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddAttackVsArmor(builder, 15, 5);
            var result = builder.Build();
            string plain = ColoredTextRenderer.RenderAsPlainText(result);

            TestBase.AssertTrue(result.Count > 0,
                "AddAttackVsArmor should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(plain.Contains("attack:", StringComparison.Ordinal),
                "AddAttackVsArmor should use attack: label",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
            TestBase.AssertTrue(plain.Contains("15 - 5 armor = 10", StringComparison.Ordinal),
                "AddAttackVsArmor should show raw - armor = net",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var multiBuilder = new ColoredTextBuilder();
            DamageFormatter.AddAttackVsArmor(multiBuilder, 23, 1, multiHitCount: 3);
            string multiPlain = ColoredTextRenderer.RenderAsPlainText(multiBuilder.Build());
            TestBase.AssertTrue(multiPlain.Contains("23 - 1 armor = 22 × 3", StringComparison.Ordinal),
                "AddAttackVsArmor multihit should show net × hit count",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string zeroArmor = DamageFormatter.FormatAttackVsArmorPlain(23, 0, 3);
            TestBase.AssertEqual("attack: 23 × 3", zeroArmor,
                "FormatAttackVsArmorPlain with 0 armor should omit DR clause",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddSpeedInfo Tests

        private static void TestAddSpeedInfo()
        {
            Console.WriteLine("\n--- Testing AddSpeedInfo ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddSpeedInfo(builder, 1.5);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddSpeedInfo should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            // Test with different speed values
            var builder2 = new ColoredTextBuilder();
            DamageFormatter.AddSpeedInfo(builder2, 0.5);
            var result2 = builder2.Build();

            TestBase.AssertTrue(result2.Count > 0,
                "AddSpeedInfo with different speed should add text",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddAmpInfo Tests

        private static void TestAddAmpInfo()
        {
            Console.WriteLine("\n--- Testing AddAmpInfo ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddAmpInfo(builder, 2.5);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddAmpInfo should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            string text2_5 = string.Concat(result.Select(s => s.Text));
            TestBase.AssertTrue(text2_5.Contains("2.50x"),
                "AddAmpInfo should format multiplier with two decimals (2.50x)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var builderSmall = new ColoredTextBuilder();
            DamageFormatter.AddAmpInfo(builderSmall, 1.02);
            var smallAmp = string.Concat(builderSmall.Build().Select(s => s.Text));
            TestBase.AssertTrue(smallAmp.Contains("1.02x"),
                "AddAmpInfo should not round small combo amps to 1.0x (expect 1.02x)",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var builderUnity = new ColoredTextBuilder();
            DamageFormatter.AddAmpInfo(builderUnity, 1.0);
            var unity = string.Concat(builderUnity.Build().Select(s => s.Text));
            TestBase.AssertTrue(unity.Contains("1.00x"),
                "AddAmpInfo should format unity multiplier as 1.00x",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddTakesDamageFrom Tests

        private static void TestAddTakesDamageFrom()
        {
            Console.WriteLine("\n--- Testing AddTakesDamageFrom ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddTakesDamageFrom(builder, 10, "Poison", ColorPalette.Warning);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddTakesDamageFrom should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddBracketedActorTakesDamage Tests

        private static void TestAddBracketedActorTakesDamage()
        {
            Console.WriteLine("\n--- Testing AddBracketedActorTakesDamage ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddBracketedActorTakesDamage(builder, "Enemy", ColorPalette.Enemy, 15, "poison");
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddBracketedActorTakesDamage should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddActorTakesDamage Tests

        private static void TestAddActorTakesDamage()
        {
            Console.WriteLine("\n--- Testing AddActorTakesDamage ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorTakesDamage(builder, "Player", Colors.White, 20, "bleed");
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddActorTakesDamage should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestAddActorTakesDamage_UsesStatusEffectTemplateForDamageType()
        {
            Console.WriteLine("\n--- Testing AddActorTakesDamage status effect damage type color ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorTakesDamage(builder, "Enemy", Colors.White, 20, "poison");
            var result = builder.Build();

            string templatedText = string.Concat(result
                .Where(segment => string.Equals(segment.SourceTemplate, StatusEffectColorHelper.GetTemplateName("poison"), StringComparison.OrdinalIgnoreCase))
                .Select(segment => segment.Text));

            TestBase.AssertEqual("poison", templatedText,
                "Poison damage type should use status-effect color template notation",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        /// <summary>
        /// DoT lines previously used a single <see cref="EntityColorHelper.GetActorColor"/> for the full name,
        /// which breaks creature per-letter shading and markup round-trip. Actor overload must match action-line naming.
        /// </summary>
        private static void TestAddActorTakesDamage_ActorOverload_MatchesCreatureNameShading()
        {
            Console.WriteLine("\n--- Testing AddActorTakesDamage (Actor) matches creature name shading ---");

            var enemy = TestDataBuilders.Enemy().WithName("Salamander").Build();
            var dotBuilder = new ColoredTextBuilder();
            DamageFormatter.AddActorTakesDamage(dotBuilder, enemy, 1, "bleed");
            var dotSegs = dotBuilder.Build();

            var nameBuilder = new ColoredTextBuilder();
            EntityColorHelper.AppendActorNameColored(nameBuilder, enemy);
            var nameSegs = nameBuilder.Build();

            TestBase.AssertTrue(nameSegs.Count > 1,
                "Salamander should use multi-segment creature shading",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            int takesIdx = dotSegs.FindIndex(s => s.Text == "takes");
            TestBase.AssertEqual(nameSegs.Count + 1, takesIdx,
                "DoT line should be name segments, space, then takes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            TestBase.AssertEqual(" ", dotSegs[nameSegs.Count].Text,
                "Space expected between name and takes",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 0; i < nameSegs.Count; i++)
            {
                TestBase.AssertEqual(nameSegs[i].Text, dotSegs[i].Text,
                    $"Name segment {i} text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
                bool rgb = nameSegs[i].Color.R == dotSegs[i].Color.R
                    && nameSegs[i].Color.G == dotSegs[i].Color.G
                    && nameSegs[i].Color.B == dotSegs[i].Color.B;
                TestBase.AssertTrue(rgb,
                    $"Name segment {i} color should match AppendActorNameColored",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        private static void TestAddActorTakesDamage_MarkupRoundTripPreservesHeroNameAndBleedTemplate()
        {
            Console.WriteLine("\n--- Testing AddActorTakesDamage markup round-trip preserves hero name and bleed template ---");

            var hero = new Character("Brogan Brightmoon", 4);
            hero.Progression.BarbarianPoints = 3;

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorTakesDamage(builder, hero, 2, "bleed");

            var heroNameSegments = HeroNamePanelColoredText.BuildLeftPanelHeroNameSegments(hero);
            string markup = ColoredTextRenderer.RenderAsMarkup(builder.Build());
            var parsed = ColoredTextParser.Parse(markup);

            TestBase.AssertTrue(parsed.Count >= heroNameSegments.Count,
                "Round-tripped bleed DoT line should include hero name segments",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            for (int i = 0; i < heroNameSegments.Count && i < parsed.Count; i++)
            {
                TestBase.AssertEqual(heroNameSegments[i].Text, parsed[i].Text,
                    $"Round-tripped hero name segment {i} text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                bool rgb = heroNameSegments[i].Color.R == parsed[i].Color.R
                    && heroNameSegments[i].Color.G == parsed[i].Color.G
                    && heroNameSegments[i].Color.B == parsed[i].Color.B;
                TestBase.AssertTrue(rgb,
                    $"Round-tripped hero name segment {i} color should preserve HUD palette",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }

            string bleedTemplateText = string.Concat(parsed
                .Where(segment => string.Equals(segment.SourceTemplate, StatusEffectColorHelper.GetTemplateName("bleed"), StringComparison.OrdinalIgnoreCase))
                .Select(segment => segment.Text));

            TestBase.AssertEqual("bleed", bleedTemplateText,
                "Round-tripped bleed damage type should preserve bleeding status-effect template",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddBracketedActorNoLongerAffected Tests

        private static void TestAddBracketedActorNoLongerAffected()
        {
            Console.WriteLine("\n--- Testing AddBracketedActorNoLongerAffected ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddBracketedActorNoLongerAffected(builder, "Enemy", ColorPalette.Enemy, "poisoned", ColorPalette.Warning);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddBracketedActorNoLongerAffected should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddActorNoLongerAffected Tests

        private static void TestAddActorNoLongerAffected()
        {
            Console.WriteLine("\n--- Testing AddActorNoLongerAffected ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddActorNoLongerAffected(builder, "Player", Colors.White, "burning", ColorPalette.Error);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddActorNoLongerAffected should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region AddEffectStacksRemain Tests

        private static void TestAddEffectStacksRemain()
        {
            Console.WriteLine("\n--- Testing AddEffectStacksRemain ---");

            var builder = new ColoredTextBuilder();
            DamageFormatter.AddEffectStacksRemain(builder, "Poison", ColorPalette.Warning, 3);
            var result = builder.Build();

            TestBase.AssertTrue(result.Count > 0,
                "AddEffectStacksRemain should add text to builder",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        private static void TestBleedStatusDetailWordsUseBleedingTemplate()
        {
            Console.WriteLine("\n--- Testing bleed status detail words use bleeding template ---");

            var stacksBuilder = new ColoredTextBuilder();
            DamageFormatter.AddEffectStacksRemain(stacksBuilder, "bleed", ColorPalette.Error, 1);
            var stacks = stacksBuilder.Build();
            string stacksBleedText = string.Concat(stacks
                .Where(segment => string.Equals(segment.SourceTemplate, StatusEffectColorHelper.GetTemplateName("bleed"), StringComparison.OrdinalIgnoreCase))
                .Select(segment => segment.Text));

            TestBase.AssertEqual("bleed", stacksBleedText,
                "Bleed stack detail should use bleeding status-effect template",
                ref _testsRun, ref _testsPassed, ref _testsFailed);

            var hero = new Character("Brogan Brightmoon", 4);
            var endedBuilder = new ColoredTextBuilder();
            DamageFormatter.AddActorNoLongerAffected(endedBuilder, hero, "bleeding", ColorPalette.Error);
            var ended = endedBuilder.Build();
            string endedBleedText = string.Concat(ended
                .Where(segment => string.Equals(segment.SourceTemplate, StatusEffectColorHelper.GetTemplateName("bleeding"), StringComparison.OrdinalIgnoreCase))
                .Select(segment => segment.Text));

            TestBase.AssertEqual("bleeding", endedBleedText,
                "Bleed ended detail should use bleeding status-effect template",
                ref _testsRun, ref _testsPassed, ref _testsFailed);
        }

        #endregion

        #region FormatDamageDisplayColored Tests

        private static void TestFormatDamageDisplayColored()
        {
            Console.WriteLine("\n--- Testing FormatDamageDisplayColored ---");

            try
            {
                var attacker = TestDataBuilders.Character()
                    .WithName("Attacker")
                    .WithStats(10, 10, 10, 10)
                    .Build();

                var target = TestDataBuilders.Enemy()
                    .WithName("Target")
                    .WithHealth(100)
                    .Build();

                var action = TestDataBuilders.CreateMockAction("JAB");

                var (damageText, rollInfo) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 1.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText != null && damageText.Count > 0,
                    "FormatDamageDisplayColored should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var hitsSegment = damageText!.FirstOrDefault(s => s.Text == "hits");
                TestBase.AssertTrue(hitsSegment != null && ColorValidator.AreColorsEqual(hitsSegment.Color, ColorPalette.White.GetColor()),
                    "\"hits\" verb in damage line should be white",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                TestBase.AssertTrue(rollInfo != null && rollInfo.Count > 0,
                    "FormatDamageDisplayColored should return roll info",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with combo amplifier
                var (damageText2, rollInfo2) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 2.0, 1.0, 0, 10, 1);

                TestBase.AssertTrue(damageText2 != null && damageText2.Count > 0,
                    "FormatDamageDisplayColored with combo should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Test with multi-hit
                var (damageText3, rollInfo3) = DamageFormatter.FormatDamageDisplayColored(
                    attacker, target, 20, 15, action, 1.0, 1.0, 0, 10, 3);

                TestBase.AssertTrue(damageText3 != null && damageText3.Count > 0,
                    "FormatDamageDisplayColored with multi-hit should return damage text",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                var multiHitHitsSuffix = damageText3!.FirstOrDefault(s => s.Text == " hits)");
                TestBase.AssertTrue(multiHitHitsSuffix != null && ColorValidator.AreColorsEqual(multiHitHitsSuffix.Color, Colors.White),
                    "multi-hit line \" hits)\" should be white (hits keyword)",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);

                // Enemy attacker: action name uses purple (not green / roll cyan / damage red)
                var enemyAttacker = TestDataBuilders.Enemy()
                    .WithName("Skeleton")
                    .WithHealth(50)
                    .Build();
                var heroTarget = TestDataBuilders.Character()
                    .WithName("Hero")
                    .WithStats(10, 10, 10, 10)
                    .Build();
                var (enemyDamageText, _) = DamageFormatter.FormatDamageDisplayColored(
                    enemyAttacker, heroTarget, 5, 3, action, 1.0, 1.0, 0, 10, 1);
                var jabSegment = enemyDamageText!.FirstOrDefault(s => s.Text == "JAB");
                TestBase.AssertTrue(jabSegment != null && ColorValidator.AreColorsEqual(jabSegment.Color, ColorPalette.Purple.GetColor()),
                    "enemy attack action name should use purple",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false,
                    $"FormatDamageDisplayColored failed: {ex.Message}",
                    ref _testsRun, ref _testsPassed, ref _testsFailed);
            }
        }

        #endregion
    }
}
