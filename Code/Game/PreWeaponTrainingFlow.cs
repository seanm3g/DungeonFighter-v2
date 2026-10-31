using System;
using System.Threading;
using RPGGame.Combat.Sequence;

namespace RPGGame
{
    /// <summary>
    /// Shared checks for the pre-weapon Training Ground tutorial flow.
    /// </summary>
    public static class PreWeaponTrainingFlow
    {
        private static int _tutorialCombatSlowPacingActive;

        /// <summary>True while the pre-weapon Training Ground dungeon is running with tutorial combat pacing.</summary>
        public static bool IsTutorialCombatSlowPacingActive =>
            Volatile.Read(ref _tutorialCombatSlowPacingActive) != 0;

        /// <summary>
        /// Enables or disables slower tutorial combat log pacing for the Training Ground run.
        /// </summary>
        public static void SetTutorialCombatSlowPacing(bool active) =>
            Volatile.Write(ref _tutorialCombatSlowPacingActive, active ? 1 : 0);
        public static bool IsTrainingGroundDungeonName(string? dungeonName) =>
            string.Equals(dungeonName, GameConstants.TrainingGroundDungeonName, StringComparison.OrdinalIgnoreCase);

        public static bool IsTrainingDummy(Actor? actor) =>
            actor is Enemy enemy &&
            string.Equals(enemy.Name, GameConstants.TrainingDummyEnemyName, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the current player is in the pre-weapon tutorial gate and the named dungeon is Training Ground.
        /// </summary>
        public static bool IsPreWeaponTrainingDungeon(GameStateManager? stateManager, string? dungeonName)
        {
            if (stateManager?.CurrentPlayer == null)
                return false;
            return stateManager.CurrentPlayer.PendingPreWeaponTrainingGround && IsTrainingGroundDungeonName(dungeonName);
        }

        public static Dungeon CreateTrainingGroundDungeon() =>
            new Dungeon(
                GameConstants.TrainingGroundDungeonName,
                1,
                1,
                "Generic",
                new System.Collections.Generic.List<string>(),
                null);

        /// <summary>The dummy's two-card sequence: a light sway and a slower rebound, so the top strip has cards to show.</summary>
        public static readonly string[] TrainingDummyActionNames = { "SWAY", "REBOUND" };

        /// <summary>
        /// Replaces the constructor's fallback pool (which builds no sequence) with <see cref="TrainingDummyActionNames"/>:
        /// plain attacks with no status effects, the second one slower and heavier.
        /// </summary>
        public static void SetTrainingDummyActions(Enemy dummy)
        {
            dummy.ActionPool.Clear();
            dummy.AddAction(new Action(TrainingDummyActionNames[0], ActionType.Attack, TargetType.SingleTarget,
                description: "The dummy rocks back on its rope and bumps into you."), 1.0);
            dummy.AddAction(new Action(TrainingDummyActionNames[1], ActionType.Attack, TargetType.SingleTarget,
                description: "A slow, heavier swing on the rebound.", damageMultiplier: 1.5, length: 1.5), 1.0);
            dummy.InitializeActionSequence();
        }

        public static TrainingGroundTutorialScript CreateTrainingGroundTutorialScript()
        {
            const CombatSequenceStepKind Attacker = CombatSequenceStepKind.Attacker;
            const CombatSequenceStepKind Roll = CombatSequenceStepKind.Roll;
            const CombatSequenceStepKind Outcome = CombatSequenceStepKind.Outcome;
            const CombatSequenceStepKind Act = CombatSequenceStepKind.Action;
            const CombatSequenceStepKind Defense = CombatSequenceStepKind.Defense;
            const CombatSequenceStepKind Damage = CombatSequenceStepKind.Damage;

            static TutorialCoachNote Note(string title, string text, TutorialCoachFocus focus = TutorialCoachFocus.None,
                CombatSequenceStepKind? step = null, bool lucky = false) =>
                new TutorialCoachNote(title, text, focus, step, lucky);

            // Nothing before the first turn: the board is explained on the first roll, in the order it plays out.
            var intro = Array.Empty<TutorialCoachNote>();

            // Six rolls cover every result. Hero: hit, Lucky rescue into a combo, critical miss, critical (the kill).
            // Dummy (its own queue, between the hero's turns): a combo that fires SWAY, then a miss that resets its strip.
            var events = new[]
            {
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    10,
                    "You breathe, plant your feet, and drive into the target. A 10 is a clean hit: reliable, plain, and enough to move the fight forward.",
                    luckRoll: 3,
                    notes: new[]
                    {
                        // First roll, walked through as it plays: timeline, whose turn, the board, the odds, then (below)
                        // the roll, the number, the box it picked, your strip, the damage.
                        Note("TRAINING GROUND",
                            "Every swing in a fight is decided by a d20, and the combat board shows each step as it happens. "
                            + "This dummy barely hits back, so take your time. Press NEXT (Enter, Space or click) to go on; Esc skips the tutorial.",
                            step: Attacker),
                        Note("TURN TIMELINE",
                            "The timeline lines up whose turn comes when. Gray cards on the left already played, the tall card is the turn "
                            + "playing now, and the cards to the right are what comes next if every roll lands its combo action (IF ALL ROLLS LAND). "
                            + "Faster actions come around sooner; a miss or a speed change reshuffles the row.",
                            TutorialCoachFocus.Timeline, Attacker),
                        Note("YOUR TURN",
                            "The lit orb shows whose turn it is: green below the die is you, red above it is the enemy. Right now it's yours.",
                            TutorialCoachFocus.TurnOrb, Attacker),
                        Note("THE BOARD",
                            "The middle of the board plays out each swing: NOW on the left holds the card being played, the die sits in the "
                            + "middle, and the boxes on the right are where the roll can lead.",
                            TutorialCoachFocus.Board, Attacker),
                        Note("THE ODDS",
                            "Each box on the right is one way this swing can go, with the die faces it needs: a miss, a plain hit, "
                            + "or your combo action. The higher the face, the better the result.",
                            TutorialCoachFocus.Branches, Attacker),
                        Note("THE ROLL",
                            "Now the d20 is thrown. You are Lucky (your Leather armor), so two dice roll: {die1} and {die2}. "
                            + "You keep the higher one.",
                            TutorialCoachFocus.Die, Roll, lucky: true),
                        Note("THE RESULT",
                            "You keep {kept}. That lands in the hit band ({hit}-{hitmax}), so the die reads HIT.",
                            TutorialCoachFocus.Die, Outcome),
                        Note("HIT",
                            "The box the roll landed on lights up and the rest fade. A plain hit is a basic strike: it deals damage, "
                            + "but it doesn't fire a card from your strip.",
                            TutorialCoachFocus.ChosenBranch, Outcome),
                        Note("YOUR ACTION STRIP",
                            "Along the bottom are your combo cards. For now you only know {action}. "
                            + "A roll in the combo band ({combo}+) fires the lit card, then the next card in the strip lines up.",
                            TutorialCoachFocus.HeroStrip, Outcome),
                        Note("DAMAGE",
                            "The hit lands on the dummy: the damage shows on its side of the board and comes off its health bar.",
                            TutorialCoachFocus.Board, Damage)
                    }),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Dummy,
                    15,
                    "The dummy swings back on its rope and catches you square.",
                    notes: new[]
                    {
                        Note("ENEMY ACTIONS",
                            "Now the dummy acts. Across the very top is its action strip: its moves, in the order it uses them. "
                            + "The dummy has two, SWAY and the slower, heavier REBOUND. The card marked NEXT is what it tries this turn. "
                            + "Enemies roll the same d20 against their own bands, with no Lucky die.",
                            TutorialCoachFocus.EnemyStrip, Attacker),
                        Note("ENEMY COMBO",
                            "A {kept} is in the combo band, so the dummy's lit card fires: SWAY, the first card on its strip.",
                            TutorialCoachFocus.EnemyStrip, Outcome),
                        Note("NEXT CARD",
                            "SWAY fired, so the dummy's NEXT marker moves on to REBOUND, its slower, heavier card. "
                            + "The timeline at the top shows when that turn comes.",
                            TutorialCoachFocus.EnemyStrip, Act),
                        Note("DEFENSE",
                            "When you are hit, you roll a defense die against the attack. Beat it by 4 or more and your armor blocks 150%; "
                            + "lose by 4 or more and it blocks only 75%.",
                            TutorialCoachFocus.Sequence, Defense)
                    }),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    3,
                    "You follow the dummy's motion instead of resetting, and the second die opens the combo path.",
                    luckRoll: 16,
                    notes: new[]
                    {
                        Note("LUCKY SAVES YOU",
                            "The first die is a {die1}, a miss on its own. Lucky's second die is a {die2}, so you keep {kept}. "
                            + "That second chance is what the Leather set buys you.",
                            TutorialCoachFocus.Die, Roll, lucky: true),
                        Note("LUCKY",
                            "Check STATUS EFFECTS: you are Lucky. Wearing 3 or more Leather pieces grants it: every attack rolls two d20 "
                            + "and keeps the higher. Take a Leather piece off and Lucky goes away.",
                            TutorialCoachFocus.StatusEffects, Roll, lucky: true),
                        Note("COMBO",
                            "{kept} reaches the combo band ({combo}+). Instead of a plain strike, the lit card on your strip fires.",
                            TutorialCoachFocus.ChosenBranch, Outcome),
                        Note("ACTION",
                            "{action} fires from your strip. Each roll in the combo band fires the next card, and combos grow stronger "
                            + "the longer the chain runs.",
                            TutorialCoachFocus.HeroStrip, Act)
                    }),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Dummy,
                    4,
                    "The dummy lurches on its rope and swings wide.",
                    notes: new[]
                    {
                        Note("MISS",
                            "The dummy rolls a {kept}, in the miss band ({missmax} or less). Nothing lands, and its combo resets: "
                            + "NEXT jumps back from REBOUND to SWAY. A miss does the same to your strip.",
                            TutorialCoachFocus.EnemyStrip, Outcome)
                    }),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    1,
                    "You get greedy after the combo and trip over your own stance. The natural 1 is a critical miss: momentum can become a mistake if you lose control.",
                    luckRoll: 1,
                    notes: new[]
                    {
                        Note("SNAKE EYES",
                            "Both dice show {kept}. Lucky can't help when there is no better die to keep.",
                            TutorialCoachFocus.Die, Roll, lucky: true),
                        Note("CRITICAL MISS",
                            "A {critmiss} is a critical miss. Like a miss your combo resets, and the fumble also makes this action take "
                            + "twice as long, so your next turn comes later. Watch the timeline reshuffle.",
                            TutorialCoachFocus.Timeline, Outcome)
                    }),
                new TrainingGroundTutorialEvent(
                    TrainingGroundTutorialActor.Hero,
                    20,
                    "You settle, see the final opening, and drive PUNCH straight through it. The natural 20 is both combo and crit, breaking the dummy apart.",
                    luckRoll: 7,
                    notes: new[]
                    {
                        Note("A NATURAL 20",
                            "{die1} and {die2}: keep the {kept}.",
                            TutorialCoachFocus.Die, Roll, lucky: true),
                        Note("CRITICAL",
                            "A {crit} is a critical hit, and it is in the combo band too: your strip card fires and its damage is multiplied.",
                            TutorialCoachFocus.Die, Outcome)
                    })
            };

            var outro = new[]
            {
                Note("TRAINING COMPLETE",
                    "You've seen every result: hit, miss, combo, critical miss and critical, how Lucky's second die can turn a miss "
                    + "into a combo, and how an enemy's strip fires and resets. Next you'll choose a weapon, which brings new cards to your strip.")
            };

            return new TrainingGroundTutorialScript(events, intro, outro);
        }
    }
}
