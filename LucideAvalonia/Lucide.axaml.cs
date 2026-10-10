using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using LucideAvalonia.Enum;
using System.Collections.Concurrent;

namespace LucideAvalonia;

public partial class Lucide : UserControl
{
    private static readonly ConcurrentDictionary<(LucideIconNames Icon, string BrushKey, double Thickness, bool Filled), DrawingImage> CloneCache = new();

    public static event EventHandler? IconsChanged;

    public static void NotifyIconsChanged() => IconsChanged?.Invoke(null, EventArgs.Empty);

    public Lucide()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static readonly StyledProperty<object?> IconSourceProperty =
        AvaloniaProperty.Register<Lucide, object?>("IconSource");

    public object? IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public static readonly StyledProperty<LucideIconNames> IconProperty =
        AvaloniaProperty.Register<Lucide, LucideIconNames>("Icon");

    public LucideIconNames Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<IBrush?> StrokeBrushProperty =
        AvaloniaProperty.Register<Lucide, IBrush?>(
            nameof(StrokeBrush),
            defaultBindingMode: BindingMode.TwoWay);

    public IBrush? StrokeBrush
    {
        get => GetValue(StrokeBrushProperty);
        set => SetValue(StrokeBrushProperty, value);
    }

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Lucide, double>("StrokeThickness");

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public static readonly StyledProperty<bool> IsFilledProperty =
        AvaloniaProperty.Register<Lucide, bool>(nameof(IsFilled));

    public bool IsFilled
    {
        get => GetValue(IsFilledProperty);
        set => SetValue(IsFilledProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IconProperty ||
            change.Property == StrokeBrushProperty ||
            change.Property == StrokeThicknessProperty ||
            change.Property == IsFilledProperty)
        {
            UpdateIcon();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        IconsChanged += OnIconsChanged;
        UpdateIcon();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        IconsChanged -= OnIconsChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnIconsChanged(object? sender, EventArgs e) => UpdateIcon();

    private void UpdateIcon()
    {
        var key = Icon.ToString();
        if (Application.Current?.TryGetResource(key, ThemeVariant.Default, out var value) == true &&
            value is DrawingImage drawing)
        {
            if (StrokeBrush is null && StrokeThickness <= 0 && !IsFilled)
            {
                IconSource = drawing;
                return;
            }

            string brushKey = StrokeBrush?.ToString() ?? string.Empty;
            IconSource = CloneCache.GetOrAdd(
                (Icon, brushKey, StrokeThickness, IsFilled),
                _ => CloneDrawing(drawing));
        }
        else
        {
            IconSource = null;
        }
    }

    private DrawingImage CloneDrawing(DrawingImage original)
    {
        if (original.Drawing is not DrawingGroup originalGroup)
            return original;

        var newGroup = new DrawingGroup();
        foreach (var child in originalGroup.Children)
        {
            if (child is GeometryDrawing geo)
            {
                var newGeo = geo.Geometry?.Clone();
                if (newGeo == null) continue;

                var brush = StrokeBrush ?? Brushes.Black;
                var thickness = StrokeThickness > 0 ? StrokeThickness : (geo.Pen?.Thickness ?? 2);
                var dashStyle = geo.Pen?.DashStyle;
                var lineCap = geo.Pen?.LineCap ?? PenLineCap.Round;
                var lineJoin = geo.Pen?.LineJoin ?? PenLineJoin.Round;
                var miterLimit = geo.Pen?.MiterLimit ?? 10;

                var newPen = new Pen(brush, thickness, dashStyle, lineCap, lineJoin, miterLimit);

                var newDrawing = new GeometryDrawing
                {
                    Geometry = newGeo,
                    Pen = newPen,
                    Brush = IsFilled ? brush : geo.Brush
                };

                newGroup.Children.Add(newDrawing);
            }
            else
            {
                newGroup.Children.Add(child);
            }
        }

        return new DrawingImage(newGroup);
    }
}
