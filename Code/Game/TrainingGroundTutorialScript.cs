using System;
using System.Collections.Generic;

namespace RPGGame
{
    public enum TrainingGroundTutorialActor
    {
        Any,
        Hero,
        Dummy
    }

    public sealed class TrainingGroundTutorialEvent
    {
        public TrainingGroundTutorialEvent(TrainingGroundTutorialActor actor, int roll, string narrativeLine)
        {
            if (roll < 1 || roll > 20)
                throw new ArgumentOutOfRangeException(nameof(roll), "Tutorial rolls must be between 1 and 20.");
            if (string.IsNullOrWhiteSpace(narrativeLine))
                throw new ArgumentException("Tutorial narrative cannot be empty.", nameof(narrativeLine));

            Actor = actor;
            Roll = roll;
            NarrativeLine = narrativeLine.Trim();
        }

        public TrainingGroundTutorialActor Actor { get; }
        public int Roll { get; }
        public string NarrativeLine { get; }

        public bool Matches(Actor actor)
        {
            if (actor == null)
                return false;

            return Actor switch
            {
                TrainingGroundTutorialActor.Any => true,
                TrainingGroundTutorialActor.Hero => actor is Character and not Enemy,
                TrainingGroundTutorialActor.Dummy => PreWeaponTrainingFlow.IsTrainingDummy(actor),
                _ => false
            };
        }
    }

    public sealed class TrainingGroundTutorialScript
    {
        private readonly List<TrainingGroundTutorialEvent> events;
        private int nextIndex;

        public TrainingGroundTutorialScript(IEnumerable<TrainingGroundTutorialEvent> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            this.events = new List<TrainingGroundTutorialEvent>(events);
        }

        public IReadOnlyList<TrainingGroundTutorialEvent> Events => events;
        public int NextIndex => nextIndex;
        public bool IsComplete => nextIndex >= events.Count;

        public void Reset() => nextIndex = 0;

        public bool TryConsumeForActor(Actor actor, out TrainingGroundTutorialEvent? tutorialEvent)
        {
            tutorialEvent = null;
            if (IsComplete)
                return false;

            var candidate = events[nextIndex];
            if (!candidate.Matches(actor))
                return false;

            nextIndex++;
            tutorialEvent = candidate;
            return true;
        }
    }
}
