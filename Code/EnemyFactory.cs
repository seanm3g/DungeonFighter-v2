using System;
using System.Collections.Generic;

namespace RPGGame
{
    public static class EnemyFactory
    {
        public static Enemy CreateEnemy(string enemyType, int level = 1)
        {
            // Try to create enemy from JSON data first
            var enemy = EnemyLoader.CreateEnemy(enemyType, level);
            if (enemy != null)
            {
                return enemy;
            }

            // Fallback to hardcoded creation if not found in JSON
            return enemyType.ToLower() switch
            {
                "goblin" => CreateGoblin(level),
                "orc" => CreateOrc(level),
                "skeleton" => CreateSkeleton(level),
                "zombie" => CreateZombie(level),
                "wraith" => CreateWraith(level),
                "spider" => CreateSpider(level),
                "slime" => CreateSlime(level),
                "bat" => CreateBat(level),
                "bandit" => CreateBandit(level),
                "cultist" => CreateCultist(level),
                "troll" => CreateTroll(level),
                "dragon" => CreateDragon(level),
                "ghost" => CreateGhost(level),
                "vampire" => CreateVampire(level),
                "werewolf" => CreateWerewolf(level),
                "gargoyle" => CreateGargoyle(level),
                "mimic" => CreateMimic(level),
                "elemental" => CreateElemental(level),
                _ => CreateGoblin(level) // Default fallback
            };
        }

        private static Enemy CreateGoblin(int level)
        {
            var enemy = new Enemy("Goblin", level, 30 + level * 5, 8 + level, 12 + level, 4 + level);
            enemy.ActionPool.Clear();
            
            var quickStab = ActionLoader.GetAction("Quick Stab");
            var dirtyTrick = ActionLoader.GetAction("Dirty Trick");
            var retreat = ActionLoader.GetAction("Retreat");
            
            if (quickStab != null) enemy.AddAction(quickStab, 0.6);
            if (dirtyTrick != null) enemy.AddAction(dirtyTrick, 0.3);
            if (retreat != null) enemy.AddAction(retreat, 0.1);
            
            return enemy;
        }

        private static Enemy CreateOrc(int level)
        {
            var enemy = new Enemy("Orc", level, 50 + level * 8, 15 + level * 2, 8 + level, 6 + level);
            enemy.ActionPool.Clear();
            
            var heavySwing = ActionLoader.GetAction("Heavy Swing");
            var battleRage = ActionLoader.GetAction("Battle Rage");
            var intimidatingRoar = ActionLoader.GetAction("Intimidating Roar");
            
            if (heavySwing != null) enemy.AddAction(heavySwing, 0.7);
            if (battleRage != null) enemy.AddAction(battleRage, 0.2);
            if (intimidatingRoar != null) enemy.AddAction(intimidatingRoar, 0.1);
            
            return enemy;
        }

        private static Enemy CreateSkeleton(int level)
        {
            var enemy = new Enemy("Skeleton", level, 35 + level * 6, 10 + level, 8 + level, 6 + level);
            enemy.ActionPool.Clear();
            
            var boneThrow = ActionLoader.GetAction("Bone Throw");
            var rattlingBones = ActionLoader.GetAction("Rattling Bones");
            var reassemble = ActionLoader.GetAction("Reassemble");
            
            if (boneThrow != null) enemy.AddAction(boneThrow, 0.6);
            if (rattlingBones != null) enemy.AddAction(rattlingBones, 0.3);
            if (reassemble != null) enemy.AddAction(reassemble, 0.1);
            
            return enemy;
        }

        private static Enemy CreateZombie(int level)
        {
            var enemy = new Enemy("Zombie", level, 45 + level * 7, 12 + level, 4 + level, 2 + level);
            enemy.ActionPool.Clear();
            
            var slowGrab = ActionLoader.GetAction("Slow Grab");
            var infectiousBite = ActionLoader.GetAction("Infectious Bite");
            var undeadResilience = ActionLoader.GetAction("Undead Resilience");
            
            if (slowGrab != null) enemy.AddAction(slowGrab, 0.7);
            if (infectiousBite != null) enemy.AddAction(infectiousBite, 0.2);
            if (undeadResilience != null) enemy.AddAction(undeadResilience, 0.1);
            
            return enemy;
        }

        private static Enemy CreateWraith(int level)
        {
            var enemy = new Enemy("Wraith", level, 25 + level * 4, 6 + level, 14 + level, 16 + level);
            enemy.ActionPool.Clear();
            
            var etherealTouch = ActionLoader.GetAction("Ethereal Touch");
            var hauntingWail = ActionLoader.GetAction("Haunting Wail");
            var phaseShift = ActionLoader.GetAction("Phase Shift");
            
            if (etherealTouch != null) enemy.AddAction(etherealTouch, 0.5);
            if (hauntingWail != null) enemy.AddAction(hauntingWail, 0.3);
            if (phaseShift != null) enemy.AddAction(phaseShift, 0.2);
            
            return enemy;
        }

        private static Enemy CreateSpider(int level)
        {
            var enemy = new Enemy("Spider", level, 30 + level * 5, 8 + level, 16 + level, 8 + level);
            enemy.ActionPool.Clear();
            
            var venomousBite = ActionLoader.GetAction("Venomous Bite");
            var webTrap = ActionLoader.GetAction("Web Trap");
            var skitter = ActionLoader.GetAction("Skitter");
            
            if (venomousBite != null) enemy.AddAction(venomousBite, 0.6);
            if (webTrap != null) enemy.AddAction(webTrap, 0.3);
            if (skitter != null) enemy.AddAction(skitter, 0.1);
            
            return enemy;
        }

        private static Enemy CreateSlime(int level)
        {
            var enemy = new Enemy("Slime", level, 40 + level * 6, 6 + level, 6 + level, 10 + level);
            enemy.ActionPool.Clear();
            
            var acidicSplash = ActionLoader.GetAction("Acidic Splash");
            var absorb = ActionLoader.GetAction("Absorb");
            var split = ActionLoader.GetAction("Split");
            
            if (acidicSplash != null) enemy.AddAction(acidicSplash, 0.6);
            if (absorb != null) enemy.AddAction(absorb, 0.3);
            if (split != null) enemy.AddAction(split, 0.1);
            
            return enemy;
        }

        private static Enemy CreateBat(int level)
        {
            var enemy = new Enemy("Bat", level, 20 + level * 3, 6 + level, 18 + level, 4 + level);
            enemy.ActionPool.Clear();
            
            var sonicScream = ActionLoader.GetAction("Sonic Scream");
            var diveBomb = ActionLoader.GetAction("Dive Bomb");
            var echolocation = ActionLoader.GetAction("Echolocation");
            
            if (sonicScream != null) enemy.AddAction(sonicScream, 0.4);
            if (diveBomb != null) enemy.AddAction(diveBomb, 0.5);
            if (echolocation != null) enemy.AddAction(echolocation, 0.1);
            
            return enemy;
        }

        private static Enemy CreateBandit(int level)
        {
            var enemy = new Enemy("Bandit", level, 35 + level * 5, 10 + level, 12 + level, 8 + level);
            enemy.ActionPool.Clear();
            
            var backstab = ActionLoader.GetAction("Backstab");
            var smokeBomb = ActionLoader.GetAction("Smoke Bomb");
            var adrenalineRush = ActionLoader.GetAction("Adrenaline Rush");
            
            if (backstab != null) enemy.AddAction(backstab, 0.6);
            if (smokeBomb != null) enemy.AddAction(smokeBomb, 0.3);
            if (adrenalineRush != null) enemy.AddAction(adrenalineRush, 0.1);
            
            return enemy;
        }

        private static Enemy CreateCultist(int level)
        {
            var enemy = new Enemy("Cultist", level, 30 + level * 4, 8 + level, 10 + level, 14 + level);
            enemy.ActionPool.Clear();
            
            var darkRitual = ActionLoader.GetAction("Dark Ritual");
            var summonShadows = ActionLoader.GetAction("Summon Shadows");
            var curse = ActionLoader.GetAction("Curse");
            
            if (darkRitual != null) enemy.AddAction(darkRitual, 0.5);
            if (summonShadows != null) enemy.AddAction(summonShadows, 0.3);
            if (curse != null) enemy.AddAction(curse, 0.2);
            
            return enemy;
        }

        private static Enemy CreateTroll(int level)
        {
            var enemy = new Enemy("Troll", level, 80 + level * 12, 18 + level * 2, 6 + level, 4 + level);
            enemy.ActionPool.Clear();
            
            var clubSmash = ActionLoader.GetAction("Club Smash");
            var regeneration = ActionLoader.GetAction("Regeneration");
            var groundStomp = ActionLoader.GetAction("Ground Stomp");
            
            if (clubSmash != null) enemy.AddAction(clubSmash, 0.6);
            if (regeneration != null) enemy.AddAction(regeneration, 0.2);
            if (groundStomp != null) enemy.AddAction(groundStomp, 0.2);
            
            return enemy;
        }

        private static Enemy CreateDragon(int level)
        {
            var enemy = new Enemy("Dragon", level, 120 + level * 20, 20 + level * 3, 12 + level, 16 + level);
            enemy.ActionPool.Clear();
            
            var fireBreath = ActionLoader.GetAction("Fire Breath");
            var wingBuffet = ActionLoader.GetAction("Wing Buffet");
            var dragonRage = ActionLoader.GetAction("Dragon Rage");
            
            if (fireBreath != null) enemy.AddAction(fireBreath, 0.4);
            if (wingBuffet != null) enemy.AddAction(wingBuffet, 0.4);
            if (dragonRage != null) enemy.AddAction(dragonRage, 0.2);
            
            return enemy;
        }

        private static Enemy CreateGhost(int level)
        {
            var enemy = new Enemy("Ghost", level, 25 + level * 4, 4 + level, 16 + level, 18 + level);
            enemy.ActionPool.Clear();
            
            var possession = ActionLoader.GetAction("Possession");
            var ghostlyWail = ActionLoader.GetAction("Ghostly Wail");
            var fadeAway = ActionLoader.GetAction("Fade Away");
            
            if (possession != null) enemy.AddAction(possession, 0.3);
            if (ghostlyWail != null) enemy.AddAction(ghostlyWail, 0.5);
            if (fadeAway != null) enemy.AddAction(fadeAway, 0.2);
            
            return enemy;
        }

        private static Enemy CreateVampire(int level)
        {
            var enemy = new Enemy("Vampire", level, 50 + level * 8, 12 + level, 14 + level, 12 + level);
            enemy.ActionPool.Clear();
            
            var bloodDrain = ActionLoader.GetAction("Blood Drain");
            var hypnoticGaze = ActionLoader.GetAction("Hypnotic Gaze");
            var batForm = ActionLoader.GetAction("Bat Form");
            
            if (bloodDrain != null) enemy.AddAction(bloodDrain, 0.6);
            if (hypnoticGaze != null) enemy.AddAction(hypnoticGaze, 0.3);
            if (batForm != null) enemy.AddAction(batForm, 0.1);
            
            return enemy;
        }

        private static Enemy CreateWerewolf(int level)
        {
            var enemy = new Enemy("Werewolf", level, 60 + level * 10, 16 + level * 2, 14 + level, 8 + level);
            enemy.ActionPool.Clear();
            
            var savageClaw = ActionLoader.GetAction("Savage Claw");
            var howl = ActionLoader.GetAction("Howl");
            var pounce = ActionLoader.GetAction("Pounce");
            
            if (savageClaw != null) enemy.AddAction(savageClaw, 0.6);
            if (howl != null) enemy.AddAction(howl, 0.2);
            if (pounce != null) enemy.AddAction(pounce, 0.2);
            
            return enemy;
        }

        private static Enemy CreateGargoyle(int level)
        {
            var enemy = new Enemy("Gargoyle", level, 70 + level * 10, 14 + level * 2, 8 + level, 10 + level);
            enemy.ActionPool.Clear();
            
            var stoneFist = ActionLoader.GetAction("Stone Fist");
            var petrify = ActionLoader.GetAction("Petrify");
            var stoneSkin = ActionLoader.GetAction("Stone Skin");
            
            if (stoneFist != null) enemy.AddAction(stoneFist, 0.6);
            if (petrify != null) enemy.AddAction(petrify, 0.2);
            if (stoneSkin != null) enemy.AddAction(stoneSkin, 0.2);
            
            return enemy;
        }

        private static Enemy CreateMimic(int level)
        {
            var enemy = new Enemy("Mimic", level, 45 + level * 7, 10 + level, 8 + level, 12 + level);
            enemy.ActionPool.Clear();
            
            var surpriseBite = ActionLoader.GetAction("Surprise Bite");
            var adhesiveTrap = ActionLoader.GetAction("Adhesive Trap");
            var disguise = ActionLoader.GetAction("Disguise");
            
            if (surpriseBite != null) enemy.AddAction(surpriseBite, 0.7);
            if (adhesiveTrap != null) enemy.AddAction(adhesiveTrap, 0.2);
            if (disguise != null) enemy.AddAction(disguise, 0.1);
            
            return enemy;
        }

        private static Enemy CreateElemental(int level)
        {
            var enemy = new Enemy("Elemental", level, 40 + level * 6, 8 + level, 10 + level, 16 + level);
            enemy.ActionPool.Clear();
            
            var elementalBlast = ActionLoader.GetAction("Elemental Blast");
            var elementalShield = ActionLoader.GetAction("Elemental Shield");
            var elementalStorm = ActionLoader.GetAction("Elemental Storm");
            
            if (elementalBlast != null) enemy.AddAction(elementalBlast, 0.5);
            if (elementalShield != null) enemy.AddAction(elementalShield, 0.3);
            if (elementalStorm != null) enemy.AddAction(elementalStorm, 0.2);
            
            return enemy;
        }
    }
} 