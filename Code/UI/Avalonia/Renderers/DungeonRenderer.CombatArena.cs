using Avalonia.Media;
using RPGGame;
using RPGGame.Combat.Sequence;
using RPGGame.UI.Avalonia.Display;
using RPGGame.UI.Avalonia.Feedback;
using RPGGame.UI.Avalonia.Layout;
using RPGGame.UI.Avalonia.Managers;
using System;
using System.Collections.Generic;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Arena combat layout (<see cref="CombatBoardLayout.IsArena"/>, geometry in <see cref="CombatArenaLayout"/>): hero plate
    /// top left and enemy plate top right (portrait, name, health, level), status effects under each portrait, both combo
    /// strips under the plates, a pointer between the plates at whoever acts next, and the log in a centered panel over
    /// the room backdrop. The hero strip itself is drawn by <see cref="RenderActionInfoStrip"/>, which reads its rect from
    /// <see cref="LayoutConstants.ACTION_INFO_X"/> and friends.
    /// </summary>
    public partial class DungeonRenderer
    {
        private static readonly Color ArenaPlateFill = Color.FromArgb(225, 10, 10, 10);
        private static readonly Color ArenaStatusFill = Color.FromArgb(200, 10, 10, 10);
        private static readonly Color ArenaHeroColor = Color.FromRgb(220, 255, 40);

        /// <summary>Outline on enemy cards already played this combo run (the hero strip uses <see cref="ArenaHeroColor"/>).</summary>
        private static readonly Color ArenaEnemyPlayedBorder = Color.FromRgb(200, 40, 40);

        private static readonly string[] ArenaHeroPortrait = { " .---. ", "( o o )", " \\ - / ", " /|^|\\ " };
        private static readonly string[] ArenaEnemyPortrait = { "/\\   /\\", "(>   <)", " \\VVV/ ", "  ^^^  " };

        /// <summary>Paints the whole arena except the hero strip. Call only while <see cref="CombatBoardLayout.IsArena"/>.</summary>
        public void RenderCombatArena(Character player, Enemy enemy, DisplayBuffer? log, IReadOnlyList<string>? enemyNamesForLogAlignment)
        {
            const ActionStripDamageLineMode mode = ActionStripDamageLineMode.EffectiveWithComboAmp;
            var screen = CombatArenaLayout.Screen();
            int sw = screen.X * 2 + screen.W, sh = LayoutConstants.SCREEN_HEIGHT + 1;
            canvas.ClearTextInArea(0, 0, sw, sh);
            canvas.ClearBoxesInArea(0, 0, sw, sh);
            canvas.ClearProgressBarsInArea(0, 0, sw, sh);
            canvas.ClearSegmentedBarsInArea(0, 0, sw, sh);
            canvas.ClearLinesInArea(0, 0, sw, sh);

            var turn = BuildBoardTurnView(player, enemy);
            Actor? actor = TurnActor(turn);
            if (CombatArenaIntro.Running)
            {
                CombatBoardAnimation.RequestFrame();
                actor = null; // nobody has the turn yet: both portraits stay lit
            }
            double open = CombatArenaIntro.Open;

            RenderArenaPlate(player, isHero: true, CombatArenaLayout.HeroPlate(), actor, open);
            RenderArenaPlate(enemy, isHero: false, CombatArenaLayout.EnemyPlate(), actor, open);
            RenderArenaCenter(actor, player, enemy, turn);

            if (!CombatArenaIntro.ShowsBelowPlates)
                return;

            RenderArenaStatus(player, CombatArenaLayout.HeroStatus(), alignRight: true);
            RenderArenaStatus(enemy, CombatArenaLayout.EnemyStatus(), alignRight: false);

            var es = CombatArenaLayout.EnemyStrip();
            var enemyCards = new List<(int X, int Y, int W, int H)>();
            var enemyCard = RenderBoardEnemyStrip(enemy, es.X, es.Y, es.W, es.H, mode, enemyCards);

            var heroCard = HeroCardRect(player, LiveSwingFor(player));
            UpdateTurnSpotlight(player, enemy, turn, enemyCard, heroCard, es, enemyCards);

            var lr = CombatArenaLayout.Log();
            if (log != null)
                RenderArenaLog(player, log, lr.X, lr.Y, lr.W, lr.H, enemyNamesForLogAlignment);
        }

        /// <summary>
        /// One side's plate, portrait at the screen-middle end. Health fills from the portrait outward, so the name and HP
        /// sit on the middle side and the level line on the outer side. The portrait grays out while the other side (or
        /// the room) has the turn. <paramref name="open"/> below 1 during the intro: the plate and bar are still opening
        /// outward from the portrait and the text has not come up yet.
        /// </summary>
        private void RenderArenaPlate(Actor actor, bool isHero, (int X, int Y, int W, int H) plate, Actor? turnActor, double open)
        {
            bool acting = ReferenceEquals(turnActor, actor);
            bool waiting = turnActor != null && !acting;
            Color accent = isHero ? ArenaHeroColor : AsciiArtAssets.Colors.Red;
            const int pw = CombatArenaLayout.PortraitW, ph = CombatArenaLayout.PortraitH;
            int px = isHero ? plate.X + plate.W - pw : plate.X;

            int shownW = pw + (int)Math.Round((plate.W - pw) * open);
            int plateX = isHero ? plate.X + plate.W - shownW : plate.X;
            AddBoardBox(plateX, plate.Y, shownW, plate.H, acting ? accent : ArenaPlateFill, ArenaPlateFill, acting ? 2 : 1);

            Color portraitFill = waiting ? Color.FromRgb(85, 85, 85) : isHero ? Color.FromRgb(120, 190, 200) : Color.FromRgb(225, 80, 80);
            AddBoardBox(px, plate.Y, pw, ph, waiting ? BoardDimBorder : AsciiArtAssets.Colors.White, portraitFill, 3);
            var art = isHero ? ArenaHeroPortrait : ArenaEnemyPortrait;
            int artY = plate.Y + (ph - art.Length) / 2;
            for (int i = 0; i < art.Length; i++)
                canvas.AddText(px + (pw - art[i].Length) / 2, artY + i, art[i], waiting ? Color.FromRgb(40, 40, 40) : AsciiArtAssets.Colors.Black);

            int ix = isHero ? plate.X + 2 : plate.X + pw + 2;
            int iw = plate.W - pw - 4;
            if (iw < 8)
                return;

            int hp, max;
            string entityId;
            double[]? dividers = null;
            string level;
            if (actor is Enemy enemy)
            {
                entityId = $"enemy_{enemy.Name}";
                hp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve(entityId, enemy.CurrentHealth);
                max = enemy.MaxHealth;
                dividers = enemy.IsPack ? enemy.PackDividerFractions : null;
                level = $"LVL {enemy.Level}  {enemy.Archetype.ToString().ToUpperInvariant()}";
            }
            else if (actor is Character hero)
            {
                entityId = $"player_{hero.Name}";
                hp = RPGGame.Combat.UI.HealthBarDisplayHold.Resolve(entityId, hero.CurrentHealth);
                max = hero.GetEffectiveMaxHealth();
                level = $"LVL {hero.Level}  {hero.GetCurrentClass().ToUpperInvariant()}";
            }
            else
                return;

            int barW = (int)Math.Round(iw * open);
            if (barW > 0)
                canvas.AddHealthBar(isHero ? ix + iw - barW : ix, plate.Y + 3, barW, hp, max, entityId: entityId, heightScale: 1.6,
                    dividerFractions: dividers, fillFromRight: isHero);
            if (open < 1.0)
                return;

            string name = Truncate(actor.Name.ToUpperInvariant(), iw);
            canvas.AddText(isHero ? ix + iw - name.Length : ix, plate.Y + 1, name, isHero ? AsciiArtAssets.Colors.White : accent);

            string hpText = $"{hp}/{max}";
            int lineY = plate.Y + 5;
            level = Truncate(level, Math.Max(1, iw - hpText.Length - 2));
            if (isHero)
            {
                canvas.AddText(ix, lineY, level, accent);
                canvas.AddText(ix + iw - hpText.Length, lineY, hpText, accent);
            }
            else
            {
                canvas.AddText(ix, lineY, hpText, AsciiArtAssets.Colors.White);
                canvas.AddText(ix + iw - level.Length, lineY, level, AsciiArtAssets.Colors.White);
            }
        }

        private static readonly Color ArenaDieFill = Color.FromRgb(16, 16, 16);

        /// <summary>
        /// Between the portraits: VS during the intro; then "next" with an arrow at the side about to act (down for the
        /// room) over the d20. The die is blank once the attacker lines up, tumbles on ROLL, lands on the roll in the
        /// outcome's color, and keeps the last roll dimmed between swings.
        /// </summary>
        private void RenderArenaCenter(Actor? actor, Character player, Enemy enemy, BoardTurnView turn)
        {
            var r = CombatArenaLayout.Center();
            if (r.W < 6)
                return;
            int cx = r.X + r.W / 2;
            if (CombatArenaIntro.Running)
            {
                if (CombatArenaIntro.Open > 0.3)
                    canvas.AddText(cx - 1, r.Y + r.H / 2, "VS", AsciiArtAssets.Colors.Gold);
                return;
            }

            const string label = "next";
            canvas.AddText(cx - label.Length / 2, r.Y, label, AsciiArtAssets.Colors.Red);
            string arrow = ReferenceEquals(actor, enemy) ? ">>>" : ReferenceEquals(actor, player) ? "<<<" : actor != null ? "vvv" : "";
            if (actor != null && arrow.Length > 0)
                canvas.AddText(cx - arrow.Length / 2, r.Y + 1, arrow, BoardActorColor(actor));

            int dw = Math.Min(r.W - 2, 9), dh = Math.Min(r.H - 2, 5);
            int dx = r.X + (r.W - dw) / 2, dy = r.Y + 2;
            var swing = turn.LiveSwing;
            string face;
            Color color;
            int thickness = 2;
            if (swing != null)
            {
                if (turn.Stage == 0 && !DieShown(turn.Stage, turn.Raw))
                    return;
                if (turn.Stage == 0)
                    (face, color) = ("?", AsciiArtAssets.Colors.White);
                else if (turn.Stage == 1)
                {
                    face = (System.Environment.TickCount64 / 70 % 20 + 1).ToString();
                    color = AsciiArtAssets.Colors.White;
                    CombatBoardAnimation.RequestFrame();
                }
                else
                {
                    face = swing.IsCritical ? $"{swing.Roll}!" : swing.Roll.ToString();
                    color = OutcomeColor(swing.Outcome);
                    thickness = 3;
                }
            }
            else if (CombatSequenceHudState.ActiveSwing is { } last && last.Source is not RPGGame.Environment)
                (face, color) = (last.Roll.ToString(), BoardDimBorder);
            else
                return;

            AddBoardBox(dx, dy, dw, dh, color, ArenaDieFill, thickness);
            canvas.AddText(dx + (dw - face.Length) / 2, dy + dh / 2, face, color);
        }

        /// <summary>Active status effects as short colored tags, one per row, under the portrait.</summary>
        private void RenderArenaStatus(Actor actor, (int X, int Y, int W, int H) r, bool alignRight)
        {
            var chips = ArenaStatusChips(actor);
            if (chips.Count == 0)
                return;
            int rows = Math.Min(chips.Count, r.H - 2);
            AddBoardBox(r.X, r.Y, r.W, rows + 2, BoardDimBorder, ArenaStatusFill);
            for (int i = 0; i < rows; i++)
            {
                var (text, color) = chips[i];
                if (i == rows - 1 && chips.Count > rows)
                    (text, color) = ($"+{chips.Count - rows} more", AsciiArtAssets.Colors.Gray);
                text = Truncate(text, r.W - 2);
                canvas.AddText(alignRight ? r.X + r.W - 1 - text.Length : r.X + 1, r.Y + 1 + i, text, color);
            }
        }

        private static List<(string Text, Color Color)> ArenaStatusChips(Actor a)
        {
            var chips = new List<(string, Color)>();
            void Add(bool on, string text, Color color)
            {
                if (on)
                    chips.Add((text, color));
            }
            Add(a.PoisonPercentOfMaxHealth > 0, $"PSN {a.PoisonPercentOfMaxHealth:0.#}%", Color.FromRgb(90, 220, 60));
            int bleed = a.BleedIntensity + a.PendingBleedFromHits;
            Add(bleed > 0, $"BLD {bleed}", AsciiArtAssets.Colors.Red);
            int burn = a.BurnIntensity + a.PendingBurnFromHits;
            Add(burn > 0, $"BRN {burn}", AsciiArtAssets.Colors.Orange);
            int acid = a.AcidIntensity + a.PendingAcidFromHits;
            Add(acid > 0 || a.AcidArmorReduction > 0, acid > 0 ? $"ACD {acid}" : $"ACD -{a.AcidArmorReduction}AR", Color.FromRgb(200, 230, 60));
            Add(a.IsStunned && a.StunTurnsRemaining > 0, $"STN {a.StunTurnsRemaining}t", AsciiArtAssets.Colors.Yellow);
            Add(a.IsWeakened && a.WeakenTurns > 0, $"WEAK {a.WeakenTurns}t", AsciiArtAssets.Colors.Gray);
            Add(a.RollPenalty != 0 && a.RollPenaltyTurns > 0, $"ACC -{a.RollPenalty}", AsciiArtAssets.Colors.Gray);
            Add(a.VulnerabilityStacks > 0 && a.VulnerabilityTurns > 0, $"VULN x{a.VulnerabilityStacks}", AsciiArtAssets.Colors.Magenta);
            Add(a.ExposeStacks > 0 && a.ExposeTurns > 0, $"EXPO x{a.ExposeStacks}", AsciiArtAssets.Colors.Magenta);
            Add(a.ArmorBreakStacks > 0 && a.ArmorBreakTurns > 0, $"ABRK x{a.ArmorBreakStacks}", AsciiArtAssets.Colors.Orange);
            Add(a.IsMarked && a.MarkTurns > 0, $"MARK {a.MarkTurns}t", AsciiArtAssets.Colors.Red);
            Add(a.IsConfused && a.ConfusionTurns > 0, $"CONF {a.ConfusionTurns}t", AsciiArtAssets.Colors.Purple);
            Add(a.IsSilenced && a.SilenceTurns > 0, $"SIL {a.SilenceTurns}t", AsciiArtAssets.Colors.Purple);
            Add(a.HasCriticalMissPenalty && a.CriticalMissPenaltyTurns > 0, $"FUMBLE {a.CriticalMissPenaltyTurns}t", AsciiArtAssets.Colors.Gray);
            Add(a.HardenStacks > 0 && a.HardenTurns > 0, $"HARD x{a.HardenStacks}", AsciiArtAssets.Colors.Cyan);
            Add(a.FortifyStacks > 0 && a.FortifyTurns > 0, $"FORT x{a.FortifyStacks}", AsciiArtAssets.Colors.Cyan);
            Add(a.FocusStacks > 0 && a.FocusTurns > 0, $"FOCS x{a.FocusStacks}", AsciiArtAssets.Colors.Cyan);
            Add(a.ReflectStacks > 0 && a.ReflectTurns > 0, $"REFL x{a.ReflectStacks}", AsciiArtAssets.Colors.Cyan);
            Add(a.HasPierce && a.PierceTurns > 0, $"PIRC {a.PierceTurns}t", AsciiArtAssets.Colors.Cyan);
            Add(a.HasAbsorb && a.AbsorbTurns > 0, $"ABSB {a.AbsorbTurns}t", AsciiArtAssets.Colors.Cyan);
            Add(a.TemporaryHP > 0 && a.TemporaryHPTurns > 0, $"THP {a.TemporaryHP}", AsciiArtAssets.Colors.Green);
            Add(a.HPRegenStacks > 0 && a.HPRegenTurns > 0, $"REGN x{a.HPRegenStacks}", AsciiArtAssets.Colors.Green);
            if (a is Character ch)
            {
                Add(ch.Effects.SlowTurns > 0, $"SLOW {ch.Effects.SlowTurns}t", AsciiArtAssets.Colors.Blue);
                Add(ch.Effects.HasShield, "SHIELD", AsciiArtAssets.Colors.Cyan);
                Add(ch.Effects.SkipNextTurn, "SKIP", AsciiArtAssets.Colors.Gray);
                Add(ch.Effects.GuaranteeNextSuccess, "SURE HIT", AsciiArtAssets.Colors.Gold);
                Add(ch.Effects.ExtraAttacks > 0, $"XATK x{ch.Effects.ExtraAttacks}", AsciiArtAssets.Colors.Gold);
                Add(ch.Effects.RerollCharges > 0, $"RROL x{ch.Effects.RerollCharges}", AsciiArtAssets.Colors.Gold);
            }
            return chips;
        }

        /// <summary>Framed log panel in the middle of the arena.</summary>
        private void RenderArenaLog(Character player, DisplayBuffer log, int x, int y, int w, int h, IReadOnlyList<string>? enemyNamesForLogAlignment)
        {
            if (h < 4 || w < 10)
                return;
            AddBoardBox(x, y, w, h, BoardDimBorder, Color.FromArgb(215, 6, 6, 6));
            CombatBoardLayout.IsPaintingBoardLog = true;
            try
            {
                new DisplayRenderer(textWriter).Render(log, x + 1, y + 1, w - 2, h - 2, clearContent: false,
                    combatEnemyNamesForPrimaryLineRightAlign: enemyNamesForLogAlignment,
                    combatHeroNameForLineAlignment: player.Name);
            }
            finally
            {
                CombatBoardLayout.IsPaintingBoardLog = false;
            }
        }
    }
}
