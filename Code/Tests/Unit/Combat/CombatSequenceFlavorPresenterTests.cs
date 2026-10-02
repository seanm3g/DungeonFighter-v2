using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media;
using RPGGame;
using RPGGame.Combat;
using RPGGame.Combat.Sequence;
using RPGGame.Tests;
using RPGGame.UI.ColorSystem;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>
    /// F7 narrative combat log: tokens, beat order, and presenter line playback.
    /// </summary>
    public static class CombatSequenceFlavorPresenterTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== CombatSequenceFlavorPresenter Tests ===\n");
            _run = _passed = _failed = 0;

            TestTokensHitIncludesOptionalColumns();
            TestTokensMissSkipsDefenseAndDamage();
            TestInterpretiveTokensHaveNoDigits();
            TestInterpretRollBands();
            TestInterpretDamageBands();
            TestInterpretOutcomeNeverSaysCombo();
            TestInterpretEffectsSoftensPoisonAndBleed();
            TestInterpretEffectsSoftensAffectedByWeaken();
            TestInterpretDoTTickProseHasNoDigits();
            TestInterpretEffectsSoftensMechanicalDoTLines();
            TestInterpretTempoFromActionLength();
            TestInterpretStanceSusceptibility();
            TestInterpretCriticalConsequences();
            TestEnemyNarrativeNamesUseDefiniteArticle();
            TestNarrativeEmphasisColorsKeywords();
            TestNarrativeEmphasisLeavesEnemyArticleUncolored();
            TestPlaceholderFill();
            TestNarrativeBeatOrder();
            TestShouldPlayRequiresNarrativeMode();
            TestHudShouldPlayFalseWhenNarrativeOn();
            TestFromEnvironmentalTokensBuildEffectSentences();
            TestPlayWritesProseParagraphWithoutHud();
            TestPlayEnvironmentalWritesOwnProseParagraph();
            TestSameAttackerContinuesParagraphWithFollowUp();
            TestNarrativeRevealsHudColumnsInBeatOrder();
            TestReserveBandHiddenWhenNarrativeOn();
            TestNarrativeTypewriterTruncatePreservesColors();

            CombatSequenceFlavorPresenter.ResetForTests();
            CombatSequencePresenter.ResetForTests();
            TestBase.PrintSummary("CombatSequenceFlavorPresenter Tests", _run, _passed, _failed);
        }

        private static void TestTokensHitIncludesOptionalColumns()
        {
            Console.WriteLine("--- Hit tokens include action/damage; defense when target is Character ---");
            var result = HitResult("SLAM", damage: 72);
            result.IsCombo = true;
            result.StatusEffectMessages.Add("Poison spreads.");
            var hero = DummyHero();
            var enemy = DummyEnemy();
            // Enemy is Character subclass — defense included for Character targets.
            var tokens = CombatSequenceFlavorTokens.From(result, hero, enemy);

            TestBase.AssertEqual("SeqHero", tokens.Attacker, "attacker name", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("the SeqFoe", tokens.Target, "enemy target uses definite article", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("a chaining SLAM", tokens.Outcome, "combo outcome uses attack name, not COMBO", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.Outcome.Contains("COMBO", StringComparison.OrdinalIgnoreCase),
                "outcome never says COMBO", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("SLAM", tokens.Action, "action name", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(
                CombatSequenceFlavorTokens.InterpretRoll(17),
                tokens.Roll,
                "roll is interpretive",
                ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(tokens.Roll),
                "roll has no digits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(tokens.Damage)
                    && !CombatSequenceFlavorTokens.ContainsDigit(tokens.Damage),
                "damage is interpretive without digits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeDamage, "include damage", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeEffects, "include effects", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.Effects.Contains("poison", StringComparison.OrdinalIgnoreCase),
                "effects text names poison in prose", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeTempo, "include tempo", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!string.IsNullOrWhiteSpace(tokens.Tempo),
                "tempo names who goes next", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeStance, "include stance", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.Stance.Contains("stance", StringComparison.OrdinalIgnoreCase),
                $"stance prose, got: {tokens.Stance}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.IncludeCritical, "no critical beat on non-crit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.IncludeCriticalMiss, "no crit-miss beat on hit", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretOutcomeNeverSaysCombo()
        {
            Console.WriteLine("--- InterpretOutcome never uses the word COMBO ---");
            var combo = HitResult("SLASH", damage: 10);
            combo.IsCombo = true;
            TestBase.AssertEqual("a chaining SLASH",
                CombatSequenceFlavorTokens.InterpretOutcome(combo, "SLASH"),
                "combo names the attack", ref _run, ref _passed, ref _failed);

            var critCombo = HitResult("POUND", damage: 20);
            critCombo.IsCombo = true;
            critCombo.IsCritical = true;
            TestBase.AssertEqual("a perfected POUND",
                CombatSequenceFlavorTokens.InterpretOutcome(critCombo, "POUND"),
                "crit combo names the attack", ref _run, ref _passed, ref _failed);

            var crit = HitResult("JAB", damage: 8);
            crit.IsCritical = true;
            TestBase.AssertEqual("a critical blow",
                CombatSequenceFlavorTokens.InterpretOutcome(crit, "JAB"),
                "plain crit", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretEffectsSoftensPoisonAndBleed()
        {
            Console.WriteLine("--- InterpretEffects turns status lines into prose ---");
            string poison = CombatSequenceFlavorTokens.InterpretEffects(
                new List<string> { "Wight is poisoned!" });
            TestBase.AssertTrue(poison.Contains("poison", StringComparison.OrdinalIgnoreCase),
                $"poison prose, got: {poison}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(poison.Contains("Wight", StringComparison.Ordinal),
                $"names the afflicted, got: {poison}", ref _run, ref _passed, ref _failed);

            string both = CombatSequenceFlavorTokens.InterpretEffects(
                new List<string> { "Wight is poisoned!", "Zephyr Crowcaller is bleeding!" });
            TestBase.AssertTrue(both.Contains("poison", StringComparison.OrdinalIgnoreCase)
                    && both.Contains("bleed", StringComparison.OrdinalIgnoreCase),
                $"joins poison and bleed, got: {both}", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretEffectsSoftensAffectedByWeaken()
        {
            Console.WriteLine("--- InterpretEffects softens 'affected by WEAKEN for N turn' ---");
            string soften = CombatSequenceFlavorTokens.InterpretEffects(
                new List<string> { "Xander Mossword affected by WEAKEN for 1 turn" });
            TestBase.AssertTrue(soften.Contains("Xander Mossword", StringComparison.Ordinal),
                $"names victim, got: {soften}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(soften.Contains("weaken", StringComparison.OrdinalIgnoreCase),
                $"names weaken, got: {soften}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(soften),
                $"no turn numerals, got: {soften}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!soften.Contains("affected by", StringComparison.OrdinalIgnoreCase),
                $"not mechanical affected-by, got: {soften}", ref _run, ref _passed, ref _failed);

            string sentence = CombatSequenceFlavorTokens.InterpretEnvironmentalStatusSentence(
                "Wraith affected by WEAKEN for 1 turn");
            TestBase.AssertTrue(sentence.Contains("Wraith", StringComparison.Ordinal),
                $"env sentence names Wraith, got: {sentence}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(sentence.Contains("falters", StringComparison.OrdinalIgnoreCase),
                $"env sentence uses finite verb, got: {sentence}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(sentence),
                $"env sentence has no digits, got: {sentence}", ref _run, ref _passed, ref _failed);
        }

        private static void TestFromEnvironmentalTokensBuildEffectSentences()
        {
            Console.WriteLine("--- FromEnvironmental builds action + per-target effect sentences ---");
            var room = new Environment("Coral Garden", "reef", false, "forest", "");
            var action = TestDataBuilders.CreateMockAction("Necrotic Aura", ActionType.Debuff);
            action.CausesWeaken = true;
            var hero = DummyHero();
            var foe = DummyEnemy();
            var heroLine = new List<ColoredText>
            {
                new ColoredText(hero.Name, ColorPalette.Gold.GetColor()),
                new ColoredText(" affected by WEAKEN for 1 turn", Colors.White)
            };
            var foeLine = new List<ColoredText>
            {
                new ColoredText(foe.Name, ColorPalette.Enemy.GetColor()),
                new ColoredText(" affected by WEAKEN for 1 turn", Colors.White)
            };

            var tokens = CombatSequenceFlavorTokens.FromEnvironmental(
                room,
                action,
                new List<List<ColoredText>> { heroLine, foeLine },
                damage: 0,
                primaryTarget: hero,
                affectedActors: new List<Actor> { hero, foe });

            TestBase.AssertTrue(tokens.IsEnvironmental, "marked environmental", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Coral Garden", tokens.Attacker, "env attacker", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Necrotic Aura", tokens.Action, "env action", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeEffects, "includes effects", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(2, tokens.EffectSentences.Count, "two effect sentences", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.ShouldInclude(CombatSequenceStepKind.Action),
                "env includes action beat", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.ShouldInclude(CombatSequenceStepKind.Attacker),
                "env skips gather-will", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.ShouldInclude(CombatSequenceStepKind.Tempo),
                "env skips tempo", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("sequenceEnvironment", tokens.ResolveBankKey(CombatSequenceStepKind.Action),
                "env action bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.EffectSentences[0].Contains(hero.Name, StringComparison.Ordinal),
                $"first effect names hero, got: {tokens.EffectSentences[0]}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.EffectSentences[1].Contains(foe.Name, StringComparison.Ordinal),
                $"second effect names foe, got: {tokens.EffectSentences[1]}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.EffectSentences[1].Contains("the " + foe.Name, StringComparison.OrdinalIgnoreCase)
                    || tokens.EffectSentences[1].StartsWith("The " + foe.Name, StringComparison.Ordinal),
                $"second effect articles the foe, got: {tokens.EffectSentences[1]}", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretDoTTickProseHasNoDigits()
        {
            Console.WriteLine("--- InterpretDoTTick replaces mechanical poison tick with prose ---");
            string prose = CombatSequenceFlavorTokens.InterpretDoTTick(
                "Salamander", "poison", damage: 1, maxHealth: 100, stillActive: true);
            TestBase.AssertTrue(prose.Contains("Salamander", StringComparison.Ordinal),
                $"names victim, got: {prose}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(prose.Contains("poison", StringComparison.OrdinalIgnoreCase)
                    || prose.Contains("Poison", StringComparison.Ordinal),
                $"names poison, got: {prose}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(prose),
                $"no numerals in DoT prose, got: {prose}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!prose.Contains("takes", StringComparison.OrdinalIgnoreCase),
                $"not mechanical takes-line, got: {prose}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!prose.Contains("max HP", StringComparison.OrdinalIgnoreCase),
                $"no % of max HP detail, got: {prose}", ref _run, ref _passed, ref _failed);

            string fade = CombatSequenceFlavorTokens.InterpretDoTTick(
                "Salamander", "poison", damage: 1, maxHealth: 100, stillActive: false);
            TestBase.AssertTrue(fade.Contains("fades", StringComparison.OrdinalIgnoreCase)
                    || fade.Contains("last", StringComparison.OrdinalIgnoreCase),
                $"cleared poison reads as fading, got: {fade}", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretEffectsSoftensMechanicalDoTLines()
        {
            Console.WriteLine("--- InterpretEffects softens takes-damage DoT lines; drops remain detail ---");
            string softened = CombatSequenceFlavorTokens.InterpretEffects(
                new List<string>
                {
                    "Salamander takes 1 poison damage",
                    "     (poison: 1% of max HP)"
                });
            TestBase.AssertTrue(softened.Contains("Salamander", StringComparison.Ordinal),
                $"names victim, got: {softened}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(softened.Contains("poison", StringComparison.OrdinalIgnoreCase),
                $"names poison, got: {softened}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(softened),
                $"no numerals, got: {softened}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!softened.Contains("max HP", StringComparison.OrdinalIgnoreCase),
                $"drops remain detail, got: {softened}", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretTempoFromActionLength()
        {
            Console.WriteLine("--- InterpretTempoHolder picks who goes next from cadence ---");
            TestBase.AssertEqual("Hero",
                CombatSequenceFlavorTokens.InterpretTempoHolder("Hero", "Goblin", 0.5, false),
                "fast action keeps attacker tempo", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Goblin",
                CombatSequenceFlavorTokens.InterpretTempoHolder("Hero", "Goblin", 2.0, false),
                "slow action cedes tempo to foe", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("Goblin",
                CombatSequenceFlavorTokens.InterpretTempoHolder("Hero", "Goblin", 1.0, true),
                "crit miss doubles recovery and cedes tempo", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretStanceSusceptibility()
        {
            Console.WriteLine("--- InterpretStance describes damage susceptibility ---");
            string aggressive = CombatSequenceFlavorTokens.InterpretStance(0.0);
            TestBase.AssertTrue(aggressive.Contains("aggressive", StringComparison.OrdinalIgnoreCase)
                    && (aggressive.Contains("open", StringComparison.OrdinalIgnoreCase)
                        || aggressive.Contains("overextended", StringComparison.OrdinalIgnoreCase)),
                $"aggressive leaves openings, got: {aggressive}", ref _run, ref _passed, ref _failed);

            string defensive = CombatSequenceFlavorTokens.InterpretStance(1.80);
            TestBase.AssertTrue(defensive.Contains("defensive", StringComparison.OrdinalIgnoreCase)
                    && (defensive.Contains("guarded", StringComparison.OrdinalIgnoreCase)
                        || defensive.Contains("harder", StringComparison.OrdinalIgnoreCase)),
                $"defensive resists damage, got: {defensive}", ref _run, ref _passed, ref _failed);

            string neutral = CombatSequenceFlavorTokens.InterpretStance(1.0);
            TestBase.AssertTrue(neutral.Contains("balanced", StringComparison.OrdinalIgnoreCase),
                $"neutral is balanced, got: {neutral}", ref _run, ref _passed, ref _failed);

            var aggressiveHit = HitResult("SLAM", damage: 10);
            aggressiveHit.SelectedAction!.BlockPercent = 0.0;
            var tokens = CombatSequenceFlavorTokens.From(aggressiveHit, DummyHero(), DummyEnemy());
            TestBase.AssertTrue(tokens.IncludeStance, "stance included after hit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.Stance.Contains("aggressive", StringComparison.OrdinalIgnoreCase),
                $"hit keeps action stance, got: {tokens.Stance}", ref _run, ref _passed, ref _failed);

            var miss = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("SLAM", ActionType.Attack),
                Hit = false,
                ModifiedBaseRoll = 3,
                AttackRoll = 3
            };
            miss.SelectedAction!.BlockPercent = 0.0;
            var missTokens = CombatSequenceFlavorTokens.From(miss, DummyHero(), DummyEnemy());
            TestBase.AssertTrue(missTokens.Stance.Contains("balanced", StringComparison.OrdinalIgnoreCase),
                $"miss resets to balanced stance, got: {missTokens.Stance}", ref _run, ref _passed, ref _failed);
        }

        private static void TestEnemyNarrativeNamesUseDefiniteArticle()
        {
            Console.WriteLine("--- Enemy names take definite article in narrative tokens ---");
            var hero = DummyHero();
            var enemy = DummyEnemy();

            TestBase.AssertEqual("SeqHero",
                CombatSequenceFlavorTokens.FormatNarrativeActorName(hero),
                "hero stays a proper noun", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("the SeqFoe",
                CombatSequenceFlavorTokens.FormatNarrativeActorName(enemy),
                "enemy gets lowercase the", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("The SeqFoe shifts stance, ready to strike at SeqHero.",
                CombatSequenceFlavorTokens.CapitalizeLeadingArticle(
                    CombatSequenceFlavorTokens.FormatNarrativeActorName(enemy)
                    + " shifts stance, ready to strike at "
                    + CombatSequenceFlavorTokens.FormatNarrativeActorName(hero)
                    + "."),
                "sentence-initial enemy capitalizes The", ref _run, ref _passed, ref _failed);

            var enemySwing = HitResult("CLAW", damage: 12);
            enemySwing.StatusEffectMessages.Add("SeqHero is poisoned!");
            var enemyTokens = CombatSequenceFlavorTokens.From(enemySwing, enemy, hero);
            TestBase.AssertEqual("the SeqFoe", enemyTokens.Attacker,
                "enemy attacker token", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("SeqHero", enemyTokens.Target,
                "hero target stays bare", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                enemyTokens.Tempo == "the SeqFoe" || enemyTokens.Tempo == "SeqHero",
                $"tempo is articulated combatant name, got: {enemyTokens.Tempo}",
                ref _run, ref _passed, ref _failed);

            var heroSwing = HitResult("SLAM", damage: 20);
            heroSwing.StatusEffectMessages.Add("SeqFoe is bleeding!");
            var heroTokens = CombatSequenceFlavorTokens.From(heroSwing, hero, enemy);
            TestBase.AssertEqual("the SeqFoe", heroTokens.Target,
                "enemy target token", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(heroTokens.Effects.Contains("the SeqFoe", StringComparison.Ordinal),
                $"effects prose articles the enemy, got: {heroTokens.Effects}", ref _run, ref _passed, ref _failed);

            var provider = new NarrativeTextProvider();
            string filled = provider.ReplacePlaceholders(
                "{attacker} shifts stance, ready to strike at {target}.",
                enemyTokens.ToReplacements());
            TestBase.AssertEqual(
                "The SeqFoe shifts stance, ready to strike at SeqHero.",
                CombatSequenceFlavorTokens.CapitalizeLeadingArticle(filled),
                "filled opener reads The enemy…", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretCriticalConsequences()
        {
            Console.WriteLine("--- Critical / critical-miss get dedicated consequence prose ---");
            string crit = CombatSequenceFlavorTokens.InterpretCriticalConsequence("SLASH");
            TestBase.AssertTrue(crit.Contains("precision", StringComparison.OrdinalIgnoreCase)
                    && crit.Contains("SLASH", StringComparison.Ordinal),
                $"crit consequence names precision + attack, got: {crit}", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!crit.Contains("COMBO", StringComparison.OrdinalIgnoreCase),
                "crit consequence never says COMBO", ref _run, ref _passed, ref _failed);

            string critMiss = CombatSequenceFlavorTokens.InterpretCriticalMissConsequence();
            TestBase.AssertTrue(
                (critMiss.Contains("fumble", StringComparison.OrdinalIgnoreCase)
                    || critMiss.Contains("footing", StringComparison.OrdinalIgnoreCase)
                    || critMiss.Contains("regather", StringComparison.OrdinalIgnoreCase)),
                $"crit-miss consequence is a fumble/recovery, got: {critMiss}", ref _run, ref _passed, ref _failed);

            var critResult = HitResult("SLASH", damage: 20);
            critResult.IsCritical = true;
            var critTokens = CombatSequenceFlavorTokens.From(critResult, DummyHero(), DummyEnemy());
            TestBase.AssertTrue(critTokens.IncludeCritical, "include critical beat", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!critTokens.IncludeCriticalMiss, "no crit-miss on crit hit", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(critTokens.Critical.Contains("precision", StringComparison.OrdinalIgnoreCase),
                "critical token filled", ref _run, ref _passed, ref _failed);

            var missResult = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("JAB", ActionType.Attack),
                Hit = false,
                IsCriticalMiss = true,
                ModifiedBaseRoll = 1,
                AttackRoll = 1
            };
            var missTokens = CombatSequenceFlavorTokens.From(missResult, DummyHero(), DummyEnemy());
            TestBase.AssertTrue(missTokens.IncludeCriticalMiss, "include crit-miss beat", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!missTokens.IncludeCritical, "no critical beat on crit miss", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(missTokens.CriticalMiss.Contains("fumble", StringComparison.OrdinalIgnoreCase)
                    || missTokens.CriticalMiss.Contains("footing", StringComparison.OrdinalIgnoreCase),
                "crit-miss token filled", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretiveTokensHaveNoDigits()
        {
            Console.WriteLine("--- Roll / defense / damage tokens leak no digits ---");
            var result = HitResult("SLAM", damage: 12);
            result.AttackRoll = 18;
            result.ModifiedBaseRoll = 18;
            var hero = DummyHero();
            var enemy = DummyEnemy();
            var tokens = CombatSequenceFlavorTokens.From(result, hero, enemy);

            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(tokens.Roll),
                $"roll '{tokens.Roll}' has no digits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IncludeDefense, "defense included vs Character", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(tokens.Defense),
                $"defense '{tokens.Defense}' has no digits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!CombatSequenceFlavorTokens.ContainsDigit(tokens.Damage),
                $"damage '{tokens.Damage}' has no digits", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(
                tokens.Defense.Contains("grazing", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("heavy", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("effective", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("hard-hitting", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("glancing", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("solid", StringComparison.OrdinalIgnoreCase)
                    || tokens.Defense.Contains("whisper", StringComparison.OrdinalIgnoreCase),
                $"defense reads as impact quality, got: {tokens.Defense}",
                ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretRollBands()
        {
            Console.WriteLine("--- InterpretRoll maps faces to poetic bands ---");
            TestBase.AssertEqual("a cursed stumble of fortune", CombatSequenceFlavorTokens.InterpretRoll(1),
                "nat 1", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("thin and shaky fortune", CombatSequenceFlavorTokens.InterpretRoll(4),
                "low", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("steady resolve", CombatSequenceFlavorTokens.InterpretRoll(12),
                "mid", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("keen, decisive fortune", CombatSequenceFlavorTokens.InterpretRoll(18),
                "high", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("near-perfect fortune", CombatSequenceFlavorTokens.InterpretRoll(20),
                "nat 20", ref _run, ref _passed, ref _failed);
        }

        private static void TestInterpretDamageBands()
        {
            Console.WriteLine("--- InterpretDamage scales by max HP share ---");
            TestBase.AssertEqual("a grazing scratch", CombatSequenceFlavorTokens.InterpretDamage(1, 100),
                "tiny", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("a solid wound", CombatSequenceFlavorTokens.InterpretDamage(12, 100),
                "solid share", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("a heavy injury", CombatSequenceFlavorTokens.InterpretDamage(25, 100),
                "heavy share", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("ruinous harm", CombatSequenceFlavorTokens.InterpretDamage(80, 100),
                "ruinous share", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("a light mend", CombatSequenceFlavorTokens.InterpretHeal(2, 100),
                "light heal", ref _run, ref _passed, ref _failed);
        }

        private static void TestTokensMissSkipsDefenseAndDamage()
        {
            Console.WriteLine("--- Miss tokens skip defense and damage ---");
            var result = new ActionExecutionResult
            {
                SelectedAction = TestDataBuilders.CreateMockAction("JAB", ActionType.Attack),
                Hit = false,
                ModifiedBaseRoll = 4,
                AttackRoll = 4
            };
            var tokens = CombatSequenceFlavorTokens.From(result, DummyHero(), DummyEnemy());
            TestBase.AssertEqual("a miss", tokens.Outcome, "miss outcome", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(tokens.IsMiss, "IsMiss on miss swing", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual("sequenceActionMiss", tokens.ResolveBankKey(CombatSequenceStepKind.Action),
                "miss action beat uses sequenceActionMiss bank", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.IncludeDefense, "no defense on miss", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.IncludeDamage, "no damage on miss", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!tokens.IncludeEffects, "no effects on miss", ref _run, ref _passed, ref _failed);
        }

        private static void TestPlaceholderFill()
        {
            Console.WriteLine("--- Placeholder fill replaces tokens ---");
            var provider = new NarrativeTextProvider();
            var tokens = new CombatSequenceFlavorTokens
            {
                Attacker = "Edric",
                Target = "the Goblin",
                Outcome = "a chaining SLAM",
                Roll = "keen, decisive fortune",
                Action = "SLAM",
                Defense = "hard-hitting and effective",
                Damage = "a solid wound",
                Effects = "poison threading through the Goblin",
                Tempo = "Edric",
                Stance = "an aggressive stance, overextended and open to the next blow",
                Critical = "extra precision in SLAM, finding the seam where flesh yields",
                IncludeDefense = true,
                IncludeDamage = true,
                IncludeEffects = true,
                IncludeTempo = true,
                IncludeStance = true,
                IncludeCritical = true
            };
            string filled = provider.ReplacePlaceholders(
                "{attacker} finds {roll} into {action} leaving {damage} ({outcome}); {stance}; tempo {tempo}; {critical}",
                tokens.ToReplacements());
            TestBase.AssertEqual(
                "Edric finds keen, decisive fortune into SLAM leaving a solid wound (a chaining SLAM); an aggressive stance, overextended and open to the next blow; tempo Edric; extra precision in SLAM, finding the seam where flesh yields",
                filled,
                "filled template", ref _run, ref _passed, ref _failed);

            string enemyOpener = provider.ReplacePlaceholders(
                "{attacker} shifts stance, ready to strike at {target}.",
                new CombatSequenceFlavorTokens { Attacker = "the Wraith", Target = "Edric" }.ToReplacements());
            TestBase.AssertEqual(
                "The Wraith shifts stance, ready to strike at Edric.",
                CombatSequenceFlavorTokens.CapitalizeLeadingArticle(enemyOpener),
                "enemy opener capitalizes The", ref _run, ref _passed, ref _failed);
        }

        private static void TestNarrativeBeatOrder()
        {
            Console.WriteLine("--- Narrative order places crit after action; stance before tempo ---");
            var order = CombatSequenceFlavorTokens.NarrativeBeatOrder;
            TestBase.AssertTrue(order[0] == CombatSequenceStepKind.Attacker, "1 attacker", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[1] == CombatSequenceStepKind.Outcome, "2 outcome", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[2] == CombatSequenceStepKind.Roll, "3 roll", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[3] == CombatSequenceStepKind.Action, "4 action", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[4] == CombatSequenceStepKind.Critical, "5 critical consequence", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[5] == CombatSequenceStepKind.CriticalMiss, "6 critical-miss consequence", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[^2] == CombatSequenceStepKind.Stance, "stance near end", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(order[^1] == CombatSequenceStepKind.Tempo, "tempo closes the paragraph", ref _run, ref _passed, ref _failed);
        }

        private static void TestShouldPlayRequiresNarrativeMode()
        {
            Console.WriteLine("--- ShouldPlay requires F7 narrative mode ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(false);
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                TestBase.AssertTrue(!CombatSequenceFlavorPresenter.ShouldPlay(),
                    "off when narrative mode off", ref _run, ref _passed, ref _failed);

                DeveloperModeState.SetNarrativeCombatLog(true);
                TestBase.AssertTrue(CombatSequenceFlavorPresenter.ShouldPlay(),
                    "on when narrative mode on", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequenceFlavorPresenter.ResetForTests();
            }
        }

        private static void TestHudShouldPlayFalseWhenNarrativeOn()
        {
            Console.WriteLine("--- HUD ShouldPlay is false when narrative mode is on ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(true);
                CombatSequencePresenter.BypassCanvasCheckForTests = true;
                TestBase.AssertTrue(!CombatSequencePresenter.ShouldPlay(),
                    "HUD skipped in narrative mode", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequencePresenter.ResetForTests();
            }
        }

        private static void TestNarrativeEmphasisColorsKeywords()
        {
            Console.WriteLine("--- Narrative emphasis colors identity tokens and keywords ---");
            var tokens = new CombatSequenceFlavorTokens
            {
                Attacker = "SeqHero",
                Target = "SeqFoe",
                Outcome = "a chaining SLAM",
                Roll = "keen, decisive fortune",
                Action = "SLAM",
                Defense = "heavy and barely checked",
                Damage = "a solid wound",
                Effects = "Poison spreads",
                Tempo = "SeqHero"
            };

            var colored = CombatSequenceNarrativeEmphasis.ColorizeSentence(
                "SeqHero answers with SLAM under a chaining SLAM as keen, decisive fortune leaving a solid wound.",
                tokens);
            string plain = ColoredTextRenderer.RenderAsPlainText(colored);
            TestBase.AssertTrue(plain.Contains("SeqHero", StringComparison.Ordinal),
                "plain keeps attacker", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(colored.Exists(s =>
                    s.Text.Contains("SeqHero", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Gold.GetColor()),
                "attacker is gold", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(colored.Exists(s =>
                    s.Text.Contains("SLAM", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Success.GetColor()),
                "action is success green", ref _run, ref _passed, ref _failed);

            // Interpretive clauses are not painted as contiguous non-white spans.
            // (White ambient words may merge into one segment after sparse keyword paint.)
            TestBase.AssertTrue(!colored.Exists(s =>
                    s.Text.Contains("keen, decisive fortune", StringComparison.OrdinalIgnoreCase)
                    && s.Color != Colors.White),
                "roll phrase is not one contiguous non-white span", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!colored.Exists(s =>
                    s.Text.Contains("a chaining SLAM", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Gold.GetColor()),
                "outcome clause is not painted gold as a chunk", ref _run, ref _passed, ref _failed);

            // Atmospheric prose stays white; only severity/status keywords paint.
            TestBase.AssertTrue(!colored.Exists(s =>
                    s.Text.Contains("fortune", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Cyan.GetColor()),
                "fortune stays uncolored (sparse narrative keywords)", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!colored.Exists(s =>
                    s.Text.Contains("solid", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Cyan.GetColor()),
                "solid stays uncolored", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!colored.Exists(s =>
                    string.Equals(s.Text.TrimEnd('.', ',', '!', '?'), "as", StringComparison.OrdinalIgnoreCase)
                    && s.Color != Colors.White),
                "glue word 'as' is not keyword-colored", ref _run, ref _passed, ref _failed);
            var damageKeywordColor = KeywordGroupManager.GetKeywordGroup("damage")?.Color
                ?? ColorPalette.Damage.GetColor();
            TestBase.AssertTrue(colored.Exists(s =>
                    s.Text.Contains("wound", StringComparison.OrdinalIgnoreCase)
                    && s.Color == damageKeywordColor),
                "wound keyword is damage red", ref _run, ref _passed, ref _failed);
        }

        private static void TestNarrativeEmphasisLeavesEnemyArticleUncolored()
        {
            Console.WriteLine("--- Enemy article stays white; only the name is enemy-colored ---");
            var tokens = new CombatSequenceFlavorTokens
            {
                Attacker = "Stormcaller",
                Target = "the Orc",
                Action = "slash",
                Tempo = "Stormcaller"
            };

            var enemyColor = ColorPalette.Enemy.GetColor();

            var midSentence = CombatSequenceNarrativeEmphasis.ColorizeSentence(
                "Fate settles on the Orc.", tokens);
            TestBase.AssertTrue(midSentence.Exists(s =>
                    string.Equals(s.Text, "Orc", StringComparison.Ordinal)
                    && s.Color == enemyColor),
                "bare enemy name is enemy-colored", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!midSentence.Exists(s =>
                    s.Text.Contains("the", StringComparison.OrdinalIgnoreCase)
                    && s.Text.Contains("Orc", StringComparison.OrdinalIgnoreCase)
                    && s.Color == enemyColor),
                "article+name is not one enemy-colored span", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!midSentence.Exists(s =>
                    string.Equals(s.Text.Trim(), "the", StringComparison.OrdinalIgnoreCase)
                    && s.Color == enemyColor),
                "leading article stays uncolored", ref _run, ref _passed, ref _failed);

            var sentenceStart = CombatSequenceNarrativeEmphasis.ColorizeSentence(
                "The Orc staggers back.", tokens);
            TestBase.AssertTrue(sentenceStart.Exists(s =>
                    string.Equals(s.Text, "Orc", StringComparison.Ordinal)
                    && s.Color == enemyColor),
                "sentence-initial enemy name is enemy-colored", ref _run, ref _passed, ref _failed);
            TestBase.AssertTrue(!sentenceStart.Exists(s =>
                    string.Equals(s.Text.Trim(), "The", StringComparison.Ordinal)
                    && s.Color == enemyColor),
                "sentence-initial The stays uncolored", ref _run, ref _passed, ref _failed);
        }

        private static void TestPlayWritesProseParagraphWithoutHud()
        {
            Console.WriteLine("--- Play grows one prose paragraph; HUD PlayedThisBlock stays false ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(true);
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                CombatSequenceFlavorPresenter.SkipDelaysForTests = true;
                CombatSequenceFlavorPresenter.RecordForTests = true;

                var result = HitResult("SLAM", damage: 72);
                result.IsCombo = true;
                result.StatusEffectMessages.Add("Poison spreads.");
                var hero = DummyHero();
                var foe = DummyEnemy();
                var steps = CombatSequenceBuilder.From(result, hero, foe);
                var tokens = CombatSequenceFlavorTokens.From(result, hero, foe);
                CombatSequenceFlavorPresenter.SetPending(steps, tokens);

                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();

                TestBase.AssertTrue(CombatSequenceFlavorPresenter.PlayedThisBlock,
                    "flavor presenter played", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!CombatSequencePresenter.PlayedThisBlock,
                    "HUD did not play", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceHudState.HasVisibleSequence,
                    "sequence bar shows finished swing", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceFlavorPresenter.LinesWrittenForTests.Count >= 4,
                    "at least four progressive paragraph updates", ref _run, ref _passed, ref _failed);

                string final = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(final.StartsWith(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, StringComparison.Ordinal),
                    $"new paragraph starts with book indent, got: '{final.Substring(0, Math.Min(8, final.Length))}'",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("SeqHero", StringComparison.Ordinal),
                    $"paragraph names attacker, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("SLAM", StringComparison.Ordinal),
                    $"paragraph keeps the attack name, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!final.Contains("COMBO", StringComparison.OrdinalIgnoreCase),
                    $"paragraph never says COMBO, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains(". ", StringComparison.Ordinal),
                    $"paragraph joins sentences with spaces, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!final.Contains("||", StringComparison.Ordinal),
                    "single prose paragraph (not stanza separators)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    final.Contains("next", StringComparison.OrdinalIgnoreCase)
                        || final.Contains("tempo", StringComparison.OrdinalIgnoreCase)
                        || final.Contains("pace", StringComparison.OrdinalIgnoreCase)
                        || final.Contains("beat", StringComparison.OrdinalIgnoreCase)
                        || final.Contains("Initiative", StringComparison.OrdinalIgnoreCase)
                        || final.Contains("ready to move", StringComparison.OrdinalIgnoreCase),
                    $"paragraph establishes tempo, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("stance", StringComparison.OrdinalIgnoreCase),
                    $"paragraph establishes stance, got: {final}", ref _run, ref _passed, ref _failed);

                // Miss: fewer progressive updates; final is still one paragraph (includes stance + tempo).
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                CombatSequenceFlavorPresenter.SkipDelaysForTests = true;
                CombatSequenceFlavorPresenter.RecordForTests = true;
                DeveloperModeState.SetNarrativeCombatLog(true);

                var miss = new ActionExecutionResult
                {
                    SelectedAction = TestDataBuilders.CreateMockAction("JAB", ActionType.Attack),
                    Hit = false,
                    ModifiedBaseRoll = 3,
                    AttackRoll = 3
                };
                var missSteps = CombatSequenceBuilder.From(miss, hero, foe);
                var missTokens = CombatSequenceFlavorTokens.From(miss, hero, foe);
                CombatSequenceFlavorPresenter.SetPending(missSteps, missTokens);
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();
                TestBase.AssertEqual(6, CombatSequenceFlavorPresenter.LinesWrittenForTests.Count,
                    "miss grows paragraph across 4 core + stance + tempo", ref _run, ref _passed, ref _failed);
                string missFinal = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(
                    missFinal.Contains("teeth", StringComparison.OrdinalIgnoreCase)
                        || missFinal.Contains("jaws", StringComparison.OrdinalIgnoreCase)
                        || missFinal.Contains("empty space", StringComparison.OrdinalIgnoreCase)
                        || missFinal.Contains("nowhere to be found", StringComparison.OrdinalIgnoreCase)
                        || missFinal.Contains("misses clean", StringComparison.OrdinalIgnoreCase)
                        || missFinal.Contains("lunges", StringComparison.OrdinalIgnoreCase),
                    $"miss paragraph uses miss-action prose, got: {missFinal}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!missFinal.Contains("SeqHero answers with", StringComparison.OrdinalIgnoreCase),
                    $"miss paragraph does not use hit-style answers-with action line, got: {missFinal}",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(missFinal.Contains("stance", StringComparison.OrdinalIgnoreCase),
                    $"miss paragraph includes stance, got: {missFinal}", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
            }
        }

        private static void TestPlayEnvironmentalWritesOwnProseParagraph()
        {
            Console.WriteLine("--- Environmental hazard plays as its own prose paragraph ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(true);
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                CombatSequenceFlavorPresenter.SkipDelaysForTests = true;
                CombatSequenceFlavorPresenter.RecordForTests = true;

                var room = new Environment("Coral Garden", "reef", false, "forest", "");
                var action = TestDataBuilders.CreateMockAction("Necrotic Aura", ActionType.Debuff);
                action.CausesWeaken = true;
                var hero = DummyHero();
                var foe = DummyEnemy();
                var statusEffects = new List<List<ColoredText>>
                {
                    new List<ColoredText>
                    {
                        new ColoredText(hero.Name, ColorPalette.Gold.GetColor()),
                        new ColoredText(" affected by WEAKEN for 1 turn", Colors.White)
                    },
                    new List<ColoredText>
                    {
                        new ColoredText(foe.Name, ColorPalette.Enemy.GetColor()),
                        new ColoredText(" affected by WEAKEN for 1 turn", Colors.White)
                    }
                };

                var steps = CombatSequenceBuilder.FromEnvironmental(
                    "Necrotic Aura", 0, null, hero, hasStatusEffects: true);
                var tokens = CombatSequenceFlavorTokens.FromEnvironmental(
                    room, action, statusEffects, damage: 0, primaryTarget: hero,
                    affectedActors: new List<Actor> { hero, foe });
                CombatSequenceFlavorPresenter.SetPending(steps, tokens);
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();

                TestBase.AssertTrue(CombatSequenceFlavorPresenter.PlayedThisBlock,
                    "env flavor played", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceFlavorPresenter.LinesWrittenForTests.Count >= 3,
                    "env grows at least action + two status sentences", ref _run, ref _passed, ref _failed);

                string final = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(final.StartsWith(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, StringComparison.Ordinal),
                    $"env paragraph indented, got: '{final.Substring(0, Math.Min(8, final.Length))}'",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("Coral Garden", StringComparison.Ordinal),
                    $"names environment, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("Necrotic Aura", StringComparison.Ordinal),
                    $"names hazard action, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains(hero.Name, StringComparison.Ordinal),
                    $"names hero weaken, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains(foe.Name, StringComparison.Ordinal),
                    $"names foe weaken, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(final.Contains("weaken", StringComparison.OrdinalIgnoreCase),
                    $"mentions weaken in prose, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!final.Contains("affected by", StringComparison.OrdinalIgnoreCase),
                    $"not mechanical affected-by, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!final.Contains("uses ", StringComparison.OrdinalIgnoreCase),
                    $"not mechanical uses-line, got: {final}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!final.Contains("stance", StringComparison.OrdinalIgnoreCase),
                    $"env skips stance, got: {final}", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
            }
        }

        private static void TestSameAttackerContinuesParagraphWithFollowUp()
        {
            Console.WriteLine("--- Same attacker continues open paragraph with follow-up bridge ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(true);
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                TextSpacingSystem.Reset();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                CombatSequenceFlavorPresenter.SkipDelaysForTests = true;
                CombatSequenceFlavorPresenter.RecordForTests = true;

                var hero = DummyHero();
                var foe = DummyEnemy();

                var first = HitResult("SLAM", damage: 40);
                CombatSequenceFlavorPresenter.SetPending(
                    CombatSequenceBuilder.From(first, hero, foe),
                    CombatSequenceFlavorTokens.From(first, hero, foe));
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();
                string afterFirst = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(afterFirst.StartsWith(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, StringComparison.Ordinal),
                    "first swing paragraph is indented", ref _run, ref _passed, ref _failed);

                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.CombatAction, "SeqHero");
                CombatSequenceFlavorPresenter.LinesWrittenForTests.Clear();

                var second = HitResult("SLASH", damage: 30);
                CombatSequenceFlavorPresenter.SetPending(
                    CombatSequenceBuilder.From(second, hero, foe),
                    CombatSequenceFlavorTokens.From(second, hero, foe));
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();

                TestBase.AssertTrue(CombatSequenceFlavorPresenter.LinesWrittenForTests.Count >= 2,
                    "follow-up swing grows the open paragraph", ref _run, ref _passed, ref _failed);
                string continued = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(continued.StartsWith(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, StringComparison.Ordinal),
                    "continued paragraph keeps original indent (not a new paragraph)", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(continued.Contains("SLAM", StringComparison.Ordinal),
                    $"continued paragraph still has first swing action, got: {continued}",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(continued.Contains("SLASH", StringComparison.Ordinal),
                    $"continued paragraph includes second swing action, got: {continued}",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    continued.Contains("again", StringComparison.OrdinalIgnoreCase)
                        || continued.Contains("another", StringComparison.OrdinalIgnoreCase)
                        || continued.Contains("second", StringComparison.OrdinalIgnoreCase)
                        || continued.Contains("Without pause", StringComparison.Ordinal)
                        || continued.Contains("does not relent", StringComparison.OrdinalIgnoreCase),
                    $"continued paragraph includes follow-up bridge prose, got: {continued}",
                    ref _run, ref _passed, ref _failed);

                // Intervening DoT forces a fresh paragraph on the next swing.
                TextSpacingSystem.RecordBlockDisplayed(TextSpacingSystem.BlockType.PoisonDamage, "SeqHero");
                CombatSequenceFlavorPresenter.LinesWrittenForTests.Clear();
                var third = HitResult("POUND", damage: 20);
                CombatSequenceFlavorPresenter.SetPending(
                    CombatSequenceBuilder.From(third, hero, foe),
                    CombatSequenceFlavorTokens.From(third, hero, foe));
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();
                string afterDot = CombatSequenceFlavorPresenter.LinesWrittenForTests[^1];
                TestBase.AssertTrue(afterDot.StartsWith(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, StringComparison.Ordinal),
                    "after DoT, new paragraph is indented", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!afterDot.Contains("SLASH", StringComparison.Ordinal),
                    "after DoT, new paragraph does not keep prior swing text",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(afterDot.Contains("POUND", StringComparison.Ordinal),
                    $"after DoT paragraph names new action, got: {afterDot}",
                    ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                TextSpacingSystem.Reset();
            }
        }

        private static void TestNarrativeRevealsHudColumnsInBeatOrder()
        {
            Console.WriteLine("--- F7 reveals HUD columns with matching prose beats (Outcome before Roll) ---");
            bool prevNarrative = DeveloperModeState.IsNarrativeCombatLog;
            bool prevInstant = DeveloperModeState.IsCombatLogInstant;
            bool prevMute = CombatUiMuteScope.GlobalMute;
            try
            {
                CombatUiMuteScope.GlobalMute = false;
                DeveloperModeState.SetCombatLogInstant(false);
                DeveloperModeState.SetNarrativeCombatLog(true);
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequenceFlavorPresenter.BypassCanvasCheckForTests = true;
                CombatSequenceFlavorPresenter.SkipDelaysForTests = true;
                CombatSequenceFlavorPresenter.RecordForTests = true;
                CombatSequencePresenter.RecordCuesForTests = true;

                // Selective reveal: Outcome can light before Roll even though Roll is left of Outcome.
                var sample = new List<CombatSequenceStep>
                {
                    new CombatSequenceStep(CombatSequenceStepKind.Attacker, "ATTACKER",
                        new List<ColoredText> { new ColoredText("Hero", Colors.White) }),
                    new CombatSequenceStep(CombatSequenceStepKind.Roll, "ROLL",
                        new List<ColoredText> { new ColoredText("17", Colors.White) }),
                    new CombatSequenceStep(CombatSequenceStepKind.Outcome, "OUTCOME",
                        new List<ColoredText> { new ColoredText("HIT", Colors.White) })
                };
                CombatSequenceHudState.BeginSelective(sample);
                TestBase.AssertTrue(
                    CombatSequenceHudState.GetStepPhase(1) == CombatSequenceHudLayout.Phase.Pending,
                    "Roll pending before any reveal",
                    ref _run, ref _passed, ref _failed);
                CombatSequenceHudState.RevealStepKind(CombatSequenceStepKind.Outcome);
                TestBase.AssertTrue(
                    CombatSequenceHudState.GetStepPhase(2) == CombatSequenceHudLayout.Phase.Active,
                    "Outcome active after its reveal",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(
                    CombatSequenceHudState.GetStepPhase(1) == CombatSequenceHudLayout.Phase.Pending,
                    "Roll still pending when Outcome revealed first",
                    ref _run, ref _passed, ref _failed);

                CombatSequenceHudState.ResetForTests();
                CombatSequenceHudState.IsBandReserved = true;
                CombatSequencePresenter.NarrativeColumnsRevealedForTests.Clear();

                var result = HitResult("SLAM", damage: 72);
                result.IsCombo = true;
                result.StatusEffectMessages.Add("Poison spreads.");
                var hero = DummyHero();
                var foe = DummyEnemy();
                CombatSequenceFlavorPresenter.SetPending(
                    CombatSequenceBuilder.From(result, hero, foe),
                    CombatSequenceFlavorTokens.From(result, hero, foe));
                CombatSequenceFlavorPresenter.PlayPendingAsync(hero).GetAwaiter().GetResult();

                var revealed = CombatSequencePresenter.NarrativeColumnsRevealedForTests;
                TestBase.AssertTrue(revealed.Count >= 4,
                    $"expected core HUD reveals, got {revealed.Count}", ref _run, ref _passed, ref _failed);
                int attackerAt = revealed.IndexOf(CombatSequenceStepKind.Attacker);
                int outcomeAt = revealed.IndexOf(CombatSequenceStepKind.Outcome);
                int rollAt = revealed.IndexOf(CombatSequenceStepKind.Roll);
                int actionAt = revealed.IndexOf(CombatSequenceStepKind.Action);
                TestBase.AssertTrue(attackerAt >= 0 && outcomeAt > attackerAt && rollAt > outcomeAt && actionAt > rollAt,
                    $"HUD reveal order follows narrative beats (Attacker→Outcome→Roll→Action), got: {string.Join(",", revealed)}",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(!revealed.Contains(CombatSequenceStepKind.Stance)
                        && !revealed.Contains(CombatSequenceStepKind.Tempo)
                        && !revealed.Contains(CombatSequenceStepKind.Critical)
                        && !revealed.Contains(CombatSequenceStepKind.CriticalMiss),
                    "narrative-only beats do not reveal HUD columns",
                    ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(CombatSequenceHudState.HasVisibleSequence,
                    "finished swing stays on the sequence bar", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(
                    CombatSequenceHudState.Steps.Count,
                    CombatSequenceHudState.CurrentIndex,
                    "bar finishes all columns after prose",
                    ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prevNarrative);
                DeveloperModeState.SetCombatLogInstant(prevInstant);
                CombatUiMuteScope.GlobalMute = prevMute;
                CombatSequenceFlavorPresenter.ResetForTests();
                CombatSequencePresenter.ResetForTests();
                CombatSequenceHudState.ResetForTests();
            }
        }

        private static void TestReserveBandHiddenWhenNarrativeOn()
        {
            Console.WriteLine("--- Sequence HUD band stays reserved when narrative log is on ---");
            bool prev = DeveloperModeState.IsNarrativeCombatLog;
            try
            {
                DeveloperModeState.SetNarrativeCombatLog(false);
                TestBase.AssertTrue(CombatSequenceHudState.ShouldReserveBand(GameState.Combat),
                    "combat reserves HUD when narrative off", ref _run, ref _passed, ref _failed);

                DeveloperModeState.SetNarrativeCombatLog(true);
                TestBase.AssertTrue(CombatSequenceHudState.ShouldReserveBand(GameState.Combat),
                    "combat still reserves HUD when narrative on", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                DeveloperModeState.SetNarrativeCombatLog(prev);
            }
        }

        private static void TestNarrativeTypewriterTruncatePreservesColors()
        {
            Console.WriteLine("--- Typewriter Truncate prefixes grow one character and keep colors ---");
            var tokens = CombatSequenceFlavorTokens.From(HitResult("SLAM", damage: 40), DummyHero(), DummyEnemy());
            var paragraph = new List<ColoredText>
            {
                new ColoredText(CombatSequenceFlavorPresenter.NarrativeParagraphIndent, Avalonia.Media.Colors.White)
            };
            CombatSequenceNarrativeEmphasis.AppendSentence(paragraph, "SeqHero strikes with SLAM.", tokens);

            string full = ColoredTextRenderer.RenderAsPlainText(paragraph);
            TestBase.AssertTrue(full.Length > CombatSequenceFlavorPresenter.NarrativeParagraphIndent.Length + 5,
                "paragraph has content after indent", ref _run, ref _passed, ref _failed);

            string previous = "";
            for (int len = 1; len <= full.Length; len++)
            {
                var slice = ColoredTextRenderer.Truncate(paragraph, len);
                string plain = ColoredTextRenderer.RenderAsPlainText(slice);
                TestBase.AssertEqual(len, plain.Length,
                    $"truncate length {len}", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(full.StartsWith(plain, StringComparison.Ordinal),
                    $"prefix at {len} matches full text", ref _run, ref _passed, ref _failed);
                if (len > 1)
                {
                    TestBase.AssertTrue(plain.StartsWith(previous, StringComparison.Ordinal),
                        "each step extends previous prefix", ref _run, ref _passed, ref _failed);
                }
                previous = plain;
            }

            TestBase.AssertTrue(paragraph.Exists(s =>
                    s.Text.Contains("SLAM", StringComparison.OrdinalIgnoreCase)
                    && s.Color == ColorPalette.Success.GetColor()),
                "action name keeps success color in full paragraph", ref _run, ref _passed, ref _failed);
        }

        private static ActionExecutionResult HitResult(string name, int damage)
        {
            var action = TestDataBuilders.CreateMockAction(name, ActionType.Attack);
            return new ActionExecutionResult
            {
                SelectedAction = action,
                Hit = true,
                Damage = damage,
                ModifiedBaseRoll = 17,
                AttackRoll = 17,
                ResolvedMultiHitCount = 1
            };
        }

        private static Character DummyHero() => TestDataBuilders.CreateTestCharacter("SeqHero", 1);

        private static Enemy DummyEnemy() => TestDataBuilders.Enemy().WithName("SeqFoe").Build();
    }
}
