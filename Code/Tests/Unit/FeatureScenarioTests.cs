using System;
using System.Collections.Generic;
using System.Linq;
using RPGGame;
using RPGGame.Actions.Execution;
using RPGGame.Actions.RollModification;
using RPGGame.Combat;
using RPGGame.Combat.Narrative;
using RPGGame.Combat.Sequence;
using RPGGame.Data;
using RPGGame.Tests;
using RPGGame.UI;
using RPGGame.UI.Avalonia;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.ColorSystem;
using RPGGame.Utils;

namespace RPGGame.Tests.Unit
{
    /// <summary>
    /// Scripted scenarios for the "18 notes" features that are rare or slow to reach in live play:
    /// scouting bands, ambush advantage, pack multi-hit spillover, region bands / matching rooms, wandering enemies,
    /// tempo, status affinities, hazards, suffix fusion, hovers, colors, poem inputs and per-run stats.
    /// Every scenario forces its own rolls / spawns so it is deterministic.
    /// </summary>
    public static class FeatureScenarioTests
    {
        private static int _run, _passed, _failed;

        public static void RunAllTests()
        {
            Console.WriteLine("=== Feature Scenario Tests ===\n");
            _run = _passed = _failed = 0;
            bool prevUi = TurnManager.DisableCombatUIOutput;
            TurnManager.DisableCombatUIOutput = true;
            try
            {
                Scenario("Scouting: TECH widens the good bands", ScoutingBands);
                Scenario("Scouting: critical gives full report and advantage", ScoutingCritical);
                Scenario("Scouting: combo / hit / miss reports", ScoutingCombosHitsMisses);
                Scenario("Scouting: critical miss lies and loses initiative", ScoutingCriticalMiss);
                Scenario("Ambush: opener rolls 2d20 keep highest once", AmbushAdvantageOnce);
                Scenario("Pack: multi-hit spills to the next body", PackMultiHitSpillover);
                Scenario("Pack: one swing can drop three bodies", PackThreeKills);
                Scenario("Pack: fall line becomes narrative prose", PackFallProse);
                Scenario("Regions: bands cover 1-100 and dungeons sit in band", RegionBandDungeons);
                Scenario("Regions: high region generates matching rooms and enemies", HighRegionDungeonGeneration);
                Scenario("Wanderers: elementals stay in their element", WanderersByElement);
                Scenario("Tempo: heavy swings slower and harder than swarm", TempoHeavyVsSwarm);
                Scenario("Affinity: plant burns double, fire is immune", AffinityBurnTicks);
                Scenario("Affinity: immune status is never applied or logged", ImmuneStatusNotApplied);
                Scenario("Affinity: immune hover cites only immunity rules", ImmuneHoverReasons);
                Scenario("Hazards: rooms use their authored hazards", RoomHazards);
                Scenario("Loot: two common suffixes fuse into one uncommon", SuffixFusion);
                Scenario("Hover: hero status explains mechanics and source", HeroStatusHover);
                Scenario("Colors: action names by class and rarity", ActionColors);
                Scenario("Sort: attributes highest to lowest", AttributeSort);
                Scenario("Poem: gear, moves and loot reach the prompt", PoemPromptNames);
                Scenario("Stats: completion screen counts this run only", PerRunStats);
                Scenario("AMP: Intelligence drives AMP, Technique does not", AmpFromIntelligence);
            }
            finally
            {
                TurnManager.DisableCombatUIOutput = prevUi;
                Dice.SetTestRoll(null);
            }

            TestBase.PrintSummary("Feature Scenario Tests", _run, _passed, _failed);
        }

        private static void Scenario(string name, System.Action body)
        {
            Console.WriteLine($"--- {name} ---");
            try
            {
                body();
            }
            catch (Exception ex)
            {
                TestBase.AssertTrue(false, $"{name} threw {ex.GetType().Name}: {ex.Message}", ref _run, ref _passed, ref _failed);
            }
        }

        private static void Check(bool condition, string message) =>
            TestBase.AssertTrue(condition, message, ref _run, ref _passed, ref _failed);

        // ---------- helpers ----------

        /// <summary>Random whose Next(min, max) always returns a fixed value (the scouting d20).</summary>
        private sealed class FixedD20 : Random
        {
            private readonly int _face;
            public FixedD20(int face) => _face = face;
            public override int Next(int minValue, int maxValue) =>
                minValue == 1 && maxValue == 21 ? _face : Math.Max(minValue, Math.Min(maxValue - 1, minValue));
            public override int Next(int maxValue) => 0;
            public override double NextDouble() => 0.0;
        }

        private static Enemy MakeEnemy(string name, int maxHealth = 100, bool isLiving = true, params string[] tags)
        {
            var enemy = new Enemy(name, 1, maxHealth, 5, 5, 5, 5, 0, PrimaryAttribute.Strength, isLiving: isLiving);
            if (tags.Length > 0)
                enemy.SetTags(tags);
            return enemy;
        }

        private static Environment MakeRoom(string name, params Enemy[] enemies)
        {
            var room = new Environment(name, "test room", true, "Den");
            room.GetEnemies().Clear();
            foreach (var e in enemies)
                room.GetEnemies().Add(e);
            return room;
        }

        private static string Plain(IEnumerable<List<ColoredText>> lines) =>
            string.Join("\n", lines.Select(l => ColoredTextRenderer.RenderAsPlainText(l)));

        private static Action MakeStrike(string name = "ScenarioStrike") => new Action
        {
            Name = name,
            Type = ActionType.Attack,
            Target = TargetType.SingleTarget,
            DamageMultiplier = 1.0,
            Length = 1.0,
            IsComboAction = false
        };

        // ---------- #1 scouting ----------

        private static void ScoutingBands()
        {
            Check(RoomScouting.Resolve(1) == RoomScouting.Outcome.CriticalMiss, "1 is a critical miss");
            Check(RoomScouting.Resolve(5) == RoomScouting.Outcome.Miss, "5 is a miss with no TECH");
            Check(RoomScouting.Resolve(5, hitSteps: 1) == RoomScouting.Outcome.Hit, "TECH hit step turns 5 into a hit");
            Check(RoomScouting.Resolve(13) == RoomScouting.Outcome.Hit, "13 is a hit");
            Check(RoomScouting.Resolve(13, comboSteps: 1) == RoomScouting.Outcome.Combo, "TECH combo step turns 13 into a combo");
            Check(RoomScouting.Resolve(19) == RoomScouting.Outcome.Combo, "19 is a combo");
            Check(RoomScouting.Resolve(19, critSteps: 1) == RoomScouting.Outcome.Critical, "TECH crit step turns 19 into a critical");
            Check(RoomScouting.Resolve(20) == RoomScouting.Outcome.Critical, "20 is a critical");
        }

        private static void ScoutingCritical()
        {
            var hero = new Character("ScoutHero", 1);
            var room = MakeRoom("Thicket", MakeEnemy("Root Sprite"), MakeEnemy("Thorn Boar"));
            var report = RoomScouting.Roll(hero, room, null, new FixedD20(20));
            string text = Plain(report.Lines);
            Check(report.Outcome == RoomScouting.Outcome.Critical, "forced 20 is a critical");
            Check(report.HeroAdvantage && !report.HeroLosesInitiative, "critical grants hero advantage");
            Check(text.Contains("Root Sprite") && text.Contains("Thorn Boar") && text.Contains("2 enemies"), $"critical shows names and count: {text}");
            Check(!text.Any(char.IsDigit) || !text.Contains("20"), "the roll itself is never shown");
            Check(ReferenceEquals(RoomScouting.TakeFor(hero, room), report), "report is waiting for the scouted room");
            Check(RoomScouting.TakeFor(hero, room) == null, "report is consumed once");
        }

        private static void ScoutingCombosHitsMisses()
        {
            var hero = new Character("ScoutHero2", 1);
            var room = MakeRoom("Den Mouth", MakeEnemy("Hedge Hound"), MakeEnemy("Alpha Wolf"));

            var combo = RoomScouting.Roll(hero, room, null, new FixedD20(16));
            string comboText = Plain(combo.Lines);
            Check(combo.Outcome == RoomScouting.Outcome.Combo && !combo.HeroAdvantage, "16 is a combo without advantage");
            Check(comboText.Contains("Hedge Hound") && comboText.Contains("Alpha Wolf"), "combo reveals every enemy");

            var hit = RoomScouting.Roll(hero, room, null, new FixedD20(9));
            string hitText = Plain(hit.Lines);
            Check(hit.Outcome == RoomScouting.Outcome.Hit, "9 is a hit");
            Check(hitText.Contains("2 creatures") && !hitText.Contains("Hedge Hound"), $"hit gives count only: {hitText}");

            var miss = RoomScouting.Roll(hero, room, null, new FixedD20(3));
            string missText = Plain(miss.Lines);
            Check(miss.Outcome == RoomScouting.Outcome.Miss, "3 is a miss");
            Check(!missText.Contains("Hedge Hound") && !missText.Contains("creature"), $"miss tells nothing: {missText}");
            RoomScouting.Clear(hero);
        }

        private static void ScoutingCriticalMiss()
        {
            var hero = new Character("ScoutHero3", 1);
            var room = MakeRoom("Vigil Circle", MakeEnemy("Vigil Alpha"));
            var report = RoomScouting.Roll(hero, room, null, new FixedD20(1));
            string text = Plain(report.Lines);
            Check(report.HeroLosesInitiative && !report.HeroAdvantage, "critical miss loses initiative");
            Check(!text.Contains("Vigil Alpha"), "false report never names the real enemy");
            Check(!text.Contains(": 1 creature moving"), $"false report never gives the true count: {text}");
            Check(text.StartsWith("You scout ahead", StringComparison.Ordinal) || text.StartsWith("You listen", StringComparison.Ordinal),
                "false report looks like a real one");
            RoomScouting.Clear(hero);
        }

        // ---------- #9 ambush ----------

        private static void AmbushAdvantageOnce()
        {
            var hero = TestDataBuilders.Character().WithName("AmbushedHero").Build();
            var enemy = TestDataBuilders.Enemy().WithName("Ambusher").Build();
            enemy.EncounterAdvantageRolls = 1;
            var lastUsed = new Dictionary<Actor, Action>();
            var lastCrit = new Dictionary<Actor, bool>();

            Dice.SetTestRoll(10);
            ActionSelector.SetStoredActionRoll(enemy, 10);
            var first = ActionExecutionFlow.Execute(enemy, hero, null, null, MakeStrike(), null, lastUsed, lastCrit);
            Dice.SetTestRoll(10);
            ActionSelector.SetStoredActionRoll(enemy, 10);
            var second = ActionExecutionFlow.Execute(enemy, hero, null, null, MakeStrike(), null, lastUsed, lastCrit);
            Dice.SetTestRoll(null);

            Check(first.MultiDiceRollDetail.Mode == MultiDiceLuckMode.Advantage, "ambusher's first roll is 2d20 keep highest");
            Check(first.ModifiedBaseRoll >= 10, "advantage never lowers the roll");
            Check(second.MultiDiceRollDetail.Mode != MultiDiceLuckMode.Advantage, "only the opening roll has advantage");
            Check(enemy.EncounterAdvantageRolls == 0, "opening advantage is spent");

            var line = new DungeonDisplayManager(new GameNarrativeManager());
            EnemyEncounterHandler.AddEnemySurpriseCombatEvent(line, hero, enemy, new Random(1));
            string log = string.Join("\n", line.CombatLog);
            Check(log.Contains("Ambusher") && log.Contains("advantage"), "ambush line names the enemy and the advantage");
        }

        // ---------- #6 / #17 packs ----------

        private static Enemy MakeRatPack()
        {
            var rats = MakeEnemy("Rats", 70);
            rats.InitializePack(7); // 7 bodies x 10 HP
            return rats;
        }

        private static void PackMultiHitSpillover()
        {
            var rats = MakeRatPack();
            int applied = PackCombat.ApplyHitsAcrossBodies(rats, new[] { 6, 6, 6 });
            Check(applied == 16, $"6+6+6 on 10-HP bodies applies 16 (6, 4, 6), got {applied}");
            Check(rats.LastOverkillWasted == 2, $"only the killing hit wastes overkill (2), got {rats.LastOverkillWasted}");
            Check(rats.LivingBodyCount == 6, "one body falls");
            Check(rats.FrontBodyRemaining == 4, "third hit landed on the next body (10 - 6 = 4)");
        }

        private static void PackThreeKills()
        {
            var rats = MakeRatPack();
            int applied = PackCombat.ApplyHitsAcrossBodies(rats, new[] { 12, 12, 12 });
            Check(rats.LivingBodyCount == 4, $"three 12-damage hits drop three 10-HP bodies, {rats.LivingBodyCount} remain");
            Check(applied == 30 && rats.LastOverkillWasted == 6, $"30 applied, 6 overkill (got {applied}, {rats.LastOverkillWasted})");
            Check(PackCombat.FormatBodiesFell(rats, 3).StartsWith("Three fall.", StringComparison.Ordinal), "fall line counts all three");
        }

        private static void PackFallProse()
        {
            var rats = MakeRatPack();
            PackCombat.ApplyHitsAcrossBodies(rats, new[] { 10, 10 });
            var result = new ActionExecutionResult
            {
                PackBodyFellText = PackCombat.FormatBodiesFell(rats, 2),
                TargetPackHeadlineCount = 7
            };
            var prose = CombatSequenceFlavorTokens.InterpretPackBodiesFell(result, rats);
            string sentence = prose.FirstOrDefault() ?? "";
            Check(sentence.StartsWith("Two of the rats fall", StringComparison.Ordinal), $"prose names the fallen: {sentence}");
            Check(sentence.Contains("five remain") && !sentence.Any(char.IsDigit), $"prose has no numerals: {sentence}");
        }

        // ---------- #5 / #13 regions and rooms ----------

        private static void RegionBandDungeons()
        {
            var catalog = new TravelRegionCatalog();
            var hero = new Character("BandHero", 50) { CurrentRegionId = "fire-3" };
            var region = catalog.GetRegionForCharacter(hero);
            Check(region.MinLevel == 51 && region.MaxLevel == 55, $"Emberforge Depths is Lv 51-55, got {region.LevelRangeLabel}");

            var dungeons = new List<Dungeon>();
            new DungeonManagerWithRegistry().RegenerateDungeons(hero, dungeons);
            var themes = new HashSet<string>(region.ResolveLinkedDungeonThemePool(), StringComparer.OrdinalIgnoreCase);
            Check(dungeons.Count == 3, "one dungeon per Fire biome");
            Check(dungeons.TrueForAll(d => region.ContainsLevel(d.MinLevel)), "every dungeon level is inside 51-55");
            Check(dungeons.TrueForAll(d => themes.Contains(d.Theme)), "only Ashen Land / Fire Temple / Lava Cave are offered");

            var unknown = new Character("LegacyHero", 23) { CurrentRegionId = "crypt" };
            Check(catalog.GetRegionForCharacter(unknown).ContainsLevel(23), "a retired region id lands in the hero's band");
        }

        private static void HighRegionDungeonGeneration()
        {
            var hero = new Character("FireHero", 50) { CurrentRegionId = "fire-3" };
            var dungeons = new List<Dungeon>();
            new DungeonManagerWithRegistry().RegenerateDungeons(hero, dungeons);
            var dungeon = dungeons.FirstOrDefault(d => d.Theme == "Fire Temple") ?? dungeons[0];
            dungeon.Generate();

            var allEnemies = EnemyLoader.GetAllEnemyData().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
            bool roomsMatch = true, enemiesMatch = true, levelsInBand = true;
            foreach (var room in dungeon.Rooms)
            {
                var data = RoomLoader.GetRoomData(room.Name);
                if (data == null || !string.Equals(data.Biome, dungeon.Theme, StringComparison.OrdinalIgnoreCase))
                    roomsMatch = false;
                var roomEnemyNames = new HashSet<string>((data?.Enemies ?? new List<RoomEnemyData>()).Select(e => e.Name), StringComparer.OrdinalIgnoreCase);
                foreach (var enemy in room.GetEnemies())
                {
                    bool local = roomEnemyNames.Contains(enemy.Name);
                    bool wanderer = allEnemies.TryGetValue(enemy.Name, out var ed)
                        && EnemySpawnFilter.IsPlacementWildcard(ed.Region)
                        && EnemyGenerationManager.WandererFitsRegion(ed, "Fire");
                    if (!local && !wanderer)
                        enemiesMatch = false;
                    if (enemy.Level < 51 || enemy.Level > 55)
                        levelsInBand = false;
                }
            }
            Check(dungeon.Rooms.Count > 0, "dungeon generated rooms");
            Check(roomsMatch, "every room belongs to the dungeon's biome");
            Check(enemiesMatch, "every enemy is local to its room or a fire-safe wanderer");
            Check(levelsInBand, "enemy levels stay inside the region band");
        }

        private static void WanderersByElement()
        {
            var all = EnemyLoader.GetAllEnemyData().ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
            Check(!EnemyGenerationManager.WandererFitsRegion(all["Fire Elemental"], "Forest"), "Fire Elemental never wanders into the forest");
            Check(EnemyGenerationManager.WandererFitsRegion(all["Fire Elemental"], "Fire"), "Fire Elemental can wander the Fire region");
            Check(EnemyGenerationManager.WandererFitsRegion(all["Rats"], "Water"), "untagged Rats can wander anywhere");
            Check(all["Rats"].ResolvedPackSize > 1, "Rats are still a pack");
        }

        // ---------- #8 tempo ----------

        private static void TempoHeavyVsSwarm()
        {
            var enemySystem = GameConfiguration.Instance.EnemySystem;
            bool prevEnabled = enemySystem.Identity.Enabled;
            enemySystem.Identity.Enabled = true;
            try
            {
                var template = EnemyLoader.GetAllEnemyData().First(e => e.Name == "Goblin");
                EnemyData WithTempo(string tempo) => new EnemyData
                {
                    Name = template.Name,
                    Archetype = template.Archetype,
                    GrowthPerLevel = template.GrowthPerLevel,
                    HealthPercent = template.HealthPercent,
                    HealthGrowthPercent = template.HealthGrowthPercent,
                    Tempo = tempo
                };
                var heavy = EnemyIdentity.Resolve(WithTempo(EnemyTempo.Heavy), 20, enemySystem)!;
                var swarm = EnemyIdentity.Resolve(WithTempo(EnemyTempo.Swarm), 20, enemySystem)!;
                double ratio = heavy.AttackTime / swarm.AttackTime;
                Check(Math.Abs(ratio - 3.5 / 0.55) < 0.01, $"heavy swings {3.5 / 0.55:0.0}x slower than swarm, got {ratio:0.00}");
                double dpsHeavy = heavy.HitDamage / heavy.AttackTime;
                double dpsSwarm = swarm.HitDamage / swarm.AttackTime;
                Check(Math.Abs(dpsHeavy - dpsSwarm) < 1e-9, "damage per second is the same for every tempo");
                Check(heavy.HitDamage > swarm.HitDamage * 6, "heavy hits are much bigger per swing");
            }
            finally
            {
                enemySystem.Identity.Enabled = prevEnabled;
            }
        }

        // ---------- #15 / #16 affinities ----------

        private static void AffinityBurnTicks()
        {
            using (GameTicker.BeginIsolatedEncounterGameTime())
            {
                var treant = MakeEnemy("Treant", 200, true, "plant");
                var fire = MakeEnemy("Ember Spirit", 200, true, "fire");
                var plain = MakeEnemy("Goblin", 200, true, "living");
                foreach (var e in new[] { treant, fire, plain })
                {
                    e.BurnIntensity = 5;
                    e.LastBurnTickTime = 0;
                }
                GameTicker.Instance.AdvanceGameTime(10);
                double now = GameTicker.Instance.GetCurrentGameTime();
                // Level 1 targets take exactly their burn intensity (DotDamagePerLevel scaling starts above level 1).
                int plainTick = plain.ProcessBurn(now);
                int plantTick = treant.ProcessBurn(now);
                int fireTick = fire.ProcessBurn(now);
                Check(plainTick == 5, $"level 1 enemy burns for exactly its intensity (5), got {plainTick}");
                Check(plantTick == plainTick * 2, $"plant burns double ({plainTick} x2), got {plantTick}");
                Check(fireTick == 0, $"fire takes no burn, got {fireTick}");
            }
        }

        private static void ImmuneStatusNotApplied()
        {
            var registry = new EffectHandlerRegistry();
            var skeleton = MakeEnemy("Skeleton", 100, false, "undead");
            var goblin = MakeEnemy("Goblin", 100, true, "living");
            var poisonAction = new Action { Name = "Venom", Type = ActionType.Attack, CausesPoison = true, DamageMultiplier = 1.0, Length = 1.0 };

            var skeletonMessages = new List<string>();
            bool appliedToSkeleton = registry.ApplyEffect("poison", skeleton, poisonAction, skeletonMessages);
            Check(!appliedToSkeleton && skeletonMessages.Count == 0, "undead is never poisoned and nothing is logged");
            Check(skeleton.PoisonPercentOfMaxHealth == 0, "no poison stacks on the skeleton");

            var goblinMessages = new List<string>();
            bool appliedToGoblin = registry.ApplyEffect("poison", goblin, poisonAction, goblinMessages);
            Check(appliedToGoblin, "living goblin is poisoned normally");
        }

        private static void ImmuneHoverReasons()
        {
            var elemental = MakeEnemy("Fire Elemental", 100, false, "elemental", "fire", "undead");
            string burn = StatusEffectHoverDetails.DescribeAffinity(elemental, "burn");
            Check(burn.StartsWith("Immune to Burn", StringComparison.Ordinal), $"fire elemental is immune to burn: {burn}");
            Check(burn.Contains("Already ablaze") && !burn.Contains("Dry flesh"), $"immune hover cites only immunity rules: {burn}");

            var treant = MakeEnemy("Treant", 100, true, "plant");
            Check(StatusEffectHoverDetails.DescribeAffinity(treant, "burn").StartsWith("Weak to Burn", StringComparison.Ordinal), "treant hover says weak to burn");
        }

        // ---------- #13 hazards ----------

        private static void RoomHazards()
        {
            var thicket = RoomLoader.CreateRoom("Thicket", "Dense Brush");
            var names = thicket.ActionPool.Select(a => a.action.Name).ToList();
            Check(names.Count == 2 && names.Contains("Thorn Vines") && names.Contains("Falling Branch"),
                $"Thicket uses only its authored hazards: {string.Join(", ", names)}");
            var ember = EnvironmentalActionLoader.TryCreateByName("Ember Rain");
            Check(ember != null && ember.CausesBurn, "Ember Rain hazard burns");
        }

        // ---------- #18 suffix fusion ----------

        private static void SuffixFusion()
        {
            var cache = LootDataCache.Load();
            var commons = cache.StatBonuses
                .Where(s => string.Equals((s.Rarity ?? "Common").Trim(), "Common", StringComparison.OrdinalIgnoreCase))
                .GroupBy(s => s.StatType).Select(g => g.First()).Take(2).ToList();
            Check(commons.Count == 2, "catalog has two different common suffixes");
            if (commons.Count < 2)
                return;

            var applier = new LootBonusApplier(cache, new Random(7));
            var item = new HeadItem("Crystal Rod", 1, 1);
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            item.StatBonuses.Add(commons[0].CloneForItemInstance());
            used.Add(commons[0].StatType);

            bool fused = applier.TryFuseCommonSuffixes(item, commons[1], used);
            Check(fused, "second common suffix triggers a fusion");
            Check(item.StatBonuses.Count == 1, $"two commons become one suffix, got {item.StatBonuses.Count}");
            Check(string.Equals(item.StatBonuses[0].Rarity?.Trim(), "Uncommon", StringComparison.OrdinalIgnoreCase),
                $"fused suffix is Uncommon, got {item.StatBonuses[0].Rarity}");
        }

        // ---------- #2 / #12 / #4 UI copy ----------

        private static void HeroStatusHover()
        {
            var hero = new Character("BurningHero", 1);
            hero.BurnIntensity = 3;
            hero.NoteStatusSource("burn", "EMBER RAIN");
            string text = string.Join("\n", HoverEffectCopy.ExplainStatusLine(hero, "Burn 3"));
            Check(text.Contains("How it works") && text.Contains("intensity"), "burn hover explains ticking and decay");
            Check(text.Contains("Right now") && text.Contains("Applied by: EMBER RAIN"), "burn hover shows live numbers and the source");
            Check(text.Contains("Ends:"), "burn hover says how it ends");
        }

        private static void ActionColors()
        {
            var barbarian = new Action { Name = "SCENARIO SMASH", Tags = new List<string> { "class", "barbarian" } };
            var hybrid = new Action { Name = "SCENARIO MIX", Tags = new List<string> { "hybrid", "warrior" } };
            var rare = new Action { Name = "SCENARIO RARE", Tags = new List<string> { "rare" } };
            Check(ActionNameColors.Resolve(barbarian) == PrimaryStatRowHighlightColors.ForWeaponPath(WeaponType.Mace), "barbarian action uses the mace path color");
            Check(ActionNameColors.Resolve(hybrid) == AsciiArtAssets.Colors.Purple, "hybrid action is purple");
            Check(ActionNameColors.Describe(rare) == "Rare action", "rare action is labeled by rarity");
            Check(ActionNameColors.Describe(barbarian).StartsWith("Barbarian class action", StringComparison.Ordinal), "class action is labeled by class");
        }

        private static void AttributeSort()
        {
            var hero = TestDataBuilders.Character().WithName("SortHero").WithStats(3, 9, 12, 6).Build();
            var defaultOrder = LeftPanelAttributeOrder.Build(hero, sortByValue: false).Select(r => r.Label).ToList();
            var sorted = LeftPanelAttributeOrder.Build(hero, sortByValue: true).Select(r => r.Label).ToList();
            Check(string.Join(",", defaultOrder) == "STR,AGI,TECH,INT", "default order is STR, AGI, TECH, INT");
            Check(sorted[0] == "TECH" && sorted[^1] == "STR", $"sorted puts the highest first: {string.Join(",", sorted)}");
            Check(LeftPanelAttributeOrder.IsAttributeHoverId(LeftPanelHoverState.Prefix + "stat:tec"), "TECH row click toggles sorting");
        }

        // ---------- #14 poem ----------

        private static void PoemPromptNames()
        {
            var packet = new BattleLimerickPacket
            {
                HeroName = "Neville Ashenblade",
                EnemyName = "Salamander",
                HeroWon = true,
                HeroHpLeft = 50,
                HeroMaxHp = 100,
                GearNames = new[] { "Glass Twig" },
                ActionNames = new[] { "STAB" }
            };
            var withLoot = BattleLimerickPacketBuilder.WithLoot(packet, new[] { "Worn Gold Coat" });
            string prompt = withLoot.ToPromptBlock();
            Check(prompt.Contains("Glass Twig") && prompt.Contains("STAB") && prompt.Contains("Worn Gold Coat"),
                "prompt carries gear, move and loot names");
            Check(prompt.Contains("optional"), "the names are offered as optional");
            Check(!prompt.Contains("Neville Ashenblade\n- "), "hero name is still only inspiration");
        }

        // ---------- per-run stats / AMP ----------

        private static void PerRunStats()
        {
            var stats = new SessionStatistics();
            stats.RecordEnemyDefeat();
            stats.RecordEnemyDefeat();
            stats.BeginDungeonRun();
            stats.RecordEnemyDefeat();
            stats.RecordDamageDealt(40);
            Check(stats.EnemiesDefeated == 3, "session total keeps counting");
            Check(stats.RunEnemiesDefeated == 1, "completion screen shows this run only");
            Check(stats.RunDamageDealt == 40, "run damage is counted from dungeon entry");
        }

        private static void AmpFromIntelligence()
        {
            var lowInt = TestDataBuilders.Character().WithName("TechHero").WithStats(10, 10, 60, 0).Build();
            var highInt = TestDataBuilders.Character().WithName("IntHero").WithStats(10, 10, 0, 60).Build();
            Check(Math.Abs(lowInt.GetComboAmplifier() - 1.0) < 0.0001, "high TECH with no INT gives no AMP");
            Check(highInt.GetComboAmplifier() > 1.3, "INT drives AMP");
        }
    }
}
