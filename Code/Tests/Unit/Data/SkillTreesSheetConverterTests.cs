using System;
using System.Linq;
using RPGGame;
using RPGGame.Data;
using RPGGame.Tests;

namespace RPGGame.Tests.Unit.Data
{
    public static class SkillTreesSheetConverterTests
    {
        public static void RunAllTests()
        {
            int run = 0, pass = 0, fail = 0;
            Console.WriteLine("=== SkillTreesSheetConverter Tests ===\n");

            ParseAlignedRows(ref run, ref pass, ref fail);
            HealShiftWhenRequiresOmitted(ref run, ref pass, ref fail);
            HealShiftWhenUnlockOmitted(ref run, ref pass, ref fail);
            ParseUnlockActionAndRequiresList(ref run, ref pass, ref fail);
            PreserveIdentityFromPrior(ref run, ref pass, ref fail);
            RoundTripPushPull(ref run, ref pass, ref fail);
            PromoteLevelOneAsRootOnNormalize(ref run, ref pass, ref fail);

            TestBase.PrintSummary("SkillTreesSheetConverter Tests", run, pass, fail);
        }

        private static void ParseAlignedRows(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ParseAlignedRows));
            string fixedCsv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Bronze Skin,Root effect,Root payoff,,,Passive,STR,b-root,Core,0,0,bronze_skin\n";

            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(fixedCsv);
            TestBase.AssertEqual(1, cfg.Trees.Count, "one tree", ref run, ref pass, ref fail);
            var node = cfg.Trees[0].Nodes.Single();
            TestBase.AssertEqual("b-root", node.Id, "id", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Passive", node.Type, "type", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Core", node.Branch, "branch", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, node.Cost, "cost", ref run, ref pass, ref fail);
            TestBase.AssertEqual("bronze_skin", node.CustomEffectId, "customEffectId", ref run, ref pass, ref fail);
            TestBase.AssertEqual("STR", cfg.Trees[0].Stat, "stat", ref run, ref pass, ref fail);
        }

        private static void HealShiftWhenRequiresOmitted(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(HealShiftWhenRequiresOmitted));
            // UnlockAction empty present; Requires empty omitted → Type sits in Requires column
            string csv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Puberty,+15 STR,Rite payoff,,Passive,STR,b-puberty,Rite,0,0,puberty,\n";

            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(csv);
            var node = cfg.Trees.Single().Nodes.Single();
            TestBase.AssertEqual("b-puberty", node.Id, "id after heal", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Passive", node.Type, "type after heal", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Rite", node.Branch, "branch after heal", ref run, ref pass, ref fail);
            TestBase.AssertEqual("puberty", node.CustomEffectId, "customEffectId after heal", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, node.Requires.Count, "no requires", ref run, ref pass, ref fail);
        }

        private static void HealShiftWhenUnlockOmitted(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(HealShiftWhenUnlockOmitted));
            // UnlockAction omitted; Requires (b-root) in UnlockAction column
            string csv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Level 1,Tags count,Payoff,b-root,Passive,STR,b-tribe,Alloy,1,4,tribe_metal,\n";

            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(csv);
            var node = cfg.Trees.Single().Nodes.Single();
            TestBase.AssertEqual("b-tribe", node.Id, "id", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Passive", node.Type, "type", ref run, ref pass, ref fail);
            TestBase.AssertEqual(1, node.Requires.Count, "requires count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("b-root", node.Requires[0], "requires", ref run, ref pass, ref fail);
            TestBase.AssertTrue(string.IsNullOrEmpty(node.UnlockActionName), "no unlock action", ref run, ref pass, ref fail);
            TestBase.AssertEqual(4, node.Cost, "cost", ref run, ref pass, ref fail);
        }

        private static void ParseUnlockActionAndRequiresList(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(ParseUnlockActionAndRequiresList));
            string csv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Mighty Swing,Finisher,Payoff,MIGHTY SWING,b-bludgeon,Action,STR,b-mighty,Impact,2,8,mighty_swing\n" +
                "Barbarian,Bronze Skin,Mace,Living Alloy,Rule effect,Payoff,,\"b-age1,b-memory\",Rule,STR,b-alloy,Confluence,4,21,living_alloy\n";

            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(csv);
            TestBase.AssertEqual(2, cfg.Trees.Single().Nodes.Count, "two nodes", ref run, ref pass, ref fail);
            var mighty = cfg.Trees[0].Nodes.First(n => n.Id == "b-mighty");
            TestBase.AssertEqual("MIGHTY SWING", mighty.UnlockActionName, "unlock", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Action", mighty.Type, "action type", ref run, ref pass, ref fail);
            var alloy = cfg.Trees[0].Nodes.First(n => n.Id == "b-alloy");
            TestBase.AssertEqual(2, alloy.Requires.Count, "multi requires", ref run, ref pass, ref fail);
            TestBase.AssertTrue(alloy.Requires.Contains("b-age1") && alloy.Requires.Contains("b-memory"),
                "requires ids", ref run, ref pass, ref fail);
        }

        private static void PreserveIdentityFromPrior(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(PreserveIdentityFromPrior));
            var prior = new SkillTreesConfig
            {
                Trees =
                {
                    new SkillTreeDefinition
                    {
                        ClassKey = "Barbarian",
                        Identity = "Take pain, make momentum."
                    }
                }
            };
            string csv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Root,E,P,,,Passive,STR,b-root,Core,0,0,bronze_skin\n";
            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(csv, prior);
            TestBase.AssertEqual("Take pain, make momentum.", cfg.Trees[0].Identity, "identity preserved", ref run, ref pass, ref fail);
        }

        private static void RoundTripPushPull(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(RoundTripPushPull));
            var source = new SkillTreesConfig
            {
                Trees =
                {
                    new SkillTreeDefinition
                    {
                        ClassKey = "Warrior",
                        Title = "Iron Discipline",
                        Weapon = "Sword",
                        Stat = "AGI",
                        Identity = "Focus tempo.",
                        Nodes =
                        {
                            new SkillTreeNodeDefinition
                            {
                                Id = "w-root",
                                Name = "Iron Discipline",
                                Branch = "Core",
                                Tier = 0,
                                Type = "Passive",
                                Cost = 0,
                                Effect = "Miss insurance.",
                                Payoff = "Focus pays.",
                                CustomEffectId = "iron_discipline"
                            },
                            new SkillTreeNodeDefinition
                            {
                                Id = "w-challenge",
                                Name = "Challenge",
                                Branch = "Command",
                                Tier = 1,
                                Type = "Action",
                                Cost = 4,
                                Requires = { "w-root" },
                                Effect = "Mark foe.",
                                Payoff = "Setup.",
                                UnlockActionName = "CHALLENGE",
                                CustomEffectId = "challenge"
                            }
                        }
                    }
                }
            }.Normalize();

            var rows = SkillTreesSheetConverter.BuildPushValueRows(source);
            TestBase.AssertEqual(3, rows.Count, "header + 2 data", ref run, ref pass, ref fail);

            var sb = new System.Text.StringBuilder();
            foreach (var row in rows)
            {
                for (int i = 0; i < row.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    string cell = row[i]?.ToString() ?? "";
                    if (cell.Contains(',') || cell.Contains('"') || cell.Contains('\n'))
                        sb.Append('"').Append(cell.Replace("\"", "\"\"")).Append('"');
                    else
                        sb.Append(cell);
                }
                sb.Append('\n');
            }

            var round = SkillTreesSheetConverter.ParseCsvToConfig(sb.ToString(), source);
            TestBase.AssertEqual(1, round.Trees.Count, "tree count", ref run, ref pass, ref fail);
            TestBase.AssertEqual(2, round.Trees[0].Nodes.Count, "node count", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Focus tempo.", round.Trees[0].Identity, "identity kept on re-parse", ref run, ref pass, ref fail);
            var challenge = round.Trees[0].Nodes.First(n => n.Id == "w-challenge");
            TestBase.AssertEqual("CHALLENGE", challenge.UnlockActionName, "unlock round-trip", ref run, ref pass, ref fail);
            TestBase.AssertEqual("w-root", challenge.Requires.Single(), "requires round-trip", ref run, ref pass, ref fail);
        }

        private static void PromoteLevelOneAsRootOnNormalize(ref int run, ref int pass, ref int fail)
        {
            TestBase.SetCurrentTestName(nameof(PromoteLevelOneAsRootOnNormalize));
            string csv =
                "Class,Tree,Weapon,Name,Effect,Payoff,UnlockAction,Requires,Type,Stat,Id,Branch,Tier,Cost,CustomEffectId\n" +
                "Barbarian,Bronze Skin,Mace,Bronze Skin,Root effect,Payoff,,,Passive,STR,b-root,Core,0,0,bronze_skin\n" +
                "Barbarian,Bronze Skin,Mace,Level 1 - Barbarian,Bone Steel Damascus count as Barbarian,Payoff,,b-root,Passive,STR,b-tribe,Alloy,1,4,tribe_metal\n" +
                "Barbarian,Bronze Skin,Mace,Gut Instinct,INT to STR,Payoff,,b-root,Mastery,STR,b-gut,Instinct,1,4,gut_instinct\n";

            var cfg = SkillTreesSheetConverter.ParseCsvToConfig(csv);
            var tribe = cfg.Trees.Single().Nodes.First(n => n.Id == "b-tribe");
            TestBase.AssertEqual(0, tribe.Tier, "Level 1 promoted to tier 0", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, tribe.Cost, "Level 1 free", ref run, ref pass, ref fail);
            TestBase.AssertEqual("Core", tribe.Branch, "Level 1 is Core", ref run, ref pass, ref fail);
            TestBase.AssertEqual(0, tribe.Requires.Count, "Level 1 has no requires", ref run, ref pass, ref fail);

            var root = cfg.Trees[0].Nodes.First(n => n.Id == "b-root");
            TestBase.AssertEqual(1, root.Tier, "identity root demoted to T1", ref run, ref pass, ref fail);
            TestBase.AssertEqual("b-tribe", root.Requires.Single(), "Bronze Skin requires Level 1", ref run, ref pass, ref fail);

            var gut = cfg.Trees[0].Nodes.First(n => n.Id == "b-gut");
            TestBase.AssertEqual("b-tribe", gut.Requires.Single(), "former b-root prereq remapped to Level 1", ref run, ref pass, ref fail);

            TestBase.AssertEqual("b-tribe", cfg.GetRootNodeId(WeaponType.Mace), "GetRootNodeId is Level 1", ref run, ref pass, ref fail);
        }
    }
}
