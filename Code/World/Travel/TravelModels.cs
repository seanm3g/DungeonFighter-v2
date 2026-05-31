using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using RPGGame.Config;

namespace RPGGame
{
    public enum TravelRollOutcome
    {
        CriticalMiss,
        Miss,
        Hit,
        Combo,
        Critical
    }

    public class TravelRegion
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Theme { get; set; } = "";
        public string Description { get; set; } = "";

        /// <summary>
        /// Dungeon theme keys this region is tied to (e.g. Forest, Lava). Travel loot rolls
        /// <see cref="TravelRouteGenerator.JourneyThemePickCount"/> times from this pool with replacement.
        /// </summary>
        public List<string> LinkedDungeonThemes { get; set; } = new();

        /// <summary>
        /// Default settlement type (Rural, Town, or City) for spawn tier weights when the room location is generic.
        /// </summary>
        public string SettlementType { get; set; } = "";

        /// <summary>
        /// Non-empty theme pool for loot and journey rolls; falls back to <see cref="Theme"/> or default dungeon theme.
        /// </summary>
        public IReadOnlyList<string> ResolveLinkedDungeonThemePool()
        {
            var fromList = (LinkedDungeonThemes ?? new List<string>())
                .Where(theme => !string.IsNullOrWhiteSpace(theme))
                .Select(theme => theme.Trim())
                .ToList();

            if (fromList.Count > 0)
                return fromList;

            if (!string.IsNullOrWhiteSpace(Theme))
                return new[] { Theme.Trim() };

            return new[] { GameConstants.DefaultDungeonTheme };
        }
    }

    public class TravelEvent
    {
        public string Id { get; set; } = "";
        public TravelRollOutcome Outcome { get; set; }
        public string Title { get; set; } = "";
        public string Narrative { get; set; } = "";
        public int ProgressDelta { get; set; }
        public int Damage { get; set; }
        public int Heal { get; set; }
        public int Xp { get; set; }
        public bool GrantsLoot { get; set; }
    }

    public class TravelStepResult
    {
        public int StepNumber { get; set; }
        public int Roll { get; set; }
        public TravelRollOutcome Outcome { get; set; }
        public TravelEvent Event { get; set; } = new TravelEvent();
        public int DelayMs { get; set; }
        public int TravelMinutes { get; set; }
        public int ProgressDelta { get; set; }
        public int DamageTaken { get; set; }
        public int HealingReceived { get; set; }
        public int XpGained { get; set; }
        public Item? LootReceived { get; set; }
    }

    public class TravelRouteResult
    {
        public TravelRegion FromRegion { get; set; } = new TravelRegion();
        public TravelRegion ToRegion { get; set; } = new TravelRegion();
        /// <summary>
        /// Number of travel events this route (from a single 4d4 roll unless overridden for tests).
        /// </summary>
        public int EventCount { get; set; }
        /// <summary>
        /// The four d4 results that summed to <see cref="EventCount"/>; empty when count was scripted.
        /// </summary>
        public int[] EventCountDice { get; set; } = Array.Empty<int>();
        /// <summary>
        /// Three dungeon themes rolled (with replacement) from <see cref="TravelRegion.ResolveLinkedDungeonThemePool"/>
        /// for the destination; travel loot picks one of these at random per drop.
        /// </summary>
        public List<string> JourneyDungeonThemes { get; set; } = new();
        public List<TravelStepResult> Steps { get; set; } = new();
        public List<Item> LootFound { get; set; } = new();
        public bool IsComplete { get; set; }
        public int TotalProgressDelta => Steps.Sum(step => step.ProgressDelta);
        public int TotalDamageTaken => Steps.Sum(step => step.DamageTaken);
        public int TotalHealingReceived => Steps.Sum(step => step.HealingReceived);
        public int TotalXpGained => Steps.Sum(step => step.XpGained);
        public int TotalDelayMs => Steps.Sum(step => step.DelayMs);
        public int TotalTravelMinutes => Steps.Sum(step => step.TravelMinutes);
    }

    public static class TravelRollResolver
    {
        public static TravelRollOutcome Resolve(int roll)
        {
            if (roll <= 1)
                return TravelRollOutcome.CriticalMiss;
            if (roll <= 5)
                return TravelRollOutcome.Miss;
            if (roll <= 13)
                return TravelRollOutcome.Hit;
            if (roll <= 19)
                return TravelRollOutcome.Combo;
            return TravelRollOutcome.Critical;
        }
    }

    public static class TravelPacing
    {
        private static int ClampD20(int roll) => roll < 1 ? 1 : (roll > 20 ? 20 : roll);

        /// <summary>
        /// Pause before the next travel step is revealed; scales with the d20 (higher roll = shorter wait).
        /// </summary>
        public static int GetStepDelayMsForRoll(int roll)
        {
            int r = ClampD20(roll);
            int pointsBelow20 = 20 - r;
            var pacing = TextDelayConfiguration.GetTravelRouteRollPacing();
            int baseMs = Math.Max(0, pacing.StepDelayBaseMs);
            int extraPer = Math.Max(0, pacing.StepExtraDelayMsPerPointBelow20);
            return baseMs + pointsBelow20 * extraPer;
        }

        /// <summary>
        /// In-world minutes contributed by this step for the journey summary; scales with the d20.
        /// </summary>
        public static int GetSummaryTravelMinutesForRoll(int roll)
        {
            int r = ClampD20(roll);
            int pointsBelow20 = 20 - r;
            var pacing = TextDelayConfiguration.GetTravelRouteRollPacing();
            int baseMin = Math.Max(0, pacing.SummaryBaseMinutes);
            int extraPer = Math.Max(0, pacing.SummaryExtraMinutesPerPointBelow20);
            return baseMin + pointsBelow20 * extraPer;
        }

        public static string FormatTravelTime(int totalMinutes)
        {
            if (totalMinutes < 60)
                return $"{Math.Max(0, totalMinutes)} min";

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return minutes == 0 ? $"{hours} hr" : $"{hours} hr {minutes} min";
        }
    }

    public class TravelRegionCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private List<TravelRegion>? regions;

        public IReadOnlyList<TravelRegion> GetAllRegions()
        {
            regions ??= LoadRegions();
            return regions;
        }

        public void Reload() => regions = null;

        public TravelRegion GetDefaultRegion() =>
            GetById(GameConstants.DefaultRegionId) ?? GetAllRegions().First();

        public TravelRegion GetRegionForCharacter(Character? character)
        {
            if (character == null)
                return GetDefaultRegion();

            var region = GetById(character.CurrentRegionId);
            if (region != null)
                return region;

            character.CurrentRegionId = GameConstants.DefaultRegionId;
            return GetDefaultRegion();
        }

        public TravelRegion? GetById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return GetAllRegions().FirstOrDefault(region =>
                string.Equals(region.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public TravelRegion? GetByTheme(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
                return null;

            return GetAllRegions().FirstOrDefault(region =>
                string.Equals(region.Theme, theme, StringComparison.OrdinalIgnoreCase));
        }

        public IReadOnlyList<TravelRegion> GetDestinationRegions(Character character)
        {
            string currentId = GetRegionForCharacter(character).Id;
            return GetAllRegions()
                .Where(region => !string.Equals(region.Id, currentId, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static List<TravelRegion> LoadRegions()
        {
            try
            {
                string path = FileManager.GetGameDataFilePath(GameConstants.RegionsJson);
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<TravelRegion>>(json, JsonOptions);
                if (loaded != null && loaded.Count > 0)
                    return loaded;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("TravelRegionCatalog", $"Failed to load regions: {ex.Message}");
            }

            return CreateFallbackRegions();
        }

        private static List<TravelRegion> CreateFallbackRegions() => new()
        {
            new TravelRegion
            {
                Id = "forest",
                DisplayName = "Whisperledger Wilds",
                Theme = "Forest",
                Description = "Trade-roads where relics once changed hands; the trees remember every inscription.",
                LinkedDungeonThemes = new List<string> { "Forest", "Crypt", "Sky" }
            },
            new TravelRegion
            {
                Id = "lava",
                DisplayName = "Ashglass Covenant",
                Theme = "Lava",
                Description = "Forges and vaults where oaths were tempered in heat and brittle glass.",
                LinkedDungeonThemes = new List<string> { "Lava", "Ice", "Sky" }
            },
            new TravelRegion
            {
                Id = "crypt",
                DisplayName = "Oubliette of Echoed Names",
                Theme = "Crypt",
                Description = "Lower ways where titles go to die, and only the dust keeps inventory.",
                LinkedDungeonThemes = new List<string> { "Crypt", "Dark", "Forest" }
            }
        };
    }

    public class TravelEventCatalog
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private List<TravelEvent>? events;

        public IReadOnlyList<TravelEvent> GetAllEvents()
        {
            events ??= LoadEvents();
            return events;
        }

        public void Reload() => events = null;

        public IReadOnlyList<TravelEvent> GetEventsForOutcome(TravelRollOutcome outcome) =>
            GetAllEvents().Where(travelEvent => travelEvent.Outcome == outcome).ToList();

        public TravelEvent GetRandomEvent(TravelRollOutcome outcome)
        {
            var matchingEvents = GetEventsForOutcome(outcome);
            if (matchingEvents.Count == 0)
                matchingEvents = CreateFallbackEvents().Where(travelEvent => travelEvent.Outcome == outcome).ToList();

            return matchingEvents[Random.Shared.Next(matchingEvents.Count)];
        }

        public Dictionary<TravelRollOutcome, int> CountByOutcome() =>
            Enum.GetValues<TravelRollOutcome>()
                .ToDictionary(outcome => outcome, outcome => GetEventsForOutcome(outcome).Count);

        private static List<TravelEvent> LoadEvents()
        {
            try
            {
                string path = FileManager.GetGameDataFilePath(GameConstants.TravelEventsJson);
                string json = File.ReadAllText(path);
                var loaded = JsonSerializer.Deserialize<List<TravelEvent>>(json, JsonOptions);
                if (loaded != null && loaded.Count > 0)
                    return loaded;
            }
            catch (Exception ex)
            {
                DebugLogger.Log("TravelEventCatalog", $"Failed to load travel events: {ex.Message}");
            }

            return CreateFallbackEvents();
        }

        private static List<TravelEvent> CreateFallbackEvents() => new()
        {
            new TravelEvent { Id = "fallback-critical-miss", Outcome = TravelRollOutcome.CriticalMiss, Title = "Bad Road", Narrative = "The road punishes a wrong turn.", ProgressDelta = -2, Damage = 4 },
            new TravelEvent { Id = "fallback-miss", Outcome = TravelRollOutcome.Miss, Title = "Slow Going", Narrative = "The route slows underfoot.", ProgressDelta = -1 },
            new TravelEvent { Id = "fallback-hit", Outcome = TravelRollOutcome.Hit, Title = "Steady Road", Narrative = "You move along normally.", ProgressDelta = 1 },
            new TravelEvent { Id = "fallback-combo", Outcome = TravelRollOutcome.Combo, Title = "Shortcut", Narrative = "You find a faster way forward.", ProgressDelta = 2, Xp = 1 },
            new TravelEvent { Id = "fallback-critical", Outcome = TravelRollOutcome.Critical, Title = "Great Fortune", Narrative = "The road offers a rare reward.", ProgressDelta = 3, Xp = 3, GrantsLoot = true }
        };
    }

    public class TravelRouteGenerator
    {
        public const int TravelEventCountDice = 4;
        public const int TravelEventCountSides = 4;
        public const int MinTravelEvents = TravelEventCountDice;
        public const int MaxTravelEvents = TravelEventCountDice * TravelEventCountSides;
        public const int JourneyThemePickCount = 3;

        private readonly TravelRegionCatalog regionCatalog;
        private readonly TravelEventCatalog eventCatalog;

        public TravelRouteGenerator()
            : this(new TravelRegionCatalog(), new TravelEventCatalog())
        {
        }

        public TravelRouteGenerator(TravelRegionCatalog regionCatalog, TravelEventCatalog eventCatalog)
        {
            this.regionCatalog = regionCatalog ?? throw new ArgumentNullException(nameof(regionCatalog));
            this.eventCatalog = eventCatalog ?? throw new ArgumentNullException(nameof(eventCatalog));
        }

        /// <summary>
        /// Rolls 4d4 (sum of four fair d4) to determine how many travel events occur on a route.
        /// </summary>
        public static int RollTravelEventCount() => RollTravelEventDice(out _);

        /// <summary>
        /// Rolls 4d4; returns the total and fills <paramref name="dice"/> with four values in 1..4.
        /// </summary>
        public static int RollTravelEventDice(out int[] dice)
        {
            dice = new int[TravelEventCountDice];
            int sum = 0;
            for (int i = 0; i < TravelEventCountDice; i++)
            {
                int face = Dice.Roll(TravelEventCountSides);
                dice[i] = face;
                sum += face;
            }

            return sum;
        }

        public static int ClampTravelEventCount(int value) =>
            Math.Clamp(value, MinTravelEvents, MaxTravelEvents);

        public TravelRouteResult GenerateRoute(
            Character player,
            string destinationRegionId,
            IReadOnlyList<int>? scriptedRolls = null,
            int? scriptedEventCount = null)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var result = CreateRouteResult(player, destinationRegionId, scriptedEventCount);

            for (int i = 0; i < result.EventCount; i++)
            {
                int roll = scriptedRolls != null && i < scriptedRolls.Count ? scriptedRolls[i] : Dice.Roll(20);
                var step = GenerateRouteStep(player, result.ToRegion, i + 1, roll, result.JourneyDungeonThemes);
                result.Steps.Add(step);

                if (step.LootReceived != null)
                    result.LootFound.Add(step.LootReceived);
            }

            CompleteRoute(player, result);
            return result;
        }

        public TravelRouteResult CreateRouteResult(Character player, string destinationRegionId, int? scriptedEventCount = null)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var fromRegion = regionCatalog.GetRegionForCharacter(player);
            var toRegion = regionCatalog.GetById(destinationRegionId)
                ?? throw new InvalidOperationException($"Unknown destination region '{destinationRegionId}'.");

            int eventCount;
            int[] eventDice;
            if (scriptedEventCount.HasValue)
            {
                eventCount = ClampTravelEventCount(scriptedEventCount.Value);
                eventDice = Array.Empty<int>();
            }
            else
            {
                eventCount = RollTravelEventDice(out int[] rolled);
                eventDice = rolled;
            }

            return new TravelRouteResult
            {
                FromRegion = fromRegion,
                ToRegion = toRegion,
                EventCount = eventCount,
                EventCountDice = eventDice,
                JourneyDungeonThemes = RollJourneyDungeonThemes(toRegion)
            };
        }

        public TravelStepResult GenerateRouteStep(
            Character player,
            TravelRegion destination,
            int stepNumber,
            int? scriptedRoll = null,
            IReadOnlyList<string>? journeyDungeonThemes = null)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            int roll = scriptedRoll ?? Dice.Roll(20);
            var outcome = TravelRollResolver.Resolve(roll);
            var travelEvent = eventCatalog.GetRandomEvent(outcome);
            return ApplyEvent(player, destination, stepNumber, roll, outcome, travelEvent, journeyDungeonThemes);
        }

        public void CompleteRoute(Character player, TravelRouteResult result)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            player.CurrentRegionId = result.ToRegion.Id;
            result.IsComplete = true;
        }

        private static List<string> RollJourneyDungeonThemes(TravelRegion destination)
        {
            var pool = destination.ResolveLinkedDungeonThemePool().ToList();
            var picks = new List<string>(JourneyThemePickCount);
            for (int i = 0; i < JourneyThemePickCount; i++)
                picks.Add(pool[Random.Shared.Next(pool.Count)]);
            return picks;
        }

        private static string PickTravelLootDungeonTheme(TravelRegion destination, IReadOnlyList<string>? journeyDungeonThemes)
        {
            if (journeyDungeonThemes != null && journeyDungeonThemes.Count > 0)
                return journeyDungeonThemes[Random.Shared.Next(journeyDungeonThemes.Count)];

            var pool = destination.ResolveLinkedDungeonThemePool().ToList();
            return pool[Random.Shared.Next(pool.Count)];
        }

        private static TravelStepResult ApplyEvent(
            Character player,
            TravelRegion destination,
            int stepNumber,
            int roll,
            TravelRollOutcome outcome,
            TravelEvent travelEvent,
            IReadOnlyList<string>? journeyDungeonThemes)
        {
            int damageTaken = 0;
            if (travelEvent.Damage > 0)
            {
                damageTaken = Math.Min(travelEvent.Damage, Math.Max(0, player.CurrentHealth - 1));
                if (damageTaken > 0)
                    player.TakeDamage(damageTaken);
            }

            int healingReceived = 0;
            if (travelEvent.Heal > 0)
            {
                int before = player.CurrentHealth;
                player.Heal(travelEvent.Heal);
                healingReceived = Math.Max(0, player.CurrentHealth - before);
            }

            int xpGained = Math.Max(0, travelEvent.Xp);
            if (xpGained > 0)
                player.AddXP(xpGained);

            Item? lootReceived = null;
            if (travelEvent.GrantsLoot)
            {
                string lootTheme = PickTravelLootDungeonTheme(destination, journeyDungeonThemes);
                lootReceived = LootGenerator.GenerateLoot(
                    player.Level,
                    player.Level,
                    player,
                    guaranteedLoot: true,
                    dungeonTheme: lootTheme);

                if (lootReceived != null)
                    player.AddToInventory(lootReceived);
            }

            double travelTimeScale = GameSettings.Instance.TravelTimeMultiplier;
            int delayMs = Math.Max(0, (int)Math.Round(TravelPacing.GetStepDelayMsForRoll(roll) * travelTimeScale));
            int travelMinutes = Math.Max(0, (int)Math.Round(TravelPacing.GetSummaryTravelMinutesForRoll(roll) * travelTimeScale));

            return new TravelStepResult
            {
                StepNumber = stepNumber,
                Roll = roll,
                Outcome = outcome,
                Event = travelEvent,
                DelayMs = delayMs,
                TravelMinutes = travelMinutes,
                ProgressDelta = travelEvent.ProgressDelta,
                DamageTaken = damageTaken,
                HealingReceived = healingReceived,
                XpGained = xpGained,
                LootReceived = lootReceived
            };
        }
    }
}
