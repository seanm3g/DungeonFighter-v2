using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        public List<TravelStepResult> Steps { get; set; } = new();
        public List<Item> LootFound { get; set; } = new();
        public int TotalProgressDelta => Steps.Sum(step => step.ProgressDelta);
        public int TotalDamageTaken => Steps.Sum(step => step.DamageTaken);
        public int TotalHealingReceived => Steps.Sum(step => step.HealingReceived);
        public int TotalXpGained => Steps.Sum(step => step.XpGained);
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
            new TravelRegion { Id = "forest", DisplayName = "Ancient Forest", Theme = "Forest", Description = "Old roads wind beneath green boughs." },
            new TravelRegion { Id = "lava", DisplayName = "Lava Caves", Theme = "Lava", Description = "Black glass paths cross red-lit vents." },
            new TravelRegion { Id = "crypt", DisplayName = "Haunted Crypt", Theme = "Crypt", Description = "Broken grave roads lead into whispering halls." }
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
        public const int RouteStepCount = 10;

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

        public TravelRouteResult GenerateRoute(Character player, string destinationRegionId, IReadOnlyList<int>? scriptedRolls = null)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            var fromRegion = regionCatalog.GetRegionForCharacter(player);
            var toRegion = regionCatalog.GetById(destinationRegionId)
                ?? throw new InvalidOperationException($"Unknown destination region '{destinationRegionId}'.");

            var result = new TravelRouteResult
            {
                FromRegion = fromRegion,
                ToRegion = toRegion
            };

            for (int i = 0; i < RouteStepCount; i++)
            {
                int roll = scriptedRolls != null && i < scriptedRolls.Count ? scriptedRolls[i] : Dice.Roll(20);
                var outcome = TravelRollResolver.Resolve(roll);
                var travelEvent = eventCatalog.GetRandomEvent(outcome);
                var step = ApplyEvent(player, toRegion, i + 1, roll, outcome, travelEvent);
                result.Steps.Add(step);

                if (step.LootReceived != null)
                    result.LootFound.Add(step.LootReceived);
            }

            player.CurrentRegionId = toRegion.Id;
            return result;
        }

        private static TravelStepResult ApplyEvent(
            Character player,
            TravelRegion destination,
            int stepNumber,
            int roll,
            TravelRollOutcome outcome,
            TravelEvent travelEvent)
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
                lootReceived = LootGenerator.GenerateLoot(
                    player.Level,
                    player.Level,
                    player,
                    guaranteedLoot: true,
                    dungeonTheme: destination.Theme);

                if (lootReceived != null)
                    player.AddToInventory(lootReceived);
            }

            return new TravelStepResult
            {
                StepNumber = stepNumber,
                Roll = roll,
                Outcome = outcome,
                Event = travelEvent,
                ProgressDelta = travelEvent.ProgressDelta,
                DamageTaken = damageTaken,
                HealingReceived = healingReceived,
                XpGained = xpGained,
                LootReceived = lootReceived
            };
        }
    }
}
