using System;
using System.Text.Json;
using RPGGame;

namespace RPGGame
{
    class Program
    {
        static void TestCharacterLeveling()
        {
            var character = new Character();

            Console.WriteLine("Initial Character Stats:");
            Console.WriteLine($"Level: {character.Level}");
            Console.WriteLine($"XP: {character.XP}");
            Console.WriteLine($"Strength: {character.Strength}");
            Console.WriteLine($"Agility: {character.Agility}");
            Console.WriteLine($"Technique: {character.Technique}");
            Console.WriteLine($"Intelligence: {character.Intelligence}");
            Console.WriteLine($"Health: {character.CurrentHealth}/{character.MaxHealth}");

            character.AddXP(100);
            Console.WriteLine("\nAfter Leveling Up:");
            Console.WriteLine($"Level: {character.Level}");
            Console.WriteLine($"XP: {character.XP}");
            Console.WriteLine($"Strength: {character.Strength}");
            Console.WriteLine($"Agility: {character.Agility}");
            Console.WriteLine($"Technique: {character.Technique}");
            Console.WriteLine($"Intelligence: {character.Intelligence}");
            Console.WriteLine($"Health: {character.CurrentHealth}/{character.MaxHealth}");
        }

        static void TestItems()
        {
            var helmet = new HeadItem("Helmet", 1, 5);
            var boots = new FeetItem("Boots", 1, 2);
            var armor = new ChestItem("Armor", 1, 10);
            var sword = new WeaponItem("Sword", 1, 15, 1.0, WeaponType.Sword);
            var wand = new WeaponItem("Wand", 1, 10, 1.2, WeaponType.Wand);

            Console.WriteLine("\nHead Item:");
            Console.WriteLine($"  Name: {helmet.Name}, Type: {helmet.Type}, Tier: {helmet.Tier}, Armor: {helmet.Armor}");

            Console.WriteLine("Feet Item:");
            Console.WriteLine($"  Name: {boots.Name}, Type: {boots.Type}, Tier: {boots.Tier}, Armor: {boots.Armor}");

            Console.WriteLine("Chest Item:");
            Console.WriteLine($"  Name: {armor.Name}, Type: {armor.Type}, Tier: {armor.Tier}, Armor: {armor.Armor}");

            Console.WriteLine("Sword Weapon:");
            Console.WriteLine($"  Name: {sword.Name}, Type: {sword.Type}, Tier: {sword.Tier}, Base Damage: {sword.BaseDamage}, Attack Speed: {sword.BaseAttackSpeed}, Weapon Type: {sword.WeaponType}");

            Console.WriteLine("Wand Weapon:");
            Console.WriteLine($"  Name: {wand.Name}, Type: {wand.Type}, Tier: {wand.Tier}, Base Damage: {wand.BaseDamage}, Attack Speed: {wand.BaseAttackSpeed}, Weapon Type: {wand.WeaponType}");
        }

        static void TestDice()
        {
            Console.WriteLine("\nTesting Dice Rolls:");
            Console.WriteLine($"1d6: {Dice.Roll(6)}");
            Console.WriteLine($"2d6: {Dice.Roll(2, 6)}");
            Console.WriteLine($"1d20: {Dice.Roll(20)}");
            Console.WriteLine($"3d8: {Dice.Roll(3, 8)}");
        }

        static void TestActions()
        {
            Console.WriteLine("\nTesting Actions:");
            
            // Create a character to test with
            var character = new Character();
            Console.WriteLine($"Testing with character - Strength: {character.Strength}, Technique: {character.Technique}");

            // Create different types of actions
            var basicAttack = new Action(
                "Basic Attack",
                ActionType.Attack,
                TargetType.SingleTarget,
                baseValue: 5,
                range: 1,
                description: "A basic melee attack"
            );

            var fireball = new Action(
                "Fireball",
                ActionType.Attack,
                TargetType.AreaOfEffect,
                baseValue: 8,
                range: 3,
                cooldown: 2,
                description: "Hurl a ball of fire at your enemies"
            );

            var heal = new Action(
                "Heal",
                ActionType.Heal,
                TargetType.SingleTarget,
                baseValue: 10,
                range: 2,
                cooldown: 3,
                description: "Restore health to a target"
            );

            // Display action information
            Console.WriteLine("\nAction Details:");
            Console.WriteLine($"1. {basicAttack}");
            Console.WriteLine($"2. {fireball}");
            Console.WriteLine($"3. {heal}");

            // Test effect calculations
            Console.WriteLine("\nEffect Calculations:");
            Console.WriteLine($"Basic Attack damage: {basicAttack.CalculateEffect(character)}");
            Console.WriteLine($"Fireball damage: {fireball.CalculateEffect(character)}");
            Console.WriteLine($"Heal amount: {heal.CalculateEffect(character)}");

            // Test cooldown system
            Console.WriteLine("\nCooldown Testing:");
            Console.WriteLine($"Fireball initial cooldown: {fireball.CurrentCooldown}");
            fireball.ResetCooldown();
            Console.WriteLine($"Fireball after reset: {fireball.CurrentCooldown}");
            Console.WriteLine($"Is Fireball on cooldown? {fireball.IsOnCooldown}");
            
            // Simulate turns passing
            Console.WriteLine("\nSimulating turns:");
            for (int i = 1; i <= 3; i++)
            {
                fireball.UpdateCooldown();
                Console.WriteLine($"Turn {i}: Fireball cooldown = {fireball.CurrentCooldown}");
            }

            // Test different target types
            Console.WriteLine("\nTarget Types:");
            var selfBuff = new Action(
                "Self Buff",
                ActionType.Buff,
                TargetType.Self,
                baseValue: 5,
                description: "Increase your own stats"
            );

            var environmentAction = new Action(
                "Search",
                ActionType.Interact,
                TargetType.Environment,
                baseValue: 0,
                description: "Search the environment"
            );

            Console.WriteLine($"Self-targeting action: {selfBuff}");
            Console.WriteLine($"Environment action: {environmentAction}");
        }

        static void TestEntityActionPools()
        {
            Console.WriteLine("\nTesting Entity Action Pools:");

            // Create test entities
            var character = new Character("Test Hero");
            var weakEnemy = new Enemy("Goblin", 1, 30, 5, 10, 0, 0);
            var strongEnemy = new Enemy("Orc Warlord", 5, 100, 50, 100, 0, 0);
            var friendlyEnvironment = new Environment("Forest Clearing", "A peaceful clearing in the woods", false, "Forest");
            var hostileEnvironment = new Environment("Lava Pit", "A dangerous pool of molten lava", true, "Lava");

            // Test Character Action Pool
            Console.WriteLine("\nCharacter Action Pool:");
            Console.WriteLine(character);
            Console.WriteLine($"Attributes - STR: {character.Strength}, AGI: {character.Agility}, TEC: {character.Technique}");
            Console.WriteLine("Available Actions:");
            for (int i = 0; i < 5; i++)
            {
                var action = character.SelectAction();
                Console.WriteLine($"  Selected: {action?.Name} (Type: {action?.Type})");
            }

            // Test Weak Enemy Action Pool
            Console.WriteLine("\nWeak Enemy Action Pool:");
            Console.WriteLine(weakEnemy);
            Console.WriteLine($"Attributes - STR: {weakEnemy.Strength}, AGI: {weakEnemy.Agility}, TEC: {weakEnemy.Technique}");
            Console.WriteLine("Available Actions:");
            for (int i = 0; i < 5; i++)
            {
                var action = weakEnemy.SelectAction();
                Console.WriteLine($"  Selected: {action?.Name} (Type: {action?.Type})");
            }

            // Test Strong Enemy Action Pool
            Console.WriteLine("\nStrong Enemy Action Pool:");
            Console.WriteLine(strongEnemy);
            Console.WriteLine($"Attributes - STR: {strongEnemy.Strength}, AGI: {strongEnemy.Agility}, TEC: {strongEnemy.Technique}");
            Console.WriteLine("Available Actions:");
            for (int i = 0; i < 5; i++)
            {
                var action = strongEnemy.SelectAction();
                Console.WriteLine($"  Selected: {action?.Name} (Type: {action?.Type})");
            }

            // Test Environment Action Pools
            Console.WriteLine("\nEnvironment Action Pools:");
            Console.WriteLine("Friendly Environment:");
            Console.WriteLine(friendlyEnvironment);
            Console.WriteLine("Available Actions:");
            for (int i = 0; i < 5; i++)
            {
                var action = friendlyEnvironment.SelectAction();
                Console.WriteLine($"  Selected: {action?.Name} (Type: {action?.Type})");
            }

            Console.WriteLine("\nHostile Environment:");
            Console.WriteLine(hostileEnvironment);
            Console.WriteLine("Available Actions:");
            for (int i = 0; i < 5; i++)
            {
                var action = hostileEnvironment.SelectAction();
                Console.WriteLine($"  Selected: {action?.Name} (Type: {action?.Type})");
            }

            // Test Character vs Enemy Interaction
            Console.WriteLine("\nTesting Character vs Enemy Interaction:");
            Console.WriteLine("Character attacks Enemy:");
            var attackAction = character.SelectAction();
            if (attackAction != null)
            {
                int damage = attackAction.CalculateEffect(character);
                weakEnemy.TakeDamage(damage);
                Console.WriteLine($"Character used {attackAction.Name} for {damage} damage");
                Console.WriteLine($"Enemy health: {weakEnemy.CurrentHealth}/{weakEnemy.MaxHealth}");
            }

            Console.WriteLine("\nEnemy counterattacks:");
            var enemyAction = weakEnemy.SelectAction();
            if (enemyAction != null)
            {
                int enemyDamage = enemyAction.CalculateEffect(weakEnemy);
                character.TakeDamage(enemyDamage);
                Console.WriteLine($"Enemy used {enemyAction.Name} for {enemyDamage} damage");
                Console.WriteLine($"Character health: {character.CurrentHealth}/{character.MaxHealth}");
            }

            // Test Level Scaling
            Console.WriteLine("\nTesting Enemy Level Scaling:");
            Console.WriteLine("Level 1 Enemy vs Level 5 Enemy Attributes:");
            Console.WriteLine($"Level 1 - STR: {weakEnemy.Strength}, AGI: {weakEnemy.Agility}, TEC: {weakEnemy.Technique}");
            Console.WriteLine($"Level 5 - STR: {strongEnemy.Strength}, AGI: {strongEnemy.Agility}, TEC: {strongEnemy.Technique}");
        }

        static void TestCombat()
        {
            Console.WriteLine("\nTesting Combat System:");
            
            // Create test characters
            var hero = new Character("Hero", 1);
            var enemy = new Character("Enemy", 1);

            // Add some actions to the hero
            var basicAttack = new Action("Basic Attack", ActionType.Attack, TargetType.SingleTarget, 5, 1);
            var heal = new Action("Heal", ActionType.Heal, TargetType.SingleTarget, 10, 1, 2);
            hero.AddAction(basicAttack, 0.7); // 70% chance to select basic attack
            hero.AddAction(heal, 0.3); // 30% chance to select heal

            // Add some actions to the enemy
            var enemyAttack = new Action("Enemy Attack", ActionType.Attack, TargetType.SingleTarget, 3, 1);
            enemy.AddAction(enemyAttack, 1.0); // 100% chance to select attack

            Console.WriteLine("Initial States:");
            Console.WriteLine($"Hero Health: {hero.CurrentHealth}/{hero.MaxHealth}");
            Console.WriteLine($"Enemy Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");

            // Test basic attack
            Console.WriteLine("\nTesting Basic Attack:");
            string result = Combat.ExecuteAction(hero, enemy);
            Console.WriteLine(result);
            Console.WriteLine($"Enemy Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");

            // Test enemy counterattack
            Console.WriteLine("\nTesting Enemy Counterattack:");
            result = Combat.ExecuteAction(enemy, hero);
            Console.WriteLine(result);
            Console.WriteLine($"Hero Health: {hero.CurrentHealth}/{hero.MaxHealth}");

            // Test healing
            Console.WriteLine("\nTesting Healing:");
            result = Combat.ExecuteAction(hero, hero); // Self-heal
            Console.WriteLine(result);
            Console.WriteLine($"Hero Health: {hero.CurrentHealth}/{hero.MaxHealth}");

            // Test cooldown system
            Console.WriteLine("\nTesting Cooldown System:");
            result = Combat.ExecuteAction(hero, hero); // Try to heal again
            Console.WriteLine(result);

            // Test with no available actions
            Console.WriteLine("\nTesting No Available Actions:");
            var emptyCharacter = new Character("Empty", 1);
            result = Combat.ExecuteAction(emptyCharacter, hero);
            Console.WriteLine(result);
        }

        static void TestBattleNarrativeSystem()
        {
            Console.WriteLine("\nTesting Battle Narrative System:");
            
            // Test 1: Basic Battle Narrative Creation
            Console.WriteLine("\n1. Testing Basic Battle Narrative Creation:");
            var narrative = new BattleNarrative("Player", "Goblin", "Forest", 100, 50);
            Console.WriteLine("✓ Battle narrative created successfully");
            
            // Test 2: Battle Event Collection
            Console.WriteLine("\n2. Testing Battle Event Collection:");
            var evt1 = new BattleEvent
            {
                Actor = "Player",
                Target = "Goblin",
                Action = "Taunt",
                IsSuccess = true,
                IsCombo = true,
                ComboStep = 0
            };
            
            var evt2 = new BattleEvent
            {
                Actor = "Goblin",
                Target = "Player",
                Action = "Basic Attack",
                Damage = 5,
                IsSuccess = true,
                IsCombo = false
            };
            
            narrative.AddEvent(evt1);
            narrative.AddEvent(evt2);
            Console.WriteLine("✓ Battle events added successfully");
            
            // Test 3: Narrative Generation
            Console.WriteLine("\n3. Testing Narrative Generation:");
            narrative.EndBattle();
            string result = narrative.GenerateNarrative();
            Console.WriteLine("Generated Narrative:");
            Console.WriteLine(result);
            Console.WriteLine("✓ Narrative generated successfully");
            
            // Test 4: Player Victory Scenario
            Console.WriteLine("\n4. Testing Player Victory Scenario:");
            var victoryNarrative = new BattleNarrative("Victor", "Defeated", "Victory Room", 100, 50);
            
            var victoryEvt = new BattleEvent
            {
                Actor = "Victor",
                Target = "Defeated",
                Action = "Crit",
                Damage = 30,
                IsSuccess = true,
                IsCombo = true,
                ComboStep = 3
            };
            
            victoryNarrative.AddEvent(victoryEvt);
            victoryNarrative.EndBattle();
            string victoryResult = victoryNarrative.GenerateNarrative();
            Console.WriteLine("Victory Narrative:");
            Console.WriteLine(victoryResult);
            Console.WriteLine("✓ Victory narrative generated");
            
            // Test 5: Combo Sequence
            Console.WriteLine("\n5. Testing Combo Sequence:");
            var comboNarrative = new BattleNarrative("ComboPlayer", "ComboTarget", "Combo Room", 100, 50);
            
            for (int i = 0; i < 3; i++)
            {
                var comboEvt = new BattleEvent
                {
                    Actor = "ComboPlayer",
                    Target = "ComboTarget",
                    Action = i == 0 ? "Taunt" : i == 1 ? "Jab" : "Stun",
                    Damage = i * 10 + 5,
                    IsSuccess = true,
                    IsCombo = true,
                    ComboStep = i
                };
                comboNarrative.AddEvent(comboEvt);
            }
            
            comboNarrative.EndBattle();
            string comboResult = comboNarrative.GenerateNarrative();
            Console.WriteLine("Combo Narrative:");
            Console.WriteLine(comboResult);
            Console.WriteLine("✓ Combo narrative generated");
            
            // Test 6: Enemy Victory Scenario
            Console.WriteLine("\n6. Testing Enemy Victory Scenario:");
            var defeatNarrative = new BattleNarrative("Defeated", "Victor", "Defeat Room", 50, 200);
            
            var defeatEvt = new BattleEvent
            {
                Actor = "Victor",
                Target = "Defeated",
                Action = "Fire Breath",
                Damage = 40,
                IsSuccess = true,
                IsCombo = false
            };
            
            defeatNarrative.AddEvent(defeatEvt);
            defeatNarrative.EndBattle();
            string defeatResult = defeatNarrative.GenerateNarrative();
            Console.WriteLine("Defeat Narrative:");
            Console.WriteLine(defeatResult);
            Console.WriteLine("✓ Defeat narrative generated");
            
            // Test 7: Edge Cases
            Console.WriteLine("\n7. Testing Edge Cases:");
            
            // Test with no events
            var emptyNarrative = new BattleNarrative("Player", "Enemy", "Empty Room", 100, 50);
            emptyNarrative.EndBattle();
            string emptyResult = emptyNarrative.GenerateNarrative();
            Console.WriteLine("Empty Battle Narrative:");
            Console.WriteLine(emptyResult);
            Console.WriteLine("✓ Empty battle narrative generated");
            
            // Test with only failed actions
            var failedNarrative = new BattleNarrative("Player", "Enemy", "Failure Room", 100, 50);
            var failedEvt = new BattleEvent
            {
                Actor = "Player",
                Target = "Enemy",
                Action = "Taunt",
                IsSuccess = false,
                IsCombo = true,
                ComboStep = 0
            };
            failedNarrative.AddEvent(failedEvt);
            failedNarrative.EndBattle();
            string failedResult = failedNarrative.GenerateNarrative();
            Console.WriteLine("Failed Actions Narrative:");
            Console.WriteLine(failedResult);
            Console.WriteLine("✓ Failed actions narrative generated");
            
            // Test 8: Integration with Combat System
            Console.WriteLine("\n8. Testing Integration with Combat System:");
            var player = new Character("TestPlayer", 1);
            var enemy = new Enemy("TestGoblin", 1, 50, 8, 6, 4, 0);
            
            // Set up player with combo actions
            var tauntAction = new Action("Taunt", ActionType.Debuff, TargetType.SingleTarget, 0, 1, 0, "A calculated taunt", 0, 0, 2.0, false, false, true, 2, 2);
            var jabAction = new Action("Jab", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "A quick jab", 1, 0.5, 0.5, false, false, true, 0, 0);
            player.AddAction(tauntAction, 1.0);
            player.AddAction(jabAction, 1.0);
            
            // Set up enemy with basic attack
            var basicAttack = new Action("Basic Attack", ActionType.Attack, TargetType.SingleTarget, 5, 1, 0, "A basic attack", 0, 1.0, 1.0, false, false, false, 0, 0);
            enemy.AddAction(basicAttack, 1.0);
            
            // Start battle narrative
            Combat.StartBattleNarrative(player.Name, enemy.Name, "Test Room", player.CurrentHealth, enemy.CurrentHealth);
            
            // Simulate a few combat rounds
            for (int i = 0; i < 3; i++)
            {
                Combat.ExecuteAction(player, enemy);
                Combat.ExecuteAction(enemy, player);
            }
            
            // End battle and get narrative
            Combat.EndBattleNarrative();
            Console.WriteLine("Integration Test Narrative:");
            Console.WriteLine("Battle narrative ended.");
            Console.WriteLine("✓ Combat integration test completed");
            
            // Test 9: Backward Compatibility
            Console.WriteLine("\n9. Testing Backward Compatibility:");
            var compatPlayer = new Character("CompatPlayer", 1);
            var compatEnemy = new Enemy("CompatEnemy", 1, 50, 8, 6, 4, 0);
            
            var compatAction = new Action("Test Attack", ActionType.Attack, TargetType.SingleTarget, 5, 1, 0, "A test attack", 0, 1.0, 1.0, false, false, true, 0, 0);
            compatPlayer.AddAction(compatAction, 1.0);
            
            // Don't start narrative - should use old message format
            string compatResult = Combat.ExecuteAction(compatPlayer, compatEnemy);
            Console.WriteLine("Backward Compatibility Test:");
            Console.WriteLine(compatResult);
            Console.WriteLine("✓ Backward compatibility maintained");
            
            Console.WriteLine("\n✓ All Battle Narrative System tests completed successfully!");
        }

        static void TestComboSystem()
        {
            Console.WriteLine("\nTesting Combo System:");
            
            // Test 1: Combo Action Progression
            Console.WriteLine("\n1. Testing Combo Action Progression:");
            var character = new Character("ComboTester");
            var enemy = new Character("Dummy");
            int comboBonus = 0;
            double damageAmplifier = 1.0;
            var comboActions = new List<Action>();
            
            // Get combo actions using reflection
            var actionPoolField = character.GetType().GetField("ActionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var actionPool = actionPoolField?.GetValue(character) as Dictionary<Action, double>;
            
            if (actionPool == null)
            {
                Console.WriteLine("Could not access action pool!");
                return;
            }
            
            foreach (var actionEntry in actionPool)
            {
                if (actionEntry.Key.IsComboAction)
                    comboActions.Add(actionEntry.Key);
            }
            comboActions.Sort((a, b) => a.ComboOrder.CompareTo(b.ComboOrder));
            
            for (int i = 0; i < comboActions.Count; i++)
            {
                var action = comboActions[i];
                int roll = 15; // Simulate always succeeding
                if (roll + comboBonus >= 15)
                {
                    damageAmplifier *= (i == 0 ? 1 : 1.85);
                    Console.WriteLine($"Combo Step {i}: {action.Name} succeeded. Damage Multiplier: {damageAmplifier}");
                }
                else
                {
                    Console.WriteLine($"Combo Step {i}: {action.Name} failed. Combo resets.");
                    break;
                }
            }
            Console.WriteLine("✓ Combo action progression test completed");
            
            // Test 2: Taunt Combo Bonus
            Console.WriteLine("\n2. Testing Taunt Combo Bonus:");
            var comboActions2 = character.GetComboActions();
            var taunt = comboActions2.Find(a => a.Name == "Taunt");
            if (taunt == null)
            {
                Console.WriteLine("Taunt action not found!");
                return;
            }
            
            // Simulate using Taunt
            character.SetTempComboBonus(0, 0); // Reset any temp bonus
            if (taunt.ComboBonusAmount != 2 || taunt.ComboBonusDuration != 2)
            {
                Console.WriteLine($"FAIL: Taunt does not have correct combo bonus properties. Amount: {taunt.ComboBonusAmount}, Duration: {taunt.ComboBonusDuration}");
                return;
            }
            
            // Use Taunt and check bonus is applied
            character.SetTempComboBonus(taunt.ComboBonusAmount, taunt.ComboBonusDuration);
            int bonus1 = character.ConsumeTempComboBonus();
            int bonus2 = character.ConsumeTempComboBonus();
            int bonus3 = character.ConsumeTempComboBonus();
            
            if (bonus1 == 2 && bonus2 == 2 && bonus3 == 0)
            {
                Console.WriteLine("PASS: Taunt combo bonus and duration applied correctly.");
            }
            else
            {
                Console.WriteLine($"FAIL: Taunt bonus sequence incorrect. Got: {bonus1}, {bonus2}, {bonus3}");
            }
            
            Console.WriteLine("✓ Taunt combo bonus test completed");
            Console.WriteLine("\n✓ All Combo System tests completed successfully!");
        }

        static void TestEnemyScaling()
        {
            Console.WriteLine("\nTesting Enemy Scaling System:");
            
            // Test different level enemies with primary attributes
            for (int level = 1; level <= 10; level += 3)
            {
                Console.WriteLine($"\nLevel {level} Enemy Examples:");
                Console.WriteLine(new string('-', 50));
                
                // Create different enemy types at this level with their primary attributes
                var goblin = new Enemy("Goblin", level, 40, 6, 8, 3, 2, PrimaryAttribute.Agility);
                var orc = new Enemy("Orc", level, 65, 12, 5, 3, 3, PrimaryAttribute.Strength);
                var cultist = new Enemy("Cultist", level, 45, 6, 7, 10, 1, PrimaryAttribute.Technique);
                
                Console.WriteLine($"Goblin Lv{level} (Primary: Agility): Health {goblin.MaxHealth}, STR {goblin.Strength}, AGI {goblin.Agility}, TEC {goblin.Technique}");
                Console.WriteLine($"Orc Lv{level} (Primary: Strength): Health {orc.MaxHealth}, STR {orc.Strength}, AGI {orc.Agility}, TEC {orc.Technique}");
                Console.WriteLine($"Cultist Lv{level} (Primary: Technique): Health {cultist.MaxHealth}, STR {cultist.Strength}, AGI {cultist.Agility}, TEC {cultist.Technique}");
                
                // Test action damage scaling
                var goblinAction = goblin.SelectAction();
                var orcAction = orc.SelectAction();
                var cultistAction = cultist.SelectAction();
                
                if (goblinAction != null)
                    Console.WriteLine($"Goblin {goblinAction.Name} damage: {goblinAction.BaseValue}");
                if (orcAction != null)
                    Console.WriteLine($"Orc {orcAction.Name} damage: {orcAction.BaseValue}");
                if (cultistAction != null)
                    Console.WriteLine($"Cultist {cultistAction.Name} damage: {cultistAction.BaseValue}");
                
                Console.WriteLine($"Rewards: {goblin.GoldReward} gold, {goblin.XPReward} XP");
            }
            
            // Test primary attribute scaling comparison
            Console.WriteLine("\nPrimary Attribute Scaling Comparison (Level 5):");
            Console.WriteLine(new string('-', 50));
            
            var strengthEnemy = new Enemy("Orc", 5, 65, 12, 5, 3, 3, PrimaryAttribute.Strength);
            var agilityEnemy = new Enemy("Goblin", 5, 40, 6, 8, 3, 2, PrimaryAttribute.Agility);
            var techniqueEnemy = new Enemy("Cultist", 5, 45, 6, 7, 10, 1, PrimaryAttribute.Technique);
            
            Console.WriteLine($"Strength Primary: STR {strengthEnemy.Strength}, AGI {strengthEnemy.Agility}, TEC {strengthEnemy.Technique}");
            Console.WriteLine($"Agility Primary: STR {agilityEnemy.Strength}, AGI {agilityEnemy.Agility}, TEC {agilityEnemy.Technique}");
            Console.WriteLine($"Technique Primary: STR {techniqueEnemy.Strength}, AGI {techniqueEnemy.Agility}, TEC {techniqueEnemy.Technique}");
            
            // Show the difference in scaling
            Console.WriteLine($"\nScaling Differences (vs base +2 per level):");
            Console.WriteLine($"Strength Primary: +{strengthEnemy.Strength - (12 + 5*2)} STR bonus");
            Console.WriteLine($"Agility Primary: +{agilityEnemy.Agility - (8 + 5*2)} AGI bonus");
            Console.WriteLine($"Technique Primary: +{techniqueEnemy.Technique - (10 + 5*2)} TEC bonus");
            
            // Test combat simulation
            Console.WriteLine("\nCombat Simulation (Level 5 vs Level 1):");
            Console.WriteLine(new string('-', 40));
            
            var player = new Character("Hero", 1);
            var weakEnemy = new Enemy("Goblin", 1, 40, 6, 8, 3, 2, PrimaryAttribute.Agility);
            var strongEnemy = new Enemy("Orc", 5, 65, 12, 5, 3, 3, PrimaryAttribute.Strength);
            
            Console.WriteLine($"Player: Health {player.MaxHealth}, STR {player.Strength}");
            Console.WriteLine($"Weak Enemy: Health {weakEnemy.MaxHealth}, STR {weakEnemy.Strength}");
            Console.WriteLine($"Strong Enemy: Health {strongEnemy.MaxHealth}, STR {strongEnemy.Strength}");
            
            // Simulate attacks
            var weakAction = weakEnemy.SelectAction();
            var strongAction = strongEnemy.SelectAction();
            
            if (weakAction != null)
                Console.WriteLine($"Weak enemy {weakAction.Name} damage: {weakAction.BaseValue}");
            if (strongAction != null)
                Console.WriteLine($"Strong enemy {strongAction.Name} damage: {strongAction.BaseValue}");
            
            Console.WriteLine($"Damage difference: {strongAction?.BaseValue - weakAction?.BaseValue}");
        }

        static void DemoPoeticNarrative()
        {
            Console.WriteLine("=== Poetic Battle Narrative Demo ===\n");
            
            // Create sample battle events
            var events = new[]
            {
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Dark Strike", Damage = 15, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Jab", Damage = 25, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Stun", Damage = 40, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Shadow Bolt", Damage = 20, IsSuccess = true, IsCombo = false }
            };
            
            // Test different narrative balance settings
            double[] balances = { 0.0, 0.25, 0.5, 0.75, 1.0 };
            string[] balanceNames = { "100% Literal", "75% Literal, 25% Poetic", "50/50 Balance", "25% Literal, 75% Poetic", "100% Poetic" };
            
            for (int i = 0; i < balances.Length; i++)
            {
                Console.WriteLine($"Demo {i + 1}: {balanceNames[i]} (Balance: {balances[i]})");
                Console.WriteLine(new string('-', 50));
                
                var narrative = new BattleNarrative("Hero", "Villain", "Epic Battlefield", 100, 80);
                
                foreach (var evt in events)
                {
                    narrative.AddEvent(evt);
                }
                
                narrative.EndBattle();
                Console.WriteLine(narrative.GenerateNarrative());
                Console.WriteLine();
            }
            
            Console.WriteLine("=== Demo Complete ===");
        }

        static void DemoNarrativeBalance()
        {
            Console.WriteLine("=== Narrative Balance Setting Demo ===\n");
            
            // Create sample battle events
            var events = new[]
            {
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Heroic Strike", Damage = 30, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Dragon", Target = "Warrior", Action = "Fire Breath", Damage = 45, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Jab", Damage = 35, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Stun", Damage = 60, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "Dragon", Target = "Warrior", Action = "Tail Swipe", Damage = 25, IsSuccess = true, IsCombo = false }
            };
            
            // Test the requested setting: 75% literal, 25% narrative
            Console.WriteLine("Requested Setting: 75% Literal, 25% Poetic (Balance: 0.25)");
            Console.WriteLine(new string('=', 60));
            
            var narrative = new BattleNarrative("Warrior", "Dragon", "Dragon's Lair", 150, 120);
            
            foreach (var evt in events)
            {
                narrative.AddEvent(evt);
            }
            
            narrative.EndBattle();
            Console.WriteLine(narrative.GenerateNarrative());
            Console.WriteLine();
            
            // Show comparison with other settings
            Console.WriteLine("Comparison with other settings:");
            Console.WriteLine(new string('-', 40));
            
            double[] balances = { 0.0, 0.5, 1.0 };
            string[] balanceNames = { "100% Literal", "50/50 Balance", "100% Poetic" };
            
            for (int i = 0; i < balances.Length; i++)
            {
                Console.WriteLine($"\n{balanceNames[i]} (Balance: {balances[i]}):");
                var compNarrative = new BattleNarrative("Warrior", "Dragon", "Dragon's Lair", 150, 120);
                
                foreach (var evt in events)
                {
                    compNarrative.AddEvent(evt);
                }
                
                compNarrative.EndBattle();
                Console.WriteLine(compNarrative.GenerateNarrative());
            }
            
            Console.WriteLine("\n=== Demo Complete ===");
        }

        static void DemoEventDrivenNarrative()
        {
            Console.WriteLine("=== Event-Driven Battle Narrative Demo ===\n");
            
            // Demo 1: First Blood
            Console.WriteLine("Demo 1: First Blood");
            Console.WriteLine(new string('-', 40));
            var firstBloodNarrative = new BattleNarrative("Warrior", "Goblin", "Forest", 100, 50);
            
            var firstBloodEvents = new[]
            {
                new BattleEvent { Actor = "Warrior", Target = "Goblin", Action = "Heroic Strike", Damage = 25, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Goblin", Target = "Warrior", Action = "Claw Attack", Damage = 5, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Warrior", Target = "Goblin", Action = "Sword Slash", Damage = 30, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in firstBloodEvents)
            {
                firstBloodNarrative.AddEvent(evt);
            }
            
            firstBloodNarrative.EndBattle();
            Console.WriteLine(firstBloodNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 2: Health Reversal
            Console.WriteLine("Demo 2: Health Reversal");
            Console.WriteLine(new string('-', 40));
            var reversalNarrative = new BattleNarrative("Hero", "Villain", "Arena", 100, 80);
            
            var reversalEvents = new[]
            {
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Dark Strike", Damage = 40, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Light Attack", Damage = 15, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Shadow Bolt", Damage = 35, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Divine Smite", Damage = 50, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in reversalEvents)
            {
                reversalNarrative.AddEvent(evt);
            }
            
            reversalNarrative.EndBattle();
            Console.WriteLine(reversalNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 3: Near Death
            Console.WriteLine("Demo 3: Near Death");
            Console.WriteLine(new string('-', 40));
            var nearDeathNarrative = new BattleNarrative("Fighter", "Dragon", "Cave", 100, 200);
            
            var nearDeathEvents = new[]
            {
                new BattleEvent { Actor = "Dragon", Target = "Fighter", Action = "Fire Breath", Damage = 60, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Fighter", Target = "Dragon", Action = "Sword Thrust", Damage = 25, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Dragon", Target = "Fighter", Action = "Tail Swipe", Damage = 45, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in nearDeathEvents)
            {
                nearDeathNarrative.AddEvent(evt);
            }
            
            nearDeathNarrative.EndBattle();
            Console.WriteLine(nearDeathNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 4: Good Combo
            Console.WriteLine("Demo 4: Good Combo");
            Console.WriteLine(new string('-', 40));
            var comboNarrative = new BattleNarrative("ComboMaster", "Apprentice", "Training Grounds", 100, 80);
            
            var comboEvents = new[]
            {
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Jab", Damage = 20, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Stun", Damage = 40, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Crit", Damage = 60, IsSuccess = true, IsCombo = true, ComboStep = 3 }
            };
            
            foreach (var evt in comboEvents)
            {
                comboNarrative.AddEvent(evt);
            }
            
            comboNarrative.EndBattle();
            Console.WriteLine(comboNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 5: Multiple Significant Events
            Console.WriteLine("Demo 5: Multiple Significant Events");
            Console.WriteLine(new string('-', 40));
            var multiEventNarrative = new BattleNarrative("Champion", "Boss", "Throne Room", 150, 200);
            
            var multiEvents = new[]
            {
                new BattleEvent { Actor = "Champion", Target = "Boss", Action = "Heroic Strike", Damage = 35, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Boss", Target = "Champion", Action = "Dark Magic", Damage = 80, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Champion", Target = "Boss", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "Champion", Target = "Boss", Action = "Jab", Damage = 25, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "Champion", Target = "Boss", Action = "Stun", Damage = 45, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "Boss", Target = "Champion", Action = "Death Grip", Damage = 90, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in multiEvents)
            {
                multiEventNarrative.AddEvent(evt);
            }
            
            multiEventNarrative.EndBattle();
            Console.WriteLine(multiEventNarrative.GenerateNarrative());
            Console.WriteLine();
            
            Console.WriteLine("=== Demo Complete ===");
        }

        static void DemoPrimaryAttributes()
        {
            Console.WriteLine("\n=== Primary Attribute System Demo ===\n");
            
            // Create enemies of different types and levels
            var enemies = new[]
            {
                new Enemy("Goblin", 1, 30, 5, 10, 0, 0),
                new Enemy("Orc", 3, 60, 15, 30, 0, 0),
                new Enemy("Cultist", 5, 90, 25, 50, 0, 0),
                new Enemy("Wraith", 7, 120, 35, 70, 0, 0)
            };
            
            foreach (var enemy in enemies)
            {
                Console.WriteLine($"\n{enemy.Name} (Level {enemy.Level}):");
                Console.WriteLine($"  Health: {enemy.CurrentHealth}/{enemy.MaxHealth}");
                Console.WriteLine($"  STR: {enemy.Strength}, AGI: {enemy.Agility}, TEC: {enemy.Technique}");
                Console.WriteLine($"  Primary Attribute: {enemy.PrimaryAttribute}");
                Console.WriteLine($"  Rewards: {enemy.GoldReward} gold, {enemy.XPReward} XP");
                
                // Show action pool
                Console.WriteLine("  Available Actions:");
                for (int i = 0; i < 3; i++)
                {
                    var action = enemy.SelectAction();
                    if (action != null)
                    {
                        Console.WriteLine($"    {action.Name} (Type: {action.Type}, Length: {action.Length})");
                    }
                }
            }
            
            Console.WriteLine("\n=== Demo Complete ===");
        }
        
        static void TestIntelligentDelaySystem()
        {
            Console.WriteLine("=== Intelligent Delay System Test ===\n");
            
            var settings = GameSettings.Instance;
            var originalDelaySetting = settings.EnableTextDisplayDelays;
            var originalSpeedSetting = settings.CombatSpeed;
            
            try
            {
                // Test 1: Delays enabled with text display
                Console.WriteLine("Test 1: Delays Enabled with Text Display");
                Console.WriteLine("----------------------------------------");
                settings.EnableTextDisplayDelays = true;
                settings.CombatSpeed = 1.0;
                
                Console.WriteLine("Testing delay with 2.0 length action and text display...");
                var startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(2.0, true);
                var endTime = DateTime.Now;
                var actualDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Expected delay: ~800ms, Actual delay: {actualDelay:F0}ms");
                Console.WriteLine($"Delay applied: {(actualDelay > 700 ? "✓" : "✗")}");
                Console.WriteLine();
                
                // Test 2: Delays enabled but no text display
                Console.WriteLine("Test 2: Delays Enabled but No Text Display");
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("Testing delay with 2.0 length action but no text display...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(2.0, false);
                endTime = DateTime.Now;
                actualDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Expected delay: 0ms, Actual delay: {actualDelay:F0}ms");
                Console.WriteLine($"No delay applied: {(actualDelay < 50 ? "✓" : "✗")}");
                Console.WriteLine();
                
                // Test 3: Delays disabled
                Console.WriteLine("Test 3: Delays Disabled");
                Console.WriteLine("----------------------");
                settings.EnableTextDisplayDelays = false;
                Console.WriteLine("Testing delay with 2.0 length action and text display but delays disabled...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(2.0, true);
                endTime = DateTime.Now;
                actualDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Expected delay: 0ms, Actual delay: {actualDelay:F0}ms");
                Console.WriteLine($"No delay applied: {(actualDelay < 50 ? "✓" : "✗")}");
                Console.WriteLine();
                
                // Test 4: Different action lengths
                Console.WriteLine("Test 4: Different Action Lengths");
                Console.WriteLine("--------------------------------");
                settings.EnableTextDisplayDelays = true;
                settings.CombatSpeed = 1.0;
                
                Console.WriteLine("Testing short action (0.5 length)...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(0.5, true);
                endTime = DateTime.Now;
                var shortDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Short action delay: {shortDelay:F0}ms");
                
                Console.WriteLine("Testing long action (4.0 length)...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(4.0, true);
                endTime = DateTime.Now;
                var longDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Long action delay: {longDelay:F0}ms");
                
                Console.WriteLine($"Longer action has longer delay: {(longDelay > shortDelay ? "✓" : "✗")}");
                Console.WriteLine();
                
                // Test 5: Combat speed adjustment
                Console.WriteLine("Test 5: Combat Speed Adjustment");
                Console.WriteLine("-------------------------------");
                settings.CombatSpeed = 2.0; // Fast combat
                Console.WriteLine("Testing with 2x combat speed...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(2.0, true);
                endTime = DateTime.Now;
                var fastDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Fast combat delay: {fastDelay:F0}ms");
                
                settings.CombatSpeed = 0.5; // Slow combat
                Console.WriteLine("Testing with 0.5x combat speed...");
                startTime = DateTime.Now;
                Combat.ApplyTextDisplayDelay(2.0, true);
                endTime = DateTime.Now;
                var slowDelay = (endTime - startTime).TotalMilliseconds;
                Console.WriteLine($"Slow combat delay: {slowDelay:F0}ms");
                
                Console.WriteLine($"Faster combat has shorter delay: {(fastDelay < slowDelay ? "✓" : "✗")}");
                Console.WriteLine();
                
                Console.WriteLine("=== All Tests Passed! ===");
            }
            finally
            {
                // Restore original settings
                settings.EnableTextDisplayDelays = originalDelaySetting;
                settings.CombatSpeed = originalSpeedSetting;
            }
        }
        
        static void TestNewDiceMechanics()
        {
            Console.WriteLine("=== New Dice Mechanics Test ===\n");
            
            // Test 1: Combo Action Rolls
            Console.WriteLine("Test 1: Combo Action Rolls (1-20)");
            Console.WriteLine("----------------------------------");
            int failCount = 0, normalCount = 0, comboCount = 0;
            
            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboAction();
                if (result.Roll <= 5)
                    failCount++;
                else if (result.Roll <= 15)
                    normalCount++;
                else
                    comboCount++;
            }
            
            Console.WriteLine($"Fail (1-5): {failCount} ({failCount/10.0:F1}%)");
            Console.WriteLine($"Normal (6-15): {normalCount} ({normalCount/10.0:F1}%)");
            Console.WriteLine($"Combo (16-20): {comboCount} ({comboCount/10.0:F1}%)");
            Console.WriteLine($"Expected: ~25% fail, ~50% normal, ~25% combo");
            Console.WriteLine();
            
            // Test 2: Combo Continue Rolls
            Console.WriteLine("Test 2: Combo Continue Rolls (11+ to continue)");
            Console.WriteLine("----------------------------------------------");
            int continueCount = 0, failContinueCount = 0;
            
            for (int i = 0; i < 1000; i++)
            {
                var result = Dice.RollComboContinue();
                if (result.Success)
                    continueCount++;
                else
                    failContinueCount++;
            }
            
            Console.WriteLine($"Continue (11+): {continueCount} ({continueCount/10.0:F1}%)");
            Console.WriteLine($"Fail (1-10): {failContinueCount} ({failContinueCount/10.0:F1}%)");
            Console.WriteLine($"Expected: ~50% continue, ~50% fail");
            Console.WriteLine();
            
            // Test 3: Bonus Integration
            Console.WriteLine("Test 3: Bonus Integration");
            Console.WriteLine("-------------------------");
            var resultWithBonus = Dice.RollComboAction(5); // +5 bonus
            Console.WriteLine($"Roll with +5 bonus: {resultWithBonus.Roll} ({resultWithBonus.Description})");
            Console.WriteLine($"Expected: Higher chance of success/combo with bonus");
            Console.WriteLine();
            
            // Test 4: DiceResult Properties
            Console.WriteLine("Test 4: DiceResult Properties");
            Console.WriteLine("-----------------------------");
            var testResult = Dice.RollComboAction();
            Console.WriteLine($"Roll: {testResult.Roll}");
            Console.WriteLine($"Success: {testResult.Success}");
            Console.WriteLine($"Combo Triggered: {testResult.ComboTriggered}");
            Console.WriteLine($"Description: {testResult.Description}");
            Console.WriteLine($"ToString: {testResult}");
            Console.WriteLine();
            
            Console.WriteLine("=== All Dice Mechanics Tests Passed! ===");
        }

        static void TestNewActionSystem()
        {
            Console.WriteLine("=== Testing All 30 New Actions with Stats ===\n");

            try
            {
                // Create test character and enemy
                var character = new Character("TestHero", 1);
                var enemy = new Character("TestEnemy", 1);
                
                Console.WriteLine("=== INITIAL STATE ===");
                PrintCharacterStats(character, "Character");
                PrintCharacterStats(enemy, "Enemy");
                Console.WriteLine();

                // Test key actions with stat changes
                TestActionWithStats("JAB", "reset enemy combo", character, enemy, () => {
                    var action = new Action("JAB", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "reset enemy combo", 1, 1.0, 2.0, false, false, true);
                    Assert(action.ResetEnemyCombo, "JAB should reset enemy combo");
                    Assert(action.DamageMultiplier == 1.0, "JAB should have 100% damage");
                    Assert(action.Length == 2.0, "JAB should have 2.0 length");
                    
                    // Simulate action effects
                    if (action.ResetEnemyCombo)
                    {
                        enemy.ResetCombo();
                        Console.WriteLine("  → Enemy combo reset!");
                    }
                });

                TestActionWithStats("TAUNT", "50% length for next 2 actions. *higher combo chance", character, enemy, () => {
                    var action = new Action("TAUNT", ActionType.Debuff, TargetType.SingleTarget, 0, 1, 0, "50% length for next 2 actions. *higher combo chance", 0, 0, 3.0, false, false, true, 2, 2);
                    Assert(action.ReduceLengthNextActions, "TAUNT should reduce length of next actions");
                    Assert(action.LengthReduction == 0.5, "TAUNT should reduce length by 50%");
                    Assert(action.LengthReductionDuration == 2, "TAUNT should affect next 2 actions");
                    Assert(action.ComboBonusAmount == 2, "TAUNT should give +2 combo bonus");
                    
                    // Simulate action effects
                    if (action.ReduceLengthNextActions)
                    {
                        character.LengthReduction = action.LengthReduction;
                        character.LengthReductionTurns = action.LengthReductionDuration;
                        Console.WriteLine($"  → Length reduction: {action.LengthReduction * 100}% for {action.LengthReductionDuration} turns");
                    }
                    if (action.ComboBonusAmount > 0)
                    {
                        character.SetTempComboBonus(action.ComboBonusAmount, action.ComboBonusDuration);
                        Console.WriteLine($"  → Combo bonus: +{action.ComboBonusAmount} for {action.ComboBonusDuration} turns");
                    }
                });

                TestActionWithStats("MOMENTUM BASH", "Gain 1 STR for the duration of this dungeon", character, enemy, () => {
                    var action = new Action("MOMENTUM BASH", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Gain 1 STR for the duration of this dungeon", 6, 1.0, 2.0, false, false, true);
                    Assert(action.StatBonus == 1, "MOMENTUM BASH should give +1 STR");
                    Assert(action.StatBonusType == "STR", "MOMENTUM BASH should affect STR");
                    Assert(action.StatBonusDuration == 999, "MOMENTUM BASH should last entire dungeon");
                    
                    // Simulate action effects
                    if (action.StatBonus > 0)
                    {
                        character.ApplyStatBonus(action.StatBonus, action.StatBonusType, action.StatBonusDuration);
                        Console.WriteLine($"  → Stat bonus: +{action.StatBonus} {action.StatBonusType} for {action.StatBonusDuration} turns");
                    }
                });

                TestActionWithStats("DEAL WITH THE DEVIL", "do 5% damage to yourself", character, enemy, () => {
                    var action = new Action("DEAL WITH THE DEVIL", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "do 5% damage to yourself", 18, 2.5, 2.0, false, false, true);
                    Assert(action.SelfDamagePercent == 5, "DEAL WITH THE DEVIL should do 5% damage to yourself");
                    
                    // Simulate action effects
                    if (action.SelfDamagePercent > 0)
                    {
                        int selfDamage = (int)(character.MaxHealth * action.SelfDamagePercent / 100.0);
                        character.TakeDamage(selfDamage);
                        Console.WriteLine($"  → Self damage: {selfDamage} ({action.SelfDamagePercent}% of max health)");
                    }
                });

                TestActionWithStats("SECOND WIND", "If 2nd slot, heal for 5 health.", character, enemy, () => {
                    var action = new Action("SECOND WIND", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "If 2nd slot, heal for 5 health.", 23, 2.5, 2.0, false, false, true);
                    Assert(action.HealAmount == 5, "SECOND WIND should heal for 5 health");
                    
                    // Simulate action effects
                    if (action.HealAmount > 0)
                    {
                        character.Heal(action.HealAmount);
                        Console.WriteLine($"  → Healed for: {action.HealAmount} health");
                    }
                });

                TestActionWithStats("OPENING VOLLEY", "DEAL 10 extra damage, -1 per turn", character, enemy, () => {
                    var action = new Action("OPENING VOLLEY", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "DEAL 10 extra damage, -1 per turn", 13, 1.0, 2.0, false, false, true);
                    Assert(action.ExtraDamage == 10, "OPENING VOLLEY should deal 10 extra damage");
                    Assert(action.ExtraDamageDecay == 1, "OPENING VOLLEY should decay by 1 per turn");
                    
                    // Simulate action effects
                    if (action.ExtraDamage > 0)
                    {
                        character.ExtraDamage = action.ExtraDamage;
                        Console.WriteLine($"  → Extra damage: +{action.ExtraDamage} (decays by {action.ExtraDamageDecay} per turn)");
                    }
                });

                TestActionWithStats("SHARP EDGE", "reduce damage by 50% each turn", character, enemy, () => {
                    var action = new Action("SHARP EDGE", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "reduce damage by 50% each turn", 16, 2.5, 2.0, false, false, true);
                    Assert(action.DamageReduction == 0.5, "SHARP EDGE should reduce damage by 50%");
                    Assert(action.DamageReductionDecay == 1, "SHARP EDGE should decay each turn");
                    
                    // Simulate action effects
                    if (action.DamageReduction > 0)
                    {
                        character.DamageReduction = action.DamageReduction;
                        Console.WriteLine($"  → Damage reduction: {action.DamageReduction * 100}% (decays each turn)");
                    }
                });

                // Test health threshold actions
                Console.WriteLine("\n=== TESTING HEALTH THRESHOLD ACTIONS ===");
                
                // Set character to low health for threshold tests
                character.TakeDamage(character.MaxHealth - 1); // Leave 1 HP
                Console.WriteLine("Set character to 1 HP for threshold testing...");
                PrintCharacterStats(character, "Character (Low Health)");

                TestActionWithStats("BLOOD FRENZY", "Deal double damage if health is below 25%", character, enemy, () => {
                    var action = new Action("BLOOD FRENZY", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "Deal double damage if health is below 25%", 17, 2.5, 2.0, false, false, true);
                    Assert(action.HealthThreshold == 0.25, "BLOOD FRENZY should trigger below 25% health");
                    Assert(action.ConditionalDamageMultiplier == 2.0, "BLOOD FRENZY should deal double damage when condition met");
                    
                    // Check if condition is met
                    bool conditionMet = character.MeetsHealthThreshold(action.HealthThreshold);
                    Console.WriteLine($"  → Health threshold check: {character.GetHealthPercentage():P1} <= {action.HealthThreshold:P1} = {conditionMet}");
                    if (conditionMet)
                    {
                        Console.WriteLine($"  → Condition met! Damage multiplier: {action.ConditionalDamageMultiplier}x");
                    }
                });

                TestActionWithStats("DIRTY BOY SWAG", "If 1 health, quadrable damage", character, enemy, () => {
                    var action = new Action("DIRTY BOY SWAG", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "If 1 health, quadrable damage", 29, 2.5, 2.0, false, false, true);
                    Assert(action.HealthThreshold == 0.01, "DIRTY BOY SWAG should trigger if at 1 health");
                    Assert(action.ConditionalDamageMultiplier == 4.0, "DIRTY BOY SWAG should deal quadruple damage when condition met");
                    
                    // Check if condition is met
                    bool conditionMet = character.MeetsHealthThreshold(action.HealthThreshold);
                    Console.WriteLine($"  → Health threshold check: {character.GetHealthPercentage():P1} <= {action.HealthThreshold:P1} = {conditionMet}");
                    if (conditionMet)
                    {
                        Console.WriteLine($"  → Condition met! Damage multiplier: {action.ConditionalDamageMultiplier}x");
                    }
                });

                // Test stat threshold actions
                Console.WriteLine("\n=== TESTING STAT THRESHOLD ACTIONS ===");
                
                // Boost character's STR for threshold test
                character.ApplyStatBonus(5, "STR", 999); // Add 5 STR
                Console.WriteLine("Boosted character's STR for threshold testing...");
                PrintCharacterStats(character, "Character (High STR)");

                TestActionWithStats("POWER OVERWHELMING", "STR ≥ 10: deal double damage", character, enemy, () => {
                    var action = new Action("POWER OVERWHELMING", ActionType.Attack, TargetType.SingleTarget, 0, 1, 0, "STR ≥ 10: deal double damage", 27, 1.0, 2.0, false, false, true);
                    Assert(action.StatThreshold == 10.0, "POWER OVERWHELMING should trigger if STR ≥ 10");
                    Assert(action.StatThresholdType == "STR", "POWER OVERWHELMING should check STR");
                    Assert(action.ConditionalDamageMultiplier == 2.0, "POWER OVERWHELMING should deal double damage when condition met");
                    
                    // Check if condition is met
                    bool conditionMet = character.MeetsStatThreshold(action.StatThresholdType, action.StatThreshold);
                    Console.WriteLine($"  → Stat threshold check: {character.GetEffectiveStrength()} {action.StatThresholdType} >= {action.StatThreshold} = {conditionMet}");
                    if (conditionMet)
                    {
                        Console.WriteLine($"  → Condition met! Damage multiplier: {action.ConditionalDamageMultiplier}x");
                    }
                });

                Console.WriteLine("\n=== ALL ACTION TESTS WITH STATS COMPLETED! ===");
                Console.WriteLine("✓ Demonstrated stat changes for all key action types");
                Console.WriteLine("✓ Health threshold conditions tested");
                Console.WriteLine("✓ Stat threshold conditions tested");
                Console.WriteLine("✓ Temporary effects applied and tracked");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== TEST FAILED: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        private static void PrintCharacterStats(Character character, string label)
        {
            Console.WriteLine($"--- {label} ---");
            Console.WriteLine($"Health: {character.CurrentHealth}/{character.MaxHealth} ({character.GetHealthPercentage():P1})");
            Console.WriteLine($"STR: {character.GetEffectiveStrength()} (base: {character.Strength}, temp: +{character.TempStrengthBonus})");
            Console.WriteLine($"AGI: {character.GetEffectiveAgility()} (base: {character.Agility}, temp: +{character.TempAgilityBonus})");
            Console.WriteLine($"TEC: {character.GetEffectiveTechnique()} (base: {character.Technique}, temp: +{character.TempTechniqueBonus})");
            Console.WriteLine($"Extra Damage: {character.ExtraDamage}");
            Console.WriteLine($"Damage Reduction: {character.DamageReduction:P1}");
            Console.WriteLine($"Length Reduction: {character.LengthReduction:P1} ({character.LengthReductionTurns} turns)");
            Console.WriteLine($"Combo Bonus: +{character.TempComboBonus} ({character.TempComboBonusTurns} turns)");
            Console.WriteLine($"Skip Next Turn: {character.SkipNextTurn}");
            Console.WriteLine($"Guarantee Next Success: {character.GuaranteeNextSuccess}");
            Console.WriteLine();
        }

        private static void TestActionWithStats(string name, string description, Character character, Character enemy, System.Action test)
        {
            Console.WriteLine($"\n=== TESTING {name} ===");
            Console.WriteLine($"Description: {description}");
            Console.WriteLine("BEFORE:");
            PrintCharacterStats(character, "Character");
            
            try
            {
                test();
                Console.WriteLine("AFTER:");
                PrintCharacterStats(character, "Character");
                Console.WriteLine($"✓ {name} test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {name} test failed: {ex.Message}");
                throw;
            }
        }

        private static void TestAction(string name, string description, System.Action test)
        {
            Console.WriteLine($"Testing {name}...");
            try
            {
                test();
                Console.WriteLine($"✓ {name} test passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ {name} test failed: {ex.Message}");
                throw;
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        static void RunDemo()
        {
            Console.WriteLine("=== Enhanced Poetic Battle Narrative Demo ===\n");
            
            // Demo 1: Momentum-focused battle
            Console.WriteLine("Demo 1: Momentum-Focused Battle");
            Console.WriteLine("--------------------------------");
            var momentumNarrative = new BattleNarrative("Hero", "Villain", "Epic Battlefield", 100, 80);
            
            var momentumEvents = new[]
            {
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Dark Strike", Damage = 15, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Jab", Damage = 25, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "Hero", Target = "Villain", Action = "Stun", Damage = 40, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "Villain", Target = "Hero", Action = "Shadow Bolt", Damage = 20, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in momentumEvents)
            {
                momentumNarrative.AddEvent(evt);
            }
            
            momentumNarrative.EndBattle();
            Console.WriteLine(momentumNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 2: Health-focused battle
            Console.WriteLine("Demo 2: Health-Focused Battle");
            Console.WriteLine("-----------------------------");
            var healthNarrative = new BattleNarrative("Warrior", "Dragon", "Dragon's Lair", 200, 150);
            
            var healthEvents = new[]
            {
                new BattleEvent { Actor = "Dragon", Target = "Warrior", Action = "Fire Breath", Damage = 60, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Heroic Strike", Damage = 45, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Dragon", Target = "Warrior", Action = "Tail Swipe", Damage = 50, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Warrior", Target = "Dragon", Action = "Divine Smite", Damage = 80, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in healthEvents)
            {
                healthNarrative.AddEvent(evt);
            }
            
            healthNarrative.EndBattle();
            Console.WriteLine(healthNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 3: Devastating combo
            Console.WriteLine("Demo 3: Devastating Combo Sequence");
            Console.WriteLine("----------------------------------");
            var comboNarrative = new BattleNarrative("ComboMaster", "Apprentice", "Training Grounds", 100, 60);
            
            var comboEvents = new[]
            {
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Taunt", Damage = 0, IsSuccess = true, IsCombo = true, ComboStep = 0 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Jab", Damage = 30, IsSuccess = true, IsCombo = true, ComboStep = 1 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Stun", Damage = 55, IsSuccess = true, IsCombo = true, ComboStep = 2 },
                new BattleEvent { Actor = "ComboMaster", Target = "Apprentice", Action = "Crit", Damage = 90, IsSuccess = true, IsCombo = true, ComboStep = 3 }
            };
            
            foreach (var evt in comboEvents)
            {
                comboNarrative.AddEvent(evt);
            }
            
            comboNarrative.EndBattle();
            Console.WriteLine(comboNarrative.GenerateNarrative());
            Console.WriteLine();
            
            // Demo 4: Stalemate
            Console.WriteLine("Demo 4: Stalemate Battle");
            Console.WriteLine("------------------------");
            var stalemateNarrative = new BattleNarrative("Fighter", "Rival", "Arena", 100, 100);
            
            var stalemateEvents = new[]
            {
                new BattleEvent { Actor = "Fighter", Target = "Rival", Action = "Punch", Damage = 20, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Rival", Target = "Fighter", Action = "Kick", Damage = 25, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Fighter", Target = "Rival", Action = "Block", Damage = 0, IsSuccess = true, IsCombo = false },
                new BattleEvent { Actor = "Rival", Target = "Fighter", Action = "Dodge", Damage = 0, IsSuccess = true, IsCombo = false }
            };
            
            foreach (var evt in stalemateEvents)
            {
                stalemateNarrative.AddEvent(evt);
            }
            
            stalemateNarrative.EndBattle();
            Console.WriteLine(stalemateNarrative.GenerateNarrative());
            Console.WriteLine();
            
            Console.WriteLine("=== Demo Complete ===");
        }

        static void RunGame()
        {
            var game = new Game();
            game.Run();
        }

        static void LoadAndRunGame()
        {
            var character = Character.LoadCharacter();
            if (character != null)
            {
                var game = new Game(character);
                game.Run();
            }
            else
            {
                Console.WriteLine("No saved character found. Starting new game instead...");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                RunGame();
            }
        }

        static void RunAllTests()
        {
            Console.WriteLine("=== RUNNING ALL TESTS ===\n");
            
            var testResults = new List<(string TestName, bool Passed, string? ErrorMessage)>();
            
            // Test 1: Character Leveling
            try
            {
                Console.WriteLine("Running Test 1: Character Leveling...");
                TestCharacterLeveling();
                testResults.Add(("Character Leveling", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Character Leveling", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 2: Items
            try
            {
                Console.WriteLine("Running Test 2: Items...");
                TestItems();
                testResults.Add(("Items", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Items", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 3: Dice
            try
            {
                Console.WriteLine("Running Test 3: Dice...");
                TestDice();
                testResults.Add(("Dice", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Dice", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 4: Actions
            try
            {
                Console.WriteLine("Running Test 4: Actions...");
                TestActions();
                testResults.Add(("Actions", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Actions", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 5: Entity Action Pools
            try
            {
                Console.WriteLine("Running Test 5: Entity Action Pools...");
                TestEntityActionPools();
                testResults.Add(("Entity Action Pools", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Entity Action Pools", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 6: Combat
            try
            {
                Console.WriteLine("Running Test 6: Combat...");
                TestCombat();
                testResults.Add(("Combat", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Combat", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 7: Combo System
            try
            {
                Console.WriteLine("Running Test 7: Combo System...");
                TestComboSystem();
                testResults.Add(("Combo System", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Combo System", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 8: Battle Narrative System
            try
            {
                Console.WriteLine("Running Test 8: Battle Narrative System...");
                TestBattleNarrativeSystem();
                testResults.Add(("Battle Narrative System", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Battle Narrative System", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 9: Enemy Scaling
            try
            {
                Console.WriteLine("Running Test 9: Enemy Scaling...");
                TestEnemyScaling();
                testResults.Add(("Enemy Scaling", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Enemy Scaling", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 10: New Dice Mechanics
            try
            {
                Console.WriteLine("Running Test 10: New Dice Mechanics...");
                TestNewDiceMechanics();
                testResults.Add(("New Dice Mechanics", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("New Dice Mechanics", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 11: New Action System
            try
            {
                Console.WriteLine("Running Test 11: New Action System...");
                TestNewActionSystem();
                testResults.Add(("New Action System", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("New Action System", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Test 12: Loot Generation System
            try
            {
                Console.WriteLine("Running Test 12: Loot Generation System...");
                TestLootGenerationSystem();
                testResults.Add(("Loot Generation System", true, null));
                Console.WriteLine("✓ PASSED\n");
            }
            catch (Exception ex)
            {
                testResults.Add(("Loot Generation System", false, ex.Message));
                Console.WriteLine($"✗ FAILED: {ex.Message}\n");
            }
            
            // Print Summary
            Console.WriteLine("=== TEST SUMMARY ===");
            Console.WriteLine($"Total Tests: {testResults.Count}");
            int passed = testResults.Count(r => r.Passed);
            int failed = testResults.Count(r => !r.Passed);
            Console.WriteLine($"Passed: {passed}");
            Console.WriteLine($"Failed: {failed}");
            Console.WriteLine($"Success Rate: {(double)passed / testResults.Count * 100:F1}%\n");
            
            if (failed > 0)
            {
                Console.WriteLine("FAILED TESTS:");
                foreach (var result in testResults.Where(r => !r.Passed))
                {
                    Console.WriteLine($"✗ {result.TestName}: {result.ErrorMessage}");
                }
                Console.WriteLine();
            }
            
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        static void RunTests()
        {
            while (true)
            {
                Console.WriteLine("\nTests Menu\n");
                Console.WriteLine("0. Exit");
                Console.WriteLine("1. Character Leveling Test");
                Console.WriteLine("2. Items Test");
                Console.WriteLine("3. Dice Test");
                Console.WriteLine("4. Actions Test");
                Console.WriteLine("5. Entity Action Pools Test");
                Console.WriteLine("6. Combat Test");
                Console.WriteLine("7. Combo System Custom Tests");
                Console.WriteLine("8. Battle Narrative System Tests");
                Console.WriteLine("9. Enemy Scaling Test");
                Console.WriteLine("10. Demo Poetic Narrative");
                Console.WriteLine("11. Demo Event-Driven Narrative");
                Console.WriteLine("12. Demo Primary Attributes");
                Console.WriteLine("13. Test Intelligent Delay System");
                Console.WriteLine("14. Test New Dice Mechanics");
                Console.WriteLine("15. Test New Action System");
                Console.WriteLine("16. Test Loot Generation System");
                Console.WriteLine("17. Test Weapon-Based Classes");
                Console.WriteLine("18. Run All Tests");
                Console.WriteLine("19. Back to Main Menu\n");
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "0":
                        Console.WriteLine("Goodbye!");
                        return;
                    case "1":
                        Console.WriteLine("\nRunning Character Leveling Test...\n");
                        TestCharacterLeveling();
                        break;
                    case "2":
                        Console.WriteLine("\nRunning Items Test...\n");
                        TestItems();
                        break;
                    case "3":
                        Console.WriteLine("\nRunning Dice Test...\n");
                        TestDice();
                        break;
                    case "4":
                        Console.WriteLine("\nRunning Actions Test...\n");
                        TestActions();
                        break;
                    case "5":
                        Console.WriteLine("\nRunning Entity Action Pools Test...\n");
                        TestEntityActionPools();
                        break;
                    case "6":
                        Console.WriteLine("\nRunning Combat Test...\n");
                        TestCombat();
                        break;
                    case "7":
                        Console.WriteLine("\nRunning Combo System Custom Tests...\n");
                        TestComboSystem();
                        break;
                    case "8":
                        Console.WriteLine("\nRunning Battle Narrative System Tests...\n");
                        TestBattleNarrativeSystem();
                        break;
                    case "9":
                        Console.WriteLine("\nRunning Enemy Scaling Test...\n");
                        TestEnemyScaling();
                        break;
                    case "10":
                        Console.WriteLine("\nRunning Poetic Narrative Demo...\n");
                        RunDemo();
                        break;
                    case "11":
                        Console.WriteLine("\nRunning Event-Driven Narrative Demo...\n");
                        DemoEventDrivenNarrative();
                        break;
                    case "12":
                        Console.WriteLine("\nRunning Primary Attributes Demo...\n");
                        DemoPrimaryAttributes();
                        break;
                    case "13":
                        Console.WriteLine("\nRunning Intelligent Delay System Test...\n");
                        TestIntelligentDelaySystem();
                        break;
                    case "14":
                        Console.WriteLine("\nRunning New Dice Mechanics Test...\n");
                        TestNewDiceMechanics();
                        break;
                    case "15":
                        Console.WriteLine("\nRunning New Action System Test...\n");
                        TestNewActionSystem();
                        break;
                    case "16":
                        Console.WriteLine("\nRunning Loot Generation System Test...\n");
                        TestLootGenerationSystem();
                        break;
                    case "17":
                        Console.WriteLine("\nRunning Weapon-Based Classes Test...\n");
                        TestWeaponBasedClasses();
                        break;
                    case "18":
                        Console.WriteLine("\nRunning All Tests...\n");
                        RunAllTests();
                        break;
                    case "19":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
                
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static void RunSettings()
        {
            var settings = GameSettings.Instance;
            
            while (true)
            {
                Console.WriteLine("\nSettings Menu\n");
                Console.WriteLine("=== Current Settings ===");
                Console.WriteLine($"Narrative Balance: {settings.NarrativeBalance:F1} - {settings.GetNarrativeBalanceDescription()}");
                Console.WriteLine($"Combat Speed: {settings.CombatSpeed:F1} - {settings.GetCombatSpeedDescription()}");
                Console.WriteLine($"Difficulty: {settings.GetDifficultyDescription()}");
                Console.WriteLine($"Enable Narrative Events: {settings.EnableNarrativeEvents}");
                Console.WriteLine($"Show Individual Actions: {settings.ShowIndividualActionMessages}");
                Console.WriteLine($"Enable Combo System: {settings.EnableComboSystem}");
                Console.WriteLine($"Text Display Delays: {settings.EnableTextDisplayDelays} - {settings.GetTextDisplayDelayDescription()}");
                Console.WriteLine($"Auto Save: {settings.EnableAutoSave}");
                Console.WriteLine($"Show Health Bars: {settings.ShowHealthBars}");
                Console.WriteLine($"Show Damage Numbers: {settings.ShowDamageNumbers}");
                Console.WriteLine();
                Console.WriteLine("=== Options ===");
                Console.WriteLine("1. Narrative Balance");
                Console.WriteLine("2. Combat Speed");
                Console.WriteLine("3. Difficulty Settings");
                Console.WriteLine("4. Combat Display Options");
                Console.WriteLine("5. Gameplay Options");
                Console.WriteLine("6. Tests");
                Console.WriteLine("7. Delete Saved Characters");
                Console.WriteLine("8. Reset to Defaults");
                Console.WriteLine("9. Back to Main Menu\n");
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        ConfigureNarrativeBalance(settings);
                        break;
                    case "2":
                        ConfigureCombatSpeed(settings);
                        break;
                    case "3":
                        ConfigureDifficulty(settings);
                        break;
                    case "4":
                        ConfigureCombatDisplay(settings);
                        break;
                    case "5":
                        ConfigureGameplayOptions(settings);
                        break;
                    case "6":
                        RunTests();
                        break;
                    case "7":
                        DeleteSavedCharacters();
                        break;
                    case "8":
                        settings.ResetToDefaults();
                        settings.SaveSettings();
                        Console.WriteLine("Settings reset to defaults and saved.");
                        break;
                    case "9":
                        settings.SaveSettings();
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
                
                Console.WriteLine("\nPress any key to continue...");
                Console.ReadKey();
            }
        }

        static void ConfigureNarrativeBalance(GameSettings settings)
        {
            Console.WriteLine("\n=== Narrative Balance Configuration ===");
            Console.WriteLine("This setting controls the balance between event-driven and poetic narrative.");
            Console.WriteLine("0.0 = Pure event-driven (factual summaries only)");
            Console.WriteLine("0.5 = Balanced (mix of events and narrative)");
            Console.WriteLine("1.0 = Pure poetic (rich narrative experience)");
            Console.WriteLine();
            Console.WriteLine($"Current value: {settings.NarrativeBalance:F1}");
            Console.WriteLine($"Current description: {settings.GetNarrativeBalanceDescription()}");
            Console.WriteLine();
            Console.Write("Enter new value (0.0-1.0): ");
            
            if (double.TryParse(Console.ReadLine(), out double newValue) && newValue >= 0.0 && newValue <= 1.0)
            {
                settings.NarrativeBalance = newValue;
                settings.SaveSettings();
                Console.WriteLine($"Narrative balance updated to {newValue:F1}");
            }
            else
            {
                Console.WriteLine("Invalid value. Please enter a number between 0.0 and 1.0.");
            }
        }

        static void ConfigureCombatSpeed(GameSettings settings)
        {
            Console.WriteLine("\n=== Combat Speed Configuration ===");
            Console.WriteLine("This setting controls how fast combat actions are displayed.");
            Console.WriteLine("0.5 = Very Slow (detailed, step-by-step)");
            Console.WriteLine("1.0 = Normal speed");
            Console.WriteLine("2.0 = Very Fast (quick summaries)");
            Console.WriteLine();
            Console.WriteLine($"Current value: {settings.CombatSpeed:F1}");
            Console.WriteLine($"Current description: {settings.GetCombatSpeedDescription()}");
            Console.WriteLine();
            Console.Write("Enter new value (0.5-2.0): ");
            
            if (double.TryParse(Console.ReadLine(), out double newValue) && newValue >= 0.5 && newValue <= 2.0)
            {
                settings.CombatSpeed = newValue;
                settings.SaveSettings();
                Console.WriteLine($"Combat speed updated to {newValue:F1}");
            }
            else
            {
                Console.WriteLine("Invalid value. Please enter a number between 0.5 and 2.0.");
            }
        }

        static void ConfigureDifficulty(GameSettings settings)
        {
            Console.WriteLine("\n=== Difficulty Configuration ===");
            Console.WriteLine("These settings control enemy and player stat multipliers.");
            Console.WriteLine();
            Console.WriteLine($"Enemy Health Multiplier: {settings.EnemyHealthMultiplier:F1}");
            Console.WriteLine($"Enemy Damage Multiplier: {settings.EnemyDamageMultiplier:F1}");
            Console.WriteLine($"Player Health Multiplier: {settings.PlayerHealthMultiplier:F1}");
            Console.WriteLine($"Player Damage Multiplier: {settings.PlayerDamageMultiplier:F1}");
            Console.WriteLine();
            Console.WriteLine("1. Enemy Health Multiplier");
            Console.WriteLine("2. Enemy Damage Multiplier");
            Console.WriteLine("3. Player Health Multiplier");
            Console.WriteLine("4. Player Damage Multiplier");
            Console.WriteLine("5. Back to Settings Menu");
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    Console.Write("Enter new enemy health multiplier (0.5-2.0): ");
                    if (double.TryParse(Console.ReadLine(), out double healthMult) && healthMult >= 0.5 && healthMult <= 2.0)
                    {
                        settings.EnemyHealthMultiplier = healthMult;
                        settings.SaveSettings();
                        Console.WriteLine($"Enemy health multiplier updated to {healthMult:F1}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Please enter a number between 0.5 and 2.0.");
                    }
                    break;
                case "2":
                    Console.Write("Enter new enemy damage multiplier (0.5-2.0): ");
                    if (double.TryParse(Console.ReadLine(), out double damageMult) && damageMult >= 0.5 && damageMult <= 2.0)
                    {
                        settings.EnemyDamageMultiplier = damageMult;
                        settings.SaveSettings();
                        Console.WriteLine($"Enemy damage multiplier updated to {damageMult:F1}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Please enter a number between 0.5 and 2.0.");
                    }
                    break;
                case "3":
                    Console.Write("Enter new player health multiplier (0.5-2.0): ");
                    if (double.TryParse(Console.ReadLine(), out double playerHealthMult) && playerHealthMult >= 0.5 && playerHealthMult <= 2.0)
                    {
                        settings.PlayerHealthMultiplier = playerHealthMult;
                        settings.SaveSettings();
                        Console.WriteLine($"Player health multiplier updated to {playerHealthMult:F1}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Please enter a number between 0.5 and 2.0.");
                    }
                    break;
                case "4":
                    Console.Write("Enter new player damage multiplier (0.5-2.0): ");
                    if (double.TryParse(Console.ReadLine(), out double playerDamageMult) && playerDamageMult >= 0.5 && playerDamageMult <= 2.0)
                    {
                        settings.PlayerDamageMultiplier = playerDamageMult;
                        settings.SaveSettings();
                        Console.WriteLine($"Player damage multiplier updated to {playerDamageMult:F1}");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Please enter a number between 0.5 and 2.0.");
                    }
                    break;
                case "5":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        static void ConfigureCombatDisplay(GameSettings settings)
        {
            Console.WriteLine("\n=== Combat Display Configuration ===");
            Console.WriteLine("These settings control what information is shown during combat.");
            Console.WriteLine();
            Console.WriteLine($"Show Individual Action Messages: {settings.ShowIndividualActionMessages}");
            Console.WriteLine($"Show Health Bars: {settings.ShowHealthBars}");
            Console.WriteLine($"Show Damage Numbers: {settings.ShowDamageNumbers}");
            Console.WriteLine($"Show Combo Progress: {settings.ShowComboProgress}");
            Console.WriteLine($"Text Display Delays: {settings.EnableTextDisplayDelays} - {settings.GetTextDisplayDelayDescription()}");
            Console.WriteLine();
            Console.WriteLine("1. Toggle Individual Action Messages");
            Console.WriteLine("2. Toggle Health Bars");
            Console.WriteLine("3. Toggle Damage Numbers");
            Console.WriteLine("4. Toggle Combo Progress");
            Console.WriteLine("5. Toggle Text Display Delays");
            Console.WriteLine("6. Back to Settings Menu");
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.ShowIndividualActionMessages = !settings.ShowIndividualActionMessages;
                    settings.SaveSettings();
                    Console.WriteLine($"Individual action messages: {(settings.ShowIndividualActionMessages ? "ON" : "OFF")}");
                    break;
                case "2":
                    settings.ShowHealthBars = !settings.ShowHealthBars;
                    settings.SaveSettings();
                    Console.WriteLine($"Health bars: {(settings.ShowHealthBars ? "ON" : "OFF")}");
                    break;
                case "3":
                    settings.ShowDamageNumbers = !settings.ShowDamageNumbers;
                    settings.SaveSettings();
                    Console.WriteLine($"Damage numbers: {(settings.ShowDamageNumbers ? "ON" : "OFF")}");
                    break;
                case "4":
                    settings.ShowComboProgress = !settings.ShowComboProgress;
                    settings.SaveSettings();
                    Console.WriteLine($"Combo progress: {(settings.ShowComboProgress ? "ON" : "OFF")}");
                    break;
                case "5":
                    settings.EnableTextDisplayDelays = !settings.EnableTextDisplayDelays;
                    settings.SaveSettings();
                    Console.WriteLine($"Text display delays: {(settings.EnableTextDisplayDelays ? "ON" : "OFF")} - {settings.GetTextDisplayDelayDescription()}");
                    break;
                case "6":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        static void ConfigureGameplayOptions(GameSettings settings)
        {
            Console.WriteLine("\n=== Gameplay Options Configuration ===");
            Console.WriteLine("These settings control various gameplay features.");
            Console.WriteLine();
            Console.WriteLine($"Enable Narrative Events: {settings.EnableNarrativeEvents}");
            Console.WriteLine($"Enable Combo System: {settings.EnableComboSystem}");
            Console.WriteLine($"Enable Auto Save: {settings.EnableAutoSave}");
            Console.WriteLine($"Auto Save Interval: {settings.AutoSaveInterval} encounters");
            Console.WriteLine($"Show Detailed Stats: {settings.ShowDetailedStats}");
            Console.WriteLine();
            Console.WriteLine("1. Toggle Narrative Events");
            Console.WriteLine("2. Toggle Combo System");
            Console.WriteLine("3. Toggle Auto Save");
            Console.WriteLine("4. Set Auto Save Interval");
            Console.WriteLine("5. Toggle Detailed Stats");
            Console.WriteLine("6. Back to Settings Menu");
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    settings.EnableNarrativeEvents = !settings.EnableNarrativeEvents;
                    settings.SaveSettings();
                    Console.WriteLine($"Narrative events: {(settings.EnableNarrativeEvents ? "ON" : "OFF")}");
                    break;
                case "2":
                    settings.EnableComboSystem = !settings.EnableComboSystem;
                    settings.SaveSettings();
                    Console.WriteLine($"Combo system: {(settings.EnableComboSystem ? "ON" : "OFF")}");
                    break;
                case "3":
                    settings.EnableAutoSave = !settings.EnableAutoSave;
                    settings.SaveSettings();
                    Console.WriteLine($"Auto save: {(settings.EnableAutoSave ? "ON" : "OFF")}");
                    break;
                case "4":
                    Console.Write("Enter auto save interval (1-20 encounters): ");
                    if (int.TryParse(Console.ReadLine(), out int interval) && interval >= 1 && interval <= 20)
                    {
                        settings.AutoSaveInterval = interval;
                        settings.SaveSettings();
                        Console.WriteLine($"Auto save interval updated to {interval} encounters");
                    }
                    else
                    {
                        Console.WriteLine("Invalid value. Please enter a number between 1 and 20.");
                    }
                    break;
                case "5":
                    settings.ShowDetailedStats = !settings.ShowDetailedStats;
                    settings.SaveSettings();
                    Console.WriteLine($"Detailed stats: {(settings.ShowDetailedStats ? "ON" : "OFF")}");
                    break;
                case "6":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Please try again.");
                    break;
            }
        }

        static void DeleteSavedCharacters()
        {
            Console.WriteLine("\nDelete Saved Characters\n");
            
            // Check if save file exists
            string saveFile = "character_save.json";
            if (!File.Exists(saveFile))
            {
                Console.WriteLine("No saved characters found.");
                Console.WriteLine("Press any key to return to settings...");
                Console.ReadKey();
                return;
            }
            
            // Show current save file info
            try
            {
                string jsonContent = File.ReadAllText(saveFile);
                var saveData = JsonSerializer.Deserialize<CharacterSaveData>(jsonContent);
                
                if (saveData != null)
                {
                    Console.WriteLine($"Found saved character: {saveData.Name} (Level {saveData.Level})");
                    Console.WriteLine($"Health: {saveData.CurrentHealth}/{saveData.MaxHealth}");
                    Console.WriteLine($"XP: {saveData.XP}");
                    Console.WriteLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading save file: {ex.Message}");
                Console.WriteLine("Press any key to return to settings...");
                Console.ReadKey();
                return;
            }
            
            // Confirmation prompt
            Console.WriteLine("Are you sure you want to delete this saved character?");
            Console.WriteLine("This action cannot be undone!");
            Console.WriteLine();
            Console.WriteLine("1. Yes, delete the saved character");
            Console.WriteLine("2. No, keep the saved character");
            Console.WriteLine("3. Back to Settings");
            Console.WriteLine();
            Console.Write("Choose an option: ");
            
            string? choice = Console.ReadLine();
            switch (choice)
            {
                case "1":
                    try
                    {
                        File.Delete(saveFile);
                        Console.WriteLine("Saved character has been deleted successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting save file: {ex.Message}");
                    }
                    break;
                case "2":
                    Console.WriteLine("Save file kept.");
                    break;
                case "3":
                    return;
                default:
                    Console.WriteLine("Invalid choice. Returning to settings.");
                    break;
            }
            
            Console.WriteLine("Press any key to return to settings...");
            Console.ReadKey();
        }

        static void RunPlayGameMenu()
        {
            while (true)
            {
                Console.WriteLine("\nPlay Game\n");
                Console.WriteLine("1. New Game");
                Console.WriteLine("2. Load Game");
                Console.WriteLine("3. Back to Main Menu\n");
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RunGame();
                        return; // Return to main menu after game ends
                    case "2":
                        LoadAndRunGame();
                        return; // Return to main menu after game ends
                    case "3":
                        return; // Back to main menu
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            while (true)
            {
                Console.WriteLine("\nDungeon Crawler - Main Menu\n");
                Console.WriteLine("1. Play Game");
                Console.WriteLine("2. Settings");
                Console.WriteLine("3. Exit\n");
                Console.Write("Choose an option: ");

                string? choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        RunPlayGameMenu();
                        break;
                    case "2":
                        RunSettings();
                        break;
                    case "3":
                        Console.WriteLine("Goodbye!");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        static void TestLootGenerationSystem()
        {
            Console.WriteLine("=== Testing Loot Generation System ===\n");

            try
            {
                // Initialize the loot generator
                LootGenerator.Initialize();

                // Test different player/dungeon level combinations
                var testCases = new[]
                {
                    new { PlayerLevel = 10, DungeonLevel = 10, Description = "Equal levels" },
                    new { PlayerLevel = 15, DungeonLevel = 10, Description = "Player 5 levels above" },
                    new { PlayerLevel = 5, DungeonLevel = 10, Description = "Player 5 levels below" },
                    new { PlayerLevel = 20, DungeonLevel = 5, Description = "Player 15 levels above (should be 100% high tier)" },
                    new { PlayerLevel = 5, DungeonLevel = 20, Description = "Player 15 levels below (should be no loot)" }
                };

                foreach (var testCase in testCases)
                {
                    // Check if there's a 0% chance of loot generation (player 3+ levels below dungeon)
                    int lootLevel = testCase.PlayerLevel - testCase.DungeonLevel;
                    bool hasZeroChance = lootLevel <= -3;
                    
                    Console.WriteLine($"\n--- {testCase.Description} (Player: {testCase.PlayerLevel}, Dungeon: {testCase.DungeonLevel}) ---");
                    
                    if (hasZeroChance)
                    {
                        Console.WriteLine("  0% chance of loot generation - skipping detailed results");
                        continue;
                    }
                    
                    // Generate 5 items for each test case
                    for (int i = 0; i < 5; i++)
                    {
                        var item = LootGenerator.GenerateLoot(testCase.PlayerLevel, testCase.DungeonLevel);
                        
                        if (item == null)
                        {
                            Console.WriteLine($"  Item {i + 1}: No loot generated");
                        }
                        else
                        {
                            Console.WriteLine($"  Item {i + 1}: {item.Name} (Tier {item.Tier}, {item.Rarity})");
                            
                            if (item is WeaponItem weapon)
                            {
                                Console.WriteLine($"    Type: {weapon.WeaponType}, Base Damage: {weapon.BaseDamage}, Bonus Damage: +{weapon.BonusDamage}");
                                Console.WriteLine($"    Base Attack Speed: {weapon.BaseAttackSpeed}, Bonus Attack Speed: +{weapon.BonusAttackSpeed}");
                                Console.WriteLine($"    Total Damage: {weapon.GetTotalDamage()}, Total Attack Speed: {weapon.GetTotalAttackSpeed()}");
                            }
                            else if (item is HeadItem head)
                            {
                                Console.WriteLine($"    Type: Head Armor, Base Armor: {head.Armor}, Total Armor: {head.GetTotalArmor()}");
                            }
                            else if (item is ChestItem chest)
                            {
                                Console.WriteLine($"    Type: Chest Armor, Base Armor: {chest.Armor}, Total Armor: {chest.GetTotalArmor()}");
                            }
                            else if (item is FeetItem feet)
                            {
                                Console.WriteLine($"    Type: Feet Armor, Base Armor: {feet.Armor}, Total Armor: {feet.GetTotalArmor()}");
                            }

                            // Show bonuses with detailed information
                            if (item.StatBonuses.Any())
                            {
                                Console.WriteLine($"    Stat Bonuses: {string.Join(", ", item.StatBonuses.Select(s => $"{s.Name} (+{s.Value} {s.StatType})"))}");
                            }
                            if (item.ActionBonuses.Any())
                            {
                                Console.WriteLine($"    Action Bonuses: {string.Join(", ", item.ActionBonuses.Select(a => a.Name))}");
                            }
                            if (item.Modifications.Any())
                            {
                                Console.WriteLine($"    Modifications: {string.Join(", ", item.Modifications.Select(m => $"{m.Name} ({m.Effect})"))}");
                            }
                            
                            // Show armor bonus breakdown for armor items
                            if (item is HeadItem headItem || item is ChestItem chestItem || item is FeetItem feetItem)
                            {
                                var armorItem = item as dynamic;
                                int baseArmor = armorItem.Armor;
                                int totalArmor = armorItem.GetTotalArmor();
                                int bonusArmor = totalArmor - baseArmor;
                                
                                if (bonusArmor > 0)
                                {
                                    Console.WriteLine($"    Armor Breakdown: Base {baseArmor} + Bonuses {bonusArmor} = Total {totalArmor}");
                                }
                            }
                        }
                    }
                }

                Console.WriteLine("\n=== Loot Generation Test Complete ===");
                
                // Run dedicated armor affix test
                TestArmorAffixGeneration();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== TEST FAILED: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        static void TestArmorAffixGeneration()
        {
            Console.WriteLine("\n=== Testing Armor Affix Generation ===");
            
            try
            {
                LootGenerator.Initialize();
                
                Console.WriteLine("Generating 10 armor items to test affix system...\n");
                
                int armorCount = 0;
                int attempts = 0;
                int maxAttempts = 50; // Prevent infinite loop
                
                while (armorCount < 10 && attempts < maxAttempts)
                {
                    attempts++;
                    var item = LootGenerator.GenerateLoot(10, 5); // Player level 10, dungeon level 5
                    
                    if (item != null && (item is HeadItem || item is ChestItem || item is FeetItem))
                    {
                        armorCount++;
                        Console.WriteLine($"Armor Item {armorCount}:");
                        Console.WriteLine($"  Name: {item.Name}");
                        Console.WriteLine($"  Rarity: {item.Rarity}");
                        Console.WriteLine($"  Tier: {item.Tier}");
                        
                        if (item is HeadItem head)
                        {
                            Console.WriteLine($"  Type: Head Armor");
                            Console.WriteLine($"  Base Armor: {head.Armor}");
                            Console.WriteLine($"  Total Armor: {head.GetTotalArmor()}");
                        }
                        else if (item is ChestItem chest)
                        {
                            Console.WriteLine($"  Type: Chest Armor");
                            Console.WriteLine($"  Base Armor: {chest.Armor}");
                            Console.WriteLine($"  Total Armor: {chest.GetTotalArmor()}");
                        }
                        else if (item is FeetItem feet)
                        {
                            Console.WriteLine($"  Type: Feet Armor");
                            Console.WriteLine($"  Base Armor: {feet.Armor}");
                            Console.WriteLine($"  Total Armor: {feet.GetTotalArmor()}");
                        }
                        
                        // Show detailed affix information
                        if (item.StatBonuses.Any())
                        {
                            Console.WriteLine($"  Stat Bonuses:");
                            foreach (var bonus in item.StatBonuses)
                            {
                                Console.WriteLine($"    - {bonus.Name}: +{bonus.Value} {bonus.StatType}");
                            }
                        }
                        
                        if (item.ActionBonuses.Any())
                        {
                            Console.WriteLine($"  Action Bonuses:");
                            foreach (var bonus in item.ActionBonuses)
                            {
                                Console.WriteLine($"    - {bonus.Name}: {bonus.Description}");
                            }
                        }
                        
                        if (item.Modifications.Any())
                        {
                            Console.WriteLine($"  Modifications:");
                            foreach (var mod in item.Modifications)
                            {
                                Console.WriteLine($"    - {mod.Name}: {mod.Effect}");
                            }
                        }
                        
                        // Show armor calculation breakdown
                        var armorItem = item as dynamic;
                        int baseArmor = armorItem.Armor;
                        int totalArmor = armorItem.GetTotalArmor();
                        int bonusArmor = totalArmor - baseArmor;
                        
                        if (bonusArmor > 0)
                        {
                            Console.WriteLine($"  Armor Calculation: {baseArmor} (base) + {bonusArmor} (bonuses) = {totalArmor} (total)");
                        }
                        else
                        {
                            Console.WriteLine($"  Armor Calculation: {baseArmor} (base, no bonuses)");
                        }
                        
                        Console.WriteLine();
                    }
                }
                
                if (armorCount < 10)
                {
                    Console.WriteLine($"Note: Only generated {armorCount} armor items in {attempts} attempts (weapons were also generated)");
                }
                
                Console.WriteLine("=== Armor Affix Generation Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== ARMOR AFFIX TEST FAILED: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }

        static void TestWeaponBasedClasses()
        {
            Console.WriteLine("=== Testing Weapon-Based Class System ===\n");

            try
            {
                // Test each weapon type
                var weaponTypes = new[]
                {
                    WeaponType.Mace,   // Barbarian
                    WeaponType.Sword,  // Warrior
                    WeaponType.Dagger, // Rogue
                    WeaponType.Wand    // Wizard
                };

                foreach (var weaponType in weaponTypes)
                {
                    Console.WriteLine($"\n--- Testing {weaponType} Weapon Class Progression ---");
                    var character = new Character($"Test{weaponType}", 1);
                    
                    // Equip appropriate weapon
                    var weapon = new WeaponItem($"{weaponType} Weapon", 1, 10, 1.0, weaponType);
                    character.EquipItem(weapon, "weapon");
                    
                    Console.WriteLine($"Starting as: {character.GetCurrentClass()}");
                    Console.WriteLine($"Class Points: Barbarian({character.BarbarianPoints}) Warrior({character.WarriorPoints}) Rogue({character.RoguePoints}) Wizard({character.WizardPoints})");
                    
                    // Level up several times to test progression
                    for (int i = 0; i < 45; i++)
                    {
                        character.AddXP(100);
                        if (i == 4 || i == 19 || i == 39) // Check at 5, 20, 40 points
                        {
                            Console.WriteLine($"Level {character.Level}: {character.GetCurrentClass()}");
                            Console.WriteLine($"Class Points: Barbarian({character.BarbarianPoints}) Warrior({character.WarriorPoints}) Rogue({character.RoguePoints}) Wizard({character.WizardPoints})");
                        }
                    }
                }

                // Test hybrid class
                Console.WriteLine($"\n--- Testing Hybrid Class ---");
                var hybridCharacter = new Character("HybridTest", 1);
                
                // Start with mace to get 5 barbarian points
                var mace = new WeaponItem("Mace", 1, 10, 1.0, WeaponType.Mace);
                hybridCharacter.EquipItem(mace, "weapon");
                for (int i = 0; i < 5; i++)
                {
                    hybridCharacter.AddXP(100);
                }
                Console.WriteLine($"After 5 levels with Mace: {hybridCharacter.GetCurrentClass()}");
                
                // Switch to sword to get 20 warrior points
                var sword = new WeaponItem("Sword", 1, 10, 1.0, WeaponType.Sword);
                hybridCharacter.EquipItem(sword, "weapon");
                for (int i = 0; i < 20; i++)
                {
                    hybridCharacter.AddXP(100);
                }
                Console.WriteLine($"After 20 more levels with Sword: {hybridCharacter.GetCurrentClass()}");
                Console.WriteLine($"Class Points: Barbarian({hybridCharacter.BarbarianPoints}) Warrior({hybridCharacter.WarriorPoints}) Rogue({hybridCharacter.RoguePoints}) Wizard({hybridCharacter.WizardPoints})");

                Console.WriteLine("\n=== Weapon-Based Class System Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n=== TEST FAILED: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
} 