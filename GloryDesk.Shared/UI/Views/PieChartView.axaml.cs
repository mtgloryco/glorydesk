using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using InventoryManagementSystem.UI.ViewModels;
using PathShape = Avalonia.Controls.Shapes.Path;

namespace InventoryManagementSystem.UI.Views;

public partial class PieChartView : UserControl
{
    public static readonly StyledProperty<IEnumerable<DashboardPieSlice>?> SlicesProperty =
        AvaloniaProperty.Register<PieChartView, IEnumerable<DashboardPieSlice>?>(nameof(Slices));

    public IEnumerable<DashboardPieSlice>? Slices
    {
        get => GetValue(SlicesProperty);
        set => SetValue(SlicesProperty, value);
    }

    static PieChartView()
    {
        SlicesProperty.Changed.AddClassHandler<PieChartView>((view, _) => view.RenderChart());
    }

    public PieChartView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RenderChart();
    }

    private void RenderChart()
    {
        if (ChartCanvas == null || LegendItems == null)
        {
            return;
        }

        ChartCanvas.Children.Clear();

        var slices = Slices?.Where(s => s.Value > 0).ToList() ?? new List<DashboardPieSlice>();
        LegendItems.ItemsSource = slices;

        if (slices.Count == 0)
        {
            ChartCanvas.Children.Add(new Ellipse
            {
                Width = 150,
                Height = 150,
                Fill = new SolidColorBrush(Color.Parse("#E5E7EB"))
            });
            return;
        }

        var total = slices.Sum(s => s.Value);
        foreach (var slice in slices)
        {
            slice.Percentage = total > 0 ? slice.Value / total * 100 : 0;
            slice.PercentDisplay = $"{slice.Percentage:0.#}%";
            slice.ColorBrush = new SolidColorBrush(Color.Parse(slice.Color));
        }

        const double cx = 75;
        const double cy = 75;
        const double outerRadius = 70;
        const double innerRadius = 42;
        var startAngle = -90.0;

        foreach (var slice in slices)
        {
            var sweep = slice.Value / total * 360;
            if (sweep <= 0)
            {
                continue;
            }

            var path = CreateDonutSlice(cx, cy, outerRadius, innerRadius, startAngle, sweep, slice.ColorBrush ?? Brushes.Gray);
            ChartCanvas.Children.Add(path);
            startAngle += sweep;
        }
    }

    private static PathShape CreateDonutSlice(double cx, double cy, double outerR, double innerR, double startAngle, double sweepAngle, IBrush fill)
    {
        if (sweepAngle >= 359.99)
        {
            sweepAngle = 359.99;
        }

        var endAngle = startAngle + sweepAngle;
        var outerStart = PolarToCartesian(cx, cy, outerR, startAngle);
        var outerEnd = PolarToCartesian(cx, cy, outerR, endAngle);
        var innerEnd = PolarToCartesian(cx, cy, innerR, endAngle);
        var innerStart = PolarToCartesian(cx, cy, innerR, startAngle);

        var largeArc = sweepAngle > 180;

        var geometry = new PathGeometry();
        var figure = new PathFigure { StartPoint = outerStart, IsClosed = true };
        figure.Segments!.Add(new ArcSegment
        {
            Point = outerEnd,
            Size = new Size(outerR, outerR),
            IsLargeArc = largeArc,
            SweepDirection = SweepDirection.Clockwise
        });
        figure.Segments.Add(new LineSegment { Point = innerEnd });
        figure.Segments.Add(new ArcSegment
        {
            Point = innerStart,
            Size = new Size(innerR, innerR),
            IsLargeArc = largeArc,
            SweepDirection = SweepDirection.CounterClockwise
        });

        geometry.Figures!.Add(figure);

        return new PathShape
        {
            Data = geometry,
            Fill = fill,
            Stroke = Brushes.White,
            StrokeThickness = 1.5
        };
    }

    private static Point PolarToCartesian(double cx, double cy, double radius, double angleDegrees)
    {
        var angle = angleDegrees * Math.PI / 180.0;
        return new Point(
            cx + radius * Math.Cos(angle),
            cy + radius * Math.Sin(angle));
    }
}
