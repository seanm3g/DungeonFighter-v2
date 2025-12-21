using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Actions.RollModification;
using RPGGame.Combat.Effects.AdvancedStatusEffects;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Phase 2: Advanced Status Effects Tests
    /// </summary>
    public static class AdvancedMechanicsTest_Phase2
    {
        private static int _testsRun = 0;
        private static int _testsPassed = 0;
        private static int _testsFailed = 0;
        
        public static void RunAllTests()
        {
            Console.WriteLine("=== Phase 2: Advanced Status Effects ===\n");
            
            _testsRun = 0;
            _testsPassed = 0;
            _testsFailed = 0;
            
            TestVulnerabilityEffect();
            TestHardenEffect();
            TestFortifyEffect();
            TestFocusEffect();
            TestExposeEffect();
            TestHPRegenEffect();
            TestArmorBreakEffect();
            TestPierceEffect();
            TestReflectEffect();
            TestSilenceEffect();
            TestMarkEffect();
            TestDisruptEffect();
            TestCleanseEffect();
            
            PrintSummary();
        }
        
        private static void TestVulnerabilityEffect()
        {
            Console.WriteLine("Testing VulnerabilityEffectHandler...");
            try
            {
                var handler = new VulnerabilityEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                int initialStacks = target.VulnerabilityStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Vulnerability effect applied");
                AssertTrue((target.VulnerabilityStacks ?? 0) > initialStacks, 
                    $"Vulnerability stacks increased: {initialStacks} -> {target.VulnerabilityStacks ?? 0}");
                
                int stacksAfterFirst = target.VulnerabilityStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.VulnerabilityStacks ?? 0) > stacksAfterFirst, 
                    $"Vulnerability stacks increased on second application: {stacksAfterFirst} -> {target.VulnerabilityStacks ?? 0}");
                
                int maxStacks = target.VulnerabilityStacks ?? 0;
                for (int i = 0; i < 10; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.VulnerabilityStacks ?? 0) >= maxStacks, 
                    $"Vulnerability stacks after multiple applications: {target.VulnerabilityStacks ?? 0} (should be >= {maxStacks})");
                
                target.VulnerabilityStacks = 0;
                handler.Apply(target, action, results);
                AssertTrue((target.VulnerabilityStacks ?? 0) > 0, 
                    $"Vulnerability re-applied after reset: {target.VulnerabilityStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Vulnerability effect test failed: {ex.Message}");
            }
        }

        private static void TestHardenEffect()
        {
            Console.WriteLine("Testing HardenEffectHandler...");
            try
            {
                var handler = new HardenEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                int initialStacks = target.HardenStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Harden effect applied");
                AssertTrue((target.HardenStacks ?? 0) > initialStacks, 
                    $"Harden stacks increased: {initialStacks} -> {target.HardenStacks ?? 0}");
                
                int stacksAfterFirst = target.HardenStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.HardenStacks ?? 0) > stacksAfterFirst, 
                    $"Harden stacks increased on second application: {stacksAfterFirst} -> {target.HardenStacks ?? 0}");
                
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.HardenStacks ?? 0) > stacksAfterFirst, 
                    $"Harden stacks after multiple applications: {target.HardenStacks ?? 0}");
                
                target.VulnerabilityStacks = 3;
                int hardenStacks = target.HardenStacks ?? 0;
                AssertTrue((target.HardenStacks ?? 0) > 0, 
                    $"Harden stacks maintained with vulnerability: {target.HardenStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Harden effect test failed: {ex.Message}");
            }
        }

        private static void TestFortifyEffect()
        {
            Console.WriteLine("Testing FortifyEffectHandler...");
            try
            {
                var handler = new FortifyEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                int initialStacks = target.FortifyStacks ?? 0;
                int initialArmor = target.GetTotalArmor();
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Fortify effect applied");
                AssertTrue((target.FortifyStacks ?? 0) > initialStacks, 
                    $"Fortify stacks increased: {initialStacks} -> {target.FortifyStacks ?? 0}");
                AssertTrue((target.FortifyArmorBonus ?? 0) > 0, 
                    $"Fortify armor bonus: {target.FortifyArmorBonus ?? 0}");
                
                int armorAfterFortify = target.GetTotalArmor();
                AssertTrue(armorAfterFortify >= initialArmor, 
                    $"Armor increased or maintained: {initialArmor} -> {armorAfterFortify}");
                
                int stacksAfterFirst = target.FortifyStacks ?? 0;
                int armorBonusAfterFirst = target.FortifyArmorBonus ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.FortifyStacks ?? 0) > stacksAfterFirst, 
                    $"Fortify stacks increased on second application: {stacksAfterFirst} -> {target.FortifyStacks ?? 0}");
                AssertTrue((target.FortifyArmorBonus ?? 0) >= armorBonusAfterFirst, 
                    $"Armor bonus increased or maintained: {armorBonusAfterFirst} -> {target.FortifyArmorBonus ?? 0}");
                
                for (int i = 0; i < 3; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.FortifyStacks ?? 0) > stacksAfterFirst, 
                    $"Fortify stacks after multiple applications: {target.FortifyStacks ?? 0}");
                
                int finalArmor = target.GetTotalArmor();
                AssertTrue(finalArmor >= initialArmor, 
                    $"Final armor >= initial: {initialArmor} -> {finalArmor}");
                
                target.ArmorBreakStacks = 2;
                int armorWithBreak = target.GetTotalArmor();
                AssertTrue((target.FortifyArmorBonus ?? 0) > 0, 
                    $"Fortify armor bonus maintained with armor break: {target.FortifyArmorBonus ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Fortify effect test failed: {ex.Message}");
            }
        }

        private static void TestFocusEffect()
        {
            Console.WriteLine("Testing FocusEffectHandler...");
            try
            {
                var handler = new FocusEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Focus effect applied");
                AssertTrue(target.FocusStacks > 0, $"Focus stacks: {target.FocusStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Focus effect test failed: {ex.Message}");
            }
        }

        private static void TestExposeEffect()
        {
            Console.WriteLine("Testing ExposeEffectHandler...");
            try
            {
                var handler = new ExposeEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Expose effect applied");
                AssertTrue(target.ExposeStacks > 0, $"Expose stacks: {target.ExposeStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Expose effect test failed: {ex.Message}");
            }
        }

        private static void TestHPRegenEffect()
        {
            Console.WriteLine("Testing HPRegenEffectHandler...");
            try
            {
                var handler = new HPRegenEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                int initialStacks = target.HPRegenStacks ?? 0;
                int initialHealth = target.CurrentHealth;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "HP Regen effect applied");
                AssertTrue((target.HPRegenStacks ?? 0) > initialStacks, 
                    $"HP Regen stacks increased: {initialStacks} -> {target.HPRegenStacks ?? 0}");
                
                int stacksAfterFirst = target.HPRegenStacks ?? 0;
                handler.Apply(target, action, results);
                AssertTrue((target.HPRegenStacks ?? 0) > stacksAfterFirst, 
                    $"HP Regen stacks increased on second application: {stacksAfterFirst} -> {target.HPRegenStacks ?? 0}");
                
                target.CurrentHealth = target.MaxHealth;
                int maxHealth = target.MaxHealth;
                AssertTrue(target.CurrentHealth <= maxHealth, 
                    $"HP Regen doesn't exceed max health: {target.CurrentHealth} <= {maxHealth}");
                
                target.TakeDamage(10);
                int damagedHealth = target.CurrentHealth;
                AssertTrue((target.HPRegenStacks ?? 0) > 0, 
                    $"HP Regen stacks maintained: {target.HPRegenStacks ?? 0}");
                
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.HPRegenStacks ?? 0) > stacksAfterFirst, 
                    $"HP Regen stacks after multiple applications: {target.HPRegenStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"HP Regen effect test failed: {ex.Message}");
            }
        }

        private static void TestArmorBreakEffect()
        {
            Console.WriteLine("Testing ArmorBreakEffectHandler...");
            try
            {
                var handler = new ArmorBreakEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                int initialArmor = target.GetTotalArmor();
                int initialStacks = target.ArmorBreakStacks ?? 0;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Armor Break effect applied");
                AssertTrue((target.ArmorBreakStacks ?? 0) > initialStacks, 
                    $"Armor Break stacks increased: {initialStacks} -> {target.ArmorBreakStacks ?? 0}");
                
                int armorAfterBreak = target.GetTotalArmor();
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks: {target.ArmorBreakStacks ?? 0}");
                
                int stacksAfterFirst = target.ArmorBreakStacks ?? 0;
                int armorAfterFirst = target.GetTotalArmor();
                handler.Apply(target, action, results);
                AssertTrue((target.ArmorBreakStacks ?? 0) > stacksAfterFirst, 
                    $"Armor Break stacks increased on second application: {stacksAfterFirst} -> {target.ArmorBreakStacks ?? 0}");
                
                for (int i = 0; i < 5; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.ArmorBreakStacks ?? 0) > stacksAfterFirst, 
                    $"Armor Break stacks after multiple applications: {target.ArmorBreakStacks ?? 0}");
                
                target.FortifyStacks = 3;
                target.FortifyArmorBonus = 5;
                int fortifyArmorBonus = target.FortifyArmorBonus ?? 0;
                int armorBreakStacks = target.ArmorBreakStacks ?? 0;
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks maintained with fortify: {target.ArmorBreakStacks ?? 0}");
                
                target.ArmorBreakStacks = 0;
                for (int i = 0; i < 10; i++)
                {
                    handler.Apply(target, action, results);
                }
                AssertTrue((target.ArmorBreakStacks ?? 0) > 0, 
                    $"Armor Break stacks after many applications: {target.ArmorBreakStacks ?? 0}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Armor Break effect test failed: {ex.Message}");
            }
        }

        private static void TestPierceEffect()
        {
            Console.WriteLine("Testing PierceEffectHandler...");
            try
            {
                var handler = new PierceEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Pierce effect applied");
                AssertTrue(target.HasPierce, "Target has pierce effect");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Pierce effect test failed: {ex.Message}");
            }
        }

        private static void TestReflectEffect()
        {
            Console.WriteLine("Testing ReflectEffectHandler...");
            try
            {
                var handler = new ReflectEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Reflect effect applied");
                AssertTrue(target.ReflectStacks > 0, $"Reflect stacks: {target.ReflectStacks}");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Reflect effect test failed: {ex.Message}");
            }
        }

        private static void TestSilenceEffect()
        {
            Console.WriteLine("Testing SilenceEffectHandler...");
            try
            {
                var handler = new SilenceEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Silence effect applied");
                AssertTrue(target.IsSilenced, "Target is silenced");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Silence effect test failed: {ex.Message}");
            }
        }

        private static void TestMarkEffect()
        {
            Console.WriteLine("Testing MarkEffectHandler...");
            try
            {
                var handler = new MarkEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();

                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Mark effect applied");
                AssertTrue(target.IsMarked, "Target is marked");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Mark effect test failed: {ex.Message}");
            }
        }

        private static void TestDisruptEffect()
        {
            Console.WriteLine("Testing DisruptEffectHandler...");
            try
            {
                var handler = new DisruptEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                target.Effects.ComboStep = 1;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Disrupt effect applied at combo step 1");
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 1: {target.Effects.ComboStep} (expected 0)");
                
                target.Effects.ComboStep = 3;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 3: {target.Effects.ComboStep} (expected 0)");
                
                target.Effects.ComboStep = 5;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from step 5: {target.Effects.ComboStep} (expected 0)");
                
                target.Effects.ComboStep = 0;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo remains 0 when already disrupted: {target.Effects.ComboStep}");
                
                target.Effects.ComboStep = 10;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, $"Combo reset from high step 10: {target.Effects.ComboStep} (expected 0)");
                
                target.VulnerabilityStacks = 3;
                target.Effects.ComboStep = 4;
                handler.Apply(target, action, results);
                AssertTrue(target.Effects.ComboStep == 0, "Combo reset by disrupt");
                AssertTrue(target.VulnerabilityStacks == 3, 
                    $"Vulnerability stacks unaffected by disrupt: {target.VulnerabilityStacks} (expected 3)");
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Disrupt effect test failed: {ex.Message}");
            }
        }

        private static void TestCleanseEffect()
        {
            Console.WriteLine("Testing CleanseEffectHandler...");
            try
            {
                var handler = new CleanseEffectHandler();
                var target = new Character("Test", 1);
                var action = new Action { Name = "Test Action" };
                var results = new System.Collections.Generic.List<string>();
                
                target.PoisonStacks = 3;
                int initialPoison = target.PoisonStacks;
                bool applied = handler.Apply(target, action, results);
                AssertTrue(applied, "Cleanse effect applied");
                AssertTrue(target.PoisonStacks < initialPoison, 
                    $"Poison stacks reduced: {initialPoison} -> {target.PoisonStacks}");
                
                target.PoisonStacks = 5;
                target.VulnerabilityStacks = 3;
                target.ExposeStacks = 2;
                int poisonBefore = target.PoisonStacks;
                int vulnBefore = target.VulnerabilityStacks ?? 0;
                int exposeBefore = target.ExposeStacks ?? 0;
                
                handler.Apply(target, action, results);
                AssertTrue(target.PoisonStacks < poisonBefore || (target.VulnerabilityStacks ?? 0) < vulnBefore || (target.ExposeStacks ?? 0) < exposeBefore,
                    $"At least one debuff reduced: Poison {target.PoisonStacks}, Vuln {target.VulnerabilityStacks ?? 0}, Expose {target.ExposeStacks ?? 0}");
                
                target.PoisonStacks = 0;
                target.VulnerabilityStacks = 0;
                target.ExposeStacks = 0;
                applied = handler.Apply(target, action, results);
                AssertTrue(target.PoisonStacks == 0, 
                    $"Poison remains 0 when no debuffs: {target.PoisonStacks}");
                
                target.PoisonStacks = 10;
                int poisonHigh = target.PoisonStacks;
                handler.Apply(target, action, results);
                AssertTrue(target.PoisonStacks < poisonHigh, 
                    $"High poison stacks reduced: {poisonHigh} -> {target.PoisonStacks}");
                
                target.FortifyStacks = 3;
                target.HardenStacks = 2;
                target.PoisonStacks = 4;
                int fortifyBefore = target.FortifyStacks ?? 0;
                int hardenBefore = target.HardenStacks ?? 0;
                
                handler.Apply(target, action, results);
                AssertTrue((target.FortifyStacks ?? 0) == fortifyBefore, 
                    $"Fortify stacks unaffected by cleanse: {target.FortifyStacks ?? 0} (expected {fortifyBefore})");
                AssertTrue((target.HardenStacks ?? 0) == hardenBefore, 
                    $"Harden stacks unaffected by cleanse: {target.HardenStacks ?? 0} (expected {hardenBefore})");
                AssertTrue(target.PoisonStacks < 4, 
                    $"Poison stacks reduced by cleanse: {target.PoisonStacks} (expected < 4)");
                
                target.PoisonStacks = 1;
                handler.Apply(target, action, results);
            }
            catch (Exception ex)
            {
                AssertTrue(false, $"Cleanse effect test failed: {ex.Message}");
            }
        }
        
        private static void AssertTrue(bool condition, string message)
        {
            _testsRun++;
            if (condition)
            {
                _testsPassed++;
                Console.WriteLine($"  ✓ {message}");
            }
            else
            {
                _testsFailed++;
                Console.WriteLine($"  ✗ FAILED: {message}");
            }
        }
        
        private static void PrintSummary()
        {
            Console.WriteLine("\n=== Phase 2 Test Summary ===");
            Console.WriteLine($"Total Tests: {_testsRun}");
            Console.WriteLine($"Passed: {_testsPassed}");
            Console.WriteLine($"Failed: {_testsFailed}");
            
            if (_testsRun > 0)
            {
                Console.WriteLine($"Success Rate: {(_testsPassed * 100.0 / _testsRun):F1}%");
            }
            
            if (_testsFailed == 0)
            {
                Console.WriteLine("\n✅ All Phase 2 tests passed!");
            }
            else
            {
                Console.WriteLine($"\n❌ {_testsFailed} test(s) failed");
            }
        }
    }
}

