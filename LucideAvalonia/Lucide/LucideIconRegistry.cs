using System.Collections.Concurrent;
using Avalonia.Media;
using LucideAvalonia.Enum;

namespace LucideAvalonia;

/// <summary>
/// Builds Lucide icons on demand. Geometry parsing is deferred until an icon
/// is first requested and then cached for the lifetime of the process.
/// </summary>
internal static class LucideIconRegistry
{
    private static readonly ConcurrentDictionary<LucideIconNames, Geometry[]> GeometryCache = new();

    public static DrawingImage? Build(
        LucideIconNames name,
        IBrush? brush,
        double thickness,
        bool filled)
    {
        var geometries = GetGeometries(name);
        if (geometries.Length == 0)
            return null;

        var effectiveBrush = brush ?? Brushes.Black;
        if (thickness <= 0)
            thickness = 2;

        var group = new DrawingGroup();
        var children = group.Children;

        foreach (var geometry in geometries)
        {
            var pen = new Pen(effectiveBrush, thickness)
            {
                LineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round,
                MiterLimit = 10,
            };

            children.Add(new GeometryDrawing
            {
                Geometry = geometry,
                Pen = pen,
                Brush = filled ? effectiveBrush : null,
            });
        }

        return new DrawingImage(group);
    }

    private static Geometry[] GetGeometries(LucideIconNames name)
        => GeometryCache.GetOrAdd(name, static n => LucideIconData.Get(n) ?? []);
}