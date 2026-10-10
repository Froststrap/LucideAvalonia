using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using LucideAvalonia.Enum;

namespace LucideAvalonia;

public partial class Lucide : UserControl
{
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

    private void UpdateIcon()
    {
        // LucideIconRegistry caches parsed geometries per icon, so this is
        // cheap to call repeatedly. No global dictionary, no IconsChanged event.
        IconSource = LucideIconRegistry.Build(Icon, StrokeBrush, StrokeThickness, IsFilled);
    }
}