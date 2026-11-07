using System;
using System.Collections.Generic;
using RPGGame;
using RPGGame.Combat.Calculators;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Combat
{
    /// <summary>Material set tempo/defense knobs: speed per worn piece, speed and damage reduction per unit minted this fight.</summary>
    public static class MaterialSetTempoTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Material Set Tempo Tests ===\n");
            _run = _passed = _failed = 0;
            MaterialBuildsLoader.Reload();

            var combat = GameConfiguration.Instance.Combat;
            var oldPiece = combat.MaterialSetSpeedPercentPerPiece;
            var oldBankSpeed = combat.MaterialFightMintSpeedPercentPerUnit;
            double oldBankSpeedMax = combat.MaterialFightMintSpeedMaxPercent;
            var oldBankDr = combat.MaterialFightMintDamageReductionPercentPerUnit;
            double oldBankDrMax = combat.MaterialFightMintDamageReductionMaxPercent;
            try
            {
                combat.MaterialSetSpeedPercentPerPiece = new(StringComparer.OrdinalIgnoreCase) { ["Iron"] = 6 };
                combat.MaterialFightMintSpeedPercentPerUnit = new(StringComparer.OrdinalIgnoreCase) { ["TRIUMPH"] = 5 };
                combat.MaterialFightMintSpeedMaxPercent = 20;
                combat.MaterialFightMintDamageReductionPercentPerUnit = new(StringComparer.OrdinalIgnoreCase) { ["RAGE"] = 4 };
                combat.MaterialFightMintDamageReductionMaxPercent = 30;

                TestSpeedPerPieceNeedsUnlockedSet();
                TestSpeedFeedsSwingBreakdown();
                TestBankSpeedIsCapped();
                TestBankDamageReductionIsCapped();
                TestOffWhenConfigEmpty();
                TestHeroAgilitySoftCap();
                TestSetDamageReductionAndAttributePerPiece();
                TestConvertGrantsPrimaryStatCapped();
                TestConvertPaysClassStatNotDamage();
            }
            finally
            {
                combat.MaterialSetSpeedPercentPerPiece = oldPiece;
                combat.MaterialFightMintSpeedPercentPerUnit = oldBankSpeed;
                combat.MaterialFightMintSpeedMaxPercent = oldBankSpeedMax;
                combat.MaterialFightMintDamageReductionPercentPerUnit = oldBankDr;
                combat.MaterialFightMintDamageReductionMaxPercent = oldBankDrMax;
            }

            TestBase.PrintSummary("Material Set Tempo Tests", _run, _passed, _failed);
        }

        private static Character EquipMaterial(string material, int count)
        {
            var hero = new Character("Hero", 1);
            var slots = new Item[]
            {
                new HeadItem("Helm", 1, 1),
                new ChestItem("Mail", 1, 1),
                new LegsItem("Greaves", 1, 1),
                new FeetItem("Boots", 1, 1),
                new WeaponItem("Mace", 1, 5, 1.0, WeaponType.Mace)
            };
            for (int i = 0; i < count; i++)
            {
                var item = slots[i];
                item.Material = material;
                switch (item.Type)
                {
                    case ItemType.Head: hero.Equipment.Head = item; break;
                    case ItemType.Chest: hero.Equipment.Body = item; break;
                    case ItemType.Legs: hero.Equipment.Legs = item; break;
                    case ItemType.Feet: hero.Equipment.Feet = item; break;
                    case ItemType.Weapon: hero.Equipment.Weapon = item; break;
                }
            }
            return hero;
        }

        private static void TestSpeedPerPieceNeedsUnlockedSet()
        {
            TestBase.SetCurrentTestName(nameof(TestSpeedPerPieceNeedsUnlockedSet));
            TestBase.AssertEqual(0.0, MaterialSetController.GetSetSpeedPercent(EquipMaterial("Iron", 1)), "1 Iron piece: set locked, no speed", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(18.0, MaterialSetController.GetSetSpeedPercent(EquipMaterial("Iron", 3)), "3 Iron pieces x 6%", ref _run, ref _passed, ref _failed);
            TestBase.AssertEqual(0.0, MaterialSetController.GetSetSpeedPercent(EquipMaterial("Bone", 3)), "Bone has no per-piece speed", ref _run, ref _passed, ref _failed);
        }

        private static void TestSpeedFeedsSwingBreakdown()
        {
            TestBase.SetCurrentTestName(nameof(TestSpeedFeedsSwingBreakdown));
            var hero = EquipMaterial("Iron", 5);
            double pct = MaterialSetController.GetSetSpeedPercent(hero);
            var breakdown = SpeedCalculator.GetBreakdown(hero);
            TestBase.AssertTrue(breakdown.SetSpeedPercent >= pct - 0.001, "swing breakdown carries the Iron set speed %", ref _run, ref _passed, ref _failed);
            var bare = EquipMaterial("Leather", 5);
            TestBase.AssertTrue(SpeedCalculator.GetBreakdown(bare).RawSeconds > breakdown.RawSeconds, "Iron set swings faster than the same gear in Leather", ref _run, ref _passed, ref _failed);
        }

        private static void TestBankSpeedIsCapped()
        {
            TestBase.SetCurrentTestName(nameof(TestBankSpeedIsCapped));
            var hero = EquipMaterial("Steel", 2);
            hero.Effects.AddMaterialKeyword("TRIUMPH", 3);
            TestBase.AssertEqual(0.0, MaterialSetController.GetSetSpeedPercent(hero), "banked but not minted this fight: no speed", ref _run, ref _passed, ref _failed);
            MaterialSetController.RecordFightMint(hero, "TRIUMPH", 3);
            TestBase.AssertEqual(15.0, MaterialSetController.GetSetSpeedPercent(hero), "3 TRIUMPH minted x 5%", ref _run, ref _passed, ref _failed);
            hero.Effects.AddMaterialKeyword("TRIUMPH", -3);
            TestBase.AssertEqual(15.0, MaterialSetController.GetSetSpeedPercent(hero), "spending the bank keeps the fight's speed", ref _run, ref _passed, ref _failed);
            MaterialSetController.RecordFightMint(hero, "TRIUMPH", 7);
            TestBase.AssertEqual(20.0, MaterialSetController.GetSetSpeedPercent(hero), "10 TRIUMPH capped at 20%", ref _run, ref _passed, ref _failed);
            MaterialSetController.ResetFightConnects(hero);
            TestBase.AssertEqual(0.0, MaterialSetController.GetSetSpeedPercent(hero), "new fight: speed resets", ref _run, ref _passed, ref _failed);
        }

        private static void TestBankDamageReductionIsCapped()
        {
            TestBase.SetCurrentTestName(nameof(TestBankDamageReductionIsCapped));
            var hero = EquipMaterial("Bone", 2);
            TestBase.AssertEqual(1.0, MaterialSetController.GetIncomingDamageMultiplier(hero), "no RAGE minted: full damage", ref _run, ref _passed, ref _failed);
            MaterialSetController.RecordFightMint(hero, "RAGE", 5);
            TestBase.AssertEqual(0.8, Math.Round(MaterialSetController.GetIncomingDamageMultiplier(hero), 4), "5 RAGE x 4% = 20% less", ref _run, ref _passed, ref _failed);
            MaterialSetController.RecordFightMint(hero, "RAGE", 20);
            TestBase.AssertEqual(0.7, Math.Round(MaterialSetController.GetIncomingDamageMultiplier(hero), 4), "25 RAGE capped at 30%", ref _run, ref _passed, ref _failed);
        }

        private static void TestHeroAgilitySoftCap()
        {
            TestBase.SetCurrentTestName(nameof(TestHeroAgilitySoftCap));
            var combat = GameConfiguration.Instance.Combat;
            int oldCap = combat.HeroAgilitySpeedSoftCap;
            double oldEff = combat.HeroAgilitySpeedOverCapEfficiency;
            bool oldExempt = combat.HeroAgilitySoftCapExemptsSets;
            try
            {
                combat.HeroAgilitySoftCapExemptsSets = false;
                combat.HeroAgilitySpeedSoftCap = 0;
                TestBase.AssertEqual(235, SpeedCalculator.GetHeroSpeedAgility(235), "cap off: AGI unchanged", ref _run, ref _passed, ref _failed);
                combat.HeroAgilitySpeedSoftCap = 120;
                combat.HeroAgilitySpeedOverCapEfficiency = 0.5;
                TestBase.AssertEqual(100, SpeedCalculator.GetHeroSpeedAgility(100), "under the cap: unchanged", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(178, SpeedCalculator.GetHeroSpeedAgility(235), "over the cap: 120 + 115 x 0.5", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(178, SpeedCalculator.GetHeroSpeedAgility(235, 40), "exemption off: set AGI still capped", ref _run, ref _passed, ref _failed);
                combat.HeroAgilitySoftCapExemptsSets = true;
                TestBase.AssertEqual(198, SpeedCalculator.GetHeroSpeedAgility(235, 40), "exempt: 120 + 75 x 0.5 + 40 set AGI", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(140, SpeedCalculator.GetHeroSpeedAgility(140, 40), "exempt: under the cap without set AGI", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.HeroAgilitySpeedSoftCap = oldCap;
                combat.HeroAgilitySpeedOverCapEfficiency = oldEff;
                combat.HeroAgilitySoftCapExemptsSets = oldExempt;
            }
        }

        private static void TestSetDamageReductionAndAttributePerPiece()
        {
            TestBase.SetCurrentTestName(nameof(TestSetDamageReductionAndAttributePerPiece));
            var combat = GameConfiguration.Instance.Combat;
            var oldDr = combat.MaterialSetDamageReductionPercentPerPiece;
            var oldAttr = combat.MaterialSetAttributePerPiece;
            try
            {
                combat.MaterialSetDamageReductionPercentPerPiece = new(StringComparer.OrdinalIgnoreCase) { ["Bronze"] = 4 };
                combat.MaterialSetAttributePerPiece = new(StringComparer.OrdinalIgnoreCase) { ["Bone"] = 5 };
                TestBase.AssertEqual(1.0, MaterialSetController.GetIncomingDamageMultiplier(EquipMaterial("Bronze", 1)), "1 Bronze piece: set locked", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0.88, Math.Round(MaterialSetController.GetIncomingDamageMultiplier(EquipMaterial("Bronze", 3)), 4), "3 Bronze x 4% DR", ref _run, ref _passed, ref _failed);

                var bone = EquipMaterial("Bone", 3);
                TestBase.AssertEqual(15, MaterialSetController.GetSetAttributeBonus(bone, "STR"), "3 Bone x 5 STR (Barbarian stat)", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0, MaterialSetController.GetSetAttributeBonus(bone, "INT"), "Bone pays no INT", ref _run, ref _passed, ref _failed);
                var plain = EquipMaterial("Leather", 3);
                TestBase.AssertEqual(plain.GetEffectiveStrength() + 15, bone.GetEffectiveStrength(), "set STR reaches effective STR", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.MaterialSetDamageReductionPercentPerPiece = oldDr;
                combat.MaterialSetAttributePerPiece = oldAttr;
            }
        }

        private static void TestConvertGrantsPrimaryStatCapped()
        {
            TestBase.SetCurrentTestName(nameof(TestConvertGrantsPrimaryStatCapped));
            var combat = GameConfiguration.Instance.Combat;
            var oldPer = combat.MaterialConvertPrimaryStatPerUnit;
            int oldCap = combat.MaterialConvertPrimaryStatMaxPerFight;
            try
            {
                combat.MaterialConvertPrimaryStatPerUnit = new(StringComparer.OrdinalIgnoreCase) { ["RAGE"] = 0.5 };
                combat.MaterialConvertPrimaryStatMaxPerFight = 6;
                var hero = EquipMaterial("Bone", 2);
                int before = hero.GetEffectiveStrength() + hero.GetEffectiveAgility() + hero.GetEffectiveTechnique() + hero.GetEffectiveIntelligence();
                TestBase.AssertEqual(4, MaterialSetController.GrantConvertPrimaryStat(hero, "RAGE", 8), "8 RAGE x 0.5 = 4 primary", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(2, MaterialSetController.GrantConvertPrimaryStat(hero, "RAGE", 8), "fight cap 6 leaves 2", ref _run, ref _passed, ref _failed);
                int after = hero.GetEffectiveStrength() + hero.GetEffectiveAgility() + hero.GetEffectiveTechnique() + hero.GetEffectiveIntelligence();
                TestBase.AssertEqual(before + 6, after, "granted points reach the hero's stats", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(0, MaterialSetController.GrantConvertPrimaryStat(hero, "CONTACT", 8), "unlisted currency grants nothing", ref _run, ref _passed, ref _failed);
                MaterialSetController.ResetFightConnects(hero);
                TestBase.AssertEqual(4, MaterialSetController.GrantConvertPrimaryStat(hero, "RAGE", 8), "new fight: cap resets", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.MaterialConvertPrimaryStatPerUnit = oldPer;
                combat.MaterialConvertPrimaryStatMaxPerFight = oldCap;
            }
        }

        private static void TestConvertPaysClassStatNotDamage()
        {
            TestBase.SetCurrentTestName(nameof(TestConvertPaysClassStatNotDamage));
            var combat = GameConfiguration.Instance.Combat;
            var oldPer = combat.MaterialConvertPrimaryStatPerUnit;
            int oldCap = combat.MaterialConvertPrimaryStatMaxPerFight;
            bool oldPays = combat.MaterialConvertPaysDamage;
            var oldPct = combat.MaterialConvertPercentByKeyword;
            double oldPctUnit = combat.MaterialConvertPercentPerUnit;
            try
            {
                combat.MaterialConvertPrimaryStatPerUnit = new(StringComparer.OrdinalIgnoreCase) { ["RAGE"] = 0.4, ["GRAZE"] = 1 };
                combat.MaterialConvertPrimaryStatMaxPerFight = 0;
                combat.MaterialConvertPercentPerUnit = 1;
                combat.MaterialConvertPercentByKeyword = new(StringComparer.OrdinalIgnoreCase) { ["RAGE"] = 5 };
                TestBase.AssertEqual("STR", MaterialSetController.GetConvertStatCode(null, "RAGE"), "RAGE (Bone, Barbarian) pays STR", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual("TEC", MaterialSetController.GetConvertStatCode(null, "GRAZE"), "GRAZE (Glass, Rogue) pays TEC", ref _run, ref _passed, ref _failed);

                var hero = EquipMaterial("Bone", 2);
                hero.Effects.AddMaterialKeyword("RAGE", 4);
                var convert = new RPGGame.Action { Name = "BONE WRATH" };
                TestBase.AssertTrue(MaterialSetController.GetConvertDamageBonus(hero, convert, 100) > 0, "damage on: convert adds damage", ref _run, ref _passed, ref _failed);
                combat.MaterialConvertPaysDamage = false;
                TestBase.AssertEqual(0, MaterialSetController.GetConvertDamageBonus(hero, convert, 100), "damage off: convert adds no damage", ref _run, ref _passed, ref _failed);
                TestBase.AssertTrue(MaterialSetController.FormatConvertRateText(convert)?.Contains("STR") == true, "damage off: card text names STR", ref _run, ref _passed, ref _failed);

                int str = hero.GetEffectiveStrength();
                TestBase.AssertEqual(1, MaterialSetController.GrantConvertPrimaryStat(hero, "RAGE", 3), "3 RAGE x 0.4 = 1.2 -> 1", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(1, MaterialSetController.GrantConvertPrimaryStat(hero, "RAGE", 2), "carried 0.2 + 0.8 -> 1 more", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(str + 2, hero.GetEffectiveStrength(), "grants land on STR", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.MaterialConvertPrimaryStatPerUnit = oldPer;
                combat.MaterialConvertPrimaryStatMaxPerFight = oldCap;
                combat.MaterialConvertPaysDamage = oldPays;
                combat.MaterialConvertPercentByKeyword = oldPct;
                combat.MaterialConvertPercentPerUnit = oldPctUnit;
            }
        }

        private static void TestOffWhenConfigEmpty()
        {
            TestBase.SetCurrentTestName(nameof(TestOffWhenConfigEmpty));
            var combat = GameConfiguration.Instance.Combat;
            var piece = combat.MaterialSetSpeedPercentPerPiece;
            var bank = combat.MaterialFightMintSpeedPercentPerUnit;
            var dr = combat.MaterialFightMintDamageReductionPercentPerUnit;
            combat.MaterialSetSpeedPercentPerPiece = new();
            combat.MaterialFightMintSpeedPercentPerUnit = new();
            combat.MaterialFightMintDamageReductionPercentPerUnit = new();
            try
            {
                var hero = EquipMaterial("Iron", 5);
                MaterialSetController.RecordFightMint(hero, "RAGE", 5);
                MaterialSetController.RecordFightMint(hero, "TRIUMPH", 5);
                TestBase.AssertEqual(0.0, MaterialSetController.GetSetSpeedPercent(hero), "no knobs: no speed", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(1.0, MaterialSetController.GetIncomingDamageMultiplier(hero), "no knobs: full damage", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.MaterialSetSpeedPercentPerPiece = piece;
                combat.MaterialFightMintSpeedPercentPerUnit = bank;
                combat.MaterialFightMintDamageReductionPercentPerUnit = dr;
            }
        }
    }
}
