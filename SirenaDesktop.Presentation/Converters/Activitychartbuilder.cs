using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace SirenaDesktop.ViewModels.Charting;

public sealed record AppUsageBar(string Name, double Minutes, SKColor Color);

public sealed class ActivityChartModel
{
    public ISeries[] Series { get; init; } = [];
    public Axis[] XAxes { get; init; } = [];
    public Axis[] YAxes { get; init; } = [];
    public double Height { get; init; }
}

public static class ActivityChartBuilder
{
    private const double BarThickness = 22;
    private const double RowHeight = 34;
    private const double BaseHeight = 60;

    public static ActivityChartModel Build(
        List<AppUsageBar> bars,
        SKColor labelColor,
        SKColor separatorColor)
    {
        if (bars is null || bars.Count == 0)
            return new ActivityChartModel();

        var sorted = bars.OrderBy(b => b.Minutes).ToList();
        var count = sorted.Count;

        var labels = sorted.Select(b => b.Name).ToArray();
        var maxMinutes = sorted[^1].Minutes;
        if (maxMinutes <= 0) maxMinutes = 1;

        var step = ComputeStep(maxMinutes);
        var axisMax = Math.Ceiling(maxMinutes / step) * step;

        var series = new List<ISeries>(count);
        for (int i = 0; i < count; i++)
        {
            var values = new double?[count];
            values[i] = sorted[i].Minutes;

            series.Add(new RowSeries<double?>
            {
                Name = sorted[i].Name,
                Values = values,
                Fill = new SolidColorPaint(sorted[i].Color),
                Stroke = null,
                MaxBarWidth = BarThickness,
                Rx = BarThickness / 2,
                Ry = BarThickness / 2,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = DataLabelsPosition.End,
                DataLabelsFormatter = p => FormatMinutes(p.Coordinate.PrimaryValue),
                AnimationsSpeed = TimeSpan.FromMilliseconds(300),
                IsHoverable = false
            });
        }

        var xAxis = new Axis
        {
            MinLimit = 0,
            MaxLimit = axisMax,
            MinStep = step,
            ForceStepToMin = true,
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(labelColor),
            SeparatorsPaint = new SolidColorPaint(separatorColor) { StrokeThickness = 0.5f },
            Labeler = FormatMinutes
        };

        var yAxis = new Axis
        {
            Labels = labels,
            TextSize = 12,
            LabelsPaint = new SolidColorPaint(labelColor),
            SeparatorsPaint = null
        };

        return new ActivityChartModel
        {
            Series = series.ToArray(),
            XAxes = new[] { xAxis },
            YAxes = new[] { yAxis },
            Height = BaseHeight + count * RowHeight
        };
    }

    private static double ComputeStep(double maxMinutes)
    {
        if (maxMinutes <= 20) return 5;
        if (maxMinutes <= 60) return 10;
        if (maxMinutes <= 180) return 30;
        if (maxMinutes <= 480) return 60;
        if (maxMinutes <= 1440) return 120;
        return 240;
    }

    internal static string FormatMinutes(double minutes)
    {
        var total = (int)Math.Round(minutes);
        if (total < 60) return $"{total}m";
        var h = total / 60;
        var m = total % 60;
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}