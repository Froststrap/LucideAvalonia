using LucideAvalonia.Enum;

namespace LucideAvalonia;

public sealed class LucideIconSource
{
    public LucideIconNames? Name { get; }
    public string? SvgPath { get; }

    public bool IsSvg => SvgPath is not null;

    private LucideIconSource(LucideIconNames? name, string? svgPath)
    {
        Name = name;
        SvgPath = svgPath;
    }

    public static LucideIconSource FromName(LucideIconNames name) => new(name, null);

    public static LucideIconSource FromSvg(string svgPath) => new(null, svgPath);

    public static implicit operator LucideIconSource?(LucideIconNames? name)
        => name.HasValue ? FromName(name.Value) : null;

    public static implicit operator LucideIconSource?(string? svgPath)
        => string.IsNullOrEmpty(svgPath) ? null : FromSvg(svgPath);

    public override string ToString() => Name?.ToString() ?? SvgPath ?? string.Empty;
}