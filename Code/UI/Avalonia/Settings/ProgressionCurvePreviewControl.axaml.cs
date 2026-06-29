using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using RPGGame.UI.Avalonia.Settings.ViewModels;

namespace RPGGame.UI.Avalonia.Settings
{
    public partial class ProgressionCurvePreviewControl : UserControl
    {
        public static readonly StyledProperty<ProgressionCurvePreviewViewModel?> PreviewModelProperty =
            AvaloniaProperty.Register<ProgressionCurvePreviewControl, ProgressionCurvePreviewViewModel?>(
                nameof(PreviewModel));

        private global::Avalonia.Controls.Canvas? chartCanvas;

        public ProgressionCurvePreviewControl()
        {
            chartCanvas = new global::Avalonia.Controls.Canvas
            {
                Height = 220,
                Margin = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(Color.Parse("#FF1E1E1E"))
            };
            Content = chartCanvas;
        }

        public ProgressionCurvePreviewViewModel? PreviewModel
        {
            get => GetValue(PreviewModelProperty);
            set => SetValue(PreviewModelProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == PreviewModelProperty || change.Property == BoundsProperty)
                RedrawChart();
        }

        public void RedrawChart()
        {
            if (chartCanvas == null || PreviewModel == null)
                return;

            chartCanvas.Children.Clear();
            var points = PreviewModel.GetChartPoints();
            if (points.Count < 2)
                return;

            double width = Math.Max(200, chartCanvas.Bounds.Width > 0 ? chartCanvas.Bounds.Width : 600);
            double height = chartCanvas.Height;
            const double pad = 24;

            int maxHp = 1;
            foreach (var p in points)
                maxHp = Math.Max(maxHp, Math.Max(p.enemyHp, p.playerHp));

            var enemyBrush = new SolidColorBrush(Color.Parse("#FF4FC3F7"));
            var playerBrush = new SolidColorBrush(Color.Parse("#FF81C784"));
            var gridBrush = new SolidColorBrush(Color.Parse("#FF333333"));

            for (int i = 0; i <= 4; i++)
            {
                double y = pad + (height - pad * 2) * i / 4.0;
                chartCanvas.Children.Add(new Line
                {
                    StartPoint = new Point(pad, y),
                    EndPoint = new Point(width - pad, y),
                    Stroke = gridBrush,
                    StrokeThickness = 1
                });
            }

            DrawPolyline(points, p => p.enemyHp, maxHp, width, height, pad, enemyBrush);
            DrawPolyline(points, p => p.playerHp, maxHp, width, height, pad, playerBrush);

            chartCanvas.Children.Add(MakeLegend("Enemy HP (Goblin)", enemyBrush, pad, 6));
            chartCanvas.Children.Add(MakeLegend("Player max HP", playerBrush, pad + 160, 6));
        }

        private static TextBlock MakeLegend(string text, IBrush brush, double x, double y) =>
            new()
            {
                Text = text,
                Foreground = brush,
                FontSize = 11,
                [global::Avalonia.Controls.Canvas.LeftProperty] = x,
                [global::Avalonia.Controls.Canvas.TopProperty] = y
            };

        private void DrawPolyline(
            IReadOnlyList<(int level, int enemyHp, int playerHp)> points,
            Func<(int level, int enemyHp, int playerHp), int> pick,
            int maxHp,
            double width,
            double height,
            double pad,
            IBrush stroke)
        {
            var polyline = new Polyline
            {
                Stroke = stroke,
                StrokeThickness = 2,
                Fill = null
            };

            int minLevel = points[0].level;
            int maxLevel = points[^1].level;
            foreach (var p in points)
            {
                double xNorm = maxLevel > minLevel ? (p.level - minLevel) / (double)(maxLevel - minLevel) : 0;
                double yNorm = pick(p) / (double)maxHp;
                double x = pad + xNorm * (width - pad * 2);
                double y = height - pad - yNorm * (height - pad * 2);
                polyline.Points!.Add(new Point(x, y));
            }

            chartCanvas!.Children.Add(polyline);
        }
    }
}
