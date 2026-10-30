using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Media;
using RPGGame.UI;
using RPGGame.UI.Avalonia.Help;
using RPGGame.UI.Avalonia.Managers;

namespace RPGGame.UI.Avalonia.Renderers
{
    /// <summary>
    /// Skill-tree screen as a tree of unlockable boxes grown bottom-up: the Core trunk rises from tier 0 at the bottom,
    /// forks into one column per branch, and the branch tips gather into the Confluence box at the top. Each tier is
    /// one band of rows, labeled down the left edge. A hybrid rail column sits on the right when the character has one.
    /// Each skill is a pixel box (name, then status and rank pips) colored by its state, and the connector out of a
    /// box lights up once that box is learned. Scrolls by row (wheel, drag, PgUp/PgDn) and opens at the roots.
    /// The details panel sits on the right when wide, below when narrow.
    /// </summary>
    public class SkillTreeRenderer
    {
        /// <summary>Text rows inside a skill box.</summary>
        public const int NodeHeight = 2;
        /// <summary>A box plus the connector row under it.</summary>
        public const int SlotStride = NodeHeight + 1;
        public const int ScrollStep = SlotStride;

        private const int HeaderRows = 3;          // title, depth/status line, blank
        private const int FooterRows = 3;          // legend, keys, back
        private const int BottomDetailRows = 9;    // details block when the panel can't sit on the right
        private const int PanelPad = 2;            // columns between the panel edge and the first box
        private const int TierGutter = 5;          // tier labels ("T12") left of the first column
        private const int LaneGap = 3;             // columns between branch columns (room for a connector)
        private const int RailGap = 5;             // columns before the rail column (divider runs down the middle)
        private const int MinBoxWidth = 14;
        private const int MaxBoxWidth = 24;
        private const int MinRightDetailWidth = 30;
        private const int MaxRightDetailWidth = 42;
        private const int MinTreeWidthForRightDetail = 64;
        private const int BigButtonHeight = 4;           // rows in the details panel's learn button
        private const int BigButtonMinPanelHeight = 16;  // shorter panels fall back to a one-row action line

        private const string LearnedGlyph = "◆";

        private static readonly Color Backdrop = Color.FromRgb(12, 12, 16);
        private static readonly Color PanelBorder = Color.FromRgb(58, 58, 68);
        private static readonly Color DetailFill = Color.FromRgb(14, 14, 19);
        private static readonly Color Dim = Color.FromRgb(78, 78, 88);
        private static readonly Color TitleColor = Color.FromRgb(170, 170, 182);
        private static readonly Color LitWire = Color.FromRgb(96, 176, 104);
        private static readonly Color DimWire = Color.FromRgb(62, 62, 74);
        private static readonly Color RailDivider = Color.FromRgb(84, 52, 92);
        private static readonly Color PipFull = Color.FromRgb(96, 196, 112);
        private static readonly Color PipEmpty = Color.FromRgb(46, 46, 56);

        /// <summary>Border, fill, name and status colors for one box state.</summary>
        private readonly record struct BoxLook(Color Border, Color Fill, Color Name, Color Status, int Thickness);

        private static readonly BoxLook LearnedLook = new(Color.FromRgb(70, 170, 95), Color.FromRgb(18, 38, 24), AsciiArtAssets.Colors.White, Color.FromRgb(110, 200, 120), 1);
        private static readonly BoxLook AvailableLook = new(Color.FromRgb(80, 190, 220), Color.FromRgb(16, 34, 42), AsciiArtAssets.Colors.White, AsciiArtAssets.Colors.Cyan, 2);
        private static readonly BoxLook NeedSpLook = new(Color.FromRgb(190, 160, 60), Color.FromRgb(36, 32, 16), Color.FromRgb(220, 220, 220), AsciiArtAssets.Colors.Yellow, 1);
        private static readonly BoxLook LockedLook = new(Color.FromRgb(84, 84, 98), Color.FromRgb(22, 22, 28), Color.FromRgb(150, 150, 160), Color.FromRgb(110, 110, 120), 1);
        private static readonly BoxLook ClosedLook = new(Color.FromRgb(50, 50, 60), Color.FromRgb(17, 17, 21), Color.FromRgb(92, 92, 102), Color.FromRgb(128, 112, 64), 1);
        private static readonly BoxLook LearnButtonLook = new(AsciiArtAssets.Colors.Gold, Color.FromRgb(58, 46, 12), AsciiArtAssets.Colors.Yellow, Color.FromRgb(220, 200, 140), 3);
        private static readonly BoxLook RailBaseLook = new(AsciiArtAssets.Colors.Magenta, Color.FromRgb(30, 18, 34), AsciiArtAssets.Colors.Magenta, Color.FromRgb(170, 130, 180), 1);

        private readonly GameCanvasControl canvas;
        private readonly List<ClickableElement> clickableElements;
        private int cardBleed;
        private double padX, padY;

        public SkillTreeRenderer(GameCanvasControl canvas, List<ClickableElement> clickableElements)
        {
            this.canvas = canvas;
            this.clickableElements = clickableElements;
        }

        public static int LastViewportHeight { get; private set; } = 16;
        public static int LastContentHeight { get; private set; }
        public static int LastAppliedScrollOffset { get; private set; }

        // ─────────────────────────────────────────────────────────────
        // Box layout (content rows, lane units) — shared with SkillTreeMenuHandler navigation
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Where every display node's box sits. Rows are content rows (row 0 is the top of the canopy); columns are
        /// lane units, fractional when a box is centered (the trunk, the Confluence). The rail column is <see cref="LaneCount"/>.
        /// </summary>
        public sealed class BoxLayout
        {
            public IReadOnlyList<string> Branches { get; init; } = Array.Empty<string>();
            public int LaneCount => Math.Max(1, Branches.Count);
            public bool HasRail { get; init; }
            /// <summary>Per <see cref="SkillTreeDisplayModel.AllNodes"/> index.</summary>
            public double[] ColPos { get; init; } = Array.Empty<double>();
            /// <summary>Per <see cref="SkillTreeDisplayModel.AllNodes"/> index: content row of the box's top.</summary>
            public int[] Top { get; init; } = Array.Empty<int>();
            public List<int> Trunk { get; init; } = new();
            /// <summary>One root-to-tip sequence per entry of <see cref="Branches"/>.</summary>
            public List<List<int>> BranchSeqs { get; init; } = new();
            public List<int> Confluence { get; init; } = new();
            /// <summary>Row with the branch names, just above the fork, or -1 when the trunk doesn't fork.</summary>
            public int TitleRow { get; init; } = -1;
            /// <summary>Row the trunk tip's wire fans out along into the branches, or -1.</summary>
            public int ForkRow { get; init; } = -1;
            /// <summary>Tier → content row of the lowest box in that tier's band (where the tier label sits).</summary>
            public IReadOnlyDictionary<int, int> TierRows { get; init; } = new Dictionary<int, int>();
            public int RailBaseTop { get; init; }
            public int RailTitleRow { get; init; }
            public int ContentHeight { get; init; }
        }

        private static readonly ConditionalWeakTable<SkillTreeDisplayModel, BoxLayout> LayoutCache = new();

        public static BoxLayout GetLayout(SkillTreeDisplayModel model) => LayoutCache.GetValue(model, ComputeBoxLayout);

        /// <summary>
        /// Grows the tree bottom-up: the Core trunk is a centered stack with tier 0 at the bottom, it forks into one
        /// column per branch, and the Confluence crowns the top. Every tier is one band of rows across all columns,
        /// so a skill sits at its tier's height and gaps in a branch show as a longer stem.
        /// Built in rows counted from the bottom, then flipped into content rows.
        /// </summary>
        private static BoxLayout ComputeBoxLayout(SkillTreeDisplayModel model)
        {
            var primary = model.PrimaryNodes;
            var order = ComputeContentLayout(primary);
            // Same order SkillTreePrerequisites unlocks a branch in: tier, then on-screen stack order, then name.
            List<int> Seq(string branch) => Enumerable.Range(0, primary.Count)
                .Where(i => string.Equals(primary[i].Branch ?? "", branch, StringComparison.OrdinalIgnoreCase))
                .OrderBy(i => primary[i].Tier)
                .ThenByDescending(i => order.Tops.TryGetValue(primary[i].Id, out int t) ? t : 0)
                .ThenBy(i => primary[i].Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var branches = BuildBranchOrder(primary).Where(b => !IsCenterBranch(b)).ToList();
            int lanes = Math.Max(1, branches.Count);
            double center = (lanes - 1) / 2.0;
            var colPos = new double[model.AllNodes.Count];
            var bottom = new int[model.AllNodes.Count]; // rows from the content bottom to the box's lower edge

            var trunk = Seq("Core");
            var seqs = branches.Select(Seq).ToList();
            var confluence = Seq("Confluence");
            var seqIndex = new Dictionary<int, int>();
            foreach (var seq in seqs.Append(trunk).Append(confluence))
            {
                for (int k = 0; k < seq.Count; k++)
                    seqIndex[seq[k]] = k;
            }
            foreach (int i in trunk.Concat(confluence))
                colPos[i] = center;
            for (int lane = 0; lane < seqs.Count; lane++)
            {
                foreach (int i in seqs[lane])
                    colPos[i] = lane;
            }

            // Each box owns a slot: the box, then the connector row above it.
            int f = 1;
            int forkF = -1, titleF = -1;
            var tierF = new Dictionary<int, int>();
            foreach (var band in Enumerable.Range(0, primary.Count).GroupBy(i => primary[i].Tier).OrderBy(g => g.Key))
            {
                if (forkF < 0 && trunk.Count > 0 && band.Any(i => !IsCenterBranch(primary[i].Branch)))
                {
                    // Fork: the fan-out wire row, then the branch names above it, then a spacer.
                    forkF = f;
                    titleF = f + 1;
                    f += 3;
                }
                int stack = 1;
                foreach (var cell in band.GroupBy(i => colPos[i]).ToList())
                {
                    var list = cell.OrderBy(i => seqIndex.TryGetValue(i, out int k) ? k : 0).ToList();
                    if (list.Count > 1 && cell.Key == center && list.Count <= lanes)
                    {
                        // A Core fork: same-tier choices side by side, centered on the trunk.
                        for (int k = 0; k < list.Count; k++)
                        {
                            colPos[list[k]] = center + k - (list.Count - 1) / 2.0;
                            bottom[list[k]] = f;
                        }
                        continue;
                    }
                    for (int k = 0; k < list.Count; k++)
                        bottom[list[k]] = f + k * SlotStride;
                    stack = Math.Max(stack, list.Count);
                }
                tierF[band.Key] = f;
                f += stack * SlotStride;
            }

            // Rail: the base box (what opens the hybrid tree) level with the roots, its skills stacked above.
            int railTitleF = 0;
            for (int i = 0; i < model.RailNodes.Count; i++)
            {
                int all = model.PrimaryCount + i;
                colPos[all] = lanes;
                bottom[all] = 1 + (i + 1) * SlotStride;
            }
            if (model.HasRail)
            {
                railTitleF = 1 + model.RailNodes.Count * SlotStride + NodeHeight;
                f = Math.Max(f, railTitleF + 1);
            }

            int height = f;
            int Row(int fromBottom) => fromBottom < 0 ? -1 : height - 1 - fromBottom;
            var top = bottom.Select(b => height - b - NodeHeight).ToArray();

            return new BoxLayout
            {
                Branches = branches,
                HasRail = model.HasRail,
                ColPos = colPos,
                Top = top,
                Trunk = trunk,
                BranchSeqs = seqs,
                Confluence = confluence,
                TitleRow = Row(titleF),
                ForkRow = Row(forkF),
                TierRows = tierF.ToDictionary(kv => kv.Key, kv => height - kv.Value - NodeHeight),
                RailBaseTop = height - 1 - NodeHeight,
                RailTitleRow = Row(railTitleF),
                ContentHeight = height
            };
        }

        public static int GetDisplayContentHeight(SkillTreeDisplayModel model) => GetLayout(model).ContentHeight;

        public static int GetNodeTopForDisplay(SkillTreeDisplayModel model, int allIndex) => GetLayout(model).Top[allIndex];

        public static int GetNodeBottomForDisplay(SkillTreeDisplayModel model, int allIndex) =>
            GetNodeTopForDisplay(model, allIndex) + NodeHeight;

        /// <summary>Column in lane units; the trunk and Confluence sit between lanes (e.g. 1.5).</summary>
        public static double GetColumnForDisplay(SkillTreeDisplayModel model, int allIndex) =>
            GetLayout(model).ColPos[allIndex];

        // ─────────────────────────────────────────────────────────────
        // Content order (tier bands) — SkillTreePrerequisites sequences branches from this
        // ─────────────────────────────────────────────────────────────

        public static int GetMaxTier(IReadOnlyList<SkillTreeNodeDefinition> nodes) =>
            nodes == null || nodes.Count == 0 ? 0 : nodes.Max(n => n.Tier);

        /// <summary>
        /// Tier bands, highest tiers toward y=0, roots toward larger Y. Same branch+tier siblings stack downward
        /// within the band (alphabetical by name). <see cref="SkillTreePrerequisites"/> reads this order to sequence
        /// each branch, so keep it stable; the box layout follows the same order.
        /// </summary>
        public static ContentLayout ComputeContentLayout(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var empty = new ContentLayout(0, new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<int, (int top, int height)>());
            if (nodes == null || nodes.Count == 0)
                return empty;

            var branchOrder = BuildBranchOrder(nodes);
            int centerCol = Math.Max(0, branchOrder.Count / 2);
            int maxTier = GetMaxTier(nodes);
            var groups = GroupByCell(nodes, branchOrder, centerCol);

            int[] stackDepth = new int[maxTier + 1];
            foreach (var kv in groups)
                stackDepth[kv.Key.tier] = Math.Max(stackDepth[kv.Key.tier], kv.Value.Count);

            var bands = new Dictionary<int, (int top, int height)>();
            int y = 0;
            for (int t = maxTier; t >= 0; t--)
            {
                int h = Math.Max(1, stackDepth[t]) * NodeHeight;
                bands[t] = (y, h);
                y += h;
            }

            var tops = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in groups)
            {
                int bandTop = bands[kv.Key.tier].top;
                for (int i = 0; i < kv.Value.Count; i++)
                    tops[kv.Value[i].Id] = bandTop + i * NodeHeight;
            }

            return new ContentLayout(y, tops, bands);
        }

        private static Dictionary<(int col, int tier), List<SkillTreeNodeDefinition>> GroupByCell(
            IReadOnlyList<SkillTreeNodeDefinition> nodes,
            IReadOnlyList<string> branchOrder,
            int centerCol)
        {
            var groups = new Dictionary<(int col, int tier), List<SkillTreeNodeDefinition>>();
            foreach (var node in nodes)
            {
                int col = ResolveColumn(node, branchOrder, centerCol);
                var key = (col, node.Tier);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<SkillTreeNodeDefinition>();
                    groups[key] = list;
                }
                list.Add(node);
            }
            foreach (var list in groups.Values)
                list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return groups;
        }

        // ─────────────────────────────────────────────────────────────
        // Scrolling
        // ─────────────────────────────────────────────────────────────

        /// <summary>Row-precise so wheel and drag move the tree smoothly; 0 is the canopy, max is the roots.</summary>
        public static int NormalizeScrollOffset(int scrollOffset, int contentHeight, int viewportHeight)
        {
            int max = Math.Max(0, contentHeight - viewportHeight);
            return Math.Clamp(scrollOffset, 0, max);
        }

        /// <summary>Scroll just far enough to show the box, keeping its connector row in view too.</summary>
        public static int EnsureNodeVisible(int scrollOffset, int nodeTop, int nodeBottom, int viewportHeight)
        {
            if (nodeTop - 1 < scrollOffset)
                return Math.Max(0, nodeTop - 1);
            if (nodeBottom + 1 > scrollOffset + viewportHeight)
                return Math.Max(0, nodeBottom + 1 - viewportHeight);
            return scrollOffset;
        }

        /// <summary>Screen cells of the tree viewport from the last render, for wheel and drag hit-tests.</summary>
        public static (int X, int Y, int Width, int Height) LastTreeBounds { get; private set; }

        public static bool TreeBoundsContain(int gridX, int gridY)
        {
            var b = LastTreeBounds;
            return b.Width > 0 && gridX >= b.X && gridX < b.X + b.Width && gridY >= b.Y && gridY < b.Y + b.Height;
        }

        // ─────────────────────────────────────────────────────────────
        // Render
        // ─────────────────────────────────────────────────────────────

        public int RenderSkillTree(
            int x,
            int y,
            int width,
            int height,
            Character player,
            int selectedIndex,
            int scrollOffset,
            string? statusMessage)
        {
            // Wipe prior center content so scroll/selection redraws never stack text, boxes or wires.
            clickableElements.Clear();
            canvas.ClearTextInArea(x, y, width, height);
            canvas.ClearBoxesInArea(x, y, width, height);
            canvas.ClearLinesInArea(x, y, width, height);

            int left = x + 1;
            int contentWidth = Math.Max(40, width - 2);
            int bottomLimit = y + height;

            var model = SkillTreeService.BuildDisplayModel(player.Progression);
            var tree = model.PrimaryTree;
            var primary = model.PrimaryPath;

            if (tree == null || primary == null || model.PrimaryNodes.Count == 0)
            {
                int ey = y + 1;
                canvas.AddText(left, ey, "SKILL TREE", AsciiArtAssets.Colors.Gold);
                ey += 2;
                foreach (var line in TextWrapper.WrapText(
                             "Equip a weapon and earn Skill Points to open your primary path tree.",
                             contentWidth))
                {
                    canvas.AddText(left, ey++, line, AsciiArtAssets.Colors.Gray);
                }
                ey++;
                AddBackOption(left, ey, contentWidth);
                LastViewportHeight = 0;
                LastContentHeight = 0;
                LastAppliedScrollOffset = 0;
                LastTreeBounds = default;
                return ey - y + 1;
            }

            var nodes = model.AllNodes;
            selectedIndex = Math.Clamp(selectedIndex, 0, nodes.Count - 1);
            var depth = DepthInfo.For(player.Progression, tree, primary.Value);
            var layout = GetLayout(model);

            // ── Header ───────────────────────────────────────────────
            int headerY = y + 1;
            DrawHeader(player, model, left, headerY, contentWidth, depth);

            // ── Geometry ─────────────────────────────────────────────
            int lanes = layout.LaneCount;
            bool showRail = layout.HasRail;
            int rightDetailWidth = Math.Clamp(contentWidth * 3 / 10, MinRightDetailWidth, MaxRightDetailWidth);
            bool detailOnRight = contentWidth - rightDetailWidth - 2 >= MinTreeWidthForRightDetail;
            int treeAreaWidth = detailOnRight ? contentWidth - rightDetailWidth - 2 : contentWidth;

            int fixedCols = 2 * PanelPad + TierGutter + (lanes - 1) * LaneGap + (showRail ? RailGap : 0);
            int boxW = Math.Clamp((treeAreaWidth - fixedCols) / (lanes + (showRail ? 1 : 0)), MinBoxWidth, MaxBoxWidth);
            int lanesWidth = lanes * boxW + (lanes - 1) * LaneGap;
            int panelWidth = 2 * PanelPad + TierGutter + lanesWidth + (showRail ? RailGap + boxW : 0);
            int panelLeft = left;
            int treeLeft = panelLeft + PanelPad + TierGutter;
            int railX = treeLeft + lanesWidth + RailGap;

            int panelTop = headerY + HeaderRows;
            int footerTop = bottomLimit - FooterRows;
            int belowPanelReserve = detailOnRight ? 0 : BottomDetailRows;
            int viewportHeight = Math.Max(NodeHeight, footerTop - belowPanelReserve - panelTop - 2);
            int contentHeight = layout.ContentHeight;
            viewportHeight = Math.Min(viewportHeight, Math.Max(NodeHeight, contentHeight + 1));
            scrollOffset = NormalizeScrollOffset(scrollOffset, contentHeight, viewportHeight);
            LastViewportHeight = viewportHeight;
            LastContentHeight = contentHeight;
            LastAppliedScrollOffset = scrollOffset;

            var view = new Viewport(panelTop + 1, panelTop + 1 + viewportHeight, scrollOffset);
            int panelHeight = viewportHeight + 2;
            canvas.AddBox(panelLeft, panelTop, panelWidth, panelHeight, PanelBorder, Backdrop);
            LastTreeBounds = (panelLeft, panelTop, panelWidth, panelHeight);

            // Cards draw their border a few pixels outside their two text rows; wires meet that border.
            double charW = canvas.GetCharWidth(), charH = canvas.GetCharHeight();
            cardBleed = charH > 0 ? Math.Max(2, (int)Math.Round(charH * 0.2)) : 0;
            var geo = new Geometry(layout, treeLeft, railX, boxW,
                charW > 0 ? cardBleed / charW : 0, charH > 0 ? cardBleed / charH : 0);
            padX = geo.PadX;
            padY = geo.PadY;

            // ── Titles, wires, boxes ─────────────────────────────────
            DrawTierGutter(layout, view, panelLeft + PanelPad, depth);
            DrawSectionTitles(model, layout, geo, view, showRail);
            if (showRail)
                Wire(view, false, RailDivider, 1.0, (railX - RailGap / 2.0, view.Scroll), (railX - RailGap / 2.0, view.Scroll + viewportHeight));
            DrawWires(player, model, layout, geo, view);

            for (int i = 0; i < model.PrimaryCount; i++)
            {
                int sy = view.ToScreen(layout.Top[i]);
                if (sy < view.Top || sy + NodeHeight > view.Bottom)
                    continue; // no partial boxes while scrolling
                DrawNodeBox(player, nodes[i], i, geo.X(i), sy, boxW, i == selectedIndex, depth, isRail: false);
            }
            if (showRail)
                DrawRail(player, model, layout, view, railX, boxW, selectedIndex);

            bool moreAbove = scrollOffset > 0;
            bool moreBelow = scrollOffset + viewportHeight < contentHeight;
            if (moreAbove)
                DrawPanelTag(panelLeft + panelWidth - 10, panelTop, $"{AsciiArtAssets.UIElements.ArrowUp} more");
            if (moreBelow)
                DrawPanelTag(panelLeft + panelWidth - 10, panelTop + panelHeight - 1, $"{AsciiArtAssets.UIElements.ArrowDown} more");
            if (moreAbove || moreBelow)
                MenuMouseScrollHint.DrawRightAligned(canvas, left, contentWidth, footerTop + 2);

            // ── Details ──────────────────────────────────────────────
            if (detailOnRight)
            {
                int detailX = panelLeft + panelWidth + 2;
                int detailW = Math.Min(rightDetailWidth, left + contentWidth - detailX);
                DrawDetailPanel(player, model, depth, selectedIndex, detailX, panelTop, detailW, panelHeight, statusMessage);
            }
            else
            {
                DrawDetailPanel(player, model, depth, selectedIndex, left, panelTop + panelHeight, Math.Min(contentWidth, panelWidth), BottomDetailRows, statusMessage);
            }

            // ── Footer ───────────────────────────────────────────────
            DrawFooter(left, footerTop, contentWidth);
            return height;
        }

        // ─────────────────────────────────────────────────────────────
        // Header / footer
        // ─────────────────────────────────────────────────────────────

        private void DrawHeader(Character player, SkillTreeDisplayModel model, int left, int y, int width, DepthInfo depth)
        {
            var tree = model.PrimaryTree!;
            var primary = model.PrimaryPath!.Value;
            int lifetime = player.Progression.GetClassPoints(primary);
            int spent = player.Progression.GetSpentSkillPoints(primary);
            int available = player.Progression.GetAvailableSkillPoints(primary);

            int cx = left;
            canvas.AddText(cx, y, "SKILL TREE", AsciiArtAssets.Colors.Gold);
            cx += 11;
            canvas.AddText(cx, y, "──", PanelBorder);
            cx += 3;
            canvas.AddText(cx, y, tree.Title, AsciiArtAssets.Colors.White);
            cx += tree.Title.Length + 3;

            string sp = $"{primary} SP: {available} free · {spent} spent · {lifetime} earned";
            var viewable = SkillTreeService.GetViewablePaths(player.Progression);
            string? switchText = null;
            if (viewable.Count > 1)
            {
                string others = string.Join("/", viewable.Where(p => p != primary));
                switchText = $"[T] {others} {AsciiArtAssets.UIElements.ArrowRight}";
            }

            int rightEdge = left + width;
            if (switchText != null && rightEdge - switchText.Length > cx + sp.Length + 2)
            {
                int sx = rightEdge - switchText.Length;
                clickableElements.Add(new ClickableElement
                {
                    X = sx,
                    Y = y,
                    Width = switchText.Length,
                    Height = 1,
                    Type = ElementType.Button,
                    Value = "t",
                    DisplayText = switchText
                });
                canvas.AddText(sx, y, switchText, AsciiArtAssets.Colors.Gold);
                rightEdge = sx - 3;
            }
            if (cx + sp.Length <= rightEdge)
                canvas.AddText(Math.Max(cx, rightEdge - sp.Length), y, sp, AsciiArtAssets.Colors.Cyan);

            // Depth line: what opens next, plus the secondary path's SP when a rail is shown.
            string depthText = depth.NextTier is int nt
                ? $"{AsciiArtAssets.UIElements.ArrowRight} Tier {nt} skills open at {depth.Gate(nt)} {primary} pts · {depth.Gate(nt) - depth.Points} to go"
                : $"{LearnedGlyph} Every tier open ({depth.Points} {primary} pts)";
            canvas.AddText(left, y + 1, Truncate(depthText, width), depth.NextTier != null ? AsciiArtAssets.Colors.Yellow : AsciiArtAssets.Colors.Green);

            if (model.HasRail && model.SecondaryPath != null)
            {
                var sec = model.SecondaryPath.Value;
                string duo = string.IsNullOrWhiteSpace(model.DuoName) ? "Shared" : model.DuoName!;
                var hybrid = ResolveRailHybrid(player.Progression)?.info;
                string railText = hybrid == null
                    ? $"{duo} rail · {sec} SP: {player.Progression.GetAvailableSkillPoints(sec)} free"
                    : HybridTrees.IsUnlocked(player.Progression, hybrid)
                        ? $"{hybrid.Key} hybrid · spends {hybrid.ClassA} + {hybrid.ClassB} SP"
                        : $"{hybrid.Key} hybrid · opens at {HybridTrees.UnlockPointsPerParent} {hybrid.ClassA} + {HybridTrees.UnlockPointsPerParent} {hybrid.ClassB} pts";
                int rx = left + depthText.Length + 4;
                if (rx + railText.Length <= left + width)
                    canvas.AddText(rx, y + 1, railText, AsciiArtAssets.Colors.Magenta);
            }
        }

        private void DrawFooter(int left, int y, int width)
        {
            int cx = left + 1;
            void Key(BoxLook look, string label)
            {
                canvas.AddBox(cx, y, 2, 1, look.Border, look.Fill, 0, look.Thickness);
                canvas.AddText(cx + 3, y, label, AsciiArtAssets.Colors.Gray);
                cx += 3 + label.Length + 3;
            }
            Key(LearnedLook, "learned");
            Key(AvailableLook, "can learn");
            Key(NeedSpLook, "need SP");
            Key(LockedLook, "learn the one before");
            Key(ClosedLook, "needs more class pts");

            canvas.AddText(left + 1, y + 1,
                Truncate("Arrows move · L / Enter learn · T switch tree · Wheel / drag / PgUp PgDn scroll · Click select", width - 1),
                Dim);
            AddBackOption(left, y + 2, width);
        }

        /// <summary>Small label sitting on the panel border (a fill box masks the border line behind it).</summary>
        private void DrawPanelTag(int x, int y, string text)
        {
            canvas.AddBox(x - 1, y, text.Length + 2, 1, Backdrop, Backdrop);
            canvas.AddText(x, y, text, AsciiArtAssets.Colors.Yellow);
        }

        // ─────────────────────────────────────────────────────────────
        // Titles and wires
        // ─────────────────────────────────────────────────────────────

        private void DrawSectionTitles(SkillTreeDisplayModel model, BoxLayout layout, Geometry geo, Viewport view, bool showRail)
        {
            if (showRail)
            {
                int ry = view.ToScreen(layout.RailTitleRow);
                if (view.Contains(ry))
                    canvas.AddText(geo.RailX, ry, Truncate((model.RailTitle ?? model.DuoName ?? "SHARED").ToUpperInvariant(), geo.BoxW), AsciiArtAssets.Colors.Magenta);
            }
            if (layout.TitleRow < 0)
                return;
            int ty = view.ToScreen(layout.TitleRow);
            if (!view.Contains(ty))
                return;
            // Left-aligned and short so the fork wire rising into the column's center stays clear.
            for (int lane = 0; lane < layout.Branches.Count; lane++)
                canvas.AddText(geo.LaneX(lane), ty, Truncate(layout.Branches[lane].ToUpperInvariant(), geo.BoxW / 2 - 1), TitleColor);
        }

        /// <summary>Tier numbers down the left edge: open tiers gray, the next tier to open yellow, later ones dim.</summary>
        private void DrawTierGutter(BoxLayout layout, Viewport view, int x, DepthInfo depth)
        {
            foreach (var (tier, row) in layout.TierRows)
            {
                int sy = view.ToScreen(row);
                if (!view.Contains(sy))
                    continue;
                Color color = depth.IsOpen(tier) ? AsciiArtAssets.Colors.Gray
                    : tier == depth.NextTier ? AsciiArtAssets.Colors.Yellow
                    : Dim;
                canvas.AddText(x, sy, $"T{tier}", color);
            }
        }

        /// <summary>
        /// Wires run upward: the trunk links box to box, the trunk tip fans out along the fork row into every branch,
        /// each branch climbs its column, and the Confluence gathers its requirements (or every branch tip) from below.
        /// A wire is lit once the box it leaves from is learned.
        /// </summary>
        private void DrawWires(Character player, SkillTreeDisplayModel model, BoxLayout layout, Geometry geo, Viewport view)
        {
            var primary = model.PrimaryNodes;
            bool Learned(int i) => player.Progression.GetSkillRank(primary[i].Id) > 0;
            double Under(int i) => layout.Top[i] + NodeHeight + 0.5;

            var trunk = layout.Trunk;
            int IndexOf(string id) => Enumerable.Range(0, primary.Count)
                .FirstOrDefault(i => string.Equals(primary[i].Id, id, StringComparison.OrdinalIgnoreCase), -1);
            // Every Core skill is fed by each skill on the Core tier below: forks split out and join back together.
            foreach (int c in trunk)
            {
                if (model.PrimaryTree == null)
                    break;
                foreach (var option in SkillTreePrerequisites.GetCorePredecessorOptions(model.PrimaryTree, primary[c]))
                {
                    int p = IndexOf(option.Id);
                    if (p >= 0)
                        Elbow(view, Learned(p), geo, p, c, Under(c));
                }
            }

            if (trunk.Count > 0)
            {
                int tip = trunk[^1];
                foreach (var seq in layout.BranchSeqs.Where(s => s.Count > 0))
                    Elbow(view, Learned(tip), geo, tip, seq[0], layout.ForkRow >= 0 ? layout.ForkRow + 0.5 : Under(seq[0]));
            }

            foreach (var seq in layout.BranchSeqs)
            {
                for (int k = 1; k < seq.Count; k++)
                    Elbow(view, Learned(seq[k - 1]), geo, seq[k - 1], seq[k], Under(seq[k]));
            }

            var conf = layout.Confluence;
            for (int k = 0; k < conf.Count; k++)
            {
                int c = conf[k];
                double joinY = Under(c);
                if (k > 0)
                {
                    Elbow(view, Learned(conf[k - 1]), geo, conf[k - 1], c, joinY);
                    continue;
                }
                var sources = (primary[c].Requires ?? new List<string>())
                    .Select(id => Enumerable.Range(0, primary.Count).FirstOrDefault(i => string.Equals(primary[i].Id, id, StringComparison.OrdinalIgnoreCase), -1))
                    .Where(i => i >= 0)
                    .ToList();
                if (sources.Count == 0)
                    sources = layout.BranchSeqs.Where(s => s.Count > 0).Select(s => s[^1]).ToList();
                if (sources.Count == 0 && trunk.Count > 0)
                    sources.Add(trunk[^1]);
                foreach (int s in sources)
                {
                    bool lit = Learned(s);
                    var column = layout.BranchSeqs.FirstOrDefault(seq => seq.Contains(s));
                    if (column != null && column[^1] != s)
                    {
                        // Not its column's tip: leave from the side and climb the gutter so the wire skips the boxes above it.
                        double gutter = geo.X(s) + geo.BoxW + LaneGap / 2.0;
                        Wire(view, lit, (geo.Right(s), layout.Top[s] + 1), (gutter, layout.Top[s] + 1), (gutter, joinY), (geo.CenterX(c), joinY), (geo.CenterX(c), geo.BottomEdge(c)));
                    }
                    else
                    {
                        Elbow(view, lit, geo, s, c, joinY);
                    }
                }
            }
        }

        /// <summary>Up from <paramref name="from"/>'s top to <paramref name="midY"/>, across, then up into <paramref name="to"/>'s bottom.</summary>
        private void Elbow(Viewport view, bool lit, Geometry geo, int from, int to, double midY) =>
            Wire(view, lit,
                (geo.CenterX(from), geo.TopEdge(from)),
                (geo.CenterX(from), midY),
                (geo.CenterX(to), midY),
                (geo.CenterX(to), geo.BottomEdge(to)));

        private void Wire(Viewport view, bool lit, params (double x, double y)[] points) =>
            Wire(view, lit, lit ? LitWire : DimWire, lit ? 2.0 : 1.2, points);

        /// <summary>Axis-aligned polyline in content rows, clipped to the viewport.</summary>
        private void Wire(Viewport view, bool lit, Color color, double thickness, params (double x, double y)[] points)
        {
            for (int i = 1; i < points.Length; i++)
            {
                double x1 = points[i - 1].x, x2 = points[i].x;
                double y1 = view.Top + points[i - 1].y - view.Scroll;
                double y2 = view.Top + points[i].y - view.Scroll;
                if (Math.Abs(x1 - x2) < 0.001 && Math.Abs(y1 - y2) < 0.001)
                    continue; // straight elbows collapse their crossbar
                if (Math.Abs(y1 - y2) < 0.001)
                {
                    if (y1 < view.Top || y1 > view.Bottom)
                        continue;
                }
                else
                {
                    double lo = Math.Max(Math.Min(y1, y2), view.Top);
                    double hi = Math.Min(Math.Max(y1, y2), view.Bottom);
                    if (hi <= lo)
                        continue;
                    y1 = lo;
                    y2 = hi;
                }
                canvas.AddLine(x1, y1, x2, y2, color, thickness);
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Boxes
        // ─────────────────────────────────────────────────────────────

        private void DrawRail(Character player, SkillTreeDisplayModel model, BoxLayout layout, Viewport view, int railX, int boxW, int selectedIndex)
        {
            var railNodes = model.RailNodes;
            var hybrid = ResolveRailHybrid(player.Progression);
            DepthInfo? railDepth = hybrid?.tree != null
                ? DepthInfo.For(player.Progression, hybrid.Value.tree, hybrid.Value.info.ParentA)
                : null;

            // Base box at the foot of the column: what opens the hybrid tree.
            int baseY = view.ToScreen(layout.RailBaseTop);
            if (hybrid != null && baseY >= view.Top && baseY + NodeHeight <= view.Bottom)
            {
                var h = hybrid.Value.info;
                int need = HybridTrees.UnlockPointsPerParent;
                int a = player.Progression.GetClassPoints(h.ParentA);
                int b = player.Progression.GetClassPoints(h.ParentB);
                bool open = HybridTrees.IsUnlocked(player.Progression, h);
                var look = open ? LearnedLook with { Name = AsciiArtAssets.Colors.Magenta } : RailBaseLook;
                DrawCard(railX, baseY, boxW, look.Border, look.Fill, look.Thickness);
                canvas.AddText(railX + 1, baseY, Truncate($"{h.Key} base", boxW - 2), look.Name);
                canvas.AddText(railX + 1, baseY + 1, Truncate(open ? "open" : $"{h.ClassA} {a}/{need} · {h.ClassB} {b}/{need}", boxW - 2), look.Status);
                if (railNodes.Count > 0)
                {
                    // Clicking the base selects the first hybrid skill so the details panel explains the rules.
                    clickableElements.Add(new ClickableElement
                    {
                        X = railX,
                        Y = baseY,
                        Width = boxW,
                        Height = NodeHeight,
                        Type = ElementType.Button,
                        Value = (model.PrimaryCount + 1).ToString(),
                        DisplayText = $"{h.Key} base"
                    });
                }
            }

            for (int i = 0; i < railNodes.Count; i++)
            {
                int allIndex = model.PrimaryCount + i;
                int sy = view.ToScreen(layout.Top[allIndex]);
                if (sy < view.Top || sy + NodeHeight > view.Bottom)
                    continue;
                DrawNodeBox(player, railNodes[i], allIndex, railX, sy, boxW, allIndex == selectedIndex, railDepth, isRail: true);
            }
        }

        /// <summary>The hybrid tree shown on the rail (primary + secondary pair), if the rail is a hybrid tree.</summary>
        private static (HybridInfo info, SkillTreeDefinition tree)? ResolveRailHybrid(CharacterProgression progression)
        {
            var pa = progression.GetPrimaryClassWeaponType();
            var pb = progression.GetSecondaryClassWeaponType();
            if (pa == null || pb == null || HybridTrees.ForPair(pa.Value, pb.Value) is not HybridInfo h)
                return null;
            var tree = SkillTreeService.Trees.Trees.FirstOrDefault(t => string.Equals(t.ClassKey, h.Key, StringComparison.OrdinalIgnoreCase));
            return tree == null ? null : (h, tree);
        }

        /// <summary>One skill: a box colored by state with the name on top, and status plus rank pips below.</summary>
        private void DrawNodeBox(
            Character player,
            SkillTreeNodeDefinition node,
            int allIndex,
            int x,
            int y,
            int w,
            bool selected,
            DepthInfo? depth,
            bool isRail)
        {
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            int rank = player.Progression.GetSkillRank(node.Id);
            int maxRank = Math.Max(1, node.MaxRank);
            bool tierClosed = depth != null && !depth.IsOpen(node.Tier) && rank == 0;
            var look = LookFor(state, tierClosed);
            if (isRail && !tierClosed && state != SkillTreeService.NodeViewState.Locked && state != SkillTreeService.NodeViewState.WrongPath)
                look = look with { Name = AsciiArtAssets.Colors.Magenta };

            Color border = selected ? AsciiArtAssets.Colors.Gold : look.Border;
            Color fill = selected ? Lighten(look.Fill, 0.12) : look.Fill;
            DrawCard(x, y, w, border, fill, selected ? 3 : look.Thickness);
            canvas.AddText(x + 1, y, Truncate(node.Name, w - 2), selected ? AsciiArtAssets.Colors.Yellow : look.Name);

            int statusRoom = w - 2;
            if (maxRank > 1 && !tierClosed)
            {
                // Rank pips: one small filled box per rank, right-aligned on the status row.
                int pips = Math.Min(maxRank, Math.Max(1, w / 3));
                int filled = (int)Math.Round(rank * (double)pips / maxRank);
                int px = x + w - 1 - pips;
                for (int i = 0; i < pips; i++)
                    canvas.AddBox(px + i, y + 1, 1, 1, fill, i < filled ? PipFull : PipEmpty, 0, 2);
                statusRoom = px - (x + 1) - 1;
            }
            string status = BoxStatus(node, state, rank, maxRank, depth, tierClosed, isRail);
            if (statusRoom > 0 && status.Length > 0)
                canvas.AddText(x + 1, y + 1, Truncate(status, statusRoom), look.Status);

            clickableElements.Add(new ClickableElement
            {
                X = x,
                Y = y,
                Width = w,
                Height = NodeHeight,
                Type = ElementType.Button,
                Value = (allIndex + 1).ToString(),
                DisplayText = node.Name
            });
        }

        /// <summary>
        /// A skill card: fill bleeds <see cref="cardBleed"/> pixels past its text rows and the border is drawn as
        /// lines on that edge, so names and status never sit on the border.
        /// </summary>
        private void DrawCard(int x, int y, int w, Color border, Color fill, int thickness, int h = NodeHeight)
        {
            canvas.AddBox(x, y, w, h, fill, fill, cardBleed);
            double l = x - padX, r = x + w + padX, t = y - padY, b = y + h + padY;
            canvas.AddLine(l, t, r, t, border, thickness);
            canvas.AddLine(r, t, r, b, border, thickness);
            canvas.AddLine(r, b, l, b, border, thickness);
            canvas.AddLine(l, b, l, t, border, thickness);
        }

        private static BoxLook LookFor(SkillTreeService.NodeViewState state, bool tierClosed)
        {
            if (tierClosed)
                return ClosedLook;
            return state switch
            {
                SkillTreeService.NodeViewState.Maxed => LearnedLook,
                SkillTreeService.NodeViewState.Available => AvailableLook,
                SkillTreeService.NodeViewState.Unaffordable => NeedSpLook,
                SkillTreeService.NodeViewState.Locked => LockedLook,
                _ => ClosedLook
            };
        }

        /// <summary>Status row: what the box is waiting on, in as few characters as possible.</summary>
        private static string BoxStatus(SkillTreeNodeDefinition node, SkillTreeService.NodeViewState state, int rank, int maxRank,
            DepthInfo? depth, bool tierClosed, bool isRail)
        {
            if (tierClosed)
                return isRail ? $"needs {depth!.Gate(node.Tier)}+{depth.Gate(node.Tier)} pts" : $"opens at {depth!.Gate(node.Tier)} pts";
            string cost = node.Cost <= 0 ? "free" : $"{node.Cost} SP";
            switch (state)
            {
                case SkillTreeService.NodeViewState.Maxed:
                    return maxRank > 1 ? "maxed" : "learned";
                case SkillTreeService.NodeViewState.Available:
                    return rank > 0 ? $"rank up · {cost}" : $"learn · {cost}";
                case SkillTreeService.NodeViewState.Unaffordable:
                    return $"need {cost}";
                case SkillTreeService.NodeViewState.Locked:
                    var tree = SkillTreePrerequisites.FindOwnerTree(node);
                    var explicitReq = (node.Requires ?? new List<string>()).FirstOrDefault();
                    if (explicitReq != null && SkillTreeService.Trees.Trees.SelectMany(t => t.Nodes)
                            .FirstOrDefault(n => string.Equals(n.Id, explicitReq, StringComparison.OrdinalIgnoreCase)) is SkillTreeNodeDefinition req)
                        return $"after {req.Name}";
                    var preds = tree != null ? SkillTreePrerequisites.GetPredecessorOptions(tree, node) : Array.Empty<SkillTreeNodeDefinition>();
                    return preds.Count > 0 ? $"after {string.Join(" or ", preds.Select(p => p.Name))}" : isRail ? "after base" : "after Core";
                default:
                    return "other path";
            }
        }

        private static Color Lighten(Color c, double amount) => Color.FromRgb(
            (byte)(c.R + (255 - c.R) * amount),
            (byte)(c.G + (255 - c.G) * amount),
            (byte)(c.B + (255 - c.B) * amount));

        // ─────────────────────────────────────────────────────────────
        // Details panel
        // ─────────────────────────────────────────────────────────────

        private void DrawDetailPanel(
            Character player,
            SkillTreeDisplayModel model,
            DepthInfo depth,
            int selectedIndex,
            int x,
            int y,
            int width,
            int height,
            string? statusMessage)
        {
            if (width < 16 || height < 4)
                return;

            var node = model.AllNodes[Math.Clamp(selectedIndex, 0, model.AllNodes.Count - 1)];
            bool onRail = model.IsRailIndex(selectedIndex);
            var state = SkillTreeService.GetNodeState(player.Progression, node);
            var ownerPath = SkillTreeService.FindOwnerPath(node);
            int rank = player.Progression.GetSkillRank(node.Id);
            int maxRank = Math.Max(1, node.MaxRank);
            Color border = onRail ? AsciiArtAssets.Colors.Magenta : AsciiArtAssets.Colors.Gold;
            int inner = width - 4;
            int bottom = y + height - 1;

            DrawCard(x, y, width, border, DetailFill, 1, height);
            canvas.AddText(x + 2, y, Truncate(node.Name.ToUpperInvariant(), inner), AsciiArtAssets.Colors.White);
            canvas.AddLine(x + 1, y + 1, x + width - 1, y + 1, Color.FromArgb(120, border.R, border.G, border.B), 1.0);

            // Action: a big button card across the bottom when the panel is tall enough, else the last row under a divider.
            bool bigButton = height >= BigButtonMinPanelHeight;
            int actionRow = bigButton ? bottom - BigButtonHeight : bottom;
            if (!bigButton)
                canvas.AddLine(x + 1, actionRow, x + width - 1, actionRow, Color.FromArgb(120, border.R, border.G, border.B), 1.0);
            var statusLines = string.IsNullOrWhiteSpace(statusMessage)
                ? new List<string>()
                : TextWrapper.WrapText(statusMessage, inner).Take(2).ToList();
            int maxRow = actionRow - (bigButton ? 1 : 0) - statusLines.Count;

            int ty = y + 1;
            void Line(string text, Color color)
            {
                if (ty >= maxRow) return;
                canvas.AddText(x + 2, ty++, Truncate(text, inner), color);
            }
            void Wrapped(string text, Color color, int maxLines = 99)
            {
                if (string.IsNullOrWhiteSpace(text)) return;
                foreach (var line in TextWrapper.WrapText(text, inner).Take(maxLines))
                    Line(line, color);
            }

            string branch = onRail ? (model.DuoName ?? "Shared") : node.Branch;
            Line($"{branch} · Tier {node.Tier} · {node.Type}", AsciiArtAssets.Colors.Cyan);

            if (ty < maxRow)
            {
                var look = LookFor(state, false);
                canvas.AddBox(x + 2, ty, 2, 1, look.Border, look.Fill, 0, look.Thickness);
                string rankText = maxRank > 1 ? $"Rank {rank}/{maxRank}" : rank > 0 ? "Learned" : "Not learned";
                canvas.AddText(x + 5, ty, rankText, AsciiArtAssets.Colors.White);
                int px = x + 5 + rankText.Length + 1;
                if (maxRank > 1 && px + maxRank <= x + width - 2)
                {
                    for (int i = 0; i < maxRank; i++)
                        canvas.AddBox(px + i, ty, 1, 1, DetailFill, i < rank ? PipFull : PipEmpty, 0, 2);
                }
                ty++;
            }
            var ownerTree = SkillTreePrerequisites.FindOwnerTree(node);
            var hybrid = HybridTrees.ForTree(ownerTree);
            string spName = hybrid != null ? $"{hybrid.ClassA} + {hybrid.ClassB}" : ownerPath?.ToString() ?? "";
            Line($"{node.Cost} {spName} SP per rank", AsciiArtAssets.Colors.Gray);
            if (hybrid == null && ownerPath.HasValue)
            {
                var prog = player.Progression;
                int pts = prog.GetClassPoints(ownerPath.Value);
                Wrapped($"Free SP {prog.GetAvailableSkillPoints(ownerPath.Value)} = {pts} class pts − {prog.GetSpentSkillPoints(ownerPath.Value)} spent. +1 per level-up with a {ownerPath.Value} equipped.", Dim, 2);
            }
            ty++;

            Wrapped(node.DisplayEffect, AsciiArtAssets.Colors.White, 5);
            if (!string.IsNullOrWhiteSpace(node.Payoff))
                Wrapped($"» {node.Payoff}", AsciiArtAssets.Colors.Gray, 2);
            ty++;

            if (hybrid != null)
            {
                // Hybrid rules as a checklist: each line is something the player can go and fix.
                var prog = player.Progression;
                int need = HybridTrees.UnlockPointsPerParent;
                int a = prog.GetClassPoints(hybrid.ParentA);
                int b = prog.GetClassPoints(hybrid.ParentB);
                int gate = HybridTrees.TierGate(node.Tier);
                int lower = Math.Min(a, b);
                void Check(bool ok, string text)
                {
                    if (ty >= maxRow) return;
                    canvas.AddText(x + 2, ty, ok ? LearnedGlyph : "x", ok ? AsciiArtAssets.Colors.Green : AsciiArtAssets.Colors.Yellow);
                    foreach (var line in TextWrapper.WrapText(text, inner - 2).Take(2))
                    {
                        if (ty >= maxRow) return;
                        canvas.AddText(x + 4, ty++, line, ok ? AsciiArtAssets.Colors.Gray : AsciiArtAssets.Colors.White);
                    }
                }
                Line($"HYBRID · {hybrid.Key.ToUpperInvariant()}", AsciiArtAssets.Colors.Magenta);
                Check(a >= need && b >= need,
                    $"Opens at {need} points in both {hybrid.ClassA} ({hybrid.ParentA}) and {hybrid.ClassB} ({hybrid.ParentB}) · you have {a} and {b}");
                if (node.Tier > 0)
                    Check(lower >= gate, $"Tier {node.Tier} needs {gate} in both · your lower is {lower}");
                Check(prog.GetAvailableSkillPoints(hybrid.ParentA) + prog.GetAvailableSkillPoints(hybrid.ParentB) >= node.Cost,
                    $"Each rank spends half from each class's free SP · free: {prog.GetAvailableSkillPoints(hybrid.ParentA)} + {prog.GetAvailableSkillPoints(hybrid.ParentB)}");
                var pred = ownerTree != null ? SkillTreePrerequisites.GetImmediatePredecessor(ownerTree, node) : null;
                if (node.Tier > 0)
                    Check(pred == null || prog.GetSkillRank(pred.Id) > 0,
                        pred != null ? $"Learn {pred.Name} first" : "First skill on its branch");
                ty++;
            }
            else
            {
                // Why it's locked, in order of what to fix first.
                if (!onRail && !depth.IsOpen(node.Tier))
                    Wrapped($"Opens at {depth.Gate(node.Tier)} {model.PrimaryPath} pts (have {depth.Points})", AsciiArtAssets.Colors.Yellow, 2);
                string req = SkillTreePrerequisites.FormatRequirementSummary(node);
                if (!string.IsNullOrEmpty(req))
                    Wrapped(req, state == SkillTreeService.NodeViewState.Locked ? AsciiArtAssets.Colors.Yellow : Dim, 2);
            }

            if (ownerTree != null && node.Tier > 0 && !IsCenterBranch(node.Branch))
            {
                var chain = SkillTreePrerequisites.GetBranchSequenceRootToTip(ownerTree, node.Branch);
                if (chain.Count > 1)
                {
                    string arrow = $" {AsciiArtAssets.UIElements.ArrowRight} ";
                    string text = "Chain: " + string.Join(arrow, chain.Select(n =>
                        string.Equals(n.Id, node.Id, StringComparison.OrdinalIgnoreCase) ? $"[{n.Name}]" : n.Name));
                    Wrapped(text, Dim, 3);
                }
            }

            // Status (last learn result etc.) always gets the rows just above the divider.
            int sy = Math.Max(y + 1, maxRow);
            foreach (var line in statusLines)
                canvas.AddText(x + 2, sy++, Truncate(line, inner), AsciiArtAssets.Colors.Yellow);

            if (bigButton)
            {
                DrawLearnButton(state, rank, node.Cost, spName, x + 2, actionRow, inner);
                return;
            }

            string action = state switch
            {
                SkillTreeService.NodeViewState.Maxed => "Fully ranked",
                SkillTreeService.NodeViewState.Available => rank > 0
                    ? $"[L] Raise rank  (-{node.Cost} {spName} SP)"
                    : $"[L] Learn  (-{node.Cost} {spName} SP)",
                SkillTreeService.NodeViewState.Unaffordable => $"Need {node.Cost} {spName} SP",
                SkillTreeService.NodeViewState.Locked => "Locked",
                _ => "Earn points on this path first"
            };
            action = Truncate(action, inner);
            Color actionColor = state == SkillTreeService.NodeViewState.Available ? AsciiArtAssets.Colors.Gold
                : state == SkillTreeService.NodeViewState.Maxed ? AsciiArtAssets.Colors.Green
                : Dim;
            canvas.AddText(x + 2, actionRow, action, actionColor);
            if (state == SkillTreeService.NodeViewState.Available)
            {
                clickableElements.Add(new ClickableElement
                {
                    X = x + 2,
                    Y = actionRow,
                    Width = action.Length,
                    Height = 1,
                    Type = ElementType.Button,
                    Value = "l",
                    DisplayText = action
                });
            }
        }

        /// <summary>Full-width learn button at the foot of the details panel; greyed out with the reason when it can't be pressed.</summary>
        private void DrawLearnButton(SkillTreeService.NodeViewState state, int rank, int cost, string spName, int x, int y, int w)
        {
            bool canLearn = state == SkillTreeService.NodeViewState.Available;
            string label = state switch
            {
                SkillTreeService.NodeViewState.Maxed => $"{LearnedGlyph} FULLY RANKED",
                SkillTreeService.NodeViewState.Available => $"{(rank > 0 ? "RAISE RANK" : "LEARN SKILL")}  [L]",
                SkillTreeService.NodeViewState.Unaffordable => "NEED MORE SP",
                SkillTreeService.NodeViewState.Locked => "LOCKED",
                _ => "OTHER PATH"
            };
            string sub = state switch
            {
                SkillTreeService.NodeViewState.Maxed => "",
                SkillTreeService.NodeViewState.Locked => "learn its prerequisites first",
                SkillTreeService.NodeViewState.WrongPath => "earn points on this path first",
                _ => cost <= 0 ? "free" : $"costs {cost} {spName} SP"
            };
            var look = state switch
            {
                SkillTreeService.NodeViewState.Available => LearnButtonLook,
                SkillTreeService.NodeViewState.Maxed => LearnedLook,
                SkillTreeService.NodeViewState.Unaffordable => NeedSpLook,
                _ => ClosedLook
            };

            DrawCard(x, y, w, look.Border, look.Fill, canLearn ? 3 : look.Thickness, BigButtonHeight);
            void Centered(int row, string text, Color color)
            {
                text = Truncate(text, w - 2);
                if (text.Length > 0)
                    canvas.AddText(x + (w - text.Length) / 2, row, text, color);
            }
            Centered(y + 1, label, look.Name);
            Centered(y + 2, sub, look.Status);

            if (canLearn)
            {
                clickableElements.Add(new ClickableElement
                {
                    X = x,
                    Y = y,
                    Width = w,
                    Height = BigButtonHeight,
                    Type = ElementType.Button,
                    Value = "l",
                    DisplayText = label
                });
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        public static IReadOnlyList<SkillTreeNodeDefinition> OrderNodes(IEnumerable<SkillTreeNodeDefinition> source) =>
            SkillTreeService.OrderNodes(source);

        public static List<string> BuildBranchOrder(IReadOnlyList<SkillTreeNodeDefinition> nodes)
        {
            var order = new List<string>();
            foreach (var n in nodes.OrderBy(n => n.Tier))
            {
                if (IsCenterBranch(n.Branch))
                    continue;
                if (!order.Any(b => string.Equals(b, n.Branch, StringComparison.OrdinalIgnoreCase)))
                    order.Add(n.Branch);
            }
            if (order.Count == 0)
                order.Add("Core");
            return order;
        }

        private static bool IsCenterBranch(string? branch) =>
            string.Equals(branch, "Core", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(branch, "Confluence", StringComparison.OrdinalIgnoreCase);

        public static int ResolveColumn(SkillTreeNodeDefinition node, IReadOnlyList<string> branchOrder, int centerCol)
        {
            if (IsCenterBranch(node.Branch))
                return centerCol;
            for (int i = 0; i < branchOrder.Count; i++)
            {
                if (string.Equals(branchOrder[i], node.Branch, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return centerCol;
        }

        private void AddBackOption(int x, int y, int width)
        {
            var back = new ClickableElement
            {
                X = x,
                Y = y,
                Width = Math.Min(22, width),
                Height = 1,
                Type = ElementType.MenuOption,
                Value = "0",
                DisplayText = MenuOptionFormatter.Format(0, "Back")
            };
            clickableElements.Add(back);
            canvas.AddMenuOption(x, y, 0, "Back", AsciiArtAssets.Colors.White, back.IsHovered);
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || max <= 0) return "";
            return text.Length <= max ? text : text.Substring(0, Math.Max(1, max - 1)) + "…";
        }

        /// <summary>Screen columns for the box layout.</summary>
        private sealed class Geometry
        {
            public BoxLayout Layout { get; }
            public int TreeLeft { get; }
            public int RailX { get; }
            public int BoxW { get; }

            public double PadX { get; }
            public double PadY { get; }

            public Geometry(BoxLayout layout, int treeLeft, int railX, int boxW, double padX, double padY)
            {
                PadX = padX;
                PadY = padY;
                Layout = layout;
                TreeLeft = treeLeft;
                RailX = railX;
                BoxW = boxW;
            }

            public int LaneX(double colPos) => TreeLeft + (int)Math.Round(colPos * (BoxW + LaneGap));
            public int X(int allIndex) => LaneX(Layout.ColPos[allIndex]);
            public double CenterX(int allIndex) => X(allIndex) + BoxW / 2.0;
            public double Left(int allIndex) => X(allIndex) - PadX;
            public double Right(int allIndex) => X(allIndex) + BoxW + PadX;
            public double TopEdge(int allIndex) => Layout.Top[allIndex] - PadY;
            public double BottomEdge(int allIndex) => Layout.Top[allIndex] + NodeHeight + PadY;
        }

        private readonly struct Viewport
        {
            public int Top { get; }
            public int Bottom { get; }
            public int Scroll { get; }

            public Viewport(int top, int bottom, int scroll)
            {
                Top = top;
                Bottom = bottom;
                Scroll = scroll;
            }

            public int ToScreen(int contentRow) => Top + contentRow - Scroll;
            public bool Contains(int screenRow) => screenRow >= Top && screenRow < Bottom;
        }

        /// <summary>Tier point gates for the viewed tree and how many points the player has toward them.</summary>
        private sealed class DepthInfo
        {
            private readonly bool hybrid;
            public int Points { get; }
            public int? NextTier { get; }

            private DepthInfo(bool hybrid, int points, int maxTier)
            {
                this.hybrid = hybrid;
                Points = points;
                for (int t = 1; t <= maxTier; t++)
                {
                    if (Gate(t) > points)
                    {
                        NextTier = t;
                        break;
                    }
                }
            }

            public int Gate(int tier) => hybrid ? HybridTrees.TierGate(tier) : SkillTreesConfig.TierPointGate(tier);
            public bool IsOpen(int tier) => tier <= 0 || Points >= Gate(tier);

            public static DepthInfo For(CharacterProgression progression, SkillTreeDefinition tree, WeaponType path)
            {
                int maxTier = tree.Nodes?.Count > 0 ? tree.Nodes.Max(n => n.Tier) : 0;
                if (HybridTrees.ForTree(tree) is HybridInfo h)
                    return new DepthInfo(true, HybridTrees.LowerParentPoints(progression, h), maxTier);
                return new DepthInfo(false, progression.GetClassPoints(path), maxTier);
            }
        }

        /// <summary>Precomputed content-space tops for each node id, plus each tier's band.</summary>
        public sealed class ContentLayout
        {
            public int ContentHeight { get; }
            public IReadOnlyDictionary<string, int> Tops { get; }
            public IReadOnlyDictionary<int, (int top, int height)> Bands { get; }

            public ContentLayout(int contentHeight, IReadOnlyDictionary<string, int> tops, IReadOnlyDictionary<int, (int top, int height)> bands)
            {
                ContentHeight = contentHeight;
                Tops = tops;
                Bands = bands;
            }
        }
    }
}
