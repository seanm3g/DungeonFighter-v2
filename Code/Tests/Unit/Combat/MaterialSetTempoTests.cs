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
            try
            {
                combat.HeroAgilitySpeedSoftCap = 0;
                TestBase.AssertEqual(235, SpeedCalculator.GetHeroSpeedAgility(235), "cap off: AGI unchanged", ref _run, ref _passed, ref _failed);
                combat.HeroAgilitySpeedSoftCap = 120;
                combat.HeroAgilitySpeedOverCapEfficiency = 0.5;
                TestBase.AssertEqual(100, SpeedCalculator.GetHeroSpeedAgility(100), "under the cap: unchanged", ref _run, ref _passed, ref _failed);
                TestBase.AssertEqual(178, SpeedCalculator.GetHeroSpeedAgility(235), "over the cap: 120 + 115 x 0.5", ref _run, ref _passed, ref _failed);
            }
            finally
            {
                combat.HeroAgilitySpeedSoftCap = oldCap;
                combat.HeroAgilitySpeedOverCapEfficiency = oldEff;
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
