using System.Collections.Concurrent;
using Avalonia.Media;
using LucideAvalonia.Enum;

namespace LucideAvalonia;

internal static class LucideIconRegistry
{
    private static readonly ConcurrentDictionary<LucideIconNames, Geometry[]> GeometryCache = new();
    private static readonly ConcurrentDictionary<string, Geometry[]> SvgCache = new(StringComparer.Ordinal);

    public static DrawingImage? Build(
        LucideIconSource? source,
        IBrush? brush,
        double thickness,
        bool filled)
    {
        if (source is null)
            return null;

        var geometries = source.IsSvg
            ? ParseSvg(source.SvgPath!)
            : GetGeometries(source.Name!.Value);

        if (geometries.Length == 0)
            return null;

        var effectiveBrush = brush ?? Brushes.Black;

        var group = new DrawingGroup();
        var children = group.Children;

        foreach (var geometry in geometries)
        {
            var pen = thickness > 0
                ? new Pen(effectiveBrush, thickness)
                {
                    LineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round,
                    MiterLimit = 10,
                }
                : null;

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

    private static Geometry[] ParseSvg(string pathData)
        => SvgCache.GetOrAdd(pathData, static data =>
        {
            if (string.IsNullOrWhiteSpace(data))
                return [];

            try
            {
                return [Geometry.Parse(data)];
            }
            catch
            {
                return [];
            }
        });
}